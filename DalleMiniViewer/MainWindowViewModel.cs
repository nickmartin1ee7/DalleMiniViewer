using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace DalleMiniViewer
{
    public class MainWindowViewModel : INotifyPropertyChanged
    {
        private const string GENERATE_ENDPOINT = "https://bf.dallemini.ai/generate";
        private readonly HttpClient _client = new();
        private readonly Stopwatch _sw = new();
        private CancellationTokenSource? _cts;
        private string _promptText = string.Empty;
        private string _promptButtonContent = string.Empty;
        private bool _promptTextBoxEnabled = true;
        private bool _cancelLastRequest;

        public bool PromptTextBoxEnabled
        {
            get => _promptTextBoxEnabled;
            set => SetProperty(ref _promptTextBoxEnabled, value);
        }

        public string PromptButtonContent
        {
            get => _promptButtonContent;
            set => SetProperty(ref _promptButtonContent, value);
        }

        public string PromptText
        {
            get => _promptText;
            set => SetProperty(ref _promptText, value);
        }

        public ObservableCollection<BitmapImage> ImageSources { get; set; } = new();

        public ICommand PromptButton { get; }

        public MainWindowViewModel()
        {
            ResetPromptContent();
            _client.Timeout = TimeSpan.FromMinutes(5);

            PromptButton = new DelegatedCommand(async _ =>
            {
                if (_cts is not null && !_cts.IsCancellationRequested)
                {
                    _cts.Cancel();
                    PromptButtonContent = "Cancelled";
                    _cancelLastRequest = true;
                    PromptTextBoxEnabled = true;
                    return;
                }

                _cts = new();
                HttpResponseMessage? response = null;

                _ = Task.Run(async () =>
                {
                    PromptTextBoxEnabled = false;

                    while (!_cts.IsCancellationRequested)
                    {
                        PromptButtonContent = _sw.Elapsed.ToString("hh\\:mm\\:ss");
                        await Task.Delay(500);
                    }
                });

                _sw.Restart();
                try
                {
                    while (!_cts.IsCancellationRequested)
                    {
                        try
                        {
                            response = await _client.PostAsync(GENERATE_ENDPOINT, new StringContent(@$"{{""prompt"":""{PromptText}""}}", Encoding.UTF8, "application/json"), _cts.Token);
                        }
                        catch (TaskCanceledException)
                        {
                            return;
                        }

                        if (response.IsSuccessStatusCode)
                        {
                            _cts.Cancel();
                        }
                        else if ((int)response.StatusCode == 429)
                        {
                            await Task.Delay(Random.Shared.Next(5_000, 10_001)); // 5-10s
                        }
                        else
                        {
                            await Task.Delay(Random.Shared.Next(500, 2_001));
                        }
                    }

                    _sw.Stop();

                    PromptTextBoxEnabled = true;

                    if (_cancelLastRequest || response is null)
                    {
                        _cancelLastRequest = false;
                        return;
                    }

                    var stringContent = await response.Content.ReadAsStringAsync();

                    var imagePayload = JsonSerializer.Deserialize<ImagePayload>(stringContent, new JsonSerializerOptions(JsonSerializerDefaults.Web));

                    if (imagePayload is null) return;

                    ImageSources.Clear();

                    foreach (var b64Image in imagePayload.Images)
                    {
                        byte[] imageBytes = Convert.FromBase64String(b64Image);

                        var bitmap = new BitmapImage();
                        bitmap.BeginInit();
                        bitmap.StreamSource = new MemoryStream(imageBytes);
                        bitmap.EndInit();

                        ImageSources.Add(bitmap);
                    }
                }
                catch (Exception ex)
                {
                    _cts.Cancel();
                    PromptText = $"Request Failed: {ex}";
                    return;
                }
            });
        }

        protected bool SetProperty<T>(ref T field, T newValue, [CallerMemberName] string? propertyName = null)
        {
            if (!Equals(field, newValue))
            {
                field = newValue;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
                return true;
            }

            return false;
        }

        public void ResetPromptContent()
        {
            PromptButtonContent = "Generate";
        }

        public event PropertyChangedEventHandler? PropertyChanged;
    }
}

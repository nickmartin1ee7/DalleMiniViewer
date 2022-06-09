using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
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
        private string _promptText;
        private bool _promptButtonEnabled = true;
        private string _promptButtonContent = "Go";

        public event PropertyChangedEventHandler? PropertyChanged;

        public bool PromptButtonEnabled
        {
            get => _promptButtonEnabled;
            set => SetProperty(ref _promptButtonEnabled, value);
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
            _client.Timeout = TimeSpan.FromMinutes(3);

            PromptButton = new DelegatedCommand(async _ =>
            {
                if (_cts is not null && !_cts.IsCancellationRequested)
                {
                    _cts.Cancel();
                }

                _cts = new();
                HttpResponseMessage? response = null;

                var buttonTimeUpdaterTask = Task.Run(async () =>
                {
                    while (!_cts.IsCancellationRequested)
                    {
                        await Task.Delay(100);
                        PromptButtonContent = _sw.Elapsed.ToString();
                    }
                });

                _sw.Restart();

                while (!_cts.IsCancellationRequested)
                {
                    response = await _client.PostAsync(GENERATE_ENDPOINT, new StringContent(@$"{{""prompt"":""{PromptText}""}}", Encoding.UTF8, "application/json"), _cts.Token);

                    if (response.IsSuccessStatusCode)
                    {
                        _cts.Cancel();
                    }
                    else
                    {
                        await Task.Delay(Random.Shared.Next(250, 501));
                    }
                }

                _sw.Stop();

                if (response is null) return;

                var stringContent = await response.Content.ReadAsStringAsync();

                var imagePayload = JsonSerializer.Deserialize<ImagePayload>(stringContent, new JsonSerializerOptions(JsonSerializerDefaults.Web));

                if (imagePayload is null) return;

                foreach (var b64Image in imagePayload.Images)
                {
                    byte[] imageBytes = Convert.FromBase64String(b64Image);

                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.StreamSource = new MemoryStream(imageBytes);
                    bitmap.EndInit();

                    ImageSources.Add(bitmap);
                }
            });
        }

        protected bool SetProperty<T>(ref T field, T newValue, [CallerMemberName] string propertyName = null)
        {
            if (!Equals(field, newValue))
            {
                field = newValue;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
                return true;
            }

            return false;
        }
    }
}

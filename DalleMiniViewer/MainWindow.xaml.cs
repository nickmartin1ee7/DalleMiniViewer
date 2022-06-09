using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using Microsoft.Win32;

namespace DalleMiniViewer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private MainWindowViewModel? _vm => DataContext as MainWindowViewModel;

        public MainWindow()
        {
            InitializeComponent();
        }

        public void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            _vm?.ResetPromptContent();
        }

        private void ListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var listBox = e.Source as ListBox;

            if (listBox is null || listBox.SelectedItem is null) return;

            Clipboard.SetImage(listBox.SelectedItem as BitmapSource);
        }
    }
}

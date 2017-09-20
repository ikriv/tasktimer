using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using IKriv.Threading.Tasks;

namespace WpfTimer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : INotifyPropertyChanged
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;
            CurrentTimeStr = "Initializing...";
            SampleText.Focus();
            Loaded += StartTimer;
        }

        public string CurrentTimeStr
        {
            get { return _currentTimeStr; }
            set { _currentTimeStr = value; OnPropertyChanged(); }
        }
        private string _currentTimeStr;

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private async void StartTimer(object sender, RoutedEventArgs args)
        {
            using (var timer = new TaskTimer(1000).Start())
            {
                foreach (var tick in timer)
                {
                    await tick;
                    CurrentTimeStr = DateTime.Now.ToString("MMM dd yyyy HH:mm:ss.fff");
                }
            }
        }
    }
}

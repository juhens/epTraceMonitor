using MahApps.Metro.Behaviors;
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

namespace epTraceMonitorGUI
{
    public partial class MainWindow : Window
    {
        public static MainWindow? mainWindow;
        public static SettingWindow settingWindow = new();

        public int mode = 0;
        public bool isPause = false;

        public MainWindow()
        {
            mainWindow = this;
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            List<Log> list = new();
            list.Add(new Log { trace = 1, fileName = "asdf.eps", funcName = "asdfs()", line = 1, code = "dsafsdafsdaf" });
            list.Add(new Log { trace = 214, fileName = "dfgj.eps", funcName = "hfghf()", line = 31, code = "dsafsdafsdaf" });
            list.Add(new Log { trace = 64, fileName = "vncvx.eps", funcName = "fsdf()", line = 51, code = "dsafsdafsdaf" });
            list.Add(new Log { trace = 455, fileName = "zngf.eps", funcName = "sdfsdf()", line = 71, code = "dsafsdafsdaf" });
            LogDataGrid.ItemsSource = list;
        }
        public class Log
        {
            public ulong trace { get; set; }
            public string? fileName { get; set; }
            public string? funcName { get; set; }
            public ulong line { get; set; }
            public string? code { get; set; }
        }

        private void PPButton_Click(object sender, RoutedEventArgs e)
        {
            isPause = !isPause;
            if (isPause)
            {
                this.PPButon.Content = "Resume";
            }
            else
            {
                this.PPButon.Content = "Pause";
            }
        }

        private void SettingButton_Click(object sender, RoutedEventArgs e)
        {
            settingWindow.Owner = this;
            settingWindow.Topmost = true;
            settingWindow.ShowDialog();
        }
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void LogDataGrid_MouseDown(object sender, MouseButtonEventArgs e)
        {
            MessageBox.Show("");
        }
    }
}

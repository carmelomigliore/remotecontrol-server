using System;
using System.Collections.Generic;
using System.DirectoryServices.AccountManagement;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Application = System.Windows.Application;

namespace Server
{
    /// <summary>
    /// Interaction logic for Start.xaml
    /// </summary>
    public partial class Start : Window
    {
        public MainWindow Window { get; set; }
        private System.Windows.Forms.NotifyIcon _trayIcon;
        //public static PrincipalContext pc = new PrincipalContext(ContextType.Machine, null);
        public Start()
        {
            InitializeComponent();
            _trayIcon = new System.Windows.Forms.NotifyIcon();
            _trayIcon.Icon = new System.Drawing.Icon("Resources/Icon.ico");
            _trayIcon.Visible = true;
            _trayIcon.DoubleClick += delegate(object sender, EventArgs args)
            {
                this.Show();
                this.WindowState = WindowState.Normal;
            };
            _trayIcon.Click += delegate(object sender, EventArgs args)
            {
                this.Hide();
            };
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (this.Port.Text.All(Char.IsDigit) && Window == null)
            {
                this.Hide();
                Window = new MainWindow(Int32.Parse(Port.Text));
                Window.Show();
                start.Content = "Stop";
                Port.IsReadOnly = true;
               // _trayIcon.ShowBalloonTip(500,"Remote control", "The server now accepts connections", ToolTipIcon.Info);
            }
            else if (Window != null)
            {
               Window.Stop();
                Window.Close();
               //_trayIcon.ShowBalloonTip(500, "Rremote control", "The server is now stopped", ToolTipIcon.Info);
                start.Content = "Start";
                Port.IsReadOnly = false;
                Window = null;
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (Window != null)
            {
                Window.Stop();
                _trayIcon.Visible = false;
                Application.Current.Shutdown();
            }
        }
    }
}

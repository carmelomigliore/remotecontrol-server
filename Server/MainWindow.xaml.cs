using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Server
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private MyServer s;

        public MainWindow(int port)
        {
            s = new MyServer(port);
            s.Window = this;
            InitializeComponent();
        }


        private void Window_Deactivated(object sender, EventArgs e)
        {
            this.Topmost = true;
            this.Activate();
        }

        public void Stop()
        {
            s.Stop();
            s = null;
            this.Close();
        }



    }


}

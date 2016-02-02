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
using System.Windows.Shapes;

namespace RockBox
{



    /// <summary>
    /// Interaction logic for Console.xaml
    /// </summary>
    public partial class ErrorConsole : Window
    {

        System.Windows.Forms.Timer timer1;
        public ErrorConsole()
        {
            InitializeComponent();
            this.txtConsole.DataContext = this;
            // sets up a timer which is needed for updating the trackbar.
            this.timer1 = new System.Windows.Forms.Timer();
            this.timer1.Enabled = true;
            this.timer1.Interval = 500;
            this.timer1.Tick += new System.EventHandler(this.timer1_Tick);

        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            this.txtConsole.Text = this.ConsoleText;
        }

        public string ConsoleText
        {
            get;
            set;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            this.Hide();
        }
    }

}

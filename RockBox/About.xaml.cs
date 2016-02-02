using System;
using System.Collections.Generic;
using System.IO;
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
using System.Reflection;
using System.Deployment.Application;

namespace RockBox
{
    /// <summary>
    /// Interaction logic for About.xaml
    /// </summary>
    public partial class About : Window
    {
        public About()
        {
            InitializeComponent();

            this.LoadCopyright();
            this.LoadChangelog();

            this.tbTitle.Text = "About - " + this.ApplicationName + " " + this.ApplicationVersion;
            this.txtAbout.Text = this.ChangelogText + "\n\n" + this.CopyrightText;

        }

        private void LoadChangelog()
        {
            FileInfo fi = new FileInfo("Changelog.txt");
            if (fi.Exists)
            {
                this.ChangelogText = File.ReadAllText(fi.FullName);
            }
        }

        private void LoadCopyright()
        {
            FileInfo fi = new FileInfo("Copyright.txt");
            if (fi.Exists)
            {
                this.CopyrightText = File.ReadAllText(fi.FullName);
            }
        }

        private string ApplicationName
        {
            get
            {
                return System.IO.Path.GetFileName(Assembly.GetEntryAssembly().GetName().Name);
            }
        }

        private string ApplicationVersion
        {
            get
            {
                string version = null;
                try
                {
                    //// get deployment version
                    version = ApplicationDeployment.CurrentDeployment.CurrentVersion.ToString();
                }
                catch (InvalidDeploymentException)
                {
                    //// you cannot read publish version when app isn't installed 
                    //// (e.g. during debug)
                    version = "not installed";
                }
                return version;
            }
        }

        private string CopyrightText
        {
            get;
            set;
        }

        private string ChangelogText
        {
            get;
            set;
        }

        private void btn_close_Click(object sender, RoutedEventArgs e)
        {
            this.Hide();
        }
    }
}

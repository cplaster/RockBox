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
    class CustomButton : Button
    {
        public CustomButton()
        {
            // auto-register any "click" will call our own custom "click" handler
            // which will change the status...  This could also be done to simplify
            // by only changing visibility, but shows how you could apply via other
            // custom properties too.
            Click += MyCustomClick;
        }

        protected void MyCustomClick(object sender, RoutedEventArgs e)
        {

            this.ShowStatus = HowToShowStatus.ShowImage1;

            if (this.IsActive == true)
            {
                this.IsActive = false;
            }
            else
            {
                this.IsActive = true;
            }
        }


        public static readonly DependencyProperty ShowStatusProperty =
              DependencyProperty.Register("ShowStatus", typeof(HowToShowStatus),
              typeof(CustomButton), new UIPropertyMetadata(HowToShowStatus.ShowNothing));

        public static readonly DependencyProperty IsActiveProperty =
               DependencyProperty.Register("IsActive", typeof(bool),
               typeof(CustomButton), new UIPropertyMetadata(false));

        public HowToShowStatus ShowStatus
        {
            get { return (HowToShowStatus)GetValue(ShowStatusProperty); }
            set { SetValue(ShowStatusProperty, value); }
        }

        public bool IsActive
        {
            get { return (bool)GetValue(IsActiveProperty); }
            set { SetValue(IsActiveProperty, value); }

        }
    }

    public enum HowToShowStatus
    {
        ShowNothing,
        ShowImage1,
        ShowImage2,
        ShowImage3
    }

}


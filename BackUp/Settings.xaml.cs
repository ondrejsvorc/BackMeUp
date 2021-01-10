using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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

namespace BackUp
{
    public partial class Settings : Window
    {

        public Settings()
        {
            InitializeComponent();
        }

        private void SettingsWindowMove(object sender, MouseButtonEventArgs e)
        {
            this.DragMove();
        }

        private void SettingsWindowClose(object sender, MouseButtonEventArgs e)
        {
            this.Close();
        }

        private void SaveSettings(object sender, RoutedEventArgs e)
        {
            const string path = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
            RegistryKey regKey;

            if ((bool)checkBoxStartUp.IsChecked)
            {
                regKey = Registry.CurrentUser.OpenSubKey(path, true);
                regKey.SetValue("BackMeUp", Assembly.GetExecutingAssembly().Location.ToString());
            }
            else
            {
                regKey = Registry.CurrentUser.OpenSubKey(path, true);
                regKey.DeleteValue("BackMeUp", false);
            }

            Properties.Settings.Default.checkBoxStartUp = (bool)checkBoxStartUp.IsChecked;
            Properties.Settings.Default.checkBoxMinimized = (bool)checkBoxMinimized.IsChecked;
            Properties.Settings.Default.checkBoxBackingUp = (bool)checkBoxBackingUp.IsChecked;

            this.Close();
        }

        private void SettingsWindowLoaded(object sender, RoutedEventArgs e)
        {
            checkBoxStartUp.IsChecked = Properties.Settings.Default.checkBoxStartUp;
            checkBoxMinimized.IsChecked = Properties.Settings.Default.checkBoxMinimized;
            checkBoxBackingUp.IsChecked = Properties.Settings.Default.checkBoxBackingUp;
        }
    }
}

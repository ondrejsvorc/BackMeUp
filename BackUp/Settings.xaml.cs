﻿using System;
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

namespace BackUp
{
    public partial class Settings : Window
    {
        // CONSTRUCTOR: REQUEST A LIST + MAKE A LIST OF XAML OBJECTS I'LL CREATE AND THEN HAVE A FOREACH TO LOOP THROUGH THEM --> X1 = Y1; X2 = Y2
        // MAKE AN INTERFACE: REQUIRE METHODS?

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
            Properties.Settings.Default.checkBoxStartUp = (bool)checkBoxStartUp.IsChecked;
            Properties.Settings.Default.checkBoxBackingUp = (bool)checkBoxBackingUp.IsChecked;

            this.Close();
        }

        private void SettingsWindowLoaded(object sender, RoutedEventArgs e)
        {
            checkBoxStartUp.IsChecked = Properties.Settings.Default.checkBoxStartUp;
            checkBoxBackingUp.IsChecked = Properties.Settings.Default.checkBoxBackingUp;
        }
    }
}

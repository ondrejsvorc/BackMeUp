using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Forms;
using TextBox = System.Windows.Controls.TextBox;
using Button = System.Windows.Controls.Button;
using MessageBox = System.Windows.MessageBox;
using Application = System.Windows.Application;
using System.Text.RegularExpressions;
using System.Windows.Threading;
using Notifications.Wpf;
using System.Drawing;
using System.Collections;

namespace BackUp
{
    public partial class MainWindow : Window
    {
        //TO-DO:

        /*
         * TOAST NOTIFICATION
         * MORE CONDITIONS
         * READONLY TXTBOXES - DONE
         * LABELS WITH CURRENT SETTINGS - KINDA DONE
         * SAVING SETTINGS
         * REMOVE THE BUTTONS DOWN THERE - DONE
         * OPTIMISE GRID(S) - DONE
         * AUTO-REFRESH INTERVAL (LB.CONTENT = INTERVAL CHANGED...)
         * RUN IN BACKGROUND
        */

        DispatcherTimer timerBackingUp = new DispatcherTimer();
        DispatcherTimer timerEverythingInOrder = new DispatcherTimer();

        FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog();

        NotifyIcon notifyIcon;

        NotificationManager notificationManager = new NotificationManager();
        NotificationContent notificationBackingUp;
        NotificationContent notificationBackingUpFailure;

        bool backingUp;
        int backUpNum = 0;

        public MainWindow()
        {
            InitializeComponent();
            SettingsLoad();

            timerEverythingInOrder.Interval = TimeSpan.FromMilliseconds(100);
            timerEverythingInOrder.Tick += IsEverythingInOrder;
            timerEverythingInOrder.Start();

            notificationBackingUp = new NotificationContent
            {
                Title = "Backing up...",
                Message = $"Backup number: {++backUpNum}",
                Type = NotificationType.Information
            };

            notificationBackingUpFailure = new NotificationContent
            {
                Title = "Backing up failed.",
                Message = "Please, check whether the source and destination path exist.",
                Type = NotificationType.Error
            };

            timerBackingUp.Tick += BackUp;

            notifyIcon = new NotifyIcon();
            notifyIcon.Icon = Properties.Resources.BackMeUp_Icon;
            notifyIcon.MouseDoubleClick += WindowShow;
        }

        private void IsEverythingInOrder(object sender, EventArgs e)
        {
            bool inOrder = true;

            if (!Directory.Exists(txtBoxSource.Text))
            {
                lbErrorMessage.Content = "Enter a valid SOURCE path.";
                inOrder = false;
            }
            else if (!Directory.Exists(txtBoxDestination.Text))
            {
                lbErrorMessage.Content = "Enter a valid DESTINATION path.";
                inOrder = false;
            }

            if (!Directory.Exists(txtBoxSource.Text) && !Directory.Exists(txtBoxDestination.Text))
            {
                lbErrorMessage.Content = "Enter a valid SOURCE and DESTINATION path.";
                inOrder = false;
            }

            if (txtBoxInterval.Text == string.Empty)
            {
                lbErrorMessage.Content = "You must enter backing up INTERVAL.";
                inOrder = false;
            }

            if (Directory.Exists(txtBoxSource.Text) && Directory.Exists(txtBoxDestination.Text) && txtBoxInterval.Text != string.Empty)
            {
                lbErrorMessage.Content = string.Empty;
            }

            toggleBtnBackUpState.IsEnabled = inOrder;
            toggleBtnBackUpState.IsChecked = inOrder;
        }

        private void WindowShow(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            this.Show();
            notifyIcon.Visible = false;
            WindowState = WindowState.Normal;
        }

        private void WindowMove(object sender, MouseButtonEventArgs e)
        {
            this.DragMove();
        }

        private void WindowMinimize(object sender, MouseButtonEventArgs e)
        {
            this.WindowState = WindowState.Minimized;

            this.Hide();
            notifyIcon.Visible = true;
        }

        private void WindowClose(object sender, MouseButtonEventArgs e)
        {
            SettingsSave();
            Application.Current.Shutdown();
        }

        private void FileSourcePath(object sender, RoutedEventArgs e)
        {
            folderBrowserDialog.ShowDialog();
            txtBoxSource.Text = folderBrowserDialog.SelectedPath;
        }

        private void FileDestinationPath(object sender, RoutedEventArgs e)
        {
            folderBrowserDialog.ShowDialog();
            txtBoxDestination.Text = folderBrowserDialog.SelectedPath;
        }

        private void BackUpState(object sender, RoutedEventArgs e)
        {
            if ((bool)toggleBtnBackUpState.IsChecked)
            {
                backingUp = true;
                timerBackingUp.Interval = TimeSpan.FromSeconds(Convert.ToDouble(txtBoxInterval.Text));
                timerBackingUp.Start();

                lbBackUpState.Content = "Backing up is now turned on";
                toggleBtnBackUpState.Content = "Backup off";
            }
            else
            {
                backingUp = false;
                timerBackingUp.Stop();

                lbBackUpState.Content = "Backing up is now turned off";
                toggleBtnBackUpState.Content = "Backup on";
            }
        }

        private void BackUp(object sender, EventArgs e)
        {
            string dirSource = txtBoxSource.Text;                                                           // Source path (what we are backing up)
            string dirName = new DirectoryInfo(dirSource).Name;                                             // Name of the folder we are backing up
            string dirDestination = txtBoxDestination.Text + $"\\{dirName}";                                // Destination path (where we are backing up)
            string dirBackUpName = dirDestination + $" - {DateTime.Now:dd/MM/yy HH-mm-ss}";                 // Not yet existing path to a new folder where backup is located

            if (backingUp)
            {
                if (Directory.Exists(dirSource))
                {
                    notificationManager.Show(notificationBackingUp);

                    txtBoxLastBackup.Text = dirName + $" - {DateTime.Now:dd/MM/yy HH-mm-ss}"; 

                    Directory.CreateDirectory(dirBackUpName);
                    Directory.SetCurrentDirectory(dirBackUpName);

                    string[] files = Directory.GetFiles(dirSource, "*.*", SearchOption.AllDirectories);

                    foreach (string file in files)
                    {
                        File.Copy(file, $"{Path.GetFileName(file)}", true);
                    }
                }
                else
                {
                    notificationManager.Show(notificationBackingUpFailure);
                }
            }
        }

        private void AcceptOnlyNumbers(object sender, TextCompositionEventArgs e)
        {
            e.Handled = Regex.IsMatch(e.Text, "[^0-9]+");
        }

        private void SettingsSave()
        {
            Properties.Settings.Default.source = txtBoxSource.Text;
            Properties.Settings.Default.destination = txtBoxDestination.Text;

            if (txtBoxInterval.Text != string.Empty)
            {
                Properties.Settings.Default.interval = Convert.ToDouble(txtBoxInterval.Text);
            }
            else
            {
                Properties.Settings.Default.interval = 0;
            }

            Properties.Settings.Default.lastBackup = txtBoxLastBackup.Text;
            Properties.Settings.Default.backUpNum = backUpNum;

            Properties.Settings.Default.Save();
        }

        private void SettingsLoad()
        {
            txtBoxSource.Text = Properties.Settings.Default.source;
            txtBoxDestination.Text = Properties.Settings.Default.destination;
            txtBoxInterval.Text = Properties.Settings.Default.interval.ToString();
            txtBoxLastBackup.Text = Properties.Settings.Default.lastBackup;
            backUpNum = Properties.Settings.Default.backUpNum;
        }

        private void SettingsWindowOpen(object sender, RoutedEventArgs e)
        {
            Settings settings = new Settings();
            settings.Show();
        }

        private void IntervalUpdate(object sender, TextChangedEventArgs e)
        {
            if ((bool)toggleBtnBackUpState.IsChecked)
            {
                lbIntervalCurrent.Content = "Updating interval...";
            }

            if (txtBoxInterval.Text != string.Empty)
            {
                timerBackingUp.Interval = TimeSpan.FromSeconds(Convert.ToDouble(txtBoxInterval.Text));
                lbIntervalCurrent.Content = $"Interval is now set to {txtBoxInterval.Text} seconds.";
            }
        }

    }
}

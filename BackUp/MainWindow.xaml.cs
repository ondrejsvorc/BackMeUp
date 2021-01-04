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

        DispatcherTimer timer = new DispatcherTimer();
        FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog();

        NotifyIcon notifyIcon;

        NotificationManager notificationManager = new NotificationManager();
        NotificationContent notificationBackingUp;
        NotificationContent notificationBackingUpFailure;

        bool backingUp;
        int backUpNumber = 0;

        public MainWindow()
        {
            InitializeComponent();
            LoadSettings();

            notificationBackingUp = new NotificationContent
            {
                Title = "Backing up...",
                Message = $"Backup number: {++backUpNumber}",
                Type = NotificationType.Information
            };

            notificationBackingUpFailure = new NotificationContent
            {
                Title = "Backing up failed.",
                Message = "Please, check whether the source and destination path exist.",
                Type = NotificationType.Error
            };

            timer.Tick += BackUp;

            notifyIcon = new NotifyIcon();
            notifyIcon.Icon = Properties.Resources.BackMeUp_Icon;
            notifyIcon.MouseDoubleClick += WindowShow;
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
            SaveSettings();
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
                timer.Interval = TimeSpan.FromSeconds(Convert.ToDouble(txtBoxInterval.Text));
                timer.Start();

                lbBackUpState.Content = "Backing up is now turned on";
                lbIntervalCurrent.Content = $"Interval is now set to {txtBoxInterval.Text} seconds";
                toggleBtnBackUpState.Content = "Backup off";
            }
            else
            {
                backingUp = false;
                timer.Stop();

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

        private void IntervalRefresh(object sender, RoutedEventArgs e)
        {
            timer.Interval = TimeSpan.FromSeconds(Convert.ToDouble(txtBoxInterval.Text));
            lbIntervalCurrent.Content = $"Interval is now set to {txtBoxInterval.Text} seconds.";
        }

        private void SaveSettings()
        {
            Properties.Settings.Default.source = txtBoxSource.Text;
            Properties.Settings.Default.destination = txtBoxDestination.Text;
            Properties.Settings.Default.interval = Convert.ToDouble(txtBoxInterval.Text);
            Properties.Settings.Default.lastBackup = txtBoxLastBackup.Text;

            Properties.Settings.Default.Save();
        }

        private void LoadSettings()
        {
            txtBoxSource.Text = Properties.Settings.Default.source;
            txtBoxDestination.Text = Properties.Settings.Default.destination;
            txtBoxInterval.Text = Properties.Settings.Default.interval.ToString();
            txtBoxLastBackup.Text = Properties.Settings.Default.lastBackup;
        }
    }
}

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
using System.Windows.Controls.Primitives;

namespace BackUp
{
    public partial class MainWindow : Window
    {
        //TO-DO:

        /*
         * TOAST NOTIFICATION - DONE
         * MORE CONDITIONS - DONE
         * READONLY TXTBOXES - DONE
         * LABELS WITH CURRENT SETTINGS - KINDA DONE
         * SAVING SETTINGS - KINDA DONE
         * REMOVE THE BUTTONS DOWN THERE - DONE
         * OPTIMISE GRID(S) - DONE
         * AUTO-REFRESH INTERVAL (LB.CONTENT = INTERVAL CHANGED...) - DONE
         * RUN IN BACKGROUND - DONE
        */

        DispatcherTimer timerBackingUp = new DispatcherTimer();
        DispatcherTimer timerEverythingInOrder = new DispatcherTimer();

        FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog();

        NotifyIcon notifyIcon = new NotifyIcon();

        NotificationManager notificationManager = new NotificationManager();
        NotificationContent notificationBackingUp;
        NotificationContent notificationBackingUpFailure;

        bool backingUp;
        int backUpNum = 0;

        public MainWindow()
        {
            SetWindowState();
            InitializeComponent();
            SettingsLoad();

            lbIntervalCurrent.Content = $"Interval is now set to {maskedTxtBoxInterval.Text}";

            if (Properties.Settings.Default.checkBoxBackingUp)
            {
                StartBackingUp();
            }

            timerEverythingInOrder.Interval = TimeSpan.FromMilliseconds(100);
            timerEverythingInOrder.Tick += ArePathsOrIntervalValid;
            timerEverythingInOrder.Start();

            // FIX BACKUP NUMBER
            notificationBackingUp = new NotificationContent
            {
                Title = "Backing up...",
                Message = $"Backup number: {++backUpNum}",
                Type = NotificationType.Information
            };

            notificationBackingUpFailure = new NotificationContent
            {
                Title = "Backing up failed.",
                Message = "In order to fix it, open the app and see what's wrong.",
                Type = NotificationType.Error
            };

            timerBackingUp.Tick += BackUp;

            notifyIcon.Icon = Properties.Resources.BackMeUp_Icon;
            notifyIcon.MouseDoubleClick += WindowShow;
        }

        private void SetWindowState()
        {
            if (Properties.Settings.Default.checkBoxMinimized)
            {
                this.WindowState = WindowState.Minimized;

                this.Hide();
                notifyIcon.Visible = true;
            }
        }

        private void ArePathsOrIntervalValid(object sender, EventArgs e)
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

            if (!maskedTxtBoxInterval.IsMaskCompleted || !CheckIntervalValidity())
            {
                lbErrorMessage.Content = "You must enter a valid INTERVAL.";
                inOrder = false;
            }

            if (Directory.Exists(txtBoxSource.Text) && Directory.Exists(txtBoxDestination.Text) && maskedTxtBoxInterval.IsMaskCompleted && CheckIntervalValidity())
            {
                lbErrorMessage.Content = string.Empty;
            }

            if (inOrder)
            {
                toggleBtnBackUpState.IsEnabled = true;
                lbIntervalCurrent.Content = $"Interval is now set to {maskedTxtBoxInterval.Text}";
            }
            else
            {
                toggleBtnBackUpState.IsEnabled = false;
                toggleBtnBackUpState.IsChecked = false;
            }
        }

        // 01234567
        // 00:00:00
        private bool CheckIntervalValidity()
        {
            bool result = true;

            char[] secsMins = new char[]
            {
                maskedTxtBoxInterval.Text[3],
                maskedTxtBoxInterval.Text[4],

                maskedTxtBoxInterval.Text[6],
                maskedTxtBoxInterval.Text[7]
            };

            for (int i = 0; i < secsMins.Length - 1; i += 2)
            {
                string charSum = secsMins[i].ToString() + secsMins[i + 1].ToString();          // '6' + '7' = '67'
                int actualSum = Convert.ToInt32(charSum);                                      // '67' to int

                if (actualSum < 60)
                {
                    result = true;
                }
                else
                {
                    result = false;
                    break;
                }
            }

            return result;
        }

        private void WindowShow(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            this.Show();
            notifyIcon.Visible = false;
            this.WindowState = WindowState.Normal;
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
                StartBackingUp();
            }
            else
            {
                StopBackingUp();
            }
        }

        private void BackUp(object sender, EventArgs e)
        {
            string dirSource = txtBoxSource.Text;                                 // Source path (what we are backing up)
            string dirName = new DirectoryInfo(dirSource).Name;                   // Name of the folder we are backing up
            string dirDestination = txtBoxDestination.Text + $"\\{dirName}";      // Destination path (where we are backing up to)
            string lastDateTime = $" - {DateTime.Now:dd/MM/yy HH-mm-ss}";           
            string dirBackUpPath = dirDestination + lastDateTime;                 // Not yet existing path to a new folder where backup is located

            if (backingUp)
            {
                if (Directory.Exists(dirSource))
                {
                    notificationManager.Show(notificationBackingUp);

                    txtBoxLastBackup.Text = dirName + lastDateTime; 

                    Directory.CreateDirectory(dirBackUpPath);
                    Directory.SetCurrentDirectory(dirBackUpPath);

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

        private void StartBackingUp()
        {
            toggleBtnBackUpState.IsChecked = true;

            backingUp = true;
            timerBackingUp.Interval = TimeSpan.Parse(maskedTxtBoxInterval.Text);
            timerBackingUp.Start();

            lbBackUpState.Content = "Backing up is now turned on";
            toggleBtnBackUpState.Content = "Backup off";
        }

        private void StopBackingUp()
        {
            toggleBtnBackUpState.IsChecked = false;

            backingUp = false;
            timerBackingUp.Stop();

            lbBackUpState.Content = "Backing up is now turned off";
            toggleBtnBackUpState.Content = "Backup on";
        }

        private void SettingsSave()
        {
            Properties.Settings.Default.source = txtBoxSource.Text;
            Properties.Settings.Default.destination = txtBoxDestination.Text;

            if (maskedTxtBoxInterval.Text != string.Empty)
            {
                Properties.Settings.Default.interval = maskedTxtBoxInterval.Text;
            }
            else
            {
                Properties.Settings.Default.interval = "00:00:00";
            }

            Properties.Settings.Default.lastBackupName = txtBoxLastBackup.Text;
            Properties.Settings.Default.backUpNum = backUpNum;

            Properties.Settings.Default.Save();
        }

        private void SettingsLoad()
        {
            txtBoxSource.Text = Properties.Settings.Default.source;
            txtBoxDestination.Text = Properties.Settings.Default.destination;
            maskedTxtBoxInterval.Text = Properties.Settings.Default.interval.ToString();
            txtBoxLastBackup.Text = Properties.Settings.Default.lastBackupName;
            backUpNum = Properties.Settings.Default.backUpNum;

            foreach (Window window in Application.Current.Windows)
            {
                if (window.GetType() == typeof(Settings))
                {
                    var windowSettings = ((Settings)window);

                    windowSettings.checkBoxBackingUp.IsChecked = Properties.Settings.Default.checkBoxBackingUp;
                    windowSettings.checkBoxMinimized.IsChecked = Properties.Settings.Default.checkBoxMinimized;
                    windowSettings.checkBoxStartUp.IsChecked = Properties.Settings.Default.checkBoxStartUp;
                    break;
                }
            }
        }

        private void SettingsWindowOpen(object sender, RoutedEventArgs e)
        {
            Settings settings = new Settings();
            settings.Show();
        }
    }
}

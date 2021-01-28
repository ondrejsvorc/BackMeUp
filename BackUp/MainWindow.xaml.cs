using System;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Forms;
using System.Windows.Threading;
using Application = System.Windows.Application;
using Notifications.Wpf;
using MessageBox = System.Windows.MessageBox;

namespace BackUp
{
    public partial class MainWindow : Window
    {
        private DispatcherTimer timerBackingUp;
        private DispatcherTimer timerEverythingInOrder;

        private FolderBrowserDialog folderBrowserDialog;

        private NotifyIcon notifyIcon;

        private NotificationManager notificationManager;
        private NotificationContent notificationBackingUp;
        private NotificationContent notificationBackingUpFailure;

        private bool backingUp;
        private int backUpNum;

        public MainWindow()
        {
            notifyIcon = new NotifyIcon();

            SetWindowState();
            InitializeComponent();
            LocalSettingsLoad();

            backUpNum = 0;

            timerBackingUp = new DispatcherTimer();
            timerEverythingInOrder = new DispatcherTimer();

            timerEverythingInOrder.Interval = TimeSpan.FromMilliseconds(50);
            timerEverythingInOrder.Tick += ArePathsOrIntervalValid;
            timerEverythingInOrder.Start();

            folderBrowserDialog = new FolderBrowserDialog();

            notificationManager = new NotificationManager();

            lbIntervalCurrent.Content = $"Interval is now set to {maskedTxtBoxInterval.Text}";

            if (Properties.Settings.Default.checkBoxBackingUp && Directory.Exists(txtBoxSource.Text) && Directory.Exists(txtBoxDestination.Text) && maskedTxtBoxInterval.IsMaskFull && CheckIntervalValidity())
            {
                StartBackingUp();
            }

            notificationBackingUp = new NotificationContent
            {
                Title = "Backing up...",
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
        public bool CheckIntervalValidity()
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
            LocalSettingsSave();
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
                    notificationBackingUp.Message = $"Backup number: {++backUpNum}";
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

        private void LocalSettingsSave()
        {
            Properties.Settings.Default.source = txtBoxSource.Text;
            Properties.Settings.Default.destination = txtBoxDestination.Text;

            if (!string.IsNullOrWhiteSpace(maskedTxtBoxInterval.Text))
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

        private void LocalSettingsLoad()
        {
            txtBoxSource.Text = Properties.Settings.Default.source;
            txtBoxDestination.Text = Properties.Settings.Default.destination;
            maskedTxtBoxInterval.Text = Properties.Settings.Default.interval.ToString();
            txtBoxLastBackup.Text = Properties.Settings.Default.lastBackupName;
            backUpNum = Properties.Settings.Default.backUpNum;
        }

        private void SettingsWindowOpen(object sender, RoutedEventArgs e)
        {
            Settings settings = new Settings();
            settings.Show();
        }
    }
}

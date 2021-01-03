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
using System.Text.RegularExpressions;
using System.Windows.Threading;

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

        object prefixTo;
        bool backingUp;

        public MainWindow()
        {
            InitializeComponent();

            prefixTo = txtBoxSource;
            timer.Tick += BackUp;
        }

        private void WindowMove(object sender, MouseButtonEventArgs e)
        {
            this.DragMove();
        }

        private void WindowMinimize(object sender, MouseButtonEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void WindowClose(object sender, MouseButtonEventArgs e)
        {
            this.Close();
        }

        private void AddPrefix(object sender, RoutedEventArgs e)
        {
            ((TextBox)prefixTo).Text += ((Button)sender).Content.ToString();
        }

        private void GetTxtBox(object sender, RoutedEventArgs e)
        {
            prefixTo = (TextBox)sender;
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
                    txtBoxDirName.Text = dirName + $" - {DateTime.Now:dd/MM/yy HH-mm-ss}"; 

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
                    MessageBox.Show("Folder you want to backup doesn't exist.");
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
    }
}

using Ookii.Dialogs.Wpf;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Input;
using ViewModel;

namespace WPF
{
    public partial class MainWindow : Window, IUIShaha
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = new ViewModelClass(this);
        }

        public List<string> ExtractFilenames(string folderName, string format = "")
        {
            var extractedFiles = new List<string>();
            try
            {
                foreach (var file in Directory.EnumerateFiles(folderName))
                {
                    if (file.EndsWith(format))
                    {
                        extractedFiles.Add(file);
                    }
                }
            }
            catch (Exception exc)
            {
                ShowError(exc.Message);
            }
            return extractedFiles;
        }

        public string? GetPwd()
        {
            VistaFolderBrowserDialog dialog = new();
            if (dialog.ShowDialog() == true)
            {
                return dialog.SelectedPath;
            }
            return null;
        }

        public void ShowError(string message)
        {
            MessageBox.Show(message);
        }
        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DragMove();
        }

        private void BtnMinimize_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;

        private void BtnMaximize_Click(object sender, RoutedEventArgs e)
        {
            if (WindowState == WindowState.Maximized)
            {
                WindowState = WindowState.Normal;
            }
            else
            {
                WindowState = WindowState.Maximized;
            }
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e) => Close();
    }
}

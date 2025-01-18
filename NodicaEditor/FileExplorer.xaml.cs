﻿using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace NodicaEditor
{
    public partial class FileExplorer : UserControl
    {
        public string RootPath { get; set; }
        private string _currentPath;

        public event Action<string> FileOpened; // Event to notify about opened files

        public FileExplorer()
        {
            InitializeComponent();
            // Ensure RootPath ends with a directory separator
            RootPath = "D:\\Parsa Stuff\\Visual Studio\\HordeRush\\HordeRush\\Res";
            if (!RootPath.EndsWith(Path.DirectorySeparatorChar.ToString()))
            {
                RootPath += Path.DirectorySeparatorChar;
            }
        }

        public void Populate(string path)
        {
            FileExplorerItemsControl.Items.Clear();
            _currentPath = path;

            try
            {
                // Add a "Back" button if not in the root directory
                if (_currentPath != RootPath)
                {
                    var backButton = CreateBackButton();
                    FileExplorerItemsControl.Items.Add(backButton);
                }

                // Add directories
                foreach (string dir in Directory.GetDirectories(path))
                {
                    string dirName = Path.GetFileName(dir);
                    var fileItem = CreateFileExplorerItem(dirName, true);
                    fileItem.Tag = dir;
                    fileItem.PreviewMouseLeftButtonDown += (sender, e) =>
                    {
                        if (e.ClickCount == 2) // Double-click to open
                        {
                            Populate(fileItem.Tag.ToString());
                        }
                    };

                    FileExplorerItemsControl.Items.Add(fileItem);
                }

                // Add files
                foreach (string file in Directory.GetFiles(path))
                {
                    string fileName = Path.GetFileName(file);
                    var fileItem = CreateFileExplorerItem(fileName, false);
                    fileItem.Tag = file;
                    fileItem.PreviewMouseLeftButtonDown += (sender, e) =>
                    {
                        if (e.ClickCount == 1) // Single-click to drag
                        {
                            string relativePath = GetRelativePath(fileItem.Tag.ToString());
                            DragDrop.DoDragDrop(fileItem, relativePath, DragDropEffects.Copy);
                            e.Handled = true;
                        }
                        else if (e.ClickCount == 2) // Double-click to open
                        {
                            FileOpened?.Invoke(fileItem.Tag.ToString());
                        }
                    };

                    FileExplorerItemsControl.Items.Add(fileItem);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error accessing path '{path}': {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private string GetRelativePath(string fullPath)
        {
            if (fullPath.StartsWith(RootPath, StringComparison.OrdinalIgnoreCase))
            {
                string relativePath = fullPath.Substring(RootPath.Length).TrimStart(Path.DirectorySeparatorChar);
                return $"Res{Path.DirectorySeparatorChar}{relativePath}";
            }
            else
            {
                return fullPath; // Handle this case as needed
            }
        }

        private Grid CreateFileExplorerItem(string name, bool isDirectory)
        {
            var grid = new Grid
            {
                Width = 64,
                Height = 64,
                Margin = new Thickness(5),
                AllowDrop = false // Set to false as we are the source
            };
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(48) });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            // Get the full path by combining the current path and the file name
            string fullPath = Path.Combine(_currentPath, name);

            var image = new Image
            {
                Source = isDirectory
                    ? new BitmapImage(new Uri("D:\\Parsa Stuff\\Visual Studio\\NodicaEditor\\NodicaEditor\\bin\\Debug\\net8.0-windows\\Res\\Icons\\Folder.png", UriKind.RelativeOrAbsolute))
                    : GetImageSourceForFile(fullPath), // Pass the full path to GetImageSourceForFile
                Width = 48,
                Height = 48,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            Grid.SetRow(image, 0);
            grid.Children.Add(image);

            var textBlock = new TextBlock
            {
                Text = name,
                TextAlignment = TextAlignment.Center,
                Foreground = Brushes.White,
                TextWrapping = TextWrapping.Wrap
            };
            Grid.SetRow(textBlock, 1);
            grid.Children.Add(textBlock);

            return grid;
        }


        private BitmapImage GetImageSourceForFile(string filePath)
        {
            // Check the file extension to determine if it's an image
            string[] imageExtensions = { ".jpg", ".jpeg", ".png", ".bmp", ".gif", ".tiff", ".webp" };
            string fileExtension = Path.GetExtension(filePath).ToLowerInvariant();

            if (Array.Exists(imageExtensions, ext => ext.Equals(fileExtension)))
            {
                try
                {
                    // Debug: Log the full path and check if the file exists
                    Debug.WriteLine($"Attempting to load image from path: {filePath}");

                    // Verify if the file exists
                    if (File.Exists(filePath))
                    {
                        Debug.WriteLine($"File exists: {filePath}");
                        return new BitmapImage(new Uri(filePath, UriKind.Absolute));
                    }
                    else
                    {
                        Debug.WriteLine($"File does not exist: {filePath}");
                        return new BitmapImage(new Uri("D:\\Parsa Stuff\\Visual Studio\\NodicaEditor\\NodicaEditor\\bin\\Debug\\net8.0-windows\\Res\\Icons\\File.png", UriKind.RelativeOrAbsolute)); // fallback icon
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error loading image: {ex.Message}");
                    // Return a fallback icon in case of error
                    return new BitmapImage(new Uri("D:\\Parsa Stuff\\Visual Studio\\NodicaEditor\\NodicaEditor\\bin\\Debug\\net8.0-windows\\Res\\Icons\\File.png", UriKind.RelativeOrAbsolute));
                }
            }

            // Return a default file icon for non-image files
            return new BitmapImage(new Uri("D:\\Parsa Stuff\\Visual Studio\\NodicaEditor\\NodicaEditor\\bin\\Debug\\net8.0-windows\\Res\\Icons\\File.png", UriKind.RelativeOrAbsolute));
        }



        private Button CreateBackButton()
        {
            var backButton = new Button
            {
                Content = CreateFileExplorerItem("..", true),
                Tag = "Back",
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Padding = new Thickness(5)
            };

            backButton.PreviewMouseLeftButtonDown += (sender, e) =>
            {
                if (e.ClickCount == 2)
                {
                    string parentDirectory = Directory.GetParent(_currentPath)?.FullName;
                    if (parentDirectory != null)
                    {
                        Populate(parentDirectory);
                    }
                }
            };

            return backButton;
        }
    }
}
﻿using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using IniParser;
using IniParser.Model;
using Microsoft.Win32;
using System.Numerics;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Nodica;
using Raylib_cs;
using System.Reflection;
using System.Windows.Media;
using Color = Raylib_cs.Color;

namespace NodicaEditor;

public partial class MainWindow : Window
{
    private SceneHierarchyManager _sceneHierarchyManager;
    private Inspector _propertyInspector;
    private static readonly FileIniDataParser _iniParser = new();
    private string _currentFilePath;

    public MainWindow()
    {
        InitializeComponent();
        _propertyInspector = new Inspector(InspectorPanel);
        _sceneHierarchyManager = new SceneHierarchyManager(SceneHierarchyTreeView, _propertyInspector);
        SceneHierarchyTreeView.SelectedItemChanged += SceneHierarchyTreeView_SelectedItemChanged;
        CommandBindings.Add(new CommandBinding(ApplicationCommands.Save, Save_Executed, Save_CanExecute));

        //_currentFilePath = @"D:\Parsa Stuff\Visual Studio\HordeRush\HordeRush\Res\Scenes\Menu\Menu.ini";
        _currentFilePath = @"D:\Parsa Stuff\Visual Studio\HordeRush\HordeRush\Res\Scenes\Gun.ini";
        if (File.Exists(_currentFilePath))
        {
            _sceneHierarchyManager.LoadScene(_currentFilePath);
        }
        else
        {
            MessageBox.Show($"The file '{_currentFilePath}' does not exist.", "File Not Found", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void OpenIniFile_Click(object sender, RoutedEventArgs e)
    {
        OpenFileDialog openFileDialog = new()
        {
            Filter = "INI files (*.ini)|*.ini|All files (*.*)|*.*",
            Title = "Open INI File"
        };

        if (openFileDialog.ShowDialog() == true)
        {
            _currentFilePath = openFileDialog.FileName;
            _sceneHierarchyManager.LoadScene(_currentFilePath);
        }
    }

    private void SceneHierarchyTreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
    {
        if (e.NewValue is TreeViewItem selectedItem && selectedItem.Tag is Node selectedNode)
        {
            _propertyInspector.DisplayNodeProperties(selectedNode);
        }
    }

    private void Save_CanExecute(object sender, CanExecuteRoutedEventArgs e)
    {
        e.CanExecute = _sceneHierarchyManager.CurrentNode != null;
    }

    private void Save_Executed(object sender, ExecutedRoutedEventArgs e)
    {
        Node selectedNode = _sceneHierarchyManager.CurrentNode;
        if (selectedNode != null && _currentFilePath != null)
        {
            Dictionary<string, object?> propertyValues = _propertyInspector.GetPropertyValues(selectedNode);

            StringBuilder sb = new StringBuilder();
            foreach (var kvp in propertyValues)
            {
                sb.AppendLine($"{kvp.Key}: {kvp.Value}");
            }
            MessageBox.Show(sb.ToString(), "Properties to be Saved");

            SaveNodePropertiesToIni(selectedNode, _currentFilePath, propertyValues);
            MessageBox.Show($"Properties for node '{selectedNode.Name}' saved successfully.", "Save Successful", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private static void SaveNodePropertiesToIni(Node node, string filePath, Dictionary<string, object?> propertyValues)
    {
        IniData iniData = _iniParser.ReadFile(filePath);

        string originalSectionName = node.Name;
        string newSectionName = propertyValues.ContainsKey("Name")
            ? propertyValues["Name"]?.ToString() ?? originalSectionName
            : originalSectionName;

        // Rename the section if necessary
        if (originalSectionName != newSectionName && iniData.Sections.ContainsSection(originalSectionName))
        {
            var section = iniData.Sections[originalSectionName];
            iniData.Sections.RemoveSection(originalSectionName);
            iniData.Sections.AddSection(newSectionName);

            foreach (var key in section)
            {
                iniData.Sections[newSectionName][key.KeyName] = key.Value;
            }
        }

        UpdateParentReferences(iniData, originalSectionName, newSectionName);

        foreach (var propertyValue in propertyValues)
        {
            string propertyName = RemovePrefixFromPropertyName(propertyValue.Key);

            // Skip invalid properties or "Name"
            if (string.IsNullOrWhiteSpace(propertyName) || propertyName == "Name")
                continue;

            // Check for nested properties
            string[] pathParts = propertyValue.Key.Split('/');
            if (pathParts.Length > 1)
            {
                // Handle nested properties
                object currentObject = node;
                PropertyInfo currentProperty = null;
                bool shouldSkip = false;

                for (int i = 0; i < pathParts.Length; i++)
                {
                    currentProperty = currentObject.GetType().GetProperty(pathParts[i]);
                    if (currentProperty == null)
                        continue;

                    // Skip the exact property if marked with SaveExclude
                    if (i == pathParts.Length - 1 && IsSaveExcluded(currentProperty))
                    {
                        shouldSkip = true;
                        break;
                    }

                    // Move to the next level
                    if (i < pathParts.Length - 1)
                    {
                        currentObject = currentProperty.GetValue(currentObject);
                        if (currentObject == null)
                            break;
                    }
                }

                if (shouldSkip || currentProperty == null)
                    continue;

                object? defaultValue = DefaultValueProvider.GetDefaultValue(currentProperty, node, propertyValue.Key);

                if (AreValuesEqual(propertyValue.Value, defaultValue))
                {
                    iniData.Sections[newSectionName].RemoveKey(propertyValue.Key);
                }
                else
                {
                    iniData.Sections[newSectionName][propertyValue.Key] = ConvertPropertyValueToString(propertyValue.Value);
                }
            }
            else
            {
                // Handle non-nested properties
                PropertyInfo propertyInfo = node.GetType().GetProperty(propertyName);
                if (propertyInfo == null || IsSaveExcluded(propertyInfo))
                    continue;

                object? defaultValue = DefaultValueProvider.GetDefaultValue(propertyInfo, node);

                if (AreValuesEqual(propertyValue.Value, defaultValue))
                {
                    iniData.Sections[newSectionName].RemoveKey(propertyName);
                }
                else
                {
                    iniData.Sections[newSectionName][propertyName] = ConvertPropertyValueToString(propertyValue.Value);
                }
            }
        }

        _iniParser.WriteFile(filePath, iniData);
    }

    private static bool IsSaveExcluded(PropertyInfo propertyInfo)
    {
        return propertyInfo.GetCustomAttribute<SaveExcludeAttribute>() != null;
    }



    private static void UpdateParentReferences(IniData iniData, string oldName, string newName)
    {
        foreach (var section in iniData.Sections)
        {
            if (section.Keys.ContainsKey("parent") && section.Keys["parent"] == oldName)
            {
                section.Keys["parent"] = newName;
            }
        }
    }

    private static string RemovePrefixFromPropertyName(string propertyName)
    {
        if (string.IsNullOrWhiteSpace(propertyName))
            throw new ArgumentException("Property name cannot be null or empty.");

        string[] parts = propertyName.Split('/');
        return parts.Length > 1 ? string.Join("/", parts.Skip(1)) : propertyName;
    }

    private static string ConvertPropertyValueToString(object? value)
    {
        return value switch
        {
            Vector2 vector => $"({vector.X},{vector.Y})",
            bool boolean => boolean ? "true" : "false",
            Color color => $"({color.R},{color.G},{color.B},{color.A})",
            _ => value?.ToString() ?? string.Empty
        };
    }

    private static bool AreValuesEqual(object? value1, object? value2)
    {
        if (value1 is null && value2 is null) return true;
        if (value1 is null || value2 is null) return false;

        if (value1 is Vector2 v1 && value2 is Vector2 v2)
        {
            return v1.Equals(v2);
        }

        if (value1 is Color c1 && value2 is Color c2)
        {
            return c1.R == c2.R && c1.G == c2.G && c1.B == c2.B && c1.A == c2.A;
        }

        return value1.Equals(value2);
    }

    private void TextBlock_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2)
        {
            var textBlock = sender as TextBlock;
            var stackPanel = textBlock.Parent as StackPanel;
            var textBox = stackPanel.Children.OfType<TextBox>().FirstOrDefault();

            VisualStateManager.GoToElementState(stackPanel, "Edit", true);
            textBox.Focus();
            textBox.SelectAll();

            e.Handled = true;
        }
    }

    private void TextBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            var textBox = sender as TextBox;
            var stackPanel = textBox.Parent as StackPanel;
            var treeViewItem = FindAncestor<TreeViewItem>(stackPanel);

            if (treeViewItem != null && treeViewItem.Tag is Node node)
            {
                _sceneHierarchyManager.RenameNode(node, textBox.Text);
            }

            VisualStateManager.GoToElementState(stackPanel, "Normal", true);
            e.Handled = true;
        }
        else if (e.Key == Key.Escape)
        {
            var textBox = sender as TextBox;
            var stackPanel = textBox.Parent as StackPanel;
            VisualStateManager.GoToElementState(stackPanel, "Normal", true);
            e.Handled = true;
        }
    }

    private void TextBox_LostFocus(object sender, RoutedEventArgs e)
    {
        var textBox = sender as TextBox;
        var stackPanel = textBox.Parent as StackPanel;
        var treeViewItem = FindAncestor<TreeViewItem>(stackPanel);

        if (treeViewItem != null && treeViewItem.Tag is Node node)
        {
            _sceneHierarchyManager.RenameNode(node, textBox.Text);
        }

        VisualStateManager.GoToElementState(stackPanel, "Normal", true);
    }

    private static T FindAncestor<T>(DependencyObject current) where T : DependencyObject
    {
        do
        {
            if (current is T ancestor)
            {
                return ancestor;
            }
            current = VisualTreeHelper.GetParent(current);
        }
        while (current != null);

        return null;
    }
}
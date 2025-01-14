﻿using System.Reflection;
using System.Windows.Controls;
using System.Windows.Media;
using Nodica;
using Color = Raylib_cs.Color;
using VerticalAlignment = System.Windows.VerticalAlignment;

namespace NodicaEditor;

public class ColorControlGenerator
{
    private static readonly SolidColorBrush BackgroundBrush = new(new System.Windows.Media.Color { R = 16, G = 16, B = 16, A = 255 });
    private static readonly SolidColorBrush ForegroundBrush = new(Colors.LightGray);
    private static readonly SolidColorBrush RedBrush = new(Colors.Red);
    private static readonly SolidColorBrush GreenBrush = new(Colors.Green);
    private static readonly SolidColorBrush BlueBrush = new(Colors.Blue);

    private record ColorComponent(string Name, SolidColorBrush Brush);

    public static StackPanel CreateColorControl(Node node, PropertyInfo property, string fullPath, Dictionary<string, object?> nodePropertyValues)
    {
        string propertyName = string.IsNullOrEmpty(fullPath) ? property.Name : fullPath;
        Color initialColor = GetColorValue(nodePropertyValues, propertyName);

        return new()
        {
            Orientation = Orientation.Horizontal,
            Margin = new(2),
            Children =
            {
                CreateColorComponentControl(node, property, propertyName, new("R", RedBrush), nodePropertyValues),
                CreateColorComponentControl(node, property, propertyName, new("G", GreenBrush), nodePropertyValues),
                CreateColorComponentControl(node, property, propertyName, new("B", BlueBrush), nodePropertyValues),
                CreateColorComponentControl(node, property, propertyName, new("A", ForegroundBrush), nodePropertyValues)
            }
        };
    }

    private static StackPanel CreateColorComponentControl(Node node, PropertyInfo property, string propertyName, ColorComponent component, Dictionary<string, object?> nodePropertyValues)
    {
        Color color = GetColorValue(nodePropertyValues, propertyName);
        byte componentValue = GetComponentValue(color, component.Name);
        ColorComponentTag tag = new(node, property, propertyName, component.Name);

        TextBlock textBlock = new()
        {
            Text = $"{component.Name}:",
            VerticalAlignment = VerticalAlignment.Center,
            Foreground = component.Brush
        };

        TextBox textBox = CreateColorComponentTextBox(componentValue, tag, nodePropertyValues);

        return new()
        {
            Orientation = Orientation.Horizontal,
            Children =
            {
                textBlock,
                textBox,
            }
        };
    }

    private static byte GetComponentValue(Color color, string componentName)
    {
        return componentName switch
        {
            "R" => color.R,
            "G" => color.G,
            "B" => color.B,
            "A" => color.A,
            _ => throw new ArgumentException($"Invalid component name: {componentName}")
        };
    }

    private static Color GetColorValue(Dictionary<string, object?> nodePropertyValues, string propertyName)
    {
        return nodePropertyValues.TryGetValue(propertyName, out object? colorValue) && colorValue is Color color ?
            color :
            new(0, 0, 0, 0);
    }

    private static TextBox CreateColorComponentTextBox(byte initialValue, ColorComponentTag tag, Dictionary<string, object?> nodePropertyValues)
    {
        TextBox textBox = new()
        {
            Text = initialValue.ToString(),
            Width = 30,
            Height = 22,
            Tag = tag,
            Background = BackgroundBrush,
            Foreground = ForegroundBrush,
            BorderBrush = BackgroundBrush,
            Margin = new(5, 0, tag.ComponentName == "A" ? 0 : 5, 0),
            Style = null
        };

        textBox.TextChanged += (sender, _) => HandleTextChanged(sender, nodePropertyValues);

        return textBox;
    }

    private static void HandleTextChanged(object? sender, Dictionary<string, object?> nodePropertyValues)
    {
        if (sender is not TextBox { Tag: ColorComponentTag tag } tb)
        {
            return;
        }

        byte componentValue = byte.Parse(tb.Text);
        Color currentColor = GetColorValue(nodePropertyValues, tag.PropertyName);
        Color updatedColor = UpdateComponentValue(currentColor, tag.ComponentName, componentValue);

        SetPropertyValue(nodePropertyValues, tag.PropertyName, updatedColor);
    }

    private static Color UpdateComponentValue(Color color, string componentName, byte componentValue)
    {
        return componentName switch
        {
            "R" => new(componentValue, color.G, color.B, color.A),
            "G" => new(color.R, componentValue, color.B, color.A),
            "B" => new(color.R, color.G, componentValue, color.A),
            "A" => new(color.R, color.G, color.B, componentValue),
            _ => throw new ArgumentException($"Invalid component name: {componentName}")
        };
    }

    private static void SetPropertyValue(Dictionary<string, object?> propertyValues, string propertyName, object? newValue)
    {
        propertyValues[propertyName] = newValue;
    }

    private record ColorComponentTag(Node Node, PropertyInfo Property, string PropertyName, string ComponentName);
}
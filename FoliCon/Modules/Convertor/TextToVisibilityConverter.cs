﻿namespace FoliCon.Modules.Convertor;

using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

public class TextToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string text)
        {
            return text == "Video not available!" ? Visibility.Collapsed : Visibility.Visible;
        }

        return Visibility.Visible;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
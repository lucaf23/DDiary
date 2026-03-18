using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace DDiary.Converters
{
    /// <summary>Converte un bool in Visibility (true = Visible, false = Collapsed).</summary>
    public class BoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => value is bool b && b ? Visibility.Visible : Visibility.Collapsed;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => value is Visibility v && v == Visibility.Visible;
    }

    /// <summary>Converte un bool negato in Visibility.</summary>
    public class InverseBoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => value is bool b && !b ? Visibility.Visible : Visibility.Collapsed;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => value is Visibility v && v != Visibility.Visible;
    }

    /// <summary>Converte una stringa hex in SolidColorBrush.</summary>
    public class HexColorToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                if (value is string hex)
                {
                    var color = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(hex);
                    return new SolidColorBrush(color);
                }
            }
            catch { }
            return new SolidColorBrush(Colors.DodgerBlue);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    /// <summary>Converte null in Visibility (null = Collapsed, non-null = Visible).</summary>
    public class NullToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => value != null ? Visibility.Visible : Visibility.Collapsed;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    /// <summary>Converte un double >= 0 in stringa formattata.</summary>
    public class DoubleToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double d)
                return d == 0 ? string.Empty : d.ToString("F1", CultureInfo.CurrentCulture);
            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (double.TryParse(value?.ToString(), NumberStyles.Any, CultureInfo.CurrentCulture, out var result))
                return result;
            return 0.0;
        }
    }

    /// <summary>Converte il nome del MealType in label italiana.</summary>
    public class MealTypeToLabelConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Models.MealType mt)
            {
                return mt switch
                {
                    Models.MealType.Colazione => "Colazione",
                    Models.MealType.MerendaMattina => "Merenda mattina",
                    Models.MealType.Pranzo => "Pranzo",
                    Models.MealType.MerendaPomeriggio => "Merenda pomeriggio",
                    Models.MealType.Cena => "Cena",
                    Models.MealType.DopoCena => "Dopo cena",
                    _ => mt.ToString()
                };
            }
            return value?.ToString() ?? string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    /// <summary>Converte bool in larghezza per sidebar animata.</summary>
    public class BoolToWidthConverter : IValueConverter
    {
        public double OpenWidth { get; set; } = 280;
        public double ClosedWidth { get; set; } = 0;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => value is bool b && b ? OpenWidth : ClosedWidth;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    /// <summary>Converte bool negato.</summary>
    public class InverseBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => value is bool b && !b;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => value is bool b && !b;
    }

    /// <summary>Converte una stringa non vuota in Visibility.</summary>
    public class StringToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => !string.IsNullOrEmpty(value?.ToString()) ? Visibility.Visible : Visibility.Collapsed;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    /// <summary>Converte bool isToday in un colore accent o trasparente.</summary>
    public class TodayToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool b && b)
                return new SolidColorBrush(System.Windows.Media.Color.FromRgb(0, 120, 212));
            return System.Windows.Media.Brushes.Transparent;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}

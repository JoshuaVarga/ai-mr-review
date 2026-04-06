using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace TodoApp.UI.Converters;

public class StrikethroughConverter : IValueConverter
{
    public static readonly StrikethroughConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is true ? TextDecorationCollection.Parse("Strikethrough") : null;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}

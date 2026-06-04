using System.Globalization;

namespace BioscoopMAUI.Converters;

public class DiscountPercentageToLabelConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not decimal discountPercentage || discountPercentage <= 0)
            return string.Empty;

        return $"{discountPercentage.ToString("0.#", culture)}% off";
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
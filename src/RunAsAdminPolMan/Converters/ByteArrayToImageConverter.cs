using System;
using System.Globalization;
using System.IO;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace RunAsAdminPolMan.Converters;

/// <summary>
/// Converts a raw byte array into a WPF BitmapImage.
/// </summary>
public class ByteArrayToImageConverter : IValueConverter
{
    /// <summary>
    /// Converts a byte array to a BitmapImage.
    /// </summary>
    /// <param name="value">The byte array.</param>
    /// <param name="targetType">The target type.</param>
    /// <param name="parameter">The parameter.</param>
    /// <param name="culture">The culture info.</param>
    /// <returns>A BitmapImage or null.</returns>
    public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not byte[] rawBytes || rawBytes.Length == 0)
        {
            return null;
        }

        try
        {
            var image = new BitmapImage();
            using var stream = new MemoryStream(rawBytes);
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad; // Crucial for memory stream disposing
            image.StreamSource = stream;
            image.EndInit();
            image.Freeze(); // Crucial for cross-thread binding safety and performance
            return image;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Not supported.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <param name="targetType">The target type.</param>
    /// <param name="parameter">The parameter.</param>
    /// <param name="culture">The culture info.</param>
    /// <returns>Throws exception.</returns>
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}

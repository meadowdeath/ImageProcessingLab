using System.Drawing;
using System.Drawing.Imaging;

namespace ImageProcessingLab.Domain.Processing;

public sealed class LinearGrayscaleProcessor
{
    public Bitmap Process(Bitmap source)
    {
        ArgumentNullException.ThrowIfNull(source);

        var result = new Bitmap(source.Width, source.Height, PixelFormat.Format32bppArgb);

        for (var y = 0; y < source.Height; y++)
        {
            for (var x = 0; x < source.Width; x++)
            {
                var pixel = source.GetPixel(x, y);
                var luminance = (0.2126 * ToLinear(pixel.R)) +
                                (0.7152 * ToLinear(pixel.G)) +
                                (0.0722 * ToLinear(pixel.B));
                var gray = ToSrgb(luminance);
                result.SetPixel(x, y, Color.FromArgb(pixel.A, gray, gray, gray));
            }
        }

        return result;
    }

    private static double ToLinear(byte channel)
    {
        var value = channel / 255.0;
        return value <= 0.04045 ? value / 12.92 : Math.Pow((value + 0.055) / 1.055, 2.4);
    }

    private static byte ToSrgb(double linearValue)
    {
        var value = linearValue <= 0.0031308
            ? linearValue * 12.92
            : (1.055 * Math.Pow(linearValue, 1.0 / 2.4)) - 0.055;

        return (byte)Math.Clamp((int)Math.Round(value * 255), 0, 255);
    }
}

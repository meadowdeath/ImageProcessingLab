using System.Drawing;
using System.Drawing.Imaging;

namespace ImageProcessingLab.Domain.Processing;

public sealed class SmoothingProcessor
{
    public Bitmap Process(Bitmap source)
    {
        ArgumentNullException.ThrowIfNull(source);

        var result = new Bitmap(
            source.Width,
            source.Height,
            PixelFormat.Format32bppArgb);

        try
        {
            for (var y = 0; y < source.Height; y++)
            {
                for (var x = 0; x < source.Width; x++)
                {
                    // Preserve border pixels unchanged.
                    if (x == 0 ||
                        y == 0 ||
                        x == source.Width - 1 ||
                        y == source.Height - 1)
                    {
                        result.SetPixel(x, y, source.GetPixel(x, y));
                        continue;
                    }

                    double sumAlpha = 0;
                    double sumR = 0;
                    double sumG = 0;
                    double sumB = 0;

                    for (var offsetY = -1; offsetY <= 1; offsetY++)
                    {
                        for (var offsetX = -1; offsetX <= 1; offsetX++)
                        {
                            var pixel = source.GetPixel(
                                x + offsetX,
                                y + offsetY);

                            var alpha = pixel.A / 255.0;

                            sumAlpha += alpha;

                            // Premultiplied RGB.
                            sumR += pixel.R * alpha;
                            sumG += pixel.G * alpha;
                            sumB += pixel.B * alpha;
                        }
                    }

                    const double pixelCount = 9.0;

                    var averageAlpha = sumAlpha / pixelCount;

                    var a = (int)Math.Round(averageAlpha * 255);

                    var r = 0;
                    var g = 0;
                    var b = 0;

                    if (averageAlpha > 0)
                    {
                        r = (int)Math.Round(
                            (sumR / pixelCount) / averageAlpha);

                        g = (int)Math.Round(
                            (sumG / pixelCount) / averageAlpha);

                        b = (int)Math.Round(
                            (sumB / pixelCount) / averageAlpha);
                    }

                    result.SetPixel(
                        x,
                        y,
                        Color.FromArgb(
                            Math.Clamp(a, 0, 255),
                            Math.Clamp(r, 0, 255),
                            Math.Clamp(g, 0, 255),
                            Math.Clamp(b, 0, 255)));
                }
            }

            return result;
        }
        catch
        {
            result.Dispose();
            throw;
        }
    }
}
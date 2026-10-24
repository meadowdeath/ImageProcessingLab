using System.Drawing;
using System.Drawing.Imaging;

namespace ImageProcessingLab.Domain.Processing;

public sealed class GaussianFilterProcessor
{
    public Bitmap Process(Bitmap source)
    {
        ArgumentNullException.ThrowIfNull(source);

        int[,] kernel =
        {
            { 1, 2, 1 },
            { 2, 4, 2 },
            { 1, 2, 1 }
        };
        const double kernelWeight = 16.0;

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
                    // Preserve border pixels unchanged, just like the Box filter.
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
                            var weight = kernel[offsetY + 1, offsetX + 1];
                            var alpha = pixel.A / 255.0;

                            sumAlpha += weight * alpha;

                            // Weight premultiplied RGB so invisible colors do not bleed into the result.
                            sumR += weight * pixel.R * alpha;
                            sumG += weight * pixel.G * alpha;
                            sumB += weight * pixel.B * alpha;
                        }
                    }

                    var averageAlpha = sumAlpha / kernelWeight;
                    var a = (int)Math.Round(averageAlpha * 255);
                    var r = 0;
                    var g = 0;
                    var b = 0;

                    if (averageAlpha > 0)
                    {
                        r = (int)Math.Round((sumR / kernelWeight) / averageAlpha);
                        g = (int)Math.Round((sumG / kernelWeight) / averageAlpha);
                        b = (int)Math.Round((sumB / kernelWeight) / averageAlpha);
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

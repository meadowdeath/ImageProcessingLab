using System.Drawing;
using System.Drawing.Imaging;

namespace ImageProcessingLab.Domain.Processing;

public sealed class LaplaceFilterProcessor
{
    public Bitmap Process(Bitmap source)
    {
        ArgumentNullException.ThrowIfNull(source);

        int[,] kernel =
        {
            { 0, -1, 0 },
            { -1, 4, -1 },
            { 0, -1, 0 }
        };

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
                    var pixel = source.GetPixel(x, y);
                    // Border pixels have zero intensity but retain their original alpha.
                    var intensity = 0;
                    if (x > 0 && y > 0 && x < source.Width - 1 && y < source.Height - 1)
                    {
                        var response = 0;
                        for (var offsetY = -1; offsetY <= 1; offsetY++)
                        {
                            for (var offsetX = -1; offsetX <= 1; offsetX++)
                            {
                                // ImageService supplies grayscale input, so red carries its intensity.
                                response += source.GetPixel(x + offsetX, y + offsetY).R *
                                            kernel[offsetY + 1, offsetX + 1];
                            }
                        }

                        intensity = Math.Clamp(Math.Abs(response), 0, 255);
                    }

                    result.SetPixel(x, y, Color.FromArgb(pixel.A, intensity, intensity, intensity));
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

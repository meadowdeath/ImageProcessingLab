using System.Drawing;
using System.Drawing.Imaging;

namespace ImageProcessingLab.Domain.Processing;

public sealed class SmoothingProcessor
{
    public Bitmap Process(Bitmap source)
    {
        ArgumentNullException.ThrowIfNull(source);

        var result = new Bitmap(source.Width, source.Height, PixelFormat.Format32bppArgb);
        try
        {
            for (var y = 0; y < source.Height; y++)
            {
                for (var x = 0; x < source.Width; x++)
                {
                    var pixel = source.GetPixel(x, y);
                    if (x == 0 || y == 0 || x == source.Width - 1 || y == source.Height - 1)
                    {
                        result.SetPixel(x, y, pixel);
                        continue;
                    }

                    var sum = 0;
                    for (var offsetY = -1; offsetY <= 1; offsetY++)
                    {
                        for (var offsetX = -1; offsetX <= 1; offsetX++)
                        {
                            // The source is grayscale, so any RGB channel carries its intensity.
                            sum += source.GetPixel(x + offsetX, y + offsetY).R;
                        }
                    }

                    var gray = (int)Math.Round(sum / 9.0);
                    result.SetPixel(x, y, Color.FromArgb(pixel.A, gray, gray, gray));
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

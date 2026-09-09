using System.Drawing;
using ImageProcessingLab.Domain.Models;

namespace ImageProcessingLab.Domain.Processing;

public sealed class HistogramProcessor
{
    public HistogramResult Process(Bitmap grayscaleImage)
    {
        ArgumentNullException.ThrowIfNull(grayscaleImage);

        var frequencies = new int[256];
        for (var y = 0; y < grayscaleImage.Height; y++)
        {
            for (var x = 0; x < grayscaleImage.Width; x++)
            {
                // In a grayscale bitmap red, green, and blue carry the same intensity.
                frequencies[grayscaleImage.GetPixel(x, y).R]++;
            }
        }

        return new HistogramResult(frequencies);
    }
}

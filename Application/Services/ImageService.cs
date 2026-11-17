using System.Drawing;
using ImageProcessingLab.Domain.Processing;

namespace ImageProcessingLab.Application.Services;

public sealed class ImageService
{
    private readonly LinearGrayscaleProcessor _grayscaleProcessor = new();
    private readonly BoxFilterProcessor _boxFilterProcessor = new();
    private readonly GaussianFilterProcessor _gaussianFilterProcessor = new();
    private readonly LaplaceFilterProcessor _laplaceFilterProcessor = new();
    private readonly CannyFilterProcessor _cannyFilterProcessor = new();

    public Bitmap LoadImage(string path)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException("The required image was not found.", path);
        }

        // Clone so the source file is not kept locked while the application is open.
        using var loadedImage = Image.FromFile(path);
        return new Bitmap(loadedImage);
    }

    public Bitmap ConvertToGrayscale(Bitmap source) => _grayscaleProcessor.Process(source);

    public Bitmap ApplyBoxFilter(Bitmap source) => _boxFilterProcessor.Process(source);

    public Bitmap ApplyGaussianFilter(Bitmap source) => _gaussianFilterProcessor.Process(source);

    public Bitmap ApplyLaplaceFilter(Bitmap source, bool isGrayscale = false)
    {
        if (isGrayscale)
        {
            return _laplaceFilterProcessor.Process(source);
        }

        // Only color input needs a temporary grayscale bitmap; the caller retains ownership of source.
        using var grayscaleSource = ConvertToGrayscale(source);
        return _laplaceFilterProcessor.Process(grayscaleSource);
    }

    // Thresholds are raw Sobel gradient magnitudes, not normalized 0-255 intensities.
    public Bitmap ApplyCannyFilter(Bitmap source, bool isGrayscale = false, double lowThreshold = 50.0, double highThreshold = 100.0)
    {
        ArgumentNullException.ThrowIfNull(source);

        using var grayscaleSource = isGrayscale ? null : ConvertToGrayscale(source);
        using var smoothedSource = ApplyGaussianFilter(grayscaleSource ?? source);
        return _cannyFilterProcessor.Process(smoothedSource, source, lowThreshold, highThreshold);
    }
}

using System.Drawing;
using ImageProcessingLab.Domain.Processing;

namespace ImageProcessingLab.Application.Services;

public sealed class ImageService
{
    private readonly LinearGrayscaleProcessor _grayscaleProcessor = new();
    private readonly BoxFilterProcessor _boxFilterProcessor = new();

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
}

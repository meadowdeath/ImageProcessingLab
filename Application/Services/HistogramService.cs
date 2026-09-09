using System.Drawing;
using ImageProcessingLab.Domain.Models;
using ImageProcessingLab.Domain.Processing;

namespace ImageProcessingLab.Application.Services;

public sealed class HistogramService
{
    private readonly HistogramProcessor _processor = new();

    public HistogramResult CreateHistogram(Bitmap grayscaleImage) => _processor.Process(grayscaleImage);
}

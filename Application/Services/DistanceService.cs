using ImageProcessingLab.Domain.Models;
using ImageProcessingLab.Domain.Processing;

namespace ImageProcessingLab.Application.Services;

public sealed class DistanceService
{
    private readonly DistanceCalculator _calculator = new();

    public double CalculateEuclidean(PixelPoint first, PixelPoint second) => _calculator.Euclidean(first, second);

    public int CalculateManhattan(PixelPoint first, PixelPoint second) => _calculator.Manhattan(first, second);

    public double CalculateMinkowski(PixelPoint first, PixelPoint second) => _calculator.Minkowski(first, second, p: 3);
}

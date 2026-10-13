using ImageProcessingLab.Domain.Models;

namespace ImageProcessingLab.Domain.Processing;

public sealed class DistanceCalculator
{
    public double Euclidean(PixelPoint first, PixelPoint second)
    {
        var (deltaX, deltaY) = GetDeltas(first, second);
        return Math.Sqrt((deltaX * deltaX) + (deltaY * deltaY));
    }

    public int Manhattan(PixelPoint first, PixelPoint second)
    {
        var (deltaX, deltaY) = GetDeltas(first, second);
        return deltaX + deltaY;
    }

    public double Minkowski(PixelPoint first, PixelPoint second, int p = 3)
    {
        if (p < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(p), "Minkowski p must be at least 1.");
        }

        var (deltaX, deltaY) = GetDeltas(first, second);
        return Math.Pow(Math.Pow(deltaX, p) + Math.Pow(deltaY, p), 1.0 / p);
    }

    public int Chessboard(PixelPoint first, PixelPoint second)
    {
        var (deltaX, deltaY) = GetDeltas(first, second);
        return Math.Max(deltaX, deltaY);
    }

    private static (int DeltaX, int DeltaY) GetDeltas(PixelPoint first, PixelPoint second) =>
        (Math.Abs(second.X - first.X), Math.Abs(second.Y - first.Y));
}

using System.Drawing;

namespace ImageProcessingLab.Domain.Models;

public readonly record struct PixelPoint(int X, int Y)
{
    public Point ToPoint() => new(X, Y);

    public override string ToString() => $"({X}, {Y})";
}

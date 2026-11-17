using System.Drawing;
using System.Drawing.Imaging;

namespace ImageProcessingLab.Domain.Processing;

public sealed class CannyFilterProcessor
{
    private enum EdgeType : byte
    {
        None,
        Weak,
        Strong
    }

    // Input is Gaussian-smoothed grayscale. Thresholds use raw Sobel magnitudes, not 8-bit intensities.
    // alphaSource retains the caller's original alpha, before Gaussian smoothing changes it.
    public Bitmap Process(Bitmap source, Bitmap alphaSource, double lowThreshold, double highThreshold)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(alphaSource);

        if (source.Size != alphaSource.Size)
        {
            throw new ArgumentException("The alpha source must have the same dimensions as the processed image.", nameof(alphaSource));
        }

        if (!double.IsFinite(lowThreshold) || lowThreshold <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(lowThreshold), "The low threshold must be finite and greater than zero.");
        }

        if (!double.IsFinite(highThreshold) || highThreshold <= lowThreshold)
        {
            throw new ArgumentOutOfRangeException(nameof(highThreshold), "The high threshold must be finite and greater than the low threshold.");
        }

        var alpha = ReadAlphaChannel(alphaSource);
        var (magnitudes, directions) = CalculateGradients(source, alpha);
        var suppressed = SuppressNonMaximum(magnitudes, directions, alpha);
        var edges = ApplyDoubleThreshold(suppressed, alpha, lowThreshold, highThreshold);
        TrackEdgesByHysteresis(edges, alpha);
        return CreateOutputBitmap(edges, alpha);
    }

    private static byte[,] ReadAlphaChannel(Bitmap source)
    {
        var alpha = new byte[source.Width, source.Height];
        for (var y = 0; y < source.Height; y++)
        {
            for (var x = 0; x < source.Width; x++)
            {
                alpha[x, y] = source.GetPixel(x, y).A;
            }
        }

        return alpha;
    }

    private static (double[,] Magnitudes, double[,] Directions) CalculateGradients(Bitmap source, byte[,] alpha)
    {
        int[,] horizontalKernel =
        {
            { -1, 0, 1 },
            { -2, 0, 2 },
            { -1, 0, 1 }
        };
        int[,] verticalKernel =
        {
            { -1, -2, -1 },
            { 0, 0, 0 },
            { 1, 2, 1 }
        };

        var magnitudes = new double[source.Width, source.Height];
        var directions = new double[source.Width, source.Height];
        for (var y = 1; y < source.Height - 1; y++)
        {
            for (var x = 1; x < source.Width - 1; x++)
            {
                if (alpha[x, y] == 0)
                {
                    continue;
                }

                var centerIntensity = source.GetPixel(x, y).R;
                double gradientX = 0;
                double gradientY = 0;
                for (var offsetY = -1; offsetY <= 1; offsetY++)
                {
                    for (var offsetX = -1; offsetX <= 1; offsetX++)
                    {
                        // Extend the center intensity into invisible neighbors, avoiding artificial opacity edges.
                        var intensity = alpha[x + offsetX, y + offsetY] == 0
                            ? centerIntensity
                            : source.GetPixel(x + offsetX, y + offsetY).R;
                        gradientX += intensity * horizontalKernel[offsetY + 1, offsetX + 1];
                        gradientY += intensity * verticalKernel[offsetY + 1, offsetX + 1];
                    }
                }

                // Keep the full Euclidean Sobel magnitude; Atan2 gives its direction in image coordinates (Y down).
                magnitudes[x, y] = Math.Sqrt((gradientX * gradientX) + (gradientY * gradientY));
                directions[x, y] = Math.Atan2(gradientY, gradientX);
            }
        }

        return (magnitudes, directions);
    }

    private static double[,] SuppressNonMaximum(double[,] magnitudes, double[,] directions, byte[,] alpha)
    {
        var width = magnitudes.GetLength(0);
        var height = magnitudes.GetLength(1);
        var suppressed = new double[width, height];
        for (var y = 1; y < height - 1; y++)
        {
            for (var x = 1; x < width - 1; x++)
            {
                if (alpha[x, y] == 0)
                {
                    continue;
                }

                var (offsetX, offsetY) = QuantizeDirection(directions[x, y]) switch
                {
                    0 => (1, 0),
                    45 => (1, 1),
                    90 => (0, 1),
                    _ => (1, -1)
                };
                var magnitude = magnitudes[x, y];
                var before = magnitudes[x - offsetX, y - offsetY];
                var after = magnitudes[x + offsetX, y + offsetY];
                // Keep only peaks across the edge. One strict comparison thins equal-magnitude plateaus.
                if (magnitude > before && magnitude >= after)
                {
                    suppressed[x, y] = magnitude;
                }
            }
        }

        return suppressed;
    }

    private static int QuantizeDirection(double direction)
    {
        // Opposite gradient directions describe the same edge normal, so fold angles into [0, 180).
        var degrees = direction * 180.0 / Math.PI;
        if (degrees < 0)
        {
            degrees += 180;
        }

        if (degrees < 22.5 || degrees >= 157.5)
        {
            return 0;
        }

        if (degrees < 67.5)
        {
            return 45;
        }

        return degrees < 112.5 ? 90 : 135;
    }

    private static EdgeType[,] ApplyDoubleThreshold(double[,] magnitudes, byte[,] alpha, double lowThreshold, double highThreshold)
    {
        var width = magnitudes.GetLength(0);
        var height = magnitudes.GetLength(1);
        var edges = new EdgeType[width, height];
        for (var y = 1; y < height - 1; y++)
        {
            for (var x = 1; x < width - 1; x++)
            {
                if (alpha[x, y] == 0)
                {
                    continue;
                }

                // Strong peaks seed tracking; weak peaks survive only if connected to a strong edge.
                var magnitude = magnitudes[x, y];
                if (magnitude >= highThreshold)
                {
                    edges[x, y] = EdgeType.Strong;
                }
                else if (magnitude >= lowThreshold)
                {
                    edges[x, y] = EdgeType.Weak;
                }
            }
        }

        return edges;
    }

    private static void TrackEdgesByHysteresis(EdgeType[,] edges, byte[,] alpha)
    {
        var width = edges.GetLength(0);
        var height = edges.GetLength(1);
        var queue = new Queue<Point>();
        for (var y = 1; y < height - 1; y++)
        {
            for (var x = 1; x < width - 1; x++)
            {
                if (alpha[x, y] > 0 && edges[x, y] == EdgeType.Strong)
                {
                    queue.Enqueue(new Point(x, y));
                }
            }
        }

        // BFS promotes entire eight-connected weak chains, marking each pixel before enqueueing it.
        while (queue.Count > 0)
        {
            var point = queue.Dequeue();
            for (var offsetY = -1; offsetY <= 1; offsetY++)
            {
                for (var offsetX = -1; offsetX <= 1; offsetX++)
                {
                    var x = point.X + offsetX;
                    var y = point.Y + offsetY;
                    if (x <= 0 || y <= 0 || x >= width - 1 || y >= height - 1 || alpha[x, y] == 0)
                    {
                        continue;
                    }

                    if (edges[x, y] == EdgeType.Weak)
                    {
                        edges[x, y] = EdgeType.Strong;
                        queue.Enqueue(new Point(x, y));
                    }
                }
            }
        }
    }

    private static Bitmap CreateOutputBitmap(EdgeType[,] edges, byte[,] alpha)
    {
        var width = edges.GetLength(0);
        var height = edges.GetLength(1);
        var result = new Bitmap(width, height, PixelFormat.Format32bppArgb);
        try
        {
            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    // Unconnected weak edges become black; original transparency is never blurred in the output.
                    var intensity = alpha[x, y] > 0 && edges[x, y] == EdgeType.Strong ? 255 : 0;
                    result.SetPixel(x, y, Color.FromArgb(alpha[x, y], intensity, intensity, intensity));
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

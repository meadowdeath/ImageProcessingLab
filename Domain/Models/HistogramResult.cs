namespace ImageProcessingLab.Domain.Models;

public sealed class HistogramResult
{
    public HistogramResult(int[] frequencies)
    {
        ArgumentNullException.ThrowIfNull(frequencies);

        if (frequencies.Length != 256)
        {
            throw new ArgumentException("A grayscale histogram must contain 256 intensity values.", nameof(frequencies));
        }

        Frequencies = frequencies;
    }

    public int[] Frequencies { get; }

    public int MaximumFrequency => Frequencies.Max();
}

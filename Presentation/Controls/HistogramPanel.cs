using System.Drawing.Drawing2D;
using ImageProcessingLab.Domain.Models;

namespace ImageProcessingLab.Presentation.Controls;

public sealed class HistogramPanel : Panel
{
    private HistogramResult? _histogram;

    public HistogramPanel()
    {
        DoubleBuffered = true;
        BackColor = Color.White;
        BorderStyle = BorderStyle.FixedSingle;
        MinimumSize = new Size(420, 340);
    }

    public void Display(HistogramResult histogram)
    {
        _histogram = histogram ?? throw new ArgumentNullException(nameof(histogram));
        Invalidate();
    }

    public void Clear()
    {
        _histogram = null;
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        e.Graphics.SmoothingMode = SmoothingMode.None;

        var bounds = ClientRectangle;
        const int top = 10;
        var labelFlags = TextFormatFlags.NoPadding;
        var zeroLabelSize = TextRenderer.MeasureText("0", Font, Size.Empty, labelFlags);
        var maxLabelSize = TextRenderer.MeasureText("255", Font, Size.Empty, labelFlags);
        // Reserve half of the widest endpoint label on both sides so labels remain centered on the axis ends.
        var horizontalPadding = Math.Max(zeroLabelSize.Width, maxLabelSize.Width) / 2 + 4;
        var bottomPadding = Math.Max(zeroLabelSize.Height, maxLabelSize.Height) + 6;
        var plot = Rectangle.FromLTRB(horizontalPadding, top, bounds.Right - horizontalPadding, bounds.Bottom - bottomPadding);

        if (plot.Width <= 0 || plot.Height <= 0)
        {
            return;
        }

        using var axisPen = new Pen(Color.DimGray);
        e.Graphics.DrawLine(axisPen, plot.Left, plot.Bottom, plot.Right, plot.Bottom);
        e.Graphics.DrawLine(axisPen, plot.Left, plot.Top, plot.Left, plot.Bottom);
        var labelTop = plot.Bottom + 4;
        TextRenderer.DrawText(e.Graphics, "0", Font,
            new Point(plot.Left - (zeroLabelSize.Width / 2), labelTop), Color.DimGray, labelFlags);
        TextRenderer.DrawText(e.Graphics, "255", Font,
            new Point(plot.Right - (maxLabelSize.Width / 2), labelTop), Color.DimGray, labelFlags);

        if (_histogram is null || _histogram.MaximumFrequency == 0)
        {
            TextRenderer.DrawText(e.Graphics, "Generate grayscale to view the histogram.", Font,
                plot, Color.DimGray, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            return;
        }

        using var barPen = new Pen(Color.SteelBlue);
        for (var intensity = 0; intensity < 256; intensity++)
        {
            var x = plot.Left + ((intensity / 255f) * plot.Width);
            // Scale every frequency against the tallest bin to fit the plot height.
            var height = (_histogram.Frequencies[intensity] / (float)_histogram.MaximumFrequency) * plot.Height;
            e.Graphics.DrawLine(barPen, x, plot.Bottom, x, plot.Bottom - height);
        }
    }
}

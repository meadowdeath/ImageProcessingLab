using System.Drawing;
using System.Globalization;
using ImageProcessingLab.Application.Services;
using ImageProcessingLab.Domain.Models;
using ImageProcessingLab.Presentation.Controls;

namespace ImageProcessingLab.Presentation;

public sealed class MainForm : Form
{
    private const string ImageFileName = "ayumu-kasuga.jpg";

    private readonly ImageService _imageService = new();
    private readonly HistogramService _histogramService = new();
    private readonly DistanceService _distanceService = new();

    private readonly PictureBox _imageBox = new();
    private readonly HistogramPanel _histogramPanel = new();
    private readonly Label _pointALabel = new();
    private readonly Label _pointBLabel = new();
    private readonly Label _euclideanLabel = new();
    private readonly Label _manhattanLabel = new();
    private readonly Label _minkowskiLabel = new();
    private readonly Label _statusLabel = new();

    private Bitmap? _originalImage;
    private Bitmap? _grayscaleImage;
    private PixelPoint? _pointA;
    private PixelPoint? _pointB;

    public MainForm()
    {
        Text = "Image Processing Lab";
        WindowState = FormWindowState.Maximized;
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;

        BuildLayout();
        Load += OnLoad;
        FormClosed += OnFormClosed;
    }

    private void BuildLayout()
    {
        var originalButton = new Button { Text = "Original", AutoSize = true };
        originalButton.Click += (_, _) => ShowOriginal();
        var grayscaleButton = new Button { Text = "Grayscale", AutoSize = true };
        grayscaleButton.Click += (_, _) => ShowGrayscale();
        var clearPointsButton = new Button { Text = "Clear Points", AutoSize = true };
        clearPointsButton.Click += (_, _) => ClearPoints();

        var actions = new FlowLayoutPanel
        {
            AutoSize = true,
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            Padding = new Padding(0, 0, 0, 4)
        };
        actions.Controls.AddRange([originalButton, grayscaleButton, clearPointsButton]);

        _imageBox.Dock = DockStyle.Fill;
        _imageBox.BackColor = Color.FromArgb(245, 245, 245);
        _imageBox.SizeMode = PictureBoxSizeMode.Zoom;
        _imageBox.Cursor = Cursors.Cross;
        _imageBox.MouseClick += OnImageMouseClick;
        _imageBox.Paint += OnImagePaint;

        var imageCaption = new Label
        {
            Text = "Click inside the displayed image to select Point A and Point B.",
            AutoSize = true,
            Dock = DockStyle.Fill,
            Padding = new Padding(0, 8, 0, 6)
        };

        var imageTitle = CreateSectionTitle("Image");
        var imageColumn = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 3, Margin = new Padding(0, 0, 8, 0) };
        imageColumn.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        imageColumn.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        imageColumn.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        imageColumn.Controls.Add(imageTitle, 0, 0);
        imageColumn.Controls.Add(_imageBox, 0, 1);
        imageColumn.Controls.Add(imageCaption, 0, 2);

        var histogramTitle = CreateSectionTitle("Grayscale Histogram (0–255)");
        var histogramColumn = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, Margin = new Padding(8, 0, 0, 0) };
        histogramColumn.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        histogramColumn.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        histogramColumn.Controls.Add(histogramTitle, 0, 0);
        histogramColumn.Controls.Add(_histogramPanel);
        histogramColumn.SetCellPosition(_histogramPanel, new TableLayoutPanelCellPosition(0, 1));
        _histogramPanel.Dock = DockStyle.Fill;

        var content = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, Padding = new Padding(0, 0, 0, 8) };
        content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40));
        content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60));
        content.Controls.Add(imageColumn, 0, 0);
        content.Controls.Add(histogramColumn, 1, 0);

        var pointDetails = new TableLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, ColumnCount = 2 };
        pointDetails.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        pointDetails.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        ConfigureMeasurementLabel(_pointALabel, "Point A: --");
        ConfigureMeasurementLabel(_pointBLabel, "Point B: --");
        var pointInstruction = new Label
        {
            Text = "Select two points on the image to calculate the distances.",
            AutoSize = true,
            Dock = DockStyle.Fill,
            ForeColor = Color.DimGray,
            Padding = new Padding(0, 0, 0, 4)
        };
        pointDetails.Controls.Add(pointInstruction, 0, 0);
        pointDetails.SetColumnSpan(pointInstruction, 2);
        pointDetails.Controls.Add(_pointALabel, 0, 1);
        pointDetails.Controls.Add(_pointBLabel, 1, 1);

        var distanceDetails = new TableLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, ColumnCount = 3 };
        distanceDetails.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));
        distanceDetails.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));
        distanceDetails.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.34f));
        ConfigureMeasurementLabel(_euclideanLabel, "Euclidean: --");
        ConfigureMeasurementLabel(_manhattanLabel, "Manhattan: --");
        ConfigureMeasurementLabel(_minkowskiLabel, "Minkowski (p = 3): --");
        distanceDetails.Controls.Add(_euclideanLabel, 0, 0);
        distanceDetails.Controls.Add(_manhattanLabel, 1, 0);
        distanceDetails.Controls.Add(_minkowskiLabel, 2, 0);

        _statusLabel.AutoSize = true;
        _statusLabel.Dock = DockStyle.Fill;
        _statusLabel.Font = new Font(Font.FontFamily, Math.Max(7f, Font.Size - 1));
        _statusLabel.ForeColor = Color.DimGray;
        _statusLabel.Padding = Padding.Empty;

        var pointSection = CreateSection("Point Selection", pointDetails);
        var distanceSection = CreateSection("Distance Measurements", distanceDetails);
        var controlsSection = CreateSection("Controls", actions);
        var statusSection = CreateSection("Status", _statusLabel);

        var root = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(12), RowCount = 5 };
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.Controls.Add(content, 0, 0);
        root.Controls.Add(pointSection, 0, 1);
        root.Controls.Add(distanceSection, 0, 2);
        root.Controls.Add(controlsSection, 0, 3);
        root.Controls.Add(statusSection, 0, 4);
        Controls.Add(root);
    }

    private Label CreateSectionTitle(string text) => new()
    {
        Text = text,
        AutoSize = true,
        Dock = DockStyle.Fill,
        Font = new Font(Font, FontStyle.Bold),
        Padding = new Padding(0, 0, 0, 6)
    };

    private TableLayoutPanel CreateSection(string title, Control content)
    {
        var section = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            RowCount = 2,
            Margin = new Padding(0, 8, 0, 0)
        };
        section.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        section.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        section.Controls.Add(CreateSectionTitle(title), 0, 0);
        section.Controls.Add(content, 0, 1);
        return section;
    }

    private static void ConfigureMeasurementLabel(Label label, string text)
    {
        label.Text = text;
        label.AutoSize = true;
        label.Padding = new Padding(0, 2, 16, 2);
    }

    private void OnLoad(object? sender, EventArgs e)
    {
        var imagePath = Path.Combine(AppContext.BaseDirectory, "Images", ImageFileName);
        try
        {
            _originalImage = _imageService.LoadImage(imagePath);
            _imageBox.Image = _originalImage;
            _statusLabel.Text = $"Loaded: Images{Path.DirectorySeparatorChar}{ImageFileName}";
        }
        catch (FileNotFoundException)
        {
            _statusLabel.Text = $"Image not found. Add {ImageFileName} to Images next to the executable, then run again.";
        }
        catch (Exception exception)
        {
            _statusLabel.Text = $"Unable to load {ImageFileName}: {exception.Message}";
        }
    }

    private void ShowOriginal()
    {
        if (_originalImage is null)
        {
            return;
        }

        _imageBox.Image = _originalImage;
        _histogramPanel.Clear();
        _statusLabel.Text = "Showing the original image.";
        _imageBox.Invalidate();
    }

    private void ShowGrayscale()
    {
        if (_originalImage is null)
        {
            return;
        }

        var newGrayscale = _imageService.ConvertToGrayscale(_originalImage);
        var oldGrayscale = _grayscaleImage;
        _grayscaleImage = newGrayscale;
        _imageBox.Image = _grayscaleImage;
        oldGrayscale?.Dispose();

        _histogramPanel.Display(_histogramService.CreateHistogram(_grayscaleImage));
        _statusLabel.Text = "Showing linear-light grayscale and its histogram.";
        _imageBox.Invalidate();
    }

    private void OnImageMouseClick(object? sender, MouseEventArgs e)
    {
        if (_imageBox.Image is null || !TryConvertToImagePoint(e.Location, out var point))
        {
            return;
        }

        if (_pointA is null)
        {
            _pointA = point;
            _pointALabel.Text = $"Point A: {point}";
            _statusLabel.Text = "Point A selected. Select Point B.";
        }
        else if (_pointB is null)
        {
            _pointB = point;
            _pointBLabel.Text = $"Point B: {point}";
            UpdateDistances();
            _statusLabel.Text = "Both points selected. Use Clear Points before selecting another pair.";
        }

        _imageBox.Invalidate();
    }

    // Zoom can leave letterbox margins; only the calculated displayed rectangle maps to image pixels.
    private bool TryConvertToImagePoint(Point mousePoint, out PixelPoint imagePoint)
    {
        imagePoint = default;
        if (_imageBox.Image is null)
        {
            return false;
        }

        var displayed = GetDisplayedImageRectangle(_imageBox.Image.Size);
        if (!displayed.Contains(mousePoint))
        {
            return false;
        }

        var imageX = Math.Min(_imageBox.Image.Width - 1,
            (int)((mousePoint.X - displayed.Left) * _imageBox.Image.Width / (float)displayed.Width));
        var imageY = Math.Min(_imageBox.Image.Height - 1,
            (int)((mousePoint.Y - displayed.Top) * _imageBox.Image.Height / (float)displayed.Height));
        imagePoint = new PixelPoint(imageX, imageY);
        return true;
    }

    private Rectangle GetDisplayedImageRectangle(Size imageSize)
    {
        var scale = Math.Min(_imageBox.ClientSize.Width / (float)imageSize.Width,
            _imageBox.ClientSize.Height / (float)imageSize.Height);
        var width = Math.Max(1, (int)Math.Round(imageSize.Width * scale));
        var height = Math.Max(1, (int)Math.Round(imageSize.Height * scale));
        return new Rectangle((_imageBox.ClientSize.Width - width) / 2, (_imageBox.ClientSize.Height - height) / 2, width, height);
    }

    private void UpdateDistances()
    {
        if (_pointA is not { } first || _pointB is not { } second)
        {
            return;
        }

        _euclideanLabel.Text = $"Euclidean: {_distanceService.CalculateEuclidean(first, second).ToString("F2", CultureInfo.InvariantCulture)} px";
        _manhattanLabel.Text = $"Manhattan: {_distanceService.CalculateManhattan(first, second)} px";
        _minkowskiLabel.Text = $"Minkowski (p = 3): {_distanceService.CalculateMinkowski(first, second).ToString("F2", CultureInfo.InvariantCulture)} px";
    }

    private void ClearPoints()
    {
        _pointA = null;
        _pointB = null;
        _pointALabel.Text = "Point A: --";
        _pointBLabel.Text = "Point B: --";
        _euclideanLabel.Text = "Euclidean: --";
        _manhattanLabel.Text = "Manhattan: --";
        _minkowskiLabel.Text = "Minkowski (p = 3): --";
        _statusLabel.Text = "Points cleared.";
        _imageBox.Invalidate();
    }

    private void OnImagePaint(object? sender, PaintEventArgs e)
    {
        if (_imageBox.Image is null)
        {
            return;
        }

        var displayed = GetDisplayedImageRectangle(_imageBox.Image.Size);
        if (_pointA is { } pointA && _pointB is { } pointB)
        {
            using var linePen = new Pen(Color.OrangeRed, 2);
            e.Graphics.DrawLine(linePen, ToDisplayPoint(pointA, displayed), ToDisplayPoint(pointB, displayed));
        }

        if (_pointA is { } first)
        {
            DrawMarker(e.Graphics, ToDisplayPoint(first, displayed), Color.DodgerBlue, "A");
        }

        if (_pointB is { } second)
        {
            DrawMarker(e.Graphics, ToDisplayPoint(second, displayed), Color.Crimson, "B");
        }
    }

    private PointF ToDisplayPoint(PixelPoint point, Rectangle displayed) => new(
        displayed.Left + ((point.X + 0.5f) * displayed.Width / _imageBox.Image!.Width),
        displayed.Top + ((point.Y + 0.5f) * displayed.Height / _imageBox.Image!.Height));

    private static void DrawMarker(Graphics graphics, PointF location, Color color, string label)
    {
        const float radius = 6;
        using var brush = new SolidBrush(color);
        using var outline = new Pen(Color.White, 2);
        graphics.FillEllipse(brush, location.X - radius, location.Y - radius, radius * 2, radius * 2);
        graphics.DrawEllipse(outline, location.X - radius, location.Y - radius, radius * 2, radius * 2);
        graphics.DrawString(label, SystemFonts.DefaultFont, brush, location.X + 8, location.Y - 8);
    }

    private void OnFormClosed(object? sender, FormClosedEventArgs e)
    {
        // PictureBox does not own these bitmaps; this form is responsible for their disposal.
        _imageBox.Image = null;
        _grayscaleImage?.Dispose();
        _originalImage?.Dispose();
    }
}

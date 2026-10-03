using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Microsoft.Win32;
using ShapePath = System.Windows.Shapes.Path;

namespace Greenshot.Editor.Mockup
{
    public partial class MainWindow : Window
    {
        private enum DrawingMode
        {
            Classic,
            ClickToAdd
        }

        private enum PointerOperation
        {
            None,
            Create,
            Move,
            Resize
        }

        private enum ResizeHandle
        {
            None,
            Start,
            End,
            TopLeft,
            TopRight,
            BottomLeft,
            BottomRight
        }

        private const double SampleCanvasWidth = 1200;
        private const double SampleCanvasHeight = 800;
        private const double GoldenRatio = 1.61803398875;
        private const double ClickPlacementOffset = 24;
        private static readonly Vector[] ClickPlacementOffsets =
        {
            new Vector(0, 0),
            new Vector(ClickPlacementOffset, ClickPlacementOffset),
            new Vector(-ClickPlacementOffset, -ClickPlacementOffset),
            new Vector(ClickPlacementOffset * 2, ClickPlacementOffset * 2),
            new Vector(-ClickPlacementOffset * 2, -ClickPlacementOffset * 2),
            new Vector(ClickPlacementOffset * 2, -ClickPlacementOffset * 2),
            new Vector(-ClickPlacementOffset * 2, ClickPlacementOffset * 2),
            new Vector(ClickPlacementOffset, -ClickPlacementOffset),
            new Vector(-ClickPlacementOffset, ClickPlacementOffset)
        };

        private readonly List<Annotation> _annotations = new List<Annotation>();
        private Annotation _activeAnnotation;
        private Annotation _selectedAnnotation;
        private AnnotationKind? _selectedTool;
        private DrawingMode _drawingMode;
        private PointerOperation _pointerOperation;
        private ResizeHandle _resizeHandle;
        private Point _pointerStart;
        private Point _initialStart;
        private Point _initialEnd;
        private Rect _initialBounds;
        private List<Point> _initialPoints;
        private bool _isRefreshingProperties;
        private bool _isUpdatingToolSelection;
        private int _clickPlacementCount;

        public MainWindow()
        {
            InitializeComponent();
            SetCanvasImage(CreateSampleCanvas(), SampleCanvasWidth, SampleCanvasHeight);
            StatusText.Text = "Sample canvas";
            RefreshPropertyEditor();
        }

        private void OpenImageButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Image files|*.png;*.jpg;*.jpeg;*.bmp;*.gif;*.tif;*.tiff|All files|*.*",
                CheckFileExists = true,
                Multiselect = false
            };

            if (dialog.ShowDialog(this) != true)
            {
                return;
            }

            LoadImage(dialog.FileName);
        }

        private void LoadImage(string fileName)
        {
            try
            {
                BitmapImage image = DecodeImage(fileName);
                SetCanvasImage(image, image.PixelWidth, image.PixelHeight);
                _annotations.Clear();
                _selectedAnnotation = null;
                _activeAnnotation = null;
                _pointerOperation = PointerOperation.None;
                _clickPlacementCount = 0;
                RenderAnnotations();
                StatusText.Text = System.IO.Path.GetFileName(fileName);
            }
            catch (IOException exception)
            {
                ShowImageLoadError(exception);
            }
            catch (UnauthorizedAccessException exception)
            {
                ShowImageLoadError(exception);
            }
            catch (NotSupportedException exception)
            {
                ShowImageLoadError(exception);
            }
            catch (ArgumentException exception)
            {
                ShowImageLoadError(exception);
            }
            catch (FormatException exception)
            {
                ShowImageLoadError(exception);
            }
        }

        private static BitmapImage DecodeImage(string fileName)
        {
            using (var stream = File.OpenRead(fileName))
            {
                var image = new BitmapImage();
                image.BeginInit();
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.StreamSource = stream;
                image.EndInit();
                image.Freeze();
                return image;
            }
        }

        private void ShowImageLoadError(Exception exception)
        {
            StatusText.Text = $"Could not open image: {exception.Message}";
        }

        private static ImageSource CreateSampleCanvas()
        {
            var drawing = new DrawingGroup();
            drawing.Children.Add(new GeometryDrawing(
                Brushes.White,
                new Pen(Brushes.LightGray, 2),
                new RectangleGeometry(new Rect(0, 0, 1200, 800))));
            drawing.Children.Add(new GeometryDrawing(
                Brushes.LightSkyBlue,
                new Pen(Brushes.DarkSlateBlue, 5),
                new RectangleGeometry(new Rect(150, 130, 420, 250))));
            drawing.Children.Add(new GeometryDrawing(
                Brushes.LightCoral,
                new Pen(Brushes.DarkRed, 5),
                new EllipseGeometry(new Point(830, 300), 145, 115)));
            drawing.Children.Add(new GeometryDrawing(
                null,
                new Pen(Brushes.ForestGreen, 8),
                new LineGeometry(new Point(170, 610), new Point(1020, 500))));

            var image = new DrawingImage(drawing);
            image.Freeze();
            return image;
        }

        private void Tool_Checked(object sender, RoutedEventArgs e)
        {
            if (_isUpdatingToolSelection)
            {
                return;
            }

            if (!(sender is RadioButton button) || !Enum.TryParse(button.Tag as string, out AnnotationKind tool))
            {
                _selectedTool = null;
                return;
            }

            if (_drawingMode == DrawingMode.ClickToAdd)
            {
                if (tool == AnnotationKind.Freehand)
                {
                    _selectedTool = AnnotationKind.Freehand;
                    return;
                }

                AddClickAnnotation(tool);
                SelectSelectionTool();
                return;
            }

            _selectedTool = tool;
        }

        private void AddClickAnnotation(AnnotationKind kind)
        {
            double width = ImageCoordinateSpace.Width * 0.1;
            double height = kind == AnnotationKind.Line || kind == AnnotationKind.Arrow
                ? 0
                : width / GoldenRatio;
            Vector offset = ClickPlacementOffsets[_clickPlacementCount % ClickPlacementOffsets.Length];
            _clickPlacementCount++;

            double left = ImageCoordinateSpace.Width / 2 - width / 2 + offset.X;
            double top = ImageCoordinateSpace.Height / 2 - height / 2 + offset.Y;
            left = Math.Max(0, Math.Min(left, ImageCoordinateSpace.Width - width));
            top = Math.Max(0, Math.Min(top, ImageCoordinateSpace.Height - height));
            Point start = new Point(left, top);
            Point end = new Point(left + width, top + height);

            var annotation = new Annotation(kind)
            {
                Start = start,
                End = end,
                Bounds = CreateBounds(start, end)
            };
            _annotations.Add(annotation);
            _selectedAnnotation = annotation;
            RenderAnnotations();
        }

        private void SelectSelectionTool()
        {
            _selectedTool = null;
            _isUpdatingToolSelection = true;
            SelectToolButton.IsChecked = true;
            _isUpdatingToolSelection = false;
        }

        private void ModeSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ModeSelector.SelectedItem is ComboBoxItem item &&
                Enum.TryParse(item.Tag as string, out DrawingMode mode))
            {
                _drawingMode = mode;
                if (_drawingMode == DrawingMode.ClickToAdd)
                {
                    SelectSelectionTool();
                }
            }
        }

        private void GeometryProperty_LostFocus(object sender, RoutedEventArgs e)
        {
            ApplyGeometryProperties();
        }

        private void GeometryProperty_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                ApplyGeometryProperties();
                e.Handled = true;
            }
        }

        private void ApplyGeometryProperties()
        {
            if (_isRefreshingProperties || _selectedAnnotation == null)
            {
                return;
            }

            bool validX = double.TryParse(PositionXTextBox.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out double x);
            bool validY = double.TryParse(PositionYTextBox.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out double y);
            bool validWidth = double.TryParse(WidthTextBox.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out double width);
            bool validHeight = double.TryParse(HeightTextBox.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out double height);
            bool isLine = _selectedAnnotation.Kind == AnnotationKind.Line || _selectedAnnotation.Kind == AnnotationKind.Arrow;
            if (!validX || !validY || !validWidth || !validHeight ||
                double.IsNaN(x) || double.IsInfinity(x) ||
                double.IsNaN(y) || double.IsInfinity(y) ||
                double.IsNaN(width) || double.IsInfinity(width) ||
                double.IsNaN(height) || double.IsInfinity(height) ||
                width < 0 || height < 0 || (!isLine && (width < 1 || height < 1)))
            {
                StatusText.Text = "Enter finite positions and non-negative dimensions.";
                RefreshPropertyEditor();
                return;
            }

            UpdateAnnotationBounds(_selectedAnnotation, new Rect(x, y, width, height));
            RenderAnnotations();
        }

        private void ColorProperty_LostFocus(object sender, RoutedEventArgs e)
        {
            if (_isRefreshingProperties || _selectedAnnotation == null || !(sender is TextBox textBox))
            {
                return;
            }

            if (!TryParseColor(textBox.Text, out Color color))
            {
                StatusText.Text = "Enter a valid color value, such as #FFFF0000.";
                RefreshPropertyEditor();
                return;
            }

            if (textBox == LineColorTextBox)
            {
                _selectedAnnotation.LineColor = color;
            }
            else
            {
                _selectedAnnotation.FillColor = color;
            }

            RenderAnnotations();
        }

        private void ColorProperty_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                ColorProperty_LostFocus(sender, e);
                e.Handled = true;
            }
        }

        private void LineThicknessProperty_LostFocus(object sender, RoutedEventArgs e)
        {
            if (_isRefreshingProperties || _selectedAnnotation == null)
            {
                return;
            }

            if (!double.TryParse(LineThicknessTextBox.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out double thickness) ||
                double.IsNaN(thickness) || double.IsInfinity(thickness) || thickness <= 0)
            {
                StatusText.Text = "Enter a finite line thickness greater than zero.";
                RefreshPropertyEditor();
                return;
            }

            _selectedAnnotation.LineThickness = thickness;
            RenderAnnotations();
        }

        private void LineThicknessProperty_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                LineThicknessProperty_LostFocus(sender, e);
                e.Handled = true;
            }
        }

        private void RefreshPropertyEditor()
        {
            bool hasSelection = _selectedAnnotation != null;
            NoSelectionText.Visibility = hasSelection ? Visibility.Collapsed : Visibility.Visible;
            PropertiesFields.IsEnabled = hasSelection;
            if (!hasSelection)
            {
                return;
            }

            _isRefreshingProperties = true;
            PositionXTextBox.Text = _selectedAnnotation.Bounds.X.ToString("0.##", CultureInfo.InvariantCulture);
            PositionYTextBox.Text = _selectedAnnotation.Bounds.Y.ToString("0.##", CultureInfo.InvariantCulture);
            WidthTextBox.Text = _selectedAnnotation.Bounds.Width.ToString("0.##", CultureInfo.InvariantCulture);
            HeightTextBox.Text = _selectedAnnotation.Bounds.Height.ToString("0.##", CultureInfo.InvariantCulture);
            LineColorTextBox.Text = _selectedAnnotation.LineColor.ToString();
            FillColorTextBox.Text = _selectedAnnotation.FillColor.ToString();
            LineThicknessTextBox.Text = _selectedAnnotation.LineThickness.ToString("0.##", CultureInfo.InvariantCulture);
            _isRefreshingProperties = false;
        }

        private static bool TryParseColor(string text, out Color color)
        {
            try
            {
                object parsed = ColorConverter.ConvertFromString(text);
                if (parsed is Color parsedColor)
                {
                    color = parsedColor;
                    return true;
                }
            }
            catch (FormatException)
            {
            }
            catch (NotSupportedException)
            {
            }

            color = Colors.Transparent;
            return false;
        }

        private static void UpdateAnnotationBounds(Annotation annotation, Rect bounds)
        {
            Rect previousBounds = annotation.Bounds;
            if (annotation.Kind == AnnotationKind.Freehand)
            {
                var resizedPoints = new List<Point>(annotation.Points.Count);
                foreach (Point point in annotation.Points)
                {
                    double normalizedX = previousBounds.Width > 0
                        ? (point.X - previousBounds.Left) / previousBounds.Width
                        : 0;
                    double normalizedY = previousBounds.Height > 0
                        ? (point.Y - previousBounds.Top) / previousBounds.Height
                        : 0;
                    resizedPoints.Add(new Point(
                        bounds.Left + normalizedX * bounds.Width,
                        bounds.Top + normalizedY * bounds.Height));
                }

                annotation.Points.Clear();
                annotation.Points.AddRange(resizedPoints);
            }

            annotation.Bounds = bounds;
            annotation.Start = new Point(bounds.Left, bounds.Top);
            annotation.End = new Point(bounds.Right, bounds.Bottom);
        }

        private void AnnotationCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Point point = e.GetPosition(AnnotationCanvas);
            if (_selectedTool == null)
            {
                Annotation hitAnnotation = FindAnnotationAt(point);
                if (hitAnnotation == null)
                {
                    _selectedAnnotation = null;
                    _activeAnnotation = null;
                    _pointerOperation = PointerOperation.None;
                    RenderAnnotations();
                    return;
                }

                _selectedAnnotation = hitAnnotation;
                ResizeHandle handle = GetResizeHandleAt(hitAnnotation, point);
                if (handle != ResizeHandle.None)
                {
                    BeginManipulation(hitAnnotation, point, PointerOperation.Resize, handle);
                }
                else
                {
                    BeginManipulation(hitAnnotation, point, PointerOperation.Move, ResizeHandle.None);
                }

                RenderAnnotations();
                e.Handled = true;
                return;
            }

            var annotation = new Annotation(_selectedTool.Value)
            {
                Start = point,
                End = point,
                Bounds = new Rect(point, point)
            };
            if (annotation.Kind == AnnotationKind.Freehand)
            {
                annotation.Points.Add(point);
            }

            _annotations.Add(annotation);
            _activeAnnotation = annotation;
            _selectedAnnotation = annotation;
            _pointerOperation = PointerOperation.Create;
            _pointerStart = point;
            AnnotationCanvas.CaptureMouse();
            RenderAnnotations();
            e.Handled = true;
        }

        private void AnnotationCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (_activeAnnotation == null || _pointerOperation == PointerOperation.None ||
                e.LeftButton != MouseButtonState.Pressed)
            {
                return;
            }

            Point point = e.GetPosition(AnnotationCanvas);
            if (_pointerOperation == PointerOperation.Create)
            {
                _activeAnnotation.End = point;
                _activeAnnotation.Bounds = CreateBounds(_activeAnnotation.Start, point);
                if (_activeAnnotation.Kind == AnnotationKind.Freehand &&
                    Distance(_activeAnnotation.Points[_activeAnnotation.Points.Count - 1], point) >= 2)
                {
                    _activeAnnotation.Points.Add(point);
                }

                if (_activeAnnotation.Kind == AnnotationKind.Freehand)
                {
                    _activeAnnotation.Bounds = GetPointBounds(_activeAnnotation.Points);
                }
            }
            else if (_pointerOperation == PointerOperation.Move)
            {
                MoveAnnotation(point - _pointerStart);
            }
            else if (_pointerOperation == PointerOperation.Resize)
            {
                ResizeAnnotation(point);
            }

            RenderAnnotations();
        }

        private void AnnotationCanvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_activeAnnotation == null)
            {
                return;
            }

            bool resetToolAfterStroke = _drawingMode == DrawingMode.ClickToAdd &&
                                        _activeAnnotation.Kind == AnnotationKind.Freehand;
            if (_pointerOperation == PointerOperation.Create)
            {
                Point point = e.GetPosition(AnnotationCanvas);
                _activeAnnotation.End = point;
                _activeAnnotation.Bounds = CreateBounds(_activeAnnotation.Start, point);
                if (_activeAnnotation.Kind == AnnotationKind.Freehand &&
                    Distance(_activeAnnotation.Points[_activeAnnotation.Points.Count - 1], point) > 0)
                {
                    _activeAnnotation.Points.Add(point);
                }

                if (_activeAnnotation.Kind == AnnotationKind.Freehand)
                {
                    _activeAnnotation.Bounds = GetPointBounds(_activeAnnotation.Points);
                }
            }

            _activeAnnotation = null;
            _pointerOperation = PointerOperation.None;
            _resizeHandle = ResizeHandle.None;
            AnnotationCanvas.ReleaseMouseCapture();
            if (resetToolAfterStroke)
            {
                SelectSelectionTool();
            }

            RenderAnnotations();
            e.Handled = true;
        }

        private void BeginManipulation(Annotation annotation, Point point, PointerOperation operation, ResizeHandle handle)
        {
            _activeAnnotation = annotation;
            _pointerOperation = operation;
            _resizeHandle = handle;
            _pointerStart = point;
            _initialBounds = annotation.Bounds;
            _initialStart = annotation.Start;
            _initialEnd = annotation.End;
            _initialPoints = new List<Point>(annotation.Points);
            AnnotationCanvas.CaptureMouse();
        }

        private void MoveAnnotation(Vector offset)
        {
            _activeAnnotation.Bounds = Offset(_initialBounds, offset);
            _activeAnnotation.Start = _initialStart + offset;
            _activeAnnotation.End = _initialEnd + offset;
            _activeAnnotation.Points.Clear();
            foreach (Point point in _initialPoints)
            {
                _activeAnnotation.Points.Add(point + offset);
            }
        }

        private void ResizeAnnotation(Point point)
        {
            if (_resizeHandle == ResizeHandle.Start || _resizeHandle == ResizeHandle.End)
            {
                if (_resizeHandle == ResizeHandle.Start)
                {
                    _activeAnnotation.Start = point;
                }
                else
                {
                    _activeAnnotation.End = point;
                }

                _activeAnnotation.Bounds = CreateBounds(_activeAnnotation.Start, _activeAnnotation.End);
                return;
            }

            Rect bounds = ResizeBounds(_initialBounds, point, _resizeHandle);
            _activeAnnotation.Bounds = bounds;
            _activeAnnotation.Start = new Point(bounds.Left, bounds.Top);
            _activeAnnotation.End = new Point(bounds.Right, bounds.Bottom);

            if (_activeAnnotation.Kind == AnnotationKind.Freehand)
            {
                _activeAnnotation.Points.Clear();
                foreach (Point originalPoint in _initialPoints)
                {
                    double normalizedX = _initialBounds.Width > 0
                        ? (originalPoint.X - _initialBounds.Left) / _initialBounds.Width
                        : 0;
                    double normalizedY = _initialBounds.Height > 0
                        ? (originalPoint.Y - _initialBounds.Top) / _initialBounds.Height
                        : 0;
                    _activeAnnotation.Points.Add(new Point(
                        bounds.Left + normalizedX * bounds.Width,
                        bounds.Top + normalizedY * bounds.Height));
                }
            }
        }

        private ResizeHandle GetResizeHandleAt(Annotation annotation, Point point)
        {
            if (annotation.Kind == AnnotationKind.Line || annotation.Kind == AnnotationKind.Arrow)
            {
                if (Distance(point, annotation.Start) <= 10)
                {
                    return ResizeHandle.Start;
                }

                if (Distance(point, annotation.End) <= 10)
                {
                    return ResizeHandle.End;
                }

                return ResizeHandle.None;
            }

            Rect bounds = annotation.Bounds;
            if (Distance(point, new Point(bounds.Left, bounds.Top)) <= 10)
            {
                return ResizeHandle.TopLeft;
            }

            if (Distance(point, new Point(bounds.Right, bounds.Top)) <= 10)
            {
                return ResizeHandle.TopRight;
            }

            if (Distance(point, new Point(bounds.Left, bounds.Bottom)) <= 10)
            {
                return ResizeHandle.BottomLeft;
            }

            if (Distance(point, new Point(bounds.Right, bounds.Bottom)) <= 10)
            {
                return ResizeHandle.BottomRight;
            }

            return ResizeHandle.None;
        }

        private Annotation FindAnnotationAt(Point point)
        {
            for (int i = _annotations.Count - 1; i >= 0; i--)
            {
                Annotation annotation = _annotations[i];
                Rect hitBounds = annotation.Bounds;
                hitBounds.Inflate(7, 7);

                if (annotation.Kind == AnnotationKind.Ellipse)
                {
                    double radiusX = Math.Max(annotation.Bounds.Width / 2, 1);
                    double radiusY = Math.Max(annotation.Bounds.Height / 2, 1);
                    double normalizedX = (point.X - annotation.Bounds.X - annotation.Bounds.Width / 2) / radiusX;
                    double normalizedY = (point.Y - annotation.Bounds.Y - annotation.Bounds.Height / 2) / radiusY;
                    if (normalizedX * normalizedX + normalizedY * normalizedY <= 1.2)
                    {
                        return annotation;
                    }
                }
                else if (annotation.Kind == AnnotationKind.Line || annotation.Kind == AnnotationKind.Arrow)
                {
                    if (DistanceToSegment(point, annotation.Start, annotation.End) <= 7)
                    {
                        return annotation;
                    }
                }
                else if (annotation.Kind == AnnotationKind.Freehand)
                {
                    for (int pointIndex = 1; pointIndex < annotation.Points.Count; pointIndex++)
                    {
                        if (DistanceToSegment(point, annotation.Points[pointIndex - 1], annotation.Points[pointIndex]) <= 7)
                        {
                            return annotation;
                        }
                    }
                }
                else if (hitBounds.Contains(point))
                {
                    return annotation;
                }
            }

            return null;
        }

        private void RenderAnnotations()
        {
            AnnotationCanvas.Children.Clear();
            foreach (Annotation annotation in _annotations)
            {
                var stroke = new SolidColorBrush(annotation.LineColor);
                var fill = new SolidColorBrush(annotation.FillColor);
                stroke.Freeze();
                fill.Freeze();
                double strokeThickness = annotation.LineThickness;

                switch (annotation.Kind)
                {
                    case AnnotationKind.Rectangle:
                        AddBoundedShape(new Rectangle
                        {
                            Stroke = stroke,
                            StrokeThickness = strokeThickness,
                            Fill = fill,
                            IsHitTestVisible = false
                        }, annotation.Bounds);
                        break;
                    case AnnotationKind.Ellipse:
                        AddBoundedShape(new Ellipse
                        {
                            Stroke = stroke,
                            StrokeThickness = strokeThickness,
                            Fill = fill,
                            IsHitTestVisible = false
                        }, annotation.Bounds);
                        break;
                    case AnnotationKind.Line:
                        AnnotationCanvas.Children.Add(new Line
                        {
                            X1 = annotation.Start.X,
                            Y1 = annotation.Start.Y,
                            X2 = annotation.End.X,
                            Y2 = annotation.End.Y,
                            Stroke = stroke,
                            StrokeThickness = strokeThickness,
                            IsHitTestVisible = false
                        });
                        break;
                    case AnnotationKind.Arrow:
                        AnnotationCanvas.Children.Add(CreateArrow(annotation, stroke, strokeThickness));
                        break;
                    case AnnotationKind.Text:
                        AddBoundedShape(new TextBlock
                        {
                            Text = annotation.Text,
                            FontSize = 28,
                            Foreground = stroke,
                            IsHitTestVisible = false
                        }, annotation.Bounds);
                        break;
                    case AnnotationKind.Freehand:
                        var polyline = new Polyline
                        {
                            Stroke = stroke,
                            StrokeThickness = strokeThickness,
                            StrokeLineJoin = PenLineJoin.Round,
                            StrokeStartLineCap = PenLineCap.Round,
                            StrokeEndLineCap = PenLineCap.Round,
                            IsHitTestVisible = false
                        };
                        foreach (Point point in annotation.Points)
                        {
                            polyline.Points.Add(point);
                        }

                        AnnotationCanvas.Children.Add(polyline);
                        break;
                }
            }

            if (_selectedAnnotation != null)
            {
                RenderSelectionHandles(_selectedAnnotation);
            }

            RefreshPropertyEditor();
        }

        private void RenderSelectionHandles(Annotation annotation)
        {
            if (annotation.Kind == AnnotationKind.Line || annotation.Kind == AnnotationKind.Arrow)
            {
                AddSelectionHandle(annotation.Start);
                AddSelectionHandle(annotation.End);
                return;
            }

            Rect bounds = annotation.Bounds;
            var selectionOutline = new Rectangle
            {
                Width = bounds.Width,
                Height = bounds.Height,
                Stroke = Brushes.DeepSkyBlue,
                StrokeThickness = 1,
                StrokeDashArray = new DoubleCollection { 4, 3 },
                IsHitTestVisible = false
            };
            Canvas.SetLeft(selectionOutline, bounds.Left);
            Canvas.SetTop(selectionOutline, bounds.Top);
            AnnotationCanvas.Children.Add(selectionOutline);

            AddSelectionHandle(new Point(bounds.Left, bounds.Top));
            AddSelectionHandle(new Point(bounds.Right, bounds.Top));
            AddSelectionHandle(new Point(bounds.Left, bounds.Bottom));
            AddSelectionHandle(new Point(bounds.Right, bounds.Bottom));
        }

        private void AddSelectionHandle(Point point)
        {
            var handle = new Rectangle
            {
                Width = 12,
                Height = 12,
                Fill = Brushes.White,
                Stroke = Brushes.DeepSkyBlue,
                StrokeThickness = 2,
                IsHitTestVisible = false
            };
            Canvas.SetLeft(handle, point.X - handle.Width / 2);
            Canvas.SetTop(handle, point.Y - handle.Height / 2);
            AnnotationCanvas.Children.Add(handle);
        }

        private void AddBoundedShape(FrameworkElement element, Rect bounds)
        {
            element.Width = Math.Max(bounds.Width, 1);
            element.Height = Math.Max(bounds.Height, 1);
            Canvas.SetLeft(element, bounds.X);
            Canvas.SetTop(element, bounds.Y);
            AnnotationCanvas.Children.Add(element);
        }

        private static ShapePath CreateArrow(Annotation annotation, Brush stroke, double strokeThickness)
        {
            Vector direction = annotation.End - annotation.Start;
            double length = direction.Length;
            if (length < 0.1)
            {
                return new ShapePath
                {
                    Data = new LineGeometry(annotation.Start, annotation.End),
                    Stroke = stroke,
                    StrokeThickness = strokeThickness,
                    IsHitTestVisible = false
                };
            }

            direction.Normalize();
            Vector perpendicular = new Vector(-direction.Y, direction.X);
            double headLength = Math.Min(24, length * 0.35);
            Point basePoint = annotation.End - direction * headLength;
            Point left = basePoint + perpendicular * (headLength * 0.55);
            Point right = basePoint - perpendicular * (headLength * 0.55);

            var geometry = new GeometryGroup();
            geometry.Children.Add(new LineGeometry(annotation.Start, annotation.End));
            var arrowHead = new PathFigure { StartPoint = annotation.End, IsClosed = true };
            arrowHead.Segments.Add(new LineSegment(left, true));
            arrowHead.Segments.Add(new LineSegment(right, true));
            var arrowHeadGeometry = new PathGeometry();
            arrowHeadGeometry.Figures.Add(arrowHead);
            geometry.Children.Add(arrowHeadGeometry);

            return new ShapePath
            {
                Data = geometry,
                Stroke = stroke,
                StrokeThickness = strokeThickness,
                Fill = stroke,
                IsHitTestVisible = false
            };
        }

        private static Rect CreateBounds(Point first, Point second)
        {
            return new Rect(
                Math.Min(first.X, second.X),
                Math.Min(first.Y, second.Y),
                Math.Abs(second.X - first.X),
                Math.Abs(second.Y - first.Y));
        }

        private static Rect GetPointBounds(IList<Point> points)
        {
            double left = points[0].X;
            double top = points[0].Y;
            double right = left;
            double bottom = top;

            for (int i = 1; i < points.Count; i++)
            {
                left = Math.Min(left, points[i].X);
                top = Math.Min(top, points[i].Y);
                right = Math.Max(right, points[i].X);
                bottom = Math.Max(bottom, points[i].Y);
            }

            return new Rect(left, top, right - left, bottom - top);
        }

        private static Rect Offset(Rect bounds, Vector offset)
        {
            return new Rect(
                bounds.X + offset.X,
                bounds.Y + offset.Y,
                bounds.Width,
                bounds.Height);
        }

        private static Rect ResizeBounds(Rect initialBounds, Point point, ResizeHandle handle)
        {
            double left = initialBounds.Left;
            double top = initialBounds.Top;
            double right = initialBounds.Right;
            double bottom = initialBounds.Bottom;

            switch (handle)
            {
                case ResizeHandle.TopLeft:
                    left = Math.Min(point.X, right - 1);
                    top = Math.Min(point.Y, bottom - 1);
                    break;
                case ResizeHandle.TopRight:
                    right = Math.Max(point.X, left + 1);
                    top = Math.Min(point.Y, bottom - 1);
                    break;
                case ResizeHandle.BottomLeft:
                    left = Math.Min(point.X, right - 1);
                    bottom = Math.Max(point.Y, top + 1);
                    break;
                case ResizeHandle.BottomRight:
                    right = Math.Max(point.X, left + 1);
                    bottom = Math.Max(point.Y, top + 1);
                    break;
            }

            return new Rect(left, top, right - left, bottom - top);
        }

        private static double Distance(Point first, Point second)
        {
            return (first - second).Length;
        }

        private static double DistanceToSegment(Point point, Point start, Point end)
        {
            Vector segment = end - start;
            double lengthSquared = segment.LengthSquared;
            if (lengthSquared < 0.001)
            {
                return Distance(point, start);
            }

            double projection = Math.Max(0, Math.Min(1, Vector.Multiply(point - start, segment) / lengthSquared));
            return Distance(point, start + segment * projection);
        }

        private void SetCanvasImage(ImageSource image, double width, double height)
        {
            ImageCoordinateSpace.Width = width;
            ImageCoordinateSpace.Height = height;
            AnnotationCanvas.Width = width;
            AnnotationCanvas.Height = height;
            CanvasImage.Source = image;
        }
    }
}

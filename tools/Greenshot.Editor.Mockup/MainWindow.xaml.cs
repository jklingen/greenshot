using System;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
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
            BottomRight,
            Top,
            Bottom,
            Left,
            Right
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
        private readonly Dictionary<Annotation, RichTextBox> _textEditors = new Dictionary<Annotation, RichTextBox>();
        private Annotation _activeAnnotation;
        private readonly List<Annotation> _selection = new List<Annotation>();
        private List<MoveSnapshot> _moveSnapshots = new List<MoveSnapshot>();
        private AnnotationKind? _selectedTool;
        private DrawingMode _drawingMode = DrawingMode.ClickToAdd;
        private PointerOperation _pointerOperation;
        private ResizeHandle _resizeHandle;
        private Point _pointerStart;
        private Point _initialStart;
        private Point _initialEnd;
        private Rect _initialBounds;
        private List<Point> _initialPoints;
        private bool _isRefreshingProperties;
        private Color _drawLineColor = Colors.Red;
        private Color _drawFillColor = Color.FromArgb(35, 255, 0, 0);
        private Annotation _cursor;
        private Annotation _editingText;
        private bool _keyboardMode;
        private int _clickPlacementCount;
        private Window _floatingPropertiesWindow;

        public MainWindow()
        {
            InitializeComponent();
            FillColorPicker.UseDefaultColors(80);
            TextFontFamilyComboBox.ItemsSource = Fonts.SystemFontFamilies
                .Select(font => font.Source)
                .OrderBy(name => name, StringComparer.CurrentCultureIgnoreCase)
                .ToList();
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
                _textEditors.Clear();
                _selection.Clear();
                _editingText = null;
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
            using (DrawingContext dc = drawing.Open())
            {
                Brush blue = new SolidColorBrush(Color.FromRgb(0x2B, 0x57, 0x9A));
                Brush teal = new SolidColorBrush(Color.FromRgb(0x17, 0xA2, 0x8B));
                Brush ink = new SolidColorBrush(Color.FromRgb(0x2A, 0x2F, 0x3A));
                Brush soft = new SolidColorBrush(Color.FromRgb(0x5B, 0x64, 0x75));
                Brush panel = new SolidColorBrush(Color.FromRgb(0xEE, 0xF3, 0xFA));

                dc.DrawRectangle(new SolidColorBrush(Color.FromRgb(0xF4, 0xF7, 0xFB)), null, new Rect(0, 0, 1200, 800));
                dc.DrawRectangle(
                    new LinearGradientBrush(Color.FromRgb(0x2B, 0x57, 0x9A), Color.FromRgb(0x17, 0xA2, 0x8B), 0),
                    null,
                    new Rect(0, 0, 1200, 150));
                DrawSampleText(dc, "Meet the new Greenshot editor \u2728", 40, 24, 44, FontWeights.Bold, Brushes.White);
                DrawSampleText(dc, "Three changes you'll feel right away. Go on, poke around \u2013 tools on the left, properties on the right!", 40, 92, 19, FontWeights.Normal, Brushes.White);

                var cards = new[]
                {
                    new
                    {
                        Title = "Properties on the right",
                        Pitch = "Goodbye cramped toolbar, hello a proper side panel.",
                        Why = new[]
                        {
                            "Room for real UI: recently used colors are one click away",
                            "Grouped and collapsible, undock it if you like",
                            "Reachable by keyboard (F6, Tab)"
                        }
                    },
                    new
                    {
                        Title = "Click to add",
                        Pitch = "Click a tool and the element just appears. Then fine-tune it.",
                        Why = new[]
                        {
                            "More intuitive for many people than select-then-drag",
                            "Makes drawing possible without a mouse",
                            "A setting: the classic select-and-drag mode stays"
                        }
                    },
                    new
                    {
                        Title = "Text on anything",
                        Pitch = "Select an element and simply start typing.",
                        Why = new[]
                        {
                            "No extra text tool, no extra step",
                            "Label boxes, ellipses, lines and arrows in seconds",
                            "Edited right on the canvas, formatted per word"
                        }
                    }
                };

                for (int i = 0; i < cards.Length; i++)
                {
                    double x = 30 + i * 390;
                    double y = 185;
                    dc.DrawRoundedRectangle(new SolidColorBrush(Color.FromArgb(28, 0, 0, 0)), null, new Rect(x + 3, y + 5, 360, 540), 16, 16);
                    dc.DrawRoundedRectangle(Brushes.White, null, new Rect(x, y, 360, 540), 16, 16);

                    dc.DrawEllipse(i == 1 ? teal : blue, null, new Point(x + 36, y + 38), 20, 20);
                    DrawSampleText(dc, (i + 1).ToString(CultureInfo.InvariantCulture), x + 29, y + 22, 26, FontWeights.Bold, Brushes.White);
                    DrawSampleText(dc, cards[i].Title, x + 66, y + 24, 24, FontWeights.Bold, ink, 280);
                    DrawSampleText(dc, cards[i].Pitch, x + 24, y + 80, 17, FontWeights.Normal, soft, 312);

                    dc.DrawRoundedRectangle(panel, null, new Rect(x + 24, y + 150, 312, 130), 10, 10);
                    DrawCardIllustration(dc, i, x + 24, y + 150, blue, teal);

                    DrawSampleText(dc, "WHY?", x + 24, y + 305, 15, FontWeights.Bold, teal);
                    double by = y + 332;
                    foreach (string reason in cards[i].Why)
                    {
                        dc.DrawEllipse(teal, null, new Point(x + 32, by + 13), 4, 4);
                        DrawSampleText(dc, reason, x + 46, by, 17, FontWeights.Normal, ink, 290);
                        by += 62;
                    }
                }

                DrawSampleText(dc, "This is a discussion mock-up. What would you change?", 40, 755, 18, FontWeights.Bold, blue);
            }

            var image = new DrawingImage(drawing);
            image.Freeze();
            return image;
        }

        private static void DrawCardIllustration(DrawingContext dc, int index, double x, double y, Brush blue, Brush teal)
        {
            var line = new Pen(Brushes.LightGray, 2);
            if (index == 0)
            {
                dc.DrawRoundedRectangle(Brushes.White, line, new Rect(x + 150, y + 14, 150, 102), 6, 6);
                DrawSampleText(dc, "Color", x + 160, y + 20, 13, FontWeights.Bold, Brushes.Gray);
                Color[] colors = { Colors.Red, Colors.Orange, Colors.Gold, Colors.SeaGreen, Colors.DodgerBlue, Colors.MediumPurple };
                for (int c = 0; c < colors.Length; c++)
                {
                    dc.DrawRoundedRectangle(new SolidColorBrush(colors[c]), null, new Rect(x + 160 + (c % 3) * 44, y + 44 + (c / 3) * 34, 34, 26), 4, 4);
                }

                dc.DrawRectangle(Brushes.Gainsboro, null, new Rect(x + 14, y + 20, 100, 8));
                dc.DrawRectangle(Brushes.Gainsboro, null, new Rect(x + 14, y + 52, 100, 8));
                dc.DrawRectangle(Brushes.Gainsboro, null, new Rect(x + 14, y + 84, 100, 8));
                dc.DrawText(new FormattedText("\u2192", CultureInfo.InvariantCulture, FlowDirection.LeftToRight, new Typeface("Segoe UI"), 30, teal, 1.0), new Point(x + 118, y + 42));
            }
            else if (index == 1)
            {
                dc.DrawRoundedRectangle(Brushes.White, line, new Rect(x + 16, y + 30, 50, 70), 6, 6);
                dc.DrawRectangle(blue, null, new Rect(x + 28, y + 44, 26, 16));
                DrawSampleText(dc, "click", x + 22, y + 66, 14, FontWeights.Bold, Brushes.Gray);
                DrawSampleText(dc, "\u2192", x + 80, y + 40, 36, FontWeights.Bold, teal);
                dc.DrawRoundedRectangle(new SolidColorBrush(Color.FromArgb(60, 255, 0, 0)), new Pen(Brushes.Red, 3), new Rect(x + 140, y + 30, 120, 70), 4, 4);
                foreach (Point corner in new[] { new Point(140, 30), new Point(260, 30), new Point(140, 100), new Point(260, 100) })
                {
                    dc.DrawRectangle(Brushes.White, new Pen(Brushes.DeepSkyBlue, 2), new Rect(x + corner.X - 4, y + corner.Y - 4, 8, 8));
                }
            }
            else
            {
                dc.DrawRoundedRectangle(new SolidColorBrush(Color.FromArgb(40, 43, 87, 154)), new Pen(blue, 3), new Rect(x + 20, y + 24, 130, 80), 4, 4);
                DrawSampleText(dc, "Look!|", x + 52, y + 50, 22, FontWeights.Bold, Brushes.Black);
                dc.DrawLine(new Pen(blue, 3), new Point(x + 175, y + 100), new Point(x + 290, y + 60));
                dc.PushTransform(new RotateTransform(-19, x + 175, y + 100));
                DrawSampleText(dc, "typed in seconds", x + 180, y + 72, 15, FontWeights.Normal, Brushes.Black);
                dc.Pop();
            }
        }
        private static void DrawSampleText(DrawingContext dc, string text, double x, double y, double size, FontWeight weight, Brush brush, double maxWidth = 0)
        {
            var formatted = new FormattedText(
                text,
                CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight,
                new Typeface(new FontFamily("Segoe UI"), FontStyles.Normal, weight, FontStretches.Normal),
                size,
                brush,
                1.0);
            if (maxWidth > 0)
            {
                formatted.MaxTextWidth = maxWidth;
                formatted.MaxTextHeight = 60;
            }

            dc.DrawText(formatted, new Point(x, y));
        }
        private void Tool_Click(object sender, RoutedEventArgs e)
        {
            if (!(sender is ToggleButton button) || !Enum.TryParse(button.Tag as string, out AnnotationKind tool))
            {
                _selectedTool = null;
                UpdateToolButtons(SelectToolButton);
                return;
            }

            if (_drawingMode == DrawingMode.ClickToAdd)
            {
                if (tool == AnnotationKind.Freehand)
                {
                    _selectedTool = AnnotationKind.Freehand;
                    UpdateToolButtons(button);
                    return;
                }

                AddClickAnnotation(tool);
                SelectSelectionTool();
                CanvasHost.Focus();
                return;
            }

            _selectedTool = tool;
            UpdateToolButtons(button);
        }

        private void UpdateToolButtons(ToggleButton active)
        {
            foreach (ToggleButton toolButton in ToolsPanel.Children)
            {
                toolButton.IsChecked = toolButton == active;
            }
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
                LineColor = _drawLineColor,
                FillColor = _drawFillColor,
                Start = start,
                End = end,
                Bounds = CreateBounds(start, end)
            };
            _annotations.Add(annotation);
            SetSelection(annotation);
            RenderAnnotations();
        }

        private void CanvasHost_FocusChanged(object sender, KeyboardFocusChangedEventArgs e)
        {
            RenderAnnotations();
        }

        private void RenderKeyboardCursor()
        {
            bool showFocus = _keyboardMode && CanvasHost.IsKeyboardFocused;
            CanvasHost.BorderBrush = showFocus ? Brushes.DeepSkyBlue : Brushes.Transparent;
            if (!showFocus || _cursor == null || !_annotations.Contains(_cursor))
            {
                return;
            }

            Rect bounds = _cursor.Bounds;
            bounds.Inflate(8, 8);
            var frame = new Rectangle
            {
                Width = bounds.Width,
                Height = bounds.Height,
                Stroke = Brushes.DeepSkyBlue,
                StrokeThickness = 2,
                StrokeDashArray = new DoubleCollection { 4, 3 },
                IsHitTestVisible = false
            };
            Canvas.SetLeft(frame, bounds.Left);
            Canvas.SetTop(frame, bounds.Top);
            AnnotationCanvas.Children.Add(frame);
        }

        private void Window_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            _keyboardMode = false;
            if (_editingText != null &&
                !IsVisualDescendantOf(e.OriginalSource as DependencyObject, _textEditors[_editingText]) &&
                !IsVisualDescendantOf(e.OriginalSource as DependencyObject, PropertiesPanel))
            {
                EndTextEditing();
            }
        }

        private static bool IsVisualDescendantOf(DependencyObject child, DependencyObject ancestor)
        {
            while (child != null)
            {
                if (child == ancestor)
                {
                    return true;
                }

                child = child is Visual
                    ? VisualTreeHelper.GetParent(child)
                    : LogicalTreeHelper.GetParent(child);
            }

            return false;
        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            Key key = e.Key == Key.System ? e.SystemKey : e.Key;
            if (key != Key.LeftShift && key != Key.RightShift && key != Key.LeftCtrl && key != Key.RightCtrl &&
                key != Key.LeftAlt && key != Key.RightAlt)
            {
                _keyboardMode = true;
            }

            if (key == Key.F6)
            {
                CycleRegion((Keyboard.Modifiers & ModifierKeys.Shift) != 0 ? -1 : 1);
                e.Handled = true;
            }
            else if (key == Key.Escape && e.OriginalSource is TextBox box && PropertiesPanel.IsKeyboardFocusWithin)
            {
                CanvasHost.Focus();
                e.Handled = true;
            }
        }

        private void CycleRegion(int direction)
        {
            int current = 0;
            if (ToolsPanel.IsKeyboardFocusWithin)
            {
                current = 1;
            }
            else if (CanvasHost.IsKeyboardFocused)
            {
                current = 2;
            }
            else if (PropertiesPanel.IsKeyboardFocusWithin)
            {
                current = 3;
            }

            int next = (current + direction + 4) % 4;
            switch (next)
            {
                case 0:
                    OpenImageButton.Focus();
                    break;
                case 1:
                    SelectToolButton.Focus();
                    break;
                case 2:
                    CanvasHost.Focus();
                    break;
                default:
                    _floatingPropertiesWindow?.Activate();
                    UndockButton.Focus();
                    break;
            }
        }

        private void CanvasHost_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.OriginalSource != CanvasHost)
            {
                return;
            }

            Key key = e.Key == Key.System ? e.SystemKey : e.Key;
            ModifierKeys modifiers = Keyboard.Modifiers;
            bool shift = (modifiers & ModifierKeys.Shift) != 0;
            bool ctrl = (modifiers & ModifierKeys.Control) != 0;
            bool alt = (modifiers & ModifierKeys.Alt) != 0;
            int step = shift ? 10 : 1;

            switch (key)
            {
                case Key.Tab:
                    if (!MoveCursor(shift ? -1 : 1, !ctrl))
                    {
                        return;
                    }

                    break;
                case Key.Left:
                case Key.Right:
                case Key.Up:
                case Key.Down:
                    if (_selection.Count == 0)
                    {
                        return;
                    }

                    int dx = key == Key.Left ? -1 : key == Key.Right ? 1 : 0;
                    int dy = key == Key.Up ? -1 : key == Key.Down ? 1 : 0;
                    if (alt)
                    {
                        ResizeSelection(dx * step, dy * step);
                    }
                    else
                    {
                        MoveSelection(new Vector(dx * step, dy * step));
                    }

                    break;
                case Key.Space:
                    if (_cursor == null || !_annotations.Contains(_cursor))
                    {
                        return;
                    }

                    if (!_selection.Remove(_cursor))
                    {
                        _selection.Add(_cursor);
                    }

                    break;
                case Key.A when ctrl:
                    _selection.Clear();
                    _selection.AddRange(_annotations);
                    break;
                case Key.Escape:
                    _selection.Clear();
                    break;
                case Key.Delete:
                    _annotations.RemoveAll(a => _selection.Contains(a));
                    _selection.Clear();
                    _cursor = null;
                    break;
                case Key.Enter:
                    if (_selection.Count == 0)
                    {
                        return;
                    }

                    LayoutExpander.IsExpanded = true;
                    PositionXTextBox.Focus();
                    PositionXTextBox.SelectAll();
                    e.Handled = true;
                    return;
                case Key.F2:
                    if (GetSelectedTextAnnotation() == null)
                    {
                        return;
                    }

                    StartTextEditing(GetSelectedTextAnnotation());
                    e.Handled = true;
                    return;
                default:
                    return;
            }

            e.Handled = true;
            RenderAnnotations();
        }

        private bool MoveCursor(int direction, bool select)
        {
            int index = _cursor != null ? _annotations.IndexOf(_cursor) : -1;
            int next = index + direction;
            if (next < 0 || next >= _annotations.Count)
            {
                return false;
            }

            _cursor = _annotations[next];
            if (select)
            {
                SetSelection(_cursor);
            }

            return true;
        }

        private void MoveSelection(Vector offset)
        {
            _moveSnapshots = _selection.Select(a => new MoveSnapshot(a)).ToList();
            MoveAnnotation(offset);
        }

        private void ResizeSelection(double dx, double dy)
        {
            foreach (Annotation annotation in _selection)
            {
                bool isLine = annotation.Kind == AnnotationKind.Line || annotation.Kind == AnnotationKind.Arrow;
                double minimum = isLine ? 0 : 1;
                Rect b = annotation.Bounds;
                UpdateAnnotationBounds(annotation, new Rect(
                    b.X,
                    b.Y,
                    Math.Max(minimum, b.Width + dx),
                    Math.Max(minimum, b.Height + dy)));
            }
        }
        private void UndockButton_Click(object sender, RoutedEventArgs e)
        {
            if (_floatingPropertiesWindow != null)
            {
                _floatingPropertiesWindow.Close();
                return;
            }

            PropertiesDockHost.Child = null;
            var window = new Window
            {
                Title = "Properties",
                Owner = this,
                Width = 320,
                Height = 420,
                ShowInTaskbar = false,
                Background = Brushes.WhiteSmoke,
                Content = new Border { Padding = new Thickness(12), Child = PropertiesPanel }
            };
            window.Closed += (s, args) =>
            {
                ((Border)window.Content).Child = null;
                PropertiesDockHost.Child = PropertiesPanel;
                PropertiesDockHost.Visibility = Visibility.Visible;
                DockColumn.Width = new GridLength(300);
                UndockButton.Content = "Undock";
                _floatingPropertiesWindow = null;
            };
            _floatingPropertiesWindow = window;
            UndockButton.Content = "Dock";
            DockColumn.Width = new GridLength(0);
            PropertiesDockHost.Visibility = Visibility.Collapsed;
            window.Show();
        }

        private void SelectSelectionTool()
        {
            _selectedTool = null;
            UpdateToolButtons(SelectToolButton);
        }

        private void ShortcutsButton_Click(object sender, RoutedEventArgs e)
        {
            ShortcutsPopup.IsOpen = !ShortcutsPopup.IsOpen;
        }

        private void ModeSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ModeSelector.SelectedItem is ComboBoxItem item &&
                Enum.TryParse(item.Tag as string, out DrawingMode mode))
            {
                _drawingMode = mode;
                if (_drawingMode == DrawingMode.ClickToAdd && ToolsPanel != null && SelectToolButton != null)
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

        private const string MixedValue = "\u2260";

        private bool IsEdited(TextBox box)
        {
            return box.Text != MixedValue && box.Text != (box.Tag as string);
        }

        private void ApplyGeometryProperties()
        {
            if (_isRefreshingProperties || _selection.Count == 0)
            {
                return;
            }

            bool editX = IsEdited(PositionXTextBox);
            bool editY = IsEdited(PositionYTextBox);
            bool editWidth = IsEdited(WidthTextBox);
            bool editHeight = IsEdited(HeightTextBox);
            if (!editX && !editY && !editWidth && !editHeight)
            {
                RefreshPropertyEditor();
                return;
            }

            int x = 0, y = 0, width = 0, height = 0;
            bool valid =
                (!editX || int.TryParse(PositionXTextBox.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out x)) &&
                (!editY || int.TryParse(PositionYTextBox.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out y)) &&
                (!editWidth || int.TryParse(WidthTextBox.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out width)) &&
                (!editHeight || int.TryParse(HeightTextBox.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out height));
            if (valid)
            {
                foreach (Annotation annotation in _selection)
                {
                    bool isLine = annotation.Kind == AnnotationKind.Line || annotation.Kind == AnnotationKind.Arrow;
                    int minimum = isLine ? 0 : 1;
                    if ((editWidth && width < minimum) || (editHeight && height < minimum))
                    {
                        valid = false;
                    }
                }
            }

            if (!valid)
            {
                StatusText.Text = "Enter whole numbers; width and height must be positive (zero is allowed for lines).";
                RefreshPropertyEditor();
                return;
            }

            foreach (Annotation annotation in _selection)
            {
                Rect b = annotation.Bounds;
                UpdateAnnotationBounds(annotation, new Rect(
                    editX ? x : b.X,
                    editY ? y : b.Y,
                    editWidth ? width : b.Width,
                    editHeight ? height : b.Height));
            }

            RenderAnnotations();
        }

        private void LineColorPicker_ColorPicked(object sender, Color color)
        {
            _drawLineColor = color;
            foreach (Annotation annotation in _selection)
            {
                annotation.LineColor = color;
            }

            RenderAnnotations();
        }

        private void FillColorPicker_ColorPicked(object sender, Color color)
        {
            _drawFillColor = color;
            foreach (Annotation annotation in _selection)
            {
                annotation.FillColor = color;
            }

            RenderAnnotations();
        }

        private void TextColorPicker_ColorPicked(object sender, Color color)
        {
            RichTextBox editor = GetActiveTextEditor();
            if (editor == null)
            {
                return;
            }

            TextRange range = GetTextFormattingRange(editor);
            range.ApplyPropertyValue(TextElement.ForegroundProperty, new SolidColorBrush(color));
            GetSelectedTextAnnotation().TextColor = color;

            RefreshTextProperties();
        }

        private void TextFontFamily_LostFocus(object sender, RoutedEventArgs e)
        {
            if (TextFontFamilyComboBox.Text != (TextFontFamilyComboBox.Tag as string))
            {
                ApplyFontFamily();
            }
        }

        private void TextFontSize_LostFocus(object sender, RoutedEventArgs e)
        {
            if (TextFontSizeTextBox.Text != (TextFontSizeTextBox.Tag as string))
            {
                ApplyFontSize();
            }
        }

        private void TextProperty_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                if (sender == TextFontFamilyComboBox)
                {
                    ApplyFontFamily();
                }
                else
                {
                    ApplyFontSize();
                }

                e.Handled = true;
            }
        }

        private void ApplyFontFamily()
        {
            RichTextBox editor = GetActiveTextEditor();
            string familyName = TextFontFamilyComboBox.Text.Trim();
            if (editor == null || string.IsNullOrEmpty(familyName) ||
                familyName == MixedValue || familyName == (TextFontFamilyComboBox.Tag as string) ||
                _isRefreshingProperties)
            {
                return;
            }

            try
            {
                var fontFamily = new FontFamily(familyName);
                TextRange range = GetTextFormattingRange(editor);
                range.ApplyPropertyValue(TextElement.FontFamilyProperty, fontFamily);
                if (range.Start == editor.Document.ContentStart && range.End == editor.Document.ContentEnd)
                {
                    GetSelectedTextAnnotation().FontFamily = familyName;
                }

                RefreshTextProperties();
            }
            catch (ArgumentException)
            {
                StatusText.Text = "Enter a valid font family.";
                RefreshTextProperties();
            }
        }

        private void ApplyFontSize()
        {
            RichTextBox editor = GetActiveTextEditor();
            if (editor == null || TextFontSizeTextBox.Text == (TextFontSizeTextBox.Tag as string) ||
                _isRefreshingProperties)
            {
                return;
            }

            if (!double.TryParse(TextFontSizeTextBox.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out double size) ||
                double.IsNaN(size) || double.IsInfinity(size) || size <= 0)
            {
                StatusText.Text = "Enter a finite font size greater than zero.";
                RefreshTextProperties();
                return;
            }

            TextRange range = GetTextFormattingRange(editor);
            range.ApplyPropertyValue(TextElement.FontSizeProperty, size);
            if (range.Start == editor.Document.ContentStart && range.End == editor.Document.ContentEnd)
            {
                GetSelectedTextAnnotation().FontSize = size;
            }

            RefreshTextProperties();
        }

        private void TextStyleButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isRefreshingProperties || !(sender is ToggleButton button))
            {
                return;
            }

            RichTextBox editor = GetActiveTextEditor();
            if (editor == null)
            {
                return;
            }

            TextRange range = GetTextFormattingRange(editor);
            switch (button.Tag as string)
            {
                case "Bold":
                    range.ApplyPropertyValue(TextElement.FontWeightProperty,
                        button.IsChecked == true ? FontWeights.Bold : FontWeights.Normal);
                    break;
                case "Italic":
                    range.ApplyPropertyValue(TextElement.FontStyleProperty,
                        button.IsChecked == true ? FontStyles.Italic : FontStyles.Normal);
                    break;
                case "Underline":
                    range.ApplyPropertyValue(Inline.TextDecorationsProperty,
                        button.IsChecked == true ? TextDecorations.Underline : null);
                    break;
            }

            RefreshTextProperties();
        }

        private void TextAlignmentButton_Click(object sender, RoutedEventArgs e)
        {
            var button = (ToggleButton)sender;
            Annotation annotation = GetSelectedTextAnnotation();
            RichTextBox editor = GetActiveTextEditor();
            if (_isRefreshingProperties || annotation == null || editor == null)
            {
                return;
            }

            string tag = (string)button.Tag;
            if (ReferenceEquals(button, AlignTopButton) || ReferenceEquals(button, AlignMiddleButton) || ReferenceEquals(button, AlignBottomButton))
            {
                annotation.TextVerticalAlignment = (VerticalAlignment)Enum.Parse(typeof(VerticalAlignment), tag);
                RenderAnnotations();
            }
            else
            {
                TextAlignment alignment = (TextAlignment)Enum.Parse(typeof(TextAlignment), tag);
                GetTextFormattingRange(editor).ApplyPropertyValue(Paragraph.TextAlignmentProperty, alignment);
                annotation.TextAlignment = alignment;
                RefreshTextProperties();
            }
        }
        private Annotation GetSelectedTextAnnotation()
        {
            return _selection.Count == 1 && Annotation.SupportsText(_selection[0].Kind)
                ? _selection[0]
                : null;
        }

        private RichTextBox GetActiveTextEditor()
        {
            Annotation annotation = GetSelectedTextAnnotation();
            return annotation != null ? GetTextEditor(annotation) : null;
        }

        private void CanvasHost_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            Annotation annotation = GetSelectedTextAnnotation();
            if (e.OriginalSource != CanvasHost || annotation == null || _editingText != null ||
                string.IsNullOrEmpty(e.Text) || char.IsControl(e.Text[0]))
            {
                return;
            }

            StartTextEditing(annotation);
            RichTextBox editor = GetTextEditor(annotation);
            editor.AppendText(e.Text);
            editor.CaretPosition = editor.Document.ContentEnd;
            e.Handled = true;
        }

        private static bool HasText(Annotation annotation)
        {
            return annotation.TextDocument != null &&
                   !string.IsNullOrWhiteSpace(new TextRange(annotation.TextDocument.ContentStart, annotation.TextDocument.ContentEnd).Text);
        }

        private TextRange GetTextFormattingRange(RichTextBox editor)
        {
            return _editingText != null && _editingText == GetSelectedTextAnnotation() && !editor.Selection.IsEmpty
                ? new TextRange(editor.Selection.Start, editor.Selection.End)
                : new TextRange(editor.Document.ContentStart, editor.Document.ContentEnd);
        }

        private void RefreshTextProperties()
        {
            Annotation annotation = GetSelectedTextAnnotation();
            TextPropertiesExpander.IsEnabled = annotation != null;
            if (annotation == null)
            {
                return;
            }

            RichTextBox editor = GetTextEditor(annotation);
            _isRefreshingProperties = true;
            TextRange range = GetTextFormattingRange(editor);
            object familyValue = range.GetPropertyValue(TextElement.FontFamilyProperty);
            TextFontFamilyComboBox.Text = familyValue is FontFamily family
                ? family.Source
                : "\u2260";
            TextFontFamilyComboBox.Tag = TextFontFamilyComboBox.Text;
            object sizeValue = range.GetPropertyValue(TextElement.FontSizeProperty);
            TextFontSizeTextBox.Text = sizeValue is double size
                ? size.ToString("0.##", CultureInfo.InvariantCulture)
                : "\u2260";
            TextFontSizeTextBox.Tag = TextFontSizeTextBox.Text;
            object colorValue = range.GetPropertyValue(TextElement.ForegroundProperty);
            Color textColor = annotation.TextColor;
            bool mixedColor = true;
            if (colorValue is SolidColorBrush brush)
            {
                textColor = brush.Color;
                mixedColor = false;
            }
            TextColorPicker.SetColor(textColor, mixedColor);

            object weightValue = range.GetPropertyValue(TextElement.FontWeightProperty);
            BoldTextButton.IsChecked = weightValue == DependencyProperty.UnsetValue
                ? (bool?)null
                : (FontWeight)weightValue == FontWeights.Bold;
            object styleValue = range.GetPropertyValue(TextElement.FontStyleProperty);
            ItalicTextButton.IsChecked = styleValue == DependencyProperty.UnsetValue
                ? (bool?)null
                : (FontStyle)styleValue == FontStyles.Italic;
            object decorationValue = range.GetPropertyValue(Inline.TextDecorationsProperty);
            UnderlineTextButton.IsChecked = decorationValue == DependencyProperty.UnsetValue
                ? (bool?)null
                : decorationValue is TextDecorationCollection decorations && decorations.Count > 0;

            object alignmentValue = range.GetPropertyValue(Paragraph.TextAlignmentProperty);
            TextAlignment? currentAlignment = alignmentValue is TextAlignment value ? value : (TextAlignment?)null;
            AlignLeftButton.IsChecked = currentAlignment == TextAlignment.Left;
            AlignCenterButton.IsChecked = currentAlignment == TextAlignment.Center;
            AlignRightButton.IsChecked = currentAlignment == TextAlignment.Right;
            AlignTopButton.IsChecked = annotation.TextVerticalAlignment == VerticalAlignment.Top;
            AlignMiddleButton.IsChecked = annotation.TextVerticalAlignment == VerticalAlignment.Center;
            AlignBottomButton.IsChecked = annotation.TextVerticalAlignment == VerticalAlignment.Bottom;            _isRefreshingProperties = false;
        }
        private void LineThicknessProperty_LostFocus(object sender, RoutedEventArgs e)
        {
            if (_isRefreshingProperties || _selection.Count == 0 || !IsEdited(LineThicknessTextBox))
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

            foreach (Annotation annotation in _selection)
            {
                annotation.LineThickness = thickness;
            }

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

        private void SetSelection(Annotation annotation)
        {
            _selection.Clear();
            if (annotation != null)
            {
                _selection.Add(annotation);
            }
        }

        private void SetPropertyText(TextBox box, Func<Annotation, string> selector)
        {
            string first = selector(_selection[0]);
            bool same = true;
            foreach (Annotation annotation in _selection)
            {
                if (selector(annotation) != first)
                {
                    same = false;
                    break;
                }
            }

            string text = same ? first : MixedValue;
            box.Text = text;
            box.Tag = text;
        }

        private void SetColorPicker(ColorPickerField picker, Func<Annotation, Color> selector, Color fallback)
        {
            if (_selection.Count == 0)
            {
                picker.SetColor(fallback, false);
                return;
            }

            Color first = selector(_selection[0]);
            bool same = _selection.All(a => selector(a) == first);
            picker.SetColor(first, !same);
        }

        private void RefreshPropertyEditor()
        {
            bool hasSelection = _selection.Count > 0;
            NoSelectionText.Visibility = hasSelection ? Visibility.Collapsed : Visibility.Visible;
            LayoutExpander.IsEnabled = hasSelection;
            LineThicknessTextBox.IsEnabled = hasSelection;
            _isRefreshingProperties = true;
            SetColorPicker(LineColorPicker, a => a.LineColor, _drawLineColor);
            SetColorPicker(FillColorPicker, a => a.FillColor, _drawFillColor);
            RefreshTextProperties();
            if (hasSelection)
            {
                SetPropertyText(PositionXTextBox, a => Math.Round(a.Bounds.X).ToString("0", CultureInfo.InvariantCulture));
                SetPropertyText(PositionYTextBox, a => Math.Round(a.Bounds.Y).ToString("0", CultureInfo.InvariantCulture));
                SetPropertyText(WidthTextBox, a => Math.Round(a.Bounds.Width).ToString("0", CultureInfo.InvariantCulture));
                SetPropertyText(HeightTextBox, a => Math.Round(a.Bounds.Height).ToString("0", CultureInfo.InvariantCulture));
                SetPropertyText(LineThicknessTextBox, a => a.LineThickness.ToString("0.##", CultureInfo.InvariantCulture));
            }

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
            if (_editingText != null &&
                IsVisualDescendantOf(e.OriginalSource as DependencyObject, _textEditors[_editingText]))
            {
                return;
            }

            CanvasHost.Focus();
            Point point = e.GetPosition(AnnotationCanvas);
            if (_selectedTool == null)
            {
                Annotation hitAnnotation = FindAnnotationAt(point);
                bool shift = (Keyboard.Modifiers & ModifierKeys.Shift) != 0;
                if (!shift && _selection.Count == 1)
                {
                    ResizeHandle selectedHandle = GetResizeHandleAt(_selection[0], point);
                    if (selectedHandle != ResizeHandle.None)
                    {
                        BeginManipulation(_selection[0], point, PointerOperation.Resize, selectedHandle);
                        RenderAnnotations();
                        e.Handled = true;
                        return;
                    }
                }

                if (hitAnnotation == null)
                {
                    if (shift)
                    {
                        return;
                    }

                    _selection.Clear();
                    _activeAnnotation = null;
                    _pointerOperation = PointerOperation.None;
                    RenderAnnotations();
                    return;
                }

                if (Annotation.SupportsText(hitAnnotation.Kind) && e.ClickCount >= 2)
                {
                    StartTextEditing(hitAnnotation);
                    e.Handled = true;
                    return;
                }

                if (shift)
                {
                    if (!_selection.Remove(hitAnnotation))
                    {
                        _selection.Add(hitAnnotation);
                    }

                    _activeAnnotation = null;
                    _pointerOperation = PointerOperation.None;
                    RenderAnnotations();
                    e.Handled = true;
                    return;
                }

                if (!_selection.Contains(hitAnnotation))
                {
                    SetSelection(hitAnnotation);
                }

                ResizeHandle handle = _selection.Count == 1 ? GetResizeHandleAt(hitAnnotation, point) : ResizeHandle.None;
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
                LineColor = _drawLineColor,
                FillColor = _drawFillColor,
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
            SetSelection(annotation);
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
            _moveSnapshots = new List<MoveSnapshot>();
            foreach (Annotation selected in _selection)
            {
                _moveSnapshots.Add(new MoveSnapshot(selected));
            }

            AnnotationCanvas.CaptureMouse();
        }

        private sealed class MoveSnapshot
        {
            public MoveSnapshot(Annotation annotation)
            {
                Annotation = annotation;
                Bounds = annotation.Bounds;
                Start = annotation.Start;
                End = annotation.End;
                Points = new List<Point>(annotation.Points);
            }

            public Annotation Annotation { get; }

            public Rect Bounds { get; }

            public Point Start { get; }

            public Point End { get; }

            public List<Point> Points { get; }
        }

        private void MoveAnnotation(Vector offset)
        {
            foreach (MoveSnapshot snapshot in _moveSnapshots)
            {
                Annotation annotation = snapshot.Annotation;
                annotation.Bounds = Offset(snapshot.Bounds, offset);
                annotation.Start = snapshot.Start + offset;
                annotation.End = snapshot.End + offset;
                annotation.Points.Clear();
                foreach (Point point in snapshot.Points)
                {
                    annotation.Points.Add(point + offset);
                }
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

            foreach (KeyValuePair<ResizeHandle, Point> handle in GetHandlePoints(annotation.Bounds))
            {
                if (Distance(point, handle.Value) <= 10)
                {
                    return handle.Key;
                }
            }

            return ResizeHandle.None;
        }

        private static List<KeyValuePair<ResizeHandle, Point>> GetHandlePoints(Rect bounds)
        {
            double midX = bounds.Left + bounds.Width / 2;
            double midY = bounds.Top + bounds.Height / 2;
            return new List<KeyValuePair<ResizeHandle, Point>>
            {
                new KeyValuePair<ResizeHandle, Point>(ResizeHandle.TopLeft, new Point(bounds.Left, bounds.Top)),
                new KeyValuePair<ResizeHandle, Point>(ResizeHandle.TopRight, new Point(bounds.Right, bounds.Top)),
                new KeyValuePair<ResizeHandle, Point>(ResizeHandle.BottomLeft, new Point(bounds.Left, bounds.Bottom)),
                new KeyValuePair<ResizeHandle, Point>(ResizeHandle.BottomRight, new Point(bounds.Right, bounds.Bottom)),
                new KeyValuePair<ResizeHandle, Point>(ResizeHandle.Top, new Point(midX, bounds.Top)),
                new KeyValuePair<ResizeHandle, Point>(ResizeHandle.Bottom, new Point(midX, bounds.Bottom)),
                new KeyValuePair<ResizeHandle, Point>(ResizeHandle.Left, new Point(bounds.Left, midY)),
                new KeyValuePair<ResizeHandle, Point>(ResizeHandle.Right, new Point(bounds.Right, midY))
            };
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

        private RichTextBox GetTextEditor(Annotation annotation)
        {
            if (_textEditors.TryGetValue(annotation, out RichTextBox existing))
            {
                return existing;
            }

            var editor = new RichTextBox
            {
                Document = annotation.TextDocument,
                BorderThickness = new Thickness(0),
                Padding = new Thickness(0),
                Background = Brushes.Transparent,
                IsReadOnly = true,
                IsHitTestVisible = false,
                Focusable = false,
                AcceptsTab = true,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                VerticalScrollBarVisibility = ScrollBarVisibility.Disabled,
                FontFamily = new FontFamily(annotation.FontFamily),
                FontSize = annotation.FontSize,
                Foreground = new SolidColorBrush(annotation.TextColor),
                VerticalContentAlignment = annotation.TextVerticalAlignment
            };
            annotation.TextDocument.FontFamily = new FontFamily(annotation.FontFamily);
            annotation.TextDocument.FontSize = annotation.FontSize;
            var defaultTextRange = new TextRange(editor.Document.ContentStart, editor.Document.ContentEnd);
            defaultTextRange.ApplyPropertyValue(TextElement.ForegroundProperty, new SolidColorBrush(annotation.TextColor));
            defaultTextRange.ApplyPropertyValue(TextElement.FontWeightProperty, FontWeights.Normal);
            defaultTextRange.ApplyPropertyValue(TextElement.FontStyleProperty, FontStyles.Normal);
            editor.TextChanged += (sender, args) =>
            {
                annotation.Text = new TextRange(editor.Document.ContentStart, editor.Document.ContentEnd).Text.TrimEnd('\r', '\n');
            };
            editor.SelectionChanged += (sender, args) =>
            {
                if (annotation == GetSelectedTextAnnotation())
                {
                    RefreshTextProperties();
                }
            };
            editor.PreviewKeyDown += TextEditor_PreviewKeyDown;
            _textEditors.Add(annotation, editor);
            foreach (Paragraph paragraph in annotation.TextDocument.Blocks.OfType<Paragraph>())
            {
                paragraph.TextAlignment = annotation.TextAlignment;
            }

            return editor;
        }

        private void AddTextEditor(Annotation annotation)
        {
            RichTextBox editor = GetTextEditor(annotation);
            editor.FontFamily = new FontFamily(annotation.FontFamily);
            editor.FontSize = annotation.FontSize;
            editor.Foreground = new SolidColorBrush(annotation.TextColor);
            if (!annotation.HasCustomTextColor)
            {
                var linked = new TextRange(editor.Document.ContentStart, editor.Document.ContentEnd);
                if (!(linked.GetPropertyValue(TextElement.ForegroundProperty) is SolidColorBrush current) || current.Color != annotation.LineColor)
                {
                    linked.ApplyPropertyValue(TextElement.ForegroundProperty, new SolidColorBrush(annotation.LineColor));
                }
            }

            editor.IsReadOnly = _editingText != annotation;
            editor.IsHitTestVisible = _editingText == annotation;
            editor.Focusable = _editingText == annotation;
            editor.Padding = new Thickness(0);
            editor.Width = double.NaN;
            editor.Height = double.NaN;
            editor.HorizontalAlignment = HorizontalAlignment.Stretch;
            editor.VerticalAlignment = annotation.TextVerticalAlignment;

            // The RichTextBox ignores VerticalContentAlignment, so it sits in a host grid and is aligned as a whole.
            if (editor.Parent is Panel oldHost)
            {
                oldHost.Children.Remove(editor);
            }

            var host = new Grid { IsHitTestVisible = false };
            host.Children.Add(editor);

            switch (annotation.Kind)
            {
                case AnnotationKind.Rectangle:
                    editor.Padding = new Thickness(6);
                    AddBoundedShape(host, annotation.Bounds);
                    break;
                case AnnotationKind.Ellipse:
                    Rect b = annotation.Bounds;
                    AddBoundedShape(host, new Rect(b.X + b.Width * 0.146, b.Y + b.Height * 0.146, b.Width * 0.708, b.Height * 0.708));
                    break;
                case AnnotationKind.Line:
                case AnnotationKind.Arrow:
                    editor.VerticalAlignment = VerticalAlignment.Bottom;
                    AddLineTextHost(annotation, host);
                    break;
                default:
                    AddBoundedShape(host, annotation.Bounds);
                    break;
            }
        }

        private void AddLineTextHost(Annotation annotation, Grid host)
        {
            Point p0 = annotation.Start;
            Point p1 = annotation.End;
            if (p1.X < p0.X)
            {
                Point swap = p0;
                p0 = p1;
                p1 = swap;
            }

            double dx = p1.X - p0.X;
            double dy = p1.Y - p0.Y;
            double length = Math.Max(Math.Sqrt(dx * dx + dy * dy), 20);
            double height = Math.Max(40, annotation.FontSize * 3);
            double offset = annotation.LineThickness / 2 + 2;
            host.Width = length;
            host.Height = height;
            host.RenderTransform = new RotateTransform(Math.Atan2(dy, dx) * 180 / Math.PI, 0, height + offset);
            Canvas.SetLeft(host, p0.X);
            Canvas.SetTop(host, p0.Y - height - offset);
            AnnotationCanvas.Children.Add(host);
        }
        private void StartTextEditing(Annotation annotation)
        {
            SetSelection(annotation);
            _editingText = annotation;
            RenderAnnotations();
            AnnotationCanvas.UpdateLayout();
            RichTextBox editor = GetTextEditor(annotation);
            Keyboard.Focus(editor);
            editor.CaretPosition = editor.Document.ContentEnd;
            RefreshPropertyEditor();
        }

        private void EndTextEditing()
        {
            if (_editingText == null)
            {
                return;
            }

            _editingText = null;
            RenderAnnotations();
        }

        private void TextEditor_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                EndTextEditing();
                CanvasHost.Focus();
                e.Handled = true;
            }
            else if ((Keyboard.Modifiers & ModifierKeys.Control) != 0 &&
                     (e.Key == Key.B || e.Key == Key.I || e.Key == Key.U))
            {
                var editor = (RichTextBox)sender;
                TextRange range = GetTextFormattingRange(editor);
                if (e.Key == Key.B)
                {
                    object value = range.GetPropertyValue(TextElement.FontWeightProperty);
                    range.ApplyPropertyValue(TextElement.FontWeightProperty,
                        value == DependencyProperty.UnsetValue || (FontWeight)value != FontWeights.Bold
                            ? FontWeights.Bold
                            : FontWeights.Normal);
                }
                else if (e.Key == Key.I)
                {
                    object value = range.GetPropertyValue(TextElement.FontStyleProperty);
                    range.ApplyPropertyValue(TextElement.FontStyleProperty,
                        value == DependencyProperty.UnsetValue || (FontStyle)value != FontStyles.Italic
                            ? FontStyles.Italic
                            : FontStyles.Normal);
                }
                else
                {
                    object value = range.GetPropertyValue(Inline.TextDecorationsProperty);
                    range.ApplyPropertyValue(Inline.TextDecorationsProperty,
                        value is TextDecorationCollection decorations && decorations.Count > 0
                            ? null
                            : TextDecorations.Underline);
                }

                RefreshTextProperties();
                e.Handled = true;
            }
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

                if (Annotation.SupportsText(annotation.Kind) &&
                    (annotation.Kind == AnnotationKind.Text || _editingText == annotation || HasText(annotation)))
                {
                    AddTextEditor(annotation);
                }
            }

            foreach (Annotation selected in _selection)
            {
                RenderSelectionHandles(selected);
            }

            RenderKeyboardCursor();
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

            foreach (KeyValuePair<ResizeHandle, Point> handle in GetHandlePoints(bounds))
            {
                AddSelectionHandle(handle.Value);
            }
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
                case ResizeHandle.Top:
                    top = Math.Min(point.Y, bottom - 1);
                    break;
                case ResizeHandle.Bottom:
                    bottom = Math.Max(point.Y, top + 1);
                    break;
                case ResizeHandle.Left:
                    left = Math.Min(point.X, right - 1);
                    break;
                case ResizeHandle.Right:
                    right = Math.Max(point.X, left + 1);
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

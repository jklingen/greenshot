using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Greenshot.Editor.Mockup
{
    public partial class ColorPickerField : UserControl
    {
        private const int MaxRecentColors = 10;

        private readonly ObservableCollection<SolidColorBrush> _recentColors = new ObservableCollection<SolidColorBrush>();

        private Color _color = Colors.Red;
        private bool _changedInPicker;
        private double _hue;
        private double _saturation = 1;
        private double _value = 1;
        private byte _alpha = 255;

        public ColorPickerField()
        {
            InitializeComponent();
            RecentList.ItemsSource = _recentColors;
            UseDefaultColors(255);
            UpdateVisuals();
        }

        public void UseDefaultColors(byte alpha)
        {
            _recentColors.Clear();
            foreach (string hex in new[] { "#E53935", "#FB8C00", "#FDD835", "#43A047", "#1E88E5", "#8E24AA", "#000000", "#FFFFFF" })
            {
                Color color = (Color)ColorConverter.ConvertFromString(hex);
                var brush = new SolidColorBrush(Color.FromArgb(alpha, color.R, color.G, color.B));
                brush.Freeze();
                _recentColors.Add(brush);
            }
        }

        public event EventHandler<Color> ColorPicked;

        public Color Color => _color;

        public void SetColor(Color color, bool isMixed)
        {
            _color = color;
            MixedMark.Visibility = isMixed ? Visibility.Visible : Visibility.Collapsed;
            SwatchFill.Fill = isMixed ? Brushes.LightGray : new SolidColorBrush(color);
            if (!PickerPopup.IsOpen)
            {
                UpdateVisuals();
            }
        }

        private void AddRecent(Color color)
        {
            for (int i = _recentColors.Count - 1; i >= 0; i--)
            {
                if (_recentColors[i].Color == color)
                {
                    _recentColors.RemoveAt(i);
                }
            }

            var brush = new SolidColorBrush(color);
            brush.Freeze();
            _recentColors.Insert(0, brush);
            while (_recentColors.Count > MaxRecentColors)
            {
                _recentColors.RemoveAt(_recentColors.Count - 1);
            }
        }

        private void UpdateVisuals()
        {
            SetHsvFromColor(_color);
            RefreshPicker();
        }

        private void SetHsvFromColor(Color color)
        {
            double r = color.R / 255.0, g = color.G / 255.0, b = color.B / 255.0;
            double max = Math.Max(r, Math.Max(g, b));
            double min = Math.Min(r, Math.Min(g, b));
            double delta = max - min;
            _alpha = color.A;
            _value = max;
            _saturation = max <= 0 ? 0 : delta / max;
            if (delta > 0)
            {
                double hue;
                if (max == r)
                {
                    hue = 60 * (((g - b) / delta + 6) % 6);
                }
                else if (max == g)
                {
                    hue = 60 * ((b - r) / delta + 2);
                }
                else
                {
                    hue = 60 * ((r - g) / delta + 4);
                }

                _hue = hue;
            }
        }

        private static Color FromHsv(double hue, double saturation, double value, byte alpha)
        {
            double c = value * saturation;
            double h = (hue % 360) / 60;
            double x = c * (1 - Math.Abs(h % 2 - 1));
            double m = value - c;
            double r = 0, g = 0, b = 0;
            if (h < 1) { r = c; g = x; }
            else if (h < 2) { r = x; g = c; }
            else if (h < 3) { g = c; b = x; }
            else if (h < 4) { g = x; b = c; }
            else if (h < 5) { r = x; b = c; }
            else { r = c; b = x; }

            return Color.FromArgb(alpha, (byte)Math.Round((r + m) * 255), (byte)Math.Round((g + m) * 255), (byte)Math.Round((b + m) * 255));
        }

        private void RefreshPicker()
        {
            Color hueColor = FromHsv(_hue, 1, 1, 255);
            Color opaque = Color.FromRgb(_color.R, _color.G, _color.B);
            SvArea.Background = new SolidColorBrush(hueColor);
            Canvas.SetLeft(SvMarker, _saturation * SvArea.Width - SvMarker.Width / 2);
            Canvas.SetTop(SvMarker, (1 - _value) * SvArea.Height - SvMarker.Height / 2);
            Canvas.SetLeft(HueMarker, _hue / 360 * HueBar.Width - HueMarker.Width / 2);
            Canvas.SetLeft(AlphaMarker, _alpha / 255.0 * AlphaBar.Width - AlphaMarker.Width / 2);
            AlphaGradient.Fill = new LinearGradientBrush(Color.FromArgb(0, opaque.R, opaque.G, opaque.B), opaque, 0);
            PreviewRect.Fill = new SolidColorBrush(_color);
            AlphaText.Text = string.Format(CultureInfo.InvariantCulture, "Opacity {0}%", Math.Round(_alpha / 255.0 * 100));
            HexTextBox.Text = _color.ToString();
        }

        private void PickFromHsv()
        {
            _changedInPicker = true;
            _color = FromHsv(_hue, _saturation, _value, _alpha);
            MixedMark.Visibility = Visibility.Collapsed;
            SwatchFill.Fill = new SolidColorBrush(_color);
            RefreshPicker();
            ColorPicked?.Invoke(this, _color);
        }

        private static double Clamp01(double value)
        {
            return Math.Max(0, Math.Min(1, value));
        }

        private void SvArea_MouseDown(object sender, MouseButtonEventArgs e)
        {
            SvArea.CaptureMouse();
            UpdateSv(e);
        }

        private void SvArea_MouseMove(object sender, MouseEventArgs e)
        {
            if (SvArea.IsMouseCaptured)
            {
                UpdateSv(e);
            }
        }

        private void UpdateSv(MouseEventArgs e)
        {
            Point p = e.GetPosition(SvArea);
            _saturation = Clamp01(p.X / SvArea.Width);
            _value = 1 - Clamp01(p.Y / SvArea.Height);
            PickFromHsv();
        }

        private void HueBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            HueBar.CaptureMouse();
            UpdateHue(e);
        }

        private void HueBar_MouseMove(object sender, MouseEventArgs e)
        {
            if (HueBar.IsMouseCaptured)
            {
                UpdateHue(e);
            }
        }

        private void UpdateHue(MouseEventArgs e)
        {
            _hue = Clamp01(e.GetPosition(HueBar).X / HueBar.Width) * 360;
            PickFromHsv();
        }

        private void AlphaBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            AlphaBar.CaptureMouse();
            UpdateAlpha(e);
        }

        private void AlphaBar_MouseMove(object sender, MouseEventArgs e)
        {
            if (AlphaBar.IsMouseCaptured)
            {
                UpdateAlpha(e);
            }
        }

        private void UpdateAlpha(MouseEventArgs e)
        {
            _alpha = (byte)Math.Round(Clamp01(e.GetPosition(AlphaBar).X / AlphaBar.Width) * 255);
            PickFromHsv();
        }

        private void Area_MouseUp(object sender, MouseButtonEventArgs e)
        {
            ((UIElement)sender).ReleaseMouseCapture();
        }
        private void Pick(Color color, bool addToRecent)
        {
            _color = color;
            MixedMark.Visibility = Visibility.Collapsed;
            SwatchFill.Fill = new SolidColorBrush(color);
            UpdateVisuals();
            if (addToRecent)
            {
                AddRecent(color);
            }

            ColorPicked?.Invoke(this, color);
        }

        private void SwatchButton_Click(object sender, RoutedEventArgs e)
        {
            _changedInPicker = false;
            UpdateVisuals();
            PickerPopup.IsOpen = true;
            HexTextBox.Focus();
            HexTextBox.SelectAll();
        }

        private void PickerPopup_Closed(object sender, EventArgs e)
        {
            if (_changedInPicker)
            {
                AddRecent(_color);
                _changedInPicker = false;
            }
        }

        private void HexTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                ApplyHex();
                e.Handled = true;
            }
            else if (e.Key == Key.Escape)
            {
                PickerPopup.IsOpen = false;
                SwatchButton.Focus();
                e.Handled = true;
            }
        }

        private void HexTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (PickerPopup.IsOpen)
            {
                ApplyHex();
            }
        }

        private void ApplyHex()
        {
            try
            {
                if (ColorConverter.ConvertFromString(HexTextBox.Text) is Color parsed)
                {
                    if (parsed != _color)
                    {
                        _changedInPicker = true;
                        Pick(parsed, false);
                    }

                    return;
                }
            }
            catch (FormatException)
            {
            }
            catch (NotSupportedException)
            {
            }

            HexTextBox.Text = _color.ToString();
        }

        private void RecentColor_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is SolidColorBrush brush)
            {
                Pick(brush.Color, true);
            }
        }
    }
}
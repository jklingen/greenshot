using System.Collections.Generic;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace Greenshot.Editor.Mockup
{
    internal enum AnnotationKind
    {
        Rectangle,
        Ellipse,
        Line,
        Arrow,
        Text,
        Freehand
    }

    internal sealed class Annotation
    {
        public Annotation(AnnotationKind kind)
        {
            Kind = kind;
            if (SupportsText(kind))
            {
                bool isText = kind == AnnotationKind.Text;
                Text = isText ? "Sample text" : string.Empty;
                TextDocument = new FlowDocument(new Paragraph(isText ? new Run("Sample text") : new Run()));
                TextAlignment = isText ? TextAlignment.Left : TextAlignment.Center;
                TextVerticalAlignment = isText ? VerticalAlignment.Top : kind == AnnotationKind.Rectangle || kind == AnnotationKind.Ellipse ? VerticalAlignment.Center : VerticalAlignment.Bottom;
                TextDocument.PagePadding = new Thickness(0);
                TextDocument.Blocks.FirstBlock.Margin = new Thickness(0);
            }
        }

        public static bool SupportsText(AnnotationKind kind)
        {
            return kind != AnnotationKind.Freehand;
        }

        private Color _textColor;

        public AnnotationKind Kind { get; }

        public Point Start { get; set; }

        public Point End { get; set; }

        public Rect Bounds { get; set; }

        public List<Point> Points { get; } = new List<Point>();

        public string Text { get; set; } = string.Empty;

        public FlowDocument TextDocument { get; }

        public string FontFamily { get; set; } = "Segoe UI";

        public double FontSize { get; set; } = 28;

        public bool HasCustomTextColor { get; private set; }

        public Color TextColor
        {
            get => HasCustomTextColor ? _textColor : LineColor;
            set
            {
                _textColor = value;
                HasCustomTextColor = true;
            }
        }

        public TextAlignment TextAlignment { get; set; }

        public VerticalAlignment TextVerticalAlignment { get; set; }

        public Color LineColor { get; set; } = Colors.Red;

        public Color FillColor { get; set; } = Color.FromArgb(35, 255, 0, 0);

        public double LineThickness { get; set; } = 3;
    }
}

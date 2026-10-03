using System.Collections.Generic;
using System.Windows;
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
        }

        public AnnotationKind Kind { get; }

        public Point Start { get; set; }

        public Point End { get; set; }

        public Rect Bounds { get; set; }

        public List<Point> Points { get; } = new List<Point>();

        public string Text { get; set; } = "Sample text";

        public Color LineColor { get; set; } = Colors.Red;

        public Color FillColor { get; set; } = Color.FromArgb(35, 255, 0, 0);

        public double LineThickness { get; set; } = 3;
    }
}

using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Brushes = System.Windows.Media.Brushes;

namespace AnnoDesigner
{
    /// <summary>
    /// Interaction logic for AnnoCanvas.xaml
    /// </summary>
    public partial class AnnoCanvas
        : UserControl
    {
        private const int _gridStep = 20;

        public bool RenderGrid { get; set; }

        public AnnoCanvas()
        {
            InitializeComponent();
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            var m = PresentationSource.FromVisual(this).CompositionTarget.TransformToDevice;
            var dpiFactor = 1 / m.M11;
            var pen = new Pen(Brushes.Black, 1 * dpiFactor);

            // assure pixel perfect drawing
            var halfPenWidth = pen.Thickness / 2;
            var guidelines = new GuidelineSet();
            guidelines.GuidelinesX.Add(halfPenWidth);
            guidelines.GuidelinesY.Add(halfPenWidth);
            drawingContext.PushGuidelineSet(guidelines);

            if (RenderGrid)
            {
                // draw grid
                for (var i = 0; i < ActualWidth; i += _gridStep)
                {
                    drawingContext.DrawLine(pen, new Point(i, 0), new Point(i, ActualHeight));
                }
                for (var i = 0; i < ActualHeight; i += _gridStep)
                {
                    drawingContext.DrawLine(pen, new Point(0, i), new Point(ActualWidth, i));
                }
            }
            // pop back guidlines set
            drawingContext.Pop();
        }
    }
}

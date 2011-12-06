using System;
using System.Collections.Generic;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;

namespace AnnoDesigner
{
    /// <summary>
    /// Interaction logic for AnnoCanvas.xaml
    /// </summary>
    public partial class AnnoCanvas
        : UserControl
    {
        #region Properties

        private int _gridStep = 20;
        public int GridSize
        {
            get
            {
                return _gridStep;
            }
            set
            {
                if (_gridStep != value)
                {
                    InvalidateVisual();
                }
                _gridStep = value;
            }
        }

        private bool _renderGrid;
        public bool RenderGrid
        {
            get
            {
                return _renderGrid;
            }
            set
            {
                if (_renderGrid != value)
                {
                    InvalidateVisual();
                }
                _renderGrid = value;
            }
        }

        private bool _renderLabel;
        public bool RenderLabel
        {
            get
            {
                return _renderLabel;
            }
            set
            {
                if (_renderLabel != value)
                {
                    InvalidateVisual();
                }
                _renderLabel = value;
            }
        }

        private bool _renderIcon;
        public bool RenderIcon
        {
            get
            {
                return _renderIcon;
            }
            set
            {
                if (_renderIcon != value)
                {
                    InvalidateVisual();
                }
                _renderIcon = value;
            }
        }

        private DesignMode _designMode;
        public DesignMode DesignMode
        {
            get
            {
                return _designMode;
            }
            set
            {
                if (_designMode != value)
                {
                    InvalidateVisual();
                }
                _designMode = value;
            }
        }

        #endregion

        private Point _mousePosition;
        private List<AnnoObject> _placedObjects;
        private AnnoObject _currentObject;

        public AnnoCanvas()
        {
            InitializeComponent();
            _placedObjects = new List<AnnoObject>();
        }

        #region Rendering

        protected override void OnRender(DrawingContext drawingContext)
        {
            var m = PresentationSource.FromVisual(this).CompositionTarget.TransformToDevice;
            var dpiFactor = 1 / m.M11;
            var pen = new Pen(Brushes.Black, 1 * dpiFactor);

            // assure pixel perfect drawing
            //BUG: doesn't work when exporting
            var halfPenWidth = pen.Thickness / 2;
            var guidelines = new GuidelineSet();
            guidelines.GuidelinesX.Add(halfPenWidth);
            guidelines.GuidelinesY.Add(halfPenWidth);
            drawingContext.PushGuidelineSet(guidelines);

            var width = RenderSize.Width;
            var height = RenderSize.Height;

            // draw background
            drawingContext.DrawRectangle(Brushes.White, null, new Rect(new Point(), RenderSize));

            // draw grid
            if (RenderGrid)
            {
                for (var i = 0; i < width; i += _gridStep)
                {
                    drawingContext.DrawLine(pen, new Point(i, 0), new Point(i, height));
                }
                for (var i = 0; i < height; i += _gridStep)
                {
                    drawingContext.DrawLine(pen, new Point(0, i), new Point(width, i));
                }
            }

            // draw mouse grid position highlight
            var mouseGridPos = ScreenToGrid(_mousePosition);
            drawingContext.DrawRectangle(Brushes.LightYellow, pen, new Rect(GridToScreen(mouseGridPos), new Size(_gridStep, _gridStep)));

            // draw placed objects
            foreach (var placedObject in _placedObjects)
            {
                RenderObject(drawingContext, placedObject, pen);
            }

            switch (_designMode)
            {
                case DesignMode.Edit:
                    // draw current object
                    if (_currentObject != null)
                    {
                        _currentObject.Position = mouseGridPos;
                        _currentObject.Position.X -= Math.Floor(_currentObject.Size.Width / 2);
                        _currentObject.Position.Y -= Math.Floor(_currentObject.Size.Height / 2);
                        _currentObject.Color.A = 128;
                        RenderObject(drawingContext, _currentObject, pen);
                        _currentObject.Color.A = 255;
                    }
                    break;  
                case DesignMode.Select:
                    break;
                default:
                throw new ArgumentOutOfRangeException();
            }

            // pop back guidlines set
            drawingContext.Pop();
        }

        private void RenderObject(DrawingContext drawingContext, AnnoObject obj, Pen pen)
        {
            // draw object rectangle
            var objRect = GetObjectScreenRect(obj);
            drawingContext.DrawRectangle(new SolidColorBrush(obj.Color), pen, objRect);
            // draw object icon
            if (_renderIcon)
            {
                var imageRect = objRect;
                drawingContext.DrawImage(new BitmapImage(new Uri(@"images\Liquor.png", UriKind.Relative)), imageRect);
            }
            // draw object label
            if (_renderLabel)
            {
                var textPoint = objRect.TopLeft;
                textPoint.Y += objRect.Height / 2;
                var text = new FormattedText(obj.Label, Thread.CurrentThread.CurrentCulture, FlowDirection.LeftToRight,
                                             new Typeface("Verdana"), 12, Brushes.Black)
                {
                    TextAlignment = TextAlignment.Center,
                    MaxTextWidth = objRect.Width,
                    MaxTextHeight = objRect.Height
                };
                textPoint.Y -= text.Height / 2;
                drawingContext.DrawText(text, textPoint);
            }
        }

        #endregion

        #region Coordinate and rectangle conversions

        private Point ScreenToGrid(Point screenPoint)
        {
            return new Point(Math.Floor(screenPoint.X / _gridStep), Math.Floor(screenPoint.Y / _gridStep));
        }

        private Point GridToScreen(Point gridPoint)
        {
            return new Point(gridPoint.X * _gridStep, gridPoint.Y * _gridStep);
        }

        private Size GridToScreen(Size gridSize)
        {
            return new Size(gridSize.Width * _gridStep, gridSize.Height * _gridStep);
        }

        private Rect GetObjectScreenRect(AnnoObject obj)
        {
            return new Rect(GridToScreen(obj.Position), GridToScreen(obj.Size));
        }

        private static Rect GetObjectCollisionRect(AnnoObject obj)
        {
            return new Rect(obj.Position, new Size(obj.Size.Width - 0.5, obj.Size.Height - 0.5));
        }

        private static Size Rotate(Size size)
        {
            return new Size(size.Height, size.Width);
        }

        #endregion

        #region Event handling

        private void HandleMouseClick(MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                switch (_designMode)
                {
                    case DesignMode.Edit:
                        // place new object
                        PlaceCurrentObject();
                        break;
                    case DesignMode.Select:
                        break;
                }
            }
            if (e.RightButton == MouseButtonState.Pressed)
            {
                // remove current object
                //_currentObject = null;
                // remove clicked object
                _placedObjects.Remove(_placedObjects.FindLast(_ => GetObjectScreenRect(_).Contains(e.GetPosition(this))));
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            _mousePosition = e.GetPosition(this);
            HandleMouseClick(e);
            InvalidateVisual();
        }

        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            HandleMouseClick(e);
            // rotate current object
            if (e.MiddleButton == MouseButtonState.Pressed && _currentObject != null)
            {
                _currentObject.Size = Rotate(_currentObject.Size);
            }
            InvalidateVisual();
        }

        #endregion

        #region Collision handling

        private bool IntersectionExists(AnnoObject a, AnnoObject b)
        {
            return GetObjectCollisionRect(a).IntersectsWith(GetObjectCollisionRect(b));
        }

        private bool IntersectionTest(AnnoObject obj)
        {
            return _placedObjects.Exists(_ => IntersectionExists(obj, _));
        }

        private void PlaceCurrentObject()
        {
            if (_currentObject != null && !IntersectionTest(_currentObject))
            {
                _placedObjects.Add(new AnnoObject(_currentObject));
            }
        }

        #endregion

        #region API

        public void SetCurrentObject(AnnoObject obj)
        {
            obj.Position = _mousePosition;
            _currentObject = obj;
            InvalidateVisual();
        }

        public void ClearPlacedObjects()
        {
            _placedObjects.Clear();
            InvalidateVisual();
        }

        #endregion

        #region Save/Load/Export methods

        public void SaveToFile()
        {
            var dialog = new SaveFileDialog
            {
                DefaultExt = ".ad",
                Filter = "Anno Designer Files (*.ad)|*.ad|All Files (*.*)|*.*"
            };
            if (dialog.ShowDialog() == true)
            {
                DataIO.SaveToFile(_placedObjects, dialog.FileName);
            }
        }

        public void OpenFile()
        {
            var dialog = new OpenFileDialog
            {
                DefaultExt = ".ad",
                Filter = "Anno Designer Files (*.ad)|*.ad|All Files (*.*)|*.*"
            };
            if (dialog.ShowDialog() == true)
            {
                DataIO.LoadFromFile(out _placedObjects, dialog.FileName);
            }
        }

        public void ExportImage()
        {
            var dialog = new SaveFileDialog
            {
                DefaultExt = ".png",
                Filter = "PNG (*.png)|*.png|All Files (*.*)|*.*"
            };
            if (dialog.ShowDialog() == true)
            {
                DataIO.RenderToFile(this, dialog.FileName);
            }
        }

        #endregion
    }
}

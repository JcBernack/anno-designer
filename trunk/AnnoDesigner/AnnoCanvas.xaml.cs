using System;
using System.Collections.Generic;
using System.Runtime.Serialization.Formatters.Binary;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Win32;
using Brushes = System.Windows.Media.Brushes;

namespace AnnoDesigner
{
    /// <summary>
    /// Interaction logic for AnnoCanvas.xaml
    /// </summary>
    public partial class AnnoCanvas
        : UserControl
    {
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

        private Point _mousePosition;
        private readonly List<AnnoObject> _placedObjects;
        private AnnoObject _currentObject;

        public AnnoCanvas()
        {
            InitializeComponent();
            _placedObjects = new List<AnnoObject>();
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
                case DesignMode.New:
                    // draw current object
                    if (_currentObject != null)
                    {
                        _currentObject.Position = mouseGridPos;
                        _currentObject.Position.X -= Math.Floor(_currentObject.Size.Width / 2);
                        _currentObject.Position.Y -= Math.Floor(_currentObject.Size.Height / 2);
                        RenderObject(drawingContext, _currentObject, pen);
                    }
                    break;
                case DesignMode.Remove:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            // pop back guidlines set
            drawingContext.Pop();
        }

        private Point ScreenToGrid(Point screenPoint)
        {
            return new Point(Math.Floor(screenPoint.X / _gridStep), Math.Floor(screenPoint.Y /_gridStep));
        }

        private Point GridToScreen(Point gridPoint)
        {
            return new Point(gridPoint.X * _gridStep, gridPoint.Y * _gridStep);
        }

        private Size GridToScreen(Size gridSize)
        {
            return new Size(gridSize.Width * _gridStep, gridSize.Height * _gridStep);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            _mousePosition = e.GetPosition(this);
            InvalidateVisual();
        }

        protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
        {
            switch (_designMode)
            {
                case DesignMode.New:
                    // place new object
                    if (_currentObject != null)
                    {
                        _placedObjects.Add(new AnnoObject(_currentObject));
                        InvalidateVisual();
                    }
                    break;
                case DesignMode.Remove:
                    // remove clicked object
                    _placedObjects.RemoveAll(_ => GetObjectScreenRect(_).Contains(e.GetPosition(this)));
                    InvalidateVisual();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        protected override void OnMouseRightButtonUp(MouseButtonEventArgs e)
        {
            _currentObject = null;
            InvalidateVisual();
        }

        private void RenderObject(DrawingContext drawingContext, AnnoObject obj, Pen pen)
        {
            var brush = new SolidColorBrush(obj.Color);
            drawingContext.DrawRectangle(brush, pen, GetObjectScreenRect(obj));
        }

        private Rect GetObjectScreenRect(AnnoObject obj)
        {
            return new Rect(GridToScreen(obj.Position), GridToScreen(obj.Size));
        }

        public void SetCurrentObject(AnnoObject obj)
        {
            obj.Position = _mousePosition;
            _currentObject = obj;
            InvalidateVisual();
        }

        public void ClearPlacedObjects()
        {
            _placedObjects.Clear();
        }

        public void SaveToFile()
        {
            var dialog = new SaveFileDialog();
            dialog.ShowDialog();
            var file = dialog.OpenFile();
            var formatter = new BinaryFormatter();
            formatter.Serialize(file, _placedObjects);
            file.Close();
        }
    }
}

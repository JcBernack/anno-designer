using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using MessageBox = Microsoft.Windows.Controls.MessageBox;
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

        public delegate void AnnoObjectHandler(AnnoObject annoObject);
        public event AnnoObjectHandler OnCurrentObjectChange;

        public event Action<string> OnShowStatusMessage;
        private void FireOnShowStatusMessage(string message)
        {
            if (OnShowStatusMessage != null)
            {
                OnShowStatusMessage(message);
            }
        }

        #endregion

        private enum MouseMode
        {
            // used if not dragging
            Standard,
            // used to drag the selection rect
            SelectionRectStart,
            SelectionRect,
            // used to drag objects around
            DragSelectionStart,
            DragSingleStart,
            DragSelection,
            DragAllStart,
            DragAll
        }

        private Point _mousePosition;
        private bool _mouseWithinControl;
        private MouseMode _currentMode;
        private MouseMode CurrentMode
        {
            get
            {
                return _currentMode;
            }
            set
            {
                _currentMode = value;
                FireOnShowStatusMessage(_currentMode.ToString());
            }
        }
        private Point _mouseDragStart;
        private Rect _selectionRect;
        
        private List<AnnoObject> _placedObjects;
        private readonly List<AnnoObject> _selectedObjects; 
        private AnnoObject _currentObject;

        private readonly Pen _linePen;
        private readonly Pen _highlightPen;
        private readonly Pen _radiusPen;
        private readonly Pen _influencedPen;
        private readonly Brush _lightBrush;
        private readonly Brush _influencedBrush;

        public AnnoCanvas()
        {
            InitializeComponent();
            CurrentMode = MouseMode.Standard;
            _placedObjects = new List<AnnoObject>();
            _selectedObjects = new List<AnnoObject>();
            _linePen = new Pen(Brushes.Black, 1);
            _highlightPen = new Pen(Brushes.Yellow, 1);
            _radiusPen = new Pen(Brushes.Black, 1);
            _influencedPen = new Pen(Brushes.LawnGreen, 1);
            var color = Colors.LightYellow;
            color.A = 92;
            _lightBrush = new SolidColorBrush(color);
            color = Colors.LawnGreen;
            color.A = 92;
            _influencedBrush = new SolidColorBrush(color);
            Focusable = true;
        }

        #region Rendering

        protected override void OnRender(DrawingContext drawingContext)
        {
            var m = PresentationSource.FromVisual(this).CompositionTarget.TransformToDevice;
            var dpiFactor = 1 / m.M11;
            _linePen.Thickness = dpiFactor * 1;
            _highlightPen.Thickness = dpiFactor * 2;
            _radiusPen.Thickness = dpiFactor * 2;
            _influencedPen.Thickness = dpiFactor * 2;

            // assure pixel perfect drawing
            //BUG: doesn't work when exporting
            var halfPenWidth = _linePen.Thickness / 2;
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
                    drawingContext.DrawLine(_linePen, new Point(i, 0), new Point(i, height));
                }
                for (var i = 0; i < height; i += _gridStep)
                {
                    drawingContext.DrawLine(_linePen, new Point(0, i), new Point(width, i));
                }
            }

            // draw mouse grid position highlight
            //drawingContext.DrawRectangle(Brushes.LightYellow, linePen, new Rect(GridToScreen(ScreenToGrid(_mousePosition)), new Size(_gridStep, _gridStep)));

            // draw placed objects
            foreach (var obj in _placedObjects)
            {
                RenderObject(drawingContext, obj);
            }
            foreach (var obj in _selectedObjects)
            {
                RenderObjectInfluence(drawingContext, obj);
                RenderObjectSelection(drawingContext, obj);
            }

            if (_currentObject == null)
            {
                // highlight object which is currently hovered
                var hoveredObj = GetObjectAt(_mousePosition);
                if (hoveredObj != null)
                {
                    drawingContext.DrawRectangle(null, _highlightPen, GetObjectScreenRect(hoveredObj));
                }
            }
            else
            {
                // draw current object
                if (_mouseWithinControl)
                {
                    MoveCurrentObjectToMouse();
                    // draw influence radius
                    RenderObjectInfluence(drawingContext, _currentObject);
                    // draw with transparency
                    _currentObject.Color.A = 128;
                    RenderObject(drawingContext, _currentObject);
                    _currentObject.Color.A = 255;
                }
            }
            // draw selection rect while dragging the mouse
            if (CurrentMode == MouseMode.SelectionRect)
            {
                drawingContext.DrawRectangle(_lightBrush, _highlightPen, _selectionRect);
            }
            // pop back guidlines set
            drawingContext.Pop();
        }

        private void MoveCurrentObjectToMouse()
        {
            if (_currentObject == null)
            {
                return;
            }
            // determine grid position beneath mouse
            var pos = _mousePosition;
            var size = GridToScreen(_currentObject.Size);
            pos.X -= size.Width / 2;
            pos.Y -= size.Height / 2;
            _currentObject.Position = RoundScreenToGrid(pos);
        }

        private void RenderObject(DrawingContext drawingContext, AnnoObject obj)
        {
            // draw object rectangle
            var objRect = GetObjectScreenRect(obj);
            drawingContext.DrawRectangle(new SolidColorBrush(obj.Color), _linePen, objRect);
            // draw object icon if it is at least 2x2 cells
            var iconRendered = false;
            if (_renderIcon && !string.IsNullOrEmpty(obj.Icon))
            {
                // draw icon 2x2 grid cells large
                var iconSize = obj.Size.Width < 2 && obj.Size.Height < 2
                    ? GridToScreen(new Size(1,1))
                    : GridToScreen(new Size(2,2));
                // center icon within the object
                var iconPos = objRect.TopLeft;
                iconPos.X += objRect.Width/2 - iconSize.Width/2;
                iconPos.Y += objRect.Height/2 - iconSize.Height/2;
                if (File.Exists(obj.Icon))
                {
                    drawingContext.DrawImage(new BitmapImage(new Uri(obj.Icon, UriKind.Relative)), new Rect(iconPos, iconSize));
                    iconRendered = true;
                }
                else
                {
                    FireOnShowStatusMessage(string.Format("Icon file missing ({0}).", obj.Icon));
                }
            }
            // draw object label
            if (_renderLabel)
            {
                var textPoint = objRect.TopLeft;
                var text = new FormattedText(obj.Label, Thread.CurrentThread.CurrentCulture, FlowDirection.LeftToRight,
                                             new Typeface("Verdana"), 12, Brushes.Black)
                {
                    MaxTextWidth = objRect.Width,
                    MaxTextHeight = objRect.Height
                };
                if (iconRendered)
                {
                    // place the text in the top left corner if a icon is present
                    text.TextAlignment = TextAlignment.Left;
                    textPoint.X += 3;
                    textPoint.Y += 2;
                }
                else
                {
                    // center the text if no icon is present
                    text.TextAlignment = TextAlignment.Center;
                    textPoint.Y += (objRect.Height - text.Height) / 2;
                }
                drawingContext.DrawText(text, textPoint);
            }
        }

        private void RenderObjectSelection(DrawingContext drawingContext, AnnoObject obj)
        {
            // draw object rectangle
            var objRect = GetObjectScreenRect(obj);
            drawingContext.DrawRectangle(null, _highlightPen, objRect);
        }

        private void RenderObjectInfluence(DrawingContext drawingContext, AnnoObject obj)
        {
            if (obj.Radius >= 0.5)
            {
                // highlight buildings within influence
                var radius = GridToScreen(obj.Radius);
                var circle = new EllipseGeometry(GetCenterPoint(GetObjectScreenRect(obj)), radius, radius);
                foreach (var o in _placedObjects)
                {
                    var oRect = GetObjectScreenRect(o);
                    var distance = GetCenterPoint(oRect);
                    distance.X -= circle.Center.X;
                    distance.Y -= circle.Center.Y;
                    // check if the center is within the influence circle
                    if (distance.X*distance.X + distance.Y*distance.Y <= radius*radius)
                    {
                        drawingContext.DrawRectangle(_influencedBrush, _influencedPen, oRect);
                    }
                }
                // draw circle
                drawingContext.DrawGeometry(_lightBrush, _radiusPen, circle);
            }
        }

        #endregion

        #region Coordinate and rectangle conversions

        /// <summary>
        /// Convert a screen coordinate to a grid coordinate by determining in which grid cell the point is contained.
        /// </summary>
        /// <param name="screenPoint"></param>
        /// <returns></returns>
        [Pure]
        private Point ScreenToGrid(Point screenPoint)
        {
            return new Point(Math.Floor(screenPoint.X / _gridStep), Math.Floor(screenPoint.Y / _gridStep));
        }

        /// <summary>
        /// Converts a screen coordinate to a grid coordinate by determining which grid cell is nearest.
        /// </summary>
        /// <param name="screenPoint"></param>
        /// <returns></returns>
        [Pure]
        private Point RoundScreenToGrid(Point screenPoint)
        {
            return new Point(Math.Round(screenPoint.X / _gridStep), Math.Round(screenPoint.Y / _gridStep));
        }

        /// <summary>
        /// Converts a length given in (pixel-)units to a length given in grid cells.
        /// </summary>
        /// <param name="screenLength"></param>
        /// <returns></returns>
        [Pure]
        private double ScreenToGrid(double screenLength)
        {
            return screenLength / _gridStep;
        }

        /// <summary>
        /// Convert a grid coordinate to a screen coordinate.
        /// </summary>
        /// <param name="gridPoint"></param>
        /// <returns></returns>
        [Pure]
        private Point GridToScreen(Point gridPoint)
        {
            return new Point(gridPoint.X * _gridStep, gridPoint.Y * _gridStep);
        }

        /// <summary>
        /// Converts a size given in grid cells to a size given in (pixel-)units.
        /// </summary>
        /// <param name="gridSize"></param>
        /// <returns></returns>
        [Pure]
        private Size GridToScreen(Size gridSize)
        {
            return new Size(gridSize.Width * _gridStep, gridSize.Height * _gridStep);
        }

        /// <summary>
        /// Converts a length given in grid cells to a length given in (pixel-)units.
        /// </summary>
        /// <param name="gridLength"></param>
        /// <returns></returns>
        [Pure]
        private double GridToScreen(double gridLength)
        {
            return gridLength * _gridStep;
        }

        /// <summary>
        /// Calculates the exact center point of a given rect
        /// </summary>
        /// <param name="rect"></param>
        /// <returns></returns>
        [Pure]
        private static Point GetCenterPoint(Rect rect)
        {
            var pos = rect.Location;
            var size = rect.Size;
            pos.X += size.Width / 2;
            pos.Y += size.Height / 2;
            return pos;
        }

        /// <summary>
        /// Generates the rect to which the given object is rendered.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        [Pure]
        private Rect GetObjectScreenRect(AnnoObject obj)
        {
            return new Rect(GridToScreen(obj.Position), GridToScreen(obj.Size));
        }

        /// <summary>
        /// Gets the rect which is used for collision detection for the given object.
        /// Prevents undesired collisions which occur when using GetObjectScreenRect().
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        [Pure]
        private static Rect GetObjectCollisionRect(AnnoObject obj)
        {
            return new Rect(obj.Position, new Size(obj.Size.Width - 0.5, obj.Size.Height - 0.5));
        }

        /// <summary>
        /// Rotates the given Size object, i.e. switches width and height.
        /// </summary>
        /// <param name="size"></param>
        /// <returns></returns>
        [Pure]
        private static Size Rotate(Size size)
        {
            return new Size(size.Height, size.Width);
        }

        #endregion

        #region Event handling

        #region Mouse

        protected override void OnMouseEnter(MouseEventArgs e)
        {
            _mouseWithinControl = true;
            Focus();
        }

        protected override void OnMouseLeave(MouseEventArgs e)
        {
            _mouseWithinControl = false;
            InvalidateVisual();
        }

        private void HandleMouse(MouseEventArgs e)
        {
            // refresh retrieved mouse position
            _mousePosition = e.GetPosition(this);
            MoveCurrentObjectToMouse();
        }

        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            HandleMouse(e);
            if (e.ClickCount > 1)
            {
                var obj = GetObjectAt(_mousePosition);
                if (obj != null)
                {
                    _currentObject = new AnnoObject(obj);
                    if (OnCurrentObjectChange != null)
                    {
                        OnCurrentObjectChange(_currentObject);
                    }
                }
                return;
            }
            _mouseDragStart = _mousePosition;
            if (e.LeftButton == MouseButtonState.Pressed && e.RightButton == MouseButtonState.Pressed)
            {
                CurrentMode = MouseMode.DragAllStart;
            }
            else if (e.LeftButton == MouseButtonState.Pressed && _currentObject != null)
            {
                // place new object
                TryPlaceCurrentObject();
            }
            else if (e.LeftButton == MouseButtonState.Pressed && _currentObject == null)
            {
                var obj = GetObjectAt(_mousePosition);
                if (obj == null)
                {
                    // user clicked nothing: start dragging the selection rect
                    CurrentMode = MouseMode.SelectionRectStart;
                }
                else if (!IsControlPressed())
                {
                    CurrentMode = _selectedObjects.Contains(obj) ? MouseMode.DragSelectionStart : MouseMode.DragSingleStart;
                }
            }
            InvalidateVisual();
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            HandleMouse(e);
            // check if user begins to drag
            if (Math.Abs(_mouseDragStart.X - _mousePosition.X) > 1 || Math.Abs(_mouseDragStart.Y - _mousePosition.Y) > 1)
            {
                switch (CurrentMode)
                {
                    case MouseMode.SelectionRectStart:
                        CurrentMode = MouseMode.SelectionRect;
                        _selectionRect = new Rect();
                        break;
                    case MouseMode.DragSelectionStart:
                        CurrentMode = MouseMode.DragSelection;
                        break;
                    case MouseMode.DragSingleStart:
                        _selectedObjects.Clear();
                        _selectedObjects.Add(GetObjectAt(_mouseDragStart));
                        CurrentMode = MouseMode.DragSelection;
                        break;
                    case MouseMode.DragAllStart:
                        CurrentMode = MouseMode.DragAll;
                        break;
                }
            }
            if (CurrentMode == MouseMode.DragAll)
            {
                // move all selected objects
                var dx = (int)ScreenToGrid(_mousePosition.X - _mouseDragStart.X);
                var dy = (int)ScreenToGrid(_mousePosition.Y - _mouseDragStart.Y);
                // check if the mouse has moved at least one grid cell in any direction
                if (dx != 0 || dy != 0)
                {
                    foreach (var obj in _placedObjects)
                    {
                        obj.Position.X += dx;
                        obj.Position.Y += dy;
                    }
                    // adjust the drag start to compensate the amount we already moved
                    _mouseDragStart.X += GridToScreen(dx);
                    _mouseDragStart.Y += GridToScreen(dy);
                }
            }
            else if (e.LeftButton == MouseButtonState.Pressed)
            {
                if (_currentObject != null)
                {
                    // place new object
                    TryPlaceCurrentObject();
                }
                else
                {
                    // selection of multiple objects
                    switch (CurrentMode)
                    {
                        case MouseMode.SelectionRect:
                            if (IsControlPressed())
                            {
                                // remove previously selected by the selection rect
                                _selectedObjects.RemoveAll(_ => GetObjectScreenRect(_).IntersectsWith(_selectionRect));
                            }
                            else
                            {
                                _selectedObjects.Clear();
                            }
                            // adjust rect
                            _selectionRect = new Rect(_mouseDragStart, _mousePosition);
                            // select intersecting objects
                            _selectedObjects.AddRange(_placedObjects.FindAll(_ => GetObjectScreenRect(_).IntersectsWith(_selectionRect)));
                            break;
                        case MouseMode.DragSelection:
                            // move all selected objects
                            var dx = (int)ScreenToGrid(_mousePosition.X - _mouseDragStart.X);
                            var dy = (int)ScreenToGrid(_mousePosition.Y - _mouseDragStart.Y);
                            // check if the mouse has moved at least one grid cell in any direction
                            if (dx == 0 && dy == 0)
                            {
                                break;
                            }
                            var unselected = _placedObjects.FindAll(_ => !_selectedObjects.Contains(_));
                            var collisionsExist = false;
                            // temporary move each object and check if collisions with unselected objects exist
                            foreach (var obj in _selectedObjects)
                            {
                                var originalPosition = obj.Position;
                                // move object
                                obj.Position.X += dx;
                                obj.Position.Y += dy;
                                // check for collisions
                                var collides = unselected.Find(_ => ObjectIntersectionExists(obj, _)) != null;
                                obj.Position = originalPosition;
                                if (collides)
                                {
                                    collisionsExist = true;
                                    break;
                                }
                            }
                            // if no collisions were found, permanently move all selected objects
                            if (!collisionsExist)
                            {
                                foreach (var obj in _selectedObjects)
                                {
                                    obj.Position.X += dx;
                                    obj.Position.Y += dy;
                                }
                                // adjust the drag start to compensate the amount we already moved
                                _mouseDragStart.X += GridToScreen(dx);
                                _mouseDragStart.Y += GridToScreen(dy);
                            }
                            break;
                    }
                }
            }
            InvalidateVisual();
        }

        protected override void OnMouseUp(MouseButtonEventArgs e)
        {
            HandleMouse(e);
            if (CurrentMode == MouseMode.DragAll)
            {
                if (e.LeftButton == MouseButtonState.Released && e.RightButton == MouseButtonState.Released)
                {
                    CurrentMode = MouseMode.Standard;
                }
                return;
            }
            if (e.ChangedButton == MouseButton.Left && _currentObject == null)
            {
                switch (CurrentMode)
                {
                    default:
                        // clear selection if no key is pressed
                        if (!IsControlPressed())
                        {
                            _selectedObjects.Clear();
                        }
                        var obj = GetObjectAt(_mousePosition);
                        if (obj != null)
                        {
                            // user clicked an object: select or deselect it
                            if (_selectedObjects.Contains(obj))
                            {
                                _selectedObjects.Remove(obj);
                            }
                            else
                            {
                                _selectedObjects.Add(obj);
                            }
                        }
                        // return to standard mode, i.e. clear any drag-start modes
                        CurrentMode = MouseMode.Standard;
                        break;
                    case MouseMode.SelectionRect:
                        // cancel dragging of selection rect
                        CurrentMode = MouseMode.Standard;
                        break;
                    case MouseMode.DragSelection:
                        // stop dragging of selected objects
                        CurrentMode = MouseMode.Standard;
                        break;
                }
            }
            if (e.ChangedButton == MouseButton.Right)
            {
                switch (CurrentMode)
                {
                    case MouseMode.Standard:
                        if (_currentObject == null)
                        {
                            var obj = GetObjectAt(_mousePosition);
                            if (obj == null)
                            {
                                if (!IsControlPressed())
                                {
                                    // clear selection
                                    _selectedObjects.Clear();
                                }
                            }
                            else
                            {
                                // remove clicked object
                                _placedObjects.Remove(obj);
                                _selectedObjects.Remove(obj);
                            }
                        }
                        else
                        {
                            // cancel placement of object
                            _currentObject = null;
                        }
                        break;
                }
            }
            // rotate current object
            if (e.ChangedButton == MouseButton.Middle && _currentObject != null)
            {
                _currentObject.Size = Rotate(_currentObject.Size);
            }
            InvalidateVisual();
        }

        #endregion

        #region Keyboard

        protected override void OnKeyDown(KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Delete:
                    _selectedObjects.ForEach(_ => _placedObjects.Remove(_));
                    _selectedObjects.Clear();
                    break;
            }
            InvalidateVisual();
        }

        protected override void OnKeyUp(KeyEventArgs e)
        {
        }

        /// <summary>
        /// Checks whether the user is pressing keys to signal that he wants to select multiple objects
        /// </summary>
        /// <returns></returns>
        private static bool IsControlPressed()
        {
            return Keyboard.Modifiers.HasFlag(ModifierKeys.Control) || Keyboard.Modifiers.HasFlag(ModifierKeys.Shift);
        }

        #endregion

        #endregion

        #region Collision handling

        private bool ObjectIntersectionExists(AnnoObject a, AnnoObject b)
        {
            return GetObjectCollisionRect(a).IntersectsWith(GetObjectCollisionRect(b));
        }

        private bool TryPlaceCurrentObject()
        {
            if (_currentObject != null && !_placedObjects.Exists(_ => ObjectIntersectionExists(_currentObject, _)))
            {
                _placedObjects.Add(new AnnoObject(_currentObject));
                return true;
            }
            return false;
        }

        private AnnoObject GetObjectAt(Point position)
        {
            return _placedObjects.FindLast(_ => GetObjectScreenRect(_).Contains(position));
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
                try
                {
                    DataIO.SaveToFile(_placedObjects, dialog.FileName);
                }
                catch (Exception)
                {
                    IOErrorMessageBox();
                }
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
                try
                {
                    DataIO.LoadFromFile(out _placedObjects, dialog.FileName);
                    _selectedObjects.Clear();
                    InvalidateVisual();
                }
                catch (Exception)
                {
                    IOErrorMessageBox();
                }
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
                try
                {
                    DataIO.RenderToFile(this, dialog.FileName);
                }
                catch (Exception)
                {
                    IOErrorMessageBox();
                }
            }
        }

        private static void IOErrorMessageBox()
        {
            MessageBox.Show("Something went wrong while saving/loading file.");
        }

        #endregion
    }
}

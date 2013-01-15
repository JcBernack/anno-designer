/**
 * Anno Designer - Web Edition
 * Date: 09.01.13
 * @requires jQuery
 * @author Jan Christoph Bernack
 */
//** Editor mouse behavior
// + MouseMove
//  - no action active: highlight object under mouse
//   -   or start dragging (selection rect or move selected objects)
//  - placing an object: move object to mouse
// + MouseClick (left)
//   - no action active: select objects (shift/ctrl mechanics)
//   -   or prepare dragging
//   - placing an object: place object if it fits (collision detection)
// + MouseDblClick (left)
//   - get properties of clicked objects
// + MouseClick (right)
//   - no action active: remove object at mouse
//   - placing an object: stop placing objects
// + MouseClick (middle)
//   - placing an object: flip object dimensions
// + MouseWheel
//   - change zoom level (grid cell size)
// + Hotkeys:
//   - entf: remove selected objects, if any

var Convert = {
    bool: function(str) { return str == "1" },
    int: function(str) { var n = parseInt(str); return isNaN(n) ? 0 : n; }
};

var Building = function(left, top, width, height, color, label) {
    this.left = left;
    this.top = top;
    this.width = width;
    this.height = height;
    this.color = color;
    this.label = label;
    this.enableLabel = false;
    this.borderless = false;
    this.road = false;
};

Building.FromObject = function (obj) {
    // type conversions needed to support creating building directly from data supplied by the webservice
    var b = new Building(
        Convert.int(obj.left),
        Convert.int(obj.top),
        Convert.int(obj.width),
        Convert.int(obj.height),
        obj.color,
        obj.label
    );
    b.enableLabel = Convert.bool(obj.enableLabel);
    b.borderless = Convert.bool(obj.borderless);
    b.road = Convert.bool(obj.road);
    return b;
};

Building.prototype.Position = function (point) {
    if (point) {
        this.left = point.x;
        this.top = point.y;
        return this;
    } else {
        return new Point(this.left, this.top);
    }
};

Building.prototype.Size = function () {
    return new Size(this.width, this.height);
};

Building.prototype.Rect = function () {
    return new Rect(this.left, this.top, this.width, this.height);
};

Building.prototype.IsValid = function () {
    return !(this.width < 1 || this.height < 1 || this.color.length != 7);
};

var MouseButton = {
    Left: 1,
    Middle: 2,
    Right: 3
};

// Constructor
var Designer = function (options) {
    // extend defaults with given options
    this._options = $.extend(true, {}, Designer.defaultOptions, options);
    // get reference to container element
    var container = $("#" + this._options.containerId);
    this._container = container;
    // prepare container, canvas and buttonpane
    container.html("");
    this._resizer = $(document.createElement("div")).addClass("resizer");
    this._canvas = document.createElement("canvas");
    this._canvas.tabIndex = 0;
    this._resizer.append(this._canvas);
    container.append(this._resizer);
    this._ctx = this._canvas.getContext("2d");
    this._registerEvents();
    if (this._options.enableEditing) {
        this._createButtonpane();
    }
    // render an empty layout
    this.SetSize();
    this.Render();
    this.RefreshResizer();
};

Designer.defaultOptions = {
    serviceUrl: "rest/",
    containerId: "editor",
    layoutDeleted: $.noop,
    autoSize: true,
    enableEditing: true,
    drawGrid: true,
    grid: 15,
    width: 15,
    height: 10,
    zoomSpeed: 1.1,
    spacing: 0.5 //TODO: implement normalization and set to 1
};

Designer.State = {
    // used if not dragging
    Standard: 0,
    // used to drag the selection rect
    SelectionRectStart: 1,
    SelectionRect: 2,
    // used to drag objects around
    DragSelectionStart: 3,
    DragSingleStart: 4,
    DragSelection: 5,
    DragAllStart: 6,
    DragAll: 7
};

// holds an array of all objects
Designer.prototype._objects = [];
// holds an array of all objects which are highlighted
Designer.prototype._selectedObjects = [];
// the object which is currently being placed, or null if none
Designer.prototype._currentObject = null;
// the object currently under the mouse
Designer.prototype._hoveredObject = null;

// holds the layout object received from the server
Designer.prototype._layout = null;
// current interaction state
Designer.prototype._state = Designer.State.Standard;
// mouse button states
Designer.prototype._mouseButtons = {
    //TODO: key-ups are missed if the buttons are released outside the canvas, this can mess up the _mouseButtons state
    left: false,
    middle: false,
    right: false,
    toString: function () {
        var buttons = [];
        for (var button in this) {
            if (button == "toString") continue;
            if (this[button]) buttons.push(button);
        }
        return "[" + buttons.join(",") + "]";
    }
};
// mouse position
Designer.prototype._mousePosition = null;
// position where dragging started
Designer.prototype._mouseDragStart = null;
// current selection rectangle
Designer.prototype._selectionRect = null;

Designer.prototype._framesRendered = 0;

Designer.prototype.Reset = function () {
    this._objects = [];
    this._selectedObjects = [];
    this._hoveredObject = null;
    this._setCurrentLayout(null);
};

Designer.prototype._createButtonpane = function () {
    var $this = this;
    $.ajax({
        url: "designer_buttonpane.html",
        success: function (data) {
            $this._container.append(data);
            // find containing element
            var pane = $this._container.find(".buttonpane");
            // prepare buttons
            pane.find("#new").button({ icons: { primary: "ui-icon-document" } })
                .click(function() { $this.New(); });
            pane.find("#save").button({ icons: { primary: "ui-icon-pencil" }, disabled: true })
                .click(function() { $this.Save(); });
            pane.find("#saveas").button({ icons: { primary: "ui-icon-disk" } })
                .click(function() { $this.SaveAs(); });
			pane.find("#delete").button({ icons: { primary: "ui-icon-trash" } })
                .click(function() { $this.Delete(); });
            pane.find("#flipSize").button({ icons: { primary: "ui-icon-transfer-e-w" }, text: false })
                .click(function() {
                    var w = $("#width"), h = $("#height");
                    var tmp = w.val();
                    w.val(h.val());
                    h.val(tmp);
                });
            pane.find("#apply").button({ icons: { primary: "ui-icon-check" } })
                .click(function() { $this.ApplyCurrentObject(); });
            // initialize color picker
            $.minicolors.init();
            // put the whole menu inside an accordion
            pane.accordion({
                heightStyle: "content",
                collapsible: true,
                active: 0
            });
            // keep reference
            $this._buttonpane = pane;
        }
    });
};

Designer.prototype.ToggleButtonpane = function(visible) {
    if (arguments.length == 0) {
        this._buttonpane.toggle();
    } else {
        this._buttonpane.toggle(visible);
    }
};

Designer.prototype.RefreshResizer = function() {
    var $this = this;
    var grid = this._options.grid;
    var resize = function(event, ui) {
        if ($this.SetSize(new Size(ui.size.width, ui.size.height).Scale(1/grid, true))) {
            // redraw if dimensions have changed
            $this.Render();
        }
    };
    this._resizer.resizable({
        grid: grid,
        minWidth: 10 * grid,
        minHeight: 10 * grid,
        helper: "resizer-helper",
        start: function(event, ui) {
            $this._resizer.addClass("resizer-helper");
        },
        resize: resize,
        stop: function(event, ui) {
            $this._resizer.removeClass("resizer-helper");
            resize(event, ui);
        }
    });
    function toggleClass(selector, css) {
        $this._resizer.find(selector).unbind(".designer")
            .bind("mouseenter.designer", function() { $this._resizer.addClass(css); })
            .bind("mouseleave.designer", function() { $this._resizer.removeClass(css); });
    }
    toggleClass(".ui-resizable-e", "resizer-helper-e");
    toggleClass(".ui-resizable-s", "resizer-helper-s");
    toggleClass(".ui-resizable-se", "resizer-helper-e resizer-helper-s");
};

// ** Sizing
Designer.prototype.SetSize = function (size) {
    switch (arguments.length) {
        // use current dimensions if called without argument
        case 0: size = new Size(this._options.width, this._options.height); break;
        case 1: break;
        // accept two arguments: width, height
        case 2: size = new Size(arguments[0], arguments[1]); break;
    }
    // remember size in grid units
    this._options.width = size.width;
    this._options.height = size.height;
    // scale size to pixel units
    size.Scale(this._options.grid);
    // compensate for last grid line
    size.width++;
    size.height++;
    // adjust resizer
    this._resizer.width(size.width);
    this._resizer.height(size.height);
    this.RefreshResizer();
    // check if nothing has changed
    if (this._canvas.width == size.width && this._canvas.height == size.height) {
        return false;
    }
    // set canvas size in pixels
    this._canvas.width = size.width;
    this._canvas.height = size.height;
    return true;
};

Designer.prototype.AutoSize = function () {
    // adjust canvas size, e.g. for changed grid-size
    // prevents collapsing to a single cell (width x height: 1x1)
    if (this._objects == null || this._objects.length == 0) {
        return this.SetSize();
    }
    // ** DEBUG, as long as server output is nonsense
    var width = 0;
    var height = 0;
    // find min width and height needed
    for (var i = 0; i < this._objects.length; i++) {
        var obj = this._objects[i];
        if (obj.left + obj.width > width) {
            width = obj.left + obj.width;
        }
        if (obj.top + obj.height > height) {
            height = obj.top + obj.height;
        }
    }
    // **
    // apply correct size
    var space = 2 * this._options.spacing;
    return this.SetSize(width + space, height + space);
};

// ** Layout helper
Designer.prototype._findObjectAtPosition = function(point) {
    var p = point.Copy().Scale(1/this._options.grid);
    for (var i = 0; i < this._objects.length; i++) {
        if (this._objects[i].Rect().ContainsPoint(p)) {
            return this._objects[i];
        }
    }
    return null;
};

Designer.prototype._parseLayout = function (layout) {
    this.Reset();
    this._setCurrentLayout(layout);
    // parse objects retrieved from service
    this._objects = [];
    for (var i = 0; i < layout.objects.length; i++) {
        this._objects.push(Building.FromObject(layout.objects[i]));
    }
    // if auto-sizing adjust canvas size to fit the layout
    if (this._options.autoSize) {
        this.AutoSize();
    }
    // render the new layout
    this.Render();
};

// ** Layout I/O
Designer.prototype.New = function () {
    this.Reset();
    this.Render();
};

Designer.prototype.Load = function (id) {
    // load file from url and parse as json
    var $this = this;
    Rest("GET", this._options.serviceUrl + "layout/" + id, null,
        function (data) {
            $this._parseLayout(data);
        }
    );
};

Designer.prototype.Save = function () {
    //TODO: implement Save()
};

Designer.prototype.SaveAs = function () {
    // validation: empty layout
    if (this._objects == null || this._objects.length == 0) {
        alert("Nothing placed.");
        return;
    }
    // validation: no name set
    var name = this._buttonpane.find("#layoutName").val();
    if (name == "") {
        alert("No name given.");
        return;
    }
    // load file from url and parse as json
    var $this = this;
    Rest("POST",this._options.serviceUrl + "layout",
        "data=" + JSON.stringify({
            name: name,
            objects: this._objects
        }),
        function () {
            alert("successfully saved");
            //TODO: refresh datatable
        });
};

Designer.prototype.Delete = function () {
    // delete the currently loaded layout
    if (this._layout == null) {
        return;
    }
    //TODO: add confirmation dialog
    var $this = this;
    Rest("DELETE", this._options.serviceUrl + "layout/" + this._layout.ID, null,
        function (data) {
            if (!data.success) {
                alert("deletion failed");
                return;
            }
            // fire deleted event
            $this._options.layoutDeleted($this._layout.ID);
            // reset the editor
            $this.Reset();
			$this.Render();
        });
};

Designer.prototype._setCurrentLayout  = function(layout) {
    this._layout = layout;
    if (layout == null) {
        // default values to show when no layout is set
        layout = { name: "", author: "", width: 0, height: 0, created: "", edited: "" };
    }
    // set information
    var b = this._buttonpane;
    b.find("#layoutName").val(layout.name);
    b.find("#layoutAuthor").html(layout.author);
    b.find("#layoutSize").html(layout.width + "x" + layout.height);
    b.find("#layoutCreated").html(layout.created);
    b.find("#layoutEdited").html(layout.edited);
};

//** Current object handling
Designer.prototype.ApplyCurrentObject = function() {
    this._currentObject = this._getCurrentProperties();
    if (!this._currentObject.IsValid()) {
        alert("object invalid");
        this._currentObject = null;
    }
};

Designer.prototype._setCurrentProperties = function(building) {
    var b = this._buttonpane;
    if (b == null || building == null) {
        return;
    }
    b.find("#width").val(building.width);
    b.find("#height").val(building.height);
    b.find("#color").val(building.color);
    b.find("#label").val(building.label);
    b.find("#enableLabel")[0].checked = building.enableLabel;
    b.find("#borderless")[0].checked = building.borderless;
    b.find("#road")[0].checked = building.road;
    $.minicolors.refresh();
};

Designer.prototype._getCurrentProperties = function() {
    var bp = this._buttonpane;
    if (bp == null) {
        return null;
    }
    var b = new Building(0, 0,
        Convert.int(bp.find("#width").val()),
        Convert.int(bp.find("#height").val()),
        bp.find("#color").val(),
        bp.find("#label").val()
    );
    b.enableLabel = bp.find("#enableLabel")[0].checked;
    b.borderless = bp.find("#borderless")[0].checked;
    b.road = bp.find("#road")[0].checked;
    return b;
};

Designer.prototype._moveCurrentObjectToMouse = function() {
    if (this._currentObject == null) {
        return false;
    }
    var obj = this._currentObject;
    // place centered on mouse position
    var size = obj.Size().Scale(this._options.grid);
    var pos = this._mousePosition.Copy();
    pos.x -= size.width / 2;
    pos.y -= size.height / 2;
    pos.Scale(1/this._options.grid, true);
    if (obj.left != pos.x || obj.top != pos.y) {
        this._currentObject.Position(pos);
        return true;
    }
    return false;
};

Designer.prototype._tryPlaceCurrentObject = function() {
    if (this._currentObject != null) {
        // check for collisions
        var rect = this._currentObject.Rect();
        for (var i = 0; i < this._objects.length; i++) {
            if (this._objects[i].Rect().IntersectsWith(rect)) {
                return false;
            }
        }
        var copy = Building.FromObject(this._currentObject);
        // add borderless objects at the start of the array, because they should be drawn first
        if (copy.borderless) {
            this._objects.unshift(copy);
        } else {
            this._objects.push(copy);
        }
        return true;
    }
    return false;
};

// ** Event handling
Designer.prototype._registerEvents = function () {
    var $this = this;
    function overwriteEvent(eventName, handler) {
        $($this._canvas).bind(eventName + ".designer", function (e) {
            if (handler) {
                handler.apply($this, [e]);
            }
            e.preventDefault();
            return false;
        });
    }
    // register mouse events
    overwriteEvent("mousedown", this._onMouseDown);
    overwriteEvent("mousemove", this._onMouseMove);
    overwriteEvent("mouseup", this._onMouseUp);
    overwriteEvent("dblclick", this._onMouseDblClick);
    overwriteEvent("mousewheel", this._onMouseWheel);
    overwriteEvent("mouseout", this._onMouseOut);
    overwriteEvent("contextmenu");
    // register keboard events
    overwriteEvent("keydown", this._onKeyDown);
};

Designer.prototype._unregisterEvents = function () {
    // remove all events
    $(this._canvas).unbind(".designer")
};

Designer.prototype._handleMousePosition = function (e) {
    this._mousePosition = new Point(e.offsetX, e.offsetY);
    return this._mousePosition;
};

Designer.prototype._handleMouseButtons = function (e, pressed) {
    switch (e.which) {
        case MouseButton.Left: this._mouseButtons.left = pressed; break;
        case MouseButton.Middle: this._mouseButtons.middle = pressed; break;
        case MouseButton.Right: this._mouseButtons.right = pressed; break;
    }
};

Designer.prototype._onMouseDown = function (e) {
    // the canvas has to be focused, otherwise key events will not work
    this._canvas.focus();
    var pos = this._handleMousePosition(e);
    var render = this._moveCurrentObjectToMouse();
    this._handleMouseButtons(e, true);
    trace("mousedown " + this._mouseButtons.toString());
    this._mouseDragStart = pos.Copy();
    var buttons = this._mouseButtons;
    if (buttons.left && buttons.right) {
        this._state = Designer.State.DragAllStart;
    } else if (buttons.left && this._currentObject != null) {
        if (this._tryPlaceCurrentObject()) {
            render = true;
        }
    } else if (buttons.left && this._currentObject == null) {
        var obj = this._findObjectAtPosition(pos);
        if (obj == null) {
            // user clicked in empty space: start dragging selecion rectangle
            this._state = Designer.State.SelectionRectStart;
            render = true;
        } else if (!e.ctrlKey && !e.shiftKey) {
            // user clicked on object:
            // - if it is selected, start dragging all selected objects
            // - if it is not, start dragging just that single object
            this._state = this._selectedObjects.contains(obj) ?
                Designer.State.DragSelectionStart : Designer.State.DragSingleStart;
        }
    }
    // re-render if necessary
    if (render) {
        this.Render();
    }
};

Designer.prototype._onMouseMove = function (e) {
    var pos = this._handleMousePosition(e);
    var render = this._moveCurrentObjectToMouse();
    trace("mousemove " + this._mouseButtons.toString());
    var dragPos = this._mouseDragStart;
    var buttons = this._mouseButtons;
    // check if user begins to drag
    if (this._state != Designer.State.Standard && (Math.abs(dragPos.x - pos.x) > 1 || Math.abs(dragPos.y - pos.y) > 1)) {
        switch (this._state) {
            case Designer.State.SelectionRectStart:
                this._state = Designer.State.SelectionRect;
                this._selectionRect = Rect.FromPoints(dragPos, pos);
                break;
            case Designer.State.DragSelectionStart:
                this._state = Designer.State.DragSelection;
                break;
            case Designer.State.DragSingleStart:
                this._selectedObjects = [this._findObjectAtPosition(dragPos)];
                this._state = Designer.State.DragSelection;
                break;
            case Designer.State.DragAllStart:
                this._state = Designer.State.DragAll;
                break;
        }
    }
    var grid = this._options.grid;
    // drag delta
    var dis;
    function GetDragDelta() {
        var dis = pos.Copy();
        dis.x -= dragPos.x;
        dis.y -= dragPos.y;
        return dis.Scale(1/grid);
    }
    var i, j, obj;
    switch (this._state) {
        default:
            if (this._currentObject == null) {
                obj = this._findObjectAtPosition(pos);
                if (this._hoveredObject != obj) {
                    this._hoveredObject = obj;
                    render = true;
                }
            }
            if (buttons.left && this._currentObject != null) {
                // keep placing objects when moving the mouse while the left button is pressed
                if (this._tryPlaceCurrentObject()) {
                    render = true;
                }
            }
            break;
        case Designer.State.SelectionRect:
            if (e.ctrlKey || e.shiftKey) {
                // remove previously selected by the selection rect
                // iterate backwards, because the array is modifed during the loop and indexes would shift
                for (j = this._selectedObjects.length-1; j >= 0; j--) {
                    obj = this._selectedObjects[j];
                    if (obj.Rect().Scale(this._options.grid).IntersectsWith(this._selectionRect)) {
                        this._selectedObjects.remove(obj);
                    }
                }
            } else {
                this._selectedObjects = [];
            }
            // adjust rect
            //TODO: snap selection rect to grid and reduce redraws?
            this._selectionRect = Rect.FromPoints(this._mouseDragStart, this._mousePosition);
            // select intersecting objects
            for (j = 0; j < this._objects.length; j++) {
                obj = this._objects[j];
                if (!this._selectedObjects.contains(obj) &&
                    obj.Rect().Scale(this._options.grid).IntersectsWith(this._selectionRect)) {
                    this._selectedObjects.push(obj);
                }
            }
            render = true;
            break;
        case Designer.State.DragSelection:
            // drag selection objects around, always checking for collisions
            dis = GetDragDelta();
            // check if the mouse has moved at least one grid cell in any direction
            if (dis.x == 0 && dis.y == 0) {
                break;
            }
            // move selected objects
            for (i = 0; i < this._selectedObjects.length; i++) {
                obj = this._selectedObjects[i];
                obj.left += dis.x;
                obj.top += dis.y;
            }
            // check for collisions with unselected objects
            var collision = false;
            for (i = 0; i < this._objects.length; i++) {
                if (this._selectedObjects.contains(this._objects[i])) {
                    continue;
                }
                var rect = this._objects[i].Rect();
                for (j = 0; j < this._selectedObjects.length; j++) {
                    if (this._selectedObjects[j].Rect().IntersectsWith(rect)) {
                        collision = true;
                        break;
                    }
                }
                if (collision){
                    break;
                }
            }
            if (collision) {
                // roll back movement on collision
                for (i = 0; i < this._selectedObjects.length; i++) {
                    obj = this._selectedObjects[i];
                    obj.left -= dis.x;
                    obj.top -= dis.y;
                }
            } else {
                // adjust the drag start to compensate the amount we already moved
                dis.Scale(grid);
                this._mouseDragStart.x += dis.x;
                this._mouseDragStart.y += dis.y;
                render = true;
            }
            break;
        case Designer.State.DragAll:
            dis = GetDragDelta();
            // check if the mouse has moved at least one grid cell in any direction
            if (dis.x == 0 && dis.y == 0) {
                break;
            }
            // move all objects
            for (i = 0; i < this._objects.length; i++) {
                this._objects[i].left += dis.x;
                this._objects[i].top += dis.y;
            }
            // adjust the drag start to compensate the amount we already moved
            dis.Scale(grid);
            this._mouseDragStart.x += dis.x;
            this._mouseDragStart.y += dis.y;
            render = true;
            break;
    }
    // re-render if necessary
    if (render) {
        this.Render();
    }
};

Designer.prototype._onMouseUp = function (e) {
    var pos = this._handleMousePosition(e);
    this._handleMouseButtons(e, false);
    trace("mouseup " + this._mouseButtons.toString());
    var buttons = this._mouseButtons;
    if (this._state == Designer.State.DragAll) {
        if (!buttons.left || !buttons.right) {
            this._state = Designer.State.Standard;
        }
        return;
    }
    var obj;
    // left button up
    if (e.which == MouseButton.Left && this._currentObject == null) {
        switch (this._state) {
            default:
                // clear selection of no control key is pressed
                if (!e.ctrlKey && !e.shiftKey) {
                    this._selectedObjects = [];
                }
                obj = this._findObjectAtPosition(pos);
                if (obj != null) {
                    // user clicked an object: (de-)select it
                    if (this._selectedObjects.contains(obj)) {
                        this._selectedObjects.remove(obj);
                    } else {
                        this._selectedObjects.push(obj);
                    }
                }
                break;
            case Designer.State.SelectionRect:
            case Designer.State.DragSelection:
                break;
        }
        // return to standard state
        this._state = Designer.State.Standard;
    }
    // right button up
    if (e.which == MouseButton.Right && this._state == Designer.State.Standard) {
        if (this._currentObject == null) {
            obj = this._findObjectAtPosition(pos);
            if (obj == null) {
                if (!e.ctrlKey && !e.shiftKey) {
                    // use right clicked in empty space: clear selection
                    this._selectedObjects = [];
                }
            } else {
                // user right clicked an existing object: remove it
                this._objects.remove(obj);
                this._selectedObjects.remove(obj);
            }
        } else {
            // cancel placement
            this._currentObject = null;
        }
    }
    // rotate current object
    if (e.which == MouseButton.Middle && this._currentObject != null) {
        obj = this._currentObject;
        var tmp = obj.width;
        obj.width = obj.height;
        obj.height = tmp;
    }
    this.Render();
};

Designer.prototype._onMouseDblClick = function (e) {
    var pos = this._handleMousePosition(e);
    trace("mousedblclick " + this._mouseButtons.toString());
    if (this._currentObject == null) {
        this._setCurrentProperties(this._findObjectAtPosition(pos));
        this.ApplyCurrentObject();
        this.Render();
    }
};

Designer.prototype._onMouseWheel = function(e) {
    trace("mousewheel");
    var delta = event.wheelDelta/50 || -event.detail;
    this._options.grid = Math.round(this._options.grid * (delta < 0 ? 1/this._options.zoomSpeed : this._options.zoomSpeed));
    if (this._options.grid < 1)
    {
        this._options.grid = 1;
    }
    if (this._options.autoSize) {
        this.AutoSize();
    }
    this.Render();
};

Designer.prototype._onMouseOut = function (e) {
    trace("mouseout");
    this._hoveredObject = null;
    this._mousePosition = null;
    this.Render();
};

Designer.prototype._onKeyDown = function (e) {
    trace("keydown [" + e.keyCode + "]");
    switch (e.keyCode) {
        // delete key
        case 46:
            if (this._selectedObjects.length == 0) {
                break;
            }
            // delete currently selected objects
            for (var i = 0; i < this._selectedObjects.length; i++) {
                this._objects.remove(this._selectedObjects[i]);
            }
            this._selectedObjects = [];
            this.Render();
            break;
    }
};

// ** Rendering
Designer.prototype.Render = function () {
    // shorthand definitions
    var o = this._options;
    var ctx = this._ctx;
    // reset transform
    ctx.setTransform(1, 0, 0, 1, 0, 0);
    // clear the whole canvas (transparent)
    ctx.clearRect(0, 0, this._canvas.width, this._canvas.height);
    // render grid
    if (o.drawGrid) {
        this._renderGrid();
    }
    // skip the rest if no data is present
    if (this._objects == null) {
        return;
    }
    var i;
    // draw current objects
    for (i = 0; i < this._objects.length; i++) {
        this._renderObject(this._objects[i]);
    }
    // draw highlights
    var obj;
    for (i = 0; i < this._selectedObjects.length; i++) {
        obj = this._selectedObjects[i];
        ctx.lineWidth = 2;
        ctx.strokeStyle = "#00FF00";
        this._strokeRect(obj.Rect().Scale(o.grid));
    }
    // draw hovered object highlight
    if (this._hoveredObject != null) {
        ctx.lineWidth = 2;
        ctx.strokeStyle = "#FFFF00";
        this._strokeRect(this._hoveredObject.Rect().Scale(o.grid));
    }
    // draw "ghost" if currently placing a new object
    if (this._currentObject != null) {
        this._renderObject(this._currentObject);
    }
    if (this._state == Designer.State.SelectionRect) {
        ctx.fillStyle = "#FFFF00";
        ctx.globalAlpha = 0.4;
        this._fillRect(this._selectionRect);
        ctx.globalAlpha = 1;
    }
    // output debug information
    for (var s in Designer.State) {
        if (this._state == Designer.State[s]) {
            $("#debugState").html(s);
        }
    }
    $("#debugFrameCount").html(++this._framesRendered);
};

Designer.prototype._renderGrid = function () {
    var ctx = this._ctx;
    var grid = this._options.grid;
    var maxWidth = this._options.width * grid;
    var maxHeight = this._options.height * grid;
    // translate half pixel to the right and down to achieve pixel perfect lines
    ctx.translate(0.5, 0.5);
    ctx.strokeStyle = "#000000";
    ctx.lineWidth = 1;
    ctx.beginPath();
    for (var x = 0; x < this._canvas.width; x += grid) {
        // vertical lines
        ctx.moveTo(x, 0);
        ctx.lineTo(x, maxHeight);
    }
    for (var y = 0; y < this._canvas.height; y += grid) {
        // horizontal lines
        ctx.moveTo(0, y);
        ctx.lineTo(maxWidth, y);
    }
    ctx.stroke();
    ctx.translate(-0.5, -0.5);
};

Designer.prototype._renderObject = function (obj) {
    var ctx = this._ctx;
    ctx.lineWidth = 1;
    ctx.fillStyle = obj.color;
    ctx.strokeStyle = "#000000";
    var rect = obj.Rect().Scale(this._options.grid);
    this._fillRect(rect);
    if (!obj.borderless) {
        this._strokeRect(rect);
    }
    ctx.fillStyle = "#000000";
    this._renderText(obj.label, obj.Position(), obj.Size());
};

Designer.prototype._renderText = function(text, point, size) {
    var ctx = this._ctx;
    ctx.textAlign = "center";
    ctx.textBaseline = "middle";
    var p = point.Copy().Scale(this._options.grid);
    var s = size.Copy().Scale(this._options.grid);
    ctx.fillText(text, p.x + s.width/2, p.y + s.height/2, s.width);
};

// ** Render helpers
Designer.prototype._strokeRect = function (rect) {
    if (this._ctx.lineWidth % 2 == 0) {
        this._ctx.strokeRect(rect.left, rect.top, rect.width, rect.height);
    } else {
        // corrects blurry lines caused by lines between two pixels
        this._ctx.translate(0.5, 0.5);
        this._ctx.strokeRect(rect.left, rect.top, rect.width, rect.height);
        this._ctx.translate(-0.5, -0.5);
    }
};

Designer.prototype._fillRect = function (rect) {
    this._ctx.fillRect(rect.left, rect.top, rect.width, rect.height);
};

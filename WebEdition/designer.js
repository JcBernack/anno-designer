/**
 * Anno Designer - Web Edition
 * Date: 09.01.13
 * @requires jQuery
 * @author Jan Christoph Bernack
 */

var Convert = {
    bool: function(str) { return str == "1" },
    int: function(str) { return parseInt(str); }
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

// Editor mouse behavior
// + MouseMove
//#  - no action active: highlight object under mouse
//   -   or start dragging (selection rect or move selected objects)
//#  - placing an object: move object to mouse
// + MouseClick (left)
//   - no action active: select objects (shift/ctrl mechanics)
//   -   or prepare dragging
//   - placing an object: place object if it fits (collision detection)
//# + MouseDblClick (left)
//#   - get properties of clicked objects
//# + MouseClick (right)
//#   - no action active: remove object at mouse
//#   - placing an object: stop placing objects
//# + MouseWheel
//#   - change zoom level (grid cell size)
// + Hotkeys:
//   - entf: remove selected objects, if any

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
    var $this = this;
    var grid = this._options.grid;
    this._resizer.resizable({
        grid: grid,
        stop: function(event, ui) {
            $this.SetSize(new Size(ui.size.width, ui.size.height).Scale(1/grid));
            $this.Render();
            $this._resizer.width($this._options.width * grid);
            $this._resizer.height($this._options.height * grid);
        }
    });
};

Designer.defaultOptions = {
    serviceUrl: "rest/layout",
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

// holds an array of all objects
Designer.prototype._objects = [];
// holds an array of all objects which are highlighted
Designer.prototype._highlightedObjects = [];
// the object which is currently being placed, or null if none
Designer.prototype._currentObject = null;
// the object currently under the mouse
Designer.prototype._hoveredObject = null;

// holds the layout object received from the server
Designer.prototype._layout = null;

Designer.prototype.Reset = function () {
    this._objects = [];
    this._highlightedObjects = [];
    this._hoveredObject = null;
    this._layout = null;
};

Designer.prototype._createButtonpane = function () {
    var $this = this;
    $.ajax({
        url: "designer_buttonpane.html",
        success: function (data) {
            $this._container.append(data);
            // find containing element
            var pane = $this._container.find(".buttonpane");
            // initialize color picker
            $.minicolors.init();
            // prepare buttons
            pane.find("#new").button({ icons: { primary: "ui-icon-document" } })
                .click(function() { $this.New(); });
            pane.find("#save").button({ icons: { primary: "ui-icon-pencil" }, disabled: true })
                .click(function() { $this.Save(); });
            pane.find("#saveas").button({ icons: { primary: "ui-icon-disk" } })
                .click(function() { $this.SaveAs(); });
			pane.find("#delete").button({ icons: { primary: "ui-icon-trash" } })
                .click(function() { $this.Delete(); });
            pane.find("#apply").button({ icons: { primary: "ui-icon-check" } })
                .click(function() { $this._currentObject = $this._getManualProperties(); });
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

Designer.prototype._setManualProperties = function(building) {
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

Designer.prototype._getManualProperties = function() {
    var bp = this._buttonpane;
    if (bp == null) {
        return null;
    }
    var b = new Building(0, 0,
        parseInt(bp.find("#width").val()),
        parseInt(bp.find("#height").val()),
        bp.find("#color").val(),
        bp.find("#label").val()
    );
    b.enableLabel = bp.find("#enableLabel")[0].checked;
    b.borderless = bp.find("#borderless")[0].checked;
    b.road = bp.find("#road")[0].checked;
    return b;
};

// ** Event handling
Designer.prototype._registerEvents = function () {
    var $this = this;
    // register mouse events
    $(this._canvas).bind("mousemove.designer", function (e) {
        $this._onMouseMove.apply($this, [e]);
    });
    $(this._canvas).bind("mouseout.designer", function (e) {
        $this._onMouseOut.apply($this, [e]);
    });
    $(this._canvas).bind("click.designer", function (e) {
        $this._onMouseClick.apply($this, [e]);
    });
    $(this._canvas).bind("dblclick.designer", function (e) {
        $this._onMouseDblClick.apply($this, [e]);
    });
    $(this._canvas).bind("contextmenu.designer", function (e) {
        $this._onMouseClickRight.apply($this, [e]);
        return false;
    });
    $(this._canvas).bind("mousewheel.designer", function (e) {
        $this._onMouseWheel.apply($this, [e]);
        e.preventDefault();
        return false;
    });
};

Designer.prototype._unregisterEvents = function () {
    // remove all events
    $(this._canvas).unbind(".designer")
};

Designer.prototype._onMouseMove = function (e) {
    var pos = new Point(e.offsetX, e.offsetY);
    var grid = this._options.grid;
    if (this._currentObject == null) {
        // find object under mouse
        this._hoveredObject = this._findObjectAtPosition(pos.Scale(1/grid));
    } else {
        // place currentObject at mouse
        this._hoveredObject = null;
        // place centered on mouse position
        var size = this._currentObject.Size().Scale(grid);
        pos.x -= size.width / 2;
        pos.y -= size.height / 2;
        this._currentObject.Position(pos.Scale(1/grid, true));
    }
    // redraw to adjust highlighting
    this.Render();
};

Designer.prototype._onMouseOut = function (e) {
    this._hoveredObject = null;
    this.Render();
};

Designer.prototype._onMouseClick = function (e) {
    if (this._currentObject != null) {
        var copy = $.extend(true, {}, this._currentObject);
        this._objects.push(copy);
        this.Render();
    }
};

Designer.prototype._onMouseDblClick = function (e) {
    if (this._currentObject == null) {
        var pos = new Point(e.offsetX, e.offsetY).Scale(1/this._options.grid);
        this._setManualProperties(this._findObjectAtPosition(pos));
        this.Render();
    }
};

Designer.prototype._onMouseClickRight = function(e) {
    // right mouse button
    if (this._currentObject != null) {
        this._currentObject = null;
    } else {
        var pos = new Point(e.offsetX, e.offsetY);
        var obj = this._findObjectAtPosition(pos.Scale(1/this._options.grid));
        this._objects.remove(obj);
    }
};

Designer.prototype._onMouseWheel = function(e) {
    // mouse wheel
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

Designer.prototype._findObjectAtPosition = function(point) {
    for (var i = 0; i < this._objects.length; i++) {
        if (this._objects[i].Rect().ContainsPoint(point)) {
            return this._objects[i];
        }
    }
    return null;
};

// ** Sizing
Designer.prototype.SetSize = function (width, height) {
    // use current dimensions if called without argument
    if (arguments.length < 2) {
        width = this._options.width;
        height = this._options.height;
    }
    // set canvas size (in pixels)
    var grid = this._options.grid;
    this._canvas.width = width * grid + 1;
    this._canvas.height = height * grid + 1;
    // remember size (in grid units)
    this._options.width = width;
    this._options.height = height;
};

// ** Layout I/O
Designer.prototype.New = function () {
    this.Reset();
    this.Render();
};

Designer.prototype.Load = function (id) {
    // load file from url and parse as json
    var $this = this;
    $.ajax({
        url: this._options.serviceUrl + "/" + id,
        type: "GET",
        dataType: "json",
        success: function (data) {
            $this._parseLayout(data);
        }
    });
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
    var name = this._buttonpane.find("#name").val();
    if (name == "") {
        alert("No name given.");
        return;
    }
    // load file from url and parse as json
    var $this = this;
    $.ajax({
        url: this._options.serviceUrl,
        type: "POST",
        dataType: "json",
        data: "data=" + JSON.stringify({
            name: name,
            objects: this._objects
        }),
        success: function () {
            alert("successfully saved");
            //TODO: refresh datatable
        }
    });
};

Designer.prototype.Delete = function () {
    // delete the currently loaded layout
    if (this._layout == null) {
        return;
    }
    //TODO: add confirmation dialog
    var $this = this;
    $.ajax({
        url: this._options.serviceUrl + "/" + this._layout.ID,
        type: "DELETE",
        dataType: "json",
        success: function (data) {
            if (!data.success) {
                alert("deletion failed");
                return;
            }
            // fire deleted event
            $this._options.layoutDeleted($this._layout.ID);
            // reset the editor
            $this.Reset();
			$this.Render();
        }
    });
};

Designer.prototype._parseLayout = function (layout) {
    this.Reset();
    this._layout = layout;
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

Designer.prototype.AutoSize = function () {
    // adjust canvas size, e.g. for changed grid-size
    // prevents collapsing to a single cell (width x height: 1x1)
    if (this._objects == null || this._objects.length == 0) {
        this.SetSize();
        return;
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
    this.SetSize(width + space, height + space);
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
    for (i = 0; i < this._highlightedObjects.length; i++) {
        obj = this._highlightedObjects[i];
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
};

// ** Render helpers
Designer.prototype._strokeRect = function (rect) {
    this._ctx.translate(0.5, 0.5);
    this._ctx.strokeRect(rect.left, rect.top, rect.width, rect.height);
    this._ctx.translate(-0.5, -0.5);
};

Designer.prototype._fillRect = function (rect) {
    this._ctx.fillRect(rect.left, rect.top, rect.width, rect.height);
};

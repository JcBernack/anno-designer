//
// Requires jQuery
//

// Editor mouse behavior
// + MouseMove
//#  - no action active: highlight object under mouse
//   -   or start dragging (selection rect or move selected objects)
//#  - placing an object: move object to mouse
// + MouseClick (left)
//   - no action active: select objects (shift/ctrl mechanics)
//   -   or prepare dragging
//   - placing an object: place object if it fits (collision detection)
// + MouseClick (left)
//   - get properties of clicked objects
// + MouseClick (right)
//   - no action active: remove object at mouse
//   - placing an object: stop placing objects
// + MouseWheel
//   - change zoom level (grid cell size)
// + Hotkeys:
//   - entf: remove selected objects, if any

var Type = {
    bool: function(str) { return str == "1" },
    int: function(str) { return parseInt(str); }
};

var Building = function(obj) {
    this.left = Type.int(obj.left);
    this.top = Type.int(obj.top);
    this.width = Type.int(obj.width);
    this.height = Type.int(obj.height);
    this.color = obj.color;
    this.label = obj.label;
    this.enableLabel = Type.bool(obj.enableLabel);
    this.borderless = Type.bool(obj.borderless);
    this.road = Type.bool(obj.road);
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
    this._resizer.resizable({
        grid: this._options.size,
        stop: function(event, ui) {
            $this.SetSize($this._toGridCoord(ui.size.width), $this._toGridCoord(ui.size.height));
            $this.Render();
            $this._resizer.width($this._toPixelCoord($this._options.width));
            $this._resizer.height($this._toPixelCoord($this._options.height));
        }
    });
};

Designer.defaultOptions = {
    serviceUrl: "rest/layout",
    containerId: "editor",
    autoSize: false,
    enableEditing: true,
    drawGrid: true,
    size: 15,
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

Designer.prototype.Reset = function () {
    this._objects = [];
    this._highlightedObjects = [];
    this._hoveredObject = null;
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
                .click(function(e) { $this.New(); });
            pane.find("#save").button({ icons: { primary: "ui-icon-pencil" }, disabled: true })
                .click(function(e) { $this.Save(); });
            pane.find("#saveas").button({ icons: { primary: "ui-icon-disk" } })
                .click(function(e) { $this.SaveAs(); });
            pane.find("#apply").button({ icons: { primary: "ui-icon-check" } })
                .click(function(e) { $this._currentObject = $this._getManualProperties(); });
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
    var b = this._buttonpane;
    var building = { };
    building.width = parseInt(b.find("#width").val());
    building.height = parseInt(b.find("#height").val());
    building.color = b.find("#color").val();
    building.label = b.find("#label").val();
    building.enableLabel = b.find("#enableLabel")[0].checked;
    building.borderless = b.find("#borderless")[0].checked;
    building.road = b.find("#road")[0].checked;
    return building;
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
    var pos = this._toGridCoord({
        x: e.offsetX,
        y: e.offsetY
    });
    if (this._currentObject == null) {
        // find object under mouse
        this._hoveredObject = this._findObjectAtPosition(pos.x, pos.y);
    } else {
        // place currentObject at mouse
        this._hoveredObject = null;
        //TODO: place centered on mouse position
        this._currentObject.left = pos.x;
        this._currentObject.top = pos.y;
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
    } else {
        var pos = this._toGridCoord({
            x: e.offsetX,
            y: e.offsetY
        });
        // find object under mouse
        for (var i = 0; i < this._objects.length; i++) {
            var o = this._objects[i];
            if (RectContainsPoint(o.left, o.top, o.width, o.height, pos.x, pos.y)) {
                this._setManualProperties(o);
                break;
            }
        }
    }
    this.Render();
};

Designer.prototype._onMouseClickRight = function(e) {
    // right mouse button
    this._currentObject = null;
};

Designer.prototype._onMouseWheel = function(e) {
    // mouse wheel
    var delta = event.wheelDelta/50 || -event.detail;
    this._options.size = Math.round(this._options.size * (delta < 0 ? 1/this._options.zoomSpeed : this._options.zoomSpeed));
    if (this._options.size < 1)
    {
        this._options.size = 1;
    }
    if (this._options.autoSize) {
        this.AutoSize();
    }
    this.Render();
};

Designer.prototype._findObjectAtPosition = function(x, y) {
    for (var i = 0; i < this._objects.length; i++) {
        var o = this._objects[i];
        if (RectContainsPoint(o.left, o.top, o.width, o.height, x, y)) {
            return o;
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
    this._canvas.width = this._toPixelCoord(width) + 1;
    this._canvas.height = this._toPixelCoord(height) + 1;
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
        }
    });
};

Designer.prototype._parseLayout = function (data) {
    this.Reset();
    // parse objects retrieved from service
    this._objects = [];
    for (var i = 0; i < data.objects.length; i++) {
        this._objects.push(new Building(data.objects[i]));
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
    // clear the whole canvas
    //ctx.fillStyle = "#000000";
    //ctx.fillRect(0, 0, this._canvas.width, this._canvas.height);
    ctx.clearRect(0, 0, this._canvas.width, this._canvas.height);
    // translate half pixel to the right and down to achieve pixel perfect lines
    //_ctx.translate(0.5, 0.5);
    var i, x, y;
    var maxWidth = Math.floor(this._canvas.width / o.size) * o.size;
    var maxHeight = Math.floor(this._canvas.height / o.size) * o.size;
    // draw grid
    if (o.drawGrid) {
        ctx.translate(0.5, 0.5);
        ctx.strokeStyle = "#000000";
        ctx.lineWidth = 1;
        ctx.beginPath();
        for (x = 0; x < this._canvas.width; x += o.size) {
            // vertical lines
            ctx.moveTo(x, 0);
            ctx.lineTo(x, maxHeight);
        }
        for (y = 0; y < this._canvas.height; y += o.size) {
            // horizontal lines
            ctx.moveTo(0, y);
            ctx.lineTo(maxWidth, y);
        }
        ctx.stroke();
        ctx.translate(-0.5, -0.5);
    }
    // skip the rest if no data is present
    if (this._objects == null) {
        return;
    }
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
        this._strokeGridRect(obj.left, obj.top, obj.width, obj.height);
    }
    // draw hovered object highlight
    if (this._hoveredObject != null) {
        ctx.lineWidth = 2;
        ctx.strokeStyle = "#FFFF00";
        obj = this._hoveredObject;
        this._strokeGridRect(obj.left, obj.top, obj.width, obj.height);
    }
    // draw "ghost" if currently placing a new object
    if (this._currentObject != null) {
        this._renderObject(this._currentObject);
    }
};

Designer.prototype._renderObject = function (obj) {
    var ctx = this._ctx;
    ctx.lineWidth = 1;
    ctx.fillStyle = obj.color;
    ctx.strokeStyle = "#000000";
    this._fillCells(obj.left, obj.top, obj.width, obj.height, obj.borderless);
};

// ** Render helpers
Designer.prototype._fillCells = function (left, top, width, height, borderless) {
    var s = this._options.size;
    this._ctx.fillRect(left * s, top * s, width * s, height * s);
    if (!borderless) {
        this._strokeGridRect(left, top, width, height);
    }
};

Designer.prototype._strokeGridRect = function (left, top, width, height) {
    var ctx = this._ctx;
    var s = this._options.size;
    ctx.translate(0.5, 0.5);
    ctx.strokeRect(left * s, top * s, width * s, height * s);
    ctx.translate(-0.5, -0.5);
};
// ** Coordinate helpers
Designer.prototype._convertCoordinate = function (value, factor) {
    var i, result;
    if (typeof value === "number") {
        result = Math.floor(value * factor);
    } else if (value instanceof Array) {
        result = [];
        for (i = 0; i < value.length; i++) {
            result[i] = this._convertCoordinate(value[i], factor);
        }
    } else if (typeof value == "object") {
        // convert each property
        result = {};
        for (i in value) {
            result[i] = this._convertCoordinate(value[i], factor);
        }
    } else {
        alert("dafuq is that?");
    }
    return result;
};

Designer.prototype._toGridCoord = function (value) {
    return this._convertCoordinate(value, 1 / this._options.size);
};

Designer.prototype._toPixelCoord = function (value) {
    return this._convertCoordinate(value, this._options.size);
};

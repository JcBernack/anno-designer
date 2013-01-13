/**
 * Geometry handling
 * Date: 11.01.13
 * @author Jan Christoph Bernack
 */

var Point = function (x, y) {
    this.x = x;
    this.y = y;
};

Point.prototype.Scale = function (factor) {
    var f = arguments[1] ? Math.round : Math.floor;
    this.x = f(this.x * factor);
    this.y = f(this.y * factor);
    return this;
};

var Size = function (width, height) {
    this.width = width;
    this.height = height;
};

Size.prototype.Scale = function (factor) {
    var f = arguments[1] ? Math.round : Math.floor;
    this.width = f(this.width * factor);
    this.height = f(this.height * factor);
    return this;
};

var Rect = function (left, top, width, height) {
    this.left = left;
    this.top = top;
    this.width = width;
    this.height = height;
};

Rect.FromPointSize = function (point, size) {
    return new Rect(point.x, point.y, size.width, size.height);
};

Rect.prototype.Position = function (point) {
    if (point) {
        this.left = point.left;
        this.top = point.top;
        return this;
    } else {
        return new Point(this.left, this.top);
    }
};

Rect.prototype.Size = function () {
    return new Size(this.width, this.height);
};

Rect.prototype.Scale = function (factor) {
    var f = arguments[1] ? Math.round : Math.floor;
    this.left = f(this.left * factor);
    this.top = f(this.top * factor);
    this.width = f(this.width * factor);
    this.height = f(this.height * factor);
    return this;
};

Rect.prototype.ContainsPoint = function(point) {
    return this.left <= point.x && point.x < this.left + this.width &&
        this.top <= point.y && point.y < this.top + this.height;
};
function RectContainsPoint(left, top, width, height, x, y) {
    return left <= x && x < left + width && top <= y && y < top + height;
}

function length(x, y)
{
    return Math.sqrt(x*x + y*y);
}

Array.prototype.contains = function(obj) {
    var i = this.length;
    while (i--) {
        if (this[i] === obj) {
            return true;
        }
    }
    return false;
};

function trace(msg)
{
    if (typeof console != 'undefined' && typeof console.log != 'undefined')
    {
        console.log(msg);
    }
}
// from http://stackoverflow.com/questions/4576724/dotted-stroke-in-canvas
var CP = window.CanvasRenderingContext2D && CanvasRenderingContext2D.prototype;
if (CP.lineTo) {
    CP.dashedLine = function(x, y, x2, y2, da) {
        if (!da) da = [10,5];
        this.save();
        var dx = (x2-x), dy = (y2-y);
        var len = Math.sqrt(dx*dx + dy*dy);
        var rot = Math.atan2(dy, dx);
        this.translate(x, y);
        this.moveTo(0, 0);
        this.rotate(rot);
        var dc = da.length;
        var di = 0, draw = true;
        x = 0;
        while (len > x) {
            x += da[di++ % dc];
            if (x > len) x = len;
            draw ? this.lineTo(x, 0): this.moveTo(x, 0);
            draw = !draw;
        }
        this.restore();
    }
}
if (CP.arc) {
    CP.arcMoveTo = function(x, y, radius, angle) {
        this.moveTo(x+radius*Math.cos(angle), y+radius*Math.sin(angle));
    }
    CP.dashedArc = function(x, y, radius, angleStart, angleEnd, da) {
        if (!da) da = [10,5];
        var len = 2*Math.PI*radius;
        var dc = da.length
        var di = 0;
        var draw = true;
        var angle = angleStart;
        while (angle < angleEnd)
        {
            var angleStep = angle + da[di++ % dc]/radius;
            if (angleStep > angleEnd) angleStep = angleEnd;
            if (draw)
            {
                this.arc(x, y, radius, angle, angleStep);
            }
            else
            {
                this.arcMoveTo(x, y, radius, angleStep);
            }
            angle = angleStep;
            draw = !draw;
        }
    }
}
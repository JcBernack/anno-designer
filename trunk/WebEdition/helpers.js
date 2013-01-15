// useful helper methods

function trace(msg)
{
//    if (typeof console != 'undefined' && typeof console.log != 'undefined')
//    {
//        console.log(msg);
//    }
    // add new line to console and count lines
    var con = $("#debugConsole");
    var total = (con.text() + msg + "\n").split("\n");
    // remove first lines, when there are too many
    var max = 50;
    if (total.length > max) {
        total = total.slice(total.length - max);
    }
    // set text
    con.text(total.join("\n"));
    // scroll down
    con.scrollTop(con[0].scrollHeight - con.height());
}

function Rest(method, url, data, success) {
    return $.ajax({
        url: url,
        type: method,
        dataType: "json",
        data: data,
        success: success
    });
}

Array.prototype.last = function () {
    return this[this.length - 1];
};

Array.prototype.remove = function (element)
{
    var i = this.indexOf(element);
    if (i == -1)
    {
        return this;
    }
    this.splice(i, 1);
    return this;
};

Array.prototype.contains = function(obj) {
    var i = this.length;
    while (i--) {
        if (this[i] === obj) {
            return true;
        }
    }
    return false;
};

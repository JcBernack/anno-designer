// useful helper methods

function trace(msg)
{
    if (typeof console != 'undefined' && typeof console.log != 'undefined')
    {
        console.log(msg);
    }
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

function Rest(method, url, data, success) {
    return $.ajax({
        url: url,
        type: method,
        dataType: "json",
        data: data,
        success: success
    });
}

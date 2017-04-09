; (function ($, window, document, undefined) {

    "use strict";

    // boilderplate based on
    // https://github.com/jquery-boilerplate/jquery-boilerplate/blob/master/dist/jquery.boilerplate.js

    // basic idea from:
    // https://seesparkbox.com/foundry/how_i_built_a_canvas_color_picker

    // Create the defaults once
    var pluginName = "colorPalette";
    var defaults = {
        onChange: function (sender, data) { console.log("change"); },
        onBegin: function (sender, data) { console.log("begin"); },
        onDone: function (sender, data) { console.log("done"); },
        updateInterval: 5
    };

    // Plugin Constructor
    function ColorPalette(element, options) {

        this.element = element;
        this.$element = $(element);
        this.$element.css("cursor", "pointer");

        if (typeof this.element.getContext !== "function")
            throw "Selected element " + this.element + " is not of type canvas.";

        this.graphics = null;
        this.eventTimer = null;
        this.position = { X: 0, Y: 0 };
        this.color = { R: 0, G: 0, B: 0 };
        this.isPainting = false;
        this.isMouseDown = false;
        this.size = { X: 0, Y: 0 };

        this.settings = $.extend({}, defaults, options);
        this._defaults = defaults;
        this._name = pluginName;

        this.init();
    }

    // Avoid prototype conflicts
    $.extend(ColorPalette.prototype, {
        init: function () {
            console.log("init");
            this.buildColorPalette();
            this.bindEvents();
        },
        buildColorPalette: function () {
            if (this.isPainting === true)
                return;

            this.isPainting = true;

            // Compute dimensions
            this.size.X = this.$element.width();
            this.size.Y = this.$element.height();
            this.element.width = this.size.X;
            this.element.height = this.size.Y;
            this.graphics = this.element.getContext('2d');
            this.graphics.clearRect(0, 0, this.size.X, this.size.Y);

            // Create a gradient the width of the canvas
            var gradient = this.graphics.createLinearGradient(0, 0, this.size.X, 0);

            // add color stops (solid colors)
            gradient.addColorStop(0, "rgb(255, 0, 0)");
            gradient.addColorStop(0.15, "rgb(255, 0, 255)");
            gradient.addColorStop(0.33, "rgb(0, 0, 255)");
            gradient.addColorStop(0.49, "rgb(0, 255, 255)");
            gradient.addColorStop(0.67, "rgb(0, 255,   0)");
            gradient.addColorStop(0.84, "rgb(255, 255, 0)");
            gradient.addColorStop(1, "rgb(255, 0, 0)");

            // Apply gradient to canvas
            this.graphics.fillStyle = gradient;
            this.graphics.fillRect(0, 0, this.size.X, this.size.Y);

            // Create semi transparent gradient (white to transparent to black)
            gradient = this.graphics.createLinearGradient(0, 0, 0, this.size.Y);
            gradient.addColorStop(0, "rgba(255, 255, 255, 1)");
            gradient.addColorStop(0.5, "rgba(255, 255, 255, 0)");
            gradient.addColorStop(0.5, "rgba(0, 0, 0, 0)");
            gradient.addColorStop(1, "rgba(0, 0, 0, 1)");

            // Apply gradient to canvas
            this.graphics.fillStyle = gradient;
            this.graphics.fillRect(0, 0, this.size.X, this.size.Y);

            this.isPainting = false;
        },
        getEventData: function () {
            var control = this;

            var eventData = {
                color: { R: control.color.R, G: control.color.G, B: control.color.B },
                rgbColor: 'rgb(' + control.color.R + ', ' + control.color.G + ', ' + control.color.B + ')',
                hexColor: control.rgb2hex(control.color),
                position: { X: control.position.X, Y: control.position.Y }
            };
            return eventData;
        },
        pollDataCallback: function () {
            var control = this;
            try {

                var imageData = control.graphics.getImageData(control.position.X, control.position.Y, 1, 1).data;
                control.color.R = control.clampInt(imageData[0], 0, 255);
                control.color.G = control.clampInt(imageData[1], 0, 255);
                control.color.B = control.clampInt(imageData[2], 0, 255);

                var eventData = control.getEventData();
                control.$element.trigger("change", [eventData]);
                if (typeof control.settings.onChange === "function")
                    control.settings.onChange(control, eventData);

            }
            catch (ex) {
                //console.error("No image data on: X: " + control.position.X + ', Y: ' + control.position.Y);
            }
        },
        bindEvents: function () {
            var control = this;

            // Bind: Mouse Move
            $(document).bind('mousemove touchmove', function (e) {

                if (control.isMouseDown !== true)
                    return;

                if (e.touches !== undefined && e.touches.length > 0) {
                    e.pageX = e.touches[0].pageX;
                    e.pageY = e.touches[0].pageY;
                }

                var x = e.pageX - control.$element.offset().left;
                var y = e.pageY - control.$element.offset().top;

                // clamp the values
                control.position.X = control.clampInt(x, 0, control.size.X);
                control.position.Y = control.clampInt(y, 0, control.size.Y);

            });

            // Bind: Mouse Up
            $(document).bind('mouseup touchend', function (e) {

                if (control.isMouseDown !== true)
                    return;

                control.isMouseDown = false;

                e.preventDefault();
                e.stopPropagation();

                clearInterval(control.eventTimer);

                var eventData = control.getEventData();
                control.$element.trigger("done", [eventData]);
                if (typeof control.settings.onDone === "function")
                    control.settings.onDone(control, eventData);
            });

            // Bind: Mouse Down
            control.$element.bind('mousedown touchstart', function (e) {
                if (control.isMouseDown === true)
                    return;

                control.isMouseDown = true;

                e.preventDefault();

                // rebuild graphics if size has changed
                if (control.$element.width() != control.size.X || control.$element.height() != control.size.Y) {
                    control.buildColorPalette();
                    console.warn("Palette was rebuilt because size was changed");
                }

                // Event: Begin
                control.pollDataCallback();
                var eventData = control.getEventData();
                control.$element.trigger("begin", [eventData]);
                if (typeof control.settings.onBegin === "function")
                    control.settings.onBegin(control, eventData);

                // Start polling the color at the current mouse coordinates
                clearInterval(control.eventTimer);
                control.eventTimer = setInterval(function () { control.pollDataCallback(); }, control.settings.updateInterval);
            });
        },
        clampInt: function (number, min, max) {
            if (number == undefined || number == null || isNaN(number))
                number = min;

            if (number < min) number = min;
            if (number > max) number = max;

            return number | 0;
        },
        rgb2hex: function (rgb) {
            return (rgb) ? "#" +
                ("0" + parseInt(rgb.R, 10).toString(16)).slice(-2) +
                ("0" + parseInt(rgb.G, 10).toString(16)).slice(-2) +
                ("0" + parseInt(rgb.B, 10).toString(16)).slice(-2) : '';
        },
    });

    // Register the plugin and prevent multiple instantiations
    $.fn[pluginName] = function (options) {
        return this.each(function () {
            if (!$.data(this, pluginName)) {
                $.data(this, pluginName,
                    new ColorPalette(this, options));
            }
        });
    };

    $.fn[pluginName + "Obj"] = function () {
        return $(this).data(pluginName);
    };

})(jQuery, window, document);


var app = {};

$(function () {

    "use strict";

    String.prototype.capitalizeFirstLetter = function () {
        return this.charAt(0).toUpperCase() + this.slice(1);
    };

    app.ByteToHex = function(c) {
        var hex = c.toString(16);
        return hex.length == 1 ? "0" + hex : hex;
    };

    app.Alert = function(message) {
      alert(message);  
    };

    app.CreateColorEventData = function(r, g, b, x, y) {
        var eventData = {
            color: { R: r, G: g, B: b },
            rgbColor: 'rgb(' + r + ',' + g + ',' + b + ')',
            hexColor: '#' + app.ByteToHex(r) + app.ByteToHex(g) + app.ByteToHex(b),
            position: { X: x, Y: y }
        };

        return eventData;
    };

    app.Constants = {
        SectionDataAttribute: 'section',
        SectionElementSuffix: 'Container',
        Sections: {
            Status: 'status',
            SimpleMode: 'simpleMode',
            TransitionMode: 'transitionMode',
            CustomImageMode: 'customImageMode'
        }
    };

    app.Templates = {
        StatusView: '',
        SimpleModeView: '',
        TransitionModeView: '',
        CustomImageModeView: '',
        SimpleModePresetView: '',
    };

    app.AjaxManager = {
        requests: [],
        addReq: function (opt) {
            this.requests.push(opt);

            if (this.requests.length === 1) {
                this.run();
            }
        },
        removeReq: function (opt) {
            if ($.inArray(opt, requests) > -1)
                this.requests.splice($.inArray(opt, requests), 1);
        },
        run: function () {
            // original complete callback
            var oricomplete = this.requests[0].complete;

            // override complete callback
            var ajxmgr = this;
            ajxmgr.requests[0].complete = function () {
                if (typeof oricomplete === 'function')
                    oricomplete();

                ajxmgr.requests.shift();
                if (ajxmgr.requests.length > 0) {
                    ajxmgr.run();
                }
            };

            $.ajax(this.requests[0]);
        },
        stop: function () {
            this.requests = [];
        }
    };

    app.AppState = {};

    app.HideAllSections = function () {
        $('div.container.section').hide();
        $("ul.nav li a[data-section]").closest('li').removeClass('active');
    };

    app.ShowSection = function (sender) {
        app.HideAllSections();
        var sectionName = typeof sender === 'string' ? sender : $(sender).data(app.Constants.SectionDataAttribute);
        $('#' + sectionName + app.Constants.SectionElementSuffix).fadeIn();
        $("ul.nav li a[data-" + app.Constants.SectionDataAttribute + "='" + sectionName + "']").closest('li').addClass('active');

        var sectionValue = app.Constants.Sections[sectionName.capitalizeFirstLetter()];
        if (sectionValue) {
            // TODO load stuff that needs reloading here
        }

        return sectionName;
    };

    app.LoadTemplates = function () {
        var tpls = $('#templates');
        return tpls.load('/views.html', function () {

            // Main Sections
            app.Templates.StatusView = Handlebars.compile(tpls.find('#status-view').html());
            app.Templates.SimpleModeView = Handlebars.compile(tpls.find('#simplemode-view').html());
            app.Templates.TransitionModeView = Handlebars.compile(tpls.find('#transitionmode-view').html());
            app.Templates.CustomImageModeView = Handlebars.compile(tpls.find('#customimagemode-view').html());
            app.Templates.SimpleModePresetView = Handlebars.compile(tpls.find('#simplemode-preset-view').html());

            // start the app
            app.InitializeNavigation();
        });
    };

    app.InitializeNavigation = function () {
        // Load section templates
        var keys = Object.keys(app.Constants.Sections);
        for (var i = 0; i < keys.length; i++) {
            var sectionName = app.Constants.Sections[keys[i]];
            var templateName = sectionName.capitalizeFirstLetter() + 'View';
            var template = app.Templates[templateName];
            if (template) {
                var container = $('#' + sectionName + 'Container');
                container.empty();
                container.append(template());
            }
        }

        // Navigation Links
        $('a[data-section]').attr('href', 'javascript:void(0);');
        $('a[data-section]').click(function () {
            app.ShowSection(this);
            $("#bs-navbar-collapse-1").collapse('hide');
            return false;
        });

        app.InitializeSingleColorControls();
        app.InitializeTranstionControls();

        // Show the welcome page!
        app.ShowSection(app.Constants.Sections.SimpleMode);
    };

    app.Initialize = function () {
        app.HideAllSections();
        app.LoadTemplates();
    };

    app.CreateElementFromTemplate = function (template, context) {
        context = context || {};

        var el = document.createElement('div');
        return template(context);
    };

    app.RefreshAppState = function (fromServer) {
        if (fromServer === true)
        {
            $.ajax({
                url: "api/appstate",
                data: { t: new Date().getTime() },
                async: false,
                success: function (data) {
                    app.AppState = data;
                }
            });
        }

        var $listEl = $('#solicColorModePresets'); 
        $listEl.empty();

        $.each(app.AppState.SolidColorPresets, function (index, value) {
            value.eventData = app.CreateColorEventData(value.R, value.G, value.B);
            $listEl.append(app.CreateElementFromTemplate(app.Templates.SimpleModePresetView, value));
        });

        return app.AppState;
    };

    app.AddSolidColorPreset = function() {
        var presetName = prompt("Enter a name for the preset", "");
        if (presetName == null || presetName == "") {
            return;
        }

        $.ajax({
            url: "api/preset",
            type: 'POST',
            contentType: 'application/json',
            data: JSON.stringify({ 
                Name: presetName,
                R: $('#solidColorModeValue').data('R'),
                G: $('#solidColorModeValue').data('G'),
                B: $('#solidColorModeValue').data('B')
                }),
            async: true,
            success: function (data) {
                app.AppState = data;
                app.RefreshAppState(false);
            }
        });
    };

    app.DeleteSolidColorPreset = function(presetName) {
        $.ajax({
            url: "api/preset",
            type: 'DELETE',
            contentType: 'application/json',
            data: JSON.stringify({ Name: presetName }),
            async: true,
            success: function (data) {
                app.AppState = data;
                app.RefreshAppState(false);
            }
        });
    };

    app.SetColor = function (sender, data, sendToApi) {
        $('#solidColorModeValue').css("background-color", data.hexColor);
        $('#solidColorModeValue').val(data.hexColor);
        $('#solidColorModeValue').data('R', data.color.R);
        $('#solidColorModeValue').data('G', data.color.G);
        $('#solidColorModeValue').data('B', data.color.B);

        if (sendToApi !== true)
            return;

        var payload = { F: 6, R: data.color.R, G: data.color.G, B: data.color.B };

        var currentRequests = [];
        var requestUrl = "/api/color";
        var requestLimit = 3;

        for (var i = 0; i < app.AjaxManager.requests.length; i++) {
            if (app.AjaxManager.requests[i].url === requestUrl)
                currentRequests.push(app.AjaxManager.requests[i]);
        }

        for (var n = 0; n < currentRequests.length - (requestLimit - 1); n++) {
            app.AjaxManager.removeReq(currentRequests[n]);
        }

        app.AjaxManager.addReq({
            type: "PUT",
            url: "/api/color",
            data: JSON.stringify(payload),
        });
    };

    app.InitializeSingleColorControls = function () {
        // Load the palettes
        $('#solidColorModeCanvas').colorPalette({
            onChange: function (sender, data) {
                app.SetColor(sender, data, true);
            },
            onDone: function (sender, data) {
                app.SetColor(sender, data, true);
            },
            onBegin: function (sender, data) {
                app.SetColor(sender, data, true);
            }
        });

        var eventData = app.CreateColorEventData(0, 0, 0, 0, 0);
        app.SetColor(null, eventData, false);
        var state = app.RefreshAppState(true);
    };

    app.TransitionDelaySlider = null;

    app.SetTranstion = function (sender) {
        var colors = app.GetTransitionColors();
        var delay = parseInt(app.TransitionDelaySlider.noUiSlider.get());

        var payload = {
            Colors: colors,
            Delay: delay
        };

        app.AjaxManager.addReq({
            type: "PUT",
            url: "/api/transition",
            data: JSON.stringify(payload)
        });
    };

    app.InitializeTranstionControls = function () {
        $('#transitionColorModeCanvas').colorPalette({
            onChange: function (sender, data) {
                $('#transitionColorModeValue').css("background-color", data.hexColor);
                $('#transitionColorModeValue').val(data.hexColor);
                $('#transitionColorModeValue').data('rgb', [data.color.R, data.color.G, data.color.B]);
            },
            onDone: function (sender, data) {
                app.AddTransitionColor();
            }
        });

        app.TransitionDelaySlider = $("#transitionDelaySlider")[0];

        noUiSlider.create(app.TransitionDelaySlider, {
            start: 30,
            behaviour: 'tap-drag', // Move handle on tap, bar is draggable
            tooltips: false,
            connect: [true, false],
            range: {
                'min': 1,
                'max': 300
            },
            'step': 1
        });

        app.TransitionDelaySlider.noUiSlider.on("set", function (e) {
            app.UpdateTransitionUI();
        });

        app.TransitionDelaySlider.noUiSlider.on("update", function (e) {
            app.UpdateTransitionUI();
        });

        $('#transitionColorModeAdd').click(function () { app.AddTransitionColor(); });

        $('#tansitionApplyButton').click(function () {
            app.SetTranstion(this);
        });

        app.UpdateTransitionUI();
    };

    app.UpdateTransitionUI = function () {
        var colors = app.GetTransitionColors();
        var delay = parseInt(app.TransitionDelaySlider.noUiSlider.get());
        $('#tansitionApplyButton').html('Animate ' + colors.length + ' colors over ' + delay + ' secs.');
        if (colors.length <= 0) {
            $('#tansitionApplyButton').attr('disabled', 'disabled');
        } else {
            $('#tansitionApplyButton').removeAttr('disabled');
        }
    };

    app.AddTransitionColor = function () {
        var $valueEl = $("#transitionColorModeValue");
        var $listEl = $("#transitionValues");
        if ($valueEl.val().length !== 7) // expect hexadecimal value
            return;

        var $newEl = $('<div class="transition-color-item">&nbsp;</div>');
        $newEl.css("background-color", $valueEl.val());
        $newEl.data("value", $valueEl.data('rgb'));
        $listEl.append($newEl);
        $newEl.click(function () {
            $newEl.remove();
            app.UpdateTransitionUI()
        });

        app.UpdateTransitionUI();
    };

    app.GetTransitionColors = function () {
        var $valueEl = $("#transitionValues");
        var result = [];
        $valueEl.children(".transition-color-item").each(function (i, e) {
            result.push($(e).data("value"));
        });

        if (result.length === 0) {
            $('#transitionValuesHelp').css("display", "block");
        } else {
            $('#transitionValuesHelp').css("display", "none");
        }

        return result;
    };

    $(function () {
        app.Initialize();
    });
});


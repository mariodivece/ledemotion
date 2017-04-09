
var app = {};

; $(function () {
    
    "use strict";
    
    String.prototype.capitalizeFirstLetter = function () {
        return this.charAt(0).toUpperCase() + this.slice(1);
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
        CustomImageModeView: ''
    };

    app.PendingRequests = [];

    app.IsRequestPending = function (requestName) {
        if (requestName === null) {
            return app.PendingRequests.length > 0;
        }

        return $.inArray(requestName, app.PendingRequests) >= 0;
    };

    app.RegisterRequest = function (requestName) {
        if (app.IsRequestPending(requestName))
            return false;

        app.PendingRequests.push(requestName);
        return true;
    };

    app.UnregisterRequest = function (requestName) {
        if (app.IsRequestPending(requestName) === false)
            return false;

        app.PendingRequests.splice($.inArray(requestName, app.PendingRequests), 1);
        return true;
    };

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

        // Load the palettes
        $('#solidColorModeCanvas').colorPalette({
            onChange: function (sender, data) {
                $('#solidColorModeValue').css("background-color", data.hexColor);
                $('#solidColorModeValue').val(data.hexColor);
            }
        });

        $('#transitionColorModeCanvas').colorPalette({
            onChange: function (sender, data) {
                $('#transitionColorModeValue').css("background-color", data.hexColor);
                $('#transitionColorModeValue').val(data.hexColor);
            },
            onDone: function (sender, data) {
                app.AddTransitionColor();
            }
        });

        var transitionDelaySlider = noUiSlider.create($("#transitionDelaySlider")[0], {
            start: 1500,
            behaviour: 'tap-drag', // Move handle on tap, bar is draggable
            tooltips: false,
            connect: [true, false],
            range: {
                'min': 1,
                'max': 3600
            }
        });

        transitionDelaySlider.on("set", function (e) {
            var value = e[0];
        });

        // Bind events
        $('#transitionColorModeAdd').click(function () { app.AddTransitionColor(); });

        // Show the welcome page!
        app.ShowSection(app.Constants.Sections.TransitionMode);
    };

    app.Initialize = function () {
        app.HideAllSections();
        app.LoadTemplates();
    };

    app.AddTransitionColor = function() {
        var $valueEl = $("#transitionColorModeValue");
        var $listEl = $("#transitionValues");
        if ($valueEl.val().length !== 7)
            return;

        var $newEl = $('<div class="transition-color-item">&nbsp;</div>');
        $newEl.css("background-color", $valueEl.val());
        $newEl.data("value", $valueEl.val());
        $listEl.append($newEl);
        $newEl.click(function () { $newEl.remove(); app.GetTransitionColors(); });
        app.GetTransitionColors();
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


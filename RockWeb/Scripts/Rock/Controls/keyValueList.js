﻿; (function () {
    function updateKeyValues(e) {
        var $span = e.closest('span.key-value-list');
        var newValue = '';

        $span.children('span.key-value-rows:first').children('div.controls-row').each(function (index) {
            if (newValue !== '') {
                newValue += '|';
            }

            var keyValue = $(this).children('.key-value-key').first().val();
            var valueValue = $(this).children('.key-value-value').first().val();

            // if the key or value have any magic chars ('^' or '|' or ','' ), URI encode each magic char using encodeURIComponent so that commas also get encoded
            var keyValueDelimiters = ['^', '|', ','];
            keyValueDelimiters.forEach(function (v, i, a) {
                var re = new RegExp('\\' + v, 'g');
                if (keyValue.indexOf(v) > -1) {
                    keyValue = keyValue.replace(re, encodeURIComponent(v));
                }
                if (valueValue.indexOf(v) > -1) {
                    valueValue = valueValue.replace(re, encodeURIComponent(v));
                }
            });

            newValue += keyValue + '^' + valueValue;
        });
        $span.children('input').first().val(newValue);

        raiseKeyValueList();
    }

    function removeKeyValue(e) {
        var $rows = $(e).closest('span.key-value-rows');
        $(e).closest('div.controls-row').remove();
        Rock.controls.modal.updateSize($(e));
        updateKeyValues($rows);
    }

    Sys.Application.add_load(function () {

        $('a.key-value-add').on('click', function (e) {
            e.preventDefault();
            var $keyValueList = $(this).closest('.key-value-list');
            $keyValueList.find('.key-value-rows').append($keyValueList.find('.js-value-html').val());
            Rock.controls.modal.updateSize($(this));
            $('a.key-value-remove').off('click');
            $('a.key-value-remove').on('click', function (e) {
                e.preventDefault();
                removeKeyValue(this);
            });
            $('.js-key-value-input').off('change');
            $('.js-key-value-input').on('change', function (e) {
                e.preventDefault();
                updateKeyValues($(this));
            });
            updateKeyValues($(this));
        });

        $('a.key-value-remove').off('click');
        $('a.key-value-remove').on('click', function (e) {
            e.preventDefault();
            removeKeyValue(this);
        });

        $('.js-key-value-input').off('change');
        $('.js-key-value-input').on('change', function (e) {
            e.preventDefault();
            updateKeyValues($(this));
        });
    });
})();

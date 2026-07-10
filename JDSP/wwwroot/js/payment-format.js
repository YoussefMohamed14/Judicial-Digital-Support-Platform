(function () {
    function digits(value) {
        return (value || '').toString().replace(/\D/g, '');
    }

    function formatCard(value) {
        return digits(value).slice(0, 19).replace(/(.{4})/g, '$1 - ').replace(/ - $/, '');
    }

    function formatExpiry(value) {
        var d = digits(value).slice(0, 4);
        if (d.length <= 2) return d;
        return d.slice(0, 2) + '/' + d.slice(2);
    }

    document.addEventListener('input', function (event) {
        var input = event.target;
        if (!(input instanceof HTMLInputElement)) return;

        if (input.classList.contains('js-card-number')) {
            var start = input.selectionStart || input.value.length;
            input.value = formatCard(input.value);
            input.setSelectionRange(input.value.length, input.value.length);
        }

        if (input.classList.contains('js-card-expiry')) {
            input.value = formatExpiry(input.value);
        }
    });

    document.addEventListener('DOMContentLoaded', function () {
        document.querySelectorAll('.js-card-number').forEach(function (input) {
            input.value = formatCard(input.value);
        });
        document.querySelectorAll('.js-card-expiry').forEach(function (input) {
            input.value = formatExpiry(input.value);
        });
    });
})();

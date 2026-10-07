
(function () {
    var STORAGE_KEY = 'ft-theme';

    function currentTheme() {
        return document.documentElement.getAttribute('data-bs-theme') === 'dark'
            ? 'dark'
            : 'light';
    }

    // The icon advertises the ACTION, not the state: a moon means
    // "click to go dark", so it belongs on a light page.
    function paintButton(button, theme) {
        var isDark = theme === 'dark';

        var moon = button.querySelector('.icon-moon');
        var sun = button.querySelector('.icon-sun');
        if (moon) moon.hidden = isDark;
        if (sun) sun.hidden = !isDark;

        var label = isDark ? 'Switch to light mode' : 'Switch to dark mode';
        button.setAttribute('aria-pressed', String(isDark));
        button.setAttribute('aria-label', label);
        button.setAttribute('title', label);
    }

    document.addEventListener('DOMContentLoaded', function () {
        var buttons = Array.prototype.slice.call(
            document.querySelectorAll('[data-theme-toggle]'));

        // The markup ships showing the moon. If the inline script already
        // put the page into dark mode, correct the icon before anyone sees it.
        buttons.forEach(function (button) { paintButton(button, currentTheme()); });

        buttons.forEach(function (button) {
            button.addEventListener('click', function () {
                var next = currentTheme() === 'dark' ? 'light' : 'dark';
                document.documentElement.setAttribute('data-bs-theme', next);

                // Wrapped: localStorage throws in private browsing rather
                // than returning null, and a broken theme button should
                // never take the rest of the page down with it.
                try {
                    localStorage.setItem(STORAGE_KEY, next);
                } catch (e) { /* choice just will not survive a reload */ }

                buttons.forEach(function (b) { paintButton(b, next); });
            });
        });
    });
})();

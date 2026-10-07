// Show / hide password.
//
// Any button marked data-toggle-password flips the input beside it in the
// same .input-group, swaps its eye icon, and updates the label a screen
// reader announces. Login, Register and anything added later all get the
// behaviour for free - no per-page script.
document.addEventListener('DOMContentLoaded', function () {
    document.querySelectorAll('[data-toggle-password]').forEach(function (button) {
        var eye = button.querySelector('.icon-eye');
        var eyeOff = button.querySelector('.icon-eye-off');

        button.addEventListener('click', function () {
            var field = button.closest('.input-group')?.querySelector('input');
            if (!field) return;

            // The state we are moving TO, not the one we are in.
            var nowVisible = field.type === 'password';

            field.type = nowVisible ? 'text' : 'password';

            // Open eye means "click to reveal", so it belongs on a HIDDEN
            // password - the icons are the opposite of the current state.
            if (eye) eye.hidden = nowVisible;
            if (eyeOff) eyeOff.hidden = !nowVisible;

            var label = nowVisible ? 'Hide password' : 'Show password';
            button.setAttribute('aria-pressed', String(nowVisible));
            button.setAttribute('aria-label', label);
            button.setAttribute('title', label);
        });
    });
});

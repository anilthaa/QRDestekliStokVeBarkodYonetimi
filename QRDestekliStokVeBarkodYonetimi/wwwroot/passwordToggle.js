(function () {
    document.addEventListener('click', function (e) {
        var btn = e.target.closest('.password-toggle-btn');
        if (!btn || btn.disabled) return;

        var field = btn.closest('[data-js-password-toggle]');
        if (!field) return;

        var input = field.querySelector('input');
        if (!input) return;

        e.preventDefault();

        var show = input.type === 'password';
        input.type = show ? 'text' : 'password';
        btn.setAttribute('aria-pressed', show ? 'true' : 'false');
        btn.setAttribute('aria-label', show ? 'Şifreyi gizle' : 'Şifreyi göster');

        var icon = btn.querySelector('i');
        if (icon) {
            icon.classList.toggle('fa-eye', !show);
            icon.classList.toggle('fa-eye-slash', show);
        }
    });
})();

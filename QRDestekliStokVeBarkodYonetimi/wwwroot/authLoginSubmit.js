// Başarılı doğrulama sonrası cookie için tam HTTP POST (/api/auth/login)
window.AuthLogin = (function () {
    function post(eposta, sifre, beniHatirla, returnUrl) {
        var form = document.getElementById('auth-login-form');
        if (!form) {
            return;
        }

        var epostaInput = form.querySelector('[name="Eposta"]');
        var sifreInput = form.querySelector('[name="Sifre"]');
        if (epostaInput) epostaInput.value = eposta || '';
        if (sifreInput) sifreInput.value = sifre || '';

        var existingRemember = form.querySelector('[name="BeniHatirla"]');
        if (existingRemember) {
            existingRemember.remove();
        }
        if (beniHatirla) {
            var remember = document.createElement('input');
            remember.type = 'hidden';
            remember.name = 'BeniHatirla';
            remember.value = 'true';
            form.appendChild(remember);
        }

        var returnInput = form.querySelector('[name="ReturnUrl"]');
        if (returnUrl) {
            if (returnInput) {
                returnInput.value = returnUrl;
            } else {
                var ru = document.createElement('input');
                ru.type = 'hidden';
                ru.name = 'ReturnUrl';
                ru.value = returnUrl;
                form.appendChild(ru);
            }
        } else if (returnInput) {
            returnInput.remove();
        }

        form.submit();
    }

    return { post: post };
})();

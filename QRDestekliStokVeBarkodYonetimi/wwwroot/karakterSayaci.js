// Karakter sayacı — input/textarea üzerinde anlık güncelleme (Blazor Server gecikmesi olmadan)
window.KarakterSayaci = (function () {
    var baglantilar = {};

    function girdiBul(kok) {
        return kok.querySelector('input.rz-inputtext, textarea.rz-inputtext, input, textarea');
    }

    function sayaciGuncelle(kok) {
        var input = girdiBul(kok);
        var sayac = kok.querySelector('[data-char-count]');
        if (!input || !sayac) {
            return;
        }

        var max = parseInt(sayac.getAttribute('data-char-max'), 10);
        if (isNaN(max)) {
            max = 0;
        }

        var uzunluk = (input.value || '').length;
        sayac.textContent = uzunluk + ' / ' + max + ' karakter';

        if (uzunluk > max) {
            sayac.classList.add('karakter-limitli-metin__count--over');
        } else {
            sayac.classList.remove('karakter-limitli-metin__count--over');
        }
    }

    function bagla(kokId) {
        if (!kokId || baglantilar[kokId]) {
            return !!baglantilar[kokId];
        }

        var kok = document.getElementById(kokId);
        if (!kok) {
            return false;
        }

        var input = girdiBul(kok);
        if (!input) {
            return false;
        }

        var handler = function () { sayaciGuncelle(kok); };
        input.addEventListener('input', handler);
        baglantilar[kokId] = { kok: kok, input: input, handler: handler };
        sayaciGuncelle(kok);
        return true;
    }

    function guncelle(kokId) {
        if (!kokId) {
            return false;
        }
        if (!baglantilar[kokId]) {
            return bagla(kokId);
        }
        sayaciGuncelle(baglantilar[kokId].kok);
        return true;
    }

    function kopar(kokId) {
        var kayit = baglantilar[kokId];
        if (!kayit) {
            return;
        }
        kayit.input.removeEventListener('input', kayit.handler);
        delete baglantilar[kokId];
    }

    return {
        bagla: bagla,
        guncelle: guncelle,
        kopar: kopar
    };
})();

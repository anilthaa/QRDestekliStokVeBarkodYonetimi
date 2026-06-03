// Profil resmi 1:1 kırpma (Cropper.js)
window.ProfilResimKirp = (function () {
    let cropper = null;

    function waitForImage(img) {
        return new Promise(function (resolve) {
            if (img.complete && img.naturalWidth > 0) {
                resolve();
                return;
            }
            img.addEventListener('load', function () { resolve(); }, { once: true });
            img.addEventListener('error', function () { resolve(); }, { once: true });
        });
    }

    async function baslat(imgElementId) {
        temizle();
        var img = document.getElementById(imgElementId);
        if (!img || typeof Cropper === 'undefined') {
            return;
        }
        await waitForImage(img);
        cropper = new Cropper(img, {
            aspectRatio: 1,
            viewMode: 1,
            dragMode: 'move',
            autoCropArea: 0.9,
            responsive: true,
            restore: false,
            guides: true,
            center: true,
            highlight: true,
            cropBoxMovable: true,
            cropBoxResizable: true,
            toggleDragModeOnDblclick: false
        });
    }

    function kirp() {
        if (!cropper) {
            return null;
        }
        var canvas = cropper.getCroppedCanvas({
            width: 256,
            height: 256,
            imageSmoothingEnabled: true,
            imageSmoothingQuality: 'high'
        });
        if (!canvas) {
            return null;
        }
        return canvas.toDataURL('image/jpeg', 0.9);
    }

    function temizle() {
        if (cropper) {
            cropper.destroy();
            cropper = null;
        }
    }

    return {
        baslat: baslat,
        kirp: kirp,
        temizle: temizle
    };
})();

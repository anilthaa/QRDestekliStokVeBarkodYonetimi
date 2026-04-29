// html5-qrcode wrapper for Blazor
window.BarkodTarayici = {
    _scanner: null,

    baslat: function (elementId, dotnetRef) {
        if (this._scanner) {
            this._scanner.stop().catch(() => {}).finally(() => {
                this._scanner.clear();
                this._scanner = null;
                this._baslatInternal(elementId, dotnetRef);
            });
        } else {
            this._baslatInternal(elementId, dotnetRef);
        }
    },

    _baslatInternal: function (elementId, dotnetRef) {
        this._scanner = new Html5Qrcode(elementId);
        const config = {
            fps: 10,
            qrbox: { width: 250, height: 180 },
            aspectRatio: 1.0,
            supportedScanTypes: [Html5QrcodeScanType.SCAN_TYPE_CAMERA]
        };

        this._scanner.start(
            { facingMode: "environment" },
            config,
            (decodedText) => {
                dotnetRef.invokeMethodAsync("OnBarkodOkundu", decodedText);
            },
            () => {}
        ).catch(err => {
            dotnetRef.invokeMethodAsync("OnKameraHata", err.toString());
        });
    },

    durdur: function () {
        if (this._scanner) {
            this._scanner.stop().catch(() => {}).finally(() => {
                this._scanner.clear();
                this._scanner = null;
            });
        }
    },

    isMobile: function () {
        return /Mobi|Android|iPhone|iPad|iPod/i.test(navigator.userAgent);
    }
};
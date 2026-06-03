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
        this._kameraVeBaslat(dotnetRef).catch(err => {
            dotnetRef.invokeMethodAsync("OnKameraHata", err?.message || err?.toString() || String(err));
        });
    },

    _kameraVeBaslat: async function (dotnetRef) {
        const config = this._taramaConfig();
        const camera = await this._enIyiArkaKamera();
        const onSuccess = (decodedText) => {
            dotnetRef.invokeMethodAsync("OnBarkodOkundu", decodedText);
        };

        try {
            await this._scanner.start(camera, config, onSuccess, () => {});
        } catch {
            await this._scanner.start({ facingMode: "environment" }, config, onSuccess, () => {});
        }

        this._videoAyarlariUygula();
    },

    _taramaConfig: function () {
        const mobile = this.isMobile();
        return {
            fps: mobile ? 15 : 10,
            aspectRatio: mobile ? 1.777778 : 1.333334,
            disableFlip: true,
            formatsToSupport: [
                Html5QrcodeSupportedFormats.QR_CODE,
                Html5QrcodeSupportedFormats.CODE_128,
                Html5QrcodeSupportedFormats.EAN_13,
                Html5QrcodeSupportedFormats.EAN_8,
                Html5QrcodeSupportedFormats.CODE_39
            ],
            supportedScanTypes: [Html5QrcodeScanType.SCAN_TYPE_CAMERA],
            qrbox: (viewWidth, viewHeight) => {
                const minEdge = Math.min(viewWidth, viewHeight);
                const width = Math.floor(Math.min(Math.max(minEdge * 0.7, 200), 320));
                const height = Math.floor(Math.min(Math.max(width * 0.55, 120), 200));
                return { width, height };
            }
        };
    },

    _enIyiArkaKamera: async function () {
        try {
            const cameras = await Html5Qrcode.getCameras();
            if (!cameras || cameras.length === 0) {
                return { facingMode: { exact: "environment" } };
            }

            const scored = cameras
                .map(cam => ({ id: cam.id, label: cam.label || "", score: this._kameraSkor(cam.label || "") }))
                .filter(c => c.score >= 0);

            if (scored.length === 0) {
                return { facingMode: "environment" };
            }

            scored.sort((a, b) => b.score - a.score);
            return scored[0].id;
        } catch {
            return { facingMode: { exact: "environment" } };
        }
    },

    _kameraSkor: function (label) {
        const l = label.toLowerCase();

        if (l.includes("front") || l.includes("ön") || l.includes("selfie") ||
            (l.includes("user") && !l.includes("environment"))) {
            return -1;
        }

        let score = 5;

        if (l.includes("back") || l.includes("rear") || l.includes("arka") ||
            l.includes("environment") || l.includes("facing back")) {
            score += 25;
        }

        if (l.includes("wide") || l.includes("ultra") || l.includes("geniş") || l.includes("0.6")) {
            score += 35;
        }

        if (l.includes("tele") || l.includes("periscope") || l.includes("zoom") ||
            l.includes("telephoto") || l.includes("3x") || l.includes("5x") || l.includes("10x")) {
            score -= 55;
        }

        if (l.includes("macro")) {
            score -= 15;
        }

        return score;
    },

    _videoAyarlariUygula: function () {
        const scanner = this._scanner;
        if (!scanner) return;

        const delay = this.isMobile() ? 1000 : 500;
        setTimeout(() => {
            scanner.applyVideoConstraints({
                advanced: [{ focusMode: "continuous" }]
            }).catch(() => {});

            this._zoomMinimumaCek(scanner);
        }, delay);
    },

    _zoomMinimumaCek: function (scanner) {
        if (typeof scanner.getRunningTrackCameraCapabilities !== "function") {
            return;
        }

        scanner.getRunningTrackCameraCapabilities()
            .then(caps => {
                if (!caps?.zoom) return;
                const minZoom = caps.zoom.min ?? 1;
                return scanner.applyVideoConstraints({
                    advanced: [{ zoom: minZoom }]
                });
            })
            .catch(() => {});
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

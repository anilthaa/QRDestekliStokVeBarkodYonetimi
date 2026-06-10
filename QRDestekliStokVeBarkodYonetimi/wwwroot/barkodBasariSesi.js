window.BarkodBasariSesi = {
    _audioCtx: null,

    _ensureAudio: function () {
        if (!this._audioCtx) {
            const AudioContext = window.AudioContext || window.webkitAudioContext;
            if (AudioContext) {
                this._audioCtx = new AudioContext();
            }
        }

        if (this._audioCtx?.state === "suspended") {
            this._audioCtx.resume().catch(() => {});
        }
    },

    cal: function () {
        try {
            this._ensureAudio();
            const ctx = this._audioCtx;
            if (!ctx) return;

            const osc = ctx.createOscillator();
            const gain = ctx.createGain();
            osc.connect(gain);
            gain.connect(ctx.destination);

            osc.frequency.value = 1200;
            osc.type = "sine";
            gain.gain.setValueAtTime(0.12, ctx.currentTime);
            gain.gain.exponentialRampToValueAtTime(0.001, ctx.currentTime + 0.1);

            osc.start(ctx.currentTime);
            osc.stop(ctx.currentTime + 0.1);
        } catch {
            // Ses çalınamazsa sessizce devam et
        }
    }
};

// Sunucu tarafında üretilen byte[] (base64) içeriğini tarayıcıda dosya olarak indirir.
window.DosyaIndir = {
    indir: function (dosyaAdi, contentType, base64) {
        try {
            const binary = atob(base64);
            const bytes = new Uint8Array(binary.length);
            for (let i = 0; i < binary.length; i++) bytes[i] = binary.charCodeAt(i);
            const blob = new Blob([bytes], { type: contentType || 'application/octet-stream' });
            const url = URL.createObjectURL(blob);

            const a = document.createElement('a');
            a.href = url;
            a.download = dosyaAdi || 'cikti';
            a.style.display = 'none';
            document.body.appendChild(a);
            a.click();
            document.body.removeChild(a);
            setTimeout(() => URL.revokeObjectURL(url), 4000);
        } catch (e) {
            console.error('DosyaIndir hatası:', e);
        }
    }
};

namespace QRDestekliStokVeBarkodYonetimi.Models
{
    /// <summary>
    /// Yetki seçim bileşenlerinde (dropdown / radio-group) kullanılan
    /// yetki seviyesi seçeneğini temsil eder.
    ///
    /// Değer aralığı <see cref="YetkiTipi"/> sabitleriyle örtüşür:
    ///   -1 = KalitimYok (kullanıcıya özel kaydı kaldır; tip yetkisine dön)
    ///    0 = Gizli
    ///    1 = Okuma
    ///    2 = Yazma
    /// </summary>
    public class PermissionLevelOption
    {
        /// <summary>Yetki sayısal değeri (<see cref="YetkiTipi"/> sabiti).</summary>
        public int Deger { get; set; }

        /// <summary>Kullanıcıya gösterilecek etiket.</summary>
        public string Etiket { get; set; } = string.Empty;

        /// <summary>Opsiyonel açıklama metni (tooltip vb. için).</summary>
        public string? Aciklama { get; set; }

        // ── Hazır liste ───────────────────────────────────────────────────────

        /// <summary>
        /// Kullanıcı tipi bazlı yetki ayarlama ekranlarında kullanılan seçenek listesi.
        /// (Gizli / Okuma / Yazma — kalıtım seçeneği yok)
        /// </summary>
        public static readonly IReadOnlyList<PermissionLevelOption> TipSecenekleri =
        [
            new() { Deger = YetkiTipi.Gizli, Etiket = "Gizli", Aciklama = "Sayfa menüde gösterilmez, erişim engellenir." },
            new() { Deger = YetkiTipi.Okuma, Etiket = "Okuma", Aciklama = "Sayfa görüntülenebilir; ekleme/silme butonları gizlenir." },
            new() { Deger = YetkiTipi.Yazma, Etiket = "Yazma", Aciklama = "Tam erişim; ekleme, düzenleme ve silme işlemleri serbest." },
        ];

        /// <summary>
        /// Kullanıcı bazlı yetki override ekranlarında kullanılan seçenek listesi.
        /// (Tip Yetkisi / Gizli / Okuma / Yazma)
        /// </summary>
        public static readonly IReadOnlyList<PermissionLevelOption> KullaniciSecenekleri =
        [
            new() { Deger = YetkiTipi.KalitimYok, Etiket = "Tip Yetkisi", Aciklama = "Kullanıcıya özel override kaldırılır; kullanıcı tipinin yetkisi geçerli olur." },
            new() { Deger = YetkiTipi.Gizli,      Etiket = "Gizli",       Aciklama = "Tip yetkisi ne olursa olsun bu kullanıcı için erişim engellenir." },
            new() { Deger = YetkiTipi.Okuma,      Etiket = "Okuma",       Aciklama = "Bu kullanıcı için okuma override'ı tanımlanır." },
            new() { Deger = YetkiTipi.Yazma,      Etiket = "Yazma",       Aciklama = "Bu kullanıcı için tam yazma override'ı tanımlanır." },
        ];
    }
}

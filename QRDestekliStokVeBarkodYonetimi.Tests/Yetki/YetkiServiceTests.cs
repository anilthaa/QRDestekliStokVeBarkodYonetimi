using NSubstitute;
using QRDestekliStokVeBarkodYonetimi.Models;
using QRDestekliStokVeBarkodYonetimi.Services;
using Xunit;

namespace QRDestekliStokVeBarkodYonetimi.Tests.Yetki;

/// <summary>
/// YetkiService — tüm iş mantığının kapsamlı birim testleri.
///
/// Kapsanan konular:
///  1. Öncelik kuralı: kullanıcıya özel override &gt; tip yetkisi &gt; gizli (0)
///  2. Gizli override, tip=Yazma olsa bile yetkiyi kapatır (kritik kural)
///  3. ForceReloadAsync farklı kullanıcı için no-op (admin menü bozulması bug-fix)
///  4. Sorgu metotları: GetYetki, GetYetkiBySayfaURL, ErişimVar, HasYazma, SadecOkuma
///  5. ErisebilirFormIdler sadece >= Okuma döndürür
///  6. Cache: aynı kullanıcı için tekrar LoadAsync DB'yi tekrar sorgulamaz
///  7. OnChange event tetiklenme kontrolü
///  8. Clear() durumu tamamen sıfırlar
/// </summary>
public class YetkiServiceTests : IDisposable
{
    private readonly IYetkiDataAccess _data;
    private readonly IAuthState       _auth;
    private readonly YetkiService     _sut;

    public YetkiServiceTests()
    {
        _data = Substitute.For<IYetkiDataAccess>();
        _auth = Substitute.For<IAuthState>();
        _sut  = new YetkiService(_data, _auth);

        // Varsayılan boş dönüşler — her test kendi senaryosunu ekler
        _data.GetKullaniciFormYetki(Arg.Any<int>())
             .Returns(Task.FromResult(Array.Empty<ItemKullaniciDetay>()));
        _data.GetKullanici(Arg.Any<int>())
             .Returns(Task.FromResult(Array.Empty<ItemKullanicilar>()));
        _data.GetKullaniciTipFormYetki(Arg.Any<int>())
             .Returns(Task.FromResult(Array.Empty<ItemKullaniciTipDetay>()));
        _data.GetTumFormlar()
             .Returns(Task.FromResult(Array.Empty<ItemForm>()));
    }

    public void Dispose() => _sut.Dispose();

    // ── Yardımcı metotlar ─────────────────────────────────────────────────

    private ItemKullanicilar MakeUser(int id, int tipId = 10)
        => new() { ID = id, KullaniciTip_ID = tipId };

    /// <summary>
    /// Sahte DB verisi kurarak LoadAsync'i tetikler.
    /// tipYetkiler : tip bazlı yetki satırları
    /// userYetkiler: kullanıcıya özel override satırları
    /// </summary>
    private async Task YukleAsync(
        int                     kullaniciId,
        ItemKullaniciTipDetay[] tipYetkiler,
        ItemKullaniciDetay[]    userYetkiler,
        int                     tipId = 10)
    {
        var user = MakeUser(kullaniciId, tipId);
        _auth.CurrentUser.Returns(user);
        _auth.IsAuthenticated.Returns(true);

        _data.GetKullanici(kullaniciId)
             .Returns(Task.FromResult(new[] { user }));
        _data.GetKullaniciTipFormYetki(tipId)
             .Returns(Task.FromResult(tipYetkiler));
        _data.GetKullaniciFormYetki(kullaniciId)
             .Returns(Task.FromResult(userYetkiler));

        await _sut.LoadAsync(kullaniciId);
    }

    // ════════════════════════════════════════════════════════════════════════
    // 1. Öncelik Kuralı Testleri
    // ════════════════════════════════════════════════════════════════════════

    [Fact(DisplayName = "Öncelik: sadece tip yetkisi varsa → tip değeri döner")]
    public async Task Oncelik_SadeceTipYetkisi_TipDegeriDoner()
    {
        var tipYetkiler = new[]
        {
            new ItemKullaniciTipDetay { Form_ID = 1, Yetki = YetkiTipi.Yazma },
            new ItemKullaniciTipDetay { Form_ID = 2, Yetki = YetkiTipi.Okuma }
        };

        await YukleAsync(kullaniciId: 1, tipYetkiler: tipYetkiler, userYetkiler: []);

        Assert.Equal(YetkiTipi.Yazma, _sut.GetYetki(1));
        Assert.Equal(YetkiTipi.Okuma, _sut.GetYetki(2));
    }

    [Fact(DisplayName = "Öncelik: kullanıcı override → tip yetkisini geçersiz kılar")]
    public async Task Oncelik_UserOverride_TipYetkisiniGeciyor()
    {
        var tipYetkiler  = new[] { new ItemKullaniciTipDetay { Form_ID = 1, Yetki = YetkiTipi.Okuma } };
        var userYetkiler = new[] { new ItemKullaniciDetay    { Form_ID = 1, Yetki = YetkiTipi.Yazma } };

        await YukleAsync(1, tipYetkiler, userYetkiler);

        Assert.Equal(YetkiTipi.Yazma, _sut.GetYetki(1));
    }

    [Fact(DisplayName = "Öncelik (KRİTİK): Gizli(0) override, tip=Yazma(2) olsa bile yetkiyi kapatır")]
    public async Task Oncelik_GizliOverride_TipYazmaOlsaBile_Gizli()
    {
        // Bu kural "override=0 geçerlidir" semantiğinin temelidir.
        // Tip Yazma verse bile kullanıcıya özel Gizli kaydı önceliklidir.
        var tipYetkiler  = new[] { new ItemKullaniciTipDetay { Form_ID = 5, Yetki = YetkiTipi.Yazma } };
        var userYetkiler = new[] { new ItemKullaniciDetay    { Form_ID = 5, Yetki = YetkiTipi.Gizli } };

        await YukleAsync(1, tipYetkiler, userYetkiler);

        Assert.Equal(YetkiTipi.Gizli, _sut.GetYetki(5));
    }

    [Fact(DisplayName = "Öncelik: sadece user override var, tip kaydı yok")]
    public async Task Oncelik_SadeceUserOverride_TipKaydiYok()
    {
        var userYetkiler = new[] { new ItemKullaniciDetay { Form_ID = 7, Yetki = YetkiTipi.Okuma } };

        await YukleAsync(1, tipYetkiler: [], userYetkiler: userYetkiler);

        Assert.Equal(YetkiTipi.Okuma, _sut.GetYetki(7));
    }

    [Fact(DisplayName = "Öncelik: hiç yetki kaydı yok → 0 (Gizli) döner")]
    public async Task Oncelik_HicYetkiYok_SifirDoner()
    {
        await YukleAsync(1, tipYetkiler: [], userYetkiler: []);

        Assert.Equal(0, _sut.GetYetki(999));
    }

    [Fact(DisplayName = "Öncelik: tip Gizli, user override Yazma → Yazma döner")]
    public async Task Oncelik_TipGizli_UserYazmaOverride_YazmaDoner()
    {
        var tipYetkiler  = new[] { new ItemKullaniciTipDetay { Form_ID = 3, Yetki = YetkiTipi.Gizli } };
        var userYetkiler = new[] { new ItemKullaniciDetay    { Form_ID = 3, Yetki = YetkiTipi.Yazma } };

        await YukleAsync(1, tipYetkiler, userYetkiler);

        Assert.Equal(YetkiTipi.Yazma, _sut.GetYetki(3));
    }

    // ════════════════════════════════════════════════════════════════════════
    // 2. Sorgu Metotları
    // ════════════════════════════════════════════════════════════════════════

    [Fact(DisplayName = "GetYetki: bilinmeyen formId → 0 döner")]
    public async Task GetYetki_BilinmeyenFormId_SifirDoner()
    {
        await YukleAsync(1, [], []);

        Assert.Equal(0, _sut.GetYetki(9999));
    }

    [Fact(DisplayName = "GetYetkiBySayfaURL: eşleşen URL → doğru yetki")]
    public async Task GetYetkiBySayfaURL_EslesmeBulur()
    {
        // GetTumFormlar formu önbelleğe alacak şekilde kur
        var formlar = new[] { new ItemForm { ID = 10, SayfaURL = "/urunler" } };
        _data.GetTumFormlar().Returns(Task.FromResult(formlar));

        var tipYetkiler = new[] { new ItemKullaniciTipDetay { Form_ID = 10, Yetki = YetkiTipi.Yazma } };
        await YukleAsync(1, tipYetkiler, []);
        await _sut.EnsureFormsLoadedAsync();

        Assert.Equal(YetkiTipi.Yazma, _sut.GetYetkiBySayfaURL("/urunler"));
    }

    [Fact(DisplayName = "GetYetkiBySayfaURL: büyük/küçük harf duyarsız eşleşme")]
    public async Task GetYetkiBySayfaURL_CaseInsensitive()
    {
        var formlar     = new[] { new ItemForm { ID = 10, SayfaURL = "/Urunler" } };
        var tipYetkiler = new[] { new ItemKullaniciTipDetay { Form_ID = 10, Yetki = YetkiTipi.Okuma } };

        _data.GetTumFormlar().Returns(Task.FromResult(formlar));
        await YukleAsync(1, tipYetkiler, []);
        await _sut.EnsureFormsLoadedAsync();

        Assert.Equal(YetkiTipi.Okuma, _sut.GetYetkiBySayfaURL("/URUNLER"));
    }

    [Fact(DisplayName = "GetYetkiBySayfaURL: eşleşmeyen URL → 0")]
    public async Task GetYetkiBySayfaURL_EslesmeyenURL_Sifir()
    {
        await YukleAsync(1, [], []);
        await _sut.EnsureFormsLoadedAsync();

        Assert.Equal(0, _sut.GetYetkiBySayfaURL("/olmayan-sayfa"));
    }

    [Fact(DisplayName = "GetYetkiBySayfaURL: null/boş URL → 0")]
    public async Task GetYetkiBySayfaURL_NullURL_Sifir()
    {
        await YukleAsync(1, [], []);

        Assert.Equal(0, _sut.GetYetkiBySayfaURL(null));
        Assert.Equal(0, _sut.GetYetkiBySayfaURL(""));
        Assert.Equal(0, _sut.GetYetkiBySayfaURL("   "));
    }

    // ErişimVar / SadecOkuma / HasYazma
    [Theory(DisplayName = "ErişimVar: >= Okuma ise true, Gizli ise false")]
    [InlineData(YetkiTipi.Gizli,  false)]
    [InlineData(YetkiTipi.Okuma,  true)]
    [InlineData(YetkiTipi.Yazma,  true)]
    public async Task ErisimVar_YetkiGoreTrueVeyaFalse(int yetki, bool beklenen)
    {
        const int formId = 1;
        var formlar      = new[] { new ItemForm { ID = formId, SayfaURL = "/sayfa" } };
        _data.GetTumFormlar().Returns(Task.FromResult(formlar));
        await YukleAsync(1, [new ItemKullaniciTipDetay { Form_ID = formId, Yetki = yetki }], []);
        await _sut.EnsureFormsLoadedAsync();

        Assert.Equal(beklenen, _sut.ErişimVar("/sayfa"));
    }

    [Theory(DisplayName = "HasYazma: sadece Yazma(2) ise true")]
    [InlineData(YetkiTipi.Gizli,  false)]
    [InlineData(YetkiTipi.Okuma,  false)]
    [InlineData(YetkiTipi.Yazma,  true)]
    public async Task HasYazma_YetkiGore(int yetki, bool beklenen)
    {
        const int formId = 1;
        var formlar      = new[] { new ItemForm { ID = formId, SayfaURL = "/sayfa" } };
        _data.GetTumFormlar().Returns(Task.FromResult(formlar));
        await YukleAsync(1, [new ItemKullaniciTipDetay { Form_ID = formId, Yetki = yetki }], []);
        await _sut.EnsureFormsLoadedAsync();

        Assert.Equal(beklenen, _sut.HasYazma("/sayfa"));
    }

    [Theory(DisplayName = "SadecOkuma: tam olarak Okuma(1) ise true")]
    [InlineData(YetkiTipi.Gizli,  false)]
    [InlineData(YetkiTipi.Okuma,  true)]
    [InlineData(YetkiTipi.Yazma,  false)]
    public async Task SadecOkuma_YetkiGore(int yetki, bool beklenen)
    {
        const int formId = 1;
        var formlar      = new[] { new ItemForm { ID = formId, SayfaURL = "/sayfa" } };
        _data.GetTumFormlar().Returns(Task.FromResult(formlar));
        await YukleAsync(1, [new ItemKullaniciTipDetay { Form_ID = formId, Yetki = yetki }], []);
        await _sut.EnsureFormsLoadedAsync();

        Assert.Equal(beklenen, _sut.SadecOkuma("/sayfa"));
    }

    // HasYetki (formId bazlı)
    [Fact(DisplayName = "HasYetki: formId ile Yazma yetki kontrolü")]
    public async Task HasYetki_FormIdBazli_YazmaKontrol()
    {
        await YukleAsync(1,
            [new ItemKullaniciTipDetay { Form_ID = 3, Yetki = YetkiTipi.Yazma }], []);

        Assert.True(_sut.HasYetki(3, YetkiTipi.Yazma));
        Assert.True(_sut.HasYetki(3, YetkiTipi.Okuma));
        Assert.False(_sut.HasYetki(3, minYetki: 99));
    }

    // ErisebilirFormIdler
    [Fact(DisplayName = "ErisebilirFormIdler: Gizli formlar dahil edilmez")]
    public async Task ErisebilirFormIdler_GizliDahilEdilmez()
    {
        var tipYetkiler = new[]
        {
            new ItemKullaniciTipDetay { Form_ID = 1, Yetki = YetkiTipi.Yazma },
            new ItemKullaniciTipDetay { Form_ID = 2, Yetki = YetkiTipi.Okuma },
            new ItemKullaniciTipDetay { Form_ID = 3, Yetki = YetkiTipi.Gizli }
        };
        await YukleAsync(1, tipYetkiler, []);

        var erisebilir = _sut.ErisebilirFormIdler();

        Assert.Contains(1, erisebilir);
        Assert.Contains(2, erisebilir);
        Assert.DoesNotContain(3, erisebilir);
    }

    [Fact(DisplayName = "ErisebilirFormIdler: yüklenmeden önce boş koleksiyon")]
    public void ErisebilirFormIdler_Yuklenmeyince_Bos()
    {
        Assert.Empty(_sut.ErisebilirFormIdler());
    }

    // ════════════════════════════════════════════════════════════════════════
    // 3. Cache Davranışı
    // ════════════════════════════════════════════════════════════════════════

    [Fact(DisplayName = "LoadAsync: aynı kullanıcı tekrar çağrılınca DB tekrar sorgulanmaz")]
    public async Task LoadAsync_AyniKullanici_TekDbSorgusu()
    {
        await YukleAsync(1, [], []);

        // İkinci çağrı — zaten yüklü olduğu için DB'ye gitmemeli
        await _sut.LoadAsync(1);

        // Her metot yalnızca 1 kez çağrılmış olmalı
        await _data.Received(1).GetKullaniciFormYetki(1);
    }

    [Fact(DisplayName = "LoadAsync: ID <= 0 → Clear çağrılır, IsLoaded=false")]
    public async Task LoadAsync_GeçersizId_Clear()
    {
        await YukleAsync(1, [], []);
        Assert.True(_sut.IsLoaded);

        await _sut.LoadAsync(0);

        Assert.False(_sut.IsLoaded);
    }

    [Fact(DisplayName = "IsLoaded: başlangıçta false")]
    public void IsLoaded_Baslangiçta_False()
    {
        Assert.False(_sut.IsLoaded);
    }

    [Fact(DisplayName = "IsLoaded: LoadAsync sonrası true")]
    public async Task IsLoaded_LoadSonrasi_True()
    {
        await YukleAsync(1, [], []);

        Assert.True(_sut.IsLoaded);
    }

    // ════════════════════════════════════════════════════════════════════════
    // 4. ForceReloadAsync — Admin Bug-Fix Testleri (KRİTİK)
    // ════════════════════════════════════════════════════════════════════════

    [Fact(DisplayName = "ForceReloadAsync (KRİTİK): farklı kullanıcı ID → no-op, admin yetkisi değişmez")]
    public async Task ForceReloadAsync_FarkliKullanici_NoOp_AdminYetkisiBozulmuyor()
    {
        // Admin (ID=1) yetkilerini yükle: Form 1 → Yazma
        const int adminId         = 1;
        const int digerKullanici  = 2;

        await YukleAsync(adminId,
            [new ItemKullaniciTipDetay { Form_ID = 1, Yetki = YetkiTipi.Yazma }], []);

        // Yükleme sonrası admin Yazma yetkisine sahip
        Assert.Equal(YetkiTipi.Yazma, _sut.GetYetki(1));
        Assert.True(_sut.IsLoaded);

        // Admin, farklı bir kullanıcı (ID=2) için ForceReload tetikliyor (UI'daki durum)
        await _sut.ForceReloadAsync(digerKullanici);

        // Admin yetkisi KORUNMALI — bu bug-fix'in özüdür
        Assert.Equal(YetkiTipi.Yazma, _sut.GetYetki(1));
        Assert.True(_sut.IsLoaded);

        // Farklı kullanıcı için DB'ye asla gidilmemeli
        await _data.DidNotReceive().GetKullaniciFormYetki(digerKullanici);
    }

    [Fact(DisplayName = "ForceReloadAsync: ID <= 0 → Clear yapılır")]
    public async Task ForceReloadAsync_GeçersizId_Clear()
    {
        await YukleAsync(1, [], []);
        Assert.True(_sut.IsLoaded);

        await _sut.ForceReloadAsync(0);

        Assert.False(_sut.IsLoaded);
    }

    [Fact(DisplayName = "ForceReloadAsync: aynı kullanıcı → cache geçersizlenir ve yeniden yüklenir")]
    public async Task ForceReloadAsync_AyniKullanici_YenidenYukleniyor()
    {
        const int id = 1;

        // İlk yükleme: Form 1 → Okuma
        await YukleAsync(id,
            [new ItemKullaniciTipDetay { Form_ID = 1, Yetki = YetkiTipi.Okuma }], []);
        Assert.Equal(YetkiTipi.Okuma, _sut.GetYetki(1));

        // Yetki değişti: Form 1 → Yazma
        var user = MakeUser(id);
        _data.GetKullaniciTipFormYetki(user.KullaniciTip_ID)
             .Returns(Task.FromResult(new[]
             {
                 new ItemKullaniciTipDetay { Form_ID = 1, Yetki = YetkiTipi.Yazma }
             }));

        // ForceReload yeni yetkiyi DB'den almalı
        await _sut.ForceReloadAsync(id);

        Assert.Equal(YetkiTipi.Yazma, _sut.GetYetki(1));
    }

    // ════════════════════════════════════════════════════════════════════════
    // 5. Clear()
    // ════════════════════════════════════════════════════════════════════════

    [Fact(DisplayName = "Clear: IsLoaded false, yetkiMap temizlenir")]
    public async Task Clear_TumDurumuSifirlar()
    {
        await YukleAsync(1,
            [new ItemKullaniciTipDetay { Form_ID = 1, Yetki = YetkiTipi.Yazma }], []);
        Assert.True(_sut.IsLoaded);

        _sut.Clear();

        Assert.False(_sut.IsLoaded);
        Assert.Equal(0, _sut.GetYetki(1));
        Assert.Empty(_sut.ErisebilirFormIdler());
    }

    // ════════════════════════════════════════════════════════════════════════
    // 6. OnChange Event
    // ════════════════════════════════════════════════════════════════════════

    [Fact(DisplayName = "OnChange: LoadAsync sonrası event tetikleniyor")]
    public async Task OnChange_LoadSonrasi_Tetikleniyor()
    {
        int sayac = 0;
        _sut.OnChange += () => sayac++;

        await YukleAsync(1, [], []);

        Assert.True(sayac > 0, "OnChange en az bir kez tetiklenmiş olmalı.");
    }

    [Fact(DisplayName = "OnChange: Clear sonrası event tetikleniyor")]
    public async Task OnChange_ClearSonrasi_Tetikleniyor()
    {
        await YukleAsync(1, [], []);

        int sayac = 0;
        _sut.OnChange += () => sayac++;

        _sut.Clear();

        Assert.Equal(1, sayac);
    }

    // ════════════════════════════════════════════════════════════════════════
    // 7. Async Sorgu Metotları (CanReadAsync / CanWriteAsync / HasPermissionAsync)
    // ════════════════════════════════════════════════════════════════════════

    [Fact(DisplayName = "CanReadAsync: Okuma yetkisi var → true")]
    public async Task CanReadAsync_OkumaYetkisi_True()
    {
        const int formId = 1;
        var formlar      = new[] { new ItemForm { ID = formId, SayfaURL = "/urunler" } };
        _data.GetTumFormlar().Returns(Task.FromResult(formlar));
        await YukleAsync(1, [new ItemKullaniciTipDetay { Form_ID = formId, Yetki = YetkiTipi.Okuma }], []);

        Assert.True(await _sut.CanReadAsync("/urunler"));
    }

    [Fact(DisplayName = "CanReadAsync: Gizli yetki → false")]
    public async Task CanReadAsync_Gizli_False()
    {
        const int formId = 1;
        var formlar      = new[] { new ItemForm { ID = formId, SayfaURL = "/urunler" } };
        _data.GetTumFormlar().Returns(Task.FromResult(formlar));
        await YukleAsync(1, [new ItemKullaniciTipDetay { Form_ID = formId, Yetki = YetkiTipi.Gizli }], []);

        Assert.False(await _sut.CanReadAsync("/urunler"));
    }

    [Fact(DisplayName = "CanWriteAsync: Yazma yetkisi var → true")]
    public async Task CanWriteAsync_YazmaYetkisi_True()
    {
        const int formId = 1;
        var formlar      = new[] { new ItemForm { ID = formId, SayfaURL = "/stok" } };
        _data.GetTumFormlar().Returns(Task.FromResult(formlar));
        await YukleAsync(1, [new ItemKullaniciTipDetay { Form_ID = formId, Yetki = YetkiTipi.Yazma }], []);

        Assert.True(await _sut.CanWriteAsync("/stok"));
    }

    [Fact(DisplayName = "CanWriteAsync: Okuma yetkisi → false")]
    public async Task CanWriteAsync_OkumaYetkisi_False()
    {
        const int formId = 1;
        var formlar      = new[] { new ItemForm { ID = formId, SayfaURL = "/stok" } };
        _data.GetTumFormlar().Returns(Task.FromResult(formlar));
        await YukleAsync(1, [new ItemKullaniciTipDetay { Form_ID = formId, Yetki = YetkiTipi.Okuma }], []);

        Assert.False(await _sut.CanWriteAsync("/stok"));
    }

    [Fact(DisplayName = "HasPermissionAsync: minYetki=Yazma, etkin=Yazma → true")]
    public async Task HasPermissionAsync_MinYazma_Yazma_True()
    {
        const int formId = 1;
        var formlar      = new[] { new ItemForm { ID = formId, SayfaURL = "/rapor" } };
        _data.GetTumFormlar().Returns(Task.FromResult(formlar));
        await YukleAsync(1, [new ItemKullaniciTipDetay { Form_ID = formId, Yetki = YetkiTipi.Yazma }], []);

        Assert.True(await _sut.HasPermissionAsync("/rapor", YetkiTipi.Yazma));
    }

    // ════════════════════════════════════════════════════════════════════════
    // 8. EnsureFormsLoadedAsync — Tek Kez Yüklenme
    // ════════════════════════════════════════════════════════════════════════

    [Fact(DisplayName = "EnsureFormsLoadedAsync: çift çağrı → DB tek sorgulanır")]
    public async Task EnsureFormsLoadedAsync_CiftCagri_TekDbSorgusu()
    {
        await _sut.EnsureFormsLoadedAsync();
        await _sut.EnsureFormsLoadedAsync();

        await _data.Received(1).GetTumFormlar();
    }

    // ════════════════════════════════════════════════════════════════════════
    // 8b. EnsureReadyAsync — URL ile direkt sayfa erişimi bug-fix testi (KRİTİK)
    // ════════════════════════════════════════════════════════════════════════

    [Fact(DisplayName = "EnsureReadyAsync (KRİTİK): URL'den direkt erişim → harita ve formlar yüklenir, ErişimVar doğru çalışır")]
    public async Task EnsureReadyAsync_HemHaritayiHemFormlariYukler()
    {
        // Senaryo: Kullanıcı login olmuş, ama sayfaya menü yerine URL ile direkt geliyor.
        // _yetkiMap henüz LoadAsync ile yüklenmemiş — sadece AuthState.CurrentUser dolu.
        const int userId = 7;
        const int formId = 1;

        var user = MakeUser(userId, tipId: 10);
        _auth.CurrentUser.Returns(user);
        _auth.IsAuthenticated.Returns(true);

        _data.GetKullanici(userId)
             .Returns(Task.FromResult(new[] { user }));
        _data.GetKullaniciTipFormYetki(10)
             .Returns(Task.FromResult(new[]
             {
                 new ItemKullaniciTipDetay { Form_ID = formId, Yetki = YetkiTipi.Yazma }
             }));
        _data.GetTumFormlar()
             .Returns(Task.FromResult(new[]
             {
                 new ItemForm { ID = formId, SayfaURL = "/urunler" }
             }));

        // Bug öncesi davranış: sadece EnsureFormsLoadedAsync çağrıldığında
        // _yetkiMap boş olduğu için ErişimVar her zaman false döner.
        await _sut.EnsureFormsLoadedAsync();
        Assert.False(_sut.ErişimVar("/urunler"),
            "Sadece formlar yüklendiyse, yetki haritası boş — bu bug pattern'idir.");

        // Bug düzeltmesi: EnsureReadyAsync hem haritayı hem formları yükler.
        await _sut.EnsureReadyAsync();

        Assert.True(_sut.IsLoaded);
        Assert.True(_sut.ErişimVar("/urunler"));
        Assert.True(_sut.HasYazma("/urunler"));
    }

    [Fact(DisplayName = "EnsureReadyAsync: kullanıcı oturumda değilken çağrılırsa Clear durumu korunur")]
    public async Task EnsureReadyAsync_OturumYok_ClearKalir()
    {
        _auth.CurrentUser.Returns((ItemKullanicilar?)null);
        _auth.IsAuthenticated.Returns(false);

        await _sut.EnsureReadyAsync();

        Assert.False(_sut.IsLoaded);
        Assert.False(_sut.ErişimVar("/herhangi"));
    }

    [Fact(DisplayName = "EnsureLoadedAsync: harita zaten yüklüyse DB'ye gitmez")]
    public async Task EnsureLoadedAsync_ZatenYukluyse_DbCagrisiYapmaz()
    {
        await YukleAsync(1, [], []);

        await _sut.EnsureLoadedAsync();
        await _sut.EnsureLoadedAsync();

        // YukleAsync 1 kez çağırdı; sonraki EnsureLoadedAsync'ler tekrar çağırmamalı
        await _data.Received(1).GetKullaniciFormYetki(1);
    }

    // ════════════════════════════════════════════════════════════════════════
    // 9. Dispose
    // ════════════════════════════════════════════════════════════════════════

    [Fact(DisplayName = "Dispose: ikinci kez çağrılınca exception fırlatmaz")]
    public void Dispose_IkincikCagri_ExceptionYok()
    {
        var svc = new YetkiService(_data, _auth);
        svc.Dispose();
        var ex = Record.Exception(() => svc.Dispose());
        Assert.Null(ex);
    }
}

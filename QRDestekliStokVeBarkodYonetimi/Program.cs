using System.Security.Claims;
using Dapper;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server;
using QRDestekliStokVeBarkodYonetimi.Components;
using QRDestekliStokVeBarkodYonetimi.Services;
using QuestPDF.Infrastructure;
using Radzen;

// QuestPDF açık kaynak (Community) lisansı — ticari olmayan / küçük şirket
// kullanımı için ücretsizdir. Kütüphane çalışmadan önce mutlaka set edilmeli.
QuestPDF.Settings.License = LicenseType.Community;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddRadzenComponents();
builder.Services.AddScoped<ThemeService>();
builder.Services.AddScoped<DialogService>();
builder.Services.AddScoped<NotificationService>();
builder.Services.AddScoped<TooltipService>();
builder.Services.AddScoped<ContextMenuService>();

var connectionString = builder.Configuration.GetConnectionString("PostgreSqlConnection")
    ?? throw new InvalidOperationException("PostgreSqlConnection connection string bulunamadı.");

builder.Services.AddSingleton(new DBClass(connectionString));

builder.Services.AddSingleton<SifreDegistirDogrulamaService>();

builder.Services.AddScoped<DataService>(sp => new DataService(
    connectionString,
    sp.GetRequiredService<EmailService>(),
    sp.GetRequiredService<SifreDegistirDogrulamaService>(),
    sp.GetRequiredService<KullaniciHesapOnayTokenService>()));
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection(JwtSettings.SectionName));
builder.Services.AddSingleton<JwtService>();

// Mail altyapısı — profil sayfasındaki e-posta doğrulama akışı için.
// SmtpSettings doluysa gerçek mail gönderir, eksikse log + false döner.
builder.Services.Configure<SmtpSettings>(builder.Configuration.GetSection(SmtpSettings.SectionName));
builder.Services.AddScoped<EmailService>();
builder.Services.AddSingleton<EpostaDogrulamaService>();
builder.Services.AddSingleton<SifreSifirlamaTokenService>();
builder.Services.AddSingleton<KullaniciHesapOnayTokenService>();

builder.Services.AddScoped<AuthStateService>();
builder.Services.AddScoped<IAuthState>(sp => sp.GetRequiredService<AuthStateService>());

builder.Services.AddScoped<IYetkiDataAccess>(sp => sp.GetRequiredService<DataService>());
builder.Services.AddScoped<YetkiService>();
builder.Services.AddSingleton<QrService>();
builder.Services.AddScoped<ExportService>();

builder.Services.AddHttpContextAccessor();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<AuthenticationStateProvider, ServerAuthenticationStateProvider>();

// ─────────────────────────────────────────────────────────────────────────────
// Kimlik Doğrulama (Cookie)
//
// Login bilgisi (kullanıcı ID, ad-soyad, eposta, kullanıcı tipi) HttpOnly bir
// cookie'de tutulur. Tarayıcı her HTTP isteğinde cookie'yi taşıdığı için yeni
// circuit / yeni sekme / F5 sonrasında oturum korunur. Token bellek-içi tutulurken
// URL'den direkt sayfa açılışlarında AuthStateService boş kalıyor ve yetki kontrolü
// kullanıcıyı /erisim-engeli'ne atıyordu — cookie buna kalıcı çözüm.
// ─────────────────────────────────────────────────────────────────────────────
builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "qr.auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.LoginPath = "/login";
        options.LogoutPath = "/api/auth/logout";
        options.AccessDeniedPath = "/erisim-engeli";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
    });

builder.Services.AddAuthorization();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();
app.UseAuthentication();
app.UseAuthorization();

// Anonim GET / isteklerinde Blazor MainLayout/Home çizilmeden login'e yönlendir
// (açılışta boş dashboard flash'ını önler).
app.Use(async (context, next) =>
{
    if (HttpMethods.IsGet(context.Request.Method) &&
        context.Request.Path == "/" &&
        !context.Response.HasStarted)
    {
        var auth = await context.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        var girisYapilmis = auth.Succeeded && auth.Principal?.Identity?.IsAuthenticated == true;
        if (!girisYapilmis)
        {
            context.Response.Redirect("/login");
            return;
        }
    }

    await next();
});

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// ─────────────────────────────────────────────────────────────────────────────
// Auth endpoint'leri — Login.razor / Register.razor / NavMenu logout form'ları
// buralara HTML form-post yapar. Cookie burada SignInAsync ile set edilir.
// ─────────────────────────────────────────────────────────────────────────────
app.MapPost("/api/auth/login", async (
        HttpContext ctx,
        DataService data) =>
    {
        var form = await ctx.Request.ReadFormAsync();
        var eposta = form["Eposta"].ToString();
        var sifre = form["Sifre"].ToString();
        var returnUrl = form["ReturnUrl"].ToString();
        var beniHatirla = string.Equals(form["BeniHatirla"].ToString(), "true", StringComparison.OrdinalIgnoreCase);

        if (string.IsNullOrWhiteSpace(eposta) || string.IsNullOrWhiteSpace(sifre))
            return Results.Redirect("/login?error=" + Uri.EscapeDataString("E-posta ve şifre zorunludur."));

        var result = await data.LoginKullanici(eposta, sifre);
        if (result.Data is null)
            return Results.Redirect("/login?error=" + Uri.EscapeDataString(
                string.IsNullOrEmpty(result.SonucAciklama) ? "Giriş başarısız." : result.SonucAciklama));

        var user = result.Data;
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.ID.ToString()),
            new(ClaimTypes.Name,           $"{user.Ad} {user.Soyad}".Trim()),
            new(ClaimTypes.Email,          user.Eposta ?? string.Empty),
            new("KullaniciTip_ID",         user.KullaniciTip_ID.ToString()),
            new("Ad",                      user.Ad ?? string.Empty),
            new("Soyad",                   user.Soyad ?? string.Empty)
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);
        var authProps = new AuthenticationProperties { IsPersistent = beniHatirla };
        if (beniHatirla)
            authProps.ExpiresUtc = DateTimeOffset.UtcNow.AddDays(30);
        await ctx.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, authProps);

        var hedef = string.IsNullOrWhiteSpace(returnUrl) ? "/" : returnUrl;
        // Açık redirect koruması: yalnız aynı origin'e izin ver
        if (!Uri.IsWellFormedUriString(hedef, UriKind.Relative)) hedef = "/";
        return Results.Redirect(hedef);
    })
    .DisableAntiforgery();

app.MapPost("/api/auth/register", async (
        HttpContext ctx,
        DataService data) =>
    {
        var form = await ctx.Request.ReadFormAsync();
        var ad = form["Ad"].ToString();
        var soyad = form["Soyad"].ToString();
        var eposta = form["Eposta"].ToString();
        var sifre = form["Sifre"].ToString();
        var sifreTekrar = form["SifreTekrar"].ToString();

        string? hata =
            string.IsNullOrWhiteSpace(ad) ? "Ad zorunludur." :
            string.IsNullOrWhiteSpace(soyad) ? "Soyad zorunludur." :
            string.IsNullOrWhiteSpace(eposta) ? "E-posta zorunludur." :
            string.IsNullOrWhiteSpace(sifre) ? "Şifre zorunludur." :
            sifre.Length < 6 ? "Şifre en az 6 karakter olmalıdır." :
            sifre != sifreTekrar ? "Şifreler eşleşmiyor." :
            null;

        if (hata is not null)
            return Results.Redirect("/register?error=" + Uri.EscapeDataString(hata));

        var result = await data.RegisterKullanici(ad, soyad, eposta, sifre);
        if (result.SonucKodu < 0 || result.Data <= 0)
            return Results.Redirect("/register?error=" + Uri.EscapeDataString(
                string.IsNullOrEmpty(result.SonucAciklama) ? "Kayıt sırasında bir hata oluştu." : result.SonucAciklama));

        return Results.Redirect("/login?registered=1");
    })
    .DisableAntiforgery();

app.MapPost("/api/auth/logout", async (HttpContext ctx) =>
    {
        await ctx.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return Results.Redirect("/login");
    })
    .DisableAntiforgery();

app.MapPost("/api/auth/forgot-password", async (
        HttpContext ctx,
        DataService data,
        EmailService email,
        SifreSifirlamaTokenService tokenSvc) =>
    {
        var form = await ctx.Request.ReadFormAsync();
        var eposta = form["Eposta"].ToString().Trim();

        if (!string.IsNullOrWhiteSpace(eposta))
        {
            var user = await data.GetKullaniciByEposta(eposta);
            if (user is not null && user.Aktif != false && !string.IsNullOrWhiteSpace(user.Eposta))
            {
                var token = tokenSvc.OlusturKaydet(user.ID);
                var link =
                    $"{ctx.Request.Scheme}://{ctx.Request.Host}/sifre-sifirla?token={Uri.EscapeDataString(token)}";
                var html =
                    "<p>Merhaba,</p>" +
                    "<p>Şifre sıfırlama talebinde bulundunuz. Aşağıdaki bağlantıya tıklayarak yeni şifrenizi " +
                    "belirleyebilirsiniz. Bağlantı 60 dakika geçerlidir.</p>" +
                    $"<p><a href=\"{link}\">Şifremi sıfırla</a></p>" +
                    "<p>Bu talebi siz oluşturmadıysanız bu e-postayı yok sayabilirsiniz.</p>";
                await email.SendAsync(user.Eposta, "Şifre sıfırlama", html);
            }
        }

        return Results.Redirect("/sifremi-unuttum?gonderildi=1");
    })
    .DisableAntiforgery();

app.MapPost("/api/auth/reset-password", async (
        HttpContext ctx,
        DataService data,
        SifreSifirlamaTokenService tokenSvc) =>
    {
        var form = await ctx.Request.ReadFormAsync();
        var token = form["Token"].ToString();
        var yeni = form["YeniSifre"].ToString();
        var yeniTekrar = form["YeniSifreTekrar"].ToString();

        if (!tokenSvc.TryPeekValidToken(token, out var userId))
            return Results.Redirect("/sifre-sifirla?error=" + Uri.EscapeDataString(
                "Bağlantı geçersiz veya süresi dolmuş. Lütfen yeni şifre sıfırlama talebi oluşturun."));

        string? sifreHata =
            string.IsNullOrWhiteSpace(yeni) ? "Şifre zorunludur." :
            yeni.Length < 6 ? "Şifre en az 6 karakter olmalıdır." :
            yeni != yeniTekrar ? "Şifreler eşleşmiyor." :
            null;

        if (sifreHata is not null)
            return Results.Redirect(
                "/sifre-sifirla?token=" + Uri.EscapeDataString(token) + "&error=" + Uri.EscapeDataString(sifreHata));

        var result = await data.SifreSifirlaTokenIle(userId, yeni);
        if (result.SonucKodu < 0)
            return Results.Redirect("/sifre-sifirla?token=" + Uri.EscapeDataString(token) + "&error=" +
                Uri.EscapeDataString(
                    string.IsNullOrEmpty(result.SonucAciklama) ? "Şifre güncellenemedi." : result.SonucAciklama));

        tokenSvc.TryConsume(token, out _);
        return Results.Redirect("/login?sifirlandi=1");
    })
    .DisableAntiforgery();

app.MapGet("/api/auth/confirm-account", async (string? token, DataService data) =>
    {
        var result = await data.KullaniciHesapOnayla(token);
        if (result.SonucKodu >= 0)
            return Results.Redirect("/hesap-onay?success=1");
        return Results.Redirect("/hesap-onay?error=" + Uri.EscapeDataString(
            result.SonucAciklama ?? "Onay işlemi başarısız."));
    });

// Menü yapısı tamamen Form tablosundaki UstMenu_ID zincirinden sürülür.
// Buraya hardcoded form/menü seed eklenmez — Form kayıtları DB üzerinden
// (SQL veya ileride eklenecek bir Form CRUD sayfasından) yönetilir.
await SeedAdminAsync(connectionString);

app.Run();

// ─────────────────────────────────────────────────────────────────────────────
// Admin kullanıcı seed  (yalnızca admin e-postası yoksa çalışır)
// ─────────────────────────────────────────────────────────────────────────────
static async Task SeedAdminAsync(string connectionString)
{
    await using var conn = new Npgsql.NpgsqlConnection(connectionString);
    try
    {
        await conn.OpenAsync();

        const string adminEmail = "admin@sistem.com";

        // ── 1. Admin zaten var mı? ────────────────────────────────────────────
        var adminVarMi = await conn.ExecuteScalarAsync<int>(
            @"SELECT COUNT(*) FROM ""Kullanicilar"" WHERE LOWER(""Eposta"") = LOWER(@Email)",
            new { Email = adminEmail });

        if (adminVarMi > 0)
        {
            Console.WriteLine("ℹ️  Admin kullanıcısı mevcut, kullanıcı seed atlandı.");
            // Yetki seed'ini yine de çalıştır (eksik yetki varsa tamamla)
            await SeedAdminYetkiAsync(conn, adminEmail);
            return;
        }

        Console.WriteLine("🔧 Admin kullanıcı seed başlatılıyor...");

        // ── 2. KullaniciTip: 'Admin' tipi ────────────────────────────────────
        var mevcutTipId = await conn.ExecuteScalarAsync<int?>(
            @"SELECT ""ID"" FROM ""KullaniciTip""
              WHERE LOWER(""Ad"") = 'admin' AND ""DelUser"" IS NULL LIMIT 1");

        int tipId;
        if (mevcutTipId.HasValue)
        {
            tipId = mevcutTipId.Value;
            Console.WriteLine($"ℹ️  Mevcut 'Admin' KullaniciTip kullanılıyor (ID: {tipId}).");
        }
        else
        {
            tipId = (int)(await conn.ExecuteScalarAsync<long>(
                @"INSERT INTO ""KullaniciTip"" (""Ad"", ""Aktif"", ""CreUser"", ""CreDate"")
                  VALUES ('Admin', true, 1, now())
                  RETURNING ""ID"""))!;
            Console.WriteLine($"✅ 'Admin' KullaniciTip oluşturuldu (ID: {tipId}).");
        }

        // ── 3. Admin kullanıcısını oluştur ────────────────────────────────────
        var sifreHash = QRDestekliStokVeBarkodYonetimi.Services.PasswordHasher.Hash("Admin@123");

        var adminId = (int)(await conn.ExecuteScalarAsync<long>(
            @"INSERT INTO ""Kullanicilar""
                (""KullaniciTip_ID"", ""Ad"", ""Soyad"", ""Eposta"", ""Sifre"", ""ProfilResmi"", ""Aktif"", ""CreUser"", ""CreDate"")
              VALUES (@TipId, 'Admin', 'Kullanıcı', @Email, @Sifre, NULL, true, 1, now())
              RETURNING ""ID""",
            new { TipId = tipId, Email = adminEmail, Sifre = sifreHash }))!;

        Console.WriteLine($"✅ Admin kullanıcı oluşturuldu — {adminEmail} / Admin@123 (ID: {adminId})");

        // ── 4. Admin tipine tüm formlar için Yazma (2) yetkisi ver ───────────
        await SeedAdminYetkiAsync(conn, adminEmail);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"⚠️  Admin seed hatası: {ex.GetType().Name} — {ex.Message}");
        if (ex.InnerException is not null)
            Console.WriteLine($"   Inner: {ex.InnerException.Message}");
    }
    finally { await conn.CloseAsync(); }
}

// ─────────────────────────────────────────────────────────────────────────────
// Admin tipine eksik form yetkilerini tamamla
// ─────────────────────────────────────────────────────────────────────────────
static async Task SeedAdminYetkiAsync(Npgsql.NpgsqlConnection conn, string adminEmail)
{
    try
    {
        // Admin kullanıcının tipini bul
        var tipId = await conn.ExecuteScalarAsync<int?>(
            @"SELECT k.""KullaniciTip_ID"" FROM ""Kullanicilar"" k
              WHERE LOWER(k.""Eposta"") = LOWER(@Email) AND k.""DelUser"" IS NULL LIMIT 1",
            new { Email = adminEmail });

        if (!tipId.HasValue) return;

        // Tüm aktif formları al
        var tumFormIdler = (await conn.QueryAsync<int>(
            @"SELECT ""ID"" FROM ""Form"" WHERE ""DelUser"" IS NULL")).ToList();

        int eklenen = 0;
        foreach (var formId in tumFormIdler)
        {
            var varMi = await conn.ExecuteScalarAsync<int?>(
                @"SELECT ""ID"" FROM ""KullaniciTipDetay""
                  WHERE ""KullaniciTip_ID"" = @TipId AND ""Form_ID"" = @FormId AND ""DelUser"" IS NULL LIMIT 1",
                new { TipId = tipId.Value, FormId = formId });

            if (!varMi.HasValue)
            {
                await conn.ExecuteAsync(
                    @"INSERT INTO ""KullaniciTipDetay"" (""KullaniciTip_ID"", ""Form_ID"", ""Yetki"", ""CreUser"", ""CreDate"")
                      VALUES (@TipId, @FormId, 2, 1, now())",
                    new { TipId = tipId.Value, FormId = formId });
                eklenen++;
            }
        }

        if (eklenen > 0)
            Console.WriteLine($"✅ Admin tipine {eklenen} yeni form için Yazma yetkisi eklendi.");
        else
            Console.WriteLine("ℹ️  Admin tipi yetkileri zaten tam.");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"⚠️  Yetki seed hatası: {ex.Message}");
    }
}
using Dapper;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server;
using QRDestekliStokVeBarkodYonetimi.Components;
using QRDestekliStokVeBarkodYonetimi.Services;
using Radzen;

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

builder.Services.AddScoped<DataService>(sp =>
    new DataService(connectionString, sp.GetRequiredService<AuthenticationStateProvider>()));

builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection(JwtSettings.SectionName));
builder.Services.AddSingleton<JwtService>();

builder.Services.AddScoped<AuthStateService>();
builder.Services.AddScoped<YetkiService>();
builder.Services.AddSingleton<QrService>();

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<AuthenticationStateProvider, ServerAuthenticationStateProvider>();

var jwtSettings = builder.Configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>()
    ?? throw new InvalidOperationException("Jwt ayarları appsettings.json içinde bulunamadı.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
        options.SaveToken = true;
        options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
        {
            ValidateIssuer           = true,
            ValidateAudience         = true,
            ValidateLifetime         = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer              = jwtSettings.Issuer,
            ValidAudience            = jwtSettings.Audience,
            IssuerSigningKey         = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(
                                           System.Text.Encoding.UTF8.GetBytes(jwtSettings.SecretKey)),
            ClockSkew = TimeSpan.FromSeconds(30)
        };
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

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Form menü yapısını her başlatmada güncelle, sonra admin seed yap
await SeedFormsAsync(connectionString);
await SeedAdminAsync(connectionString);

app.Run();

// ─────────────────────────────────────────────────────────────────────────────
// Form / Menü seed  (her başlatmada çalışır, idempotent)
// ─────────────────────────────────────────────────────────────────────────────
static async Task SeedFormsAsync(string connectionString)
{
    await using var conn = new Npgsql.NpgsqlConnection(connectionString);
    try
    {
        await conn.OpenAsync();

        // Yerel yardımcı: URL bazlı form ID'si getir; yoksa INSERT edip döner
        async Task<int> UpsertForm(string ad, bool isMenu, int? ustMenuId, int sira, string? sayfaUrl, string? icon)
        {
            // Var mı? SayfaURL null olanlar Ad ile eşleştirilir
            int? mevcutId = sayfaUrl is not null
                ? await conn.ExecuteScalarAsync<int?>(
                    @"SELECT ""ID"" FROM ""Form"" WHERE ""SayfaURL"" = @Url AND ""DelUser"" IS NULL LIMIT 1",
                    new { Url = sayfaUrl })
                : await conn.ExecuteScalarAsync<int?>(
                    @"SELECT ""ID"" FROM ""Form"" WHERE ""Ad"" = @Ad AND ""SayfaURL"" IS NULL AND ""DelUser"" IS NULL LIMIT 1",
                    new { Ad = ad });

            if (mevcutId.HasValue)
            {
                // UstMenu_ID ve Sira'yı güncelle (önceki düz seed'i düzelt)
                await conn.ExecuteAsync(
                    @"UPDATE ""Form"" SET ""UstMenu_ID"" = @UstMenuId, ""Sira"" = @Sira, ""Ad"" = @Ad,
                             ""IsMenu"" = @IsMenu, ""Icon"" = @Icon, ""UpdUser"" = 1, ""UpdDate"" = now()
                      WHERE ""ID"" = @Id",
                    new { UstMenuId = ustMenuId, Sira = sira, Ad = ad, IsMenu = isMenu, Icon = icon, Id = mevcutId.Value });
                return mevcutId.Value;
            }

            return (int)(await conn.ExecuteScalarAsync<long>(
                @"INSERT INTO ""Form"" (""Ad"", ""IsMenu"", ""UstMenu_ID"", ""Sira"", ""SayfaURL"", ""Icon"", ""CreUser"", ""CreDate"")
                  VALUES (@Ad, @IsMenu, @UstMenuId, @Sira, @SayfaUrl, @Icon, 1, now())
                  RETURNING ""ID""",
                new { Ad = ad, IsMenu = isMenu, UstMenuId = ustMenuId, Sira = sira, SayfaUrl = sayfaUrl, Icon = icon }))!;
        }

        // ── Üst menü grupları (UstMenu_ID = null) ────────────────────────────
        //    Ana Sayfa menüde hardcoded — Form tablosuna eklenmez.
        //    Grup formlarının SayfaURL'si null — sadece Ad ile tanımlanır.
        int idStokGrup  = await UpsertForm("Stok Yönetimi", true, null, 1, null, "swap_vert");
        int idTanimGrup = await UpsertForm("Tanımlar",       true, null, 2, null, "tune");
        int idYonetGrup = await UpsertForm("Yönetim",        true, null, 3, null, "admin_panel_settings");

        // ── Stok Yönetimi alt menüleri ────────────────────────────────────────
        await UpsertForm("Stok Hareketleri", true, idStokGrup,  1, "/stok-hareketleri", "swap_vert");
        await UpsertForm("Stok Giriş",       true, idStokGrup,  2, "/stok-giris",        "arrow_downward");
        await UpsertForm("Stok Çıkış",       true, idStokGrup,  3, "/stok-cikis",        "arrow_upward");

        // ── Tanımlar alt menüleri ─────────────────────────────────────────────
        await UpsertForm("Birimler",    true, idTanimGrup, 1, "/birimler",    "scale");
        await UpsertForm("Kategoriler", true, idTanimGrup, 2, "/kategoriler", "category");
        await UpsertForm("Ürünler",     true, idTanimGrup, 3, "/urunler",     "inventory_2");

        // ── Yönetim alt menüleri ──────────────────────────────────────────────
        await UpsertForm("Kullanıcılar",      true,  idYonetGrup, 1, "/kullanicilar",     "people");
        await UpsertForm("Kullanıcı Tipleri", true,  idYonetGrup, 2, "/kullanici-tipler", "manage_accounts");

        // ── Yetki formları (menüde görünmez, yetki sistemi için gerekli) ──────
        await UpsertForm("Kullanıcı Yetki",     false, null, 10, "/kullanici-yetki",     null);
        await UpsertForm("Kullanıcı Tip Yetki", false, null, 11, "/kullanici-tip-yetki", null);

        Console.WriteLine("✅ Form / menü yapısı hazır.");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"⚠️  Form seed hatası: {ex.GetType().Name} — {ex.Message}");
        if (ex.InnerException is not null)
            Console.WriteLine($"   Inner: {ex.InnerException.Message}");
    }
    finally { await conn.CloseAsync(); }
}

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
                (""KullaniciTip_ID"", ""Ad"", ""Soyad"", ""Eposta"", ""Sifre"", ""Aktif"", ""CreUser"", ""CreDate"")
              VALUES (@TipId, 'Admin', 'Kullanıcı', @Email, @Sifre, true, 1, now())
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
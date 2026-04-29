using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server;
using QRDestekliStokVeBarkodYonetimi.Components;
using QRDestekliStokVeBarkodYonetimi.Services;
using Radzen;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddRadzenComponents();
builder.Services.AddScoped<ThemeService>();
builder.Services.AddScoped<DialogService>();
builder.Services.AddScoped<NotificationService>();
builder.Services.AddScoped<TooltipService>();
builder.Services.AddScoped<ContextMenuService>();

// PostgreSQL bağlantı cümlesi + veri tabanı servisleri
var connectionString = builder.Configuration.GetConnectionString("PostgreSqlConnection")
    ?? throw new InvalidOperationException("PostgreSqlConnection connection string bulunamadı.");

builder.Services.AddSingleton(new DBClass(connectionString));

// DataService: Kategori / Kullanıcı CRUD. AuthenticationStateProvider'a ihtiyaç duyduğu için scoped.
builder.Services.AddScoped<DataService>(sp =>
    new DataService(connectionString, sp.GetRequiredService<AuthenticationStateProvider>()));

// JWT ayarları + JwtService
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection(JwtSettings.SectionName));
builder.Services.AddSingleton<JwtService>();

// UI tarafı için oturum servisi
builder.Services.AddScoped<AuthStateService>();

// Yetki servisi (form bazlı yetki cache'i)
builder.Services.AddScoped<YetkiService>();

// QR kod üretim servisi
builder.Services.AddSingleton<QrService>();

// Blazor AuthenticationStateProvider (DataService bunu talep ediyor) + cascading AuthenticationState
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<AuthenticationStateProvider, ServerAuthenticationStateProvider>();

// JWT Bearer Authentication (API endpoint'lerinizi korumak için)
var jwtSettings = builder.Configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>()
    ?? throw new InvalidOperationException("Jwt ayarları appsettings.json içinde bulunamadı.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
        options.SaveToken = true;
        options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidAudience = jwtSettings.Audience,
            IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(
                System.Text.Encoding.UTF8.GetBytes(jwtSettings.SecretKey)),
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();

app.UseAuthentication();
app.UseAuthorization();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Admin kullanıcı seed
await SeedAdminAsync(app.Services, connectionString);

app.Run();

static async Task SeedAdminAsync(IServiceProvider services, string connectionString)
{
    try
    {
        var db = new QRDestekliStokVeBarkodYonetimi.Services.DBClass(connectionString);

        // Kullanıcı var mı?
        var mevcutSayisi = await db.SQLExecuteScalar<int>(
            @"SELECT COUNT(*) FROM ""Kullanicilar"" WHERE ""DelUser"" IS NULL");

        if (mevcutSayisi > 0) return; // Zaten en az bir kullanıcı var, seed gerekmiyor

        // KullaniciTip yoksa oluştur
        var tipSayisi = await db.SQLExecuteScalar<int>(
            @"SELECT COUNT(*) FROM ""KullaniciTip"" WHERE ""DelUser"" IS NULL");

        int tipId;
        if (tipSayisi == 0)
        {
            tipId = await db.SQLExecuteScalar<int>(
                @"INSERT INTO ""KullaniciTip"" (""Ad"", ""Aktif"", ""CreUser"", ""CreDate"")
                  VALUES ('Admin', true, 1, now())
                  RETURNING ""ID""");
        }
        else
        {
            tipId = await db.SQLExecuteScalar<int>(
                @"SELECT ""ID"" FROM ""KullaniciTip"" WHERE ""DelUser"" IS NULL ORDER BY ""ID"" LIMIT 1");
        }

        // Admin şifresini hashle
        var sifreHash = QRDestekliStokVeBarkodYonetimi.Services.PasswordHasher.Hash("Admin@123");

        await db.SQLExecute(
            @"INSERT INTO ""Kullanicilar""
                (""KullaniciTip_ID"", ""Ad"", ""Soyad"", ""Eposta"", ""Sifre"", ""Aktif"", ""CreUser"", ""CreDate"")
              VALUES
                (@TipId, 'Admin', 'Kullanıcı', 'admin@sistem.com', @Sifre, true, 1, now())",
            new { TipId = tipId, Sifre = sifreHash });

        Console.WriteLine("✅ Admin kullanıcı oluşturuldu — admin@sistem.com / Admin@123");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"⚠️  Admin seed hatası: {ex.Message}");
    }
}

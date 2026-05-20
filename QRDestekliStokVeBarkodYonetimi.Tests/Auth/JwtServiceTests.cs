using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using QRDestekliStokVeBarkodYonetimi.Models;
using QRDestekliStokVeBarkodYonetimi.Services;
using Xunit;

namespace QRDestekliStokVeBarkodYonetimi.Tests.Auth;

/// <summary>
/// JwtService — token üretme ve doğrulama testleri.
/// Gerçek DB gerekmez.
/// </summary>
public class JwtServiceTests
{
    private const string TestIssuer   = "test-issuer";
    private const string TestAudience = "test-audience";
    private const string TestSecret   = "bu-en-az-32-karakter-uzunlugunda-test-anahtaridir";

    private static JwtService Build(int accessTokenMinutes = 60) => new(
        Options.Create(new JwtSettings
        {
            Issuer             = TestIssuer,
            Audience           = TestAudience,
            SecretKey          = TestSecret,
            AccessTokenMinutes = accessTokenMinutes,
            RefreshTokenDays   = 7
        }));

    /// <summary>
    /// JwtService'i devre dışı bırakmadan, aynı anahtar/issuer/audience ile
    /// imzalanmış ama süresi GEÇMIŞ bir token üretir.
    /// (JWT kütüphanesi expires &lt; notBefore'a izin vermediğinden
    ///  JwtService.GenerateAccessToken'a negatif dakika verip bu durum test edilemez.)
    /// </summary>
    private static string CreateExpiredToken()
    {
        var key         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TestSecret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var past        = DateTime.UtcNow.AddHours(-2);

        var token = new JwtSecurityToken(
            issuer:             TestIssuer,
            audience:           TestAudience,
            claims:             [new Claim("sub", "42")],
            notBefore:          past,
            expires:            past.AddMinutes(30),   // süresi 1.5 saat önce doldu
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static ItemKullanicilar TestKullanici(int id = 42) => new()
    {
        ID              = id,
        Ad              = "Test",
        Soyad           = "Kullanici",
        Eposta          = "test@example.com",
        KullaniciTip_ID = 5
    };

    // ── JwtService oluşturma ──────────────────────────────────────────────

    [Fact(DisplayName = "JwtService: kısa anahtar (< 32 karakter) → InvalidOperationException")]
    public void Constructor_KisaAnahtar_InvalidOperationException()
    {
        var options = Options.Create(new JwtSettings
        {
            Issuer    = "x",
            Audience  = "x",
            SecretKey = "kisaanahtar"       // < 32 karakter
        });
        Assert.Throws<InvalidOperationException>(() => new JwtService(options));
    }

    // ── Token üretme ─────────────────────────────────────────────────────

    [Fact(DisplayName = "GenerateAccessToken: null kullanıcı → ArgumentNullException")]
    public void GenerateAccessToken_NullKullanici_ArgumentNull()
    {
        var sut = Build();
        Assert.Throws<ArgumentNullException>(() => sut.GenerateAccessToken(null!));
    }

    [Fact(DisplayName = "GenerateAccessToken: token string boş değil")]
    public void GenerateAccessToken_GecerliKullanici_TokenBosDegil()
    {
        var sut    = Build();
        var result = sut.GenerateAccessToken(TestKullanici());

        Assert.False(string.IsNullOrEmpty(result.Token));
    }

    [Fact(DisplayName = "GenerateAccessToken: ExpiresUtc gelecekte")]
    public void GenerateAccessToken_ExpiresUtc_Gelecekte()
    {
        var sut    = Build(accessTokenMinutes: 60);
        var result = sut.GenerateAccessToken(TestKullanici());

        Assert.True(result.ExpiresUtc > DateTime.UtcNow);
    }

    [Fact(DisplayName = "GenerateAccessToken: claim sub = kullanıcı ID")]
    public void GenerateAccessToken_ClaimSub_KullaniciId()
    {
        const int id = 99;
        var sut      = Build();
        var result   = sut.GenerateAccessToken(TestKullanici(id));

        var handler    = new JwtSecurityTokenHandler();
        var parsed     = handler.ReadJwtToken(result.Token);
        var subClaim   = parsed.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Sub);

        Assert.Equal(id.ToString(), subClaim?.Value);
    }

    [Fact(DisplayName = "GenerateAccessToken: claim email = kullanıcı eposta")]
    public void GenerateAccessToken_ClaimEmail_KullaniciEposta()
    {
        var kullanici = TestKullanici();
        var result    = Build().GenerateAccessToken(kullanici);

        var handler  = new JwtSecurityTokenHandler();
        var parsed   = handler.ReadJwtToken(result.Token);
        var email    = parsed.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Email);

        Assert.Equal(kullanici.Eposta, email?.Value);
    }

    [Fact(DisplayName = "GenerateAccessToken: KullaniciTip_ID claim'i var")]
    public void GenerateAccessToken_KullaniciTipIdClaimi_Var()
    {
        var kullanici = TestKullanici();
        var result    = Build().GenerateAccessToken(kullanici);

        var handler  = new JwtSecurityTokenHandler();
        var parsed   = handler.ReadJwtToken(result.Token);
        var tipClaim = parsed.Claims.FirstOrDefault(c => c.Type == "KullaniciTip_ID");

        Assert.Equal(kullanici.KullaniciTip_ID.ToString(), tipClaim?.Value);
    }

    // ── Token doğrulama ───────────────────────────────────────────────────

    [Fact(DisplayName = "ValidateToken: geçerli token → ClaimsPrincipal döner")]
    public void ValidateToken_GecerliToken_PrincipalDoner()
    {
        var sut    = Build();
        var token  = sut.GenerateAccessToken(TestKullanici()).Token;
        var result = sut.ValidateToken(token);

        Assert.NotNull(result);
        Assert.IsAssignableFrom<ClaimsPrincipal>(result);
    }

    [Fact(DisplayName = "ValidateToken: boş string → null")]
    public void ValidateToken_BosToken_Null()
    {
        Assert.Null(Build().ValidateToken(""));
    }

    [Fact(DisplayName = "ValidateToken: çöp string → null (crash yok)")]
    public void ValidateToken_CopToken_Null()
    {
        Assert.Null(Build().ValidateToken("bu.gecersiz.jwt"));
    }

    [Fact(DisplayName = "ValidateToken: farklı anahtarla imzalanmış token → null")]
    public void ValidateToken_FarkliAnahtar_Null()
    {
        var sut1   = Build();
        var sut2   = new JwtService(Options.Create(new JwtSettings
        {
            Issuer             = "test-issuer",
            Audience           = "test-audience",
            SecretKey          = "baska-bir-32-karakter-anahtar-abcdef1234",
            AccessTokenMinutes = 60
        }));

        var token  = sut1.GenerateAccessToken(TestKullanici()).Token;
        var result = sut2.ValidateToken(token);

        Assert.Null(result);
    }

    [Fact(DisplayName = "ValidateToken: süresi dolmuş token → null")]
    public void ValidateToken_SuresiDolmus_Null()
    {
        var sut   = Build();
        var token = CreateExpiredToken();

        Assert.Null(sut.ValidateToken(token));
    }

    // ── GetPrincipalFromExpiredToken ───────────────────────────────────────

    [Fact(DisplayName = "GetPrincipalFromExpiredToken: süresi dolmuş token → Principal (imza geçerli)")]
    public void GetPrincipalFromExpiredToken_SuresiDolmus_PrincipalDoner()
    {
        var sut   = Build();
        var token = CreateExpiredToken();

        var result = sut.GetPrincipalFromExpiredToken(token);

        Assert.NotNull(result);
    }

    [Fact(DisplayName = "GetPrincipalFromExpiredToken: boş string → null")]
    public void GetPrincipalFromExpiredToken_BosToken_Null()
    {
        Assert.Null(Build().GetPrincipalFromExpiredToken(""));
    }

    // ── Refresh token ─────────────────────────────────────────────────────

    [Fact(DisplayName = "GenerateRefreshToken: her çağrıda farklı değer üretir")]
    public void GenerateRefreshToken_HerCagridaFarkli()
    {
        var sut = Build();
        var t1  = sut.GenerateRefreshToken();
        var t2  = sut.GenerateRefreshToken();

        Assert.NotEqual(t1, t2);
    }

    [Fact(DisplayName = "GenerateRefreshToken: boş değil")]
    public void GenerateRefreshToken_BosDegil()
    {
        Assert.False(string.IsNullOrEmpty(Build().GenerateRefreshToken()));
    }
}

using QRDestekliStokVeBarkodYonetimi.Services;
using Xunit;

namespace QRDestekliStokVeBarkodYonetimi.Tests.Auth;

/// <summary>
/// PasswordHasher — PBKDF2/SHA-256 hash ve doğrulama testleri.
/// Gerçek DB gerekmez; tamamen izole birim testleridir.
/// </summary>
public class PasswordHasherTests
{
    // ── Hash format testleri ───────────────────────────────────────────────

    [Fact(DisplayName = "Hash: geçerli şifre → iterations.salt.hash formatı")]
    public void Hash_GecerliSifre_UcParceliFormat()
    {
        var hash = PasswordHasher.Hash("test123");

        var parts = hash.Split('.');
        Assert.Equal(3, parts.Length);
        Assert.True(int.TryParse(parts[0], out var iterations) && iterations > 0,
            "Birinci parça pozitif tamsayı (iterations) olmalı.");
        Assert.False(string.IsNullOrEmpty(parts[1]), "İkinci parça (salt) boş olmamalı.");
        Assert.False(string.IsNullOrEmpty(parts[2]), "Üçüncü parça (hash) boş olmamalı.");
    }

    [Fact(DisplayName = "Hash: aynı şifre iki kez hash'lense farklı değerler üretir (random salt)")]
    public void Hash_AyniSifre_FarkliHashler()
    {
        var h1 = PasswordHasher.Hash("Parola123!");
        var h2 = PasswordHasher.Hash("Parola123!");

        Assert.NotEqual(h1, h2);
    }

    [Fact(DisplayName = "Hash: boş şifre → ArgumentException")]
    public void Hash_BosSifre_ArgumentException()
    {
        Assert.Throws<ArgumentException>(() => PasswordHasher.Hash(""));
    }

    // ── Verify testleri ───────────────────────────────────────────────────

    [Fact(DisplayName = "Verify: doğru şifre + hash → true")]
    public void Verify_DogruSifre_True()
    {
        const string sifre = "Guvenli@2026";
        var hash = PasswordHasher.Hash(sifre);

        Assert.True(PasswordHasher.Verify(sifre, hash));
    }

    [Fact(DisplayName = "Verify: yanlış şifre → false")]
    public void Verify_YanlisSifre_False()
    {
        var hash = PasswordHasher.Hash("dogruSifre");

        Assert.False(PasswordHasher.Verify("yanlisSifre", hash));
    }

    [Fact(DisplayName = "Verify: null hash → false (crash yok)")]
    public void Verify_NullHash_False()
    {
        Assert.False(PasswordHasher.Verify("herhangiSifre", null));
    }

    [Fact(DisplayName = "Verify: boş hash string → false")]
    public void Verify_BosHash_False()
    {
        Assert.False(PasswordHasher.Verify("herhangiSifre", ""));
    }

    [Fact(DisplayName = "Verify: bozuk format (tek parça) → false")]
    public void Verify_BozukFormat_False()
    {
        Assert.False(PasswordHasher.Verify("sifre", "bu-gecersiz-format"));
    }

    [Fact(DisplayName = "Verify: bozuk Base64 salt → false (crash yok)")]
    public void Verify_BozukBase64_False()
    {
        Assert.False(PasswordHasher.Verify("sifre", "100000.NOT_BASE64!!!.YWJj"));
    }

    [Theory(DisplayName = "Verify: çeşitli şifreler doğru eşleşiyor")]
    [InlineData("abc")]
    [InlineData("12345678")]
    [InlineData("T3st!@#$%^&*()_+")]
    [InlineData("türkçeŞİÖÜÇĞ")]
    public void Verify_CesitliSifreler_True(string sifre)
    {
        var hash = PasswordHasher.Hash(sifre);
        Assert.True(PasswordHasher.Verify(sifre, hash));
    }
}

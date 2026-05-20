using QRDestekliStokVeBarkodYonetimi.Services;
using Xunit;

namespace QRDestekliStokVeBarkodYonetimi.Tests.Auth;

/// <summary>
/// Bellek içi şifre değişikliği OTP servisi birim testleri — DB gerektirmez.
/// </summary>
public sealed class SifreDegistirDogrulamaServiceTests
{
    private sealed class FixedUtcTimeProvider : TimeProvider
    {
        private DateTimeOffset _utcNow;

        public FixedUtcTimeProvider(DateTimeOffset startUtc)
        {
            _utcNow = startUtc;
        }

        public void Advance(TimeSpan delta) => _utcNow += delta;

        public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;

        public override DateTimeOffset GetUtcNow() => _utcNow;
    }

    [Fact]
    public void Dogrula_DogruKod_HashDondururVeKaydiSiler()
    {
        var clock = new FixedUtcTimeProvider(new DateTimeOffset(2026, 1, 15, 12, 0, 0, TimeSpan.Zero));
        var sut = new SifreDegistirDogrulamaService(clock);
        var hash = "pbkdf2.test.hash.placeholdersufficientlength";

        var kod = sut.OlusturVeKaydet(42, hash);

        Assert.Equal(6, kod.Length);
        Assert.True(kod.All(char.IsDigit));

        var (ok, got) = sut.Dogrula(42, kod);
        Assert.True(ok);
        Assert.Equal(hash, got);

        var yanlisIkinci = sut.Dogrula(42, kod);
        Assert.False(yanlisIkinci.Ok);
        Assert.Null(yanlisIkinci.YeniSifreHash);
    }

    [Fact]
    public void Dogrula_YanlisKod_Red()
    {
        var sut = new SifreDegistirDogrulamaService();
        sut.OlusturVeKaydet(7, "hash_a");

        var r = sut.Dogrula(7, "999999");

        Assert.False(r.Ok);
        Assert.Null(r.YeniSifreHash);
        Assert.True(sut.BekleyenVar(7));
    }

    [Fact]
    public void Dogrula_SuresiDolmus_Red()
    {
        var clock = new FixedUtcTimeProvider(new DateTimeOffset(2026, 3, 1, 10, 0, 0, TimeSpan.Zero));
        var sut = new SifreDegistirDogrulamaService(clock);

        var kod = sut.OlusturVeKaydet(99, "hash_exp");
        clock.Advance(TimeSpan.FromMinutes(11));

        var r = sut.Dogrula(99, kod);
        Assert.False(r.Ok);
        Assert.Null(r.YeniSifreHash);
        Assert.False(sut.BekleyenVar(99));
    }

    [Fact]
    public void Iptal_BekleyeniKaldirilir()
    {
        var sut = new SifreDegistirDogrulamaService();
        sut.OlusturVeKaydet(5, "h");
        Assert.True(sut.BekleyenVar(5));

        sut.Iptal(5);
        Assert.False(sut.BekleyenVar(5));
        Assert.False(sut.Dogrula(5, "000001").Ok);
    }
}

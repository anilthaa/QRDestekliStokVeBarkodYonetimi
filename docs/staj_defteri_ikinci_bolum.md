# Staj Defteri — İkinci Bölüm (17 Nisan – 5 Haziran 2026)

**Proje:** QR Destekli Stok ve Barkod Yönetimi | .NET 8, Blazor Server, Radzen, PostgreSQL, Dapper  
**Staj günleri:** Çarşamba, Perşembe, Cuma

---

## 17 Nisan 2026

- Domain modelleri (ürün, kullanıcı, stok, form) ve DataService/DBClass veri erişim katmanı geliştirildi.
- PasswordHasher yapılandırıldı; parola hashleme akışı entegre edildi.
- JwtService ve AuthStateService ile kimlik doğrulama altyapısı kuruldu.
- Login/Register ekranları AuthLayout altında geliştirildi; cookie oturumu yapılandırıldı.

## 22 Nisan – 24 Nisan Haftası

- Radzen DataGrid tabanlı liste şablonu oluşturuldu; birim modülü CRUD sayfaları geliştirildi.
- Kategori, kullanıcı, kullanıcı tipi ve ürün modülleri için liste/tanım formları entegre edildi.
- QrService geliştirilerek ürün barkod ve QR kod sayfaları yapılandırıldı.
- NavMenu yeni modül rotalarıyla güncellendi.

## 29 Nisan – 1 Mayıs Haftası

- Stok giriş, stok çıkış ve stok hareketleri sayfaları geliştirildi; barkodTarayici.js entegre edildi.
- Dashboard özet modeli ve Home.razor kartları yapılandırıldı.
- YetkiService, kullanıcı/kullanıcı tipi yetki ekranları ve dinamik NavMenu geliştirildi.
- AccessDenied eklendi; tüm listelerde salt okuma/yazma RBAC kontrolleri yayıldı.

## 6 Mayıs – 8 Mayıs Haftası

- xUnit test projesi solution'a eklendi ve yapılandırıldı.
- JwtService, PasswordHasher ve SifreDegistirDogrulamaService için birim testleri yazıldı.
- YetkiServiceTests ve YetkiTipiTests geliştirildi; mock IYetkiDataAccess/IAuthState entegre edildi.
- QrServiceTests eklendi; dotnet test ile test paketi doğrulandı.

## 20 Mayıs – 22 Mayıs Haftası

- Profil sayfası, SifremiUnuttum/SifreSifirla ve SifreSifirlamaTokenService geliştirildi; EmailService SMTP altyapısı yapılandırıldı.
- ExportService, ExportButton ve dosyaIndir.js ile Excel/CSV dışa aktarma eklendi; dashboard CSS optimize edildi.
- KullaniciHesapOnayTokenServiceTests dahil test entegrasyonu tamamlandı; IAuthState/IYetkiDataAccess arayüzleri ayrıştırıldı.
- passwordToggle.js, DialogFragments ve DataGrid FilterMode.Simple geçişi uygulandı; NavMenu profil bağlantısı entegre edildi.
- Dashboard kartlarından stok hareketlerine tarih/tip parametreli URL yönlendirmesi yapılandırıldı.

## 3 Haziran – 5 Haziran Haftası

- StokIslemleri hub sayfası geliştirildi; stok hareketlerine ürün görseli ve inceleme-düzenleme diyalogu eklendi.
- ProfilResimKirpma, ProfilResimIslemci ve ProfilResimKirpState ile profil kırpma akışı entegre edildi; SignalR hub limiti yapılandırıldı.
- Stok hareketleri filtre mantığı ayrıştırıldı; dinamik şifre sıfırlama URL ve auth form hata korunması düzeltildi.
- Ürün karakter sınırı doğrulaması, üç nokta menü, sıra numarası ve grid yenileme optimize edildi; auth sayfaları AuthCardHeader ile birleştirildi. *(05.06.2026)*

---

*Atlanan günler: 23 Nisan (resmi tatil), 1 Mayıs (resmi tatil), 13–15 Mayıs (staja gidilmedi), 27–29 Mayıs (Kurban Bayramı)*

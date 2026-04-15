# Architecture Hardening Backlog

Bu dosya, `Application + Infrastructure` ayrımından sonra yapılacak iyileştirmeleri takip etmek için tutulur.

## 1) Application Result Contract
- [ ] `Application` katmanında HTTP'den bağımsız bir `Result<T>` modeli tanımla.
- [ ] Başarısız durumlar için standart hata kodu/kategori seti oluştur.
- [ ] Controller katmanında `Result<T> -> APIResult<T>` mapleme standardı belirle.

## 2) Capability Policy Merkezi
- [ ] Provider capability kontrollerini (`SupportsInvoiceCreate/Cancel/Download`) policy helper içinde merkezileştir.
- [ ] Application servislerinde tekrarlanan capability guard kodlarını sadeleştir.

## 3) Mapping Basitleştirme
- [ ] `Invoice` payload birleştirmelerini küçük mapper/factory sınıflarına taşı.
- [ ] Controller içinde kalan DTO birleştirme ve defaultlama kodlarını minimize et.

## 4) Gözlemlenebilirlik
- [ ] Provider çağrılarında structured logging standartlarını tanımla.
- [ ] Correlation id akışını tüm endpoint -> application -> provider zincirine taşı.

## 5) Test İskeleti
- [ ] `IntegrationService.Application.Tests` projesi aç.
- [ ] `OrderApplicationService` için temel unit testler (resolver mock, happy path, provider not found) ekle.
- [ ] `InvoiceApplicationService` için capability guard ve operasyon testleri ekle.

## 6) Güvenlik ve Bağımlılık Sağlığı
- [ ] `IntegrationService.Providers.N11` içindeki `NU1903` uyarısını gidermek için bağımlılık güncellemesi planla.
- [ ] Bağımlılık taramasını CI adımı haline getir.

# ADR-0002: Merkezi JWT doğrulaması ve güvenilir kullanıcı kimliği sınırı

- Durum: Kabul edildi
- Tarih: 2026-07-14
- Karar sahipleri: Vitrin geliştirme ekibi

## Bağlam

Servisler kullanıcı kimliğini farklı yöntemlerle çıkarıyordu. Bazı endpointler JWT'yi yalnızca decode ediyor, bazıları imzayı özel bir yardımcıyla doğruluyor, bazıları ise `UserId` veya `MakerId` değerini doğrudan request body'den kabul ediyordu. Gateway'deki doğrulama tek başına yeterli değildi; servislerin Compose ağı içinden doğrudan çağrılması halinde aynı güvenlik sınırı uygulanmıyordu.

External login endpointi de e-posta, provider kimliği ve profil bilgilerini istemciden doğrulamadan kabul ederek geçerli bir Vitrin JWT'si üretebiliyordu.

## Karar

1. Gateway ve kullanıcıya açık bütün servisler ortak `AddVitrinJwtAuthentication` yapılandırmasını kullanacaktır.
2. JWT imzası, issuer, audience, süre ve izin verilen algoritma her serviste doğrulanacaktır.
3. Yetki kuralları endpoint içinde string karşılaştırmalarıyla değil, ortak `AdminOnly` ve `MakerOrAdmin` policy'leriyle uygulanacaktır.
4. İşlem yapan kullanıcının kimliği yalnızca doğrulanmış JWT içindeki `sub` claim'inden alınacaktır. Request body içindeki `UserId` veya `MakerId` caller kimliği olarak kabul edilmeyecektir.
5. Kaynak sahipliği gerektiren işlemler, doğrulanmış `sub` ile kaynağın sahibi karşılaştırılarak yetkilendirilecektir.
6. Google login için ID token Google doğrulama endpointinde kontrol edilecek; audience, issuer ve verified email doğrulanacaktır.
7. GitHub login için access token ile GitHub kullanıcı ve e-posta endpointleri çağrılacak; yalnız doğrulanmış e-posta kabul edilecektir.
8. Aynı e-posta farklı bir authentication yöntemiyle kayıtlıysa otomatik hesap birleştirme yapılmayacaktır.

## Sonuçlar

### Olumlu

- Servislerin authentication davranışı tek noktadan yönetilir.
- İç ağdan doğrudan servis çağrısı Gateway'i atlayarak yetki kazanamaz.
- Sahte veya süresi dolmuş tokenlardan claim okunmaz.
- IDOR ve kimlik taklidi riskleri caller identity sınırında azaltılır.
- Policy adları ve claim tipleri servisler arasında tutarlı kalır.
- OAuth sağlayıcı bilgileri istemci beyanı olmaktan çıkar, sağlayıcı tarafından doğrulanmış kimliğe dönüşür.

### Maliyet ve kısıtlar

- JWT sırrı bütün token doğrulayan servislere güvenli biçimde dağıtılmalıdır.
- OAuth login sırasında Auth servisi sağlayıcıya ağ çağrısı yapar; timeout ve sağlayıcı kesintileri login akışını etkileyebilir.
- Farklı sağlayıcıdaki mevcut hesaplar otomatik bağlanmaz. Güvenli, yeniden kimlik doğrulamalı bir account-linking akışı ayrıca tasarlanmalıdır.
- Gateway doğrulama yapsa bile servislerin de doğrulama yapması küçük bir işlem maliyeti oluşturur; bu maliyet defense-in-depth için kabul edilmiştir.

## Takip işleri

- JWT signing key rotation ve `kid` desteği
- Asimetrik anahtar veya merkezi identity provider değerlendirmesi
- Güvenli account-linking akışı
- Authentication/authorization entegrasyon testleri
- Login/register ve external-login rate limiting
- OAuth sağlayıcı çağrıları için gözlemlenebilirlik ve kontrollü retry politikası

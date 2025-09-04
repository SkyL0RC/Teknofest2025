# Telefon Rehberi - Frontend

Bu proje, Telefon Rehberi uygulamasının frontend kısmıdır.

## Teknolojiler

- **Angular.js 1.8.3** - JavaScript Framework
- **Bootstrap 5** - CSS Framework
- **Font Awesome** - Icons
- **AG Grid** - Data Grid Component
- **HTTP Server** - Development Server

## Özellikler

- ✅ Kişi Ekleme
- ✅ Kişi Güncelleme
- ✅ Kişi Silme
- ✅ Kişi Arama
- ✅ AG Grid ile Tablo Görünümü
- ✅ Responsive Tasarım
- ✅ Bootstrap 5 UI
- ✅ Font Awesome İkonları
- ✅ Kullanıcı Bildirimleri
- ✅ Silme Onayı

## Kurulum

### Gereksinimler

- Node.js (npm ile birlikte gelir)
- Backend API'nin çalışıyor olması

### Çalıştırma

1. Proje dizinine gidin:
```bash
cd Frontend
```

2. Bağımlılıkları yükleyin:
```bash
npm install
```

3. Development server'ı başlatın:
```bash
npm run dev
```

4. Tarayıcıda açın:
- Frontend: http://localhost:3000

## Backend Bağlantısı

Frontend, backend API'sine `http://localhost:5180` adresinden bağlanır. Backend'in çalışıyor olması gerekir.

## Proje Yapısı

```
Frontend/
├── index.html          # Ana HTML dosyası
├── app.js             # Angular.js uygulaması
├── package.json       # Node.js bağımlılıkları
└── README.md         # Bu dosya
```

## Özellikler Detayı

### Kişi Yönetimi
- **Ekleme**: Yeni kişi ekleme formu
- **Güncelleme**: Mevcut kişi bilgilerini düzenleme
- **Silme**: Kişi silme (onay ile)
- **Arama**: Ad, soyad, telefon, email ile arama

### UI/UX
- **Responsive**: Mobil uyumlu tasarım
- **AG Grid**: Gelişmiş tablo özellikleri
- **Bootstrap 5**: Modern UI bileşenleri
- **Font Awesome**: Güzel ikonlar
- **Alert Mesajları**: İşlem sonuçları için bildirimler

## API Entegrasyonu

Frontend, backend API'si ile şu endpoint'leri kullanır:

- `GET /api/contacts` - Kişileri listele
- `POST /api/contacts` - Yeni kişi ekle
- `PUT /api/contacts/{id}` - Kişi güncelle
- `DELETE /api/contacts/{id}` - Kişi sil
- `GET /api/contacts/search?q={term}` - Kişi ara

## Geliştirme

VS Code ile açmak için:

```bash
code .
```

## Not

Bu frontend, Angular.js 1.x kullanır. Modern Angular (2+) için ayrı bir proje oluşturulabilir. 
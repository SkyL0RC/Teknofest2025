# Telefon Rehberi - Backend API

Bu proje, Telefon Rehberi uygulamasının backend API'sidir.

## Teknolojiler

- **ASP.NET Core 8.0 Web API**
- **Entity Framework Core (Code First)**
- **MS SQL Server (LocalDB)**
- **NLog** - Logging
- **Swagger/OpenAPI** - API Documentation
- **CORS** - Cross-Origin Resource Sharing

## Özellikler

- ✅ Katmanlı Mimari (Layered Architecture)
- ✅ Generic Repository Design Pattern
- ✅ Entity Framework Core Code First
- ✅ NLog ile Logging
- ✅ Swagger API Dokümantasyonu
- ✅ CORS Desteği
- ✅ Model Validation
- ✅ Data Transfer Objects (DTOs)

## Kurulum

### Gereksinimler

- .NET 8.0 SDK
- SQL Server LocalDB (Visual Studio ile birlikte gelir)

### Çalıştırma

1. Proje dizinine gidin:
```bash
cd Backend
```

2. Bağımlılıkları yükleyin:
```bash
dotnet restore
```

3. Veritabanını oluşturun:
```bash
dotnet ef database update
```

4. Uygulamayı çalıştırın:
```bash
dotnet run
```

5. Tarayıcıda açın:
- API: http://localhost:5180
- Swagger: http://localhost:5180/swagger

## API Endpoints

### Contacts

- `GET /api/contacts` - Tüm kişileri listele
- `GET /api/contacts/{id}` - ID'ye göre kişi getir
- `POST /api/contacts` - Yeni kişi ekle
- `PUT /api/contacts/{id}` - Kişi güncelle
- `DELETE /api/contacts/{id}` - Kişi sil
- `GET /api/contacts/search?q={term}` - Kişi ara

## Proje Yapısı

```
Backend/
├── Controllers/          # API Controllers
├── Services/            # Business Logic Layer
├── Repositories/        # Data Access Layer
├── Models/              # Entity Models
├── DTOs/               # Data Transfer Objects
├── Data/               # Entity Framework Context
└── Properties/         # Configuration
```

## Veritabanı

Uygulama ilk çalıştırıldığında otomatik olarak veritabanı oluşturulur ve örnek veriler eklenir.

## Logging

Tüm işlemler NLog ile loglanır. Loglar `logs/` klasöründe saklanır. 
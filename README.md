<h1 align="center">🔥 YANDES - Yangın Algılama ve Destek Sistemi 🔥</h1>
<p align="center">
  <i>Uydu verileri ile yangın tespiti ve afet yönetimi</i><br/>
  <b>#Teknofest2025 🚀</b>
</p>

<p align="center">
  <a href="https://dotnet.microsoft.com/download"><img src="https://img.shields.io/badge/.NET-9.0-blue.svg" alt=".NET 9.0"/></a>
  <a href="https://python.org"><img src="https://img.shields.io/badge/Python-3.11+-green.svg" alt="Python 3.11+"/></a>
  <a href="LICENSE"><img src="https://img.shields.io/badge/License-MIT-yellow.svg" alt="MIT License"/></a>
  <img src="https://img.shields.io/badge/Status-Active-brightgreen.svg" alt="Status"/>
</p>

---

## 🌟 Proje Hakkında  
> **YANDES**, uydu verilerini kullanarak **yangın tespiti, analizi ve afet yönetimi** için geliştirilmiş kapsamlı bir web uygulamasıdır.  
> Hem bilimsel algoritmaları hem de kullanıcı dostu arayüzü ile **çevreye duyarlı bir teknoloji** sunar.  

---

## ✨ Özellikler  

| Kategori | Özellikler |
|----------|------------|
| 🛰️ **Uydu Veri İşleme** | Sentinel-1 SAR (bulut bağımsız), Sentinel-2 & Landsat 8-9 optik veriler, NDVI & NBR algoritmaları, gerçek zamanlı değişim tespiti |
| 🔬 **Analiz Algoritmaları** | NDVI → Vegetasyon yoğunluğu, NBR → Yanma şiddeti, Değişim Tespiti → Öncesi-sonrası karşılaştırma, İstatistiksel raporlama |
| 🎮 **Kullanıcı Deneyimi** | İnteraktif harita, oyunlaştırma, kullanıcı profil yönetimi, modern ve responsive tasarım |

---

## 🚀 Hızlı Başlangıç  

### Gereksinimler
- [.NET 9.0 SDK](https://dotnet.microsoft.com/download)  
- [Python 3.11+](https://python.org)  
- [Node.js](https://nodejs.org)  

### Kurulum
```bash
# 1. Projeyi klonla
git clone https://github.com/yourusername/yandes.git
cd yandes

# 2. Python bağımlılıklarını yükle
pip install rasterio numpy scipy matplotlib plotly folium bokeh geopandas sentinelsat

# 3. Backend'i başlat
cd Backend/Fire
dotnet run

# Backend: http://localhost:5299

# 4. Frontend'i başlat
cd Frontend
npm install
npm run dev

# Frontend: http://localhost:3000
```

📌 Açılış:  
- Ana sayfa → `http://localhost:3000`  
- Uydu veri keşfi → `http://localhost:3000/satellite-explorer.html`  

---

## 📊 Veri Setleri  

| Veri Kaynağı | Çözünürlük | Özellikler | Zaman Aralığı |
|--------------|------------|------------|---------------|
| **Sentinel-1 SAR** | 10m | Bulut örtüsünden etkilenmez | 12 Haz – 6 Tem 2025 |
| **Landsat 8 & 9** | 30m | 7 spektral bant | Yangın öncesi (12-13 Haz) / sonrası (6-7 Tem) |
| **Sentinel-2 Optik** | 10–20m | 10 spektral bant | Yangın öncesi (12 Haz) / sonrası (5 Tem) |

---

## 🔧 API Endpoints  

### 📂 Veri Keşfi  
- `GET /api/dataexplorer/datasets` → Veri setlerini listele  
- `GET /api/dataexplorer/dataset/{dataType}/{*dataPath}` → Veri detayları  
- `POST /api/dataexplorer/process` → Veri işleme  
- `POST /api/dataexplorer/analyze` → Yangın analizi  

### 👤 Kullanıcı Yönetimi  
- `POST /api/auth/register` → Yeni kullanıcı  
- `POST /api/auth/login` → Giriş yap  
- `GET /api/profile` → Profil bilgileri  
- `PUT /api/profile` → Profil güncelleme  

### 🔥 Yangın Yönetimi  
- `GET /api/fires/recent` → Son yangınlar  
- `GET /api/fires/nearby` → Yakındaki yangınlar  
- `POST /api/fires` → Yangın raporu oluştur  

---

## 🏗️ Proje Yapısı  
```bash
yandes/
├── Backend/
│   └── Fire/               # ASP.NET Core Web API
│       ├── Controllers/    # API Controllers
│       ├── Services/       # İş mantığı
│       ├── DTOs/           # Veri transfer nesneleri
│       ├── PythonScripts/  # Python entegrasyonu
│       └── Program.cs
├── Frontend/               # Angular.js Frontend
│   ├── index.html
│   ├── satellite-explorer.html
│   └── app.js
├── datas/                  # Uydu veri setleri
└── README.md
```

---

## 🧪 Test  

### Python Scriptleri
```bash
# Landsat veri testi
python Backend/Fire/PythonScripts/satellite_processor.py landsat "veri_yolu"

# Sentinel-1 testi
python Backend/Fire/PythonScripts/satellite_processor.py sentinel1 "veri_yolu"

# NDVI hesaplama
python Backend/Fire/PythonScripts/satellite_processor.py ndvi "red_band" "nir_band" "output"
```

### API Testleri
```bash
# Veri setlerini listele
curl http://localhost:5299/api/dataexplorer/datasets

# Kullanıcı kaydı
curl -X POST http://localhost:5299/api/auth/register \
-H "Content-Type: application/json" \
-d '{"username":"test","email":"test@example.com","password":"password123"}'
```

---

## 📈 Analiz Algoritmaları  

### 🌱 NDVI
```math
NDVI = (NIR - Red) / (NIR + Red)
```
- **Amaç**: Vegetasyon yoğunluğu  
- **Değer Aralığı**: -1 → 1  
- **Yorum**: Yüksek değer = Yoğun vegetasyon  

### 🔥 NBR
```math
NBR = (NIR - SWIR2) / (NIR + SWIR2)
```
- **Amaç**: Yanma şiddeti  
- **Değer Aralığı**: -1 → 1  
- **Yorum**: Düşük değer = Yüksek yanma şiddeti  

---

## 🔮 Yol Haritası  

- [ ] ⚡ Gerçek Zamanlı Veri İşleme (stream processing)  
- [ ] 🤖 Makine Öğrenmesi ile otomatik yangın tespiti  
- [ ] 🌍 3D Görselleştirme (DEM entegrasyonu)  
- [ ] 📦 Docker desteği  
- [ ] 🗄️ PostgreSQL/PostGIS entegrasyonu  
- [ ] ⚡ Redis cache ile performans artışı  

---

<p align="center">🚀 <b>YANDES - Doğa için teknoloji!</b> 🌍</p>


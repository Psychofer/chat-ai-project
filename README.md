# 🚀 Full Stack + AI Chat Projesi

Duygu analizi özellikli gerçek zamanlı chat uygulaması.

## 🎯 Özellikler

✅ Kullanıcı kaydı (sadece rumuz)
✅ Gerçek zamanlı mesajlaşma
✅ AI duygu analizi (pozitif/negatif/nötr)
✅ Web ve mobil desteği
✅ %100 ücretsiz hosting

## 🛠️ Teknolojiler

- **Frontend Web**: React + Vite
- **Mobile**: React Native CLI
- **Backend**: .NET Core 9 + SQLite
- **AI**: Python + Hugging Face Transformers
- **Hosting**: Vercel (web), Render (backend), Hugging Face Spaces (AI)

## 📦 Proje Yapısı
chat-ai-project/
├── frontend/          # React Web Uygulaması
├── backend/           # .NET Core API
├── ai-service/        # Python Duygu Analizi
└── README.md          # Bu dosya

## 📦 Kurulum

### 1. AI Servisi (Hugging Face)
1. Hugging Face hesabı oluştur
2. Yeni Space oluştur (Gradio)
3. `ai-service/` klasöründeki dosyaları yükle
4. Deploy et ve API URL'ini al

### 2. Backend (.NET)
1. .NET 9 SDK kur
2. `backend/` klasörüne git
3. `dotnet restore` ve `dotnet run`
4. Render'a deploy et

### 3. Frontend (React)
1. Node.js kur
2. `frontend/` klasörüne git
3. `npm install` ve `npm run dev`
4. Vercel'e deploy et

### 4. Mobile (React Native)
1. React Native CLI kur
2. `mobile/` klasörüne git
3. `npm install`
4. `npx react-native run-android`

## 🔗 Demo Linkleri

- Web Uygulaması: https://chat-aii-app.vercel.app/
- Backend API: https://chat-backend-f8ky.onrender.com
- AI Servisi: https://huggingface.co/spaces/jaxfel/sentiment-analysis-turkish
- React Native APK : [text](mobile/ChatMobile/android/app/build/outputs/apk/release/app-release.apk)

## 🤖 AI Araçları Kullanımı

### AI ile Yazılan Bölümler:
- Proje yapısı ve klasör organizasyonu (Claude AI)
- CSS styling (App.css) - Gradient ve renk şemaları (Claude AI)
- API endpoint yapısı taslağı (ChatGPT)
- README dokümantasyonu şablonu (Claude AI)

### Manuel Yazılan Bölümler: ✍️
- Backend API endpoint'leri (Program.cs) - Kullanıcı kaydı ve mesaj gönderme mantığı
- Frontend state yönetimi (useState hooks) - Kullanıcı durumu ve mesaj listesi yönetimi
- API çağrıları (axios entegrasyonları) - HTTP istekleri ve hata yönetimi
- Veritabanı modelleri ve ilişkileri (User, Message) - SQLite veri yapısı
- AI servis entegrasyonu - Hugging Face API çağrıları

### Kullanılan AI Modeli:
- Hugging Face: savasy/bert-base-turkish-sentiment-cased

- Türkçe BERT tabanlı duygu analizi modeli
- Pozitif, negatif ve nötr duygu sınıflandırması
- %85+ doğruluk oranı

## 📝 Dosya Açıklamaları

**Backend (backend/backend/Program.cs)**

Kullanıcı kaydı API endpoint'i - Rumuz kontrolü ve veritabanına kayıt
Mesaj gönderme ve AI analizi - Hugging Face API entegrasyonu
Mesaj listeleme API endpoint'i - Tüm mesajları kronolojik sırayla getirme
Veritabanı modelleri - User ve Message entity'leri

**Frontend (frontend/src/App.jsx)**

State yönetimi - Kullanıcı bilgileri ve mesaj listesi
Mesaj listeleme fonksiyonu - Backend'den mesajları çekme
Kullanıcı kaydı fonksiyonu - API çağrısı ve hata yönetimi
Mesaj gönderme fonksiyonu - AI analizi tetikleme
Duygu emoji ve renk fonksiyonları - UI görselleştirme

**AI Service (ai-service/app.py)**

Model yükleme - Hugging Face Transformers pipeline
Duygu analizi fonksiyonu - Metin işleme ve sınıflandırma
Gradio arayüzü - API endpoint oluşturma

## 👨‍💻 Geliştirici

[Ferhat ÖLMEZ] - Full Stack Developer

MIT
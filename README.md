# Ai1669

سرویس CallCenter یک API در ASP.NET Core 8 است. فایل صوتی فارسی را به OpenAI می‌فرستد، نام شهر را در متن با فهرست شهرها تطبیق می‌دهد و پاسخ فارسی را با صدای زن به‌صورت MP3 برمی‌گرداند.

## نصب و اجرا

.NET 8 SDK و Visual Studio 2022 نسخهٔ 17.8 یا جدیدتر را نصب کنید. برای اجرای HTTPS محلی در ویندوز، به گواهی توسعه اعتماد کنید:

```powershell
dotnet dev-certs https --trust
dotnet restore Ai1669.sln
dotnet build Ai1669.sln --no-restore
dotnet user-secrets set "OpenAI:ApiKey" "<your OpenAI API key>" --project src/CallCenter
dotnet run --project src/CallCenter
```

در Visual Studio، فایل `Ai1669.sln` را باز کنید و پروژهٔ CallCenter را اجرا کنید. Swagger در محیط Development در `https://localhost:7209/swagger` باز می‌شود. در محیط اجرا می‌توانید کلید را از طریق متغیر `OpenAI__ApiKey` و سامانهٔ مدیریت اسرار تزریق کنید. در صورت نیاز، از `appsettings.Development.json.example` برای تنظیم مدل و صدا استفاده کنید.

## API

`POST /api/voice-coverage` یک فیلد `audio` از نوع `multipart/form-data` دریافت می‌کند. فایل‌های WAV، MP3، MPGA، MPEG، M4A، MP4، WEBM، FLAC و OGG تا سقف ۲۴ MiB پذیرفته می‌شوند.

```powershell
curl.exe --fail -F "audio=@sample.wav" https://localhost:7209/api/voice-coverage --output reply.mp3
```

پاسخ موفق `audio/mpeg` است. هدر `X-Coverage-Status` یکی از مقادیر `Active`، `Inactive`، `UnknownCity`، `MultipleCities` و `NoSpeech` را نشان می‌دهد. هدر `X-Audio-Generated-By: AI` نشان می‌دهد که صدا مصنوعی است. رابط کاربر باید هنگام پخش این موضوع را به کاربر اطلاع دهد. خطاهای HTTP در قالب Problem Details گزارش می‌شوند.

| شناسه | شهر | خدمات فعال |
| --- | --- | --- |
| 1 | بندرعباس | بله |
| 2 | تهران | بله |
| 3 | شیراز | خیر |

تبدیل گفتار با `gpt-4o-mini-transcribe` و تولید صدا با `gpt-4o-mini-tts` و صدای `nova` انجام می‌شود. فایل ورودی و متن پاسخ برای پردازش به OpenAI ارسال می‌شوند.

## ساختار

تنها پروژهٔ Solution در `src/CallCenter` قرار دارد. Controller فایل را اعتبارسنجی می‌کند و عملیات را به Application می‌سپارد. Application از قراردادهای تبدیل گفتار، تولید صدا و خواندن شهرها استفاده می‌کند. Infrastructure کلاینت OpenAI و مخزن درون‌حافظه‌ای را فراهم می‌کند. برای اتصال آتی به SQL Server، پیاده‌سازی `ICityRepository` را جایگزین و ثبت آن را در `Program.cs` تغییر دهید.

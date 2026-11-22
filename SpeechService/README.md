# Local Whisper on Windows

Use 64-bit Python 3.12, the .NET 8 SDK, the x64 Microsoft Visual C++ Redistributable (required by CTranslate2), and a reachable SQL Server. The speech service uses faster-whisper's `large-v3` model with CPU, int8, Persian transcription, and beam size 5. Inference runs one request at a time and long recordings can take several minutes. PyAV supplies the audio decoding libraries.

## Install and start the speech service

Run these commands in PowerShell from the repository root:

```powershell
cd SpeechService
py -3.12 -m venv .venv
.\.venv\Scripts\python.exe -m pip install -r requirements.txt
.\.venv\Scripts\python.exe -m uvicorn app:app --host 127.0.0.1 --port 8000
```

The first startup downloads [Systran/faster-whisper-large-v3](https://huggingface.co/Systran/faster-whisper-large-v3) into the Hugging Face cache. Allow internet access and several gigabytes of free disk space. Subsequent starts reuse the cache. For offline setup, copy the complete CTranslate2 model directory from a connected machine and set `$env:WHISPER_MODEL_PATH = "C:\models\faster-whisper-large-v3"` to the directory containing `model.bin` before starting Uvicorn. Keep the service bound to loopback because its endpoints have no authentication.

Check readiness in a second PowerShell window:

```powershell
Invoke-RestMethod http://127.0.0.1:8000/health
```

To check transcription independently of the API and SQL Server, send a short Persian recording:

```powershell
curl.exe --fail -F "audio=@C:\recordings\sample.wav" http://127.0.0.1:8000/transcribe
```

## Start the ASP.NET API and test with Swagger

Apply `CallCenter/Database/001_ServiceCities.sql` to the application database and populate the city catalog as described in the [database guide](../CallCenter/Database/VoiceCoverage.md). Set `ConnectionStrings__MainConnection` securely in the API process environment. Run these commands from the repository root:

```powershell
dotnet restore Ai1669.sln
dotnet build Ai1669.sln --no-restore
dotnet run --project CallCenter --launch-profile http
```

Open `http://localhost:5028/swagger`, select `POST /api/voice-coverage`, and submit a recording in the `audio` field. Compare the returned `transcript` with a reference transcript and check the resulting city and coverage status. Use the same recordings and reference text across accuracy runs.

If the local speech service uses another port, set `Speech__BaseUrl` in the API process environment. The default is `http://127.0.0.1:8000/`. The default request timeout is 30 minutes. Override it with a .NET TimeSpan value such as `Speech__Timeout=01:00:00` if needed.

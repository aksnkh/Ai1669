from contextlib import asynccontextmanager
from threading import Lock
import os

from av import FFmpegError
from fastapi import FastAPI, HTTPException, Request, UploadFile
from faster_whisper import WhisperModel

MAX_AUDIO_BYTES = 20 * 1024 * 1024
transcription_lock = Lock()


@asynccontextmanager
async def lifespan(app: FastAPI):
    model_path = os.environ.get("WHISPER_MODEL_PATH") or "large-v3"
    app.state.model = WhisperModel(model_path, device="cpu", compute_type="int8")
    yield


app = FastAPI(lifespan=lifespan)


@app.get("/health")
def health():
    return {"status": "ready"}


@app.post("/transcribe")
def transcribe(audio: UploadFile, request: Request):
    if not audio.size:
        raise HTTPException(status_code=400, detail="Audio is empty.")
    if audio.size > MAX_AUDIO_BYTES:
        raise HTTPException(status_code=413, detail="Audio exceeds 20 MiB.")

    try:
        with transcription_lock:
            segments, _ = request.app.state.model.transcribe(
                audio.file, language="fa", task="transcribe", beam_size=5
            )
            text = "".join(segment.text for segment in segments).strip()
    except FFmpegError as error:
        raise HTTPException(status_code=400, detail="Audio could not be decoded.") from error

    return {"text": text}

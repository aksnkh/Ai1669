const start = document.querySelector("#start");
const stop = document.querySelector("#stop");
const status = document.querySelector("#status");
const transcriptPanel = document.querySelector("#transcript-panel");
const transcript = document.querySelector("#transcript");
const reply = document.querySelector("#reply");

const api = "/api/voice-coverage";
const coverageLabels = {
    Active: "خدمات در این شهر فعال است.",
    Inactive: "خدمات در این شهر فعال نیست.",
    UnknownCity: "شهر در فهرست پیدا نشد.",
    MultipleCities: "لطفاً فقط نام یک شهر را بگویید.",
    NoSpeech: "گفتار واضحی دریافت نشد."
};

let current;
let replyUrl;

function closeMicrophone(session) {
    clearTimeout(session.timer);
    session.connection?.close();
    session.stream?.getTracks().forEach(track => track.stop());
    session.connection = undefined;
    session.stream = undefined;
}

function finish(session, message, cancel = false) {
    if (current !== session) return;
    current = undefined;
    if (cancel) session.abort.abort();
    closeMicrophone(session);
    status.textContent = message;
    start.disabled = false;
    stop.hidden = true;
}

function resetReply() {
    reply.pause();
    reply.removeAttribute("src");
    reply.load();
    reply.hidden = true;
    if (replyUrl) URL.revokeObjectURL(replyUrl);
    replyUrl = undefined;
}

async function post(path, data, signal) {
    const result = await fetch(api + path, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(data),
        signal
    });
    if (!result.ok) {
        const problem = await result.json().catch(() => ({}));
        const message = {
            404: "گفت‌وگوی زنده در این محیط فعال نیست.",
            429: "درخواست‌های زیادی فرستاده شده است. کمی بعد دوباره تلاش کنید."
        }[result.status];
        throw new Error(message || problem.title || "سرویس پوشش خدمات پاسخ نداد.");
    }
    return result;
}

async function answer(session, text) {
    if (current !== session || session.responding) return;
    session.responding = true;
    closeMicrophone(session);

    const finalText = text.trim();
    transcript.textContent = finalText;
    transcriptPanel.hidden = !finalText;
    if (!finalText) {
        finish(session, "گفتار واضحی دریافت نشد. دوباره تلاش کنید.");
        return;
    }

    status.textContent = "در حال بررسی پوشش و ساخت پاسخ…";
    try {
        const result = await post("/text", { transcript: finalText }, session.abort.signal);
        const audio = await result.blob();
        if (current !== session) return;
        const url = URL.createObjectURL(audio);
        replyUrl = url;
        reply.src = url;
        reply.hidden = false;
        const message = coverageLabels[result.headers.get("X-Coverage-Status")] || "پاسخ آماده است.";
        finish(session, message);
        await reply.play().catch(() => {
            if (replyUrl === url) {
                status.textContent = message + " برای شنیدن پاسخ، پخش را بزنید.";
            }
        });
    } catch (error) {
        finish(session, error.message);
    }
}

function handleEvent(session, message) {
    if (current !== session || session.responding) return;
    const event = JSON.parse(message.data);
    switch (event.type) {
        case "input_audio_buffer.speech_started":
            status.textContent = "در حال شنیدن…";
            break;
        case "conversation.item.input_audio_transcription.delta":
            session.partial += event.delta;
            transcript.textContent = session.partial;
            transcriptPanel.hidden = false;
            break;
        case "conversation.item.input_audio_transcription.completed":
            void answer(session, event.transcript ?? "");
            break;
        case "conversation.item.input_audio_transcription.failed":
        case "error":
            finish(session, "تبدیل گفتار به متن انجام نشد. دوباره تلاش کنید.", true);
            break;
    }
}

start.addEventListener("click", async () => {
    if (!navigator.mediaDevices?.getUserMedia || !window.RTCPeerConnection) {
        status.textContent = "برای استفاده از میکروفون، صفحه را در مرورگر سازگار روی localhost یا HTTPS باز کنید.";
        return;
    }

    resetReply();
    transcript.textContent = "";
    transcriptPanel.hidden = true;
    const session = { abort: new AbortController(), partial: "", responding: false };
    current = session;
    start.disabled = true;
    stop.hidden = false;
    status.textContent = "در انتظار اجازهٔ میکروفون…";

    try {
        const stream = await navigator.mediaDevices.getUserMedia({
            audio: { echoCancellation: true, noiseSuppression: true }
        });
        if (current !== session) {
            stream.getTracks().forEach(track => track.stop());
            return;
        }
        session.stream = stream;
        status.textContent = "در حال اتصال…";
        session.timer = setTimeout(() => finish(session, "زمان شنیدن به پایان رسید. دوباره شروع کنید.", true), 60000);

        const connection = new RTCPeerConnection();
        session.connection = connection;
        for (const track of stream.getAudioTracks()) connection.addTrack(track, stream);

        const events = connection.createDataChannel("oai-events");
        events.addEventListener("open", () => {
            if (current === session) status.textContent = "حالا صحبت کنید.";
        });
        events.addEventListener("message", message => handleEvent(session, message));
        events.addEventListener("close", () => {
            if (!session.responding) finish(session, "اتصال گفت‌وگوی زنده بسته شد.", true);
        });
        connection.addEventListener("connectionstatechange", () => {
            if (connection.connectionState === "failed") {
                finish(session, "اتصال گفت‌وگوی زنده برقرار نشد.", true);
            }
        });

        await connection.setLocalDescription(await connection.createOffer());
        const credentials = await post("/realtime/session", { language: "fa" }, session.abort.signal);
        const { value } = await credentials.json();
        const response = await fetch("https://api.openai.com/v1/realtime/calls", {
            method: "POST",
            headers: { Authorization: `Bearer ${value}`, "Content-Type": "application/sdp" },
            body: connection.localDescription.sdp,
            signal: session.abort.signal
        });
        if (!response.ok) throw new Error("OpenAI اتصال زنده را نپذیرفت.");
        await connection.setRemoteDescription({ type: "answer", sdp: await response.text() });
    } catch (error) {
        const message = error.name === "NotAllowedError" ? "اجازهٔ دسترسی به میکروفون داده نشد."
            : error.name === "NotFoundError" ? "میکروفون پیدا نشد." : error.message;
        finish(session, message, true);
    }
});

stop.addEventListener("click", () => {
    if (current) finish(current, "گفت‌وگو لغو شد.", true);
});

window.addEventListener("pagehide", () => {
    if (current) finish(current, "گفت‌وگو پایان یافت.", true);
    resetReply();
});

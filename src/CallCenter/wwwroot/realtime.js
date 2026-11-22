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

function setStart(label, disabled, ready = false) {
    start.textContent = label;
    start.disabled = disabled;
    start.classList.toggle("ready", ready);
}

function enableSpeaking(session) {
    if (current !== session || session.ready || !session.serverReady
        || session.connection.connectionState !== "connected" || session.events.readyState !== "open") return;

    session.ready = true;
    clearTimeout(session.timer);
    session.timer = setTimeout(() => finish(session, "زمان شروع صحبت به پایان رسید.", true), 60000);
    setStart("حالا صحبت کنید", false, true);
    status.textContent = "میکروفون آماده است. دکمه را بزنید و صحبت کنید.";
}

function beginListening(session) {
    session.listening = true;
    for (const track of session.stream.getAudioTracks()) track.enabled = true;
    clearTimeout(session.timer);
    session.timer = setTimeout(() => finish(session, "زمان شنیدن به پایان رسید. دوباره شروع کنید.", true), 60000);
    setStart("در حال شنیدن…", true, true);
    status.textContent = "در حال شنیدن نام شهر…";
}

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
    setStart("آماده‌سازی میکروفون", false);
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
    setStart("در حال آماده‌کردن پاسخ…", true);

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
    if (event.type === "session.created" || event.type === "transcription_session.created") {
        session.serverReady = true;
        enableSpeaking(session);
        return;
    }
    if (event.type === "error") {
        finish(session, "اتصال رونویسی زنده خطا داد. دوباره تلاش کنید.", true);
        return;
    }
    if (!session.listening) return;

    switch (event.type) {
        case "input_audio_buffer.speech_stopped":
            setStart("در حال تبدیل گفتار…", true);
            status.textContent = "در حال تبدیل گفتار به متن…";
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
            finish(session, "تبدیل گفتار به متن انجام نشد. دوباره تلاش کنید.", true);
            break;
    }
}

start.addEventListener("click", async () => {
    if (current) {
        if (current.ready && !current.listening) beginListening(current);
        return;
    }

    if (!navigator.mediaDevices?.getUserMedia || !window.RTCPeerConnection) {
        status.textContent = "برای استفاده از میکروفون، صفحه را در مرورگر سازگار روی localhost یا HTTPS باز کنید.";
        return;
    }

    resetReply();
    transcript.textContent = "";
    transcriptPanel.hidden = true;
    const session = { abort: new AbortController(), partial: "", responding: false };
    current = session;
    setStart("در حال آماده‌سازی…", true);
    stop.hidden = false;
    status.textContent = "در انتظار اجازهٔ میکروفون…";

    try {
        const stream = await navigator.mediaDevices.getUserMedia({
            audio: { echoCancellation: true, noiseSuppression: true, autoGainControl: true }
        });
        if (current !== session) {
            stream.getTracks().forEach(track => track.stop());
            return;
        }
        session.stream = stream;
        status.textContent = "در حال اتصال…";
        session.timer = setTimeout(() => finish(session, "اتصال در زمان مقرر آماده نشد.", true), 30000);

        const connection = new RTCPeerConnection();
        session.connection = connection;
        for (const track of stream.getAudioTracks()) {
            track.enabled = false;
            connection.addTrack(track, stream);
        }

        const events = connection.createDataChannel("oai-events");
        session.events = events;
        events.addEventListener("open", () => enableSpeaking(session));
        events.addEventListener("message", message => handleEvent(session, message));
        events.addEventListener("close", () => {
            if (!session.responding) finish(session, "اتصال گفت‌وگوی زنده بسته شد.", true);
        });
        connection.addEventListener("connectionstatechange", () => {
            if (["failed", "disconnected"].includes(connection.connectionState)) {
                finish(session, "اتصال گفت‌وگوی زنده برقرار نشد.", true);
                return;
            }
            enableSpeaking(session);
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

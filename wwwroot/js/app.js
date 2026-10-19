export function get(key) {
    return localStorage.getItem(key);
}

export function set(key, value) {
    localStorage.setItem(key, value);
}

export function remove(key) {
    localStorage.removeItem(key);
}

let audioCtx = null;

export function beep(frequency, durationMs, volume) {
    try {
        if (!audioCtx) {
            const Ctx = window.AudioContext || window.webkitAudioContext;
            if (!Ctx) return;
            audioCtx = new Ctx();
        }
        if (audioCtx.state === 'suspended') {
            audioCtx.resume();
        }
        const osc = audioCtx.createOscillator();
        const gain = audioCtx.createGain();
        osc.type = 'sine';
        osc.frequency.value = frequency;
        gain.gain.value = volume ?? 0.15;
        osc.connect(gain);
        gain.connect(audioCtx.destination);
        const now = audioCtx.currentTime;
        osc.start(now);
        gain.gain.setValueAtTime(gain.gain.value, now + durationMs / 1000 - 0.03);
        gain.gain.linearRampToValueAtTime(0.0001, now + durationMs / 1000);
        osc.stop(now + durationMs / 1000);
    } catch (e) {
        console.warn('beep failed', e);
    }
}

let noiseBuffer = null;

function ensureAudio() {
    if (!audioCtx) {
        const Ctx = window.AudioContext || window.webkitAudioContext;
        if (!Ctx) return null;
        audioCtx = new Ctx();
    }
    if (audioCtx.state === 'suspended') {
        audioCtx.resume();
    }
    return audioCtx;
}

function crackleBuffer(ctx) {
    if (!noiseBuffer) {
        const length = Math.floor(ctx.sampleRate * 0.8);
        noiseBuffer = ctx.createBuffer(1, length, ctx.sampleRate);
        const data = noiseBuffer.getChannelData(0);
        for (let i = 0; i < length; i++) {
            data[i] = (Math.random() * 2 - 1) * (1 - i / length);
        }
    }
    return noiseBuffer;
}

function fireworkBurst(ctx, at, volume) {
    const whistle = ctx.createOscillator();
    const whistleGain = ctx.createGain();
    whistle.type = 'sine';
    whistle.frequency.setValueAtTime(400, at);
    whistle.frequency.exponentialRampToValueAtTime(1500, at + 0.58);
    whistleGain.gain.setValueAtTime(0.0001, at);
    whistleGain.gain.exponentialRampToValueAtTime(volume * 0.3, at + 0.12);
    whistleGain.gain.exponentialRampToValueAtTime(0.0001, at + 0.6);
    whistle.connect(whistleGain);
    whistleGain.connect(ctx.destination);
    whistle.start(at);
    whistle.stop(at + 0.62);

    const pop = at + 0.62;
    const noise = ctx.createBufferSource();
    noise.buffer = crackleBuffer(ctx);
    const filter = ctx.createBiquadFilter();
    filter.type = 'bandpass';
    filter.frequency.setValueAtTime(1600, pop);
    filter.frequency.exponentialRampToValueAtTime(500, pop + 0.7);
    filter.Q.value = 0.8;
    const noiseGain = ctx.createGain();
    noiseGain.gain.setValueAtTime(0.0001, pop);
    noiseGain.gain.exponentialRampToValueAtTime(volume, pop + 0.02);
    noiseGain.gain.exponentialRampToValueAtTime(0.0001, pop + 0.75);
    noise.connect(filter);
    filter.connect(noiseGain);
    noiseGain.connect(ctx.destination);
    noise.start(pop);
    noise.stop(pop + 0.8);
}

export function firework(delaysMs, volume) {
    try {
        if (window.matchMedia && window.matchMedia('(prefers-reduced-motion: reduce)').matches) return;
        const ctx = ensureAudio();
        if (!ctx || !delaysMs) return;
        const now = ctx.currentTime + 0.05;
        for (const delay of delaysMs) {
            fireworkBurst(ctx, now + delay / 1000, volume ?? 0.12);
        }
    } catch (e) {
        console.warn('firework audio failed', e);
    }
}

// ---------- keep the screen awake ----------
// Browsers drop a screen wake lock whenever the page is hidden, so it is requested again when the
// page comes back. Where the Wake Lock API is missing or unreliable (older iOS, iOS home-screen apps
// before 18.4) a tiny muted looping video keeps the screen on instead. Playing it may need a user
// gesture, so when nothing could be acquired the next tap anywhere tries again.

const keepAwakeVideoSrc = 'data:video/mp4;base64,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';
const gestureEvents = ['pointerup', 'touchend', 'click', 'keydown'];
const iosStandalone = navigator.standalone === true;

let keepAwake = false;
let wakeLock = null;
let fallbackVideo = null;
let gestureArmed = false;

function hasWakeLock() {
    return !!wakeLock && !wakeLock.released;
}

async function acquireNativeWakeLock() {
    if (hasWakeLock()) return true;
    if (!('wakeLock' in navigator)) return false;
    try {
        const lock = await navigator.wakeLock.request('screen');
        if (!keepAwake) {
            lock.release();
            return false;
        }
        wakeLock = lock;
        lock.addEventListener('release', () => {
            if (wakeLock === lock) wakeLock = null;
        });
        return true;
    } catch (e) {
        console.warn('wake lock failed', e);
        return false;
    }
}

async function startFallbackVideo() {
    if (!fallbackVideo) {
        const video = document.createElement('video');
        video.setAttribute('playsinline', '');
        video.setAttribute('muted', '');
        video.setAttribute('aria-hidden', 'true');
        video.muted = true;
        video.loop = true;
        video.style.cssText = 'position:fixed;left:0;bottom:0;width:1px;height:1px;opacity:.01;pointer-events:none;';
        video.src = keepAwakeVideoSrc;
        document.body.appendChild(video);
        fallbackVideo = video;
    }
    if (!fallbackVideo.paused) return true;
    try {
        await fallbackVideo.play();
        return true;
    } catch (e) {
        console.warn('keep-awake video failed', e);
        return false;
    }
}

function stopFallbackVideo() {
    if (!fallbackVideo) return;
    try {
        fallbackVideo.pause();
        fallbackVideo.remove();
    } catch (e) {
        console.warn('keep-awake video stop failed', e);
    }
    fallbackVideo = null;
}

async function acquireKeepAwake() {
    if (!keepAwake || document.visibilityState !== 'visible') return;

    const native = await acquireNativeWakeLock();
    if (!keepAwake) return;

    let video = false;
    if (!native || iosStandalone) {
        video = await startFallbackVideo();
    } else {
        stopFallbackVideo();
    }

    if (keepAwake && !native && !video) {
        armGesture();
    }
}

function onGesture() {
    disarmGesture();
    if (!keepAwake) return;
    // Start the video inside the gesture itself; the async wake lock request may outlive it.
    if (fallbackVideo || !('wakeLock' in navigator) || iosStandalone) {
        startFallbackVideo();
    }
    acquireKeepAwake();
}

function armGesture() {
    if (gestureArmed) return;
    gestureArmed = true;
    gestureEvents.forEach(type => document.addEventListener(type, onGesture, true));
}

function disarmGesture() {
    if (!gestureArmed) return;
    gestureArmed = false;
    gestureEvents.forEach(type => document.removeEventListener(type, onGesture, true));
}

document.addEventListener('visibilitychange', () => {
    if (keepAwake && document.visibilityState === 'visible') {
        acquireKeepAwake();
    }
});

export async function requestWakeLock() {
    keepAwake = true;
    await acquireKeepAwake();
}

export function releaseWakeLock() {
    keepAwake = false;
    disarmGesture();
    stopFallbackVideo();
    try {
        if (wakeLock) {
            wakeLock.release();
        }
    } catch (e) {
        console.warn('wake lock release failed', e);
    }
    wakeLock = null;
}

export function speak(text) {
    try {
        const synth = window.speechSynthesis;
        if (!synth || !text) return;
        synth.cancel();
        const utterance = new SpeechSynthesisUtterance(text);
        utterance.rate = 1;
        synth.speak(utterance);
    } catch (e) {
        console.warn('speak failed', e);
    }
}

export function cancelSpeech() {
    try {
        if (window.speechSynthesis) {
            window.speechSynthesis.cancel();
        }
    } catch (e) {
        console.warn('cancel speech failed', e);
    }
}

export function getScrollY() {
    return window.scrollY;
}

export function scrollToY(y) {
    window.scrollTo(0, y);
}

export function historyBack() {
    history.back();
}

export function scrollIntoView(elementId) {
    try {
        const el = document.getElementById(elementId);
        if (el) {
            el.scrollIntoView({ block: 'center' });
        }
    } catch (e) {
        console.warn('scrollIntoView failed', e);
    }
}

export function getBoundingRect(element) {
    if (!element) return null;
    const rect = element.getBoundingClientRect();
    return { left: rect.left, top: rect.top, width: rect.width, height: rect.height };
}

export function setPointerCapture(element, pointerId) {
    try {
        element?.setPointerCapture(pointerId);
    } catch (e) {
        console.warn('setPointerCapture failed', e);
    }
}

export function blockArrowScroll(element) {
    if (!element) return;
    const keys = ['ArrowUp', 'ArrowDown', 'ArrowLeft', 'ArrowRight', 'PageUp', 'PageDown', 'Home', 'End'];
    element.addEventListener('keydown', e => {
        if (keys.includes(e.key)) {
            e.preventDefault();
        }
    });
}

export function releasePointerCapture(element, pointerId) {
    try {
        if (element?.hasPointerCapture(pointerId)) {
            element.releasePointerCapture(pointerId);
        }
    } catch (e) {
        console.warn('releasePointerCapture failed', e);
    }
}

export function downloadJson(fileName, content) {
    const blob = new Blob([content], { type: 'application/json' });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = fileName;
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
    URL.revokeObjectURL(url);
}

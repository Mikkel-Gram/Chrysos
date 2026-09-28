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

let wakeLock = null;

export async function requestWakeLock() {
    try {
        if ('wakeLock' in navigator) {
            wakeLock = await navigator.wakeLock.request('screen');
        }
    } catch (e) {
        console.warn('wake lock failed', e);
    }
}

export function releaseWakeLock() {
    try {
        if (wakeLock) {
            wakeLock.release();
            wakeLock = null;
        }
    } catch (e) {
        console.warn('wake lock release failed', e);
    }
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

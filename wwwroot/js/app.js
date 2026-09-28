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

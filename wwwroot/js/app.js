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

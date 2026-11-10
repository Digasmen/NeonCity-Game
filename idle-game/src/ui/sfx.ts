// Efeitos sonoros sintetizados com WebAudio: sem ficheiros, só osciladores curtos.

const MUTE_KEY = 'neonstack.mute';
let ctx: AudioContext | null = null;
let muted = false;
try {
  muted = localStorage.getItem(MUTE_KEY) === '1';
} catch {
  /* sem armazenamento */
}

// O browser só deixa tocar som depois de um gesto do jogador.
addEventListener(
  'pointerdown',
  () => {
    try {
      ctx ??= new AudioContext();
      if (ctx.state === 'suspended') void ctx.resume();
    } catch {
      /* sem áudio */
    }
  },
  { passive: true },
);

function tone(freq: number, dur: number, type: OscillatorType = 'square', vol = 0.05, slideTo = freq, delay = 0): void {
  if (muted || !ctx || ctx.state !== 'running') return;
  const t0 = ctx.currentTime + delay;
  const osc = ctx.createOscillator();
  const gain = ctx.createGain();
  osc.type = type;
  osc.frequency.setValueAtTime(freq, t0);
  osc.frequency.exponentialRampToValueAtTime(Math.max(30, slideTo), t0 + dur);
  gain.gain.setValueAtTime(vol, t0);
  gain.gain.exponentialRampToValueAtTime(0.0001, t0 + dur);
  osc.connect(gain).connect(ctx.destination);
  osc.start(t0);
  osc.stop(t0 + dur + 0.02);
}

export const sfx = {
  tap: () => tone(520, 0.05, 'square', 0.03),
  buy: () => tone(440, 0.09, 'triangle', 0.07, 880),
  hire: () => [392, 523, 659].forEach((f, i) => tone(f, 0.12, 'triangle', 0.07, f, i * 0.08)),
  milestone: () => [523, 659, 784, 1047].forEach((f, i) => tone(f, 0.16, 'square', 0.05, f, i * 0.07)),
  build: () => {
    tone(110, 0.3, 'sawtooth', 0.06, 440);
    tone(660, 0.2, 'triangle', 0.06, 990, 0.25);
  },
  overclock: () => tone(180, 0.35, 'sawtooth', 0.05, 1400),
  toggle: () => tone(300, 0.08, 'square', 0.04, 200),
  coin: () => tone(988, 0.08, 'triangle', 0.06, 1319),
  alarm: () => [0, 0.3, 0.6].forEach((d) => tone(880, 0.25, 'sawtooth', 0.06, 440, d)),
  zap: () => tone(1400, 0.1, 'square', 0.05, 200),
  win: () => [659, 784, 1047, 1319].forEach((f, i) => tone(f, 0.18, 'triangle', 0.07, f, i * 0.09)),
  fail: () => tone(300, 0.5, 'sawtooth', 0.07, 60),
  node: (i: number) => tone(330 + i * 40, 0.12, 'sine', 0.08),
};

export const isMuted = () => muted;
export function setMuted(m: boolean): void {
  muted = m;
  try {
    localStorage.setItem(MUTE_KEY, m ? '1' : '0');
  } catch {
    /* sem armazenamento */
  }
}

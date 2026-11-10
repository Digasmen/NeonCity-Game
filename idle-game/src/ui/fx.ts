import { h } from './dom';

/** Onda de toque a partir do ponto onde o jogador carregou. */
export function ripple(el: HTMLElement, ev: MouseEvent): void {
  const r = el.getBoundingClientRect();
  const dot = h('span', 'ripple');
  // Toques de teclado não trazem coordenadas: usa o centro.
  const x = ev.clientX ? ev.clientX - r.left : r.width / 2;
  const y = ev.clientY ? ev.clientY - r.top : r.height / 2;
  dot.style.left = `${x}px`;
  dot.style.top = `${y}px`;
  el.append(dot);
  setTimeout(() => dot.remove(), 520);
}

/** Texto que sobe e desaparece (ganhos, bónus). */
export function floatText(parent: HTMLElement, text: string, cls = ''): void {
  const f = h('span', `float ${cls}`, text);
  parent.append(f);
  setTimeout(() => f.remove(), cls.includes('mega') ? 1500 : 900);
}

/** Reinicia uma animação CSS de curta duração aplicando a classe por instantes. */
export function pulse(el: Element, cls: string, ms = 500): void {
  el.classList.remove(cls);
  void (el as HTMLElement).offsetWidth;
  el.classList.add(cls);
  setTimeout(() => el.classList.remove(cls), ms);
}

/** Faíscas a saltar de um ponto (venda, drone abatido). */
export function sparks(parent: HTMLElement, n = 6): void {
  for (let i = 0; i < n; i++) {
    const p = h('i', 'spark');
    const a = (Math.PI * 2 * i) / n + Math.random();
    const d = 18 + Math.random() * 22;
    p.style.setProperty('--dx', `${Math.cos(a) * d}px`);
    p.style.setProperty('--dy', `${Math.sin(a) * d}px`);
    parent.append(p);
    setTimeout(() => p.remove(), 600);
  }
}

/**
 * Fundo animado da torre: silhueta da cidade com janelas a piscar e chuva néon.
 * As cores são fixas porque o jogo tem um único visual nocturno.
 */
export function startBackdrop(canvas: HTMLCanvasElement): void {
  const g = canvas.getContext('2d');
  if (!g) return;
  const still = matchMedia('(prefers-reduced-motion: reduce)').matches;
  let w = 0;
  let hgt = 0;
  interface Tower { x: number; w: number; h: number; hue: number; lit: boolean[] }
  let towers: Tower[] = [];
  const rain = Array.from({ length: 70 }, () => ({ x: Math.random(), y: Math.random(), v: 0.5 + Math.random() * 0.7, l: 8 + Math.random() * 14 }));

  function resize() {
    const dpr = Math.min(2, devicePixelRatio || 1);
    w = canvas.clientWidth;
    hgt = canvas.clientHeight;
    canvas.width = Math.max(1, w * dpr);
    canvas.height = Math.max(1, hgt * dpr);
    g!.setTransform(dpr, 0, 0, dpr, 0, 0);
    towers = [];
    for (let x = -10; x < w; ) {
      const tw = 26 + Math.random() * 44;
      const cols = Math.floor((tw - 8) / 7);
      const th = hgt * (0.25 + Math.random() * 0.6);
      const rows = Math.floor(th / 9);
      towers.push({ x, w: tw, h: th, hue: [322, 190, 265, 40][Math.floor(Math.random() * 4)], lit: Array.from({ length: cols * rows }, () => Math.random() < 0.28) });
      x += tw + 2 + Math.random() * 8;
    }
  }

  let last = performance.now();
  function draw(now: number) {
    const dt = Math.min(0.05, (now - last) / 1000);
    last = now;
    const sky = g!.createLinearGradient(0, 0, 0, hgt);
    sky.addColorStop(0, '#0b0716');
    sky.addColorStop(0.7, '#1a0c33');
    sky.addColorStop(1, '#3a1048');
    g!.fillStyle = sky;
    g!.fillRect(0, 0, w, hgt);

    for (const t of towers) {
      g!.fillStyle = '#1d1342';
      g!.fillRect(t.x, hgt - t.h, t.w, t.h);
      const cols = Math.floor((t.w - 8) / 7);
      if (cols < 1) continue;
      if (!still && Math.random() < 0.02) {
        const k = Math.floor(Math.random() * t.lit.length);
        t.lit[k] = !t.lit[k];
      }
      g!.fillStyle = `hsla(${t.hue}, 95%, 68%, 0.8)`;
      for (let k = 0; k < t.lit.length; k++) {
        if (t.lit[k]) g!.fillRect(t.x + 5 + (k % cols) * 7, hgt - t.h + 6 + Math.floor(k / cols) * 9, 3, 4);
      }
    }

    g!.strokeStyle = 'rgba(120, 220, 255, 0.28)';
    g!.lineWidth = 1;
    g!.beginPath();
    for (const r of rain) {
      if (!still) {
        r.y += r.v * dt;
        if (r.y > 1.05) {
          r.y = -0.05;
          r.x = Math.random();
        }
      }
      const x = r.x * w;
      const y = r.y * hgt;
      g!.moveTo(x, y);
      g!.lineTo(x - 2, y + r.l);
    }
    g!.stroke();
    if (!still) requestAnimationFrame(draw);
  }

  new ResizeObserver(() => {
    resize();
    if (still) draw(performance.now());
  }).observe(canvas);
  resize();
  requestAnimationFrame(draw);
}

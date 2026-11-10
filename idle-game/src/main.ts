import './style.css';
import { fmt, fmtTime } from './game/format';
import { load, save, wipe } from './game/save';
import * as S from './game/sim';
import { h, openModal, toast } from './ui/dom';
import { createUI, Game } from './ui/ui';

const game: Game = { state: load() ?? S.newState(), buyMode: 1 };

// Ganhos enquanto o jogo esteve fechado.
const away = (Date.now() - game.state.savedAt) / 1000;
if (away > 5) {
  const gain = S.simulateOffline(game.state, away);
  if (away > 60 && gain > 0) {
    const { body, close } = openModal('Enquanto estiveste fora');
    body.append(
      h('p', '', `A torre trabalhou ${fmtTime(Math.min(away, S.OFFLINE_CAP))} sem ti.`),
      h('p', 'big', `+¤${fmt(gain)}`),
      h('p', 'fine', `Os ganhos offline contam no máximo ${fmtTime(S.OFFLINE_CAP)}.`),
    );
    const ok = h('button', 'btn primary wide', 'Recolher');
    ok.onclick = close;
    body.append(ok);
  }
}

const ui = createUI(document.getElementById('app')!, game, {
  onReboot() {
    game.state = S.reboot(game.state);
    save(game.state);
  },
  onWipe() {
    wipe();
    game.state = S.newState();
  },
});

let last = performance.now();
function frame(now: number) {
  let dt = (now - last) / 1000;
  last = now;
  if (dt > 3) {
    // Separador esteve escondido: salta o tempo de uma vez.
    const gain = S.simulateOffline(game.state, dt);
    if (gain > 0) toast(`De volta: +¤${fmt(gain)} enquanto estiveste noutro separador.`);
  } else {
    while (dt > 0) {
      const step = Math.min(dt, 0.1);
      S.tick(game.state, step);
      dt -= step;
    }
  }
  ui.render();
  requestAnimationFrame(frame);
}
requestAnimationFrame(frame);

setInterval(() => save(game.state), 10_000);
addEventListener('pagehide', () => save(game.state));
document.addEventListener('visibilitychange', () => {
  if (document.hidden) save(game.state);
});

// Acesso pela consola, útil para testar e afinar o equilíbrio.
(window as unknown as { neon: Game }).neon = game;

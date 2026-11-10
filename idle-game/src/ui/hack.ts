import { h, openModal } from './dom';
import { sfx } from './sfx';

const ROUNDS = [4, 5, 6]; // nós a memorizar em cada ronda
const CELLS = 16;

const wait = (ms: number) => new Promise((r) => setTimeout(r, ms));

/**
 * Mini-jogo "Quebrar o ICE": memoriza a sequência de nós e repete-a antes de o tempo acabar.
 * Chama `onDone` com o número de rondas ultrapassadas (0 a 3).
 */
export function openHack(onDone: (cleared: number) => void): void {
  let cleared = 0;
  let finished = false;
  let alive = true;

  const finish = () => {
    if (finished) return;
    finished = true;
    alive = false;
    onDone(cleared);
  };
  const { body, close } = openModal('Quebrar o ICE', { onClose: finish });

  const status = h('p', 'hack-status', 'Memoriza a sequência de nós e repete-a pela mesma ordem.');
  const pips = h('div', 'hack-pips');
  ROUNDS.forEach(() => pips.append(h('i')));
  const timer = h('div', 'bar hack-timer');
  const timerFill = h('i');
  timer.append(timerFill);
  const grid = h('div', 'hack-grid');
  const cells: HTMLButtonElement[] = [];
  for (let i = 0; i < CELLS; i++) {
    const c = h('button', 'hack-cell');
    c.setAttribute('aria-label', `Nó ${i + 1}`);
    c.disabled = true;
    cells.push(c);
    grid.append(c);
  }
  const start = h('button', 'btn primary wide', 'Iniciar intrusão');
  body.append(status, pips, grid, timer, start);

  const end = (msg: string) => {
    status.textContent = msg;
    cells.forEach((c) => (c.disabled = true));
    start.textContent = 'Recolher prémio';
    start.hidden = false;
    start.onclick = close;
  };

  async function playRound(round: number): Promise<void> {
    const len = ROUNDS[round];
    const order = [...Array(CELLS).keys()].sort(() => Math.random() - 0.5).slice(0, len);
    cells.forEach((c) => {
      c.className = 'hack-cell';
      c.disabled = true;
    });
    timerFill.style.transition = 'none';
    timerFill.style.width = '100%';
    status.textContent = `Ronda ${round + 1} de ${ROUNDS.length}: memoriza ${len} nós.`;
    await wait(600);
    for (const idx of order) {
      if (!alive) return;
      cells[idx].classList.add('lit');
      sfx.node(idx % 8);
      await wait(420);
      cells[idx].classList.remove('lit');
      await wait(110);
    }
    if (!alive) return;

    status.textContent = 'Repete a sequência.';
    const limit = len * 1500;
    void timerFill.offsetWidth;
    timerFill.style.transition = `width ${limit}ms linear`;
    timerFill.style.width = '0%';
    let step = 0;
    let over = false;
    const timeout = setTimeout(() => {
      if (over || !alive) return;
      over = true;
      end(`Tempo esgotado. Rondas ultrapassadas: ${cleared}.`);
    }, limit);

    cells.forEach((c, idx) => {
      c.disabled = false;
      c.onclick = () => {
        if (over || !alive) return;
        if (idx === order[step]) {
          c.classList.add('ok');
          sfx.node(idx % 8);
          c.disabled = true;
          step++;
          if (step === len) {
            over = true;
            clearTimeout(timeout);
            cleared = round + 1;
            pips.children[round].classList.add('on');
            if (cleared === ROUNDS.length) end('ICE quebrado. Acesso total à rede da megacorp.');
            else
              void wait(500).then(() => {
                if (alive) void playRound(round + 1);
              });
          }
        } else {
          over = true;
          clearTimeout(timeout);
          c.classList.add('bad');
          cells[order[step]].classList.add('lit');
          end(`Nó errado, a ligação caiu. Rondas ultrapassadas: ${cleared}.`);
        }
      };
    });
  }

  start.onclick = () => {
    start.hidden = true;
    void playRound(0);
  };
}

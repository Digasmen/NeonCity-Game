import { ELEV_MANAGER, FLOORS, TERM_MANAGER } from '../game/data';
import { fmt, fmtTime } from '../game/format';
import * as S from '../game/sim';
import { h, openModal, setText, toast, toggle } from './dom';
import { openHack } from './hack';

export type BuyMode = 1 | 10 | 'max';
export interface Game {
  state: S.GameState;
  buyMode: BuyMode;
}
export interface Hooks {
  onReboot(): void;
  onWipe(): void;
}

/** Altura de cada piso em px; tem de coincidir com --floor-h no CSS. */
const FLOOR_H = 112;

interface Controls {
  up: HTMLButtonElement;
  mgr: HTMLButtonElement;
  update(): void;
}

export function createUI(root: HTMLElement, game: Game, hooks: Hooks) {
  const s = () => game.state;

  // ---------- Botões de nível e gestor, comuns a pisos, elevador e terminal ----------
  function controls(key: string, managerName: string): Controls {
    const up = h('button', 'btn up');
    const upTop = h('span', 'b-top');
    const upCost = h('span', 'b-sub');
    up.append(upTop, upCost);
    up.onclick = (ev) => {
      ev.stopPropagation();
      S.buyLevels(s(), key, game.buyMode);
    };

    const mgr = h('button', 'btn mgr');
    const mgrTop = h('span', 'b-top');
    const mgrSub = h('span', 'b-sub');
    mgr.append(mgrTop, mgrSub);
    mgr.onclick = (ev) => {
      ev.stopPropagation();
      if (!S.hasManager(s(), key)) {
        if (S.hireManager(s(), key)) toast(`${managerName} contratado. Esta estação passa a trabalhar sozinha.`);
      } else S.overclock(s(), key);
    };

    return {
      up,
      mgr,
      update() {
        const st = s();
        const q = S.quote(st, key, game.buyMode);
        setText(upTop, `Nível +${q.n}`);
        setText(upCost, `¤${fmt(q.cost)}`);
        up.disabled = st.credits < q.cost;

        if (!S.hasManager(st, key)) {
          const cost = S.managerCost(key);
          setText(mgrTop, 'Gestor');
          setText(mgrSub, `¤${fmt(cost)}`);
          mgr.disabled = st.credits < cost;
          toggle(mgr, 'hired', false);
          toggle(mgr, 'hot', false);
          return;
        }
        toggle(mgr, 'hired', true);
        const o = st.oc[key];
        const active = !!o && st.clock < o.until;
        const cooling = !!o && !active && st.clock < o.ready;
        setText(mgrTop, managerName);
        setText(mgrSub, active ? `x2 · ${fmtTime(o.until - st.clock)}` : cooling ? `pronto em ${fmtTime(o.ready - st.clock)}` : 'Overclock x2');
        mgr.disabled = active || cooling;
        toggle(mgr, 'hot', active);
      },
    };
  }

  // ---------- HUD ----------
  const hud = h('header', 'hud');
  const money = h('div', 'money');
  const brand = h('div', 'brand', 'NEON STACK');
  const credits = h('div', 'credits');
  const sub = h('div', 'money-sub');
  const rate = h('span', 'rate');
  const chips = h('span', 'chip-badge');
  const boost = h('span', 'boost-badge');
  sub.append(rate, chips, boost);
  money.append(brand, credits, sub);

  const hudBtns = h('div', 'hud-btns');
  const hackBtn = h('button', 'btn hack');
  const hackTop = h('span', 'b-top', 'Hack');
  const hackSub = h('span', 'b-sub');
  hackBtn.append(hackTop, hackSub);
  hackBtn.onclick = () => {
    if (s().clock < s().hackReady) return;
    openHack((cleared) => {
      const r = S.applyHack(s(), cleared);
      if (r.duration > 0) toast(`Lucro x${r.mult} durante ${r.duration}s${r.lump > 0 ? ` e ¤${fmt(r.lump)} desviados` : ''}.`);
      else toast('Intrusão falhada. Tenta de novo daqui a 45s.');
    });
  };
  const rebootBtn = h('button', 'btn', 'Reboot');
  rebootBtn.onclick = () => openReboot();
  const menuBtn = h('button', 'btn', 'Menu');
  menuBtn.onclick = () => openMenu();
  hudBtns.append(hackBtn, rebootBtn, menuBtn);
  hud.append(money, hudBtns);

  // ---------- Fluxo e modo de compra ----------
  const flowBar = h('div', 'flow');
  const pills = {
    floors: h('span', 'pill'),
    elev: h('span', 'pill'),
    term: h('span', 'pill'),
  };
  const flowChain = h('div', 'flow-chain');
  flowChain.append(pills.floors, h('span', 'arrow', '›'), pills.elev, h('span', 'arrow', '›'), pills.term);
  const modes = h('div', 'modes');
  modes.setAttribute('role', 'group');
  modes.setAttribute('aria-label', 'Quantidade por compra');
  const modeBtns: [BuyMode, HTMLButtonElement][] = ([1, 10, 'max'] as BuyMode[]).map((m) => {
    const b = h('button', 'mode', m === 'max' ? 'MAX' : `x${m}`);
    b.onclick = () => (game.buyMode = m);
    modes.append(b);
    return [m, b];
  });
  flowBar.append(flowChain, modes);
  const hint = h('div', 'hint');

  // ---------- Torre ----------
  const stage = h('div', 'stage');
  const scroll = h('main', 'tower-scroll');
  const tower = h('div', 'tower');
  const build = h('div', 'build');
  const buildInfo = h('div', 'build-info');
  const buildName = h('b');
  const buildSub = h('small');
  buildInfo.append(buildName, buildSub);
  const buildBtn = h('button', 'btn primary');
  const buildTop = h('span', 'b-top', 'Construir piso');
  const buildCost = h('span', 'b-sub');
  buildBtn.append(buildTop, buildCost);
  buildBtn.onclick = () => {
    if (S.unlockFloor(s())) toast(`Novo piso: ${FLOORS[s().floors.length - 1].name}.`);
  };
  build.append(buildInfo, buildBtn);

  const floorsEl = h('div', 'floors');
  const lobby = h('div', 'lobby');
  const depot = h('div', 'depot');
  const depotVal = h('b');
  depot.append(h('small', '', 'Cais'), depotVal);
  const track = h('div', 'track');
  const courier = h('i', 'courier');
  track.append(courier);
  const market = h('div', 'market', 'Mercado Negro');
  lobby.append(h('div'), depot, track, market);

  const shaft = h('div', 'shaft');
  const cabin = h('div', 'cabin');
  const cabinLoad = h('span');
  cabin.append(cabinLoad);
  shaft.append(cabin);
  tower.append(build, floorsEl, lobby, shaft);
  scroll.append(tower);
  stage.append(scroll);

  interface FloorView {
    room: HTMLElement;
    level: HTMLElement;
    fill: HTMLElement;
    rate: HTMLElement;
    stock: HTMLElement;
    mile: HTMLElement;
    ctl: Controls;
  }
  let floorViews: FloorView[] = [];
  let builtFor: S.GameState | null = null;

  function buildFloors() {
    const st = s();
    floorsEl.textContent = '';
    floorViews = st.floors.map((_, i) => {
      const def = FLOORS[i];
      const row = h('div', 'floor');
      row.style.setProperty('--h', String(def.hue));
      const room = h('div', 'room');
      room.setAttribute('role', 'button');
      room.tabIndex = 0;
      const head = h('div', 'room-head');
      const name = h('span', 'fname', def.name);
      const level = h('span', 'flevel');
      head.append(name, level);
      const bar = h('div', 'bar');
      const fill = h('i');
      bar.append(fill);
      const foot = h('div', 'room-foot');
      const rateEl = h('span', 'frate');
      const stock = h('span', 'fstock');
      foot.append(rateEl, stock);
      const mile = h('div', 'fmile');
      room.append(head, bar, foot, mile);
      const act = () => S.tap(s(), 'f' + i);
      room.onclick = act;
      room.onkeydown = (ev) => {
        if (ev.key === 'Enter' || ev.key === ' ') act();
      };
      const ctl = controls('f' + i, def.manager);
      const actions = h('div', 'actions');
      actions.append(ctl.up, ctl.mgr);
      row.append(h('div'), room, actions);
      floorsEl.append(row);
      return { room, level, fill, rate: rateEl, stock, mile, ctl };
    });
    shaft.style.height = `${(st.floors.length + 1) * FLOOR_H}px`;
    builtFor = st;
  }

  // ---------- Painel inferior: elevador e terminal ----------
  const dock = h('footer', 'dock');
  function card(key: 'elev' | 'term', title: string, managerName: string) {
    const el = h('div', 'card');
    el.setAttribute('role', 'button');
    el.tabIndex = 0;
    const head = h('div', 'card-head');
    const level = h('span', 'flevel');
    head.append(h('b', '', title), level);
    const stat = h('div', 'card-stat');
    const bar = h('div', 'bar');
    const fill = h('i');
    bar.append(fill);
    const ctl = controls(key, managerName);
    const actions = h('div', 'card-actions');
    actions.append(ctl.up, ctl.mgr);
    el.append(head, stat, bar, actions);
    const act = () => S.tap(s(), key);
    el.onclick = act;
    el.onkeydown = (ev) => {
      if (ev.target === el && (ev.key === 'Enter' || ev.key === ' ')) act();
    };
    dock.append(el);
    return { el, level, stat, fill, ctl };
  }
  const elevCard = card('elev', 'Elevador', ELEV_MANAGER);
  const termCard = card('term', 'Terminal', TERM_MANAGER);

  root.append(hud, flowBar, hint, stage, dock);

  // ---------- Modais ----------
  function openReboot() {
    const st = s();
    const gain = S.chipsOnReboot(st);
    const { body, close } = openModal('Reboot');
    body.append(
      h('p', '', 'Vende a torre a uma megacorp e recomeça do zero. Em troca recebes Chips de Memória, que aumentam o lucro para sempre.'),
    );
    const stats = h('dl', 'stats');
    const row = (k: string, v: string) => stats.append(h('dt', '', k), h('dd', '', v));
    row('Chips actuais', `${fmt(st.chips)} (+${fmt(st.chips * S.CHIP_BONUS * 100)}% de lucro)`);
    row('Ganhos nesta torre', `¤${fmt(st.runEarned)}`);
    row('Chips a receber', fmt(gain));
    row('Lucro depois do Reboot', `+${fmt((st.chips + gain) * S.CHIP_BONUS * 100)}%`);
    body.append(stats);
    const go = h('button', 'btn primary wide', gain > 0 ? 'Vender a torre' : 'Ainda não rende nenhum chip');
    go.disabled = gain < 1;
    let armed = false;
    go.onclick = () => {
      if (!armed) {
        armed = true;
        go.textContent = 'Toca outra vez para confirmar';
        return;
      }
      close();
      hooks.onReboot();
      toast(`Reboot concluído. +${fmt(gain)} Chips de Memória.`);
    };
    body.append(go);
    if (gain < 1) body.append(h('p', 'fine', 'O primeiro chip chega aos ¤500K ganhos nesta torre.'));
  }

  function openMenu() {
    const st = s();
    const { body, close } = openModal('Menu');
    const how = h('ol', 'how');
    [
      'Os pisos produzem carga. Toca num piso sem gestor para arrancar um ciclo.',
      'O elevador leva a carga dos pisos para o cais.',
      'O terminal vende a carga do cais no mercado negro.',
      'O mais lento dos três limita o lucro. A barra de fluxo no topo mostra qual é, a vermelho.',
      'Gestores automatizam cada estação e têm um Overclock que duplica a velocidade por 30s.',
      'Os níveis 10, 25, 50, 100… de cada piso duplicam a produção.',
    ].forEach((t) => how.append(h('li', '', t)));
    body.append(how);
    const stats = h('dl', 'stats');
    const row = (k: string, v: string) => stats.append(h('dt', '', k), h('dd', '', v));
    row('Total ganho', `¤${fmt(st.totalEarned)}`);
    row('Reboots', String(st.reboots));
    row('Intrusões com sucesso', String(st.hacks));
    row('Tempo de jogo', fmtTime(st.clock));
    body.append(stats);
    const wipe = h('button', 'btn danger wide', 'Apagar todo o progresso');
    let armed = false;
    wipe.onclick = () => {
      if (!armed) {
        armed = true;
        wipe.textContent = 'Toca outra vez para apagar tudo';
        return;
      }
      close();
      hooks.onWipe();
      toast('Progresso apagado.');
    };
    body.append(wipe);
  }

  // ---------- Efeitos ----------
  let lastSold = s().sold;
  let lastFx = 0;
  function saleFx(amount: number) {
    const now = performance.now();
    if (now - lastFx < 450) return;
    lastFx = now;
    const f = h('span', 'float', `+¤${fmt(amount)}`);
    market.append(f);
    setTimeout(() => f.remove(), 900);
  }

  function spawnStray() {
    const d = h('button', 'stray');
    d.setAttribute('aria-label', 'Drone de carga perdido: toca para ficar com a carga');
    d.style.top = `${12 + Math.random() * 40}%`;
    d.append(h('i'));
    d.onclick = () => {
      const gain = S.claimDrone(s());
      toast(`Drone perdido interceptado: +¤${fmt(gain)}.`);
      d.remove();
    };
    d.addEventListener('animationend', () => d.remove());
    stage.append(d);
  }

  const samples: { t: number; sold: number }[] = [];
  function incomeRate(st: S.GameState): number {
    const last = samples[samples.length - 1];
    if (last && (st.clock - last.t > 5 || st.sold < last.sold)) samples.length = 0;
    if (!last || st.clock - last.t >= 0.5 || samples.length === 0) samples.push({ t: st.clock, sold: st.sold });
    while (samples.length > 2 && st.clock - samples[0].t > 10) samples.shift();
    const first = samples[0];
    const dt = st.clock - first.t;
    return dt > 1 ? (st.sold - first.sold) / dt : 0;
  }

  function hintText(st: S.GameState, fl: ReturnType<typeof S.flow>): string {
    if (!st.elev.manager && st.credits < S.ELEV_MANAGER_COST && st.runEarned < 200) {
      if (st.depot > 0 && st.term.phase === 'idle') return 'Toca em TERMINAL para vender a carga que está no cais.';
      return 'Toca em ELEVADOR para ir buscar a carga ao piso.';
    }
    if (!st.elev.manager) return 'Contrata o gestor do elevador para ele trabalhar sozinho.';
    if (!st.term.manager) return 'Contrata o gestor do terminal para vender sem tocares.';
    if (st.floors.some((f) => !f.manager)) return 'Pisos sem gestor só produzem quando lhes tocas.';
    if (fl.bottleneck === 'elev') return 'Gargalo: o elevador não dá vazão. Sobe-lhe o nível.';
    if (fl.bottleneck === 'term') return 'Gargalo: o terminal vende devagar. Sobe-lhe o nível.';
    return 'Os pisos são o limite. Sobe níveis ou constrói o próximo piso.';
  }

  // ---------- Render (a cada frame) ----------
  let scrolled = false;
  function render() {
    const st = s();
    if (builtFor !== st || floorViews.length !== st.floors.length) {
      const grew = builtFor === st;
      buildFloors();
      lastSold = st.sold;
      if (!grew) scrolled = false;
    }
    if (!scrolled) {
      scroll.scrollTop = scroll.scrollHeight;
      scrolled = true;
    }
    const fl = S.flow(st);
    const mult = S.globalMult(st);

    setText(credits, `¤${fmt(st.credits)}`);
    setText(rate, `+¤${fmt(incomeRate(st))}/s`);
    setText(chips, st.chips > 0 ? `${fmt(st.chips)} chips · +${fmt(st.chips * S.CHIP_BONUS * 100)}%` : '');
    chips.hidden = st.chips === 0;
    const hacking = S.hackActive(st);
    boost.hidden = !hacking;
    if (hacking) setText(boost, `Hack x${st.hackMult} · ${fmtTime(st.hackUntil - st.clock)}`);

    const hackWait = st.hackReady - st.clock;
    hackBtn.disabled = hackWait > 0;
    toggle(hackBtn, 'ready', hackWait <= 0);
    setText(hackSub, hackWait > 0 ? fmtTime(hackWait) : 'pronto');
    toggle(rebootBtn, 'ready', S.chipsOnReboot(st) >= Math.max(5, st.chips));

    setText(pills.floors, `Pisos ${fmt(fl.floors)}/s`);
    setText(pills.elev, `Elevador ${fmt(fl.elev)}/s`);
    setText(pills.term, `Terminal ${fmt(fl.term)}/s`);
    (['floors', 'elev', 'term'] as const).forEach((k) => toggle(pills[k], 'slow', fl.bottleneck === k));
    for (const [m, b] of modeBtns) {
      toggle(b, 'on', game.buyMode === m);
      b.setAttribute('aria-pressed', String(game.buyMode === m));
    }
    setText(hint, hintText(st, fl));

    st.floors.forEach((f, i) => {
      const v = floorViews[i];
      setText(v.level, `Nv ${f.level}`);
      v.fill.style.transform = `scaleX(${f.working ? Math.min(1, f.progress) : 0})`;
      setText(v.rate, `${fmt(S.floorRate(i, f.level))}/s`);
      setText(v.stock, `carga ${fmt(f.stock)}`);
      const nm = S.nextMilestone(f.level);
      setText(v.mile, f.manager ? (nm ? `x2 ao nível ${nm}` : 'nível máximo de bónus') : 'Toca para produzir');
      toggle(v.room, 'manual', !f.manager);
      toggle(v.room, 'busy', f.working);
      v.ctl.update();
    });

    const next = st.floors.length;
    build.hidden = next >= FLOORS.length;
    if (next < FLOORS.length) {
      const cost = S.floorUnlockCost(next);
      setText(buildName, `Piso ${next + 1}: ${FLOORS[next].name}`);
      setText(buildSub, `Produz ${fmt(S.floorRate(next, 1))}/s no nível 1`);
      setText(buildCost, `¤${fmt(cost)}`);
      buildBtn.disabled = st.credits < cost;
    }

    // Elevador
    const e = st.elev;
    const cap = S.elevCapacity(e.level, st.floors.length);
    cabin.style.transform = `translateY(${-e.pos * FLOOR_H}px)`;
    setText(cabinLoad, e.load > 0 ? fmt(e.load) : '');
    toggle(cabin, 'full', e.load > 0);
    setText(elevCard.level, `Nv ${e.level}`);
    setText(elevCard.stat, e.manager || e.phase !== 'idle' ? `${fmt(S.elevRate(e.level))}/s · leva ${fmt(cap)}` : 'Toca para recolher');
    elevCard.fill.style.transform = `scaleX(${Math.min(1, e.load / cap)})`;
    toggle(elevCard.el, 'manual', !e.manager);
    toggle(elevCard.el, 'busy', e.phase !== 'idle');
    elevCard.ctl.update();

    // Terminal
    const t = st.term;
    setText(depotVal, fmt(st.depot));
    const x = t.phase === 'out' ? t.t : t.phase === 'back' ? 1 - t.t : 0;
    courier.style.left = `${x * 100}%`;
    toggle(courier, 'loaded', t.phase === 'out');
    setText(termCard.level, `Nv ${t.level}`);
    setText(termCard.stat, t.manager || t.phase !== 'idle' ? `${fmt(S.termRate(t.level))}/s · x${fmt(mult * 100) === '100' ? '1' : mult.toFixed(2)} lucro` : st.depot > 0 ? 'Toca para vender' : 'À espera de carga');
    termCard.fill.style.transform = `scaleX(${t.phase === 'out' ? t.t : t.phase === 'back' ? 1 : 0})`;
    toggle(termCard.el, 'manual', !t.manager);
    toggle(termCard.el, 'busy', t.phase !== 'idle');
    termCard.ctl.update();

    if (st.sold > lastSold) saleFx(st.sold - lastSold);
    lastSold = st.sold;

    if (st.clock >= st.nextDrone) {
      st.nextDrone = st.clock + 50 + Math.random() * 50;
      if (!document.querySelector('.stray') && !document.querySelector('.overlay')) spawnStray();
    }
  }

  return { render };
}

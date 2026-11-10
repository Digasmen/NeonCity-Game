import { ELEV_MANAGER, FLOORS, TERM_MANAGER } from '../game/data';
import { fmt, fmtTime } from '../game/format';
import * as S from '../game/sim';
import { h, openModal, setText, toast, toggle } from './dom';
import { floatText, pulse, ripple, sparks, startBackdrop } from './fx';
import { openHack } from './hack';
import { isMuted, setMuted, sfx } from './sfx';

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
const FLOOR_H = 128;

interface Controls {
  up: HTMLButtonElement;
  mgr: HTMLButtonElement;
  update(): void;
}

/** Preenche o fundo do botão conforme a fracção do custo que o jogador já tem. */
function setAfford(btn: HTMLElement, credits: number, cost: number): void {
  const p = String(Math.round(Math.min(1, credits / cost) * 100));
  if (btn.dataset.p !== p) {
    btn.dataset.p = p;
    btn.style.setProperty('--p', `${p}%`);
  }
}

export function createUI(root: HTMLElement, game: Game, hooks: Hooks) {
  const s = () => game.state;

  // ---------- Botões de nível e gestor, comuns a pisos, elevador e terminal ----------
  function controls(key: string, managerName: string, host: () => HTMLElement): Controls {
    const up = h('button', 'btn up');
    const upTop = h('span', 'b-top');
    const upCost = h('span', 'b-sub');
    up.append(upTop, upCost);
    up.onclick = (ev) => {
      ev.stopPropagation();
      const st = s();
      const isFloor = key[0] === 'f';
      const before = isFloor ? S.milestoneMult(st.floors[Number(key.slice(1))].level) : 0;
      if (!S.buyLevels(st, key, game.buyMode)) return;
      pulse(host(), 'leveled', 600);
      const after = isFloor ? S.milestoneMult(st.floors[Number(key.slice(1))].level) : 0;
      if (after > before) {
        floatText(host(), `PRODUÇÃO x${after / before}`, 'mega');
        pulse(host(), 'milestone', 1200);
        sfx.milestone();
      } else sfx.buy();
    };

    const mgr = h('button', 'btn mgr');
    const mgrTop = h('span', 'b-top');
    const mgrSub = h('span', 'b-sub');
    mgr.append(mgrTop, mgrSub);
    mgr.onclick = (ev) => {
      ev.stopPropagation();
      if (!S.hasManager(s(), key)) {
        if (S.hireManager(s(), key)) {
          toast(`${managerName} contratado. Esta estação passa a trabalhar sozinha.`);
          pulse(host(), 'leveled', 600);
          sfx.hire();
        }
      } else if (S.overclock(s(), key)) sfx.overclock();
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
        setAfford(up, st.credits, q.cost);

        if (!S.hasManager(st, key)) {
          const cost = S.managerCost(key);
          setText(mgrTop, 'Gestor');
          setText(mgrSub, `¤${fmt(cost)}`);
          mgr.disabled = st.credits < cost;
          setAfford(mgr, st.credits, cost);
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
        toggle(host(), 'overclocked', active);
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

  const hudRight = h('div', 'hud-right');
  const hudBtns = h('div', 'hud-btns');
  const hackBtn = h('button', 'btn hack');
  const hackTop = h('span', 'b-top', 'Hack');
  const hackSub = h('span', 'b-sub');
  hackBtn.append(hackTop, hackSub);
  hackBtn.onclick = () => {
    if (s().clock < s().hackReady) return;
    sfx.tap();
    openHack((cleared) => {
      const r = S.applyHack(s(), cleared);
      if (r.duration > 0) {
        toast(`Lucro x${r.mult} durante ${r.duration}s${r.lump > 0 ? ` e ¤${fmt(r.lump)} desviados` : ''}.`);
        sfx.win();
      } else {
        toast('Intrusão falhada. Tenta de novo daqui a 45s.');
        sfx.fail();
      }
    });
  };
  const rebootBtn = h('button', 'btn', 'Reboot');
  rebootBtn.onclick = () => openReboot();
  const menuBtn = h('button', 'btn', 'Menu');
  menuBtn.onclick = () => openMenu();
  hudBtns.append(hackBtn, rebootBtn, menuBtn);

  const modes = h('div', 'modes');
  modes.setAttribute('role', 'group');
  modes.setAttribute('aria-label', 'Quantidade por compra');
  const modeBtns: [BuyMode, HTMLButtonElement][] = ([1, 10, 'max'] as BuyMode[]).map((m) => {
    const b = h('button', 'mode', m === 'max' ? 'MAX' : `x${m}`);
    b.onclick = () => {
      game.buyMode = m;
      sfx.tap();
    };
    modes.append(b);
    return [m, b];
  });
  hudRight.append(hudBtns, modes);
  hud.append(money, hudRight);

  // ---------- Fluxo ----------
  const flowBar = h('div', 'flow');
  const pills = { floors: h('span', 'pill'), elev: h('span', 'pill'), term: h('span', 'pill') };
  flowBar.append(pills.floors, h('span', 'arrow', '›'), pills.elev, h('span', 'arrow', '›'), pills.term);

  // ---------- Heat ----------
  const heatRow = h('div', 'heat');
  const heatMeter = h('div', 'heat-meter');
  const heatFill = h('i');
  heatMeter.append(heatFill);
  const heatInfo = h('div', 'heat-info');
  const heatLabel = h('b', '', 'HEAT');
  const heatText = h('span');
  heatInfo.append(heatLabel, heatText);
  const heatMain = h('div', 'heat-main');
  heatMain.append(heatInfo, heatMeter);
  const bribeBtn = h('button', 'btn bribe');
  const bribeTop = h('span', 'b-top', 'Subornar');
  const bribeSub = h('span', 'b-sub');
  bribeBtn.append(bribeTop, bribeSub);
  bribeBtn.onclick = () => {
    if (S.bribe(s())) {
      sfx.coin();
      pulse(heatMeter, 'cooled', 600);
      toast(`Suborno aceite. Heat -${S.BRIBE_DROP}.`);
    }
  };
  heatRow.append(heatMain, bribeBtn);
  const hint = h('div', 'hint');

  // ---------- Torre ----------
  const stage = h('div', 'stage');
  const backdrop = h('canvas', 'backdrop');
  backdrop.setAttribute('aria-hidden', 'true');
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
  let justBuilt = -1;
  buildBtn.onclick = () => {
    if (!S.unlockFloor(s())) return;
    justBuilt = s().floors.length - 1;
    toast(`Novo piso: ${FLOORS[justBuilt].name}.`);
    sfx.build();
    if (S.heatUnlocked(s()) && justBuilt + 1 === S.HEAT_UNLOCK_FLOORS)
      setTimeout(() => toast('Novo: modo ILEGAL. Um piso ilegal produz x3 mas sobe o Heat.'), 1800);
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
  stage.append(backdrop, scroll);

  interface FloorView {
    row: HTMLElement;
    room: HTMLElement;
    dockCell: HTMLElement;
    level: HTMLElement;
    fill: HTMLElement;
    bot: HTMLElement;
    pile: HTMLElement[];
    pileEl: HTMLElement;
    rate: HTMLElement;
    stock: HTMLElement;
    mile: HTMLElement;
    law: HTMLButtonElement;
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
      if (i === justBuilt) row.classList.add('drop-in');
      const dockCell = h('div', 'dock-cell');
      const room = h('div', 'room');
      room.setAttribute('role', 'button');
      room.tabIndex = 0;
      const head = h('div', 'room-head');
      const name = h('span', 'fname', def.name);
      const level = h('span', 'flevel');
      head.append(name, level);
      const scene = h('div', 'scene');
      const pileEl = h('div', 'pile');
      const pile = [0, 1, 2, 3, 4].map(() => h('i'));
      pileEl.append(...pile);
      const lane = h('div', 'lane');
      const bot = h('i', 'bot');
      lane.append(bot);
      scene.append(pileEl, lane, h('i', 'machine'));
      const bar = h('div', 'bar');
      const fill = h('i');
      bar.append(fill);
      const foot = h('div', 'room-foot');
      const rateEl = h('span', 'frate');
      const stock = h('span', 'fstock');
      foot.append(rateEl, stock);
      const tools = h('div', 'room-tools');
      const mile = h('span', 'fmile');
      const law = h('button', 'law');
      law.onclick = (ev) => {
        ev.stopPropagation();
        if (S.toggleIllegal(s(), i)) {
          sfx.toggle();
          pulse(room, 'leveled', 600);
        }
      };
      tools.append(mile, law);
      room.append(head, scene, bar, foot, tools);
      const act = (ev: MouseEvent) => {
        if (s().floors[i].manager) return;
        S.tap(s(), 'f' + i);
        ripple(room, ev);
        sfx.tap();
      };
      room.onclick = act;
      room.onkeydown = (ev) => {
        if (ev.target === room && (ev.key === 'Enter' || ev.key === ' ')) room.click();
      };
      const ctl = controls('f' + i, def.manager, () => room);
      const actions = h('div', 'actions');
      actions.append(ctl.up, ctl.mgr);
      row.append(dockCell, room, actions);
      floorsEl.append(row);
      return { row, room, dockCell, level, fill, bot, pile, pileEl, rate: rateEl, stock, mile, law, ctl };
    });
    shaft.style.height = `${(st.floors.length + 1) * FLOOR_H}px`;
    if (justBuilt >= 0) scroll.scrollTo({ top: 0, behavior: 'smooth' });
    justBuilt = -1;
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
    const ctl = controls(key, managerName, () => el);
    const actions = h('div', 'card-actions');
    actions.append(ctl.up, ctl.mgr);
    el.append(head, stat, bar, actions);
    el.onclick = (ev) => {
      if (S.hasManager(s(), key)) return;
      S.tap(s(), key);
      ripple(el, ev);
      sfx.tap();
    };
    el.onkeydown = (ev) => {
      if (ev.target === el && (ev.key === 'Enter' || ev.key === ' ')) el.click();
    };
    dock.append(el);
    return { el, level, stat, fill, ctl };
  }
  const elevCard = card('elev', 'Elevador', ELEV_MANAGER);
  const termCard = card('term', 'Terminal', TERM_MANAGER);

  root.append(hud, flowBar, heatRow, hint, stage, dock);
  startBackdrop(backdrop);

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
      pulse(root, 'rebooted', 1200);
      sfx.build();
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
      'O elevador leva a carga dos pisos para o cais e o terminal vende-a no mercado negro.',
      'O mais lento dos três limita o lucro. A barra de fluxo mostra qual é, a vermelho.',
      'Gestores automatizam cada estação e têm um Overclock que duplica a velocidade por 30s.',
      'Os níveis 10, 25, 50, 100… de cada piso duplicam a produção.',
      `Um piso em modo ILEGAL produz x${S.ILLEGAL_MULT} mas sobe o Heat. Suborna a polícia para o baixar.`,
      `Com o Heat a 100 há uma rusga: abate os drones em ${S.RAID_TIME}s ou perdes a carga do cais e os pisos ilegais ficam selados ${S.SEAL_TIME}s.`,
      'Com o jogo fechado, os pisos ilegais trabalham ao ritmo legal e o Heat desce.',
    ].forEach((t) => how.append(h('li', '', t)));
    body.append(how);
    const stats = h('dl', 'stats');
    const row = (k: string, v: string) => stats.append(h('dt', '', k), h('dd', '', v));
    row('Total ganho', `¤${fmt(st.totalEarned)}`);
    row('Reboots', String(st.reboots));
    row('Intrusões com sucesso', String(st.hacks));
    row('Rusgas repelidas', String(st.raidsWon));
    row('Tempo de jogo', fmtTime(st.clock));
    body.append(stats);
    const sound = h('button', 'btn wide', isMuted() ? 'Som: desligado' : 'Som: ligado');
    sound.onclick = () => {
      setMuted(!isMuted());
      sound.textContent = isMuted() ? 'Som: desligado' : 'Som: ligado';
      sfx.tap();
    };
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
    body.append(sound, wipe);
  }

  // ---------- Rusga: drones da polícia para abater ----------
  let raidEl: HTMLElement | null = null;
  let raidTimer: HTMLElement | null = null;
  function openRaid() {
    const st = s();
    if (!st.raid) return;
    raidEl = h('div', 'raid');
    const banner = h('div', 'raid-banner');
    raidTimer = h('b');
    banner.append(h('span', '', 'RUSGA! Abate os drones'), raidTimer);
    raidEl.append(banner);
    for (let k = 0; k < st.raid.left; k++) {
      const cop = h('button', 'cop');
      cop.setAttribute('aria-label', 'Drone da polícia');
      cop.style.left = `${6 + Math.random() * 74}%`;
      cop.style.top = `${16 + Math.random() * 64}%`;
      cop.style.animationDelay = `${-Math.random() * 2}s`;
      cop.style.animationDuration = `${1.6 + Math.random() * 1.4}s`;
      cop.append(h('i'));
      cop.onclick = () => {
        if (cop.classList.contains('down')) return;
        cop.classList.add('down');
        sparks(cop, 8);
        sfx.zap();
        S.raidHit(s());
        setTimeout(() => cop.remove(), 350);
      };
      raidEl.append(cop);
    }
    stage.append(raidEl);
  }
  function closeRaid() {
    const el = raidEl;
    raidEl = null;
    raidTimer = null;
    if (el) setTimeout(() => el.remove(), 350);
  }

  function spawnStray() {
    const d = h('button', 'stray');
    d.setAttribute('aria-label', 'Drone de carga perdido: toca para ficar com a carga');
    d.style.top = `${12 + Math.random() * 40}%`;
    d.append(h('i'));
    d.onclick = () => {
      const gain = S.claimDrone(s());
      toast(`Drone perdido interceptado: +¤${fmt(gain)}.`);
      sfx.coin();
      pulse(credits, 'bump', 400);
      d.remove();
    };
    d.addEventListener('animationend', () => d.remove());
    stage.append(d);
  }

  // ---------- Acontecimentos da simulação -> animações ----------
  let lastSaleFx = 0;
  function drainEvents() {
    for (const ev of S.events) {
      const v = 'floor' in ev ? floorViews[ev.floor] : undefined;
      switch (ev.type) {
        case 'cycle':
          if (v) pulse(v.pileEl, 'bump', 300);
          break;
        case 'load':
          if (v) {
            const crate = h('i', 'crate-fx');
            v.dockCell.append(crate);
            setTimeout(() => crate.remove(), 450);
          }
          pulse(cabin, 'bump', 300);
          break;
        case 'unload':
          pulse(depot, 'bump', 300);
          break;
        case 'sale': {
          const now = performance.now();
          if (now - lastSaleFx < 500) break;
          lastSaleFx = now;
          floatText(market, `+¤${fmt(ev.gain)}`);
          sparks(market, 6);
          pulse(market, 'bump', 300);
          pulse(credits, 'bump', 400);
          break;
        }
        case 'raidStart':
          sfx.alarm();
          openRaid();
          break;
        case 'raidWon':
          closeRaid();
          sfx.win();
          toast('Rusga repelida. A polícia recua e o Heat desce para 45.');
          break;
        case 'raidFail':
          closeRaid();
          sfx.fail();
          pulse(stage, 'shake', 500);
          toast(`Rusga perdida. ${ev.lost > 0 ? `Carga confiscada: ${fmt(ev.lost)}. ` : ''}Pisos ilegais selados ${S.SEAL_TIME}s.`);
          break;
      }
    }
    S.events.length = 0;
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
    if (st.raid) return 'Rusga em curso: toca em todos os drones da polícia.';
    if (!st.elev.manager && st.credits < S.ELEV_MANAGER_COST && st.runEarned < 200) {
      if (st.depot > 0 && st.term.phase === 'idle') return 'Toca em TERMINAL para vender a carga que está no cais.';
      return 'Toca em ELEVADOR para ir buscar a carga ao piso.';
    }
    if (!st.elev.manager) return 'Contrata o gestor do elevador para ele trabalhar sozinho.';
    if (!st.term.manager) return 'Contrata o gestor do terminal para vender sem tocares.';
    if (st.clock < st.sealedUntil) return `Pisos ilegais selados pela polícia: ${fmtTime(st.sealedUntil - st.clock)}.`;
    if (st.heat >= S.HEAT_WARN) return 'Heat quase no máximo. Suborna a polícia ou passa pisos a legal.';
    if (st.floors.some((f) => !f.manager)) return 'Pisos sem gestor só produzem quando lhes tocas.';
    if (fl.bottleneck === 'elev') return 'Gargalo: o elevador não dá vazão. Sobe-lhe o nível.';
    if (fl.bottleneck === 'term') return 'Gargalo: o terminal vende devagar. Sobe-lhe o nível.';
    if (S.heatUnlocked(st) && !st.floors.some((f) => f.illegal)) return `Passa um piso a ILEGAL para produzir x${S.ILLEGAL_MULT}, com o Heat a subir.`;
    return 'Os pisos são o limite. Sobe níveis ou constrói o próximo piso.';
  }

  // ---------- Render (a cada frame) ----------
  let scrolled = false;
  let shown = s().credits; // valor animado do contador de créditos
  let lastFrame = performance.now();
  function render() {
    const now = performance.now();
    const fdt = Math.min(0.1, (now - lastFrame) / 1000);
    lastFrame = now;
    const st = s();
    if (builtFor !== st || floorViews.length !== st.floors.length) {
      const grew = builtFor === st;
      buildFloors();
      if (!grew) {
        scrolled = false;
        shown = st.credits;
        if (raidEl) raidEl.remove();
        raidEl = null;
      }
    }
    if (!scrolled) {
      scroll.scrollTop = scroll.scrollHeight;
      scrolled = true;
    }
    drainEvents();
    if (st.raid && !raidEl) openRaid(); // rusga que vinha de um jogo gravado

    const fl = S.flow(st);
    const mult = S.globalMult(st);

    // O contador persegue o valor real em vez de saltar.
    shown += (st.credits - shown) * Math.min(1, fdt * 10);
    if (Math.abs(st.credits - shown) < Math.max(0.5, st.credits * 1e-4)) shown = st.credits;
    setText(credits, `¤${fmt(shown)}`);
    setText(rate, `+¤${fmt(incomeRate(st))}/s`);
    setText(chips, st.chips > 0 ? `${fmt(st.chips)} chips · +${fmt(st.chips * S.CHIP_BONUS * 100)}%` : '');
    chips.hidden = st.chips === 0;
    const hacking = S.hackActive(st);
    boost.hidden = !hacking;
    toggle(root, 'hacked', hacking);
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

    // Heat
    const heatOn = S.heatUnlocked(st);
    heatRow.hidden = !heatOn;
    if (heatOn) {
      const hr = S.heatRate(st);
      heatFill.style.transform = `scaleX(${st.heat / 100})`;
      const sealed = st.clock < st.sealedUntil;
      setText(
        heatText,
        st.raid ? 'RUSGA' : sealed ? `selado ${fmtTime(st.sealedUntil - st.clock)}` : `${Math.floor(st.heat)}% ${hr > 0 ? '▲ a subir' : st.heat > 0 ? '▼ a descer' : '· calmo'}`,
      );
      toggle(heatRow, 'warn', st.heat >= S.HEAT_WARN);
      toggle(stage, 'alert', st.heat >= S.HEAT_WARN && !st.raid);
      const cost = S.bribeCost(st);
      setText(bribeSub, `¤${fmt(cost)}`);
      bribeBtn.disabled = !!st.raid || st.heat <= 0 || st.credits < cost;
      setAfford(bribeBtn, st.credits, cost);
    }
    if (raidTimer && st.raid) setText(raidTimer, `${Math.max(0, st.raid.until - st.clock).toFixed(1)}s`);

    const cap = S.elevCapacity(st.elev.level, st.floors.length);
    st.floors.forEach((f, i) => {
      const v = floorViews[i];
      const sealed = S.isSealed(st, i);
      setText(v.level, `Nv ${f.level}`);
      const p = f.working ? Math.min(1, f.progress) : 0;
      v.fill.style.transform = `scaleX(${p})`;
      v.bot.style.left = `${(1 - p) * 100}%`;
      toggle(v.bot, 'carry', f.working);
      const lit = f.stock <= 0 ? 0 : Math.max(1, Math.min(5, Math.ceil((5 * f.stock) / cap)));
      v.pile.forEach((c, k) => toggle(c, 'on', k < lit));
      setText(v.rate, `${fmt(S.floorOutput(st, i))}/s`);
      setText(v.stock, `carga ${fmt(f.stock)}`);
      const nm = S.nextMilestone(f.level);
      setText(v.mile, sealed ? 'SELADO' : f.manager ? (nm ? `x2 ao nível ${nm}` : 'bónus no máximo') : 'Toca para produzir');
      v.law.hidden = !heatOn;
      setText(v.law, f.illegal ? `ILEGAL x${S.ILLEGAL_MULT}` : 'LEGAL');
      v.law.setAttribute('aria-pressed', String(!!f.illegal));
      v.law.disabled = sealed;
      toggle(v.row, 'illegal', !!f.illegal);
      toggle(v.row, 'sealed', sealed);
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
      setAfford(buildBtn, st.credits, cost);
    }

    // Elevador
    const e = st.elev;
    cabin.style.transform = `translateY(${-e.pos * FLOOR_H}px)`;
    setText(cabinLoad, e.load > 0 ? fmt(e.load) : '');
    toggle(cabin, 'full', e.load > 0);
    toggle(cabin, 'open', e.phase === 'load' || e.phase === 'unload');
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
    toggle(courier, 'flying', t.phase !== 'idle');
    setText(termCard.level, `Nv ${t.level}`);
    setText(
      termCard.stat,
      t.manager || t.phase !== 'idle' ? `${fmt(S.termRate(t.level))}/s · lucro x${mult === 1 ? '1' : mult.toFixed(2)}` : st.depot > 0 ? 'Toca para vender' : 'À espera de carga',
    );
    termCard.fill.style.transform = `scaleX(${t.phase === 'out' ? t.t : t.phase === 'back' ? 1 : 0})`;
    toggle(termCard.el, 'manual', !t.manager);
    toggle(termCard.el, 'busy', t.phase !== 'idle');
    termCard.ctl.update();

    if (st.clock >= st.nextDrone) {
      st.nextDrone = st.clock + 50 + Math.random() * 50;
      if (!st.raid && !document.querySelector('.stray') && !document.querySelector('.overlay')) spawnStray();
    }
  }

  return { render };
}

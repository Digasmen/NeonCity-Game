import { FLOORS } from './data';

/*
 * Simulação da cadeia vertical: PISOS produzem carga -> o ELEVADOR recolhe-a
 * para o cais -> o TERMINAL vende-a por créditos. O mais lento dos três é o gargalo.
 * Este módulo não toca no DOM, para poder ser testado e reequilibrado à parte.
 */

// ---------- Estado ----------

export interface FloorState {
  level: number;
  stock: number;
  progress: number; // 0..1 do ciclo actual
  working: boolean;
  manager: boolean;
}

export type ElevPhase = 'idle' | 'up' | 'load' | 'down' | 'unload';
export type TermPhase = 'idle' | 'out' | 'back';

export interface GameState {
  v: 1;
  clock: number; // segundos de jogo (inclui tempo offline)
  credits: number;
  sold: number; // total vendido pelo terminal (para medir ¤/s)
  runEarned: number; // ganho desde o último Reboot
  totalEarned: number;
  chips: number;
  reboots: number;
  hacks: number;
  depot: number;
  floors: FloorState[];
  elev: { level: number; manager: boolean; phase: ElevPhase; pos: number; target: number; timer: number; load: number; queued: boolean };
  term: { level: number; manager: boolean; phase: TermPhase; t: number; carry: number; queued: boolean };
  /** Overclock por estação: 'f0', 'f1', ..., 'elev', 'term'. */
  oc: Record<string, { until: number; ready: number }>;
  hackUntil: number;
  hackMult: number;
  hackReady: number;
  nextDrone: number;
  savedAt: number;
}

function newFloor(manager = false): FloorState {
  return { level: 1, stock: 0, progress: 0, working: false, manager };
}

export function newState(chips = 0, totalEarned = 0, reboots = 0, hacks = 0): GameState {
  return {
    v: 1,
    clock: 0,
    credits: 0,
    sold: 0,
    runEarned: 0,
    totalEarned,
    chips,
    reboots,
    hacks,
    depot: 0,
    // O primeiro piso já vem com gestor, para o jogo arrancar a mexer.
    floors: [newFloor(true)],
    elev: { level: 1, manager: false, phase: 'idle', pos: 0, target: 0, timer: 0, load: 0, queued: false },
    term: { level: 1, manager: false, phase: 'idle', t: 0, carry: 0, queued: false },
    oc: {},
    hackUntil: 0,
    hackMult: 1,
    hackReady: 0,
    nextDrone: 50,
    savedAt: Date.now(),
  };
}

// ---------- Equilíbrio ----------

export const MILESTONES = [10, 25, 50, 100, 200, 300, 400, 500];
export const CHIP_BONUS = 0.05;
export const OFFLINE_CAP = 2 * 3600;
export const OC_DURATION = 30;
export const OC_COOLDOWN = 180;
export const ELEV_MANAGER_COST = 40;
export const TERM_MANAGER_COST = 60;

const FLOOR_GROWTH = 1.14;
const STATION_GROWTH = 1.215;
const MOVE_TIME = 0.35; // s por piso
const LOAD_TIME = 0.3;
const TERM_LEG = 1.5; // s por trajecto (ida ou volta)

export function milestoneMult(level: number): number {
  let m = 1;
  for (const t of MILESTONES) if (level >= t) m *= 2;
  return m;
}
export function nextMilestone(level: number): number | null {
  return MILESTONES.find((t) => t > level) ?? null;
}

export const floorUnlockCost = (i: number) => (i === 0 ? 0 : 100 * Math.pow(22, i));
export const floorManagerCost = (i: number) => 60 * Math.pow(22, i);
/** Custo de subir do nível `level` para `level + 1`. */
export const floorLevelCost = (i: number, level: number) => 8 * Math.pow(18, i) * Math.pow(FLOOR_GROWTH, level - 1);
/** Produção em carga/segundo. */
export const floorRate = (i: number, level: number) => 2 * Math.pow(11, i) * level * milestoneMult(level);
export const floorCycle = (i: number) => Math.min(1.5 + 0.4 * i, 5);

export const elevRate = (level: number) => 8 * Math.pow(1.18, level - 1);
export const elevLevelCost = (level: number) => 20 * Math.pow(STATION_GROWTH, level);
export const elevTripTime = (floors: number) => floors * (2 * MOVE_TIME + LOAD_TIME) + LOAD_TIME;
export const elevCapacity = (level: number, floors: number) => elevRate(level) * elevTripTime(floors);

export const termRate = (level: number) => 10 * Math.pow(1.18, level - 1);
export const termLevelCost = (level: number) => 25 * Math.pow(STATION_GROWTH, level);
export const termCapacity = (level: number) => termRate(level) * 2 * TERM_LEG;

/** Custo total de `n` níveis numa progressão geométrica que começa em `base`. */
export function bulkCost(base: number, growth: number, n: number): number {
  return (base * (Math.pow(growth, n) - 1)) / (growth - 1);
}
export function maxAffordable(base: number, growth: number, credits: number): number {
  if (credits < base) return 0;
  return Math.floor(Math.log((credits * (growth - 1)) / base + 1) / Math.log(growth) + 1e-9);
}

export type StationKey = string; // 'f<i>' | 'elev' | 'term'

/** Custo do próximo nível e factor de crescimento de uma estação. */
export function levelCostOf(s: GameState, key: StationKey): { base: number; growth: number } {
  if (key === 'elev') return { base: elevLevelCost(s.elev.level), growth: STATION_GROWTH };
  if (key === 'term') return { base: termLevelCost(s.term.level), growth: STATION_GROWTH };
  const i = Number(key.slice(1));
  return { base: floorLevelCost(i, s.floors[i].level), growth: FLOOR_GROWTH };
}

/** Resolve o modo de compra (x1, x10, MAX) em quantidade e custo. */
export function quote(s: GameState, key: StationKey, mode: number | 'max'): { n: number; cost: number } {
  const { base, growth } = levelCostOf(s, key);
  const n = mode === 'max' ? Math.max(1, maxAffordable(base, growth, s.credits)) : mode;
  return { n, cost: bulkCost(base, growth, n) };
}

// ---------- Leituras ----------

export function isOverclocked(s: GameState, key: StationKey): boolean {
  const o = s.oc[key];
  return !!o && s.clock < o.until;
}
const speed = (s: GameState, key: StationKey) => (isOverclocked(s, key) ? 2 : 1);

export function hackActive(s: GameState): boolean {
  return s.clock < s.hackUntil;
}
export function globalMult(s: GameState): number {
  return (1 + CHIP_BONUS * s.chips) * (hackActive(s) ? s.hackMult : 1);
}

/** Débitos teóricos (carga/s) de cada etapa; o mínimo é o gargalo. */
export function flow(s: GameState) {
  let floors = 0;
  s.floors.forEach((f, i) => {
    if (f.manager) floors += floorRate(i, f.level) * speed(s, 'f' + i);
  });
  const elev = elevRate(s.elev.level) * speed(s, 'elev');
  const term = termRate(s.term.level) * speed(s, 'term');
  const chain = Math.min(floors, elev, term);
  const bottleneck: 'floors' | 'elev' | 'term' = chain === floors ? 'floors' : chain === elev ? 'elev' : 'term';
  return { floors, elev, term, chain, bottleneck };
}

export function chipsOnReboot(s: GameState): number {
  return Math.floor(Math.sqrt(s.runEarned / 5e5));
}

// ---------- Acções do jogador ----------

function spend(s: GameState, cost: number): boolean {
  if (s.credits < cost) return false;
  s.credits -= cost;
  return true;
}

export function buyLevels(s: GameState, key: StationKey, mode: number | 'max'): boolean {
  const { n, cost } = quote(s, key, mode);
  if (!spend(s, cost)) return false;
  if (key === 'elev') s.elev.level += n;
  else if (key === 'term') s.term.level += n;
  else s.floors[Number(key.slice(1))].level += n;
  return true;
}

export function managerCost(key: StationKey): number {
  if (key === 'elev') return ELEV_MANAGER_COST;
  if (key === 'term') return TERM_MANAGER_COST;
  return floorManagerCost(Number(key.slice(1)));
}
export function hasManager(s: GameState, key: StationKey): boolean {
  if (key === 'elev') return s.elev.manager;
  if (key === 'term') return s.term.manager;
  return s.floors[Number(key.slice(1))].manager;
}
export function hireManager(s: GameState, key: StationKey): boolean {
  if (hasManager(s, key) || !spend(s, managerCost(key))) return false;
  if (key === 'elev') s.elev.manager = true;
  else if (key === 'term') s.term.manager = true;
  else s.floors[Number(key.slice(1))].manager = true;
  return true;
}

export function canUnlockFloor(s: GameState): boolean {
  return s.floors.length < FLOORS.length;
}
export function unlockFloor(s: GameState): boolean {
  if (!canUnlockFloor(s) || !spend(s, floorUnlockCost(s.floors.length))) return false;
  s.floors.push(newFloor());
  return true;
}

/** Toque manual: arranca um ciclo/viagem numa estação sem gestor. */
export function tap(s: GameState, key: StationKey): void {
  if (key === 'elev') s.elev.queued = true;
  else if (key === 'term') s.term.queued = true;
  else s.floors[Number(key.slice(1))].working = true;
}

export function overclock(s: GameState, key: StationKey): boolean {
  if (!hasManager(s, key)) return false;
  const o = s.oc[key];
  if (o && s.clock < o.ready) return false;
  s.oc[key] = { until: s.clock + OC_DURATION, ready: s.clock + OC_COOLDOWN };
  return true;
}

function earn(s: GameState, amount: number): void {
  s.credits += amount;
  s.runEarned += amount;
  s.totalEarned += amount;
}

/** Prémio do mini-jogo de hacking, conforme as rondas ultrapassadas (0 a 3). */
export function applyHack(s: GameState, cleared: number): { mult: number; duration: number; lump: number } {
  const tiers = [
    { mult: 1, duration: 0, lumpSec: 0 },
    { mult: 2, duration: 30, lumpSec: 0 },
    { mult: 2, duration: 60, lumpSec: 30 },
    { mult: 3, duration: 60, lumpSec: 60 },
  ];
  const t = tiers[Math.max(0, Math.min(3, cleared))];
  const lump = t.lumpSec > 0 ? Math.max(25, t.lumpSec * flow(s).chain * (1 + CHIP_BONUS * s.chips)) : 0;
  if (t.duration > 0) {
    s.hackMult = t.mult;
    s.hackUntil = s.clock + t.duration;
    s.hacks++;
  }
  if (lump > 0) earn(s, lump);
  s.hackReady = s.clock + (cleared > 0 ? 180 : 45);
  return { mult: t.mult, duration: t.duration, lump };
}

/** Drone de carga perdido: quem o apanhar fica com a carga. */
export function claimDrone(s: GameState): number {
  const gain = Math.max(20, 30 * flow(s).chain * globalMult(s));
  earn(s, gain);
  return gain;
}

export function reboot(s: GameState): GameState {
  return newState(s.chips + chipsOnReboot(s), s.totalEarned, s.reboots + 1, s.hacks);
}

// ---------- Avanço do tempo ----------

/** Avança a simulação `dt` segundos (usar passos pequenos, <= 0.1s). */
export function tick(s: GameState, dt: number): void {
  s.clock += dt;
  const n = s.floors.length;

  for (let i = 0; i < n; i++) {
    const f = s.floors[i];
    if (f.manager) f.working = true;
    if (!f.working) continue;
    f.progress += (dt * speed(s, 'f' + i)) / floorCycle(i);
    if (f.progress >= 1) {
      f.stock += floorRate(i, f.level) * floorCycle(i);
      f.progress = f.manager ? f.progress - 1 : 0;
      f.working = f.manager;
    }
  }

  const e = s.elev;
  const es = dt * speed(s, 'elev');
  const cap = elevCapacity(e.level, n);
  switch (e.phase) {
    case 'idle':
      if (e.manager || e.queued) {
        e.queued = false;
        e.target = 0;
        e.phase = 'up';
      }
      break;
    case 'up':
      e.pos += es / MOVE_TIME;
      if (e.pos >= e.target + 1) {
        e.pos = e.target + 1;
        e.phase = 'load';
        e.timer = s.floors[e.target].stock > 0 ? LOAD_TIME : 0;
      }
      break;
    case 'load':
      e.timer -= es;
      if (e.timer <= 0) {
        const f = s.floors[e.target];
        const take = Math.min(f.stock, cap - e.load);
        f.stock -= take;
        e.load += take;
        if (e.load >= cap * 0.999 || e.target >= n - 1) e.phase = 'down';
        else {
          e.target++;
          e.phase = 'up';
        }
      }
      break;
    case 'down':
      e.pos -= es / MOVE_TIME;
      if (e.pos <= 0) {
        e.pos = 0;
        e.phase = 'unload';
        e.timer = LOAD_TIME;
      }
      break;
    case 'unload':
      e.timer -= es;
      if (e.timer <= 0) {
        s.depot += e.load;
        e.load = 0;
        e.phase = 'idle';
      }
      break;
  }

  const t = s.term;
  const ts = dt * speed(s, 'term');
  switch (t.phase) {
    case 'idle':
      if ((t.manager || t.queued) && s.depot > 0) {
        t.carry = Math.min(s.depot, termCapacity(t.level));
        s.depot -= t.carry;
        t.phase = 'out';
        t.t = 0;
      }
      t.queued = false;
      break;
    case 'out':
      t.t += ts / TERM_LEG;
      if (t.t >= 1) {
        const gain = t.carry * globalMult(s);
        earn(s, gain);
        s.sold += gain;
        t.carry = 0;
        t.phase = 'back';
        t.t = 0;
      }
      break;
    case 'back':
      t.t += ts / TERM_LEG;
      if (t.t >= 1) {
        t.phase = 'idle';
        t.t = 0;
      }
      break;
  }
}

/**
 * Aproxima um período longo sem simular cada passo (jogo fechado ou separador escondido).
 * Só as estações com gestor trabalham. Devolve os créditos ganhos.
 */
export function simulateOffline(s: GameState, seconds: number): number {
  const sec = Math.max(0, Math.min(seconds, OFFLINE_CAP));
  s.clock += sec;
  s.floors.forEach((f, i) => {
    if (f.manager) f.stock += floorRate(i, f.level) * sec;
  });
  if (s.elev.manager) {
    let room = elevRate(s.elev.level) * sec;
    for (const f of s.floors) {
      const take = Math.min(f.stock, room);
      f.stock -= take;
      s.depot += take;
      room -= take;
    }
  }
  let gain = 0;
  if (s.term.manager) {
    const soldCargo = Math.min(s.depot, termRate(s.term.level) * sec);
    s.depot -= soldCargo;
    gain = soldCargo * (1 + CHIP_BONUS * s.chips);
    earn(s, gain);
  }
  return gain;
}

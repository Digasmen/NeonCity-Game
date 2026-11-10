import { describe, expect, it } from 'vitest';
import { fmt } from '../src/game/format';
import * as S from '../src/game/sim';
import { FLOORS } from '../src/game/data';

function run(s: S.GameState, seconds: number) {
  for (let t = 0; t < seconds; t += 0.1) S.tick(s, 0.1);
}

describe('formatação', () => {
  it('abrevia números grandes', () => {
    expect(fmt(0)).toBe('0');
    expect(fmt(999)).toBe('999');
    expect(fmt(1234)).toBe('1.23K');
    expect(fmt(999999)).toBe('999K');
    expect(fmt(5e9)).toBe('5.00B');
  });
});

describe('compras', () => {
  it('bulkCost e maxAffordable são coerentes', () => {
    const cost = S.bulkCost(8, 1.14, 10);
    expect(S.maxAffordable(8, 1.14, cost)).toBe(10);
    expect(S.maxAffordable(8, 1.14, cost - 0.01)).toBe(9);
    expect(S.maxAffordable(8, 1.14, 7)).toBe(0);
  });

  it('não compra sem créditos', () => {
    const s = S.newState();
    expect(S.buyLevels(s, 'f0', 1)).toBe(false);
    s.credits = 1000;
    expect(S.buyLevels(s, 'f0', 10)).toBe(true);
    expect(s.floors[0].level).toBe(11);
    expect(S.milestoneMult(11)).toBe(2);
  });
});

describe('cadeia', () => {
  it('sem gestores no elevador e terminal nada se vende sozinho', () => {
    const s = S.newState();
    run(s, 30);
    expect(s.floors[0].stock).toBeGreaterThan(0);
    expect(s.credits).toBe(0);
  });

  it('toques manuais levam a carga até à venda', () => {
    const s = S.newState();
    run(s, 5);
    S.tap(s, 'elev');
    run(s, 3);
    expect(s.depot).toBeGreaterThan(0);
    S.tap(s, 'term');
    run(s, 4);
    expect(s.credits).toBeGreaterThan(0);
  });

  it('com gestores, o rendimento aproxima-se do gargalo', () => {
    const s = S.newState();
    s.elev.manager = true;
    s.term.manager = true;
    run(s, 200);
    const rate = s.credits / 200;
    expect(rate).toBeGreaterThan(S.flow(s).chain * 0.8);
    expect(rate).toBeLessThanOrEqual(S.flow(s).chain * 1.05);
  });

  it('offline rende o mesmo que o gargalo permite', () => {
    const s = S.newState();
    s.elev.manager = true;
    s.term.manager = true;
    const gain = S.simulateOffline(s, 1000);
    expect(gain).toBeCloseTo(S.flow(s).chain * 1000, 3);
  });

  it('reboot mantém chips e reinicia a torre', () => {
    const s = S.newState();
    s.runEarned = 5e7;
    s.floors.push({ level: 5, stock: 0, progress: 0, working: false, manager: true });
    const r = S.reboot(s);
    expect(r.chips).toBe(10);
    expect(r.floors.length).toBe(1);
    expect(S.globalMult(r)).toBeCloseTo(1.5);
  });
});

describe('heat', () => {
  function tower() {
    const s = S.newState();
    s.floors.push({ level: 1, stock: 0, progress: 0, working: false, manager: true, illegal: false });
    s.elev.manager = true;
    s.term.manager = true;
    return s;
  }

  it('só existe a partir do segundo piso', () => {
    const s = S.newState();
    expect(S.toggleIllegal(s, 0)).toBe(false);
    expect(S.toggleIllegal(tower(), 0)).toBe(true);
  });

  it('pisos ilegais produzem mais e sobem o heat até à rusga', () => {
    const s = tower();
    const legal = S.flow(s).floors;
    S.toggleIllegal(s, 0);
    S.toggleIllegal(s, 1);
    expect(S.flow(s).floors).toBeCloseTo(legal * S.ILLEGAL_MULT);
    expect(S.heatRate(s)).toBeGreaterThan(0);
    run(s, 86);
    expect(s.raid).not.toBeNull();
  });

  it('abater todos os drones repele a rusga', () => {
    const s = tower();
    S.toggleIllegal(s, 0);
    s.heat = 99.99;
    run(s, 1);
    const n = s.raid!.left;
    for (let i = 0; i < n; i++) S.raidHit(s);
    expect(s.raid).toBeNull();
    expect(s.heat).toBe(45);
    expect(s.sealedUntil).toBe(0);
  });

  it('rusga perdida confisca o cais e sela os pisos ilegais', () => {
    const s = tower();
    S.toggleIllegal(s, 1);
    s.heat = 99.99;
    s.elev.manager = false;
    run(s, 1);
    s.depot = 500;
    s.term.manager = false;
    run(s, S.RAID_TIME + 1);
    expect(s.raid).toBeNull();
    expect(s.depot).toBe(0);
    expect(S.isSealed(s, 1)).toBe(true);
    expect(S.isSealed(s, 0)).toBe(false);
    const before = s.floors[1].stock;
    run(s, 5);
    expect(s.floors[1].stock).toBe(before);
    run(s, S.SEAL_TIME);
    expect(s.floors[1].stock).toBeGreaterThan(before);
  });

  it('suborno baixa o heat e custa créditos', () => {
    const s = tower();
    s.heat = 60;
    expect(S.bribe(s)).toBe(false);
    s.credits = 1e6;
    expect(S.bribe(s)).toBe(true);
    expect(s.heat).toBe(60 - S.BRIBE_DROP);
    expect(s.credits).toBeLessThan(1e6);
  });

  it('offline o heat só desce e não há rusgas', () => {
    const s = tower();
    S.toggleIllegal(s, 0);
    S.toggleIllegal(s, 1);
    s.heat = 90;
    S.simulateOffline(s, 3600);
    expect(s.heat).toBe(0);
    expect(s.raid).toBeNull();
  });
});

describe('ritmo de progressão (bot ganancioso)', () => {
  it('desbloqueia vários pisos na primeira hora', () => {
    const s = S.newState();
    const reached: string[] = [];
    for (let t = 0; t < 3600; t += 0.1) {
      S.tick(s, 0.1);
      if (!s.elev.manager) S.tap(s, 'elev');
      if (!s.term.manager) S.tap(s, 'term');
      if (Math.round(t * 10) % 10 !== 0) continue;
      for (const k of ['elev', 'term', ...s.floors.map((_, i) => 'f' + i)]) S.hireManager(s, k);
      if (S.unlockFloor(s)) reached.push(`piso ${s.floors.length} aos ${Math.round(t / 60)}min`);
      // Investe sempre no gargalo.
      for (let guard = 0; guard < 50; guard++) {
        const b = S.flow(s).bottleneck;
        const key =
          b === 'floors'
            ? s.floors
                .map((f, i) => ({ k: 'f' + i, c: S.floorLevelCost(i, f.level) / S.floorRate(i, 1) }))
                .sort((a, z) => a.c - z.c)[0].k
            : b;
        if (!S.buyLevels(s, key, 1)) break;
      }
    }
    console.log(reached.join(' | '), '| chips no reboot:', S.chipsOnReboot(s));
    expect(s.floors.length).toBeGreaterThanOrEqual(4);
    expect(s.floors.length).toBeLessThan(FLOORS.length);
  });
});

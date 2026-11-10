import { GameState, newState } from './sim';

const KEY = 'neonstack.save.v1';

// O localStorage pode falhar (modo privado, armazenamento bloqueado): o jogo corre na mesma, só não guarda.

export function save(s: GameState): void {
  s.savedAt = Date.now();
  try {
    localStorage.setItem(KEY, JSON.stringify(s));
  } catch {
    /* sem armazenamento */
  }
}

export function load(): GameState | null {
  try {
    const raw = localStorage.getItem(KEY);
    if (!raw) return null;
    const data = JSON.parse(raw);
    if (!data || data.v !== 1 || !Array.isArray(data.floors) || data.floors.length === 0) return null;
    const base = newState();
    return { ...base, ...data, elev: { ...base.elev, ...data.elev }, term: { ...base.term, ...data.term } };
  } catch {
    return null;
  }
}

export function wipe(): void {
  try {
    localStorage.removeItem(KEY);
  } catch {
    /* sem armazenamento */
  }
}

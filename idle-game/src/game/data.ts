export interface FloorDef {
  name: string;
  /** Nome do gestor (IA ou personagem) que automatiza o piso. */
  manager: string;
  /** Matiz néon do piso (graus HSL). */
  hue: number;
}

/** Pisos da megatorre, do rés-do-chão para o topo. */
export const FLOORS: FloorDef[] = [
  { name: 'Noodles Sintéticos', manager: 'Mama-9', hue: 22 },
  { name: 'Oficina de Sucata', manager: 'Rust', hue: 48 },
  { name: 'Arcade Neural', manager: 'Glitch', hue: 285 },
  { name: 'Clínica de Implantes', manager: 'Dr. Bisturi', hue: 168 },
  { name: 'Quinta de Dados', manager: 'Daemon', hue: 205 },
  { name: 'Clube Holográfico', manager: 'Vex', hue: 322 },
  { name: 'Biolab de Clones', manager: 'Dra. Helix', hue: 135 },
  { name: 'Fábrica de Androides', manager: 'Unit Zero', hue: 0 },
  { name: 'Covil de Netrunners', manager: 'Ghost', hue: 258 },
  { name: 'Forja de IA', manager: 'Oráculo', hue: 188 },
  { name: 'Banco de Cripto-Sombras', manager: 'A Directora', hue: 62 },
  { name: 'Doca Orbital', manager: 'HALCYON', hue: 232 },
];

export const ELEV_MANAGER = 'Lift-OS';
export const TERM_MANAGER = 'Mr. Wu';

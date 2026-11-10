# Neon Stack

Jogo idle de cadeia vertical num cenário cyberpunk. Geres uma megatorre: os **pisos** produzem carga, o **elevador** leva-a para o cais e o **terminal** vende-a no mercado negro. O mais lento dos três limita o lucro.

Projecto independente do city-builder Unity que está na raiz deste repositório.

## Correr

```bash
cd idle-game
npm install
npm run dev      # servidor local com recarregamento
npm run build    # gera dist/index.html, um ficheiro único que abre directamente no browser
npm test         # testes da simulação
```

Teste de fumo no browser (precisa do Playwright instalado):

```bash
npm run build && node scripts/smoke.mjs dist/index.html <pasta-para-capturas>
```

## O que já existe

- 12 pisos temáticos, cada um com gestor próprio e bónus x2 nos níveis 10, 25, 50, 100, 200…
- Elevador e terminal com níveis e gestores; barra de fluxo que assinala o gargalo.
- Compra em x1, x10 ou MAX.
- Overclock por gestor: velocidade x2 durante 30s, 3 minutos de recarga.
- Mini-jogo **Quebrar o ICE** (memória de sequência, 3 rondas) que dá lucro x2 ou x3 temporário e créditos.
- Drone de carga perdido que atravessa a torre de vez em quando; tocar-lhe dá créditos.
- **Reboot** (prestige): troca a torre por Chips de Memória, +5% de lucro permanente cada.
- Ganhos offline até 2 horas e gravação automática em `localStorage`.

## Estrutura

| Ficheiro | Conteúdo |
|---|---|
| `src/game/sim.ts` | Estado, fórmulas de equilíbrio e simulação. Não toca no DOM. |
| `src/game/data.ts` | Nomes dos pisos e dos gestores. |
| `src/game/save.ts` | Gravação e leitura do progresso. |
| `src/ui/ui.ts` | Ecrã principal: HUD, torre, painel do elevador e do terminal, modais. |
| `src/ui/hack.ts` | Mini-jogo de hacking. |
| `tests/sim.test.ts` | Testes da simulação e um bot que mede o ritmo de progressão. |

Para afinar o equilíbrio, as constantes estão no bloco "Equilíbrio" de `sim.ts`. Na consola do browser, `neon.state` dá acesso ao estado do jogo.

## Ideias para a seguir

- **Heat**: pisos ilegais rendem mais mas atraem rusgas da polícia corporativa.
- **Facções** (Corpos, Gangues, Netrunners) com gestores exclusivos por Reboot.
- Árvore de implantes onde gastar os chips.
- Mais mini-jogos: corrida de drones, leilão do mercado negro, defesa contra rusga.
- Segunda torre e mercado negro rotativo com ofertas temporárias.
- Som e arte própria para pisos e personagens.

// Teste de fumo no browser: joga uns segundos, tira capturas e reporta erros.
// Uso: node scripts/smoke.mjs dist/index.html <pasta-para-capturas>
import { createRequire } from 'node:module';
import { pathToFileURL } from 'node:url';

// Playwright está instalado globalmente, por isso é carregado com require.
const { chromium } = createRequire(import.meta.url)('playwright');
const [file, out] = process.argv.slice(2);
const browser = await chromium.launch();
const errors = [];
const sizes = [
  ['phone', { width: 390, height: 780 }],
  ['desktop', { width: 1100, height: 800 }],
];

for (const [name, viewport] of sizes) {
  const page = await browser.newPage({ viewport });
  page.on('pageerror', (e) => errors.push(`${name}: ${e.message}`));
  page.on('console', (m) => {
    if (m.type() === 'error') errors.push(`${name} consola: ${m.text()}`);
  });
  await page.goto(pathToFileURL(file).href);
  await page.waitForTimeout(2500);
  await page.screenshot({ path: `${out}/${name}-start.png` });

  // Loop manual: elevador, depois terminal.
  await page.click('.dock .card:nth-child(1)');
  await page.waitForTimeout(2200);
  await page.click('.dock .card:nth-child(2)');
  await page.waitForTimeout(2200);
  const credits = await page.evaluate(() => window.neon.state.credits);
  if (!(credits > 0)) errors.push(`${name}: a venda manual não rendeu créditos`);

  // Salta para um estado de meio de jogo.
  await page.evaluate(() => {
    const s = window.neon.state;
    s.credits = 5e7;
    s.runEarned = 6e7;
    s.chips = 12;
    s.nextDrone = s.clock + 1;
  });
  for (let i = 0; i < 3; i++) await page.click('.build .btn');
  await page.click('.mode:nth-child(2)');
  for (const sel of ['.floor .btn.up', '.floor .btn.mgr', '.card .btn.up', '.card .btn.mgr']) {
    for (const b of await page.$$(sel)) await b.click();
  }
  await page.waitForTimeout(6000);
  await page.screenshot({ path: `${out}/${name}-mid.png` });
  const overflow = await page.evaluate(() => document.documentElement.scrollWidth > innerWidth);
  if (overflow) errors.push(`${name}: scroll horizontal`);

  await page.click('.btn.hack');
  await page.click('.modal .btn.primary');
  await page.waitForTimeout(1600);
  await page.screenshot({ path: `${out}/${name}-hack.png` });
  await page.click('.modal .close');
  await page.click('.hud-btns .btn:nth-child(2)');
  await page.waitForTimeout(200);
  await page.screenshot({ path: `${out}/${name}-reboot.png` });

  const st = await page.evaluate(() => {
    const s = window.neon.state;
    return { floors: s.floors.length, credits: s.credits, sold: s.sold, depot: s.depot };
  });
  console.log(name, JSON.stringify(st));
  await page.close();
}

await browser.close();
console.log(errors.length ? 'ERROS:\n' + errors.join('\n') : 'sem erros');
if (errors.length) process.exit(1);

import { defineConfig } from 'vite';
import { viteSingleFile } from 'vite-plugin-singlefile';

// O build gera um único dist/index.html (JS e CSS embutidos), fácil de alojar ou abrir directamente.
export default defineConfig({
  base: './',
  plugins: [viteSingleFile()],
});

/** Cria um elemento com classe e texto opcionais. */
export function h<K extends keyof HTMLElementTagNameMap>(tag: K, cls = '', text = ''): HTMLElementTagNameMap[K] {
  const el = document.createElement(tag);
  if (cls) el.className = cls;
  if (text) el.textContent = text;
  return el;
}

const textCache = new WeakMap<Element, string>();
/** Só toca no DOM quando o texto muda (o render corre a cada frame). */
export function setText(el: Element, text: string): void {
  if (textCache.get(el) === text) return;
  textCache.set(el, text);
  el.textContent = text;
}

export function toggle(el: Element, cls: string, on: boolean): void {
  if (el.classList.contains(cls) !== on) el.classList.toggle(cls, on);
}

/** Janela modal simples. Devolve o corpo para preencher e uma função para fechar. */
export function openModal(title: string, opts: { closable?: boolean; onClose?: () => void } = {}) {
  const overlay = h('div', 'overlay');
  const box = h('div', 'modal');
  box.setAttribute('role', 'dialog');
  box.setAttribute('aria-label', title);
  const head = h('div', 'modal-head');
  head.append(h('h2', '', title));
  const body = h('div', 'modal-body');
  let closed = false;
  const close = () => {
    if (closed) return;
    closed = true;
    overlay.remove();
    opts.onClose?.();
  };
  if (opts.closable !== false) {
    const x = h('button', 'btn ghost close', 'Fechar');
    x.onclick = close;
    head.append(x);
    overlay.addEventListener('click', (ev) => {
      if (ev.target === overlay) close();
    });
  }
  box.append(head, body);
  overlay.append(box);
  document.body.append(overlay);
  return { body, close };
}

export function toast(msg: string): void {
  let wrap = document.querySelector('.toasts');
  if (!wrap) {
    wrap = h('div', 'toasts');
    document.body.append(wrap);
  }
  const t = h('div', 'toast', msg);
  wrap.append(t);
  setTimeout(() => t.remove(), 3200);
}

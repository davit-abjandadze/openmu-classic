'use strict';

// The item exchange page: the settings, the items you can get (filtered by class and kind) and the
// items you can give in, all from the current configuration of the server.
(() => {
  const Classes = window.OldMuClasses;
  const { russian, t, number } = window.OldMuLocale;
  const grid = document.querySelector('[data-targets]');
  const inputs = document.querySelector('[data-inputs]');
  const message = document.querySelector('[data-message]');

  // Item groups of the game.
  const groups = {
    0: 'Sword', 1: 'Axe', 2: 'Mace', 3: 'Spear', 4: 'Bow', 5: 'Staff', 6: 'Shield',
    7: 'Helm', 8: 'Armor', 9: 'Pants', 10: 'Gloves', 11: 'Boots',
  };
  const categories = [
    ['all', 'All', () => true],
    ['weapon', 'Weapons', (g) => g <= 5],
    ['shield', 'Shields', (g) => g === 6],
    ['helm', 'Helms', (g) => g === 7],
    ['armor', 'Armors', (g) => g === 8],
    ['pants', 'Pants', (g) => g === 9],
    ['gloves', 'Gloves', (g) => g === 10],
    ['boots', 'Boots', (g) => g === 11],
  ];
  const familyNames = { dw: 'DW', dk: 'DK', elf: 'Elf', mg: 'MG' };

  let targets = [];
  let family = 'all';
  let category = 'all';

  const families = (item) => [...new Set(item.classes.map(Classes.family).filter(Boolean))];
  const statName = (designation) => (designation || '').replace(/^Total /, '');

  function el(tag, className, text) {
    const node = document.createElement(tag);
    if (className) node.className = className;
    if (text !== undefined) node.textContent = text;
    return node;
  }

  function renderTargets() {
    const filter = categories.find(([key]) => key === category)[2];
    const shown = targets.filter((t) => filter(t.group) && (family === 'all' || families(t).includes(family)));
    if (shown.length === 0) {
      grid.replaceChildren(el('p', 'empty', t('No item for this choice.')));
      return;
    }

    grid.replaceChildren(...shown.map((item) => {
      const card = el('article', 'item-card');
      const head = el('div', 'item-head');
      head.append(el('span', 'item-kind', t(groups[item.group] || 'Item')), el('span', 'item-size', `${item.width}×${item.height}`));
      card.append(head, el('h3', '', item.name));

      const badges = el('div', 'item-classes');
      for (const f of families(item)) badges.append(el('span', `badge badge-${f}`, familyNames[f]));
      card.append(badges);

      const reqs = el('ul', 'item-reqs');
      const order = ['Level', 'Strength', 'Agility', 'Vitality', 'Energy', 'Leadership'];
      const sorted = [...item.requirements].sort((a, b) => order.indexOf(statName(a.attribute)) - order.indexOf(statName(b.attribute)));
      for (const r of sorted) {
        const li = el('li');
        li.append(el('span', '', t(statName(r.attribute))), el('b', '', number(r.value)));
        reqs.append(li);
      }
      if (item.requirements.length) {
        card.append(el('div', 'item-label', t('Requires (excellent +0)')), reqs);
      }

      const command = `/exchange ${item.name}`;
      const cmd = el('button', 'item-cmd', command);
      cmd.type = 'button';
      cmd.title = t('Copy the command');
      cmd.addEventListener('click', async () => {
        try {
          await navigator.clipboard.writeText(command);
          cmd.textContent = t('Copied!');
          setTimeout(() => { cmd.textContent = command; }, 1200);
        } catch {
          // Clipboard needs HTTPS; the command is visible anyway.
        }
      });
      card.append(cmd);
      return card;
    }));
  }

  function renderInputs(items) {
    const byGroup = new Map();
    for (const item of items) {
      const name = groups[item.group];
      if (!name) continue;
      if (!byGroup.has(name)) byGroup.set(name, []);
      byGroup.get(name).push(item);
    }
    inputs.replaceChildren(...[...byGroup].map(([name, list]) => {
      const box = el('details', 'input-group');
      const summary = el('summary', '', t(name.endsWith('s') ? name : name + 's'));
      summary.append(el('span', 'count', list.length));
      box.append(summary);
      const ul = el('ul');
      for (const item of list) {
        const li = el('li');
        const level = russian ? `уровень выпадения ${item.dropLevel}` : `drop level ${item.dropLevel}`;
        li.append(el('span', '', item.name), el('small', '', level));
        ul.append(li);
      }
      box.append(ul);
      return box;
    }));
  }

  function renderCategoryTabs() {
    const container = document.querySelector('[data-category-tabs]');
    container.replaceChildren(...categories.map(([key, label]) => {
      const button = el('button', '', t(label));
      button.type = 'button';
      button.setAttribute('aria-pressed', String(key === category));
      button.addEventListener('click', () => {
        category = key;
        container.querySelectorAll('button').forEach((b) => b.setAttribute('aria-pressed', String(b === button)));
        renderTargets();
      });
      return button;
    }));
  }

  async function load() {
    try {
      const response = await fetch('/api/guide/exchange');
      const exchange = await response.json();
      if (!response.ok) throw new Error();
      const values = { ...exchange, maximumOption: exchange.maximumOptionLevel * 4 };
      document.querySelectorAll('[data-x]').forEach((node) => {
        if (values[node.dataset.x] !== undefined) node.textContent = values[node.dataset.x];
      });
      if (!exchange.active) {
        message.textContent = t('The item exchange is switched off at the moment.');
        message.hidden = false;
      }
      targets = exchange.targets;
      renderTargets();
      renderInputs(exchange.inputs);
    } catch {
      grid.replaceChildren(el('p', 'empty', t('The list is not available right now.')));
    }
  }

  Classes.renderTabs(document.querySelector('[data-class-tabs]'), family, (chosen) => {
    family = chosen;
    renderTargets();
  });
  renderCategoryTabs();
  load();
})();

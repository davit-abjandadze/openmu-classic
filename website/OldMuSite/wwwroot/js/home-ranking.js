'use strict';

// The "Top Players" block of the home page: the top 10 of three boards, filtered by class family.
(() => {
  const tabs = document.querySelector('[data-class-tabs]');
  const cards = [...document.querySelectorAll('[data-top-board]')];
  if (!tabs || cards.length === 0) return;

  const Classes = window.OldMuClasses;
  const number = (value) => (value ?? 0).toLocaleString('en-US');
  const columns = {
    characters: [(e) => e.resets, (e) => e.level],
    'pvp-week': [(e) => e.prestige, (e) => e.kills, (e) => e.deaths, (e) => e.assists],
    'pvp-all': [(e) => e.prestige, (e) => e.kills, (e) => e.deaths, (e) => e.assists],
  };
  const data = {};
  let filter = 'all';

  function message(card, text) {
    const row = document.createElement('tr');
    const td = document.createElement('td');
    td.className = 'empty';
    td.colSpan = 2 + columns[card.dataset.topBoard].length;
    td.textContent = text;
    row.append(td);
    card.querySelector('tbody').replaceChildren(row);
  }

  function render(card) {
    const board = card.dataset.topBoard;
    const link = card.querySelector('.see-all');
    link.href = `/ranking.html#${board}${filter === 'all' ? '' : '/' + filter}`;
    const entries = data[board];
    if (!entries) return;

    const matching = entries.filter((e) => Classes.matches(e.characterClass, filter));
    link.textContent = matching.length > 0 ? `See all ${matching.length}` : 'Open ranking';
    if (matching.length === 0) {
      message(card, 'Nobody is ranked here yet.');
      return;
    }

    card.querySelector('tbody').replaceChildren(...matching.slice(0, 10).map((entry, index) => {
      const row = document.createElement('tr');
      const rank = document.createElement('td');
      rank.className = 'rank';
      rank.textContent = index + 1;
      const name = document.createElement('td');
      name.className = 'name';
      if (entry.online) {
        const dot = document.createElement('span');
        dot.className = 'dot online';
        dot.title = 'Online now';
        name.append(dot);
      }
      name.append(entry.name);
      const cls = document.createElement('small');
      cls.textContent = Classes.short(entry.characterClass);
      cls.title = entry.characterClass;
      name.append(cls);
      row.append(rank, name);
      for (const value of columns[board]) {
        const td = document.createElement('td');
        td.className = 'num';
        td.textContent = number(value(entry));
        row.append(td);
      }
      return row;
    }));
  }

  Classes.renderTabs(tabs, filter, (family) => {
    filter = family;
    cards.forEach(render);
  });

  cards.forEach(async (card) => {
    message(card, 'Loading…');
    try {
      const response = await fetch(`/api/rankings/${card.dataset.topBoard}`, { cache: 'no-store' });
      const result = await response.json();
      if (!response.ok) throw new Error(result.error);
      data[card.dataset.topBoard] = result.entries;
      render(card);
    } catch (error) {
      message(card, error.message || 'The ranking is not available right now.');
    }
  });
})();

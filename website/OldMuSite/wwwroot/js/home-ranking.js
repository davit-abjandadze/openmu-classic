'use strict';

// The "Top Players" block of the home page: the top 10 of three boards, filtered by class family.
(() => {
  const tabs = document.querySelector('[data-class-tabs]');
  const cards = [...document.querySelectorAll('[data-top-board]')];
  if (!tabs || cards.length === 0) return;

  const Classes = window.OldMuClasses;
  const { russian, t, number, page } = window.OldMuLocale;
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
    td.textContent = t(text);
    row.append(td);
    card.querySelector('tbody').replaceChildren(row);
  }

  function render(card) {
    const board = card.dataset.topBoard;
    const link = card.querySelector('.see-all');
    link.href = page(`/ranking.html#${board}${filter === 'all' ? '' : '/' + filter}`);
    const entries = data[board];
    if (!entries) return;

    const matching = entries.filter((e) => Classes.matches(e.characterClass, filter));
    link.textContent = matching.length > 0
      ? russian ? `Весь рейтинг (${number(matching.length)})` : `See all ${matching.length}`
      : t('Open ranking');
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
        dot.title = t('Online now');
        name.append(dot);
      }
      name.append(entry.name);
      const cls = document.createElement('small');
      cls.textContent = Classes.short(entry.characterClass);
      cls.title = t(entry.characterClass);
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
      if (!response.ok) throw new Error();
      data[card.dataset.topBoard] = result.entries;
      render(card);
    } catch {
      message(card, 'The ranking is not available right now.');
    }
  });
})();

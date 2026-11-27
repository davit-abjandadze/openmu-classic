'use strict';

// The ranking page: one table per board, chosen by the tabs (and the #hash, so a board can be linked).
(() => {
  const head = document.querySelector('[data-head]');
  const body = document.querySelector('[data-body]');
  const note = document.querySelector('[data-note]');
  const search = document.querySelector('[data-search]');
  const tabs = [...document.querySelectorAll('[data-board]')];
  const onlineCount = document.querySelector('[data-online-now] [data-online-count]');
  const onlineLabel = document.querySelector('[data-online-now] [data-online-label]');

  const { t, number, playerLabel } = window.OldMuLocale;

  const boards = {
    characters: {
      note: 'The best characters by resets, then level.',
      columns: [['Resets', (e) => number(e.resets)], ['Level', (e) => number(e.level)]],
    },
    'pvp-week': {
      note: 'PvP prestige of this week – kill players on the PvP spots and win duels. Resets every week.',
      columns: [['Prestige', (e) => number(e.prestige)], ['Kills', (e) => number(e.kills)], ['Deaths', (e) => number(e.deaths)]],
    },
    'pvp-all': {
      note: 'PvP prestige of all time.',
      columns: [['Prestige', (e) => number(e.prestige)], ['Kills', (e) => number(e.kills)], ['Deaths', (e) => number(e.deaths)]],
    },
    'devil-square': {
      note: 'The sum of all Devil Square scores of a character.',
      columns: [['Score', (e) => number(e.score)], ['Games', (e) => number(e.games)]],
    },
    'blood-castle': {
      note: 'The sum of all Blood Castle scores of a character.',
      columns: [['Score', (e) => number(e.score)], ['Games', (e) => number(e.games)]],
    },
  };

  const Classes = window.OldMuClasses;
  const classTabs = document.querySelector('[data-class-tabs]');
  let current = null;
  let family = 'all';
  let entries = [];
  let request = 0;

  function cell(row, text, className) {
    const td = document.createElement('td');
    if (className) td.className = className;
    td.textContent = text;
    row.append(td);
    return td;
  }

  function renderHead(board) {
    const row = document.createElement('tr');
    for (const title of ['#', 'Character', 'Class', ...boards[board].columns.map((c) => c[0])]) {
      const th = document.createElement('th');
      th.textContent = t(title);
      row.append(th);
    }
    head.replaceChildren(row);
  }

  function renderBody() {
    const columns = boards[current].columns;
    const filter = search.value.trim().toLowerCase();
    const shown = entries
      .filter((e) => Classes.matches(e.characterClass, family))
      .filter((e) => !filter || e.name.toLowerCase().includes(filter));
    if (shown.length === 0) {
      const row = document.createElement('tr');
      const td = cell(row, t(filter || family !== 'all' ? 'No character found.' : 'Nobody is ranked here yet.'), 'empty');
      td.colSpan = 3 + columns.length;
      body.replaceChildren(row);
      return;
    }

    // With a class filter, the places are counted within the class.
    body.replaceChildren(...shown.map((entry, index) => {
      const place = family === 'all' ? entry.rank : index + 1;
      const row = document.createElement('tr');
      if (place <= 3) row.className = `top top-${place}`;
      cell(row, place, 'rank');
      const name = cell(row, '', 'name');
      const dot = document.createElement('span');
      dot.className = entry.online ? 'dot online' : 'dot';
      dot.title = t(entry.online ? 'Online now' : 'Offline');
      name.append(dot, entry.name);
      cell(row, t(entry.characterClass), 'class');
      for (const [, value] of columns) cell(row, value(entry), 'num');
      return row;
    }));
  }

  function showOnline(players, serverOnline) {
    onlineCount.textContent = serverOnline ? number(players) : '0';
    onlineLabel.textContent = serverOnline ? playerLabel(players) : t('server offline');
    onlineCount.parentElement.classList.toggle('offline', !serverOnline);
  }

  async function load(board) {
    if (!boards[board]) board = 'characters';
    current = board;
    tabs.forEach((tab) => tab.setAttribute('aria-selected', String(tab.dataset.board === board)));
    note.textContent = t(boards[board].note);
    renderHead(board);
    const row = document.createElement('tr');
    cell(row, t('Loading…'), 'empty').colSpan = 3 + boards[board].columns.length;
    body.replaceChildren(row);

    const id = ++request;
    try {
      const response = await fetch(`/api/rankings/${board}`, { cache: 'no-store' });
      const result = await response.json();
      if (id !== request) return; // another tab was chosen meanwhile
      if (!response.ok) throw new Error();
      entries = result.entries;
      showOnline(result.players, result.serverOnline);
      renderBody();
    } catch {
      if (id !== request) return;
      entries = [];
      const failed = document.createElement('tr');
      cell(failed, t('The ranking is not available right now.'), 'empty').colSpan = 3 + boards[board].columns.length;
      body.replaceChildren(failed);
    }
  }

  // The hash is "#board" or "#board/family", e.g. #pvp-week/elf.
  const updateHash = () => history.replaceState(null, '', `#${current}${family === 'all' ? '' : '/' + family}`);

  function fromHash() {
    const [board, wanted] = location.hash.slice(1).split('/');
    family = Classes.isFamily(wanted) ? wanted : 'all';
    Classes.renderTabs(classTabs, family, (chosen) => {
      family = chosen;
      updateHash();
      if (current) renderBody();
    });
    load(board);
  }

  tabs.forEach((tab) => tab.addEventListener('click', () => {
    load(tab.dataset.board);
    updateHash();
  }));
  search.addEventListener('input', () => { if (current) renderBody(); });
  window.addEventListener('hashchange', fromHash);

  fromHash();
})();

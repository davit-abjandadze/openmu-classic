'use strict';

// The class families of the server and the short names of their classes, used by the class
// filters of the rankings. Classes of other families (left in the test data) only show under "All".
window.OldMuClasses = (() => {
  const classes = {
    'Dark Wizard': ['dw', 'DW'], 'Soul Master': ['dw', 'SM'], 'Grand Master': ['dw', 'GM'],
    'Dark Knight': ['dk', 'DK'], 'Blade Knight': ['dk', 'BK'], 'Blade Master': ['dk', 'BM'],
    'Fairy Elf': ['elf', 'FE'], 'Muse Elf': ['elf', 'ME'], 'High Elf': ['elf', 'HE'],
    'Magic Gladiator': ['mg', 'MG'], 'Duel Master': ['mg', 'DM'],
  };

  const families = [
    ['all', 'All'], ['dw', 'Dark Wizard'], ['dk', 'Dark Knight'], ['elf', 'Fairy Elf'], ['mg', 'Magic Gladiator'],
  ];

  const family = (name) => (classes[name] || [null])[0];

  // Short name for the table: the known abbreviation, or the initials of an unknown class.
  const short = (name) => (classes[name] || [null, (name || '').split(' ').map((w) => w[0]).join('')])[1];

  const matches = (name, filter) => filter === 'all' || family(name) === filter;

  // Builds the filter buttons into the container and calls onChange with the chosen family.
  function renderTabs(container, selected, onChange) {
    container.replaceChildren(...families.map(([key, label]) => {
      const button = document.createElement('button');
      button.type = 'button';
      button.textContent = label;
      button.dataset.family = key;
      button.setAttribute('aria-pressed', String(key === selected));
      button.addEventListener('click', () => {
        container.querySelectorAll('button').forEach((b) => b.setAttribute('aria-pressed', String(b === button)));
        onChange(key);
      });
      return button;
    }));
  }

  const isFamily = (key) => families.some(([k]) => k === key);

  return { family, short, matches, renderTabs, isFamily };
})();

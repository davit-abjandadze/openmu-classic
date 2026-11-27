'use strict';

// Shared by all pages: mobile menu, server status, download/discord links, next event.
(() => {
  const { russian, t, number, playerLabel } = window.OldMuLocale;
  const languageLink = document.querySelector('[data-language-link]');
  const alternatePage = languageLink.getAttribute('href');
  const syncLanguageLink = () => {
    languageLink.href = alternatePage + location.search + location.hash;
  };
  syncLanguageLink();
  window.addEventListener('hashchange', syncLanguageLink);
  window.addEventListener('popstate', syncLanguageLink);
  languageLink.addEventListener('click', syncLanguageLink);

  const toggle = document.querySelector('.menu-toggle');
  const nav = document.querySelector('.nav');
  if (toggle && nav) {
    toggle.addEventListener('click', () => {
      const open = nav.classList.toggle('open');
      toggle.setAttribute('aria-expanded', String(open));
    });
    nav.addEventListener('click', (e) => {
      if (e.target.closest('a')) nav.classList.remove('open');
    });
  }

  async function loadStatus() {
    try {
      const response = await fetch('/api/status', { cache: 'no-store' });
      const status = await response.json();
      document.querySelectorAll('[data-status]').forEach((pill) => {
        pill.classList.toggle('online', status.online);
        pill.classList.toggle('offline', !status.online);
        const count = number(status.players);
        pill.querySelector('[data-status-text]').textContent = status.online
          ? russian
            ? `${count} ${playerLabel(status.players)}`
            : `Online · ${count} ${status.players === 1 ? 'player' : 'players'}`
          : t('Offline');
      });
      document.querySelectorAll('[data-online-badge]').forEach((badge) => {
        badge.hidden = !status.online;
        badge.querySelector('[data-online-count]').textContent = number(status.players);
        badge.querySelector('[data-online-label]').textContent = playerLabel(status.players);
      });
      if (status.downloadUrl) {
        document.querySelectorAll('[data-download]').forEach((link) => {
          link.href = status.downloadUrl;
          link.rel = 'noopener';
        });
      } else {
        document.querySelectorAll('[data-download-label]').forEach((link) => {
          link.textContent = t('Coming soon');
          link.removeAttribute('href');
        });
      }
      if (status.discordUrl) {
        document.querySelectorAll('[data-discord]').forEach((el) => {
          el.hidden = false;
          el.querySelector('a').href = status.discordUrl;
        });
      }
    } catch {
      document.querySelectorAll('[data-status-text]').forEach((el) => { el.textContent = t('Status unknown'); });
    }
  }

  // Minutes since midnight in Tbilisi, whatever the visitor's own time zone is.
  function tbilisiMinutes() {
    const parts = new Intl.DateTimeFormat('en-GB', { timeZone: 'Asia/Tbilisi', hour: '2-digit', minute: '2-digit', hourCycle: 'h23' })
      .formatToParts(new Date());
    const get = (type) => Number(parts.find((p) => p.type === type).value);
    return get('hour') * 60 + get('minute');
  }

  function showNextEvent() {
    const target = document.querySelector('[data-next-event]');
    if (!target) return;
    const now = tbilisiMinutes();
    let best = null;
    document.querySelectorAll('[data-times]').forEach((cell) => {
      const name = cell.parentElement.firstElementChild.textContent;
      cell.dataset.times.split(',').forEach((time) => {
        const [h, m] = time.split(':').map(Number);
        let wait = h * 60 + m - now;
        if (wait < 0) wait += 24 * 60;
        if (!best || wait < best.wait) best = { name, time, wait };
      });
    });
    if (!best) return;
    const hours = Math.floor(best.wait / 60);
    const minutes = best.wait % 60;
    const left = russian
      ? best.wait === 0 ? 'сейчас' : `через ${hours ? hours + ' ч ' : ''}${minutes} мин`
      : best.wait === 0 ? 'now' : `in ${hours ? hours + ' h ' : ''}${minutes} min`;
    target.innerHTML = '';
    target.append(russian ? 'Далее: ' : 'Next: ');
    const b = document.createElement('b');
    b.textContent = best.name;
    target.append(b, russian ? ` в ${best.time} · ${left}` : ` at ${best.time} – ${left}`);
  }

  loadStatus();
  showNextEvent();
  setInterval(showNextEvent, 30000);
  setInterval(loadStatus, 60000);
})();

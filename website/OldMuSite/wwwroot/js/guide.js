'use strict';

// Fills the current settings of the item exchange into the guide, e.g. the number of items to give in.
(async () => {
  try {
    const response = await fetch('/api/guide/exchange');
    if (!response.ok) return;
    const exchange = await response.json();
    document.querySelectorAll('[data-exchange]').forEach((el) => {
      const value = exchange[el.dataset.exchange];
      if (value !== undefined) el.textContent = value;
    });
  } catch {
    // The defaults in the page stay.
  }
})();

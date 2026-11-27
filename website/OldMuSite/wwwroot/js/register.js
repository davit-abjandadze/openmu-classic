'use strict';

// The registration form. A security check (proof of work) is solved in a worker as soon as the
// page opens, so it's usually done before the visitor has typed the password.
(() => {
  const { t, registrationError } = window.OldMuLocale;
  const form = document.getElementById('register-form');
  const message = form.querySelector('[data-message]');
  const submit = form.querySelector('[data-submit]');
  const security = form.querySelector('[data-security]');
  const securityText = form.querySelector('[data-security-text]');
  const success = document.querySelector('[data-success]');

  const rules = {
    login: (v) => /^[A-Za-z0-9]{4,10}$/.test(v.trim()) ? null : 'The login must have 4 to 10 letters (a–z) or digits.',
    password: (v) => {
      if (!/^[\x21-\x7E]{6,20}$/.test(v)) return 'The password must have 6 to 20 characters (latin letters, digits, symbols), without spaces.';
      if (v.toLowerCase() === form.login.value.trim().toLowerCase()) return 'The password must not be the same as the login.';
      return null;
    },
    repeat: (v) => v === form.password.value ? null : "The passwords don't match.",
  };

  let challenge = null; // { token, nonce } once solved
  let solving = null;   // promise of the running solution
  let worker = null;

  function setSecurity(state, text) {
    security.classList.remove('ready', 'failed');
    if (state) security.classList.add(state);
    securityText.textContent = t(text);
  }

  function solveChallenge() {
    challenge = null;
    if (worker) worker.terminate();
    setSecurity(null, 'Preparing the security check…');
    solving = (async () => {
      const response = await fetch('/api/register/challenge', { cache: 'no-store' });
      if (!response.ok) {
        const result = await response.json().catch(() => ({}));
        throw new Error(result.error || 'The security check could not be loaded.');
      }
      const { token, bits } = await response.json();
      const salt = token.split('.')[0];
      const nonce = await new Promise((resolve, reject) => {
        worker = new Worker('/js/pow-worker.js');
        worker.onmessage = (e) => { worker.terminate(); worker = null; resolve(e.data.nonce); };
        worker.onerror = () => reject(new Error('Your browser could not run the security check.'));
        worker.postMessage({ salt, bits });
      });
      challenge = { token, nonce };
      setSecurity('ready', 'Security check passed');
      return challenge;
    })();
    solving.catch((error) => setSecurity('failed', registrationError(error.message, 'The security check could not be loaded.')));
    return solving;
  }

  function showError(text) {
    message.textContent = text;
    message.className = 'form-message error';
  }

  function clearError() {
    message.textContent = '';
    message.className = 'form-message';
  }

  function markField(name, error) {
    const field = form.querySelector(`[data-field="${name}"]`);
    if (!field) return;
    const hint = field.querySelector('.hint');
    field.classList.toggle('invalid', !!error);
    hint.textContent = error ? t(error) : hint.dataset.hint;
  }

  function validate() {
    let first = null;
    for (const [name, rule] of Object.entries(rules)) {
      const error = rule(form[name].value);
      markField(name, error);
      if (error && !first) first = name;
    }
    return first;
  }

  // Validate a field when leaving it, and again while typing once it was marked invalid.
  for (const name of Object.keys(rules)) {
    const input = form[name];
    input.addEventListener('blur', () => { if (input.value) markField(name, rules[name](input.value)); });
    input.addEventListener('input', () => {
      if (input.closest('.field').classList.contains('invalid')) markField(name, rules[name](input.value));
    });
  }

  form.addEventListener('submit', async (event) => {
    event.preventDefault();
    clearError();
    const invalid = validate();
    if (invalid) {
      form[invalid].focus();
      return;
    }

    submit.disabled = true;
    submit.textContent = t('Please wait…');
    try {
      const solved = challenge || await (solving || solveChallenge()).catch(() => solveChallenge());
      const response = await fetch('/api/register', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          login: form.login.value.trim(),
          password: form.password.value,
          passwordRepeat: form.repeat.value,
          website: form.website.value,
          token: solved.token,
          nonce: solved.nonce,
        }),
      });
      const result = await response.json().catch(() => ({}));
      if (response.ok) {
        form.hidden = true;
        success.querySelector('[data-success-login]').textContent = result.login;
        success.classList.add('show');
        if (worker) worker.terminate();
        return;
      }

      const text = registrationError(result.error, 'Something went wrong. Please try again.');
      showError(text);
      if (result.field) {
        markField(result.field, text);
        form[result.field]?.focus();
      }
      solveChallenge(); // a challenge can be used only once
    } catch (error) {
      showError(registrationError(error.message, 'The server could not be reached. Please try again.'));
      solveChallenge();
    } finally {
      submit.disabled = false;
      submit.textContent = t('Create account');
    }
  });

  solveChallenge();
})();

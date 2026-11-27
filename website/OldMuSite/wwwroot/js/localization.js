'use strict';

window.OldMuLocale = (() => {
  const russian = document.documentElement.lang === 'ru';
  const locale = russian ? 'ru-RU' : 'en-US';
  const numbers = new Intl.NumberFormat(locale);
  const plurals = new Intl.PluralRules(locale);
  const translations = {
    'Checking…': 'Проверяем…',
    'Offline': 'Не в сети',
    'Online now': 'В сети',
    'server offline': 'сервер не в сети',
    'Coming soon': 'Скоро',
    'Status unknown': 'Статус неизвестен',
    'Loading…': 'Загрузка…',
    'Open ranking': 'Открыть рейтинг',
    'Nobody is ranked here yet.': 'В рейтинге пока никого нет.',
    'No character found.': 'Персонаж не найден.',
    'The ranking is not available right now.': 'Рейтинг сейчас недоступен.',
    'All': 'Все',
    'Character': 'Персонаж',
    'Class': 'Класс',
    'Level': 'Уровень',
    'Resets': 'Ресеты',
    'Prestige': 'Престиж',
    'Kills': 'Убийства',
    'Deaths': 'Смерти',
    'Score': 'Очки',
    'Games': 'Игры',
    'The best characters by resets, then level.': 'Лучшие персонажи по числу ресетов, затем по уровню.',
    'PvP prestige of this week – kill players on the PvP spots and win duels. Resets every week.':
      'Престиж PvP за неделю. Сражайтесь в PvP-зонах и побеждайте в дуэлях. Рейтинг обнуляется каждую неделю.',
    'PvP prestige of all time.': 'Престиж PvP за всё время.',
    'The sum of all Devil Square scores of a character.': 'Сумма очков персонажа на Площади Дьявола.',
    'The sum of all Blood Castle scores of a character.': 'Сумма очков персонажа в Кровавом замке.',
    'Dark Wizard': 'Тёмный маг',
    'Soul Master': 'Повелитель душ',
    'Grand Master': 'Великий маг',
    'Dark Knight': 'Тёмный рыцарь',
    'Blade Knight': 'Рыцарь клинка',
    'Blade Master': 'Мастер клинка',
    'Fairy Elf': 'Эльфийка',
    'Muse Elf': 'Эльфийка-муза',
    'High Elf': 'Высшая эльфийка',
    'Magic Gladiator': 'Магический гладиатор',
    'Duel Master': 'Мастер дуэлей',
    'Sword': 'Меч',
    'Axe': 'Топор',
    'Mace': 'Булава',
    'Spear': 'Копьё',
    'Bow': 'Лук',
    'Staff': 'Посох',
    'Shield': 'Щит',
    'Helm': 'Шлем',
    'Armor': 'Броня',
    'Weapons': 'Оружие',
    'Swords': 'Мечи',
    'Axes': 'Топоры',
    'Maces': 'Булавы',
    'Spears': 'Копья',
    'Bows': 'Луки',
    'Staffs': 'Посохи',
    'Shields': 'Щиты',
    'Helms': 'Шлемы',
    'Armors': 'Броня',
    'Pants': 'Штаны',
    'Gloves': 'Перчатки',
    'Boots': 'Сапоги',
    'Strength': 'Сила',
    'Agility': 'Ловкость',
    'Vitality': 'Выносливость',
    'Energy': 'Энергия',
    'Leadership': 'Лидерство',
    'Item': 'Предмет',
    'No item for this choice.': 'По этим фильтрам предметов нет.',
    'Requires (excellent +0)': 'Требования (Excellent +0)',
    'Copy the command': 'Скопировать команду',
    'Copied!': 'Скопировано!',
    'The item exchange is switched off at the moment.': 'Обмен предметов сейчас отключён.',
    'The list is not available right now.': 'Список сейчас недоступен.',
    'The login must have 4 to 10 letters (a–z) or digits.': 'Логин должен содержать от 4 до 10 латинских букв (a–z) или цифр.',
    'The login must have 4 to 10 letters (a-z) or digits.': 'Логин должен содержать от 4 до 10 латинских букв (a–z) или цифр.',
    'The password must have 6 to 20 characters (latin letters, digits, symbols), without spaces.':
      'Пароль должен содержать от 6 до 20 символов: латинские буквы, цифры или знаки, без пробелов.',
    'The password must have 6 to 20 characters, without spaces.': 'Пароль должен содержать от 6 до 20 символов без пробелов.',
    'The password must not be the same as the login.': 'Пароль не должен совпадать с логином.',
    "The passwords don't match.": 'Пароли не совпадают.',
    'Preparing the security check…': 'Готовим проверку безопасности…',
    'The security check could not be loaded.': 'Не удалось загрузить проверку безопасности.',
    'Your browser could not run the security check.': 'Браузер не смог выполнить проверку безопасности.',
    'Security check passed': 'Проверка безопасности пройдена',
    'Please wait…': 'Подождите…',
    'Something went wrong. Please try again.': 'Произошла ошибка. Попробуйте ещё раз.',
    'The server could not be reached. Please try again.': 'Не удалось связаться с сервером. Попробуйте ещё раз.',
    'Create account': 'Создать аккаунт',
    'Too many requests. Wait a few minutes and try again.': 'Слишком много запросов. Подождите несколько минут и попробуйте снова.',
    'The security check is missing. Reload the page and try again.': 'Проверка безопасности отсутствует. Обновите страницу и попробуйте снова.',
    'The security check is invalid. Reload the page and try again.': 'Проверка безопасности недействительна. Обновите страницу и попробуйте снова.',
    'That was too fast. Wait a moment and try again.': 'Запрос отправлен слишком быстро. Немного подождите и попробуйте снова.',
    'The security check has expired. Try again.': 'Срок проверки безопасности истёк. Попробуйте ещё раз.',
    'The security check failed. Try again.': 'Проверка безопасности не пройдена. Попробуйте ещё раз.',
    'The security check was already used. Try again.': 'Эта проверка безопасности уже использована. Попробуйте ещё раз.',
    'Too many accounts were created in the last hour. Please try again later.': 'За последний час создано слишком много аккаунтов. Попробуйте позже.',
    'This login is already taken.': 'Этот логин уже занят.',
    'The server refused this login or password.': 'Сервер отклонил этот логин или пароль.',
    'Registration is not available right now. Please try again later.': 'Регистрация сейчас недоступна. Попробуйте позже.',
  };

  const t = (text) => russian && Object.hasOwn(translations, text) ? translations[text] : text;
  const number = (value) => numbers.format(value ?? 0);
  const page = (path) => russian ? '/ru' + path : path;
  const playerLabel = (count) => russian
    ? { one: 'игрок онлайн', few: 'игрока онлайн', many: 'игроков онлайн', other: 'игрока онлайн' }[plurals.select(count)]
    : count === 1 ? 'player online' : 'players online';

  function registrationError(error, fallback) {
    if (!russian) return error || fallback;
    if (Object.hasOwn(translations, error)) return t(error);

    // The registration API includes a wait in these two limit messages.
    const limit = /^You have already created (\d+) accounts today\. Try again in (.+)\.$/.exec(error);
    const cooldown = /^You have just created an account\. Try again in (.+)\.$/.exec(error);
    if (limit || cooldown) {
      const duration = (limit ? limit[2] : cooldown[1])
        .replace('a minute', '1 мин').replace(' h ', ' ч ').replace(' min', ' мин');
      return limit
        ? `Достигнут дневной лимит регистрации (${number(limit[1])}). Повторите попытку через ${duration}.`
        : `Вы только что создали аккаунт. Повторите попытку через ${duration}.`;
    }
    return t(fallback);
  }

  return { russian, t, number, page, playerLabel, registrationError };
})();

# Old MU production deployment

Runs the Old MU server (this fork, classic Season 1 content for the Season 6
client) with Docker Compose on a single Linux server:

| Container | What it does |
|---|---|
| `oldmu-server` | The server, built from `src/Startup/Dockerfile` of this repository |
| `oldmu-website` | The public website with the registration form, port 80 (`website/OldMuSite`) |
| `oldmu-database` | PostgreSQL 18, not reachable from outside |
| `oldmu-backup` | `pg_dump` every 6 hours into `./backups`, keeps 14 days |

## First start

1. Install Docker with the compose plugin on the server and clone this
   repository on the `classic-season1` branch.
2. In this folder, copy `.env.example` to `.env` and fill it in. `PUBLIC_HOST`
   is the domain or public IP which the game client connects to.
3. Open the firewall for the game ports only (example for `ufw`):

   ```bash
   ufw allow 22/tcp
   ufw allow 80/tcp
   ufw allow 44405:44406/tcp
   ufw allow 45901:45906/tcp
   ufw allow 45980/tcp
   ufw enable
   ```

   Docker publishes ports past `ufw`, so the compose file itself binds the admin
   panel to `127.0.0.1` and doesn't publish the database at all.
4. Build and start:

   ```bash
   docker compose up -d --build
   docker compose logs -f openmu-startup
   ```

   An empty database is created and initialized with the classic data on the
   first start; that takes a few minutes. Unlike a development start, it gets
   no test accounts (`CREATE_TEST_ACCOUNTS`), so create the first player
   accounts in the admin panel.

## Admin panel

The admin panel listens on `127.0.0.1:8080` of the server only. Open it through
an SSH tunnel from your own computer:

```bash
ssh -L 8080:127.0.0.1:8080 user@your-server
```

and then browse to <http://localhost:8080>. Log in with `OPENMU_ADMIN_USER` /
`OPENMU_ADMIN_PASSWORD`.

## Website and registration

The website runs on port 80 (`WEBSITE_PORT`). Registration asks for a login and a password
only; it creates the account through the server's API (`POST /api/accounts`) with an API key:

1. Log in to the admin panel (`OPENMU_ADMIN_USER`, see below), open the **API keys** page
   (`/api-keys`) and create a key for the application "website" with the role **Registration**
   ("Account registration (and viewer)"). It can create accounts and read the status and the
   rankings, but not change, ban or disconnect accounts like an Operator key, so a broken website
   can't do more than that.
2. Put the key into `.env` as `WEBSITE_API_KEY`, set `DOWNLOAD_URL` (and `DISCORD_URL`), and
   restart the site: `docker compose up -d website`.

Without the key the site works, but registration answers "not available right now".

Protection against mass registration, all in the website (`website/OldMuSite/appsettings.json`,
overridable with `Site__…` variables):

* the browser has to solve a proof of work first (`ProofOfWorkBits`, 21 = about a second on a
  PC); a solved check can be used once, not sooner than 3 seconds and not later than 20 minutes
  after the page got it,
* one IP address (an IPv6 /64) may create 3 accounts per 24 hours (`AccountsPerIpPerDay`) and
  has to wait 10 minutes between two (`MinutesBetweenAccountsPerIp`),
* all visitors together may create 40 accounts per hour (`AccountsPerHourTotal`),
* at most 10 form submissions per IP in 10 minutes, and a hidden field that only bots fill in.

The limits are kept in the `website-data` volume, so a restart doesn't reset them. The game
server additionally lets only 2 accounts per IP be online.

For HTTPS put a reverse proxy like Caddy in front, publish the site only to it
(`WEBSITE_PORT=127.0.0.1:8081`) and set `WEBSITE_BEHIND_PROXY=true`, otherwise every visitor
gets the proxy's address and shares its limits.

## Updating

```bash
git pull
docker compose up -d --build
```

Mandatory data updates are applied automatically at the start. Take a backup
first (see below) — a failed update can't be undone otherwise.

## Backups

Dumps land in `./backups` as `openmu-<utc time>.dump`. Copy them to another
machine regularly — a backup on the same disk doesn't help when the server is
lost.

Take one right now:

```bash
docker compose exec backup sh -c 'pg_dump -Fc -d openmu -f /backups/openmu-manual.dump'
```

Restore one (stops the server, replaces the whole database):

```bash
docker compose stop openmu-startup
docker compose exec backup sh -c 'dropdb --force openmu && createdb openmu && pg_restore -d openmu /backups/openmu-<time>.dump'
docker compose start openmu-startup
```

## Launcher

Set `Host` in the launcher's `launcher.json` to `PUBLIC_HOST` and keep
`Port` 44406.

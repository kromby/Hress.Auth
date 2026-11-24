# Hress.Auth

Authentication service for Hress.Org and other Hress apps: hosted login, OAuth 2.0 authorization code + PKCE, and token issuing. Moved out of [`kromby/Hress.Org`](https://github.com/kromby/Hress.Org) (see kromby/Hress.Org#780).

> Status: password login and the legacy magic-code exchange are in (kromby/Hress.Org#783). The OAuth flow and hosted pages arrive in kromby/Hress.Org#784–#791.

## API

| Endpoint | Caller | What it does |
|---|---|---|
| `GET /api/health` | anyone | Liveness |
| `POST /api/login` `{ username, password }` (JSON) | the auth app's own login page only (same origin) | Checks the password and sets the `__Host-hress-session` cookie (HttpOnly, Secure, SameSite=Lax). Returns 204, **no token**. 401 on wrong credentials, 403 cross-site, 415 if not JSON |
| `POST /api/authenticate` `{ code }` | Hress.Org SPA `/login/magic` (**legacy**) | Exchanges a one-use magic code from the old site for an access token. Not for new apps |

Access tokens: HS256 JWT, `sub` = user id, 7 days, same format Hress.Org issued.

## Layout

```
api/
├── Ez.Hress.Auth/                # Domain: Entities/, UseCases/, DataAccess/ (see ARCHITECTURE_RULES.md)
├── Ez.Hress.Auth.FunctionsApi/   # Azure Functions (.NET 10 isolated), served as the SWA managed API under /api
└── Ez.Hress.Auth.UnitTest/       # xUnit + Moq
web/                              # Hosted pages (login, change password, log out) + staticwebapp.config.json
```

## Hosting

One Azure Static Web App (Free plan) serves the pages and the managed Functions API on the same origin, so the login session cookie and the pages share one address. Other apps call the API from the browser (e.g. `POST /api/token`). CORS for that is handled in code by `CorsMiddleware`, because the SWA managed API has no CORS setting. Any HTTP trigger called from another site must accept `options`.

| Resource | Name |
|---|---|
| Static Web App | `ez-stapp-hressauth` (ez-rg-hressorg, West Europe) |
| Storage (OAuth codes, Table Storage) | `ezsthressauth` |
| Application Insights | `ez-appi-hressauth` |

Same SQL database as Hress.Org (`adm_User`, `adm_MagicCode`).

## Settings

App settings on the Static Web App (Environment variables). Local: copy `api/Ez.Hress.Auth.FunctionsApi/local.settings.example.json` to `local.settings.json` (git-ignored).

| Setting | Purpose |
|---|---|
| `Ez.Hress.Auth.AllowedOrigins` | Comma-separated origins allowed to call the API from a browser, matched exactly |
| `Ez.Hress.Auth.Storage.ConnectionString` | Table Storage for OAuth authorization codes |
| `Ez.Hress.Database.ConnectionString` | Hress SQL database |
| `APPLICATIONINSIGHTS_CONNECTION_STRING` | Telemetry |
| `Ez.Hress.Auth.{Key,Issuer,Audience}` | Token signing (#783). New key, so tokens from Hress.Org stop working at switch-over |
| `Ez.Hress.Auth.Salt` | Password hashing salt. **Must equal Hress.Org's current salt**, or existing passwords stop matching |
| `Ez.Hress.Auth.SessionKey` | Signs the auth app's own login session cookie (`__Host-hress-session`). Separate from the token key |
| `Ez.Hress.Auth.SessionDays` | Session lifetime in days (default 30) |
| `Ez.Hress.Auth.Clients` | OAuth client registry (#791) |

Secrets never go in the repo.

## Build and run

```bash
cd api
dotnet build Ez.Hress.Auth.sln
dotnet test Ez.Hress.Auth.sln
cd Ez.Hress.Auth.FunctionsApi && func start --port 7073
curl -i http://localhost:7073/api/health -H "Origin: http://localhost:3000"
```

To run pages and API together like production: `swa start web --api-location api/Ez.Hress.Auth.FunctionsApi`.

## CI/CD

`.github/workflows/ci-cd.yml` builds and tests on every push and PR. Pushes to `main` deploy to the Static Web App, using the `AZURE_STATIC_WEB_APPS_API_TOKEN` repo secret.

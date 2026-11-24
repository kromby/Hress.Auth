# CLAUDE.md

Authentication service for the Hress platform. See `README.md` for layout, hosting and settings, and `ARCHITECTURE_RULES.md` for code conventions (same as Hress.Org: Entities / UseCases / DataAccess, interactors without interfaces, data access behind `I{Entity}DataAccess`, singletons in `Program.cs`, `[{Class}.{Method}]` logging).

- Build/test: `cd api && dotnet build Ez.Hress.Auth.sln && dotnet test Ez.Hress.Auth.sln`
- Run: `cd api/Ez.Hress.Auth.FunctionsApi && func start --port 7073`
- HTTP triggers are `AuthorizationLevel.Anonymous`. The SWA managed API does not enforce function keys.
- Triggers called by other sites from the browser must accept `options` (CORS is handled by `CorsMiddleware`).
- Never log passwords, codes, tokens or code verifiers.
- Backlog: kromby/Hress.Org#780 and its sub-issues.

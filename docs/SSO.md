# Keycloak SSO setup

This guide configures Keycloak for local development of Task Tracker. The API is an OpenID Connect confidential client: it redirects the browser to Keycloak using authorization code flow with PKCE, then creates an HttpOnly application session cookie. Keycloak does not issue a token directly to Angular. Every task endpoint requires a signed-in session, but tasks are shared by all signed-in users rather than owned by individual accounts.

## Local URLs and prerequisites

The development setup uses these addresses:

| Service | URL |
| --- | --- |
| Keycloak | `http://localhost:8080` |
| Keycloak realm | `TaskTracker-realm` |
| API | `http://localhost:5080` |
| Angular app | `http://localhost:4200` |

Use `localhost` consistently in the browser and Keycloak configuration. Do not mix it with `127.0.0.1`: OIDC issuer and redirect URI validation compare these hostnames literally. You need Docker Desktop or another running Keycloak instance, plus the .NET and Node.js prerequisites in the repository [README](../README.md).

### Start Keycloak with Compose

Follow step 1 in the [README](../README.md) to create `.env` and run `docker compose up -d --wait` from the repository root. The Compose service publishes Keycloak only on `127.0.0.1:8080` and persists its data in the `tasktracker-keycloak-data` Docker volume.

Open `http://localhost:8080/admin/` and sign in with username `admin` and the `KEYCLOAK_ADMIN_PASSWORD` from `.env` on first initialization. That bootstrap password does not change an admin account in an existing volume. Use the credentials already stored in that Keycloak instance. Keep the browser hostname as `localhost`; do not switch between `localhost` and `127.0.0.1` because OIDC issuer and redirect validation compare hostnames literally. To inspect startup logs, run `docker compose logs -f keycloak`; stop and restart the service with `docker compose stop keycloak` and `docker compose up -d keycloak`.

Use one Keycloak instance on port 8080. If you also have a manually started container publishing that port, stop it before starting Compose. A manually started container may use a different data volume and admin password; realms created in one instance do not appear in the other. `start-dev` is only for local development; do not expose it to untrusted networks or use it in production.

## Create the realm

1. In the Admin Console, open the realm selector in the upper-left corner and choose **Create realm**.
2. Set **Realm name** to `TaskTracker-realm` and create it. Realm names are case-sensitive and form part of the issuer URL.
3. Confirm the realm is selected before creating the client or user.


## Create the API client

In the `TaskTracker-realm` realm, create an OpenID Connect client with these settings. Keycloak's form labels can vary slightly between releases.

1. Open **Clients**, choose **Create client**, select **OpenID Connect**, and set **Client ID** to `TaskTracker-backend`.
2. Keep **Client authentication** enabled. This makes it a confidential server-side client; Angular must never receive its secret.
3. Enable **Standard flow** (authorization code flow). Leave **Direct access grants** and **Implicit flow** disabled; this application does not use them.
4. Set the client's **Valid redirect URIs** to the exact URI `http://localhost:5080/signin-oidc`.
5. Set **Valid post logout redirect URIs** to `http://localhost:5080/signout-callback-oidc`.
6. Save the client. In its **Credentials** tab, copy the generated **Client secret**; the API needs this server-side secret to authenticate the client.

Do not use a wildcard redirect URI. The Angular development proxy forwards `/api`, `/signin-oidc`, `/signout-callback-oidc`, and `/signout-oidc` to the API, which runs on port 5080. The OIDC callback is therefore registered against the API port, not Angular's port 4200. Web origins / CORS are not needed for this server-side flow.

## Create a Task Tracker user

1. With `TaskTracker-realm` selected, open **Users** and choose **Create new user**.
2. Enter a username and create the user. Email and other profile fields are optional for this app.
3. Open the user's **Credentials** tab, set a password, and turn **Temporary** off if you want to sign in without a forced password change.
4. Save the password and use this account to test the application. Do not use the Keycloak administrator account as the application user.

The app uses the `preferred_username` claim as the displayed user name. Keycloak's standard `profile` client scope normally supplies it. If the app signs in but shows no user name, check that the `profile` scope is assigned to the client and that the user's username is set.

## Configure the API secret

The repository's `appsettings.json` already sets the local authority to `http://localhost:8080/realms/TaskTracker-realm` and the client ID to `TaskTracker-backend`. Store the client secret in .NET user-secrets from the repository root:

```powershell
dotnet user-secrets set "Keycloak:ClientSecret" "<client secret from Keycloak>" --project backend/TaskTracker.Api
```

Replace the placeholder with the secret copied from the client's **Credentials** tab. The API also needs the PostgreSQL connection string configured as described in the [README](../README.md). User-secrets are loaded by the API in Development and stored outside the repository. Do not commit the client secret, put it in `.env`, or put it in Angular configuration. `dotnet user-secrets list` prints secret values; avoid running it where output may be recorded or shared.

For a different Keycloak URL or client ID, configure `Keycloak:Authority` and `Keycloak:ClientId` as user-secrets too. In another environment, supply `Keycloak__Authority`, `Keycloak__ClientId`, and `Keycloak__ClientSecret` through the deployment's secret manager. The root `.env` file is read by Docker Compose; .NET does not load it automatically.

## Run and verify

After creating the realm, client, user, and API client secret, start the API from the repository root:

```powershell
dotnet run --project backend/TaskTracker.Api
```

In a second terminal, start Angular:

```powershell
cd frontend
npm ci
npm start
```

Open `http://localhost:4200` and choose **Sign in with Keycloak**. You should be redirected to Keycloak, then returned to the tasks page after signing in. Sign in with the application user created in `TaskTracker-realm`, not the Keycloak Admin Console account. Create a task and refresh to verify the session and shared task list still work. Sign out and confirm you return to the app.

Useful checks:

- `GET http://localhost:5080/api/auth/me` returns `authenticated: false` before sign-in, and `authenticated: true` with the signed-in name afterward. In the browser it also sets the cookies needed for antiforgery validation.
- A signed-out request to `GET http://localhost:5080/api/tasks` returns `401`; API requests do not redirect to the Keycloak login page.
- After sign-in, `GET /api/tasks` succeeds. Browser writes include the antiforgery token automatically through Angular's XSRF support.
- The API's OIDC callbacks are `http://localhost:5080/signin-oidc` and `http://localhost:5080/signout-callback-oidc` for this local HTTP setup.

## Troubleshooting

| Symptom | Check |
| --- | --- |
| Admin Console rejects `admin` / password from `.env` | The bootstrap password is applied only when Keycloak first initializes an empty data volume. Use the existing admin password for a persisted volume; changing `.env` does not reset it. Also confirm the browser is on the Compose instance at `localhost:8080`, not a different Keycloak container. |
| Discovery URL returns 404 | Create `TaskTracker-realm` in the Keycloak instance currently bound to port 8080. Check capitalization and the URL above. |
| `invalid_request` or `Authentication failed` during login | Confirm `TaskTracker-backend` exists in `TaskTracker-realm`, not only in `master`; the client ID must match exactly and **Client authentication** and **Standard flow** must be enabled. Keycloak event logs can identify a missing client. |
| `invalid_redirect_uri` in Keycloak | The client has the exact `http://localhost:5080/signin-oidc` redirect URI. Check hostname, scheme, port, path, and spelling. |
| Issuer or authority validation error | `Keycloak:Authority` exactly matches the realm issuer: `http://localhost:8080/realms/TaskTracker-realm`. Confirm the discovery URL above works. |
| `invalid_client` or client authentication failure | The client ID is `TaskTracker-backend`, client authentication is enabled, and the API secret is current. If the client secret was regenerated, update user secrets and restart the API. |
| Sign-in returns to the app with an auth error | Check the API output for the OIDC error, ensure Standard flow is enabled, verify the realm user has a usable password, and check that the API can reach Keycloak at the configured authority. |
| Logout reports an invalid redirect URI | Register the exact `http://localhost:5080/signout-callback-oidc` post logout URI on the client. |
| Sign-in works but the name is blank | Check the user's username and that the `profile` client scope provides `preferred_username`. |
| Browser cannot reach the callback | Keep Keycloak and the browser on the same `localhost` naming scheme, ensure the API is listening on port 5080, and check the Angular proxy configuration if it changed. |

After changing the client secret or API settings, restart the API. Restart Angular after changing `frontend/proxy.conf.json`.

## Deployment notes

Use HTTPS for the application and Keycloak outside local Development. Register the externally reachable HTTPS callback and post logout callback URIs with Keycloak, and configure the matching HTTPS issuer, client ID, client secret, and `Frontend:Origin`. The frontend origin defaults to `http://localhost:4200`; it is also the only origin accepted for post-login return URLs, so set it explicitly for deployment.

Deploy Angular and the API behind the same public origin, and route the OIDC callback paths (`/signin-oidc`, `/signout-callback-oidc`, and `/signout-oidc`) to the API. Ensure the reverse proxy preserves the external HTTPS scheme when forwarding requests. Use a production Keycloak deployment, a narrowly scoped client, and a secret manager; never use `start-dev` or commit client credentials.

`GET /api/auth/me` issues an antiforgery token and sets `XSRF-TOKEN`. Angular sends `X-XSRF-TOKEN` for relative mutation requests. Logout is a CSRF-protected POST that clears the application cookie and redirects through Keycloak logout. API tools must retain the session and antiforgery cookies and send the token on POST, PUT, and DELETE; start the session in a browser first.

References: [ASP.NET Core OpenID Connect configuration](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/configure-oidc-web-authentication) and [Angular XSRF protection](https://angular.dev/best-practices/security).



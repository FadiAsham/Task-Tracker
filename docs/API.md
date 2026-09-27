# Task Tracker API reference

Base URL: `http://localhost:5080`. Interactive documentation: [API Explorer](http://localhost:5080/docs). OpenAPI JSON: [v1 contract](http://localhost:5080/openapi/v1.json).

Start PostgreSQL, Keycloak, and the API using the [setup guide](../README.md). Both documentation routes are available only in Development, which is selected by the project's default launch profile. Task endpoints require the Keycloak-backed application session; signed-out requests return 401. The task list is shared among signed-in users. See [SSO setup](SSO.md) for sign-in and the antiforgery token required for writes.

## Endpoints

| Method | Path | Success | Other responses |
| --- | --- | --- | --- |
| GET | `/api/tasks` | 200, paginated tasks | 400, 500 |
| GET | `/api/tasks/{id}` | 200, task | 404, 500 |
| POST | `/api/tasks` | 201, task with Location header | 400, 500 |
| PUT | `/api/tasks/{id}` | 200, updated task | 400, 404, 409, 500 |
| DELETE | `/api/tasks/{id}` | 204, no body | 404, 500 |

IDs are UUIDs. Malformed UUID paths do not match the route and return 404. Request bodies use `Content-Type: application/json`; unsupported body media types can return 415.

## Create a task

`POST /api/tasks`

```json
{
  "title": "Plan the next release",
  "description": "Demonstrate persistence, validation, and CRUD.",
  "status": "Todo",
  "priority": "High",
  "dueDate": "2026-10-01"
}
```

| Field | Rules |
| --- | --- |
| title | Required; trimmed; 1-150 characters after trimming |
| description | Optional/null; at most 2000 characters |
| status | Todo, InProgress, Done; defaults to Todo when omitted |
| priority | Low, Medium, High; defaults to Medium when omitted |
| dueDate | Optional/null; YYYY-MM-DD; past dates allowed |

Enums must be JSON strings. The server generates the UUID and UTC timestamps. Example 201 response:

```json
{
  "id": "bf1361f9-e358-4357-88c9-3a1a795e3e66",
  "title": "Plan the next release",
  "description": "Demonstrate persistence, validation, and CRUD.",
  "status": "Todo",
  "priority": "High",
  "dueDate": "2026-10-01",
  "createdAt": "2026-09-26T12:00:00+00:00",
  "updatedAt": "2026-09-26T12:00:00+00:00"
}
```

The `Location` response header points to `/api/tasks/{id}`. Example IDs and timestamps here are illustrative; use the ID returned by your own request.

## List and retrieve

`GET /api/tasks?page=1&pageSize=20&status=Todo`

- `page`: optional, defaults to 1; must be a positive 32-bit integer.
- `pageSize`: optional, defaults to 20; valid range 1-100. Values above 100 are rejected, not clamped.
- `status`: optional status filter; use Todo, InProgress, or Done.
- Ordering: createdAt descending, then id ascending. Total count reflects the filter. Beyond-last-page requests return an empty items array.

Response shape (`items` contains task objects with the fields shown above):

```json
{
  "items": [],
  "totalCount": 0,
  "page": 1,
  "pageSize": 20
}
```

`GET /api/tasks/{id}` returns a single task object or 404 if it does not exist.

## Update

`PUT /api/tasks/{id}` accepts the same body as creation and returns the updated task. Send every editable field you want to retain: this is replacement, not a partial PATCH. Omitted description/dueDate become null; omitted status/priority reset to their defaults. ID and createdAt remain unchanged; updatedAt advances to the server's current UTC time at microsecond precision.

Concurrent edits use last-write-wins. If a task is removed while an update is being saved, the response can be 409; refresh before retrying.

## Delete

`DELETE /api/tasks/{id}` permanently removes the task and returns 204 without a response body. Repeating the deletion returns 404. The API itself does not prompt for confirmation; the Angular UI provides that step.

## Errors

Errors use Problem Details (`application/problem+json`). Validation errors include an `errors` dictionary; field keys and messages depend on the invalid input. An illustrative response:

```json
{
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "Title": ["The Title field is required."]
  },
  "traceId": "request-specific-trace-id"
}
```

500 responses contain a generic message and trace ID without exposing exception details. Check API logs using the trace ID when investigating a failure.

## CRUD requests with an authenticated session

```powershell
$baseUrl = "http://localhost:5080/api/tasks"
$body = @{ title = "Explore the API"; status = "Todo"; priority = "Medium" } | ConvertTo-Json
$task = Invoke-RestMethod $baseUrl -Method Post -ContentType "application/json" -Body $body
Invoke-RestMethod "$baseUrl/$($task.id)"
Invoke-RestMethod "$($baseUrl)?page=1&pageSize=20&status=Todo"
$update = @{ title = "Explore the API"; status = "Done"; priority = "High" } | ConvertTo-Json
Invoke-RestMethod "$baseUrl/$($task.id)" -Method Put -ContentType "application/json" -Body $update
Invoke-RestMethod "$baseUrl/$($task.id)" -Method Delete
```

These task requests require an authenticated session cookie; POST, PUT, and DELETE also require the antiforgery token returned by `GET /api/auth/me`. The snippet above is therefore illustrative and will return 401 if run without first establishing a session and adding the required cookies and token. The generated OpenAPI contract can also be imported into Postman, but an API client must retain the browser-established session cookies and send the token on writes. Interactive UI integration uses [Scalar.AspNetCore](https://scalar.com/products/api-references/integrations/aspnetcore/integration); the contract continues to be generated by ASP.NET Core's built-in OpenAPI support.

## Authentication

`GET /api/auth/login` starts browser sign-in with Keycloak. Use browser navigation, not an AJAX request. `GET /api/auth/me` is available signed in or out and returns the current session plus the antiforgery token; it also sets the required cookies. Task endpoints require a signed-in cookie session. Unsigned requests return 401. POST, PUT and DELETE require the token in the `X-XSRF-TOKEN` header (Angular sends it automatically for relative API requests). `POST /api/auth/logout` also requires authentication and the antiforgery token. See [SSO setup](SSO.md) for the complete flow and API tool requirements.

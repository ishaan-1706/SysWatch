# API Reference (Planned)

**Status**: REST API planned for Phase 2

## Endpoints (Coming Soon)

### Notifications

```
GET /api/notifications?page=1&pageSize=50&severity=warning&since=2025-01-01
```

Get paginated list of notifications.

**Query Parameters**:
- `page`: Page number (default 1)
- `pageSize`: Results per page (default 50)
- `severity`: Filter by severity (info, warning, error, critical)
- `source`: Filter by source (windows-eventlog, etc.)
- `acknowledged`: Filter by read status (true/false)
- `since`: ISO 8601 date (e.g., 2025-01-01T12:00:00Z)

**Response**:
```json
{
  "items": [
    {
      "id": 1,
      "sourceId": "evt-123",
      "source": "windows-eventlog",
      "severity": 2,
      "title": "Disk space low",
      "message": "Free space on C: is 5 GB",
      "tags": ["System", "Warning"],
      "occurredAt": "2025-01-15T10:30:00Z",
      "ingestedAt": "2025-01-15T10:31:00Z",
      "isAcknowledged": false,
      "metadata": {...}
    }
  ],
  "total": 250,
  "page": 1,
  "pageSize": 50
}
```

---

```
POST /api/notifications/{id}/acknowledge
```

Mark notification as read.

**Response**: 204 No Content

---

### Filters

```
GET /api/filters
```

List all filters.

**Response**:
```json
{
  "filters": [
    {
      "id": 1,
      "name": "Suppress Info",
      "sourcePattern": "*",
      "titlePattern": "*",
      "minSeverity": 0,
      "action": 0,
      "isEnabled": true
    }
  ]
}
```

---

```
POST /api/filters
```

Create a new filter.

**Request Body**:
```json
{
  "name": "Suppress Disk Warnings",
  "sourcePattern": "windows-eventlog",
  "titlePattern": "*Disk*",
  "minSeverity": 1,
  "action": 0,
  "isEnabled": true
}
```

---

```
PUT /api/filters/{id}
```

Update a filter.

---

```
DELETE /api/filters/{id}
```

Delete a filter.

---

### Statistics

```
GET /api/stats/summary
```

Get counts by severity.

**Response**:
```json
{
  "total": 1250,
  "unacknowledged": 42,
  "bySeverity": {
    "info": 800,
    "warning": 350,
    "error": 85,
    "critical": 15
  },
  "bySource": {
    "windows-eventlog": 1200,
    "custom-api": 50
  }
}
```

## Authentication (Planned)

Coming in Phase 2:
- JWT token generation
- API key management
- HTTPS enforcement

## Rate Limiting

Coming in Phase 2:
- 100 requests/minute per client
- Burst allowance: 10 requests

## WebSocket (Real-time Updates)

Coming in Phase 3:
- Real-time notification feed
- Filter updates
- Service health status

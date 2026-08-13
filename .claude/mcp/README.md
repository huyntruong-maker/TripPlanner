# MCP Servers

Model Context Protocol server configs for the `claude-fullstack-pack` stack. Each file uses
the standard `mcpServers` shape so it can be merged into a project `.mcp.json` or added with
`claude mcp add`.

## Servers

| File | Server | What it gives the agent | Required env |
| --- | --- | --- | --- |
| `github-mcp.json` | `@modelcontextprotocol/server-github` | Repos, issues, PRs, Actions, code search on GitHub | `GITHUB_TOKEN` |
| `filesystem-mcp.json` | `@modelcontextprotocol/server-filesystem` | Read/write scoped to the project directory | none (path arg) |
| `postgres-mcp.json` | `@modelcontextprotocol/server-postgres` | Inspect/query a PostgreSQL database | `POSTGRES_CONNECTION_STRING` |
| `mssql-mcp.json` | `@executeautomation/mssql-mcp-server` (community) | Inspect/query a SQL Server database | `CONNECTION_STRING` |

> Note: the MSSQL server is a community package (no official Anthropic/MCP SQL Server server
> exists at time of writing). Swap in your preferred community MSSQL MCP if desired; keep the
> `CONNECTION_STRING` env contract.

## Environment variables

Set these in your shell / `.env` (never commit real values):

```bash
export GITHUB_TOKEN="ghp_xxx"                    # fine-grained PAT, least privilege
export POSTGRES_CONNECTION_STRING="postgresql://user:pass@localhost:5432/app"
export CONNECTION_STRING="Server=localhost;Database=app;User Id=sa;Password=...;TrustServerCertificate=True"
```

`filesystem-mcp.json` is scoped via `${CLAUDE_PROJECT_DIR}` so the server can only touch the
project directory.

## Merge into a project `.mcp.json`

A project `.mcp.json` uses the same shape. Combine the servers you want under one
`mcpServers` object:

```json
{
  "mcpServers": {
    "github": { "command": "npx", "args": ["-y", "@modelcontextprotocol/server-github"], "env": { "GITHUB_TOKEN": "${GITHUB_TOKEN}" } },
    "filesystem": { "command": "npx", "args": ["-y", "@modelcontextprotocol/server-filesystem", "${CLAUDE_PROJECT_DIR}"] }
  }
}
```

Copy the individual `<name>: { ... }` blocks from each file in this folder into that object.

## Add via the CLI

```bash
claude mcp add github     -- npx -y @modelcontextprotocol/server-github
claude mcp add filesystem -- npx -y @modelcontextprotocol/server-filesystem "$PWD"
claude mcp add postgres   -- npx -y @modelcontextprotocol/server-postgres "$POSTGRES_CONNECTION_STRING"
claude mcp add mssql      -- npx -y @executeautomation/mssql-mcp-server
```

Set the env vars in the same shell before launching Claude Code so `${VAR}` references
resolve. Use read-only DB credentials for the database servers unless writes are required.

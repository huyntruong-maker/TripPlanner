# Rules

This directory holds always-follow guidelines that agents should obey while working in this
stack (ASP.NET Core Web API + React/TypeScript). Unlike skills (which are invoked for a
specific task), rules are persistent constraints that apply across all work.

## Layout

```
rules/
  common/      Cross-cutting rules for every language/area
    coding-style.md
    git-workflow.md
    testing.md
    security.md
    agents.md          When to delegate to which specialist agent
  dotnet/      Backend-specific rules
    dotnet-style.md
    dotnet-testing.md
  react/       Frontend-specific rules
    react-style.md
    react-testing.md
```

## How it maps to ~/.claude/rules/ when installed

Claude Code reads rule files from `~/.claude/rules/` (user-global) and from a project's
`.claude/rules/` (project-local). To install these rules:

- User-global (apply everywhere):
  ```bash
  mkdir -p ~/.claude/rules
  cp -r rules/* ~/.claude/rules/
  ```
- Project-local (apply only in one repo, takes precedence / adds to global):
  ```bash
  mkdir -p .claude/rules
  cp -r rules/* .claude/rules/
  ```

The folder structure is preserved (`common/`, `dotnet/`, `react/`), so
`rules/dotnet/dotnet-style.md` becomes `~/.claude/rules/dotnet/dotnet-style.md`.

You can also reference individual rule files from your `CLAUDE.md` so they are loaded into
context. Keep each rule file short and imperative; agents apply all of them continuously.

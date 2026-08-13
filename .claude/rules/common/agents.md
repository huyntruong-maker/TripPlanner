# Agent Delegation (Common)

When a task fits a specialist, delegate to the matching agent in this pack instead of
doing everything inline. Pick by the primary nature of the task.

- Backend API work (endpoints, DI, EF Core, auth) -> the .NET / backend agent;
  apply `dotnet-webapi-patterns` and `database-and-ef-core` skills.
- Frontend work (components, state, forms, accessibility) -> the React / frontend agent;
  apply `react-ui-engineering` and `react-data-and-state` skills.
- API contract or interface design spanning both -> the API design agent;
  apply `api-and-interface-design` before implementation.
- Test authoring or coverage gaps -> the testing agent; apply `dotnet-testing` and the
  React testing rules.
- Security review or hardening -> the security agent; apply `security-and-hardening`.
- CI/CD, Docker, or release tasks -> the devops/release agent; apply `ci-cd-and-automation`
  and `deployment-and-release`.

Guidelines:
- Delegate when the task is deep and self-contained; keep coordination in the main thread.
- Give the agent the goal, constraints, and the relevant skill names.
- For cross-cutting work, sequence: design contract -> backend -> frontend -> tests -> security.
- Do not delegate trivial edits; the overhead is not worth it.

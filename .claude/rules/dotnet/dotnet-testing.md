# .NET Testing Rules

- Use xUnit with `[Fact]`/`[Theory]` + `[InlineData]`; structure as Arrange-Act-Assert.
- Assert with FluentAssertions for readable failures.
- Mock collaborators with NSubstitute or Moq; never mock the system under test.
- Name tests `Method_State_ExpectedResult`.
- Integration-test the real pipeline with `WebApplicationFactory<Program>`.
- Use Testcontainers for the database; avoid EF InMemory for query-behavior tests.
- Keep tests independent and parallel-safe; seed and isolate per test, no shared static state.
- Replace nondeterministic deps (clock, HTTP) with fakes injected via `ConfigureTestServices`.
- Cover unhappy paths: validation errors, 4xx/5xx responses, empty and boundary inputs.
- Collect coverage with coverlet (`--collect:"XPlat Code Coverage"`) and enforce a CI floor.
- `dotnet test` must pass locally and in CI before merge; fix flaky tests immediately.

Test real behavior through real infrastructure where it matters.

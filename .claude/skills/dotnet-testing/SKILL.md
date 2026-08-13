---
name: dotnet-testing
description: Guides agents through testing .NET APIs with xUnit, FluentAssertions, WebApplicationFactory, and Testcontainers. Use when writing or reviewing unit and integration tests for an ASP.NET Core backend.
---

# .NET Testing

## Overview
A pragmatic test strategy for ASP.NET Core: fast unit tests with xUnit + FluentAssertions,
mocking via NSubstitute or Moq, and integration tests with WebApplicationFactory backed by
real databases through Testcontainers.

## When to Use
- Adding tests for new services, handlers, or endpoints.
- Setting up an integration test harness.
- Reviewing test coverage or flaky tests in a PR.

## Process

1. Structure tests by type:
   ```
   tests/UnitTests          (pure logic, fast, no IO)
   tests/IntegrationTests   (WebApplicationFactory + Testcontainers)
   ```

2. Use xUnit with `[Fact]` and `[Theory]`/`[InlineData]`. Arrange-Act-Assert.
   ```csharp
   [Theory]
   [InlineData(0)]
   [InlineData(-1)]
   public void Reject_NonPositive_Quantity(int qty) {
       var act = () => new OrderLine(qty);
       act.Should().Throw<ArgumentException>();
   }
   ```

3. Assert with FluentAssertions for readable failures.
   ```csharp
   result.Should().NotBeNull();
   result.Items.Should().HaveCount(2).And.OnlyHaveUniqueItems();
   ```

4. Mock collaborators (not the system under test) with NSubstitute or Moq.
   ```csharp
   var repo = Substitute.For<IOrderRepository>();
   repo.GetAsync(id, Arg.Any<CancellationToken>()).Returns(order);
   ```

5. Integration-test the real pipeline with WebApplicationFactory.
   ```csharp
   public class ApiFactory : WebApplicationFactory<Program> {
       protected override void ConfigureWebHost(IWebHostBuilder b) =>
           b.ConfigureTestServices(s => s.AddScoped<IClock>(_ => new FakeClock()));
   }
   ```

6. Use Testcontainers for a real DB instead of in-memory providers (which hide SQL bugs).
   ```csharp
   var db = new PostgreSqlBuilder().WithImage("postgres:16").Build();
   await db.StartAsync();
   // pass db.GetConnectionString() into the factory
   ```

7. Replace external dependencies (clock, time, HTTP clients) with fakes/stubs so tests
   are deterministic. Seed data per-test; isolate state.

8. Keep tests independent and parallel-safe. No shared mutable static state; use fresh
   containers or transactions rolled back per test where possible.

9. Measure coverage with `coverlet` and enforce a floor in CI.
   ```bash
   dotnet test --collect:"XPlat Code Coverage"
   ```

10. Name tests for behavior: `Method_State_ExpectedResult`.

## Rationalizations
| Excuse | Reality |
| --- | --- |
| "EF InMemory is good enough." | It misses real SQL, constraints, and concurrency. |
| "Mock everything for speed." | Over-mocking tests the mocks, not the code. |
| "Integration tests are too slow." | Testcontainers + parallel runs keep them viable. |
| "100% coverage means done." | Coverage measures reach, not correctness. |
| "Tests can share a static seed." | Shared state causes order-dependent flakiness. |

## Red Flags
- Tests depend on run order or wall-clock time.
- Mocking the class under test or asserting on mock internals only.
- Using EF InMemory to validate query behavior.
- No assertions, or asserting only `NotNull`.
- Integration tests hitting a shared, dev database.

## Verification
- `dotnet test` passes locally and in CI.
- Integration tests spin up and tear down their own containers.
- Coverage report generated; CI gate enforced.
- Tests pass when run in parallel and in random order.
- Each failing test message clearly states the expected behavior.

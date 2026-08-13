# Coding Style (Common)

Always-follow conventions across the codebase, regardless of language.

- Write clear, intention-revealing names. No abbreviations or single letters except loop indices.
- Keep functions small and single-purpose. Extract when a function does more than one thing.
- Prefer pure functions and explicit inputs/outputs over hidden global state.
- Fail fast: validate inputs at boundaries and return early instead of deep nesting.
- No dead code, commented-out blocks, or `TODO` without a tracking issue.
- Keep formatting automatic: rely on `dotnet format` and Prettier/ESLint; never hand-format.
- Comments explain *why*, not *what*. Delete comments that restate the code.
- No magic numbers or strings; name constants and use config/tokens.
- Handle errors explicitly; never swallow exceptions or ignore rejected promises.
- Keep files focused; split modules/components that grow past a clear single responsibility.
- Match the existing patterns of the file you are editing before introducing new ones.
- Run the formatter and linter before committing; CI must stay green.

When in doubt, optimize for readability and the next maintainer.

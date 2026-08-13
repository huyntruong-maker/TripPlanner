---
description: Review the current diff/branch by invoking the code-reviewer and security-auditor agents — five-axis review with severity labels.
argument-hint: "[branch, PR, or diff scope; defaults to current working diff]"
---

Review the change set ($ARGUMENTS, or the current working diff/branch if empty).

Steps:
1. Determine scope: the staged/working diff or the named branch/PR. Confirm it is ~100-400 lines and one logical change; if much larger, review in focused passes.
2. Invoke the `code-reviewer` agent to review across five axes — correctness, design, tests, security, readability — labeling each finding Blocker / Major / Minor / Nit (per code-review-and-quality).
3. Invoke the `security-auditor` agent for a focused security pass (authz/authn, input validation, secrets, injection, dependency risk).
4. Build and run the tests yourself; do not rely on the author's word.
5. Report a consolidated verdict (approve / approve-with-nits / request changes) with an explicit blocker list.

Verification: all five axes addressed, every finding has a severity label, build + tests were run by the reviewer with evidence, and a clear verdict is recorded.

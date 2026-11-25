---
name: adr
description: Record an architecture decision (ADR) for the padel API in docs/adr. Use when a change chooses between reasonable alternatives, adds or replaces a library or infrastructure component, changes an API convention, or reverses an earlier decision; also when asked to "write an ADR" or "document why".
---

# Record an architecture decision

ADRs live in `docs/adr`. [0001](../../../docs/adr/0001-record-architecture-decisions.md) explains when to write one.

1. **Check the existing records** in `docs/adr/README.md`. If the decision changes an earlier one, the new record supersedes it (step 5).
2. **Number:** the next free number, four digits. **File name:** `NNNN-decision-in-a-few-words.md`, in lowercase with hyphens.
3. **Copy** `docs/adr/template.md` and fill it in:
   - **Title:** `# N. The decision`, phrased as what we do ("Cursor paging for lists"), not as the problem.
   - **Status:** `Accepted` when the change is made in the same pull request; otherwise `Proposed`. **Date:** today.
   - **Context:** the problem and its constraints, with facts (numbers, versions, error messages). Leave out the solution.
   - **Decision:** what we do, specific enough to check in the code: name the classes, settings and libraries. Include the alternatives that were considered and why they were rejected.
   - **Consequences:** what becomes easier or harder, what we accept, and when to revisit the decision.
   - Keep it short, about one screen. Link to other ADRs as `[ADR 6](0006-....md)`.
4. **Add a row** to the table in `docs/adr/README.md`.
5. **Superseding:** in the old record, change only the status to `Superseded by [NNNN](NNNN-....md)`, and update its row in the index. Accepted records are otherwise never edited.
6. If the decision changes how people work in the code (a rule, a command or a convention), also update `CLAUDE.md` and the README.

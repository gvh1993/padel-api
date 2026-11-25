# 1. Record architecture decisions

- **Status:** Accepted
- **Date:** 2026-11-25

## Context

The code shows *what* was built, but not *why*, or which options were rejected. Without that, a later change can undo a deliberate decision, or the same discussion happens again.

## Decision

Significant decisions are recorded as Architecture Decision Records (ADRs) in `docs/adr`, one Markdown file per decision, numbered in order: `NNNN-short-title.md`. Each record has a status, a date, the context, the decision and its consequences. Copy [template.md](template.md) to start a new one.

A decision is significant when it is hard to reverse, affects several layers, or chooses between reasonable alternatives (a library, a pattern, a protocol).

Records are not edited after they are accepted, apart from their status. A changed decision gets a new record that supersedes the old one; the old one's status becomes `Superseded by NNNN`.

## Consequences

- New team members (and coding agents) can read why the code looks the way it does.
- Writing a record takes a few minutes per decision, and it belongs in the same pull request as the change.

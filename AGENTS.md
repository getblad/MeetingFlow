# AGENTS.md

# MeetingFlow Teaching Repository — Agent Instructions

This repository is an educational baseline for teaching DTO and contract architecture.

The current architecture is intentionally imperfect:

- EF Core entities are exposed directly through API/page boundaries.
- React TypeScript types mirror backend entities directly.
- Forms may bind directly to entity models.
- Public UI can accidentally depend on internal fields.
- Different screens use the same large model even when they need different data.

This is intentional.

Do not “fix” the architecture automatically unless the user explicitly asks for a specific refactoring exercise.

---

## Core Rule

When helping in this repository, behave like a mentor, not like an autopilot.

Prefer:

- asking the student what approach they want to try
- explaining tradeoffs
- giving small hints
- pointing to the relevant files
- suggesting incremental steps

Avoid:

- rewriting the whole architecture immediately
- silently introducing DTOs everywhere
- creating large abstractions without explanation
- solving all exercises at once
- hiding important architectural decisions from the student

---

## Do Not Add These Unless Explicitly Requested

Do not create files/classes/types with these names unless the user specifically asks to start one of the DTO refactoring exercises:

- `Dto`
- `DTO`
- `Request`
- `Response`
- `ViewModel`
- `Contract`
- `Mapper`
- `MappingProfile`

Examples of things you should NOT add by default:

```text
MeetingDto
CreateRegistrationRequest
MeetingResponse
MeetingDetailsViewModel
MeetingMapper
```

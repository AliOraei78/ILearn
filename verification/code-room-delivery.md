# Sidebar and code room delivery

Date: 2026-11-03.

The existing lobby now appears under «آموزشگاه و کارگاه». The original `/lobby` route, lesson content and saved training progress remain available. All twelve proposed code-room templates are implemented as independent metadata; technology is supplied separately to the reviewer.

| Requirement | Implementation and verified evidence |
| --- | --- |
| Coordinated sidebar and top band | `GameShell.razor` reuses `PlayerBar` and the existing palette, font and square controls. Desktop navigation stays on the right; mobile navigation has a labelled toggle. Native-host screenshots were visually inspected. |
| Three menu destinations | `/lobby`, `/code-room` and `/interview` were reached through actual links in browser checks. The interview destination is visibly marked «به‌زودی». |
| Existing goals and lessons under the academy | The deployed lobby displays the existing subjects and `/lobby?view=chapters` displays the C# chapter list. Existing unit tests for curriculum and progression pass. |
| Problem and answer sections | `CodeRoom.razor` shows the prompt/code and editor in separate desktop columns; narrow layouts stack them. Source, hint and rubric were checked for each authored template. |
| Code-style input | The editor has line numbers, monospace code, Tab/Shift+Tab indentation and Escape-then-Tab focus escape. Drafts survive autosave, reload and navigation before the autosave delay. |
| At least ten distinct templates | Twelve definitions in `CodeRoomTemplates.All` have authored examples. Forward execution, inverse input, repair, tests, completion, ordering and the remaining formats have separate response contracts. |
| Practice attached to chapters | Every current C# chapter supplies its existing independent workshop as a room exercise, with separate progress. There are 123 chapter practices plus 12 authored examples, totaling 135. |
| Generality across technologies | Authored examples include C#, PostgreSQL and Redis. Reviewer requests carry technology, template, chapter, rubric and a reference solution. Unit tests and fixture requests verify SQL/Redis context. |
| AI assessment | The room has no personal-completion action. Valid review results update feedback and completion; service errors and invalid responses add no completion or XP. Invalid structured output is retried once. |
| Save and XP consistency | Room keys use `code-room/`; academy keys retain `csharp/`. The room remembers its own exercise without changing the academy position. XP counts each completed room exercise once, and the same total appears on the profile. |
| Authentication and deep links | The shared return-destination allowlist accepts the two new local routes and continues rejecting external or malformed destinations. The real Docker login returned to the requested room subject. |
| Local deployment | `docker compose up -d --build ilearn` succeeded. PostgreSQL remained running and healthy. The deployed content SHA-256 matches the source, and browser checks found all 135 exercises. |

Validation:

- The solution build passed with zero warnings and errors.
- All 141 .NET tests passed, with none failed or skipped.
- The deterministic browser flow passed 21 behavioral checks and 16 layout checks, including mobile and enlarged text, with no browser or asset errors.
- The rebuilt Docker app passed five additional browser checks with no browser or asset errors.
- Existing navigation test labels were updated for the lobby rename. Historical full-app browser audits were not rerun in their entirety.

Generated evidence: `code-room-browser.json`, `code-room-docker.json`, `code-room-desktop.png`, `code-room-mobile.png`, and `code-room-docker-desktop.png` in this directory. These artifacts are ignored by Git.

Limits:

- Only the interview menu and its honest placeholder destination were requested; an interview conversation engine is not included.
- Player code is reviewed as text and is not executed. Browser grading tests use a local fixture and do not measure the accuracy or availability of the real model. Production retains the existing fixed free OpenRouter model and zero-price provider policy.
- SQL and Redis have starter chapter content. The full existing 123-chapter learning curriculum remains C#.

# Browser verification and anti-slop gate

Final UI/UX audit (2026-11-02): [findings, fixes, responsive matrix, evidence and reproduction](verification/ux-audit-2026-11-02.md). Release/Production passed 80 .NET tests. Chrome and Edge each passed 51 browser groups and 3,024 layout/state/viewport checks across all 32 activities and 46 base viewport sizes, with zero measured layout findings or browser errors. The running Docker app on port 5081 was rebuilt and passed 10 smoke groups and 46 viewport checks. These results supersede the earlier reports below. Safari, Firefox and physical devices were not tested.

First audit: [18 initial findings and fixes](verification/full-audit-2026-11-02.md), retained as historical evidence.

Chapter S08: [six mini-games and delivery gate](verification/chapter08-delivery.md). 62 automated tests passed; 18 browser check groups include all six game surfaces at four widths, persistence and keyboard controls.

Current RightPixel account behavior and exact email template: [rightpixel-account-delivery.md](verification/rightpixel-account-delivery.md). 43 unit tests and 62 isolated browser/integration checks, including concurrent sends across two app instances.

Latest account controls/remember/recovery verification: [account-controls-delivery.md](verification/account-controls-delivery.md). 36 automated tests and 56 isolated browser checks, plus a dedicated local test-account login.

Current brand/profile/email delivery: [brand-email-delivery.md](verification/brand-email-delivery.md). 36 automated tests and 47 isolated browser checks passed. Real SMTP configuration is pending explicit authorization; no real verification email was sent by the tests.

Latest account/lobby/Postgres verification: [accounts-delivery.md](verification/accounts-delivery.md), with 30 automated tests and 37 browser checks. The older sections below describe earlier versions.

Latest verification: [dark theme and review delivery gate](verification/dark-theme-check.md), including browser keyboard checks, responsive measurements, score tests and real model limitations. Earlier styling evidence: [mild pixel theme](verification/pixel-theme-check.md). The sections below preserve the earlier baseline and Docker checks.

Date: 2026-11-01. Local URL: http://127.0.0.1:5080.
Mode: during (session choice). Direction: DESIGN.md.
Final stack: .NET 10, Blazor Interactive Server, native HTML controls and local CSS.
MudBlazor was removed after its global CSS hid native focus outlines. No UI package remains.

## Build and automated checks

PASS: dotnet build, zero warnings and zero errors.
PASS: dotnet test, 12 passed, zero failed or skipped.
Tests cover authored content structure and answer keys, exact multi-select answers, compiled ref/dispatch examples, persistence and backup recovery, refusing corrupt data without a backup, structured model output, malformed-output retry, Persian-output validation, service failures and cancellation.

## Recorded browser interactions

| Control or game | Observed result |
|---|---|
| S05: پیشگوی خروجی | Wrong answer entered review queue; corrected answer earned 10 XP; resubmit stayed at 10 XP. |
| S05: کلید گمشده | Both selectors accepted out; correct feedback and completion appeared. |
| S05: جفت‌یاب | Four mappings submitted; correct feedback appeared. |
| S05: کد درهم | Six upward moves restored valid order; correct feedback appeared; boundary move buttons disabled. |
| S05: ضدنمونه | quantity=0 accepted; hint opened and closed. |
| S06: قاضی کامپایلر | Private access rejected by answer key; correct choice completed. |
| S06: شکار باگ | Line 2 identified; explanation appeared. |
| S06: سبدبندی | Three row mappings completed. |
| S06: جراحی یک‌حرکتی | private set accepted; explanation appeared. |
| S06: تست‌چین | Three required cases checked and accepted. Prompt explicitly requires the zero boundary. |
| S07: ردپای اجرا | Vip, Base, Vip entered across steps; previous/next steps worked; correct submission completed. |
| S07: کارآگاه کد | Submission before clues showed validation; two clues opened; third disabled; diagnosis accepted. |
| S07: مهندسی معکوس | override selected and accepted. |
| S07: قراردادساز | abstract, abstract, override selected and accepted. |
| S07: اثر دومینو | Both affected base-reference calls selected and accepted. |
| Session selector and all exercise navigation | All three sessions and all fifteen game destinations opened. Previous/next exercise navigation worked. |
| Empty AI answer | Review and personal-completion buttons were disabled. |
| AI review | Real qwen2.5-coder:3b response received locally, for a conceptual answer and a full workshop submission. |
| Cancel AI review | Pending review cancelled; typing and review remained available. |
| Personal completion | Recorded personal source and XP without requiring model approval. |
| Workshop editor | Multiline C# preserved; computed direction was ltr. |
| Completed session | S07 showed 8/8 and completed-session message, with total test XP 200. |
| Fixed rubric | Opened and displayed the authored rubric beside model feedback. |
| Model unavailable | Temporary test-only endpoint 127.0.0.1:1 produced readable error; full submitted code remained present; personal completion worked. Real endpoint restored afterward. |
| Review queue | An incorrect trace answer entered queue; navigating from S05 using review returned to that S07 activity. |
| Empty review queue | Displayed an explicit no-pending-mistakes message. |
| Curriculum disclosure | Opened and closed; 44 real roadmap entries present. |
| Summary disclosure | Opened and closed while the activity stayed available. |
| Reload and server restart | Selected session, draft, game completion and XP recovered. |
| Keyboard | Tab from textarea reached the next enabled action; visible brown focus outline observed after removing MudBlazor. |
| Fresh final session | Returned to S05, 0 XP, no completed games and no pending mistakes. Test progress archived under App_Data/browser-test-progress. |

The application recovery dialog uses the framework reconnect event mechanism. Its retry/resume event bindings were inspected in the bundled framework-template JavaScript; these exceptional framework-managed states are separate from the recorded learning-flow click-through.

## Responsive and visual evidence

Four game templates checked at widths 320, 390, 768 and 1366: choice, row selection, ordering and stepped execution.
All sixteen width/template checks returned document.scrollWidth <= window.innerWidth.
Workshop textarea also passed the mobile overflow check.
Final mobile screenshot inspected after viewport repaint; right edge, navigation and work surface fit.
Final fresh browser tab console: no warnings or errors.
Intentional server shutdown during restart testing produced expected connection warnings in the old tab; final checks used a fresh tab.

Screenshots: verification/desktop.jpg and verification/mobile.jpg.
No Lighthouse or field Web Vitals measurement was made.

## Delivery gate

- R-01 PASS: one teal accent, paper background and ink text; purposes in DESIGN.md.
- R-02 PASS: authored UI copy contains no em dash.
- R-03 PASS: responsive checks above; controls and option labels have minimum 44px targets.
- R-04 PASS: no decorative icon library or generic feature icons.
- R-05 PASS: one study workspace; composition follows exercises, with deliberate uniform rhythm.
- R-06 PASS: local Persian-readable font; monospace limited to code.
- R-07 PASS: no decorative background patterns.
- R-08 PASS: navigation uses text; ordering buttons describe the actual movement.
- R-09 PASS: only real completion and progress statuses.
- R-10 PASS: no blur or glass surfaces.
- R-11 PASS: consistent small control radii, no pill-based treatment.
- R-12 PASS: work surface uses a border; hover inset identifies interactive controls.
- R-13 PASS: no glow.
- R-14 PASS: one exercise surface, no promotional feature-card grid.
- R-15 PASS: actions say submit answer, review with Ollama, personal completion and review mistakes.
- R-16 PASS: no marketing buzzwords.
- R-17 PASS: XP, session counts and 44 roadmap entries derive from actual content/state.
- R-18 PASS: no testimonials.
- R-19 PASS: no decorative animations; recovery styling simplified to a static dialog.
- R-20 PASS: numbered workshop exercises and Persian instructional voice establish the product identity.
- R-21 PASS: fixed light theme is the accepted user direction.
- R-22 PASS: no illustrations.
- R-23 PASS: no logo, avatar or other invented identity assets; navigation follows the approved one-page plan.
- R-24 PASS: exercise navigation reaches actual content; future roadmap entries are plain text.
- R-25 PASS: measured palette contrast: text/paper 12.63:1, secondary text/background 6.17:1, white/teal 7.05:1, code text/background 10.79:1, error text/background 9.61:1.
- R-26 PASS: learning-flow actions verified in the table; framework recovery handlers inspected; no placeholder controls.
- R-27 PASS: empty input and empty review states, pending model state and model/storage error handling present.
- R-28 PASS: no FAQ.
- R-29 PASS: neutral paper/ink plus teal; brown reserved for focus/error semantics.
- R-30 PASS: no product clone or borrowed visual assets.
- R-31 PASS: major visual choices have written reasons in DESIGN.md.
- R-32 PASS: native keyboard controls; visible focus verified; native dialog supports Escape.
- R-33 PASS: UI edits made directly in Razor/CSS, no source-rewriting helper scripts.
- R-34 PASS: a single tested theme is shipped.
- R-35 PASS: application built and run; recorded learning-control click-through and final console checks above.
- R-36 PASS: no fabricated compliance, customer or performance claims.
- R-37 PASS: explicit user direction and design read recorded before implementation.
- R-38 PASS: actual roadmap, authored exercises and earned progress; no disguised placeholders.
- R-39 PASS: native controls, visible field labels, landmarks, fa language and title; pointer/disabled states, keyboard focus and motion reduction present.
- R-40 PASS: no shipped image/video/embed media or third-party blocking scripts; performance numbers are not claimed.

## Liveliness and craftsmanship

- PASS: ENERGY 1 / RHYTHM 1 / MOTION 1 explicitly set and followed.
- PASS: current exercise is the focal point.
- PASS: whitespace separates summary, activity navigation and exercise.
- PASS: teal identifies the primary action and earned progress.
- PASS: numbered exercises and concise Persian feedback repeat the workshop motif.
- PASS: design read declared before generation.
- C-1 PASS: major choices have explicit reasons.
- C-2 PASS: recorded learning actions perform real behavior.
- C-3 PASS: no template marketing sections.
- C-4 PASS: mobile, empty, pending, error, reload and keyboard paths checked.
- C-5 PASS: progress represents actual app state; no fabricated public claims.

## Practical limits

Single local user and one active browser tab. User code is reviewed, never executed.
The 3B model sometimes gives weak or inaccurate advice even with valid Persian JSON.
A fixed authored rubric is available, and personal completion remains available.
Only S05-S07 contain playable lessons; the remaining roadmap entries are informational.

## Docker verification (2026-11-01)

- PASS: docker compose config --quiet and docker compose up -d --build.
- PASS: Linux production container runs as uid/gid 1654, not root, on 127.0.0.1:5081.
- PASS: Content/catalog.txt and framework Blazor JavaScript are present in the published image. Docker build now asserts both files exist.
- Fixed the .NET 10 restore-before-Razor behavior by explicitly setting RequiresAspNetWebAssets. Without it the first image built successfully but had no Blazor startup script. Reference: https://github.com/dotnet/aspnetcore/issues/69341.
- PASS: Browser rendered the course, session controls and exercise; hint opened and closed through the live Blazor connection.
- PASS: Final fresh browser console had no warnings or errors. Screenshot: verification/docker.jpg.
- PASS: Host Ollama API reached from the Compose network with the same host-gateway mapping; qwen2.5-coder:3b was present. The model was not downloaded into Docker.
- PASS: App_Data writable as the application user. A temporary persistence probe survived force-recreating the container and was removed afterward.
- PASS: Progress file SHA-256 unchanged across container recreation. Existing user progress was not reset.
- PASS: Linux data-protection keys use a separate named volume, isolated from existing Windows DPAPI keys.
- PASS: dotnet test after packaging changes: 12 passed, zero failed or skipped.
- UI styles and exercise logic were unchanged by containerization; the original UI delivery gate remains applicable.


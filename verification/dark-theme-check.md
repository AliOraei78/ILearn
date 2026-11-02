# Dark theme and review verification

2026-11-01. Anti-slop: during (session choice). Direction: approved fixed dark palette, local IranSans, mild pixel contours, one-page workshop. ENERGY 1 / RHYTHM 1 / MOTION 1. Reasons: DESIGN.md.

## Evidence

- Unit suite: 23 passed, zero failed/skipped. Includes outgoing HTTP question/code/rubric/answer/lesson, score boundaries and invalid scores/retry, cancellation, old progress and persistence.
- Docker Release publish passed, including framework/content assertions. Production: localhost:5081. Progress SHA-256 matched across recreation; QA used separate tmpfs App_Data.
- Browser: first introduction, start, review, resume, lesson selection and reload exercised. Intro adds no XP or ninth activity. Draft and challenge position survived intro/resume and reload.
- Both editors: Tab inserts four spaces; Undo restores text. Multiline code a/newline/b indented both lines, Shift+Tab restored them, Undo restored indentation. Escape then Tab moved to the next summary with gold focus outline. Native controls keep ordinary Tab.
- Intro/workshop: 320, 390, 768, 1366px, no horizontal overflow; label gap 12px. All 15 games opened at 320/1366px; every hint opened/closed. Measurements: dark-layout.json. Prior complete game-action regression: pixel-theme-check.md; game logic unchanged.
- Chromium CSS.getPlatformFontsForNode reported custom IRANSansWeb-Bold on h1. Both WOFF2 files bundled; regular HTTP 200, 31564 bytes. Code retains Consolas/monospace. Screenshot: dark-desktop.jpg.
- Fresh deployed browser continued existing 80 XP, skipped introduction, rendered saved challenge, and had no console warnings/errors. Earlier QA connection errors occurred during disposable container replacement.
- sRGB contrast: main/surface 13.19:1, muted/surface 8.23:1, feedback 10.17:1, primary 8.58:1, error 10.45:1, muted/page 9.35:1, input border/surface 4.78:1.

## Real model limits

qwen2.5-coder:3b was tested with price<0 and price<=0 variants. It sometimes missed the zero-price defect, invented a defect in correct code, or returned English. Prompt experiments did not make this model reliable. The shipped prompt remains Persian and includes the full question and boundary/overflow assumptions. Invalid JSON/scores or non-Persian summaries are retried once and then rejected visibly. Valid JSON does not establish semantic correctness. Personal completion remains available. Old feedback receives no invented score. Code is never executed.

## Anti-slop delivery gate

- R-01 PASS: no gradients/glows.
- R-02 PASS: no em dash in authored UI copy.
- R-03 PASS: responsive measurements above show no overflow.
- R-04 PASS: no decorative icon library.
- R-05 PASS: content-driven single workshop surface.
- R-06 PASS: requested IranSans; monospace reserved for code.
- R-07 PASS: no background pattern.
- R-08 PASS: no decorative button arrows.
- R-09 PASS: no capsule marketing badges.
- R-10 PASS: no glass effects.
- R-11 PASS: square contours match approved pixel direction.
- R-12 PASS: hard shadows identify work surface/primary action; reasons documented.
- R-13 PASS: no glow effects.
- R-14 PASS: one active exercise, no feature-card grid.
- R-15 PASS: action labels describe start, review and submit behavior.
- R-16 PASS: no marketing buzzwords.
- R-17 PASS: XP from saved completion; percentage explicitly estimates model opinion.
- R-18 PASS: no testimonials.
- R-19 PASS: interaction states only, no template animation.
- R-20 PASS: numbered Persian challenges and requested pixel/font identity.
- R-21 PASS: fixed dark theme explicitly approved.
- R-22 PASS: no illustrations.
- R-23 PASS: local fonts from user-named source; no invented assets.
- R-24 PASS: same-page navigation uses real handlers.
- R-25 PASS: text contrast exceeds AA; ratios above.
- R-26 PASS: real handlers; click evidence above and prior game regression.
- R-27 PASS: loading, blank, pending/cancel and model-error states observed; draft retained.
- R-28 PASS: no FAQ.
- R-29 PASS: green neutrals/teal accent; gold/error colors identify functional states.
- R-30 PASS: original workshop retained; no product clone.
- R-31 PASS: palette, typography, layout, spacing and controls have reasons in DESIGN.md.
- R-32 PASS: native keyboard controls, visible focus and Escape/Tab exit verified.
- R-33 PASS: direct source patches; no external source/CSS rewrite script.
- R-34 PASS: one approved dark theme; native color-scheme dark.
- R-35 PASS: build, tests and browser click-through executed.
- R-36 PASS: no fabricated performance/security claims.
- R-37 PASS: approved direction and explicit 1/1/1 dials.
- R-38 PASS: no fictional public people/features/statistics.
- R-39 PASS: native named/labeled controls, focus, 44px targets, reduced-motion override, Persian lang/title/landmarks retained.
- R-40 PASS: no media or third-party blocking scripts added; no performance claims.
- Liveliness PASS: 1/1/1 dials, current-challenge focal point, structural whitespace, teal action and repeated numbered/square motif.
- C-1 PASS: reasons documented in DESIGN.md.
- C-2 PASS: real learning actions; no dead decorative controls.
- C-3 PASS: introduction and exercises serve learning, no template sections.
- C-4 PASS: intro, challenge, keyboard, responsive, pending/error and reload paths checked; reconnect CSS also dark.
- C-5 PASS: evidence separated from the model's fallible judgments.

Only Chromium tested. Other engines and screen-reader audio were not tested. Model grading remains fallible.

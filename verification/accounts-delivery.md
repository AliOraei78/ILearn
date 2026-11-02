# Account, lobby and PostgreSQL delivery

2026-11-01. Anti-slop during, session choice. ENERGY 1 / RHYTHM 1 / MOTION 1. Direction and reasons: DESIGN.md.

## Evidence

## Independent player bar

PlayerBar.razor now renders identity, Level/title, progress and account actions in one reusable top section above the page header on both home and profile. Existing darker input surface (#111916) and one border separate account state from chapter navigation; reasons recorded in DESIGN.md. No sticky overlay or animation added.

- R-03 PASS: Chrome measured home and profile at 320, 390, 768 and 1366px, with no document overflow and player bar bottom at or above page header top. A 40-character username also passed at 320px after deployment.
- R-25 PASS: calculated text/background contrast for the new band: #e9efe9 15.32:1, #b3c1b7 9.56:1, #79cdb4 9.54:1 against #111916.
- R-26 PASS: profile action navigated to /profile, character 3 saved and updated the shared bar, logout returned to /login.
- R-35 PASS: Docker Release publish succeeded; browser clicks and eight responsive measurements passed without page JavaScript errors. player-bar-desktop.png and player-bar-mobile.png visually inspected.
- Purpose and liveliness PASS: explicit 1/1/1 direction retained, one separated account band, native controls, reused requested pixel character and documented layout/color purpose. Shared data and account handlers unchanged. Other gate items below retain the previous implementation/evidence.

- Docker Release publish succeeded. PostgreSQL 18 and app run at http://127.0.0.1:5081.
- 30 automated tests passed. Existing 23 retained; level boundaries and complete authored references covered.
- 37 browser checks passed in installed Chrome, with zero page JavaScript errors. Exact recorded interactions: accounts-browser.json. Reproducible local runner: accounts-flow.cjs (uses the existing RightPixel Playwright installation and installed Chrome).
- Registration leads to a fresh topic lobby. Topic opens chapters; each of 24 activities opens. All 15 deterministic games accepted their authored keys, including row mapping, ordering, trace and detective clues. Every fixed-game hint opened and closed. All nine editors accepted Persian drafts. Review request cancelled; personal completion saved. Full reference opened without changing Level or draft. Avatar 2 saved. Logout returned to login. Incorrect password displayed validation. Missing antiforgery token returned 400. A second browser context registered independently at Level 1. A stale tab received the conflict message instead of overwriting the newer save.
- Widths 320, 390, 768, 1366 passed document overflow checks on the workshop. Desktop and mobile lobby screenshots inspected. Remaining roadmap entries are in a native disclosure.
- Data digest matched before and after restarting both PostgreSQL and app containers. Legacy progress.json and .bak retained their original sizes (15216, 7988 bytes) and modification times (2026-11-01 17:43:21 host display).
- Authored solutions are always available. Model explanation uses the configured Ollama endpoint; its generated explanation is supplementary. No code is executed or certified by the model.

## Delivery gate

- R-01 PASS: existing green surfaces and teal action accent; no new gradients or glow.
- R-02 PASS: new authored copy has no em dash.
- R-03 PASS: four browser widths passed; controls retain 44px minimums and wrapping.
- R-04 PASS: reused requested pixel characters identify the current account; no icon library added.
- R-05 PASS: topic, chapter and one exercise surface follow the learning flow; no marketing sections.
- R-06 PASS: existing local IranSans retained; code remains monospace.
- R-07 PASS: no decorative background pattern.
- R-08 PASS: actions use named text; no decorative arrows.
- R-09 PASS: no marketing capsules.
- R-10 PASS: no glass effects.
- R-11 PASS: existing square pixel contours retained.
- R-12 PASS: work surface and primary action keep the documented hard shadows only.
- R-13 PASS: no glow.
- R-14 PASS: one work surface; chapter rows represent actual playable content.
- R-15 PASS: buttons name account, chapter, answer, review and character actions.
- R-16 PASS: no marketing buzzwords in new copy.
- R-17 PASS: Level and progress derive from completed authored activities; no invented usage numbers.
- R-18 PASS: no testimonials.
- R-19 PASS: no decorative animation added.
- R-20 PASS: Persian numbered games, pixel avatars and square green surfaces retain the specific identity.
- R-21 PASS: fixed dark theme follows the existing approved direction.
- R-22 PASS: requested game characters reused; no stock illustrations.
- R-23 PASS: user explicitly authorized RightPixel avatars and the subject/chapter navigation.
- R-24 PASS: account routes exist; unfinished chapters are plain text in a disclosure.
- R-25 PASS: new controls reuse the already measured dark palette; main/surface 13.19:1, muted/surface 8.23:1 and primary 8.58:1 from dark-theme-check.md. No new text colors introduced.
- R-26 PASS: game, account, avatar and answer handlers exercised as recorded above; no placeholder actions.
- R-27 PASS: loading/error branches exist for profile and progress; login errors, empty review and model pending/cancel states provided. Stale-save failure observed.
- R-28 PASS: no FAQ.
- R-29 PASS: green neutral surfaces, teal accent and existing functional error/focus colors.
- R-30 PASS: requested reuse is limited to RightPixel character rendering, not another product layout.
- R-31 PASS: new lobby, character and progression purposes documented in DESIGN.md.
- R-32 PASS: native buttons, links, selects and details preserve keyboard semantics and existing visible focus/textarea escape handling; no custom keyboard controls added.
- R-33 PASS: UI implemented in Razor/CSS; no source-patching helper retained. Avatar source adapted from the user-designated TypeScript renderer.
- R-34 PASS: only the approved fixed dark theme ships.
- R-35 PASS: Docker publish, 30 tests and 37 recorded browser checks; screenshots inspected.
- R-36 PASS: no performance, certification or customer claims added.
- R-37 PASS: explicit existing design direction and 1/1/1 dials applied.
- R-38 PASS: chapter availability and progression come from real content; test accounts are private QA fixtures, not public product claims.
- R-39 PASS: native named controls, form labels, canvas image label, cursor/focus/disabled CSS and Persian document landmarks retained. No added motion or zoom restrictions.
- R-40 PASS: canvas reserves 64x64 pixels; no third-party blocking script or unmeasured performance score.
- Liveliness PASS: 1/1/1 dials, current-choice focal point, structural whitespace, teal action accent and repeated pixel motif.
- C-1 PASS: reasons documented in DESIGN.md.
- C-2 PASS: functional actions recorded in browser results.
- C-3 PASS: all sections serve account selection, learning or progress.
- C-4 PASS: native responsive controls, pending/error branches and stale-save protection retained.
- C-5 PASS: progress comes from state; no fabricated public claims.

## Limits

## Login heading focus correction

## Quiet lobby navigation

Save now accepts an announcement flag. Topic/chapter navigation saves the current position silently and clears any earlier success notice; successful response changes still announce saving, and storage errors remain visible. Docker Release publish passed. Chrome click-through: start chapter -> save confirmation; topics -> confirmation cleared; chapters -> no confirmation; select S05 -> exercise resumes without confirmation; change answer -> confirmation shown. No page JavaScript errors. R-26 PASS and R-35 PASS for the changed behavior; the existing gate items above retain their prior implementation and evidence.

Removed automatic heading focus only for the login/register component. Native keyboard focus styling and other pages' navigation focus remain intact. Docker Release publish passed. Installed Chrome verified both /login and /register: H1 is not the active element; the first Tab reaches #name with a visible 3px outline; no page JavaScript errors. R-32 PASS and R-35 PASS for this correction. Other gate items above retain the same implementation and evidence; no copy, palette, layout, assets or account behavior changed.

Only Chrome was exercised; screen-reader audio, physical devices and field performance were not measured. Model semantic correctness is not guaranteed. Database outage and model timeout UI branches were inspected in code; this run did not force those failures in the deployed service. Full anti-slop add-on folders are still absent; core rules were used. No skill files were downloaded or AGENTS.md pointer invented.

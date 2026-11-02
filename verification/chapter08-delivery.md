# Chapter 08 delivery verification

Date: 2026-11-02. Anti-slop: during, session choice; core only. Optional addons remain deferred until matching release files are supplied.
Design read: Persian learning workspace for programming students, existing dark pixel styling, ENERGY 1 / RHYTHM 1 / MOTION 1.

## Build and automated evidence

- dotnet build ILearn.slnx --no-restore: succeeded, zero errors.
- dotnet test ILearn.slnx --no-restore: 62 passed, zero failed or skipped.
- NU1900 remains: NuGet vulnerability metadata was unreachable. No dependency was added.
- Four playable chapters, 32 activities; S08 has six games, one explanation and one workshop, totaling 100 XP.
- Domain tests cover limits, duplicate observations, invalid indexes, multiple budget solutions, mission branches, exact assessment/matching answers, old JSON, game-state roundtrips and repeat XP.
- Compiled examples cover explicit implementation, default interface dispatch and static abstract constrained calls.
- Redis-labelled fixtures exercise all six rule branches without executing C# or contacting Redis. These are rule-reuse fixtures, not a playable Redis course.

## Recorded browser interactions

Run: node verification/chapter08-flow.cjs, against localhost:5083 with the existing isolated ilearn_email_qa database.
A new QA account was registered through the normal signup flow. Verification mail was captured by localhost SMTP, not sent to an external provider. Existing credentials were not read or copied.

The passing run recorded 18 check groups in chapter08-browser.json:
- Concept: guess, feature feedback and completion; reload and reselect S08 restored history and disabled the previous guess.
- Hint: open and close. Reference answer: displayed; optional model request cancelled.
- Experiment: answer before observation rejected; observation opened and correct law completed.
- Budget: both the combined contract and separate contracts accepted; restart retained completion.
- Mission: incorrect branch failed; restart and three correct decisions reached success.
- Assessment: all four correction selectors accepted the authored mapping.
- Deduction: three correct cells accepted; reload restored all marks.
- Each of six game surfaces fit widths 320, 390, 768 and 1366 (24 checks).
- Keyboard focus outline checked after Tab; Space sent to a focused native deduction button.
- Explanation and workshop editors accepted text and personal completion.
- Browser pageerror collection: empty.

Screenshots chapter08-mobile.png and chapter08-desktop.png were captured. Mobile image was visually inspected. The new long assessment selector overflow found at 320px was fixed with a constrained grid column and full-width select.

## Anti-slop Delivery Gate

This gate covers the changed learning surfaces, not an unrelated audit of all account flows.

- R-01 PASS: Existing dark green and teal palette retained for hierarchy; purpose in DESIGN.md.
- R-02 PASS: Authored game copy uses periods and commas; no em dash introduced in UI copy.
- R-03 PASS: All six games passed four widths; native controls retain 44px minimum height.
- R-04 PASS: No new icons or icon library.
- R-05 PASS: Single numbered exercise workspace; no marketing template sections.
- R-06 PASS: Existing IRANSans retained for Persian readability; no decorative typography.
- R-07 PASS: No background patterns added.
- R-08 PASS: Actions use descriptive text, without decorative arrows.
- R-09 PASS: Only real completion and count indicators.
- R-10 PASS: No blur or glass surfaces.
- R-11 PASS: Existing square controls retained.
- R-12 PASS: Existing workspace and primary-action shadows retained; rationale in DESIGN.md.
- R-13 PASS: No glow added.
- R-14 PASS: No feature-card grid; options represent actual selectable answers.
- R-15 PASS: Specific actions name guess, experiment, submit and restart.
- R-16 PASS: Instructional copy contains no marketing claims.
- R-17 PASS: XP and counts derive from content; budget explicitly labelled hypothetical.
- R-18 PASS: No testimonials.
- R-19 PASS: No new animation.
- R-20 PASS: Numbered Persian exercises and existing pixel treatment retained.
- R-21 PASS: Fixed dark theme follows the user's existing direction.
- R-22 PASS: No new illustrations.
- R-23 PASS: No new logo, avatar or identity assets; S08 uses the requested chapter navigation.
- R-24 PASS: Chapter and exercise navigation lead to loaded content.
- R-25 PASS: Measured text/surface 13.19:1, muted/surface 8.23:1, primary text/accent 8.58:1.
- R-26 PASS: Game actions and selectors exercised in the recorded browser flow; handlers inspected.
- R-27 PASS: Zero-count states, save-pending message and existing load/save error handling retained; local state is not erased by Save errors (code inspection).
- R-28 PASS: No FAQ added.
- R-29 PASS: Existing neutral greens and one teal accent retained.
- R-30 PASS: No copied product layout or new visual reference.
- R-31 PASS: Purpose of new comparison labels and responsive deduction layout recorded in DESIGN.md.
- R-32 PASS: Native keyboard controls; Tab focus visibly outlined; Space tested on a deduction control.
- R-33 PASS: Changes made directly through source patches; no source-rewriting UI script added.
- R-34 PASS: One existing fixed theme; no toggle introduced.
- R-35 PASS: App run, build passed and recorded browser interactions completed without page errors.
- R-36 PASS: No security, performance or customer claims introduced.
- R-37 PASS: DESIGN.md read; dials and Design Read recorded above.
- R-38 PASS: Clearly authored educational scenarios; no fictional social proof or disguised product statistics.
- R-39 PASS: Native named buttons, visible select labels, text comparison states and existing pointer/disabled/focus styling inspected; no new media.
- R-40 PASS: No new shipped media or third-party scripts; no unmeasured performance score claimed.

## Liveliness and craftsmanship

- PASS: explicit dials match the existing calm, uniform, static learning workspace.
- PASS: the active exercise is the focal point; existing spacing separates instructions, input and feedback.
- PASS: teal marks primary actions; numbered challenges remain the repeated identity motif.
- C-1 PASS: design decisions have a written purpose in DESIGN.md.
- C-2 PASS: changed controls have real handlers and recorded interactions.
- C-3 PASS: new content serves six requested learning mechanics.
- C-4 PASS: responsive and keyboard checks above; persistence roundtrip and reload paths verified.
- C-5 PASS: counts and XP reflect actual state; educational budgets are labelled hypothetical.

## Limits

The production Docker deployment was not updated. The temporary QA server was stopped after checking. Optional AI quality was not evaluated; deterministic games do not need AI. Full database-outage injection was not performed; save-error preservation was checked by source inspection. NuGet vulnerability metadata and field performance metrics were not measured.


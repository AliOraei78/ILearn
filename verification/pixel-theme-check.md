# Mild pixel theme: verification and anti-slop delivery gate

Date: 2026-11-01.
Mode: during, selected by the user in this conversation.
Direction: mild pixel styling, fixed light palette, original elements, layout and Persian font.
Dials: ENERGY 1 / RHYTHM 1 / MOTION 1.

## Scope and evidence

Only the main stylesheet, reconnect-dialog stylesheet and design documentation changed.
SHA-256 comparisons matched for Home.razor, App.razor, ReconnectModal.razor, Core.cs and lessons.json.
No new control, font, image, dependency, API or theme switch was added.

Browser tests ran in a separate container with tmpfs at /app/App_Data and no learner-data bind mount.
The live learner progress was not reset or replaced by test data.
Screenshots use a fresh preview session, not the learner's answers.

The final image passed Docker publish and its framework-script/content-file assertions.
It was deployed at http://127.0.0.1:5081.
The C# unit suite was not rerun for this CSS-only change; the browser checks below directly cover its visual and interaction risks.

Geometry evidence: pixel-layout.json.
Choice, row-select, ordering, stepped execution and editor views were compared before and after at 320, 390, 768 and 1366 pixels.
All 20 final comparisons had identical font, font size, padding and gaps, with no position or size differences above 0.5px.
All 20 checks returned no document horizontal overflow.

## Recorded browser click-through

| Exercise | Observed result |
|---|---|
| پیشگوی خروجی | Wrong answer displayed guidance; correct answer completed; repeat submission did not add XP. |
| کلید گمشده | Two native dropdowns selected out and submission completed. |
| جفت‌یاب | Four row dropdowns mapped correctly and completed. |
| کد درهم | Down/up controls worked; six upward moves restored the valid order and completed. |
| ضدنمونه | Zero-boundary choice completed. |
| قاضی کامپایلر | Private-access answer completed. |
| شکار باگ | Line 2 selection completed. |
| سبدبندی | Three mappings completed. |
| جراحی یک‌حرکتی | Private-set repair completed. |
| تست‌چین | Three checkboxes selected and accepted. |
| ردپای اجرا | Three answers entered; previous/next steps worked; submission completed. |
| کارآگاه کد | Early submit showed validation; two clues opened; third disabled; diagnosis completed. |
| مهندسی معکوس | Override option completed. |
| قراردادساز | Three modifier selectors completed. |
| اثر دومینو | Both affected calls selected and accepted. |
| Lesson and activity navigation | All three lessons and all fifteen game destinations opened. |
| Hint | Opened and closed without losing the answer. |
| Previous/next activity | Changed the activity and respected boundary disabled states. |
| Roadmap disclosure | Opened and closed; 44 entries counted. |
| Review queue | Mistake added; review returned to its exercise; correction cleared it; empty queue displayed a message. |
| Empty answer | Review and personal completion disabled. |
| AI pending/cancel | Pending text and cancel button appeared; cancel stopped the UI review state. |
| Personal completion | Saved the personal completion source. |
| Fixed rubric | Opened and displayed the authored criterion. |
| Keyboard | Tab reached the next focusable control; a visible brown outline was measured. |
| Data error | Corrupt progress was injected only into disposable tmpfs; the real error state rendered with readable text. |
| Reconnect dialog | Stopping only the test container opened it; square 360px dialog observed; Escape closed it. |
| Final disabled style | Opacity 1, dashed border, 44px height and not-allowed cursor observed in the final image. |

Final production browser console: no warnings or errors.
Temporary test containers were used for the intentional data and connection failures.
The final preview images are pixel-desktop.jpg and pixel-mobile.jpg.
Lighthouse, field Web Vitals and cross-browser performance were not measured.

## Hard gate

- R-02 PASS: no em dash in authored theme CSS or documentation.
- R-03 PASS: 20 final viewport/template overflow checks passed; original spacing and 44px controls retained.
- R-17 PASS: XP and counts still come from the unchanged application state and curriculum.
- R-18 PASS: no testimonials or people were added.
- R-23 PASS: user-approved mild pixel direction; no new logo, avatar, image, font asset or navigation.
- R-24 PASS: original real exercise destinations remain; no new links.
- R-25 PASS: contrast ratios: body 12.63:1, secondary text 6.17:1, disabled text 5.75:1, primary action 7.05:1, code 10.79:1, error 9.61:1; focus against paper 5.78:1.
- R-26 PASS: learning-control actions recorded above; native semantics and handlers unchanged.
- R-27 PASS: empty, pending and real error states exercised in isolated data.
- R-28 PASS: no FAQ.
- R-32 PASS: Tab focus visible; native reconnect dialog closed with Escape; native inputs retained.
- R-33 PASS: CSS edited directly using apply_patch; no stylesheet-rewriting scripts.
- R-34 PASS: one light theme, tested across the recorded breakpoints.
- R-35 PASS: Docker publish/run and browser click-through recorded above; final production console clean.
- R-36 PASS: no invented performance, security or customer claims.
- R-37 PASS: approved direction and design read declared before edits; DESIGN.md records the reasons.
- R-38 PASS: original curriculum and application counts retained; screenshots explicitly use preview data.
- R-39 PASS: original native controls, labels and landmarks retained; hover, active, selected, focus and disabled styling present; pointer and motion-reduction rules retained.
- R-40 PASS: no media, external fonts or third-party scripts added; no unmeasured performance score claimed.

## Purpose gate

- R-01 PASS: original paper/ink/teal palette retained for reading and primary-action hierarchy.
- R-04 PASS: no decorative icons added.
- R-06 PASS: Persian-reading font unchanged; monospace limited to original code surfaces.
- R-07 PASS: no grid or decorative background pattern.
- R-08 PASS: original movement controls retained; no decorative arrows.
- R-09 PASS: only actual completion and progress states.
- R-10 PASS: no glass or blur.
- R-12 PASS: hard external shadows limited to the work surface and primary action; inset strokes mark boundaries without changing dimensions.
- R-13 PASS: no glow.
- R-14 PASS: one actual work surface; no promotional card layout.
- R-19 PASS: no added animation; existing immediate press offset and reduced-motion rule retained.
- R-22 PASS: no illustration.

## Liveliness

- PASS: explicit ENERGY 1 / RHYTHM 1 / MOTION 1.
- PASS: calm appearance, fixed rhythm and no decorative motion match the dials.
- PASS: exercise surface remains the focal point.
- PASS: measured original whitespace and gaps retained.
- PASS: teal remains the deliberate action/progress accent.
- PASS: repeated square contours and inset frames provide the mild pixel motif.
- PASS: design read declared before generation.

## Craftsmanship and quality locks

- C-1 PASS: each major visual decision has a reason in DESIGN.md.
- C-2 PASS: click-through verified real learning actions.
- C-3 PASS: no sections or UI elements added.
- C-4 PASS: viewport, pending, error, disabled, keyboard and reconnect paths checked.
- C-5 PASS: no fabricated product claims.
- R-05 PASS: original content-led layout retained; uniform rhythm is the explicit user preference.
- R-11 PASS: square frames replace small radii consistently; native radio/checkbox shapes stay standard.
- R-15 PASS: original specific action labels retained.
- R-16 PASS: no marketing copy added.
- R-20 PASS: numbered programming exercises plus restrained square frames preserve the learning-workshop identity.
- R-21 PASS: fixed light theme is explicitly approved.
- R-29 PASS: same restrained palette; brown reserved for focus/error semantics.
- R-30 PASS: no visual clone or borrowed product assets.
- R-31 PASS: palette, layout, typography, spacing, frame and shadow purposes written down.

## Boundaries

Pixel treatment is on frames and controls. Persian text stays readable; native dropdown menus, radios and checkboxes remain browser controls.
No changes were made to scoring, model review or content.


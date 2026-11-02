# ILearn design direction

User direction: minimal Persian UI, one page, fixed dark theme, reusable controls, no decorative animation. The approved plan keeps a mild pixel treatment and uses local IranSans fonts.

ENERGY 1 / RHYTHM 1 / MOTION 1.

- Account forms use a narrow 520px reading column. Secondary account actions are tactile square pixel controls with a functional portrait, key or back glyph and a short supporting line; raised/pressed edges distinguish actions without decorative animation. The remember option is a native checkbox in a full clickable row. Existing colors and 1/1/1 dials remain unchanged.

- The user requested a pixel brand mark: an open book with code brackets ties the logo to learning programming. It uses the existing teal, ink and light text palette, and links to the public introduction.
- The user explicitly requested RightPixel's exact email template. Its table composition, dark blue/green email palette, monospace wordmark and code box are retained; only the project branding changes to ILearn. This email-specific direction does not change the app's visual system.
- Introduction copy explains the real current game and offers account entry. The lobby is an ordered list of actual subjects; chapter navigation appears only after selecting a subject.
- Profile and lobby navigation remain native links, styled as the same square buttons as actions. A block-pixel portrait glyph identifies the profile control without introducing an icon dependency.

- Dark green surfaces (#141B19 and #1C2723) follow the user's approved palette for extended code practice.
- Light text (#E9EFE9), muted text (#B3C1B7) and teal (#79CDB4) distinguish reading, supporting information and primary actions.
- Bundled IranSans Regular and Bold follow the user's typography request; monospace is reserved for code.
- One column and one work surface keep focus on the current problem and reduce layout variants; dimensions, spacing and breakpoints stay unchanged.
- Numbered challenges form the identity motif and show real progress.
- Comfortable line spacing and 44px controls support reading and touch.
- Square contours and inset strokes repeat a mild pixel motif without custom controls or changes to hit areas.
- The exercise surface has a 4px hard shadow to mark the main work area; the primary action has a 2px hard shadow that presses down on activation.
- The progress bar uses the existing dimensions and square ends to match the pixel motif; radio buttons, checkboxes and dropdowns remain native controls.
- Disabled buttons use readable ink and dashed borders instead of faded text, keeping contrast and dimensions intact.
- The user requested RightPixel's pixel avatars. Reuse its Canvas rectangles at 64px with smoothing disabled; the avatar identifies the active save. No new logo is generated.
- A topic lobby leads to three playable chapters. Unavailable roadmap chapters live in a native disclosure to keep the first choice short.
- The header shows Level and title; its single progress bar measures the 200 XP to the next title. This replaces competing score/level indicators.
- Player identity and progression live in a separate full-width top band, above page titles and chapter navigation. A slightly darker existing surface and one bottom border separate account state from learning content; the same component is used on the profile page. Responsive rows keep the avatar, name and actions readable without fixing the band over the exercise.
- The introduction occupies the same work surface before challenges, keeping prerequisite reading separate without a new route.
- A 12px label gap protects readability around the input focus ring. Native dark controls keep implementation small.

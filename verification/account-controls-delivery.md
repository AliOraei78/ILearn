# Account controls, persistent login and recovery

2026-11-01. Anti-slop during (same session selection), core rules. Design Read: simple Persian learning game, pixel account controls; ENERGY 1 / RHYTHM 1 / MOTION 1. DESIGN.md records why square pressable surfaces, functional glyphs and the narrow form column are used.

## Evidence

- Docker Release publish succeeded. 36 automated tests passed. Updated email-flow.cjs recorded 56 checks in email-browser.json with no page JavaScript errors, including the existing game/profile/email regression.
- Remember me: checked login returned a persistent cookie with at least 29 days remaining; unchecked login returned a session cookie. Profile changes preserve the current persistence choice.
- Recovery: local SMTP captured the recovery code; wrong code was rejected; correct code set the new password and returned to sign-in without automatic authentication. The previous persistent session was rejected; the recovered password signed in. Separate reset hashes/rows and the captured original session stamp prevent signup/reset crossover and stale resets; successful reset consumes the row.
- Login, register and forgotten-password pages each passed 320, 390, 768, 1366px overflow checks. Updated desktop/mobile screenshots visually inspected. Main app separately verified keyboard Space toggles the native remember checkbox with visible 3px focus, account links have no underline, recovery link opens its page and back action returns to login.
- Explicitly requested local test user ilearn_tester created with a random password and an independent save. Its credentials are only in ignored App_Data/test-account.json. Main-app browser signed in with that account and opened profile. No default password, HTTP seed endpoint or public verification bypass was added.

## Delivery gate

- R-01 PASS: existing green surfaces/teal accent, no gradient.
- R-02 PASS: new copy contains no em dash.
- R-03 PASS: twelve new account page/width checks and prior game checks passed; touch targets remain at least 44px.
- R-04 PASS: portrait means account, key means recovery, directional glyph means return. No icon library.
- R-05 PASS: form, remember option and two relevant secondary actions; no marketing/template filler.
- R-06 PASS: local IranSans retained; small supporting copy uses readable line spacing.
- R-07 PASS: no background pattern.
- R-08 PASS: return glyph has a documented navigation purpose, not a decorative CTA arrow.
- R-09 PASS: no marketing capsules.
- R-10 PASS: no glass effects.
- R-11 PASS: same square pixel contours.
- R-12 PASS: small hard shadow gives secondary controls a pressable edge; documented purpose.
- R-13 PASS: no glow.
- R-14 PASS: account actions reflect real account needs, no feature-card section.
- R-15 PASS: labels name account creation, recovery, sign-in and returning to lobby.
- R-16 PASS: natural Persian action text, no marketing buzzwords.
- R-17 PASS: 30-day description matches actual authentication expiry and tested cookie.
- R-18 PASS: no testimonials or invented public people.
- R-19 PASS: only hover/pressed feedback and functional existing resend timer.
- R-20 PASS: repeated pixel glyphs, square pressable edges and Persian learning-game voice.
- R-21 PASS: approved fixed dark theme preserved.
- R-22 PASS: no stock illustration.
- R-23 PASS: user requested creative simple controls and a dedicated test account; no new human likeness or identity asset.
- R-24 PASS: login, register, forgotten-password, reset-password and lobby targets exist.
- R-25 PASS: existing measured palette retained; text/muted/primary exceed AA as recorded in earlier delivery reports; no new colors.
- R-26 PASS: real account actions, remember cookie behavior, recovery, profile return and login tested.
- R-27 PASS: bad code, expired/missing recovery context, input errors and unavailable SMTP have explicit messages; form empty state is usable and native validation applies.
- R-28 PASS: no FAQ.
- R-29 PASS: no palette expansion.
- R-30 PASS: native project pixel design, no product clone.
- R-31 PASS: column width, glyphs, shadows and secondary-action treatment explained in DESIGN.md.
- R-32 PASS: native links/checkbox/forms; keyboard Space and 3px visible focus verified. No outline suppression.
- R-33 PASS: Razor/CSS edited directly; no rewriting helper.
- R-34 PASS: one approved dark theme.
- R-35 PASS: build, 36 tests, 56 browser checks, targeted deployed-app interaction and visual QA recorded.
- R-36 PASS: no certification/performance/customer claim.
- R-37 PASS: same explicit direction and 1/1/1 dials.
- R-38 PASS: helpers describe real behavior; test identity stays a private QA fixture.
- R-39 PASS: native semantics, accessible names, visible labels, cursor states and reduced-motion behavior retained; SVG glyphs decorative and hidden from assistive technology.
- R-40 PASS: SVGs reserve 20x20; no blocking third-party script or fabricated performance score.
- Liveliness PASS: 1/1/1, primary sign-in focus, structured secondary choices, teal functional accents and repeated pixel motif.
- C-1 PASS: design choices explained.
- C-2 PASS: meaningful behavior tested.
- C-3 PASS: each control serves an account task.
- C-4 PASS: responsive, keyboard, error and persistence states checked.
- C-5 PASS: only measured/implemented behavior claimed.

## Limits

Only installed Chrome was exercised. Real email delivery is still unconfigured: prior automatic approval review rejected reusing RightPixel SMTP credentials without explicit authorization. No transfer was retried. Recovery transport was tested with a local receiver only. Optional anti-slop add-on skill files remain absent; core rules were applied.

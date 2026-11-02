# RightPixel account mechanics in ILearn

2026-11-01. The user's request adds RightPixel's account behavior and exact email template while retaining ILearn's app appearance. Anti-slop during, same session selection; ENERGY 1 / RHYTHM 1 / MOTION 1 for the app. The separately requested email composition is documented in DESIGN.md.

## Behavior transferred

- Registration reserves an email/name/avatar profile without a password. One-time email confirmation issues a random, single-use 10-minute password grant. Initial password setup signs in; password replacement returns to login and revokes every prior session/grant.
- Passwords: 6–128 Unicode scalars, with letter, digit and punctuation/symbol; Identity V3 hasher with 220000 iterations. Successful login upgrades old hashes with a compare-and-update guard so it cannot overwrite a concurrent password change. Unknown accounts use a dummy hash verification.
- Login is email/password. Existing email-less accounts retain a separate legacy username login. Display names for email accounts need not be unique. Profile IDs, saves and the dedicated local test account are preserved.
- Server-backed sessions: seven-day absolute lifetime, 24-hour idle expiry, explicit revocation on logout, all-session revocation on password change. Remember changes cookie persistence, not the seven-day server maximum. Cookies are HttpOnly, SameSite Strict and non-sliding; localhost HTTP operation is preserved.
- Durable failed-login limits: five per identity or fifty per network in fifteen minutes. Auth request limit: sixty per network per minute. Successful login/reset clears identity failure attribution while retaining network counts, as in RightPixel.
- All code-send actions share the normalized email lock and deadline in Postgres. Time is read from the database after lock acquisition. Delivery history preserves the 120-second gate even after a code is consumed, and former flow deadlines are honored during migration. Five code failures, two-minute code expiry, five sends/email/hour and 100 sends/network/hour apply.
- HTML layout, colors, typography and copy exactly follow RightPixel's Verification.html, except brand names/wordmark are ILearn. Both HTML and plain-text MIME alternatives are sent. The build asserts the template is published.

## Evidence

- Docker Release build succeeded with zero compiler warnings/errors. 43 unit tests passed, including email normalization/injection rejection and the RightPixel password policy.
- 62 browser/integration checks passed with no page JavaScript errors. Exact list: rightpixel-auth-browser.json. Runner: rightpixel-auth.cjs (email-flow.cjs and accounts-flow.cjs delegate to it). Mail capture uses only local SMTP and an isolated database; no real email sent.
- Four simultaneous requests to one app sent exactly one email. Two live app instances sharing the same database also sent exactly one email. Changing from signup to recovery, uppercase email, code consumption, reload and app restart did not bypass the deadline. A new send succeeded only after the server deadline was advanced in the isolated time fixture.
- Captured HTML matched the published template, and that template matched the original RightPixel file after only brand substitution and newline normalization. email-preview.html/png contain sample code 123456, not a live code.
- Tested wrong/expired/consumed codes, weak password rejection, consumed/expired grants, initial sign-in, recovery sign-out, cookie replay after logout, all-session revocation, seven-day cookie, 24-hour idle expiry, seven-day server expiry, five/50 failed-login limits and five/100 sending limits. Existing 15 fixed games and nine draft editors passed.
- Legacy username login succeeded. A known 100000-iteration fixture hash upgraded to 220000 after login. Seven app pages passed widths 320, 390, 768, 1366; email preview passed 390/768. Screenshots inspected. Dedicated main-app test account still signed in by email and opened its existing profile.
- Original progress.json/.bak retained the same sizes and modification times. Authentication tables were migrated transactionally with a database schema lock; no progress rows or profile IDs were recreated.

## Delivery gate

- R-01 PASS: app retains green/teal; email uses the explicitly requested source palette.
- R-02 PASS: new authored UI copy has no em dash.
- R-03 PASS: 28 app page/width measurements and two email widths passed.
- R-04 PASS: existing functional pixel glyphs retained; no new icon library.
- R-05 PASS: account steps follow the requested workflow; game/lobby composition retained.
- R-06 PASS: app IranSans unchanged; email wordmark/code typography deliberately matches the requested template.
- R-07 PASS: no background pattern added.
- R-08 PASS: existing return glyph indicates real navigation.
- R-09 PASS: no marketing badges.
- R-10 PASS: no glass effects.
- R-11 PASS: existing square app contours retained.
- R-12 PASS: app's documented control/work-surface shadows unchanged; no email shadows added.
- R-13 PASS: no glow.
- R-14 PASS: no feature grid introduced.
- R-15 PASS: actions specify code confirmation, password setup, sign-in and recovery.
- R-16 PASS: no new marketing buzzwords.
- R-17 PASS: displayed lifetimes match implemented/tested limits.
- R-18 PASS: no testimonials or fictional public identities.
- R-19 PASS: functional resend timer and existing pressed states only.
- R-20 PASS: ILearn's pixel identity retained; source email reuse explicitly requested.
- R-21 PASS: approved dark app theme retained.
- R-22 PASS: no stock illustrations.
- R-23 PASS: user explicitly requested this email template and account workflow.
- R-24 PASS: /login, /legacy-login, /register, /verify, /set-password and recovery destinations work.
- R-25 PASS: app uses previously measured palette. Email contrast calculations: footer 6.14:1, supporting text 7.91:1 and code 13.53:1 on their actual backgrounds.
- R-26 PASS: account actions and all game/editor flows exercised as listed in the JSON evidence.
- R-27 PASS: missing/used/expired grant, wrong code, failed login, cooldown and SMTP/storage failure messages provided. Native required inputs and pending-account states remain usable.
- R-28 PASS: no FAQ.
- R-29 PASS: app palette unchanged; email colors are the owner's explicit template direction, with the expiry warning serving a functional role.
- R-30 PASS: email is an explicitly requested reuse; app layout was not copied.
- R-31 PASS: app/email typography, palette and workflow purposes recorded in DESIGN.md.
- R-32 PASS: native controls and visible focus retained; public account steps avoid automatic heading focus. Prior keyboard evidence remains applicable.
- R-33 PASS: source modified directly; no UI source-rewrite helper.
- R-34 PASS: one approved dark app theme.
- R-35 PASS: build, 43 tests, 62 recorded browser checks, production test-account smoke and visual email/app checks completed.
- R-36 PASS: no invented customer/security certification/performance claims.
- R-37 PASS: explicit direction plus explicit template reuse; no silent style change.
- R-38 PASS: authentic project content and the user-requested source template, no fabricated social proof.
- R-39 PASS: labelled native inputs/selects/forms, hidden decorative SVGs, Persian document metadata and existing focus/reduced-motion styles retained.
- R-40 PASS: image/glyph dimensions retained; template is static HTML with no scripts/media and no unmeasured performance scores claimed.
- Liveliness PASS: app 1/1/1 direction, one task per account step, structural whitespace and pixel identity retained.
- C-1 PASS: source reuse and retained app design are intentional/documented.
- C-2 PASS: account handlers have real tested behavior.
- C-3 PASS: each step serves registration, authentication or recovery.
- C-4 PASS: responsive states, concurrency, invalid/expired input and revocation exercised.
- C-5 PASS: reported outcomes backed by local test evidence.

## Operational limits

SMTP credentials from RightPixel remain untouched: earlier automatic approval review rejected credential transfer without explicit reuse approval. Local mail capture verified the transport format and behavior, not real-provider delivery. The existing app can still serve local games without SMTP; actual verification needs authorized SMTP configuration. Raw JSON endpoint shapes and deployment proxy/TLS settings were not copied: this app retains native forms and localhost operation. No user data was sent to a real mailbox by these tests.

Only installed Chrome was tested, not screen-reader audio or field performance. Optional anti-slop add-on files remain absent; core rules applied. Old pending signup/reset tables are retained only to preserve their prior send deadlines; new requests use the shared email challenge.

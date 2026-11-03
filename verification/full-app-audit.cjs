// Local browser audit. Accounts, email and model calls stay inside an isolated audit database and fixtures.
const fs = require('node:fs');
const path = require('node:path');
const http = require('node:http');
const assert = require('node:assert/strict');
const { createHash } = require('node:crypto');
const { spawn, execFileSync } = require('node:child_process');
const { chromium, expect } = require(process.env.PLAYWRIGHT_MODULE || 'E:/Products/RightPixel/src/rightpixel-web/node_modules/@playwright/test');
const { viewports, core, measure, enlarged } = require('./layout-checks.cjs');
const { server: smtp, messages } = require('./test-smtp.cjs')();
const channel = process.env.ILEARN_FULL_BROWSER || 'chrome';
const baseline = process.argv.includes('--baseline');
const uxOnly = process.argv.includes('--ux-only');
const base = 'http://127.0.0.1:5089', database = 'ilearn_full_audit';
const settings = Object.fromEntries(fs.readFileSync('.env', 'utf8').split(/\r?\n/).map(line => line.match(/^([A-Z_]+)=(.*)$/)).filter(Boolean).map(match => [match[1], match[2].replace(/^"|"$/g, '')]));
const lessons = JSON.parse(fs.readFileSync('ILearn/Content/csharp-v2.json', 'utf8'));
const samples = JSON.parse(fs.readFileSync('ILearn/Content/code-room.json', 'utf8'));
const exercises = [...samples, ...lessons.map(lesson => ({ Subject: 'csharp', Technology: 'C#', LessonId: lesson.Id, Template: 'implement', Activity: lesson.Activities.find(activity => activity.Kind === 'workshop') }))];
const interview = JSON.parse(fs.readFileSync('ILearn/Content/interviews.json', 'utf8'))[0];
const checks = [], layouts = [], errors = [], findings = [], visited = { exercises: [], questions: [] };
let app, browser, mode = 'accept', requests = 0;
smtp.on('connection', socket => socket.on('error', error => { if (error.code !== 'ECONNRESET') errors.push(error.message); }));
const model = http.createServer(async (request, response) => {
    try {
        let body = ''; for await (const chunk of request) body += chunk;
        const payload = JSON.parse(body); requests++;
        assert.equal(payload.model, 'nvidia/nemotron-3.5-super-vl:free');
        assert.equal(payload.provider.allow_fallbacks, false);
        const selected = mode;
        if (selected === 'delay') await new Promise(resolve => setTimeout(resolve, 1200));
        if (response.destroyed) return;
        if (selected === 'error') { response.writeHead(503).end('{}'); return; }
        const result = JSON.stringify({ score: selected === 'reject' ? 40 : 100, summary: 'بازخورد محلی برای آزمون رابط.', issues: selected === 'reject' ? ['پاسخ به اصلاح نیاز دارد.'] : [], nextHint: 'شرط مرزی را بررسی کن.' });
        response.writeHead(200, { 'content-type': 'application/json' });
        response.end(JSON.stringify({ choices: [{ message: { content: result } }] }));
    } catch (error) { errors.push('fixture: ' + error.message); response.writeHead(500).end('{}'); }
});
const record = name => { checks.push(name); console.log('PASS ' + name); };
const probe = (condition, issue) => { if (!condition) findings.push(issue); };
const key = item => `code-room/${item.Subject}/${item.LessonId}/${item.Activity.Id}`;

(async () => {
    const sql = (statement, targetDatabase = 'postgres') => {
        assert.ok(['postgres', database].includes(targetDatabase));
        return execFileSync('docker', ['exec', 'ilearn-postgres-1', 'psql', '-v', 'ON_ERROR_STOP=1', '-U', 'ilearn', '-d', targetDatabase, '-Atc', statement], { encoding: 'utf8', windowsHide: true }).trim();
    };
    if (sql(`SELECT count(*) FROM pg_database WHERE datname='${database}'`) === '0') sql(`CREATE DATABASE ${database}`);
    await new Promise(resolve => smtp.listen(2532, '127.0.0.1', resolve));
    await new Promise(resolve => model.listen(11441, '127.0.0.1', resolve));
    app = spawn('dotnet', [path.resolve('ILearn/bin/Debug/net10.0/ILearn.dll'), '--urls', base, '--contentRoot', path.resolve('ILearn')], {
        windowsHide: true,
        env: { ...process.env, ASPNETCORE_ENVIRONMENT: 'Development', ConnectionStrings__ILearn: `Host=127.0.0.1;Port=${settings.ILEARN_DB_PORT || 5441};Database=${database};Username=ilearn;Password=${settings.ILEARN_DB_PASSWORD};GssEncryptionMode=Disable`, Auth__CodeKey: settings.AUTH_CODE_KEY, EmailSettings__Host: '127.0.0.1', EmailSettings__Port: '2532', EmailSettings__EnableSsl: 'false', EmailSettings__UserName: '', EmailSettings__Password: '', EmailSettings__FromEmail: 'qa@example.test', OpenRouter__Url: 'http://127.0.0.1:11441/api/v1/chat/completions', OpenRouter__ApiKey: 'full-audit-local-fixture', Logging__LogLevel__Default: 'Warning' },
        stdio: ['ignore', 'pipe', 'pipe']
    });
    const log = fs.createWriteStream('verification/full-app-host.log'); app.stdout.pipe(log); app.stderr.pipe(log);
    for (let attempt = 0; ; attempt++) {
        try { if ((await fetch(base + '/login')).ok) break; } catch {}
        if (attempt >= 100 || app.exitCode !== null) throw new Error('Audit host did not start. See verification/full-app-host.log.');
        await new Promise(resolve => setTimeout(resolve, 200));
    }
    browser = await chromium.launch({ headless: true, channel });
    const context = await browser.newContext({ viewport: { width: 1440, height: 1000 } });
    const page = await context.newPage();
    page.on('pageerror', error => errors.push(error.message));
    page.on('response', response => { if (response.status() >= 400 && /\.(css|js|woff2|svg)(\?|$)/.test(response.url())) errors.push(response.status() + ' ' + response.url()); });
    const click = name => page.getByRole('button', { name, exact: true }).click();
    const saved = () => expect(page.locator('.code-room-workspace [role=status]').last()).toHaveText('ذخیره شد');
    async function room(item) {
        await page.goto(base + '/code-room?subject=' + item.Subject + '&exercise=' + encodeURIComponent(key(item)));
        await expect(page.locator('#code-problem-title')).toHaveText(item.Activity.Title);
        await expect(page.locator('#draft')).toBeVisible();
    }
    async function question(item) {
        await page.goto(base + '/interview?question=' + item.Activity.Id);
        await expect(page.locator('#interview-question-title')).toHaveText(item.Activity.Title);
        await expect(page.locator('#draft')).toBeVisible();
    }
    const screenshot = async name => {
        for (const width of [390, 1440]) {
            await page.setViewportSize({ width, height: 900 }); await page.evaluate(() => scrollTo(0, 0));
            await page.screenshot({ path: `verification/full-app-${baseline ? 'before' : 'after'}-${channel}-${name}-${width}.png`, fullPage: true });
        }
    };
    const accountFile = `ILearn/App_Data/full-app-${channel}.json`;
    if (fs.existsSync(accountFile)) {
        const account = JSON.parse(fs.readFileSync(accountFile, 'utf8'));
        await page.goto(base + '/login'); await page.locator('#email').fill(account.email); await page.locator('#password').fill(account.password); await click('وارد بازی شو');
    } else {
        const account = { email: `full-app-${channel}-${Date.now()}@example.test`, password: 'Audit-local-927!' };
        await page.goto(base + '/register'); await page.getByLabel('نام بازیکن', { exact: true }).fill('بررسی کامل رابط'); await page.getByLabel('ایمیل', { exact: true }).fill(account.email); await click('ارسال کد تأیید');
        await expect(page).toHaveURL(/\/verify$/); await expect.poll(() => messages.length).toBeGreaterThan(0);
        await page.locator('#code').fill(messages.at(-1).code); await click('تأیید ایمیل و ادامه'); await expect(page).toHaveURL(/\/set-password$/);
        await page.getByLabel('رمز تازه', { exact: true }).fill(account.password); await page.getByLabel('تکرار رمز تازه', { exact: true }).fill(account.password); await click('ذخیره رمز');
        await expect(page).toHaveURL(/\/lobby$/); fs.writeFileSync(accountFile, JSON.stringify(account));
    }
    await expect(page.getByRole('button', { name: /C# جامع/ })).toBeVisible();
    record('isolated registration/login using local SMTP');

    await room(samples.find(item => item.Template === 'repair'));
    probe(await page.locator('h1').innerText() === 'کُد روم', 'code room title does not use کُد روم');
    await screenshot('code-room');
    const prose = samples.find(item => item.Template === 'extract-contract');
    await room(prose);
    probe(await page.locator('.code-lines').count() === 0, 'prose editor has misleading code line numbers when text wraps');
    await page.locator('#draft').fill('این پاسخ یک متن توضیحی است. '.repeat(12)); await saved();
    await screenshot('prose');
    const redis = samples.find(item => item.Subject === 'redis');
    await room(redis);
    await page.getByRole('link', { name: 'شبیه‌ساز مصاحبه', exact: true }).click();
    await expect(page.locator('#interview-question')).toBeVisible();
    await page.getByRole('link', { name: /ک.?د روم/, exact: true }).click();
    await expect(page.locator('#draft')).toBeVisible();
    probe(await page.locator('#code-exercise').inputValue() === key(redis), 'returning to code room loses the saved Redis/SQL exercise');
    await page.goto(base + '/code-room?exercise=' + encodeURIComponent(key(redis)));
    await expect(page.locator('#draft')).toBeVisible();
    probe(await page.locator('#code-exercise').inputValue() === key(redis), 'code room exercise deep link without subject selects a different exercise');
    await page.goto(base + '/code-room?exercise=missing'); await expect(page.locator('#draft')).toBeVisible();
    probe(await page.locator('main').innerText().then(text => text.includes('تمرین درخواستی پیدا نشد')), 'invalid exercise fallback silently erases its recovery message');
    if (!baseline) {
        await page.goto(base + '/code-room?subject=missing'); await expect(page.locator('#draft')).toBeVisible();
        await expect(page.locator('#code-subject')).toHaveValue('csharp'); await expect(page.locator('main')).toContainText('موضوع درخواستی پیدا نشد');
        await room(redis); await page.goto(base + '/code-room?subject=csharp&exercise=missing'); await expect(page.locator('#draft')).toBeVisible();
        await expect(page.locator('main')).toContainText('تمرین درخواستی پیدا نشد');
        await expect(page.locator('#code-exercise')).toHaveAttribute('aria-describedby', 'selected-code-exercise');
    }
    record('return navigation, deep links, invalid exercise recovery and prose editor probes');

    const surfaces = new Set();
    for (const item of uxOnly ? [] : exercises) {
        await room(item);
        await expect(page.locator('.code-room-question')).toHaveText(item.Activity.Prompt);
        if (!baseline) {
            await page.locator('#draft').fill('پاسخ تازه برای سنجش نمایش معیار ' + key(item));
            await expect(page.locator('.code-room-workspace details').last()).toContainText('پس از ثبت');
            await expect(page.locator('.code-room-workspace details').last()).not.toContainText(item.Activity.Explanation);
        }
        await measure(page, 'room ' + key(item), layouts);
        if (!surfaces.has(item.Template)) {
            await measure(page, 'room ' + item.Template + ' all sizes', layouts, viewports); await enlarged(page, 'room ' + item.Template, layouts);
            for (const detail of await page.locator('.code-room-workspace details').all()) await detail.locator('summary').click();
            await measure(page, 'room ' + item.Template + ' expanded', layouts);
            surfaces.add(item.Template);
        }
        visited.exercises.push(key(item));
    }
    if (!uxOnly) record('all 135 code exercises; 12 templates at 46 standard/boundary sizes and 200% text');
    for (const item of uxOnly ? [] : interview.Questions) {
        await question(item);
        await expect(page.locator('.interview-workspace .code-room-question')).toHaveText(item.Activity.Prompt);
        if (!baseline) {
            await page.locator('#draft').fill('پاسخ تازه برای سنجش نمایش معیار ' + item.Activity.Id);
            await expect(page.locator('.interview-workspace details').nth(1)).toContainText('پس از ثبت');
            await expect(page.locator('.interview-workspace details').nth(1)).not.toContainText(item.Activity.Explanation);
        }
        await measure(page, 'interview ' + item.Activity.Id, layouts);
        await expect(page.locator('.interview-workspace details').nth(2)).not.toHaveAttribute('open', '');
        if (['C001', 'C017', 'C100'].includes(item.Activity.Id)) {
            for (const detail of await page.locator('.interview-workspace details').all()) await detail.locator('summary').click();
            await measure(page, 'interview ' + item.Activity.Id + ' expanded all sizes', layouts, viewports); await enlarged(page, 'interview ' + item.Activity.Id, layouts);
        }
        visited.questions.push(item.Activity.Id);
    }
    if (uxOnly) await question(interview.Questions.at(-1));
    await screenshot('interview');
    if (!uxOnly) record('all 100 interview questions; expanded code/references, responsive sizes and 200% text');
    if (!baseline) {
        const previousViewport = page.viewportSize();
        for (const width of [280, 320, 360, 390, 480, 600]) {
            await page.setViewportSize({ width, height: 844 });
            const previous = await page.locator('.interview-navigation button').first().boundingBox();
            const next = await page.locator('.interview-navigation button').last().boundingBox();
            const count = await page.locator('.interview-navigation > span').boundingBox();
            assert.ok(Math.abs(previous.width - next.width) < 1 && Math.abs(previous.y - next.y) < 1, 'mobile question controls must share a row and width');
            assert.ok(count.y + count.height <= previous.y, 'mobile question count belongs above the equal navigation controls');
        }
        await measure(page, 'balanced interview navigation', layouts, viewports); await enlarged(page, 'balanced interview navigation', layouts);
        await page.setViewportSize(previousViewport);
        record('interview previous/next controls have equal widths and a separate count on mobile');
    }
    const review = page.getByRole('button', { name: 'بررسی پاسخ با هوش مصنوعی', exact: true });
    const feedback = page.locator('.interview-workspace .feedback');
    await page.locator('#draft').fill('پاسخ مستقل برای آزمون بازخورد.'); await saved();
    mode = 'reject'; await review.click(); await expect(feedback).toContainText('40٪'); await expect(feedback).toHaveClass(/error/);
    mode = 'error'; await review.click(); await expect(feedback).toContainText('در دسترس نیست');
    mode = 'accept'; await review.click(); await expect(feedback).toContainText('100٪'); await expect(feedback).not.toHaveClass(/error/);
    if (!baseline) await expect(page.locator('.interview-workspace details').nth(1)).toContainText(interview.Questions.at(-1).Activity.Explanation);
    mode = 'delay'; const prior = requests; await review.click(); await expect.poll(() => requests).toBeGreaterThan(prior);
    await page.locator('#draft').fill('پاسخ تازه نباید نتیجهٔ بررسی قبلی را دریافت کند.'); await saved();
    await page.waitForTimeout(1300); await expect(feedback).toHaveCount(0); mode = 'accept';
    record('rejected, failed, accepted and stale review states preserve drafts');
    if (!baseline) {
        await room(samples.find(item => item.Template === 'repair'));
        await page.locator('#draft').fill('x'.repeat(15999)); await page.locator('#draft').press('Tab');
        await expect(page.locator('#draft')).toHaveValue('x'.repeat(15999));
        await page.locator('#draft').fill('a\nb');
        await page.locator('#draft').evaluate(field => field.setSelectionRange(0, field.value.length)); await page.locator('#draft').press('Tab');
        await expect(page.locator('#draft')).toHaveValue('    a\n    b'); await page.locator('#draft').press('Shift+Tab');
        await expect(page.locator('#draft')).toHaveValue('a\nb');
        await page.locator('#draft').press('Escape'); await page.locator('#draft').press('Tab'); await expect(page.locator('#draft')).not.toBeFocused();
        await saved();
        await page.evaluate(() => document.documentElement.style.fontSize = '200%');
        assert.equal(await page.locator('#draft').evaluate(field => getComputedStyle(field).fontSize), '32px');
        assert.equal(await page.locator('.code-lines').evaluate(field => getComputedStyle(field).fontSize), '32px');
        await page.evaluate(() => document.documentElement.style.fontSize = '');
        record('editor character limit, multiline indentation, keyboard escape and scalable line numbers');

        await page.setViewportSize({ width: 390, height: 844 });
        const menu = page.getByRole('button', { name: 'بخش‌های بازی', exact: true });
        await menu.click(); await expect(menu).toHaveAttribute('aria-expanded', 'true');
        await page.getByRole('link', { name: 'کُد روم', exact: true }).focus(); await page.keyboard.press('Escape');
        await expect(page.locator('#game-navigation')).not.toBeVisible(); await expect(menu).toBeFocused();
        await menu.click(); await measure(page, 'open mobile menu', layouts, core.filter(size => size.width < 901));
        await page.getByRole('link', { name: 'شبیه‌ساز مصاحبه', exact: true }).click(); await expect(page.locator('#draft')).toBeVisible();
        await expect(page.locator('#game-navigation')).not.toBeVisible();
        await page.setViewportSize({ width: 1440, height: 1000 });
        record('mobile navigation opens, closes on Escape with focus restored, and navigates');

        const returnUrl = page.url();
        for (const state of ['show', 'retrying', 'failed', 'paused', 'resume-failed']) {
            await page.evaluate(state => { const dialog = document.querySelector('#components-reconnect-modal'); dialog.className = 'components-reconnect-' + state; dialog.showModal(); }, state);
            await measure(page, 'interview reconnect ' + state, layouts, viewports); await enlarged(page, 'interview reconnect ' + state, layouts);
            await page.keyboard.press('Escape'); await expect(page.locator('#components-reconnect-modal')).not.toBeVisible();
        }
        await page.evaluate(() => { const dialog = document.querySelector('#components-reconnect-modal'); dialog.className = 'components-reconnect-failed'; dialog.showModal(); });
        await page.getByRole('button', { name: 'بارگذاری همین صفحه', exact: true }).click(); await expect(page.locator('#draft')).toBeVisible();
        assert.equal(page.url(), returnUrl);
        await page.evaluate(() => document.querySelector('#blazor-error-ui').style.display = 'block');
        await page.getByRole('button', { name: 'بارگذاری دوباره', exact: true }).click(); await expect(page.locator('#draft')).toBeVisible();
        assert.equal(page.url(), returnUrl);
        record('all reconnect states at standard sizes; recovery reloads the current interview URL');

        async function game(lessonId, kind) {
            const lesson = lessons.find(item => item.Id === lessonId), activity = lesson.Activities.find(item => item.Kind === kind);
            await page.goto(base + `/lobby?lesson=${lessonId}&activity=${lesson.Activities.indexOf(activity)}`);
            const start = page.getByRole('button', { name: /^(شروع|ادامه) چالش‌ها$/ });
            await expect(page.locator('#activity-title').or(start)).toBeVisible(); if (await start.count()) await start.click();
            await expect(page.locator('#activity-title')).toHaveText(activity.Title); await click('شروع مجدد دور');
            return activity;
        }
        const groupGame = await game('R005', 'groups');
        for (const card of groupGame.Puzzle.Groups[0]) await page.locator('.options input').nth(card).check();
        await click('بررسی گروه'); await expect(page.locator('.puzzle-controls')).toContainText('گروه درست کشف و قفل شد.');
        const undoSelection = page.getByRole('button', { name: 'برگشت آخرین انتخاب', exact: true });
        probe(await undoSelection.isDisabled(), 'group discovery leaves undo that can restore locked cards');
        if (await undoSelection.isEnabled()) {
            await undoSelection.click(); await expect(page.locator('.puzzle-controls')).toContainText('انتخاب برگشت');
            probe(await page.locator('.options input:disabled:checked').count() === 0, 'undo restores selected locked group cards and blocks further groups');
        }
        if (!findings.length) {
            const nextCard = groupGame.Puzzle.Groups[1][0]; await page.locator('.options input').nth(nextCard).check();
            await expect(undoSelection).toBeEnabled(); await undoSelection.click(); await expect(page.locator('.options input:checked')).toHaveCount(0);
            await page.reload(); await expect(page.locator('#activity-title')).toHaveText(groupGame.Title);
            await expect(page.locator('.puzzle-controls')).toContainText('گروه‌های کشف‌شده: 1'); await expect(undoSelection).toBeDisabled();
            const account = JSON.parse(fs.readFileSync(accountFile, 'utf8')); assert.match(account.email, /@example\.test$/);
            const oldSelection = JSON.stringify(groupGame.Puzzle.Groups[0].slice(0, 2).map(String));
            sql(`UPDATE progress SET data=jsonb_set(jsonb_set(data,'{Entries,csharp/R005/groups,Answers}','${oldSelection}'::jsonb),'{Entries,csharp/R005/groups,GameState,Undo}','[${oldSelection}]'::jsonb),revision=revision+1 WHERE profile_id=(SELECT id FROM profiles WHERE email='${account.email.replaceAll("'", "''")}')`, database);
            await page.reload(); await expect(page.locator('#activity-title')).toHaveText(groupGame.Title);
            await expect(page.locator('.options input:disabled:checked')).toHaveCount(0); await expect(undoSelection).toBeDisabled();
            await expect(page.locator('.puzzle-controls')).toContainText('گروه‌های کشف‌شده: 1');
        }
        await measure(page, 'group discovery and old undo recovery', layouts, viewports); await enlarged(page, 'group discovery and old undo recovery', layouts);
        const conflictGame = await game('R029', 'contradiction');
        for (const card of conflictGame.Puzzle.Cases[0].Input) await page.locator('.options input').nth(card).check();
        await click('بررسی تناقض'); await expect(page.locator('.puzzle-controls')).toContainText('مرحلهٔ ۲:');
        probe(await undoSelection.isDisabled(), 'contradiction stage two offers a stale selection undo that cannot undo this stage');
        await measure(page, 'contradiction second stage', layouts, viewports); await enlarged(page, 'contradiction second stage', layouts);
        await click('اصلاح مجموعهٔ انتخابی'); await expect(page.locator('.puzzle-controls')).toContainText('مرحلهٔ ۱:');
        record('group locking and contradiction stage changes keep undo consistent');

        const workshopIndex = lessons[0].Activities.findIndex(activity => activity.Kind === 'workshop');
        for (const route of ['/code-room?subject=csharp', `/lobby?lesson=R001&activity=${workshopIndex}`, '/interview?question=C017']) {
            await page.goto(base + route);
            const start = page.getByRole('button', { name: /^(شروع|ادامه) چالش‌ها$/ });
            await expect(page.locator('#draft').or(start)).toBeVisible(); if (await start.count()) await start.click();
            await expect(page.locator('#draft')).toBeVisible();
            const stale = await context.newPage(); await stale.goto(base + '/profile'); await expect(stale.locator('h1')).toHaveText('پروفایل من');
            await stale.evaluate(url => { const link = document.createElement('a'); link.href = url; link.id = 'audit-editor-link'; link.textContent = 'ویرایشگر آزمایش'; document.querySelector('.game-shell').append(link); }, page.url());
            await stale.locator('#audit-editor-link').click(); await expect(stale.locator('#draft')).toBeVisible();
            await stale.getByRole('link', { name: 'پروفایل من', exact: true }).click(); await expect(stale.locator('h1')).toHaveText('پروفایل من');
            await stale.goBack(); await expect(stale.locator('#draft')).toBeVisible();
            await page.locator('#draft').fill('نسخهٔ ذخیره‌شدهٔ تازه ' + Date.now());
            await expect(page.getByText('ذخیره شد', { exact: true }).last()).toBeVisible();
            const unsaved = 'متن تب قدیمی برای آزمون محافظ خروج ' + Date.now();
            await stale.locator('#draft').fill(unsaved); await expect(stale.locator('main [role=alert]')).toContainText('ذخیره نشد');
            let unload = false;
            stale.once('dialog', async dialog => { unload = dialog.type() === 'beforeunload'; await dialog.dismiss(); });
            await stale.evaluate(() => document.querySelector('#blazor-error-ui').style.display = 'block');
            await stale.getByRole('button', { name: 'بارگذاری دوباره', exact: true }).click({ noWaitAfter: true });
            await expect.poll(() => unload).toBe(true); await expect(stale.locator('#draft')).toHaveValue(unsaved);
            const beforeLink = stale.url();
            for (const linkName of [route.startsWith('/code-room') ? 'شبیه‌ساز مصاحبه' : 'کُد روم', 'پروفایل من', 'ILearn، معرفی بازی']) {
                await stale.getByRole('link', { name: linkName, exact: true }).click();
                await expect(stale.locator('#draft')).toHaveValue(unsaved); assert.equal(stale.url(), beforeLink);
            }
            let backUnload = false;
            stale.once('dialog', async dialog => { backUnload = dialog.type() === 'beforeunload'; await dialog.dismiss(); });
            try { await stale.goBack(); } catch (error) { if (!backUnload) throw error; }
            await expect(stale).toHaveURL(beforeLink); await expect(stale.locator('#draft')).toHaveValue(unsaved);
            try { await stale.goForward(); } catch (error) { if (!backUnload) throw error; }
            await expect(stale).toHaveURL(beforeLink); await expect(stale.locator('#draft')).toHaveValue(unsaved);
            await stale.close();
        }
        record('all three editors block reload, browser Back/Forward and sidebar/profile/brand links when saving conflicts');
        const popupPromise = context.waitForEvent('page');
        const beforePopup = page.url();
        await page.getByRole('link', { name: 'پروفایل من', exact: true }).click({ modifiers: ['Control'] });
        const popup = await popupPromise; await expect(popup.locator('h1')).toHaveText('پروفایل من');
        assert.equal(page.url(), beforePopup); await popup.close();
        record('modified clicks retain native open-in-new-tab behavior');
    }
    if (!baseline) assert.deepEqual(findings, []);
    assert.deepEqual(layouts.filter(result => result.findings.length), []); assert.deepEqual(errors, []);
})().catch(error => { errors.push(error.stack || error.message); process.exitCode = 1; }).finally(async () => {
    const hashes = Object.fromEntries(['ILearn/bin/Debug/net10.0/ILearn.dll', ...['csharp-v2', 'lessons', 'revisions', 'code-room', 'interviews'].map(name => `ILearn/Content/${name}.json`)].map(file => [file, createHash('sha256').update(fs.readFileSync(file)).digest('hex')]));
    const result = { completed: process.exitCode !== 1, baseline, uxOnly, channel, checks, findings, visited, hashes, layoutChecks: layouts.length, failedLayouts: layouts.filter(result => result.findings.length), errors, ai: 'Local deterministic fixture; no external model request.' };
    fs.writeFileSync(`verification/full-app-${baseline ? 'before' : 'after'}-${channel}${uxOnly ? '-ux' : ''}.json`, JSON.stringify(result, null, 2));
    console.log(JSON.stringify({ ...result, visited: { exercises: visited.exercises.length, questions: visited.questions.length } }));
    if (browser) await browser.close(); if (app && app.exitCode === null) app.kill();
    await Promise.all([new Promise(resolve => model.close(resolve)), new Promise(resolve => smtp.close(resolve))]);
});

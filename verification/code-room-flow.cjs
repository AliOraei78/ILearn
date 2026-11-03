const fs = require('node:fs');
const path = require('node:path');
const http = require('node:http');
const assert = require('node:assert/strict');
const { spawn } = require('node:child_process');
const { chromium, expect } = require(process.env.PLAYWRIGHT_MODULE || 'E:/Products/RightPixel/src/rightpixel-web/node_modules/@playwright/test');
const { measure, enlarged, core } = require('./layout-checks.cjs');
const base = process.env.ILEARN_CODE_ROOM_URL || 'http://127.0.0.1:5086', modelPort = 11439;
const samples = JSON.parse(fs.readFileSync('ILearn/Content/code-room.json', 'utf8'));
const account = JSON.parse(fs.readFileSync('ILearn/App_Data/test-account.json', 'utf8'));
assert.match(account.email, /@example\.test$/);
const settings = Object.fromEntries(fs.readFileSync('.env', 'utf8').split(/\r?\n/).map(line => line.match(/^([A-Z_]+)=(.*)$/)).filter(Boolean).map(match => [match[1], match[2].replace(/^"|"$/g, '')]));
const checks = [], layouts = [], errors = [], requests = [];
let mode = 'accept', app, browser;
const model = http.createServer(async (request, response) => {
    let body = ''; for await (const chunk of request) body += chunk;
    const payload = JSON.parse(body), context = JSON.parse(payload.messages[1].content);
    assert.equal(payload.model, 'nvidia/nemotron-3.5-super-vl:free');
    assert.equal(payload.provider.allow_fallbacks, false);
    requests.push({ technology: context.technology, template: context.template });
    const selected = mode;
    if (selected === 'delay') await new Promise(resolve => setTimeout(resolve, 2500));
    if (response.destroyed) return;
    if (selected === 'error') { response.writeHead(503).end('{}'); return; }
    const result = selected === 'invalid' ? 'invalid-json' : JSON.stringify({ score: selected === 'reject' ? 40 : 100, summary: selected === 'reject' ? 'شرط مرزی پاسخ نیاز به اصلاح دارد.' : 'پاسخ با معیار تمرین سازگار است.', issues: selected === 'reject' ? ['مرز بازه را بررسی کن.'] : [], nextHint: 'حالت مرزی را هم بررسی کن.' });
    response.writeHead(200, { 'content-type': 'application/json' });
    response.end(JSON.stringify({ choices: [{ message: { content: result } }] }));
});

(async () => {
    await new Promise(resolve => model.listen(modelPort, '127.0.0.1', resolve));
    app = spawn('dotnet', [path.resolve('ILearn/bin/Debug/net10.0/ILearn.dll'), '--urls', base, '--contentRoot', path.resolve('ILearn')], {
        windowsHide: true,
        env: { ...process.env, ASPNETCORE_ENVIRONMENT: 'Development', ConnectionStrings__ILearn: `Host=127.0.0.1;Port=${settings.ILEARN_DB_PORT || 5441};Database=ilearn;Username=ilearn;Password=${settings.ILEARN_DB_PASSWORD};GssEncryptionMode=Disable`, Auth__CodeKey: settings.AUTH_CODE_KEY, OpenRouter__Url: `http://127.0.0.1:${modelPort}/api/v1/chat/completions`, OpenRouter__ApiKey: 'code-room-test-fixture', Logging__LogLevel__Default: 'Warning' },
        stdio: ['ignore', 'pipe', 'pipe']
    });
    const appLog = fs.createWriteStream('verification/code-room-app.log');
    app.stdout.pipe(appLog); app.stderr.pipe(appLog);
    for (let attempt = 0; ; attempt++) {
        try { if ((await fetch(base + '/login')).ok) break; } catch {}
        if (attempt >= 100 || app.exitCode !== null) throw new Error('Code room test host did not become ready. See the local app log.');
        await new Promise(resolve => setTimeout(resolve, 200));
    }
    browser = await chromium.launch({ headless: true, channel: process.env.ILEARN_LIVE_BROWSER || 'chrome' });
    const page = await browser.newPage({ viewport: { width: 1440, height: 1000 } });
    page.on('pageerror', error => errors.push(error.message));
    page.on('response', response => { if (response.status() >= 400 && /\.(css|js|woff2|svg)(\?|$)/.test(response.url())) errors.push(response.status() + ' ' + response.url()); });
    const review = page.getByRole('button', { name: 'بررسی پاسخ با هوش مصنوعی', exact: true });
    const feedback = page.locator('.code-room-workspace .feedback');
    const xp = () => page.locator('.player-bar-progress label').innerText();
    const key = sample => `code-room/${sample.Subject}/${sample.LessonId}/${sample.Activity.Id}`;
    async function open(sample) {
        await page.goto(base + '/code-room?subject=' + sample.Subject + '&exercise=' + encodeURIComponent(key(sample)));
        await expect(page.locator('#code-problem-title')).toHaveText(sample.Activity.Title);
        await expect(page.locator('#draft')).toBeVisible();
    }
    async function saved() { await expect(page.locator('.code-room-workspace [role=status]').last()).toHaveText('ذخیره شد'); }
    await page.goto(base + '/login?returnUrl=%2Fcode-room%3Fsubject%3Dcsharp');
    await page.locator('#email').fill(account.email); await page.locator('#password').fill(account.password);
    await page.getByRole('button', { name: 'وارد بازی شو', exact: true }).click();
    await expect(page.locator('h1')).toHaveText('کُد روم');
    await expect(page.locator('#game-navigation .active')).toHaveText('کُد روم');
    await expect(page.locator('#code-template option')).toHaveCount(13);
    assert.equal(await page.locator('#code-chapter option').count(), 124);
    checks.push('authenticated room, shared header/sidebar, twelve templates and practice for all 123 chapters');

    for (const sample of samples) {
        await open(sample);
        await expect(page.locator('.code-room-meta')).toContainText(sample.Technology);
        await page.locator('#draft').fill('پاسخ تازه برای بررسی معیار ' + sample.Template); await saved();
        await page.getByText('معیار بررسی این تمرین', { exact: true }).click();
        await expect(page.locator('.code-room-question')).toHaveText(sample.Activity.Prompt);
        await expect(page.locator('details').last()).toContainText('پس از ثبت');
        await expect(page.locator('details').last()).not.toContainText(sample.Activity.Explanation);
        checks.push('problem, editor and non-spoiling criteria: ' + sample.Template);
    }
    const repair = samples.find(sample => sample.Template === 'repair');
    await open(repair);
    const priorDraft = await page.locator('#draft').inputValue();
    await page.locator('#draft').fill(''); if (priorDraft.length > 0) await saved();
    await page.getByRole('button', { name: 'شروع از کد مسئله', exact: true }).click();
    await expect(page.locator('#draft')).toHaveValue(repair.Activity.Code); await saved();
    await expect(page.getByRole('button', { name: 'شروع از کد مسئله', exact: true })).toBeDisabled();
    const before = await xp();
    mode = 'error'; await review.click(); await expect(feedback).toContainText('در دسترس نیست'); assert.equal(await xp(), before);
    mode = 'reject'; await review.click(); await expect(feedback).toContainText('40٪'); await expect(feedback).toHaveClass(/error/); assert.equal(await xp(), before);
    mode = 'accept'; await page.locator('#draft').fill(repair.Activity.SolutionCode); await saved(); await review.click();
    await expect(feedback).toContainText('100٪'); await expect(feedback).not.toHaveClass(/error/);
    const acceptedXp = await xp(); await review.click(); await expect(feedback).toContainText('100٪'); await expect(review).toBeEnabled(); assert.equal(await xp(), acceptedXp);
    checks.push('starter preserves existing work; service failure and incorrect answers add no XP; accepted answers do not duplicate XP');

    const normalize = samples.find(sample => sample.Template === 'infer-rule');
    await open(normalize); await page.locator('#draft').fill('first draft'); await saved();
    const requestCount = requests.length; mode = 'delay'; await review.click();
    await expect.poll(() => requests.length).toBeGreaterThan(requestCount);
    await page.locator('#draft').fill('newer draft'); await saved();
    await expect(page.locator('[aria-labelledby=code-answer-title]')).toHaveAttribute('aria-busy', 'false');
    await page.waitForTimeout(2600); await expect(feedback).toHaveCount(0);
    await page.reload(); await expect(page.locator('#draft')).toHaveValue('newer draft');
    checks.push('editing cancels pending AI; stale result ignored; autosaved draft survives reload');
    await page.locator('#draft').focus(); await page.locator('#draft').press('End'); await page.keyboard.press('Tab');
    await expect(page.locator('#draft')).toHaveValue('newer draft    ');
    await page.keyboard.press('Escape'); await page.keyboard.press('Tab'); await expect(page.locator('#draft')).not.toBeFocused();
    await saved(); checks.push('editor indentation and keyboard escape work');

    const redis = samples.find(sample => sample.Template === 'predict');
    await open(redis); await page.locator('#draft').fill(redis.Activity.SolutionCode); await saved(); mode = 'invalid';
    const invalidBefore = requests.length; await review.click(); await expect(feedback).toContainText('پاسخ معتبر نداد');
    await expect.poll(() => requests.length - invalidBefore).toBe(2); mode = 'accept'; await review.click(); await expect(feedback).toContainText('100٪'); await expect(review).toBeEnabled();
    const sql = samples.find(sample => sample.Template === 'optimize');
    await open(sql); await page.locator('#draft').fill(sql.Activity.SolutionCode); await saved(); await review.click(); await expect(feedback).toContainText('100٪');
    assert.ok(requests.some(request => request.technology === 'Redis' && request.template === 'predict'));
    assert.ok(requests.some(request => request.technology === 'PostgreSQL' && request.template === 'optimize'));
    checks.push('invalid AI schema retries once; SQL and Redis use their own technology/template context and the fixed free model');
    await page.locator('#code-subject').selectOption('csharp');
    await page.locator('#code-chapter').selectOption('R008');
    await page.locator('#code-template').selectOption('complete');
    await expect(page.locator('#code-problem-title')).toHaveText(samples.find(sample => sample.Template === 'complete').Activity.Title);
    checks.push('subject, chapter and template selectors navigate to matching practice');
    const lessons = JSON.parse(fs.readFileSync('ILearn/Content/csharp-v2.json', 'utf8'));
    const workshopIndex = lessons[0].Activities.findIndex(activity => activity.Kind === 'workshop');
    await page.goto(base + '/lobby?lesson=R001&activity=' + workshopIndex);
    const start = page.getByRole('button', { name: /^(شروع|ادامه) چالش‌ها$/ });
    if (await start.count()) await start.click();
    await expect(page.locator('#draft')).toBeVisible();
    await page.locator('#draft').fill('training draft stays in the academy');
    await page.getByRole('link', { name: 'کُد روم', exact: true }).click();
    await page.goto(base + '/code-room?subject=csharp&chapter=R001&template=implement');
    await expect(page.locator('#code-problem-title')).toBeVisible();
    await expect(page.locator('#draft')).not.toHaveValue('training draft stays in the academy');
    await page.locator('#draft').fill('room draft stays in the code room');
    await page.getByRole('link', { name: 'آموزش این فصل', exact: true }).click();
    await expect(page.locator('#draft')).toHaveValue('training draft stays in the academy');
    await page.getByRole('link', { name: 'کُد روم', exact: true }).click();
    await expect(page.locator('#draft')).toHaveValue('room draft stays in the code room');
    checks.push('navigation flushes pending drafts; academy and room drafts for the same chapter remain separate');
    const profileXp = await xp();
    await page.getByRole('link', { name: 'پروفایل من', exact: true }).click();
    await expect(page.locator('h1')).toHaveText('پروفایل من'); assert.equal(await xp(), profileXp);
    await measure(page, 'profile shared navigation', layouts);
    await open(sql);
    await measure(page, 'code room SQL', layouts, [...core, { width: 1100, height: 900 }, { width: 1101, height: 900 }]); await enlarged(page, 'code room', layouts);
    await page.setViewportSize({ width: 1440, height: 1000 }); await page.evaluate(() => window.scrollTo(0, 0)); await page.screenshot({ path: 'verification/code-room-desktop.png', fullPage: true });
    await page.setViewportSize({ width: 390, height: 844 }); await page.evaluate(() => window.scrollTo(0, 0)); await page.screenshot({ path: 'verification/code-room-mobile.png', fullPage: true });
    const toggle = page.getByRole('button', { name: 'بخش‌های بازی', exact: true });
    await expect(page.locator('#game-navigation')).not.toBeVisible(); await toggle.click(); await expect(toggle).toHaveAttribute('aria-expanded', 'true');
    await page.getByRole('link', { name: 'آموزشگاه و کارگاه', exact: true }).click(); await expect(page.locator('h1')).toHaveText('آموزشگاه و کارگاه');
    await page.getByRole('button', { name: 'بخش‌های بازی', exact: true }).click();
    await page.getByRole('link', { name: /شبیه‌ساز مصاحبه/ }).click(); await expect(page.locator('h1')).toHaveText('شبیه‌ساز مصاحبه'); await expect(page.locator('#interview-question')).toBeVisible();
    await measure(page, 'interview destination', layouts); checks.push('mobile navigation and interview question destination');
    await page.goto(base + '/code-room?subject=csharp&chapter=R001&template=predict');
    await expect(page.getByRole('heading', { name: 'تمرینی با این فیلترها پیدا نشد' })).toBeVisible();
    await page.getByRole('button', { name: 'نمایش همه تمرین‌های این موضوع' }).click(); await expect(page.locator('#code-problem-title')).toBeVisible();
    checks.push('empty filter state and recovery');
    const failedLayouts = layouts.filter(result => result.findings.length);
    fs.writeFileSync('verification/code-room-browser.json', JSON.stringify({ completed: failedLayouts.length === 0 && errors.length === 0, checks, requests, layoutChecks: layouts.length, layouts, errors, ai: 'Local deterministic fixture; no external model request' }, null, 2));
    assert.deepEqual(failedLayouts, []); assert.deepEqual(errors, []);
    console.log(JSON.stringify({ checks: checks.length, layoutChecks: layouts.length, modelRequests: requests.length, errors }));
})().catch(error => { console.error(error.stack || error.message); process.exitCode = 1; }).finally(async () => {
    if (browser) await browser.close();
    if (app && app.exitCode === null) app.kill();
    await new Promise(resolve => model.close(resolve));
});

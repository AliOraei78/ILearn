const fs = require('node:fs');
const path = require('node:path');
const http = require('node:http');
const assert = require('node:assert/strict');
const { spawn } = require('node:child_process');
const { chromium, expect } = require(process.env.PLAYWRIGHT_MODULE || 'E:/Products/RightPixel/src/rightpixel-web/node_modules/@playwright/test');
const { measure, enlarged, core } = require('./layout-checks.cjs');
const base = 'http://127.0.0.1:5088', modelPort = 11440;
const bank = JSON.parse(fs.readFileSync('ILearn/Content/interviews.json', 'utf8'))[0];
const account = JSON.parse(fs.readFileSync('ILearn/App_Data/test-account.json', 'utf8'));
assert.match(account.email, /@example\.test$/);
const settings = Object.fromEntries(fs.readFileSync('.env', 'utf8').split(/\r?\n/).map(line => line.match(/^([A-Z_]+)=(.*)$/)).filter(Boolean).map(match => [match[1], match[2].replace(/^"|"$/g, '')]));
const checks = [], layouts = [], errors = [], requests = [], liveReviews = [];
let mode = 'accept', app, browser;
const model = http.createServer(async (request, response) => {
    try {
        let body = ''; for await (const chunk of request) body += chunk;
        const payload = JSON.parse(body), context = JSON.parse(payload.messages[1].content);
        assert.equal(payload.model, 'nvidia/nemotron-3.5-super-vl:free');
        assert.equal(payload.provider.allow_fallbacks, false);
        assert.deepEqual(payload.provider.max_price, { prompt: 0, completion: 0, request: 0 });
        assert.equal(context.template, 'interview'); assert.equal(context.technology, 'C#');
        const question = bank.Questions.find(item => item.Activity.Id === context.activityId);
        assert.ok(question); assert.equal(context.rubric, question.Activity.Explanation);
        assert.equal(context.referenceAnswer, question.Activity.Solution);
        assert.match(context.templateDescription, /مصاحبه/);
        requests.push({ id: context.activityId, mode });
        const selected = mode;
        if (selected === 'live') {
            assert.ok(settings.OPENROUTER_API_KEY, 'No live key configured');
            const result = await fetch('https://openrouter.ai/api/v1/chat/completions', { method: 'POST', headers: { authorization: 'Bearer ' + settings.OPENROUTER_API_KEY, 'content-type': 'application/json' }, body, signal: AbortSignal.timeout(85000) });
            const raw = await result.text();
            let verdict; try { verdict = JSON.parse(JSON.parse(raw).choices[0].message.content); } catch {}
            liveReviews.push({ id: context.activityId, status: result.status, verdict });
            response.writeHead(result.status, { 'content-type': 'application/json' }); response.end(raw); return;
        }
        if (selected === 'delay') await new Promise(resolve => setTimeout(resolve, 1600));
        else await new Promise(resolve => setTimeout(resolve, 30));
        if (response.destroyed) return;
        if (selected === 'error') { response.writeHead(503).end('{}'); return; }
        const result = selected === 'invalid' ? 'invalid-json' : JSON.stringify({ score: selected === 'reject' ? 45 : 100, summary: selected === 'reject' ? 'پاسخ مفهوم اصلی را نادرست توضیح داده است.' : 'پاسخ تمام معیارهای همین سؤال را پوشش می‌دهد.', issues: selected === 'reject' ? ['تفاوت دو مفهوم را دقیق‌تر بیان کن.'] : [], nextHint: 'دلیل رفتاری را با مثال توضیح بده.' });
        response.writeHead(200, { 'content-type': 'application/json' }); response.end(JSON.stringify({ choices: [{ message: { content: result } }] }));
    } catch (error) { errors.push('model fixture: ' + error.message); response.writeHead(500).end('{}'); }
});

(async () => {
    await new Promise(resolve => model.listen(modelPort, '127.0.0.1', resolve));
    app = spawn('dotnet', [path.resolve('ILearn/bin/Debug/net10.0/ILearn.dll'), '--urls', base, '--contentRoot', path.resolve('ILearn')], {
        windowsHide: true,
        env: { ...process.env, ASPNETCORE_ENVIRONMENT: 'Development', ConnectionStrings__ILearn: `Host=127.0.0.1;Port=${settings.ILEARN_DB_PORT || 5441};Database=ilearn;Username=ilearn;Password=${settings.ILEARN_DB_PASSWORD};GssEncryptionMode=Disable`, Auth__CodeKey: settings.AUTH_CODE_KEY, OpenRouter__Url: `http://127.0.0.1:${modelPort}/api/v1/chat/completions`, OpenRouter__ApiKey: 'interview-test-fixture', Logging__LogLevel__Default: 'Warning' },
        stdio: ['ignore', 'pipe', 'pipe']
    });
    const appLog = fs.createWriteStream('verification/interview-app.log'); app.stdout.pipe(appLog); app.stderr.pipe(appLog);
    for (let attempt = 0; ; attempt++) {
        try { if ((await fetch(base + '/login')).ok) break; } catch {}
        if (attempt >= 100 || app.exitCode !== null) throw new Error('Interview test host did not become ready. See the local app log.');
        await new Promise(resolve => setTimeout(resolve, 200));
    }
    browser = await chromium.launch({ headless: true, channel: process.env.ILEARN_LIVE_BROWSER || 'chrome' });
    const context = await browser.newContext({ viewport: { width: 1440, height: 1000 } });
    const page = await context.newPage();
    page.on('dialog', dialog => dialog.accept());
    page.on('pageerror', error => errors.push(error.message));
    page.on('response', response => { if (response.status() >= 400 && /\.(css|js|woff2|svg)(\?|$)/.test(response.url())) errors.push(response.status() + ' ' + response.url()); });
    const review = page.getByRole('button', { name: 'بررسی پاسخ با هوش مصنوعی', exact: true });
    const feedback = page.locator('.interview-workspace .feedback');
    async function submitReview() {
        const before = requests.length; await review.click();
        await expect.poll(() => requests.length).toBeGreaterThan(before); await expect(review).toBeEnabled();
    }
    const saved = () => expect(page.locator('.interview-workspace [role=status]').last()).toHaveText('ذخیره شد');
    const xp = () => page.locator('.player-bar-progress label').innerText();
    async function open(id) {
        await page.goto(base + '/interview?subject=csharp&question=' + id);
        await expect(page.locator('#interview-question-title')).toHaveText(bank.Questions.find(item => item.Activity.Id === id).Activity.Title);
    }
    await page.goto(base + '/interview?subject=csharp&question=C017');
    await expect(page.locator('#email')).toBeVisible();
    await page.locator('#email').fill(account.email); await page.locator('#password').fill(account.password);
    await page.getByRole('button', { name: 'وارد بازی شو', exact: true }).click();
    await expect(page.locator('#interview-question-title')).toHaveText(bank.Questions[16].Activity.Title);
    assert.equal(await page.locator('#interview-question option').count(), 100);
    assert.equal(await page.locator('#interview-category option').count(), 11);
    assert.equal(await page.locator('#interview-subject option').count(), 1);
    assert.equal(await page.locator('#game-navigation small').count(), 0);
    checks.push('authentication returns to the requested question; 100 questions, ten categories and shared navigation');

    for (const question of bank.Questions) {
        await page.locator('#interview-question').selectOption(question.Activity.Id);
        await expect(page.locator('#interview-question-title')).toHaveText(question.Activity.Title);
        await expect(page.locator('.interview-workspace .code-room-question')).toHaveText(question.Activity.Prompt);
        await expect(page.locator('#draft')).toHaveAttribute('dir', 'rtl');
        const details = page.locator('.interview-workspace details');
        await expect(details.nth(2)).not.toHaveAttribute('open', '');
        await page.locator('#draft').fill('پاسخ تازه برای بررسی معیار ' + question.Activity.Id); await saved();
        await details.nth(1).locator('summary').click(); await expect(details.nth(1)).toContainText('پس از ثبت');
        await expect(details.nth(1)).not.toContainText(question.Activity.Explanation);
        await details.nth(2).locator('summary').click(); await expect(details.nth(2)).toContainText(question.Activity.Solution);
        await details.nth(3).locator('summary').click();
        const urls = await details.nth(3).locator('a').evaluateAll(links => links.map(link => link.href));
        assert.deepEqual(urls.sort(), bank.Sources.filter(source => question.SourceIds.includes(source.Id)).map(source => source.Url).sort());
    }
    checks.push('all 100 actual questions render their own prompt, hidden reference, rubric, RTL answer field and exact source links');
    await expect(page.getByRole('button', { name: 'سؤال بعدی', exact: true })).toBeDisabled();
    await page.getByRole('button', { name: 'سؤال قبلی', exact: true }).click(); await expect(page.locator('#interview-question')).toHaveValue('C099');
    await page.locator('#interview-question').selectOption('C001');
    await expect(page.getByRole('button', { name: 'سؤال قبلی', exact: true })).toBeDisabled();
    const priorDraft = await page.locator('#draft').inputValue();
    await page.locator('#draft').fill(''); if (priorDraft.length) await saved(); await expect(review).toBeDisabled();
    await page.locator('#draft').fill('   '); await saved(); await expect(review).toBeDisabled();
    const beforeXp = await xp();
    await page.locator('#draft').fill(bank.Questions[0].Activity.Solution); await saved();
    mode = 'reject'; await submitReview(); await expect(feedback).toContainText('45٪'); await expect(feedback).toHaveClass(/error/);
    mode = 'accept'; await submitReview(); await expect(feedback).toContainText('100٪'); await expect(feedback).not.toHaveClass(/error/);
    await expect(page.locator('.interview-workspace')).toContainText('قبلاً با هوش مصنوعی تأیید شده');
    const completion = await page.locator('#interview-progress').innerText();
    await submitReview(); assert.equal(await page.locator('#interview-progress').innerText(), completion);
    assert.equal(await xp(), beforeXp);
    checks.push('boundary navigation, empty/whitespace validation, incorrect/correct verdicts and completion without altering XP');

    mode = 'error'; await submitReview(); await expect(feedback).toContainText('در دسترس نیست');
    await expect(page.locator('#draft')).toHaveValue(bank.Questions[0].Activity.Solution);
    mode = 'invalid'; const invalidBefore = requests.length; await submitReview(); await expect.poll(() => requests.length - invalidBefore).toBe(2); await expect(feedback).toContainText('پاسخ معتبر نداد');
    mode = 'accept'; await submitReview(); await expect(feedback).toContainText('100٪');
    checks.push('service failure and invalid schema preserve the answer; one schema retry; user can retry successfully');

    mode = 'delay'; let priorRequests = requests.length; await review.click(); await expect.poll(() => requests.length).toBeGreaterThan(priorRequests);
    await page.locator('#draft').fill('پاسخ جدید باید جایگزین بررسی قدیمی شود.'); await saved();
    await expect(page.locator('[aria-labelledby=interview-answer-title]')).toHaveAttribute('aria-busy', 'false');
    await page.waitForTimeout(1700); await expect(feedback).toHaveCount(0);
    await page.reload(); await expect(page.locator('#draft')).toHaveValue('پاسخ جدید باید جایگزین بررسی قدیمی شود.');
    priorRequests = requests.length; await review.click(); await expect.poll(() => requests.length).toBeGreaterThan(priorRequests);
    await page.getByRole('button', { name: 'لغو بررسی', exact: true }).click(); await expect(review).toBeEnabled();
    await page.waitForTimeout(1700); await expect(feedback).toHaveCount(0);
    priorRequests = requests.length; await review.click(); await expect.poll(() => requests.length).toBeGreaterThan(priorRequests);
    await page.getByRole('button', { name: 'سؤال بعدی', exact: true }).click(); await expect(page.locator('#interview-question')).toHaveValue('C002');
    await page.waitForTimeout(1700); await expect(feedback).toHaveCount(0);
    mode = 'accept'; checks.push('edits, cancel and navigation discard stale AI responses; autosaved answers survive reload');

    await page.locator('#draft').fill('پاسخ دوم مستقل است.');
    await page.getByRole('button', { name: 'سؤال قبلی', exact: true }).click();
    await expect(page.locator('#draft')).toHaveValue('پاسخ جدید باید جایگزین بررسی قدیمی شود.');
    await page.getByRole('button', { name: 'سؤال بعدی', exact: true }).click(); await expect(page.locator('#draft')).toHaveValue('پاسخ دوم مستقل است.');
    await page.getByRole('link', { name: 'کُد روم', exact: true }).click(); await expect(page.locator('#draft')).not.toHaveValue('پاسخ دوم مستقل است.');
    await page.getByRole('link', { name: 'شبیه‌ساز مصاحبه', exact: true }).click(); await expect(page.locator('#draft')).toHaveValue('پاسخ دوم مستقل است.');
    await page.locator('#draft').focus(); await page.keyboard.press('Tab'); await expect(page.locator('#draft')).not.toBeFocused();
    checks.push('immediate navigation saves drafts; questions and code room remain independent; normal Tab exits the answer field');

    await page.locator('#interview-category').selectOption(bank.Questions[0].Category);
    await expect(page.locator('#interview-question option')).toHaveCount(10);
    await page.locator('#interview-level').selectOption('پیشرفته');
    await expect(page.getByRole('heading', { name: 'سؤالی با این فیلترها پیدا نشد', exact: true })).toBeVisible();
    await measure(page, 'empty interview filter', layouts);
    await page.getByRole('button', { name: 'نمایش همه سؤال‌های این موضوع', exact: true }).click(); await expect(page.locator('#interview-question option')).toHaveCount(100);
    await page.locator('#interview-level').selectOption('پیشرفته'); await expect(page.locator('#interview-question option')).toHaveCount(bank.Questions.filter(item => item.Level === 'پیشرفته').length);
    await page.goto(base + '/interview?question=missing'); await expect(page.locator('#interview-question')).toHaveValue('C001');
    await expect(page.locator('.interview-workspace')).toContainText('سؤال درخواستی پیدا نشد');
    checks.push('category and difficulty filters, empty recovery and invalid-question fallback');

    await open('C017');
    for (const item of await page.locator('.interview-workspace details').all()) await item.locator('summary').click();
    await measure(page, 'interview question with code and sources', layouts, [...core, { width: 1100, height: 900 }, { width: 1101, height: 900 }]);
    await enlarged(page, 'interview', layouts);
    await page.setViewportSize({ width: 1440, height: 1000 }); await page.evaluate(() => window.scrollTo(0, 0)); await page.screenshot({ path: 'verification/interview-desktop.png', fullPage: true });
    await page.setViewportSize({ width: 390, height: 844 }); await page.evaluate(() => window.scrollTo(0, 0)); await page.screenshot({ path: 'verification/interview-mobile.png', fullPage: true });
    const toggle = page.getByRole('button', { name: 'بخش‌های بازی', exact: true });
    await toggle.click(); await page.getByRole('link', { name: 'پروفایل من', exact: true }).click(); await expect(page.locator('h1')).toHaveText('پروفایل من');
    await page.getByRole('button', { name: 'بخش‌های بازی', exact: true }).click(); await page.getByRole('link', { name: 'شبیه‌ساز مصاحبه', exact: true }).click();
    await expect(page.locator('#interview-question')).toHaveValue('C017');
    checks.push('mobile menu and profile return to the saved interview question; layout and enlarged text');

    await page.setViewportSize({ width: 1440, height: 1000 });
    const stale = await page.context().newPage(); stale.on('dialog', dialog => dialog.accept());
    await stale.goto(base + '/interview?question=C017'); await expect(stale.locator('#draft')).toBeVisible();
    const validDraft = 'نسخهٔ ذخیره‌شدهٔ معتبر برای بررسی تداخل تب‌ها ' + Date.now();
    await page.locator('#draft').fill(validDraft); await saved();
    await stale.locator('#draft').fill('نوشتهٔ ذخیره‌نشده در تب قدیمی');
    await expect(stale.locator('main [role=alert]')).toContainText('ذخیره نشد');
    await stale.getByRole('link', { name: 'کُد روم', exact: true }).click(); await expect(stale.locator('h1')).toHaveText('شبیه‌ساز مصاحبه');
    await expect(stale.locator('#draft')).toHaveValue('نوشتهٔ ذخیره‌نشده در تب قدیمی');
    await stale.close(); await page.reload(); await expect(page.locator('#draft')).toHaveValue(validDraft);
    checks.push('concurrent-save conflict keeps unsaved text and blocks internal navigation without overwriting the newer save');

    if (process.env.ILEARN_INTERVIEW_LIVE === '1') {
        for (const id of ['C002','C017','C032','C041','C053','C064','C066','C078','C088','C100']) {
            await open(id); await page.locator('#draft').fill(bank.Questions.find(item => item.Activity.Id === id).Activity.Solution); await saved();
            mode = 'live'; await review.click(); await expect(review).toBeEnabled({ timeout: 100000 });
            console.log(JSON.stringify({ liveQuestion: id, ...liveReviews.at(-1) }));
            if (liveReviews.at(-1)?.status !== 200) break;
        }
        mode = 'accept';
    }
    assert.deepEqual(layouts.filter(item => item.findings.length), []); assert.deepEqual(errors, []);
    console.log(JSON.stringify({ checks: checks.length, renderedQuestions: 100, layoutChecks: layouts.length, modelRequests: requests.length, liveReviews: liveReviews.length, errors }));
})().catch(error => { console.error(error.stack || error.message); process.exitCode = 1; }).finally(async () => {
    fs.writeFileSync('verification/interview-browser.json', JSON.stringify({ completed: process.exitCode !== 1, checks, renderedQuestions: checks.some(check => check.startsWith('all 100')) ? 100 : 0, layouts, requests, liveReviews, errors, ai: 'Deterministic local fixture; live requests only with ILEARN_INTERVIEW_LIVE=1' }, null, 2));
    if (browser) await browser.close(); if (app && app.exitCode === null) app.kill(); await new Promise(resolve => model.close(resolve));
});

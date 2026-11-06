// Opt-in live check: only generated public questions reach the fixed free provider.
// Never print the configured key or the original exercise payload.
const fs = require('node:fs');
const path = require('node:path');
const assert = require('node:assert/strict');
const { execFileSync } = require('node:child_process');
const { chromium, expect: baseExpect } = require(process.env.PLAYWRIGHT_MODULE || 'E:/Products/RightPixel/src/rightpixel-web/node_modules/@playwright/test');
const expect = baseExpect.configure({ timeout: 100000 });
if (!process.argv.includes('--synthetic-free')) throw new Error('Explicit --synthetic-free opt-in is required.');
const settings = Object.fromEntries(fs.readFileSync(path.join(__dirname, '../.env'), 'utf8').split(/\r?\n/).map(line => line.match(/^([A-Z_]+)=(.*)$/)).filter(Boolean).map(match => [match[1], match[2].replace(/^"|"$/g, '')]));
const key = settings.OPENROUTER_API_KEY;
if (!key) throw new Error('A configured OpenRouter key is required.');
const redact = value => String(value).replaceAll(key, '[redacted]');
const adb = process.env.ILEARN_ADB || 'C:/Users/ASUS/AppData/Local/Android/Sdk/platform-tools/adb.exe';
const run = (...args) => execFileSync(adb, ['-s', 'emulator-5556', ...args], { encoding: 'utf8', windowsHide: true, timeout: 30000 }).trim();
const endpoint = 'https://openrouter.ai/api/v1/chat/completions';
const report = { completed: false, date: new Date().toISOString(), syntheticDataOnly: true, requests: [], checks: [], errors: [] };
const explanationOnly = process.argv.includes('--explanation-only');
if (explanationOnly) {
    const previous = JSON.parse(fs.readFileSync(path.join(__dirname, 'android-live-ai-report.json'), 'utf8'));
    assert.equal(previous.requests.length, 4);
    assert.ok(previous.requests.every(item => item.status === 200 && item.cost === 0));
    report.requests = previous.requests; report.checks = previous.checks; report.continuedFrom = previous.date;
}
let browser, page, mode = 'correct', credentialsSet = false;
const record = name => { report.checks.push(name); console.log('PASS ' + name); };
async function go(url) {
    await page.evaluate(url => { const a = document.createElement('a'); a.href = url; document.body.append(a); a.click(); a.remove(); }, url);
    await expect(page).toHaveURL('https://appassets.androidplatform.net' + url);
}
async function grade(selector) {
    await page.locator('#draft').fill(mode === 'incorrect' ? '5' : '4');
    const previous = report.requests.length;
    await page.getByRole('button', { name: 'بررسی پاسخ با هوش مصنوعی', exact: true }).click();
    await expect(page.locator(selector)).toContainText(mode === 'incorrect' ? 'غلط' : 'درست؛');
    await expect(page.getByRole('button', { name: 'بررسی پاسخ با هوش مصنوعی', exact: true })).toBeEnabled();
    assert.ok(report.requests.length > previous);
    const response = report.requests.at(-1);
    assert.equal(response.status, 200);
    assert.equal(response.cost, 0);
    if (mode === 'incorrect') assert.ok(response.review.score < 70);
    else assert.equal(response.review.score, 100);
}
(async () => {
    assert.equal(run('shell', 'getprop', 'ro.product.model'), 'Android SDK built for x86_64');
    await expect.poll(() => /Active default network: (?!none)/.test(run('shell', 'dumpsys', 'connectivity')), { timeout: 30000 }).toBe(true);
    const pid = run('shell', 'pidof', 'ir.ilearn.offline');
    run('forward', 'tcp:9223', 'localabstract:webview_devtools_remote_' + pid);
    browser = await chromium.connectOverCDP('http://127.0.0.1:9223', { noDefaults: true });
    const context = browser.contexts()[0]; page = context.pages()[0];
    page.on('pageerror', error => report.errors.push(redact(error.message)));
    page.on('requestfailed', request => { if (request.url() === endpoint) report.errors.push(redact(request.failure()?.errorText || 'Provider request failed')); });
    const responses = [];
    page.on('response', response => {
        if (response.url() !== endpoint || response.request().method() !== 'POST') return;
        responses.push((async () => {
            const body = await response.json();
            const item = { status: response.status(), cost: body.usage?.cost, model: body.model };
            if (body.choices?.[0]?.message?.content) {
                const content = body.choices[0].message.content;
                try { item.review = JSON.parse(content); } catch { item.explanation = content; }
            }
            if (body.error) item.error = { code: body.error.code, message: redact(body.error.message).slice(0, 300) };
            report.requests.push(item);
        })());
    });
    await context.route(endpoint, async route => {
        if (route.request().method() === 'OPTIONS') return route.continue();
        const request = route.request(), payload = request.postDataJSON();
        if (request.headers().authorization !== 'Bearer ' + key) throw new Error('Authorization header mismatch.');
        assert.equal(payload.model, 'nvidia/nemotron-3.5-super-vl:free');
        assert.equal(payload.provider.allow_fallbacks, false);
        assert.deepEqual(payload.provider.max_price, { prompt: 0, completion: 0, request: 0 });
        const reviewing = !!payload.response_format;
        payload.messages = reviewing ? [
            { role: 'system', content: 'این یک آزمون عمومی و ساختگی است. پاسخ صحیح 100 و پاسخ نادرست کمتر از 70 می‌گیرد. خروجی فقط JSON با score، summary فارسی، issues فارسی و nextHint فارسی باشد.' },
            { role: 'user', content: JSON.stringify({ question: 'خروجی Console.WriteLine(2 + 2) در C# چیست؟', referenceAnswer: '4', studentAnswer: mode === 'incorrect' ? '5' : '4', rubric: 'فقط عدد 4 پاسخ درست است.' }) }
        ] : [
            { role: 'system', content: 'پاسخ عمومی ساختگی را کوتاه و با فارسی ساده توضیح بده.' },
            { role: 'user', content: 'چرا Console.WriteLine(2 + 2) در C# عدد 4 را چاپ می‌کند؟ فقط همین مثال ساده را توضیح بده.' }
        ];
        await route.continue({ postData: JSON.stringify(payload) });
    });
    await page.locator('.player-bar-identity strong').waitFor({ timeout: 45000 });
    await go('/profile');
    await page.locator('#api-key').fill(key);
    credentialsSet = true;
    await page.getByRole('button', { name: 'ذخیره کلید', exact: true }).click();
    await expect(page.getByText('کلید روی این دستگاه تنظیم شده است.', { exact: true })).toBeVisible();
    if (!explanationOnly) {
    await go('/code-room');
    await grade('.code-room-workspace .feedback'); record('live AI accepts a correct synthetic answer inside the APK');
    mode = 'incorrect'; await grade('.code-room-workspace .feedback'); record('live AI rejects an incorrect synthetic answer inside the APK');
    mode = 'correct'; await go('/interview'); await grade('.interview-workspace .feedback'); record('live interview grading parses and displays the provider response');
    await go('/lobby?lesson=R001&activity=3');
    const start = page.getByRole('button', { name: /^(شروع|ادامه) چالش‌ها$/ }); if (await start.count()) await start.click();
    await grade('.work > .feedback'); record('live academy grading parses and displays the provider response');
    }
    const roadmap = JSON.parse(fs.readFileSync(path.join(__dirname, '../ILearn/Content/csharp-v2.json'), 'utf8'));
    const progress = await page.evaluate(async () => JSON.parse(await ilearnOffline.read('progress')));
    const chapter = roadmap.find(item => item.Activities.some(activity => activity.Kind === 'workshop' && !progress?.Entries?.[`csharp/${item.Id}/${activity.Id}`]?.FullAnswer));
    if (!chapter) throw new Error('Use a fresh QA installation after all workshop explanations have been cached.');
    const workshop = chapter.Activities.findIndex(item => item.Kind === 'workshop');
    await go('/lobby?lesson=' + chapter.Id + '&activity=' + workshop);
    const begin = page.getByRole('button', { name: /^(شروع|ادامه) چالش‌ها$/ }); if (await begin.count()) await begin.click();
    await page.getByRole('button', { name: 'پاسخ کامل', exact: true }).click();
    await page.getByRole('button', { name: 'توضیح ساده‌تر با هوش مصنوعی', exact: true }).click();
    await expect(page.locator('#reference-answer [role=status]')).toContainText('توضیح مدل:');
    await Promise.all(responses);
    assert.equal(report.requests.length, 5);
    assert.ok(report.requests.every(item => item.status === 200 && item.cost === 0));
    assert.match(report.requests.at(-1).explanation, /[\u0600-\u06ff]/);
    record('live simpler explanation works; all five provider requests report zero cost');
    await page.evaluate(() => ilearnOffline.setApiKey(''));
    run('shell', 'cmd', 'connectivity', 'airplane-mode', 'enable'); run('shell', 'svc', 'wifi', 'disable');
    await page.reload();
    await page.getByRole('button', { name: 'پاسخ کامل', exact: true }).click();
    await expect(page.locator('#reference-answer [role=status]')).toContainText('توضیح مدل:');
    assert.equal(await page.evaluate(() => ilearnOffline.hasApiKey()), false);
    record('the real model explanation remains available after reload in airplane mode without a stored key');
    run('shell', 'screencap', '-p', '/sdcard/ilearn-live-ai.png');
    run('pull', '/sdcard/ilearn-live-ai.png', path.join(__dirname, 'android-live-ai.png'));
    report.completed = true;
})().catch(error => { report.failure = redact(error.stack || error.message); console.error(report.failure); process.exitCode = 1; }).finally(async () => {
    try {
        if (page && credentialsSet) {
            await page.evaluate(() => ilearnOffline.setApiKey(''));
            report.credentialsRemoved = !await page.evaluate(() => ilearnOffline.hasApiKey());
        }
        if (browser) await browser.close();
        run('shell', 'cmd', 'connectivity', 'airplane-mode', 'enable'); run('shell', 'svc', 'wifi', 'disable');
        report.returnedToAirplaneMode = run('shell', 'settings', 'get', 'global', 'airplane_mode_on') === '1';
    } catch (error) { report.cleanupFailure = redact(error.message); process.exitCode = 1; }
    fs.writeFileSync(path.join(__dirname, 'android-live-ai-report.json'), JSON.stringify(report, null, 2));
    console.log(JSON.stringify({ completed: report.completed, checks: report.checks.length, responses: report.requests.map(item => ({ status: item.status, cost: item.cost })), credentialsRemoved: report.credentialsRemoved, errors: report.errors }));
});

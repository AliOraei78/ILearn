// Isolated HTTPS app and synthetic SMTP/model fixtures only. No production credentials or provider traffic.
const assert = require('node:assert/strict');
const fs = require('node:fs');
const http = require('node:http');
const path = require('node:path');
const { chromium, expect } = require(process.env.PLAYWRIGHT_MODULE || 'E:/Products/RightPixel/src/rightpixel-web/node_modules/@playwright/test');
const mail = require('./test-smtp.cjs')();
mail.server.on('connection', socket => socket.on('error', error => { if (error.code !== 'ECONNRESET') throw error; }));
const base = process.env.ILEARN_SECURITY_URL || 'https://127.0.0.1:5090';
assert.equal(new URL(base).hostname, '127.0.0.1');
const checks = [], errors = [];
let modelRequests = 0;
const model = http.createServer((request, response) => {
    let data = '';
    request.on('data', chunk => data += chunk);
    request.on('end', () => {
        const body = JSON.parse(data); modelRequests++;
        assert.equal(body.model, 'nvidia/nemotron-3.5-super-vl:free');
        assert.equal(body.provider.allow_fallbacks, false);
        assert.deepEqual(body.provider.max_price, { prompt: 0, completion: 0, request: 0 });
        const content = body.response_format ? JSON.stringify({ score: 100, summary: 'پاسخ ساختگی برای آزمون امنیت.', issues: [], nextHint: 'ادامه بده.' }) : 'توضیح فارسی آزمایشی.';
        response.writeHead(200, { 'content-type': 'application/json' }).end(JSON.stringify({ choices: [{ message: { content } }] }));
    });
});
const listen = (server, port) => new Promise(resolve => server.listen(port, '127.0.0.1', resolve));
const record = name => { checks.push(name); console.log('PASS ' + name); };
(async () => {
    await listen(mail.server, 2529); await listen(model, 11439);
    const browser = await chromium.launch({ channel: 'chrome', headless: true });
    let completed = false;
    try {
        const context = await browser.newContext({ ignoreHTTPSErrors: true });
        const page = await context.newPage();
        page.on('pageerror', error => errors.push(error.message));
        page.on('dialog', dialog => { errors.push('Unexpected script dialog'); dialog.dismiss(); });
        let response = await page.goto(base + '/login');
        const headers = response.headers();
        assert.equal(response.status(), 200);
        assert.equal(headers['x-content-type-options'], 'nosniff');
        assert.equal(headers['x-frame-options'], 'DENY');
        assert.equal(headers['referrer-policy'], 'no-referrer');
        assert.equal(headers['cache-control'], 'no-store');
        assert.ok(!headers.server);
        assert.ok(headers['content-security-policy'].includes("frame-ancestors 'none'"));
        const nonce = await page.locator('script[type=importmap]').evaluate(node => node.nonce);
        assert.ok(nonce && headers['content-security-policy'].includes("'nonce-" + nonce + "'"));
        record('HTTPS login sends CSP nonce, no-store, anti-framing and privacy headers');
        response = await context.request.get('http://127.0.0.1:5089/login'); assert.equal(response.status(), 400);
        response = await context.request.get(base + '/login', { headers: { Host: 'evil.example' } }); assert.equal(response.status(), 400);
        response = await context.request.get(base + '/login', { headers: { Host: 'qa.ilearn.test' } }); assert.ok(response.headers()['strict-transport-security']);
        record('Production rejects HTTP and unapproved hosts; public HTTPS emits HSTS');
        for (const file of ['/.env', '/appsettings.json', '/App_Data/keys', '/ILearn.Android/keys/password.txt', '/Content/csharp-v2.json']) {
            response = await context.request.get(base + file); assert.equal(response.status(), 404, file);
        }
        record('Secrets, server configuration, keys and source content are not public static files');
        const token = () => page.locator('input[name=__RequestVerificationToken]').first().inputValue();
        const post = async (action, form) => context.request.post(base + '/auth/' + action, { form: { __RequestVerificationToken: await token(), ...form }, maxRedirects: 0 });
        for (const action of ['login', 'register', 'forgot', 'resend', 'verify', 'password', 'profile', 'logout']) {
            response = await context.request.post(base + '/auth/' + action, { form: { email: 'csrf@example.test' }, maxRedirects: 0 });
            assert.equal(response.status(), 400, action);
        }
        response = await post('login', { __RequestVerificationToken: 'forged', email: 'security-player@example.test' }); assert.equal(response.status(), 400);
        response = await post('login', { email: 'x'.repeat(70000), password: 'invalid' }); assert.ok([400, 413].includes(response.status()), 'body limit: ' + response.status());
        response = await context.request.post(base + '/auth/login', { form: { email: 'x'.repeat(33000) }, headers: { RequestVerificationToken: await token() }, maxRedirects: 0 });
        assert.ok([400, 413].includes(response.status()), 'value limit: ' + response.status());
        record('Every account mutation rejects absent/forged CSRF tokens; oversized form is rejected');
        await page.goto(base + '/profile'); await page.waitForURL('**/login?**');
        record('Anonymous profile access redirects to login');
        response = await post('login', { email: 'security-player@example.test', password: 'Quiet river birds sing 93!', returnUrl: '//evil.example' });
        assert.equal(response.status(), 302); assert.equal(response.headers().location, '/lobby');
        await page.goto(base + '/profile');
        await expect(page.locator('.player-bar-identity strong')).toHaveText('<img src=x onerror=alert(1)>');
        assert.equal(await page.locator('img[src=x]').count(), 0);
        const sessionCookies = await context.cookies();
        for (const cookie of sessionCookies.filter(cookie => /AspNetCore|ILearn\./.test(cookie.name))) {
            assert.equal(cookie.secure, true); assert.equal(cookie.httpOnly, true); assert.equal(cookie.sameSite, 'Strict');
        }
        record('Real login uses secure HttpOnly Strict cookies, blocks external return URLs and encodes stored HTML');
        await page.evaluate(() => {
            window.__securityViolations = [];
            document.addEventListener('securitypolicyviolation', event => window.__securityViolations.push(event.violatedDirective));
            const script = document.createElement('script'); script.textContent = 'window.__securityInjected=true'; document.body.append(script);
            const frame = document.createElement('iframe'); frame.srcdoc = '<script>parent.__securityFrame=true<\/script>'; document.body.append(frame);
        });
        await expect.poll(() => page.evaluate(() => window.__securityViolations.length)).toBeGreaterThan(0);
        assert.equal(await page.evaluate(() => window.__securityInjected || window.__securityFrame || false), false);
        record('Enforced CSP prevents injected scripts and srcdoc frames from executing');
        await page.goto(base + '/code-room');
        await page.locator('#draft').fill('Synthetic security test answer.');
        await page.getByRole('button', { name: 'بررسی پاسخ با هوش مصنوعی', exact: true }).click();
        await expect(page.locator('.code-room-workspace .feedback, .room-workspace .feedback, .work > .feedback').first()).toContainText('پاسخ ساختگی', { timeout: 30000 });
        assert.equal(modelRequests, 1);
        record('Authenticated Blazor still saves and grades through the fixed free model fixture under CSP');
        await page.goto(base + '/profile');
        response = await post('logout', {}); assert.equal(response.status(), 302);
        const replay = await browser.newContext({ ignoreHTTPSErrors: true }); await replay.addCookies(sessionCookies);
        response = await replay.request.get(base + '/profile', { maxRedirects: 0 }); assert.equal(response.status(), 302);
        const tampered = sessionCookies.map(cookie => cookie.name.includes('.Cookies') ? { ...cookie, value: cookie.value.slice(0, -2) + 'xx' } : cookie);
        await replay.clearCookies(); await replay.addCookies(tampered);
        response = await replay.request.get(base + '/profile', { maxRedirects: 0 }); assert.equal(response.status(), 302);
        await replay.close();
        record('Logout revokes replayed cookies; forged authentication cookies cannot access profiles');
        await page.goto(base + '/login');
        response = await post('login', { email: 'legacy-player@example.test', password: 'Abcd1!' });
        assert.equal(response.status(), 302); assert.equal(response.headers().location, '/forgot-password?weak=true');
        response = await context.request.get(base + '/profile', { maxRedirects: 0 }); assert.equal(response.status(), 302);
        await page.goto(base + '/forgot-password?weak=true'); await expect(page.getByRole('alert')).toContainText('رمز قبلی');
        record('A correct legacy weak password cannot create a session and must be replaced through email proof');
        await page.goto(base + '/register');
        const email = 'browser-security-' + Date.now() + '@example.test';
        await page.locator('#name').fill('Security browser QA'); await page.locator('#email').fill(email);
        await page.locator('form[action="/auth/register"] button.primary').click(); await page.waitForURL('**/verify');
        await expect.poll(() => mail.messages.find(message => message.recipient.includes(email))?.code).toMatch(/^\d{6}$/);
        await page.locator('#code').fill(mail.messages.find(message => message.recipient.includes(email)).code);
        await page.locator('form[action="/auth/verify"] button').click(); await page.waitForURL('**/set-password');
        const grant = (await context.cookies()).find(cookie => cookie.name === 'ILearn.PasswordGrant');
        assert.ok(grant.secure && grant.httpOnly && grant.sameSite === 'Strict');
        response = await post('password', { password: 'Abcd1!', confirmation: 'Abcd1!' }); assert.equal(response.status(), 302); assert.ok(response.headers().location.includes('message='));
        await page.reload(); assert.equal(await page.locator('#set-password').getAttribute('minlength'), '15');
        await page.locator('#set-password').fill('A private long phrase 29!'); await page.locator('#set-confirmation').fill('A private long phrase 29!');
        await page.locator('form[action="/auth/password"] button.primary').click(); await page.waitForURL('**/lobby');
        await expect(page.locator('.player-bar-identity strong')).toHaveText('Security browser QA');
        await page.goto(base + '/profile'); await post('logout', {}); await page.goto(base + '/login');
        await context.addCookies([grant]); response = await post('password', { password: 'Another private phrase 74!', confirmation: 'Another private phrase 74!' });
        assert.ok(response.headers().location.includes('error=expired'));
        record('Registration, email proof and 15-character password work; consumed password grants cannot be reused');
        let limited = false;
        for (let i = 0; i < 65; i++) { response = await post('unknown', {}); if (response.status() === 429) { limited = true; break; } }
        assert.ok(limited); record('Authentication endpoint rate limit returns HTTP 429');
        assert.deepEqual(errors, []); completed = true;
    } finally {
        fs.writeFileSync(path.join(__dirname, 'security-web-report.json'), JSON.stringify({ completed, date: new Date().toISOString(), checks, errors, modelFixtureRequests: modelRequests }, null, 2));
        console.log(JSON.stringify({ completed, checks: checks.length, errors, modelFixtureRequests: modelRequests }));
        await browser.close(); mail.server.close(); model.close();
    }
})().catch(error => { console.error(error); process.exitCode = 1; });

// Dedicated Docker QA stack, HTTPS loopback, synthetic seeded account. No SMTP/provider calls.
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const { chromium, expect } = require(process.env.PLAYWRIGHT_MODULE || 'E:/Products/RightPixel/src/rightpixel-web/node_modules/@playwright/test');
const { execFileSync } = require('node:child_process');
const base = 'https://127.0.0.1:5093', checks = [], errors = [];
const record = name => { checks.push(name); console.log('PASS ' + name); };
(async () => {
    const browser = await chromium.launch({ channel: 'chrome', headless: true });
    const context = await browser.newContext({ ignoreHTTPSErrors: true });
    const page = await context.newPage(); page.on('pageerror', error => errors.push(error.message));
    let completed = false;
    try {
        const config = JSON.parse(execFileSync('docker', ['inspect', 'ilearn-security-qa-ilearn-1'], { encoding: 'utf8', windowsHide: true }))[0];
        assert.equal(config.Config.User, '1654'); assert.equal(config.HostConfig.ReadonlyRootfs, true);
        assert.deepEqual(config.HostConfig.CapDrop, ['ALL']); assert.ok(config.HostConfig.SecurityOpt.includes('no-new-privileges:true'));
        assert.equal(execFileSync('docker', ['exec', 'ilearn-security-qa-ilearn-1', 'stat', '-c', '%a', '/app/App_Data/keys'], { encoding: 'utf8', windowsHide: true }).trim(), '700');
        assert.ok(!config.Config.Env.some(value => /^(PGPASSWORD|ILEARN_DB_PASSWORD|POSTGRES_PASSWORD)=/.test(value)));
        record('Actual Linux container is non-root, read-only, capability-free; key directory is 0700 and no administrator DB credential is injected');
        for (const headers of [{}, { 'X-Forwarded-Proto': 'https' }]) {
            const response = await context.request.get('http://127.0.0.1:5091/login', { headers }); assert.equal(response.status(), 400);
        }
        record('Linux rejects insecure HTTP and forged forwarded scheme from an untrusted peer');
        let response = await page.goto(base + '/login'); assert.equal(response.status(), 200);
        assert.ok(response.headers()['content-security-policy'].includes("frame-ancestors 'none'"));
        assert.equal(response.headers()['cache-control'], 'no-store');
        await page.locator('#email').fill('security-player@example.test'); await page.locator('#password').fill('Quiet river birds sing 93!');
        await page.getByRole('button', { name: 'وارد بازی شو', exact: true }).click(); await page.waitForURL('**/lobby');
        await expect(page.locator('.player-bar-identity strong')).toHaveText('<img src=x onerror=alert(1)>');
        for (const cookie of (await context.cookies()).filter(cookie => cookie.name.startsWith('.AspNetCore.'))) {
            assert.ok(cookie.secure && cookie.httpOnly && cookie.sameSite === 'Strict');
        }
        await page.goto(base + '/code-room'); await page.locator('#draft').fill('Linux read-only container save test.');
        await expect(page.locator('.code-room-workspace [role=status]').last()).toHaveText('ذخیره شد');
        await page.reload(); await expect(page.locator('#draft')).toHaveValue('Linux read-only container save test.');
        record('Linux HTTPS login, encoded profile and private draft persistence work with the restricted DB role');
        await page.goto(base + '/profile');
        const token = await page.locator('input[name=__RequestVerificationToken]').first().inputValue();
        response = await context.request.post(base + '/auth/profile', { form: { name: 'x' } }); assert.equal(response.status(), 400);
        response = await context.request.post(base + '/auth/profile', { form: { name: 'x'.repeat(70000), __RequestVerificationToken: token } }); assert.ok([400, 413].includes(response.status()));
        response = await context.request.post(base + '/auth/logout', { form: { __RequestVerificationToken: token }, maxRedirects: 0 }); assert.equal(response.status(), 302);
        response = await context.request.get(base + '/profile', { maxRedirects: 0 }); assert.equal(response.status(), 302);
        record('Linux CSRF, request-body limit and server-side logout enforcement pass');
        assert.deepEqual(errors, []); completed = true;
    } finally {
        fs.writeFileSync(path.join(__dirname, 'security-linux-report.json'), JSON.stringify({ completed, checks, errors, date: new Date().toISOString() }, null, 2));
        console.log(JSON.stringify({ completed, checks: checks.length, errors })); await browser.close();
    }
})().catch(error => { console.error(error); process.exitCode = 1; });

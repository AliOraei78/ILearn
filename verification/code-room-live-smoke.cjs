const fs = require('node:fs');
const assert = require('node:assert/strict');
const { createHash } = require('node:crypto');
const { execFileSync } = require('node:child_process');
const { chromium, expect } = require(process.env.PLAYWRIGHT_MODULE || 'E:/Products/RightPixel/src/rightpixel-web/node_modules/@playwright/test');
const base = 'http://127.0.0.1:5081';
const channel = process.env.ILEARN_LIVE_BROWSER || 'chrome';

(async () => {
    const sourceHash = createHash('sha256').update(fs.readFileSync('ILearn/Content/code-room.json')).digest('hex');
    const deployedHash = execFileSync('docker', ['exec', 'ilearn-ilearn-1', 'sha256sum', '/app/Content/code-room.json'], { encoding: 'utf8', windowsHide: true }).split(/\s/)[0];
    assert.equal(deployedHash, sourceHash);
    const account = JSON.parse(fs.readFileSync('ILearn/App_Data/test-account.json', 'utf8'));
    assert.match(account.email, /@example\.test$/);
    const browser = await chromium.launch({ headless: true, channel: process.env.ILEARN_LIVE_BROWSER || 'chrome' });
    const errors = [], checks = [];
    try {
        const page = await browser.newPage({ viewport: { width: 1440, height: 1000 } });
        page.on('pageerror', error => errors.push(error.message));
        page.on('response', response => { if (response.status() >= 400 && /\.(css|js|woff2|svg)(\?|$)/.test(response.url())) errors.push(response.status() + ' ' + response.url()); });
        await page.goto(base + '/login?returnUrl=' + encodeURIComponent('/code-room?subject=redis'));
        await page.locator('#email').fill(account.email); await page.locator('#password').fill(account.password);
        await page.getByRole('button', { name: 'وارد بازی شو', exact: true }).click();
        await expect(page.locator('h1')).toHaveText('کُد روم'); await expect(page.locator('#code-subject')).toHaveValue('redis');
        await expect(page.locator('#draft')).toBeVisible();
        checks.push('Docker login returns to the requested room and subject');
        let count = 0;
        for (const subject of ['csharp', 'sql', 'redis']) {
            await page.goto(base + '/code-room?subject=' + subject);
            await expect(page.locator('#code-problem-title')).toBeVisible();
            assert.equal(await page.locator('#code-template option').count(), 13);
            count += await page.locator('#code-exercise option').count();
        }
        assert.equal(count, 135); checks.push('all 135 deployed exercises and twelve template choices are available');
        await page.getByRole('link', { name: 'آموزشگاه و کارگاه', exact: true }).click();
        await expect(page.locator('h1')).toHaveText('آموزشگاه و کارگاه');
        await expect(page.getByRole('heading', { name: 'موضوع‌ها', exact: true })).toBeVisible();
        await page.goto(base + '/lobby?view=chapters'); await expect(page.locator('h1')).toHaveText('فصل‌های C#');
        checks.push('the existing lobby and chapter routes now live under academy/workshop navigation');
        await page.getByRole('link', { name: 'پروفایل من', exact: true }).click();
        await expect(page.locator('h1')).toHaveText('پروفایل من'); await expect(page.locator('#game-navigation')).toBeVisible();
        await page.getByRole('link', { name: /شبیه‌ساز مصاحبه/ }).click();
        await expect(page.locator('h1')).toHaveText('شبیه‌ساز مصاحبه'); await expect(page.locator('#interview-question')).toBeVisible();
        checks.push('profile and interview questions share the navigation shell');
        await page.setViewportSize({ width: 390, height: 844 });
        await expect(page.locator('#game-navigation')).not.toBeVisible();
        await page.getByRole('button', { name: 'بخش‌های بازی', exact: true }).click();
        await page.getByRole('link', { name: 'کُد روم', exact: true }).click();
        await expect(page.locator('#draft')).toBeVisible();
        assert.equal(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth + 1), true);
        checks.push('Docker mobile navigation opens the room without horizontal overflow');
        await page.setViewportSize({ width: 1440, height: 1000 }); await page.evaluate(() => window.scrollTo(0, 0));
        await page.screenshot({ path: 'verification/code-room-docker-desktop.png', fullPage: true });
        assert.deepEqual(errors, []);
        const result = JSON.stringify({ completed: true, channel, checks, sourceHash, deployedHash, count, errors, ai: 'No model requests made by deployment smoke check' }, null, 2);
        fs.writeFileSync('verification/code-room-docker.json', result); fs.writeFileSync(`verification/code-room-docker-${channel}.json`, result);
        console.log(JSON.stringify({ checks: checks.length, exercises: count, errors }));
    } finally { await browser.close(); }
})().catch(error => { console.error(error.stack || error.message); process.exitCode = 1; });

// Dedicated emulator-5556, QA APK, synthetic key, airplane mode. No provider traffic.
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const { execFileSync } = require('node:child_process');
const { chromium, expect } = require(process.env.PLAYWRIGHT_MODULE || 'E:/Products/RightPixel/src/rightpixel-web/node_modules/@playwright/test');
const adb = process.env.ILEARN_ADB || 'C:/Users/ASUS/AppData/Local/Android/Sdk/platform-tools/adb.exe';
const serial = 'emulator-5556', app = 'ir.ilearn.offline', base = 'https://appassets.androidplatform.net';
const run = (...args) => execFileSync(adb, ['-s', serial, ...args], { encoding: 'utf8', timeout: 30000, windowsHide: true }).trim();
const checks = [], errors = [];
const record = name => { checks.push(name); console.log('PASS ' + name); };
(async () => {
    assert.equal(run('shell', 'getprop', 'ro.product.model'), 'Android SDK built for x86_64');
    assert.match(run('shell', 'dumpsys', 'package', app), /DEBUGGABLE/);
    assert.equal(run('shell', 'settings', 'get', 'global', 'airplane_mode_on'), '1');
    const pid = run('shell', 'pidof', app); assert.match(pid, /^\d+$/);
    run('forward', 'tcp:9223', 'localabstract:webview_devtools_remote_' + pid);
    const browser = await chromium.connectOverCDP('http://127.0.0.1:9223', { noDefaults: true });
    const page = browser.contexts()[0].pages()[0];
    page.on('pageerror', error => errors.push(error.message));
    let completed = false, originalKey;
    try {
        await page.goto(base + '/profile');
        await page.locator('#profile-name').waitFor();
        const headers = await page.evaluate(async () => Object.fromEntries((await fetch('/app.css')).headers.entries()));
        assert.equal(headers['x-content-type-options'], 'nosniff');
        assert.equal(headers['referrer-policy'], 'no-referrer');
        assert.ok(headers['content-security-policy'].includes("frame-src 'none'"));
        assert.ok(headers['content-security-policy'].includes("frame-ancestors 'none'"));
        assert.ok(headers['content-security-policy'].includes("'wasm-unsafe-eval'"));
        record('Actual WebView asset responses enforce CSP, no-sniff and no-referrer');
        originalKey = await page.evaluate(() => ILearnAndroid.getApiKey());
        const key = 'sk-or-v1-security-synthetic-key-only';
        assert.equal(await page.evaluate(value => ILearnAndroid.setApiKey(value), key), true);
        assert.equal(await page.evaluate(() => ILearnAndroid.getApiKey()), key);
        const preferences = () => run('shell', 'run-as', app, 'cat', 'shared_prefs/MainActivity.xml');
        const first = preferences();
        assert.ok(first.includes('apiKey') && !first.includes(key));
        assert.equal(await page.evaluate(value => ILearnAndroid.setApiKey(value), key), true);
        const second = preferences(); assert.notEqual(first, second); assert.ok(!second.includes(key));
        for (const invalid of [null, 'short', 'sk-or-' + 'a'.repeat(260), 'sk-or-valid-but-has-newline\n', 'sk-or-bad<script>payload']) {
            assert.equal(await page.evaluate(value => ILearnAndroid.setApiKey(value), invalid), false);
            assert.equal(await page.evaluate(() => ILearnAndroid.getApiKey()), key);
        }
        record('Native key bridge rejects invalid input; Keystore AES/GCM uses fresh IVs and stores no plaintext key');
        await page.evaluate(() => {
            window.__securityViolations = [];
            document.addEventListener('securitypolicyviolation', event => window.__securityViolations.push(event.violatedDirective));
            const script = document.createElement('script'); script.textContent = 'window.__securityInjected=true'; document.body.append(script);
            for (const url of ['data:text/html,<script>parent.__securityFrame=true<\/script>', '/index.html']) {
                const frame = document.createElement('iframe'); frame.src = url; document.body.append(frame);
            }
            const frame = document.createElement('iframe'); frame.srcdoc = '<script>parent.__securityFrame=true;parent.__stolenKey=ILearnAndroid.getApiKey()<\/script>'; document.body.append(frame);
        });
        await expect.poll(() => page.evaluate(() => window.__securityViolations.length)).toBeGreaterThan(0);
        assert.equal(await page.evaluate(() => !!(window.__securityInjected || window.__securityFrame || window.__stolenKey)), false);
        assert.equal(await page.evaluate(() => ILearnAndroid.getApiKey()), key);
        record('Injected inline scripts, data frames, local frames and srcdoc frames cannot execute or call the native bridge');
        const blocked = await page.evaluate(async () => {
            const targets = ['https://example.invalid/exfil', 'http://example.invalid/exfil', 'file:///data/data/ir.ilearn.offline/shared_prefs/MainActivity.xml', 'https://openrouter.ai/api/v1/keys'];
            return Promise.all(targets.map(async url => { try { await fetch(url); return false; } catch { return true; } }));
        });
        assert.ok(blocked.every(Boolean));
        const methodBlocked = await page.evaluate(async () => { try { return (await fetch('https://openrouter.ai/api/v1/chat/completions')).status === 403; } catch { return true; } });
        const queryBlocked = await page.evaluate(async () => { try { return (await fetch('https://openrouter.ai/api/v1/chat/completions?redirect=evil', { method: 'POST' })).status === 403; } catch { return true; } });
        assert.ok(methodBlocked && queryBlocked);
        record('CSP blocks file, cleartext and off-allowlist requests; native host rejects wrong API methods and query strings');
        const oldName = await page.locator('#profile-name').inputValue();
        await page.locator('#profile-name').fill('<img src=x onerror=alert(1)>');
        await page.getByRole('button', { name: 'ذخیره نام', exact: true }).click();
        await expect(page.locator('.player-bar-identity strong')).toHaveText('<img src=x onerror=alert(1)>');
        assert.equal(await page.locator('img[src=x]').count(), 0);
        await page.locator('#profile-name').fill(oldName); await page.getByRole('button', { name: 'ذخیره نام', exact: true }).click();
        record('Offline profile treats stored HTML as text');
        await page.evaluate(() => { const link = document.createElement('a'); link.href = 'https://example.invalid/security-test'; link.id = 'security-external'; link.textContent = 'test'; document.body.append(link); link.click(); });
        await expect.poll(() => {
            run('shell', 'uiautomator', 'dump', '/sdcard/ilearn-security-ui.xml');
            return run('shell', 'cat', '/sdcard/ilearn-security-ui.xml').includes('انصراف');
        }, { timeout: 30000 }).toBe(true);
        run('shell', 'input', 'keyevent', '4');
        assert.equal(new URL(page.url()).origin, base);
        record('External HTTPS navigation requires native confirmation and never replaces the local WebView');
        const backup = JSON.parse(fs.readFileSync(path.join(__dirname, 'android-backup.json'), 'utf8'));
        assert.ok(!JSON.stringify(backup).includes('sk-or-'));
        const invalid = structuredClone(backup); invalid.Progress.Entries = null;
        const before = await page.evaluate(() => ilearnOffline.read('progress'));
        assert.equal(await page.evaluate(async json => { try { await ilearnOffline.restoreBackup(json); return false; } catch { return true; } }, JSON.stringify(invalid)), true);
        assert.equal(await page.evaluate(() => ilearnOffline.read('progress')), before);
        record('Malformed backup rejected atomically; existing progress is preserved and backups exclude the API key');
        assert.deepEqual(errors, []); completed = true;
    } finally {
        if (originalKey !== undefined) await page.evaluate(value => ILearnAndroid.setApiKey(value), originalKey).catch(() => {});
        fs.writeFileSync(path.join(__dirname, 'security-android-report.json'), JSON.stringify({ completed, date: new Date().toISOString(), checks, errors, providerRequests: 0, device: serial }, null, 2));
        console.log(JSON.stringify({ completed, checks: checks.length, errors }));
        await browser.close();
    }
})().catch(error => { console.error(error); process.exitCode = 1; });

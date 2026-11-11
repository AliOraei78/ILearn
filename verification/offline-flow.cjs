// Published WebAssembly only. All AI responses are intercepted locally; no provider request is sent.
const fs = require('node:fs');
const path = require('node:path');
const assert = require('node:assert/strict');
const { chromium, expect } = require(process.env.PLAYWRIGHT_MODULE || 'E:/Products/RightPixel/src/rightpixel-web/node_modules/@playwright/test');
const { measure } = require('./layout-checks.cjs');
const openChallenges = require('./teaching-navigation.cjs');
const base = process.env.ILEARN_OFFLINE_URL || 'http://127.0.0.1:5087';
const read = name => JSON.parse(fs.readFileSync(path.join(__dirname, '../ILearn/Content/', name), 'utf8'));
const lessons = [...read('csharp-v2.json'), ...read('lessons.json')];
const samples = read('code-room.json');
const exercises = [...samples, ...lessons.filter(l => l.Id.startsWith('R')).flatMap(l => l.Activities.filter(a => a.Kind === 'workshop').map(a => ({ Subject: 'csharp', Technology: 'C#', LessonId: l.Id, Template: 'implement', Activity: a })))];
const questions = read('interviews.json')[0].Questions;
const sizes = [[320,568],[360,640],[390,844],[412,915],[430,932],[640,360],[844,390],[768,1024]].map(([width,height]) => ({width,height}));
const checks = [], layouts = [], errors = [], kinds = new Set();
const curriculumOnly = process.argv.includes('--curriculum-only');
const flowsOnly = !curriculumOnly && process.argv.includes('--flows-only');
let mode = 'accept', requests = 0, completed = false, visited = 0;
const record = name => { checks.push(name); console.log('PASS ' + name); };
(async () => {
    const browser = await chromium.launch({ channel: 'chrome', headless: true });
    const context = await browser.newContext({ viewport: { width: 390, height: 844 }, isMobile: true, hasTouch: true });
    await context.route('**/*', async route => {
        const request = route.request();
        if (request.url().startsWith(base + '/')) return route.continue();
        if (request.url() !== 'https://openrouter.ai/api/v1/chat/completions') { errors.push('Unexpected external request: ' + request.url()); return route.abort(); }
        const headers = { 'access-control-allow-origin': '*', 'access-control-allow-headers': '*' };
        if (request.method() === 'OPTIONS') return route.fulfill({status: 204, headers});
        const selectedMode = mode;
        const payload = request.postDataJSON(); requests++;
        assert.equal(payload.model, 'nvidia/nemotron-3.5-super-vl:free');
        assert.equal(payload.provider.allow_fallbacks, false);
        assert.deepEqual(payload.provider.max_price, { prompt: 0, completion: 0, request: 0 });
        assert.equal(request.headers().authorization, 'Bearer sk-or-v1-ilearn-local-test-key');
        if (selectedMode === 'delay') await new Promise(resolve => setTimeout(resolve, 1500));
        if (selectedMode === 'error') return route.fulfill({status: 503, headers, body: '{}'});
        const content = payload.response_format ? selectedMode === 'invalid' ? '{}' : JSON.stringify({score: selectedMode === 'reject' ? 40 : 100, summary: 'بازخورد محلی آزمون موبایل.', issues: selectedMode === 'reject' ? ['پاسخ نیاز به اصلاح دارد.'] : [], nextHint: 'حالت مرزی را بررسی کن.'}) : 'این توضیح سادهٔ آزمایشی از پاسخ مرجع درس استفاده می‌کند.';
        try { await route.fulfill({ status: 200, headers: {...headers, 'content-type': 'application/json'}, body: JSON.stringify({choices:[{message:{content}}]}) }); } catch (error) { if (selectedMode !== 'delay') throw error; }
    });
    const page = await context.newPage();
    page.on('pageerror', error => errors.push(error.message));
    page.on('response', response => { if (response.status() >= 400 && /\.(css|js|wasm|woff2|svg)(\?|$)/.test(response.url())) errors.push(response.status() + ' ' + response.url()); });
    const click = name => page.getByRole('button', { name, exact: true }).click();
    const stored = () => page.evaluate(async () => JSON.parse(await ilearnOffline.read('progress')));
    const go = async url => {
        await page.evaluate(url => { const link = document.createElement('a'); link.href = url; document.body.append(link); link.click(); link.remove(); }, url);
        await expect(page).toHaveURL(base + url);
    };
    async function layout(name, all = true) {
        await measure(page, name, layouts, all ? sizes : [sizes[0], sizes[2], sizes[6]]);
        await page.evaluate(() => document.documentElement.style.fontSize = '200%');
        await measure(page, name + ' text 200%', layouts, [sizes[0], sizes[6]]);
        await page.evaluate(() => document.documentElement.style.fontSize = '');
    }
    const solve = require('./offline-solve.cjs')(page, expect);
    try {
        await page.goto(base);
        await expect(page.getByRole('heading', {name:'ساخت پروفایل محلی', exact:true})).toBeVisible();
        await layout('create local profile');
        await page.locator('#profile-name').fill('آزمون آفلاین');
        await page.locator('#profile-avatar').selectOption('avatar-07');
        await click('ساخت پروفایل و شروع');
        await expect(page.getByRole('heading', {name:'آموزشگاه و کارگاه', exact:true})).toBeVisible();
        await expect(page.locator('.player-bar-identity strong')).toHaveText('آزمون آفلاین');
        await expect(page.locator('form[action^="/auth"]')).toHaveCount(0);
        await page.reload();
        await expect(page.locator('.player-bar-identity strong')).toHaveText('آزمون آفلاین');
        record('local profile creation, avatar and reload without account/server');
        await layout('lobby');
        await page.getByRole('button', {name:/C# جامع/}).click();
        await page.locator('#phase-filter').selectOption('0');
        await expect(page.locator('.chapters:not(.archive-chapters)>li')).toHaveCount(123);
        await layout('chapters');
        const first = lessons[0];
        await go('/lobby?lesson=' + first.Id);
        await openChallenges(page, expect);
        await click('ثبت جواب');
        await expect(page.locator('.work > div.feedback')).toContainText('ابتدا');
        await expect.poll(async () => Object.values((await stored()).Entries).some(e => e.NeedsReview)).toBe(false);
        record('blank answer does not record a review mistake');
        if(!flowsOnly) for (const lesson of lessons) {
            await go('/lobby?lesson=' + lesson.Id + '&view=intro');
            await expect(page.locator('#lesson-introduction-title')).toBeVisible();
            await expect(page.locator('.summary').first()).not.toBeEmpty();
            await openChallenges(page, expect);
            for (const [index, activity] of lesson.Activities.entries()) {
                if (index) await page.locator('#activity-picker').selectOption(String(index));
                await expect(page.locator('#activity-title')).toHaveText(activity.Title);
                await click('پاسخ کامل');
                await expect(page.locator('#reference-answer > p').first()).not.toBeEmpty();
                if (activity.SolutionCode) await expect(page.locator('#reference-code')).toHaveText(activity.SolutionCode);
                await expect(page.locator('[data-reference-file]')).toHaveCount(Object.keys(activity.ReferenceFiles || {}).length);
                await click('بستن پاسخ کامل');
                if (!kinds.has(activity.Kind)) {
                    await layout('activity ' + activity.Kind, false);
                    await solve(activity);
                    const key = `csharp/${lesson.Id}/${activity.Id}`;
                    await expect.poll(async () => (await stored()).Entries[key]?.Complete).toBe(true);
                    kinds.add(activity.Kind);
                }
                visited++;
            }
            if (visited % 40 < lesson.Activities.length) console.log('Rendered ' + visited + ' learning activities');
        }
        if (!flowsOnly) {
            const activities = lessons.flatMap(lesson => lesson.Activities);
            assert.equal(visited, activities.length);
            assert.deepEqual(kinds, new Set(activities.map(activity => activity.Kind)));
            record('all ' + visited + ' activities and ' + lessons.length + ' introductions render; every activity template solved locally');
        }
        if (curriculumOnly) {
            completed = true;
            return;
        }
        await go('/code-room');
        await expect(page.locator('#code-exercise option')).toHaveCount(exercises.filter(e=>e.Subject==='csharp').length);
        const templates = new Set();
        for (const exercise of exercises) {
            const key = `code-room/${exercise.Subject}/${exercise.LessonId}/${exercise.Activity.Id}`;
            await go('/code-room?subject=' + exercise.Subject + '&exercise=' + encodeURIComponent(key));
            await expect(page.locator('#code-problem-title')).toHaveText(exercise.Activity.Title);
            await expect(page.getByRole('button',{name:'بررسی پاسخ با هوش مصنوعی',exact:true})).toBeVisible();
            if (!templates.has(exercise.Template)) { await layout('code room ' + exercise.Template, false); templates.add(exercise.Template); }
        }
        assert.equal(exercises.length, 135);
        record('all 135 code exercises and 12 templates available with AI and offline reference');
        await page.locator('#draft').fill('پاسخ محلی بدون کلید');
        await click('بررسی پاسخ با هوش مصنوعی');
        await expect(page.locator('.code-room-workspace .feedback.error')).toContainText('هوش مصنوعی');
        assert.equal(requests, 0);
        await expect(page.locator('#draft')).toHaveValue('پاسخ محلی بدون کلید');
        record('missing AI key keeps draft and does not send a request');
        await go('/profile');
        await expect(page.locator('#ai-settings')).toBeVisible();
        await layout('profile and AI settings');
        await page.locator('#api-key').fill('sk-or-v1-ilearn-local-test-key'); await click('ذخیره کلید');
        await expect(page.locator('#api-key')).toHaveValue('');
        await expect(page.getByText('کلید روی این دستگاه تنظیم شده است.',{exact:true})).toBeVisible();
        await page.getByRole('button',{name:'کاراکتر 48',exact:true}).click();
        await page.reload(); await expect(page.getByRole('button',{name:'کاراکتر 48',exact:true})).toHaveAttribute('aria-pressed','true');
        await go('/code-room?subject=csharp&exercise=' + encodeURIComponent('code-room/csharp/' + samples[0].LessonId + '/' + samples[0].Activity.Id));
        await page.locator('#draft').fill('پاسخ آزمایشی AI');
        mode = 'reject'; await click('بررسی پاسخ با هوش مصنوعی');
        await expect(page.locator('.code-room-workspace .feedback.error')).toContainText('بازخورد محلی');
        mode = 'accept'; await click('بررسی پاسخ با هوش مصنوعی');
        await expect(page.locator('.code-room-workspace .feedback:not(.error)')).toContainText('بازخورد محلی');
        const count = Object.values((await stored()).Entries).filter(e => e.Complete).length;
        await click('بررسی پاسخ با هوش مصنوعی');
        await expect(page.locator('.code-room-workspace .feedback:not(.error)')).toBeVisible();
        assert.equal(Object.values((await stored()).Entries).filter(e => e.Complete).length, count);
        mode = 'invalid'; const before = requests; await click('بررسی پاسخ با هوش مصنوعی');
        await expect(page.locator('.code-room-workspace .feedback.error')).toBeVisible(); assert.equal(requests, before + 2);
        mode = 'error'; await click('بررسی پاسخ با هوش مصنوعی'); await expect(page.locator('.code-room-workspace .feedback.error')).toBeVisible();
        await expect(page.locator('#draft')).toHaveValue('پاسخ آزمایشی AI');
        mode = 'delay'; await click('بررسی پاسخ با هوش مصنوعی'); await click('لغو بررسی'); await expect(page.getByRole('button',{name:'بررسی پاسخ با هوش مصنوعی',exact:true})).toBeEnabled();
        mode = 'accept';
        record('AI retained: rejection, acceptance, one-time XP, invalid JSON retry, service failure and cancellation');
        await go('/interview');
        await expect(page.locator('#interview-question option')).toHaveCount(100);
        for (const question of questions) {
            await page.locator('#interview-question').selectOption(question.Activity.Id);
            await expect(page.locator('#interview-question-title')).toHaveText(question.Activity.Title);
            await page.getByText('پاسخ مرجع', {exact:true}).click();
            await expect(page.locator('details[open] .summary')).toHaveText(question.Activity.Solution);
        }
        await layout('interview');
        await page.locator('#draft').fill('پاسخ تشریحی آزمایشی'); await click('بررسی پاسخ با هوش مصنوعی');
        await expect(page.locator('.interview-workspace .feedback')).toContainText('بازخورد محلی');
        await click('ثبت شخصی تکمیل');
        await expect(page.locator('.interview-workspace .feedback')).toContainText('ارزیابی شخصی');
        record('all 100 interview questions/reference answers; AI and personal completion remain separate');
        await go('/lobby?lesson=R001&activity=3');
        await expect(page.locator('#draft')).toBeVisible();
        await page.locator('#draft').fill('پیش‌نویس کارگاه روی دستگاه');
        await click('پاسخ کامل'); await click('توضیح ساده‌تر با هوش مصنوعی');
        await expect(page.locator('#reference-answer')).toContainText('توضیح سادهٔ آزمایشی');
        await click('بررسی پاسخ با هوش مصنوعی'); await expect(page.locator('.work > div.feedback')).toContainText('بازخورد محلی');
        record('academy AI grading and simpler explanation share the fixed free model');
        await go('/code-room'); await page.locator('#draft').fill('پیش‌نویس هم‌زمان');
        const second = await context.newPage(); await second.goto(page.url()); await expect(second.locator('#draft')).toHaveValue('پیش‌نویس هم‌زمان');
        await page.locator('#draft').fill('نسخه تازهٔ صفحه اول'); await expect.poll(async () => Object.values((await stored()).Entries).some(e=>e.Draft==='نسخه تازهٔ صفحه اول')).toBe(true);
        await second.locator('#draft').fill('نوشتهٔ صفحه قدیمی'); await expect(second.locator('main [role=alert]')).toContainText('ذخیره نشد');
        await second.getByRole('link',{name:'پروفایل من',exact:true}).click(); await expect(second.locator('#draft')).toHaveValue('نوشتهٔ صفحه قدیمی');
        assert.ok(!Object.values((await stored()).Entries).some(e=>e.Draft==='نوشتهٔ صفحه قدیمی')); await second.close();
        record('IndexedDB revision conflict preserves the current save, blocks navigation and keeps the stale draft');
        await go('/profile');
        const downloadPromise = page.waitForEvent('download'); await click('ذخیره فایل پشتیبان'); const download = await downloadPromise;
        const backupPath = path.join(__dirname, 'offline-backup.json'); await download.saveAs(backupPath);
        const backup = JSON.parse(fs.readFileSync(backupPath,'utf8'));
        assert.equal(backup.Format,'ILearn.Offline'); assert.equal(backup.Profile.Name,'آزمون آفلاین'); assert.ok(!JSON.stringify(backup).includes('sk-or-v1-ilearn-local-test-key'));
        const malformed=structuredClone(backup);malformed.Profile.Id='-'.repeat(36);
        assert.equal(await page.evaluate(json=>{try{ilearnOffline.validateBackup(json);return false}catch{return true}},JSON.stringify(malformed)),true);
        await page.locator('#backup-file').setInputFiles({name:'invalid.json',mimeType:'application/json',buffer:Buffer.from('{"Format":"ILearn.Offline","Version":1,"Progress":null}')});
        await expect(page.locator('main [role=alert]')).toContainText('معتبر نیست');
        await page.locator('#profile-name').fill('نام موقت'); await click('ذخیره نام');
        await page.locator('#backup-file').setInputFiles(backupPath); await expect(page.getByRole('button',{name:'جایگزینی سیو با پشتیبان',exact:true})).toBeVisible();
        await expect(page.locator('.player-bar-identity strong')).toHaveText('نام موقت');
        await click('جایگزینی سیو با پشتیبان'); await expect(page.locator('.player-bar-identity strong')).toHaveText('آزمون آفلاین');
        assert.deepEqual((await stored()).Entries, backup.Progress.Entries);
        record('backup export/import excludes credentials, rejects invalid files and confirms before replacing data');
        await click('حذف کلید از دستگاه'); await expect(page.getByText('هنوز کلیدی تنظیم نشده است؛ ثبت شخصی و پاسخ مرجع آماده‌اند.',{exact:true})).toBeVisible();
        await page.evaluate(() => scrollTo(0,0)); await page.screenshot({path:path.join(__dirname,'offline-final-profile.png'),fullPage:true});
        await go('/lobby'); await page.screenshot({path:path.join(__dirname,'offline-final-lobby.png'),fullPage:true});
        completed = true;
    } catch (error) { await page.screenshot({path:path.join(__dirname,'offline-failure.png'),fullPage:true}); throw error; }
    finally {
        const failedLayouts = layouts.filter(x=>x.findings.length);
        fs.writeFileSync(path.join(__dirname,'offline-browser-report.json'),JSON.stringify({completed,date:new Date().toISOString(),checks,activities:visited,templates:[...kinds],layoutChecks:layouts.length,failedLayouts,errors,modelFixtureRequests:requests},null,2));
        console.log(JSON.stringify({completed,checks:checks.length,activities:visited,templates:kinds.size,layoutChecks:layouts.length,failedLayouts:failedLayouts.length,issues:[...new Set(failedLayouts.flatMap(x=>x.findings))],errors,modelFixtureRequests:requests}));
        await browser.close(); if (!completed || failedLayouts.length || errors.length) process.exitCode=1;
    }
})().catch(error=>{console.error(error);process.exitCode=1;});

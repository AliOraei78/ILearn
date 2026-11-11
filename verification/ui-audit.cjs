// Run against an isolated database and the local SMTP/OpenRouter fixtures on ports 2528/11435.
const fs = require('node:fs');
const http = require('node:http');
const assert = require('node:assert/strict');
const path = require('node:path');
const { execFileSync } = require('node:child_process');
const { chromium, expect } = require(process.env.PLAYWRIGHT_MODULE || 'E:/Products/RightPixel/src/rightpixel-web/node_modules/@playwright/test');
const openChallenges = require('./teaching-navigation.cjs');
const { server: smtp, messages } = require('./test-smtp.cjs')();
const base = process.env.ILEARN_AUDIT_URL || 'http://127.0.0.1:5084';
const database = process.env.ILEARN_AUDIT_DATABASE || 'ilearn_ui_audit';
if(!['ilearn_ui_audit','ilearn_csharp_v2'].includes(database))throw new Error('Use an isolated audit database.');
const baseline = process.argv.includes('--baseline');
// Retain the original four-chapter account/UI regression; curriculum-flow.cjs covers all ten chapters.
const lessons = JSON.parse(fs.readFileSync(path.join(__dirname, '../ILearn/Content/lessons.json'), 'utf8')).filter(l => ['S05', 'S06', 'S07', 'S08'].includes(l.Id));
const widths = [280, 320, 360, 390, 480, 481, 600, 601, 760, 761, 768, 900, 901, 1024, 1280, 1366, 1440, 1920];
const standardSizes = [[320,568],[360,640],[375,667],[390,844],[393,852],[412,915],[414,896],[430,932],[480,800],[640,360],[768,1024],[800,1280],[820,1180],[1024,768],[1280,720],[1366,768],[1440,900],[1536,864],[1600,900],[1920,1080],[2560,1440]];
standardSizes.push([1280,1024],[2002,1222],[3440,1440],[3840,2160]);
standardSizes.push([844,500],[844,501],[1366,500],[1366,501]);
const viewports = [...new Map([...widths.map(width=>({width,height:900})), ...standardSizes.map(([width,height])=>({width,height}))].map(size=>[`${size.width}x${size.height}`,size])).values()];
const checks = [], layouts = [], errors = [], screenshots = new Set();
let completed = false;
let modelMode = 'success';
let modelRequests = 0;
const model = http.createServer(async (request, response) => {
    let body = ''; for await (const chunk of request) body += chunk;
    modelRequests++;
    if (modelMode === 'slow') await new Promise(resolve => { const timer=setTimeout(resolve,10000); response.once('close',()=>{clearTimeout(timer);resolve();}); });
    if (modelMode === 'error') { response.writeHead(503).end(); return; }
    const payload = JSON.parse(body);
    const content = payload.response_format ? JSON.stringify({ score: modelMode === 'insufficient' ? 69 : 100, summary: 'پاسخ با معیار تمرین بررسی شد.', issues: [], nextHint: 'تمرین بعد را انجام بده.' }) : 'این توضیح آزمایشی از پاسخ مرجع درس استفاده می‌کند.';
    response.setHeader('Content-Type', 'application/json');
    response.end(JSON.stringify({ choices: [{ message: { content } }] }));
});
smtp.on('connection', socket => socket.on('error', error => { if (error.code !== 'ECONNRESET') errors.push(error.message); }));

(async () => {
    await new Promise(resolve => smtp.listen(Number(process.env.ILEARN_AUDIT_SMTP_PORT||2528), '127.0.0.1', resolve));
    await new Promise(resolve => model.listen(Number(process.env.ILEARN_AUDIT_MODEL_PORT||11435), '127.0.0.1', resolve));
    const browser = await chromium.launch({ headless: true, channel: process.env.ILEARN_AUDIT_BROWSER || 'chrome' });
    const context = await browser.newContext({ viewport: { width: 1366, height: 900 } });
    const page = await context.newPage();
    const watchPage=target=>{
        target.on('pageerror',error=>errors.push(error.message));
        target.on('response',response=>{if(response.status()>=400&&/\.(css|js|woff2|svg)(\?|$)/.test(response.url()))errors.push(response.status()+' '+response.url());});
    };
    watchPage(page);context.on('page',watchPage);
    const click = name => page.getByRole('button', { name, exact: true }).click();
    // Every SQL mutation below is restricted to this disposable database and the account created by this run.
    const sql = statement => execFileSync('docker', ['exec', 'ilearn-postgres-1', 'psql', '-v', 'ON_ERROR_STOP=1', '-U', 'ilearn', '-d', database, '-c', statement], { encoding: 'utf8', stdio: ['ignore', 'pipe', 'pipe'] });
    const archived = async (id,target=page) => {
        const button=target.getByRole('button',{name:new RegExp('^'+id+' ·')});
        if(!await button.isVisible())await target.getByText('آرشیو مسیر قبلی؛ حفظ پیشرفت و امتیاز',{exact:true}).click();
        await button.click();
    };
    const record = name => { checks.push(name); console.log('PASS ' + name); };
    const screenshot = async name => {
        if (screenshots.has(name)) return;
        screenshots.add(name);
        await page.screenshot({ path: path.join(__dirname, `audit-${baseline ? 'before' : 'after'}-${name}.png`), fullPage: true });
    };
    const measure = async (name, sizes = viewports, target = page) => {
        for (const size of sizes) {
            await target.setViewportSize(size);
            await target.evaluate(() => document.fonts.ready);
            const findings = await target.evaluate(() => {
                const visible = element => element.checkVisibility() && element.getBoundingClientRect().width > 0 && element.getBoundingClientRect().height > 0;
                const issues = [];
                if (document.documentElement.scrollWidth > innerWidth + 1) issues.push('document overflow: ' + document.documentElement.scrollWidth);
                for (const element of document.querySelectorAll('button,.button-link,.account-action,.option,summary,input:not([type=hidden]):not([type=checkbox]):not([type=radio]),select,textarea')) {
                    if (!visible(element)) continue;
                    const rect = element.getBoundingClientRect();
                    const label = (element.getAttribute('aria-label') || element.textContent || element.id).trim().slice(0, 60);
                    if (rect.width < 43 || rect.height < 43) issues.push('small target: ' + label);
                    if (rect.left < -1 || rect.right > innerWidth + 1) issues.push('outside viewport: ' + label);
                    if (!['SELECT', 'INPUT', 'TEXTAREA'].includes(element.tagName) && element.scrollWidth > element.clientWidth + 2) issues.push('clipped content: ' + label);
                }
                for (const canvas of document.querySelectorAll('.avatar-grid canvas')) {
                    const child = canvas.getBoundingClientRect(), parent = canvas.closest('button').getBoundingClientRect();
                    if (child.left < parent.left || child.right > parent.right) { issues.push('avatar exceeds button'); break; }
                }
                for (const button of document.querySelectorAll('button + button')) {
                    const previous = button.previousElementSibling;
                    if (!visible(button) || !visible(previous)) continue;
                    const a = previous.getBoundingClientRect(), b = button.getBoundingClientRect();
                    const horizontal = Math.abs(a.top - b.top) < 2;
                    if (horizontal && Math.min(Math.abs(a.right - b.left), Math.abs(b.right - a.left)) < 7) issues.push('adjacent buttons without gap: ' + button.textContent.trim().slice(0, 30));
                }
                for (const toolbar of document.querySelectorAll('.toolbar + .toolbar')) {
                    if (visible(toolbar) && toolbar.getBoundingClientRect().top - toolbar.previousElementSibling.getBoundingClientRect().bottom < 7) issues.push('adjacent action rows without gap');
                }
                const root = document.querySelector('dialog[open]') || document;
                const blocks = [...root.querySelectorAll('button,select,input:not([type=hidden]),textarea,h1,h2,h3,p,label,pre,summary,th')]
                    .filter(visible).map(element => ({ element, rect: element.getBoundingClientRect() }));
                for (let i = 0; i < blocks.length; i++) for (let j = i + 1; j < blocks.length; j++) {
                    const a = blocks[i], b = blocks[j];
                    if (a.element.contains(b.element) || b.element.contains(a.element)) continue;
                    if (Math.min(a.rect.right, b.rect.right) - Math.max(a.rect.left, b.rect.left) > 1 && Math.min(a.rect.bottom, b.rect.bottom) - Math.max(a.rect.top, b.rect.top) > 1)
                        issues.push('overlap: ' + a.element.tagName + '/' + b.element.tagName + ' ' + a.element.textContent.trim().slice(0, 30));
                }
                return [...new Set(issues)];
            });
            layouts.push({ name, ...size, findings });
        }
    };
    const enlarged = async (name,target=page) => {
        await target.evaluate(() => document.documentElement.style.fontSize = '200%');
        await measure(name + ' text 200%', [{ width: 320, height: 568 }, { width: 768, height: 1024 }, { width: 1366, height: 900 }],target);
        await target.evaluate(() => document.documentElement.style.fontSize = '');
    };
    const complete = async () => {
        await click('ثبت جواب');
        await expect(page.locator('.work .feedback[role=status]')).toContainText('درست بود.');
        await expect(page.locator('.work .feedback[role=status]')).not.toHaveClass(/error/);
        await expect(page.getByText('تکمیل ثبت شد:', { exact: false })).toBeVisible();
    };
    const selectLesson = async lesson => {
        await click('بازگشت به آموزشگاه');
        await page.getByRole('button', { name: /C# جامع/ }).click();
        await archived(lesson.Id);
        const introduction = page.locator('#lesson-introduction-title');
        await expect(page.locator('#activity-title').or(introduction)).toBeVisible();
        if (await introduction.isVisible()) {
            await measure(lesson.Id + ' introduction', [{ width: 320, height: 568 }, { width: 1366, height: 900 }]);
        }
        await openChallenges(page, expect);
    };
    const navigate = async (lesson, index) => {
        await page.locator('nav[aria-label="چالش‌های جلسه"] button').nth(index).click();
        await expect(page.locator('#activity-title')).toHaveText(lesson.Activities[index].Title);
    };
    try {
        for (const route of ['/', '/login', '/register', '/forgot-password', '/reset-password', '/verify', '/set-password', '/missing-audit-page', '/Error']) {
            await page.goto(base + route);
            await measure(route);
            if (!baseline) await enlarged(route);
        }
        record('public pages, empty verification/grant, error and 404');
        await page.goto(base + '/lobby');
        await expect(page).toHaveURL(/\/login/);
        record('anonymous lobby requires login');
        const email = `ui-audit-${Date.now()}@example.test`, password = 'Audit-local-927!';
        await page.goto(base + '/register');
        await page.getByLabel('نام بازیکن', { exact: true }).fill('آزمایش رابط با نام طولانی برای بررسی چیدمان');
        await page.getByLabel('ایمیل', { exact: true }).fill(email);
        await click('ارسال کد تأیید');
        await expect(page).toHaveURL(/\/verify$/);
        assert.ok(messages.at(-1)?.code, 'local verification email received');
        await measure('verification pending');
        await page.locator('#code').fill('000000' === messages.at(-1).code ? '111111' : '000000');
        await click('تأیید ایمیل و ادامه');
        await expect(page.locator('main [role=alert]')).toBeVisible();
        await measure('invalid verification code');
        await page.locator('#code').fill(messages.at(-1).code.replace(/[0-9]/g,d=>'۰۱۲۳۴۵۶۷۸۹'[Number(d)]));
        await click('تأیید ایمیل و ادامه');
        await expect(page).toHaveURL(/\/set-password$/);
        await measure('password grant');
        await page.getByLabel('رمز تازه', { exact: true }).fill(password);
        await page.getByLabel('تکرار رمز تازه', { exact: true }).fill(password+'x');
        assert.ok(await page.locator('#set-confirmation').evaluate(element=>element.validity.customError));
        await click('ذخیره رمز'); await expect(page).toHaveURL(/\/set-password$/);
        await page.getByLabel('تکرار رمز تازه', { exact: true }).fill(password);
        await click('ذخیره رمز');
        await expect(page).toHaveURL(/\/lobby$/);
        await expect(page.getByRole('heading', { name: 'موضوع‌ها', exact: true })).toBeVisible();
        await measure('subject lobby');
        await click('مرور اشتباه‌ها (0)');
        await expect(page.getByText('فعلاً تمرینی برای مرور نداری.')).toBeVisible();
        record('registration, invalid/valid code, password, authenticated lobby and empty review');
        await page.goto(base + '/');
        await expect(page.getByRole('link',{name:'ورود به حساب',exact:true})).toHaveCount(0);
        await expect(page.getByRole('link',{name:'ساخت حساب کاربری',exact:true})).toHaveCount(0);
        await expect(page.getByRole('link',{name:'برگشت به آموزشگاه',exact:true})).toBeVisible();
        await measure('signed-in introduction'); await enlarged('signed-in introduction');
        await page.setViewportSize({width:390,height:844}); await screenshot('signed-in-introduction-mobile');
        await page.setViewportSize({width:1440,height:900}); await screenshot('signed-in-introduction-desktop');
        for(const route of ['/login','/register']){await page.goto(base+route);await expect(page).toHaveURL(/\/lobby$/);await expect(page.locator('#password')).toHaveCount(0);}
        record('signed-in introduction hides sign-in/sign-up; authenticated account routes return to lobby');
        await page.getByRole('button', { name: /C# جامع/ }).click();
        await measure('chapter list');
        await page.locator('summary').click();
        await measure('chapter roadmap');
        await archived('S05');
        await openChallenges(page, expect);
        await click('چالش بعد');await expect(page.locator('#activity-title')).toHaveText(lessons[0].Activities[1].Title);await expect(page.locator('#activity-title')).toBeFocused();
        await click('چالش قبل');await expect(page.locator('#activity-title')).toHaveText(lessons[0].Activities[0].Title);await expect(page.locator('#activity-title')).toBeFocused();
        record('next/previous activity moves keyboard focus into the new question');
        await navigate(lessons[0],1); await page.goBack(); await expect(page.locator('#activity-title')).toHaveText(lessons[0].Activities[0].Title);
        await page.goForward(); await expect(page.locator('#activity-title')).toHaveText(lessons[0].Activities[1].Title);
        await page.reload(); await expect(page.locator('#activity-title')).toHaveText(lessons[0].Activities[1].Title);
        await page.goto(base+'/lobby');await click('ادامه آخرین چالش');await expect(page.locator('#activity-title')).toHaveText(lessons[0].Activities[1].Title);
        await click('فهرست فصل‌ها');await expect(page.getByRole('heading',{name:'مسیر C# از صفر تا پیشرفته',exact:true})).toBeVisible();
        await page.goBack();await expect(page.locator('#activity-title')).toHaveText(lessons[0].Activities[1].Title);
        record('Back/Forward, reload, resume and contextual chapter list preserve the activity');
        await page.goto(base+'/lobby?lesson=S05&activity=not-a-number');await expect(page.locator('#activity-title')).toHaveText(lessons[0].Activities[1].Title);
        await page.goto(base+'/lobby?lesson=missing&activity=7');await expect(page.getByText('فصل درخواستی پیدا نشد؛',{exact:false})).toBeVisible();
        await click('ادامه آخرین چالش');await expect(page.locator('#activity-title')).toHaveText(lessons[0].Activities[1].Title);
        record('malformed activity and unknown chapter recover without overwriting the saved destination');
        for (const lesson of lessons) {
            if (lesson.Id !== 'S05') await selectLesson(lesson);
            for (let index = 0; index < lesson.Activities.length; index++) {
                const activity = lesson.Activities[index];
                await navigate(lesson, index);
                await measure(`${lesson.Id}/${activity.Id}/${activity.Kind}`);
                await measure(`${lesson.Id}/${activity.Id}/landscape`, [{ width: 568, height: 320 }, { width: 844, height: 390 }]);
                if (!baseline) await enlarged(`${lesson.Id}/${activity.Id}`);
                await page.setViewportSize({ width: 320, height: 720 });
                await screenshot(activity.Kind + '-mobile');
                await page.setViewportSize({ width: 1366, height: 900 });
                if(!['explain','workshop'].includes(activity.Kind)){
                    await click('ثبت جواب');await expect(page.locator('.work .feedback[role=status]')).toBeVisible();
                    await expect(page.locator('.work .feedback[role=status]')).not.toContainText('درست بود.');
                    await expect(page.locator('.work .feedback[role=status]')).toHaveClass(/error/);
                    if(activity.Kind!=='order') await expect(page.getByRole('button',{name:'مرور اشتباه‌ها (0)',exact:true})).toBeVisible();
                }
                if (activity.Kind === 'concept') {
                    for(const option of [0,1,3,4]) await click(activity.Options[option]);
                    await expect(page.getByText('فرصت‌های این دور تمام شد.',{exact:false})).toBeVisible();
                    await measure('concept exhausted guesses');await screenshot('concept-exhausted');
                    await click('شروع مجدد دور');
                    await page.getByRole('button', { name: activity.Options[Number(activity.Answers[0])], exact: true }).click();
                } else if (activity.Kind === 'experiment') {
                    await click('آزمایش 1');
                    await page.locator('.options input').nth(Number(activity.Answers[0])).check();
                } else if (activity.Kind === 'mission') {
                    let node = 0;
                    for (let step = 0; !activity.Game.Nodes[node].Success && step < activity.Game.Limit; step++) {
                        const choice = 0;
                        await click(activity.Game.Nodes[node].Choices[choice]);
                        node = activity.Game.Nodes[node].Next[choice];
                    }
                } else if(activity.Kind==='budget'){
                    await page.locator('.options input').nth(3).check();
                    await expect(page.getByText('بیشتر از بودجه؛ یکی از گزینه‌ها را بردار.',{exact:false})).toBeVisible();
                    await measure('budget exceeded');await screenshot('budget-exceeded');
                    await page.locator('.options input').nth(3).uncheck();
                    for(const answer of activity.Answers)await page.locator('.options input').nth(Number(answer)).check();
                } else if (activity.Kind === 'deduction') {
                    for (const key of activity.Answers) {
                        const [row, column] = key.split(':').map(Number);
                        await page.locator('.deduction tbody tr').nth(row).locator('button').nth(column).click();
                    }
                } else if (['slots', 'assessment'].includes(activity.Kind)) {
                    for (const key of activity.Answers) {
                        const [row, value] = key.split(':');
                        await page.locator('#slot-' + row).selectOption(value);
                        await expect(page.locator('#slot-' + row)).toHaveValue(value);
                        if (baseline) await page.waitForTimeout(150);
                    }
                } else if (activity.Kind === 'trace') {
                    for (const key of activity.Answers) {
                        const [row, value] = key.split(':').map(Number);
                        if (row > 0) await click('قدم بعد');
                        await page.locator('.options input').nth(value).check();
                        if(row===1){await page.reload();await expect(page.getByText('قدم 2 از 3:',{exact:false})).toBeVisible();}
                    }
                } else if (activity.Kind === 'order') {
                    const order = activity.Options.map((_, i) => i).reverse();
                    for (let target = 0; target < activity.Answers.length; target++) {
                        let position = order.indexOf(Number(activity.Answers[target]));
                        while (position > target) {
                            await page.getByRole('button', { name: `بالا بردن قطعه ${position + 1}`, exact: true }).click();
                            [order[position], order[position - 1]] = [order[position - 1], order[position]];
                            position--;
                        }
                    }
                } else if (['explain', 'workshop'].includes(activity.Kind)) {
                    await expect(page.getByRole('button', { name: 'ثبت شخصی تکمیل', exact: true })).toBeDisabled();
                    await page.locator('#draft').fill(activity.Kind === 'workshop' ? 'interface IAudit { string Read(); }\n// آزمایش' : 'این پاسخ آزمایشی برای بررسی ذخیره و رابط است.');
                    if(activity.Kind==='explain'){await page.keyboard.press('Tab');assert.ok(await page.evaluate(()=>document.activeElement.id!=='draft'));}
                    await click('ثبت شخصی تکمیل');
                    await expect(page.getByText('تکمیل ثبت شد:', { exact: false })).toBeVisible();
                } else {
                    if (activity.Kind === 'detective') { await click('سرنخ 1'); await click('سرنخ 2'); await page.reload();await expect(page.getByRole('button',{name:'سرنخ 1',exact:true})).toHaveAttribute('aria-pressed','true'); }
                    for (const answer of activity.Answers) await page.locator('.options input').nth(Number(answer)).check();
                }
                if (!['explain', 'workshop'].includes(activity.Kind)) {await expect(page.locator('.work .feedback[role=status]')).toHaveCount(0);await complete();}
                if (!baseline && activity.Kind === 'deduction') {
                    const cell = page.locator('.deduction button').first();
                    await cell.focus(); await page.keyboard.press('Space'); await page.keyboard.press('Space');
                    await expect(cell).toHaveText('رد'); await expect(cell).toBeFocused();
                    await page.keyboard.press('Space'); await expect(cell).toHaveText('نامشخص');
                    record('deduction rapid keyboard cycling keeps focus and all three states');
                }
                const requestsBeforeReference=modelRequests;
                await click('پاسخ کامل');
                await expect(page.getByRole('heading', { name: 'پاسخ مرجع درس', exact: true })).toBeVisible();
                assert.equal(modelRequests,requestsBeforeReference,'reference answer must not contact AI');
                await click('توضیح ساده‌تر با هوش مصنوعی');
                await expect(page.locator('#reference-answer [role=status]')).toContainText('توضیح مدل:');
                if (!['explain', 'workshop'].includes(activity.Kind)) { await click('یک راهنمایی'); await click('بستن راهنمایی'); }
                await measure(`${lesson.Id}/${activity.Id}/completed-reference`, [{ width: 280, height: 653 }, { width: 390, height: 844 }, { width: 1366, height: 900 }]);
                if (!baseline) { await click('بستن پاسخ کامل'); await expect(page.locator('#reference-answer')).toHaveCount(0); }
                record(`${lesson.Id}/${activity.Id}: solve, completion, reference, hint and layout`);
            }
        }
        const last = lessons.at(-1);
        await navigate(last, 7);
        await page.locator('#draft').focus();
        await page.keyboard.press('End'); await page.keyboard.press('Tab');
        await expect(page.locator('#draft')).toBeFocused();
        await page.keyboard.press('Escape'); await page.keyboard.press('Tab');
        assert.ok(await page.evaluate(() => document.activeElement.id !== 'draft'));
        record('editor indentation and Escape/Tab keyboard exit');
        modelMode = 'error';
        await click('بررسی پاسخ با هوش مصنوعی');
        await expect(page.locator('.work .feedback[role=status]')).toContainText('مدل در دسترس نیست');
        await expect(page.locator('.work .feedback[role=status]')).toHaveClass(/error/);
        await expect(page.locator('.work .feedback[role=status]')).not.toContainText('Response status code');
        await expect(page.locator('#draft')).not.toHaveValue('');
        await measure('AI service failure');
        modelMode = 'slow';
        await click('بررسی پاسخ با هوش مصنوعی');
        await measure('AI review pending');
        await click('لغو بررسی');
        await expect(page.getByRole('button', { name: 'بررسی پاسخ با هوش مصنوعی', exact: true })).toBeEnabled();
        modelMode = 'insufficient';
        await click('بررسی پاسخ با هوش مصنوعی');
        await expect(page.locator('.work .feedback[role=status]')).toContainText('69');
        await expect(page.locator('.work .feedback[role=status]')).toHaveClass(/error/);
        await measure('AI insufficient grade');
        await click('ثبت شخصی تکمیل');
        await expect(page.locator('.work .feedback[role=status]')).toHaveText('تکمیل با ارزیابی شخصی ثبت شد.');
        await expect(page.locator('.work .feedback[role=status]')).not.toHaveClass(/error/);
        modelMode = 'success';
        await click('بررسی پاسخ با هوش مصنوعی');
        await expect(page.locator('.work .feedback[role=status]')).toContainText('100');
        await expect(page.locator('.work .feedback[role=status]')).not.toHaveClass(/error/);
        await page.locator('#draft').fill('interface IAudit { string Read(); }\n// پاسخ ویرایش شد');
        await expect(page.locator('.work .feedback[role=status]')).toHaveCount(0);
        modelMode='slow';await click('بررسی پاسخ با هوش مصنوعی');await click('پاسخ کامل');
        await expect(page.getByRole('button',{name:'لغو بررسی',exact:true})).toBeVisible();
        await expect(page.getByRole('button',{name:'لغو توضیح مدل',exact:true})).toHaveCount(0);
        await click('بستن پاسخ کامل');await click('لغو بررسی');modelMode='success';await click('ثبت شخصی تکمیل');
        record('AI success, service failure, cancellation and preserved draft using local fixture');
        await page.reload();
        await expect(page.locator('.player-bar-progress')).toContainText(baseline ? '0 از' : '400 امتیاز تجربه');
        await expect(page.locator('#draft')).not.toHaveValue('');
        record('reload restores chapter position, draft and 400 XP');
        if (!baseline) {
            await page.setViewportSize({ width: 390, height: 844 });
            await page.locator('#activity-picker').selectOption('0');
            await expect(page.locator('#activity-title')).toHaveText(last.Activities[0].Title);
            await click('شروع مجدد دور');
            await click(last.Activities[0].Options[0]);
            await click('ثبت جواب');
            await expect(page.locator('.work .feedback[role=status]')).toContainText('هنوز درست نیست');
            await click('مرور اشتباه‌ها (1)');
            await expect(page.locator('#activity-title')).toHaveText(last.Activities[0].Title);
            await click(last.Activities[0].Options[Number(last.Activities[0].Answers[0])]);
            await complete();
            await expect(page.getByRole('button', { name: 'مرور اشتباه‌ها (0)', exact: true })).toBeVisible();
            await expect(page.locator('.player-bar-progress')).toContainText('400 امتیاز تجربه');
            record('mobile challenge picker, failed answer, review return and no duplicate XP after reset');
            await page.setViewportSize({ width: 1366, height: 900 });
            sql(`UPDATE progress SET data=jsonb_set(data,'{Positions,S06}','999'),revision=revision+1 WHERE profile_id=(SELECT id FROM profiles WHERE email='${email}')`);
            await page.goto(base+'/lobby'); await expect(page.getByRole('heading', { name: 'موضوع‌ها', exact: true })).toBeVisible();
            await page.getByRole('button', { name: /C# جامع/ }).click();
            await archived('S06');
            await expect(page.locator('#activity-title')).toHaveText(lessons[1].Activities.at(-1).Title);
            sql(`UPDATE progress SET data=jsonb_set(jsonb_set(data,'{Positions,S05}','3'),'{Entries,csharp/S05/order,Answers}','["bad","1","2","3"]'),revision=revision+1 WHERE profile_id=(SELECT id FROM profiles WHERE email='${email}')`);
            await page.goto(base+'/lobby'); await expect(page.getByRole('heading', { name: 'موضوع‌ها', exact: true })).toBeVisible();
            await page.getByRole('button', { name: /C# جامع/ }).click();
            await archived('S05');
            await expect(page.locator('#activity-title')).toHaveText(lessons[0].Activities[3].Title);
            await expect(page.locator('.order-row')).toHaveCount(4);
            record('stale saved chapter position and malformed ordering recover without losing progress');
            const peer = await page.context().newPage();
            await peer.goto(base + '/lobby'); await expect(peer.getByRole('heading', { name: 'موضوع‌ها', exact: true })).toBeVisible();
            await navigate(lessons[0], 7);
            await expect(page.locator('main [role=alert]')).toHaveCount(0);
            await peer.getByRole('button', { name: /C# جامع/ }).click();
            await expect(peer.locator('main [role=alert]')).toContainText('تب دیگه');
            await expect(peer.getByRole('heading',{name:'موضوع‌ها',exact:true})).toBeVisible();
            await expect(peer.getByText('ذخیره شد', { exact: true })).toHaveCount(0);
            await peer.close();
            record('concurrent-tab save conflict refuses overwrite and does not announce a successful save');
        }
        await page.getByRole('link', { name: 'پروفایل من', exact: true }).click();
        await expect(page.locator('.avatar-grid button')).toHaveCount(48);
        await measure('profile and 48 avatars');
        await page.locator('#new-password').fill('Temporary-982!');
        await expect(page.locator('#new-password-confirmation')).toHaveAttribute('required','');
        await page.locator('#new-password-confirmation').fill('different');
        assert.ok(await page.locator('#new-password-confirmation').evaluate(e=>e.validity.customError));
        await page.locator('#new-password').fill('');await page.locator('#new-password-confirmation').fill('');
        assert.ok(await page.locator('#new-password-confirmation').evaluate(e=>e.checkValidity()&&!e.required));
        if (!baseline) await enlarged('profile');
        await page.setViewportSize({ width: 320, height: 720 }); await screenshot('profile-mobile');
        await page.setViewportSize({ width: 1366, height: 900 }); await screenshot('profile-desktop');
        await page.getByRole('button', { name: 'کاراکتر 48', exact: true }).click();
        await expect(page.getByRole('button', { name: 'کاراکتر 48', exact: true })).toHaveAttribute('aria-pressed', baseline ? '' : 'true');
        if (!baseline) {
            await page.getByRole('button', { name: 'کاراکتر 48', exact: true }).focus();
            await page.keyboard.press('Shift+Tab'); await page.keyboard.press('Tab');
            await expect(page.getByRole('button', { name: 'کاراکتر 48', exact: true })).toBeFocused();
            assert.equal(await page.getByRole('button', { name: 'کاراکتر 48', exact: true }).evaluate(element => getComputedStyle(element).outlineColor), 'rgb(227, 182, 107)');
        }
        await page.locator('.avatar-grid').evaluate(grid=>{for(const number of [47,46,45])grid.querySelector(`[aria-label="کاراکتر ${number}"]`).click();});
        await expect(page.getByRole('button',{name:'کاراکتر 45',exact:true})).toHaveAttribute('aria-pressed','true');
        await page.reload();
        await expect(page.getByRole('button', { name: 'کاراکتر 45', exact: true })).toHaveAttribute('aria-pressed', 'true');
        await page.locator('#profile-name').fill('آ');
        await page.locator('#current-password').fill(password);
        await click('ذخیره مشخصات');
        await expect(page.getByText('مشخصاتت ذخیره شد.')).toBeVisible();
        await expect(page.locator('.player-bar-identity strong')).toHaveText('آ');
        await click('خروج');
        await expect(page).toHaveURL(/\/login$/);
        await page.goto(base+'/profile');await expect(page).toHaveURL(/\/login\?ReturnUrl=/i);
        await page.getByLabel('ایمیل', { exact: true }).fill(email);
        await page.getByLabel('رمز عبور', { exact: true }).fill('wrong-test-password');await click('وارد بازی شو');
        await expect(page.locator('#email')).toHaveValue(email);
        await expect(page.locator('input[name=returnUrl]')).toHaveValue('/profile');
        await measure('invalid login password');
        await page.getByLabel('رمز عبور', { exact: true }).fill(password);
        await page.getByRole('checkbox', { name: /مرا به خاطر بسپار/ }).check();
        await click('وارد بازی شو');
        await expect(page).toHaveURL(/\/profile$/);
        const cookie = (await page.context().cookies()).find(cookie => cookie.name.includes('Cookies'));
        assert.ok(cookie?.expires > Date.now() / 1000, 'remember cookie is persistent');
        record('avatar persistence, profile update, logout and remembered login');
        if (!baseline) {
            const oldSession = await browser.newContext({ storageState: await page.context().storageState() });
            oldSession.on('page',watchPage);
            try {
                const oldPage = await oldSession.newPage();
                await oldPage.goto(base+'/lobby?lesson=S08&activity=7');await expect(oldPage.locator('#draft')).toBeVisible();
                const oldToken=await oldPage.locator('input[name=__RequestVerificationToken]').inputValue();
                const oldProfile=await oldSession.newPage();await oldProfile.goto(base+'/profile');await expect(oldProfile.locator('.avatar-grid')).toBeVisible();
                const oldGame=await oldSession.newPage();await oldGame.goto(base+'/lobby?lesson=S05&activity=0');await expect(oldGame.locator('.options input').first()).toBeVisible();
                const invalidForm=await oldSession.newPage();await invalidForm.goto(base+'/profile');await invalidForm.locator('#current-password').fill(password);
                await invalidForm.locator('main form input[name=__RequestVerificationToken]').evaluate(element=>element.remove());
                const invalidResponse=invalidForm.waitForResponse(response=>response.url().endsWith('/auth/profile')&&response.request().method()==='POST');
                await invalidForm.getByRole('button',{name:'ذخیره مشخصات',exact:true}).click();assert.equal((await invalidResponse).status(),400);
                await expect(invalidForm.getByRole('heading',{name:'درخواست معتبر نیست',exact:true})).toBeVisible();
                await measure('invalid stale form',viewports,invalidForm);await enlarged('invalid stale form',invalidForm);await invalidForm.close();
                record('invalid form keeps HTTP 400 and shows a Persian recovery page');
                sql(`UPDATE email_deliveries SET sent_at=clock_timestamp()-interval '3 minutes' WHERE email='${email}'; UPDATE email_challenges SET next_send_at=clock_timestamp()-interval '3 minutes' WHERE email='${email}'`);
                await page.goto(base + '/forgot-password');
                await page.getByLabel('ایمیل حساب', { exact: true }).fill(email);
                await click('ارسال کد تأیید');
                await expect(page).toHaveURL(/\/verify$/);
                await expect(page.getByRole('link',{name:'تغییر ایمیل',exact:true})).toHaveAttribute('href','/forgot-password');
                await page.locator('#code').fill(messages.at(-1).code);
                await click('تأیید ایمیل و ادامه');
                await expect(page).toHaveURL(/\/set-password$/);
                const recoveredPassword = 'Audit-recovered-928!';
                await page.getByLabel('رمز تازه', { exact: true }).fill(recoveredPassword);
                await page.getByLabel('تکرار رمز تازه', { exact: true }).fill(recoveredPassword);
                await click('ذخیره رمز');
                await expect(page).toHaveURL(/\/login\?reset=true$/);
                await oldPage.locator('#draft').fill('این پاسخ هنگام پایان نشست باید برای کپی باقی بماند.');
                await expect(oldPage.locator('main [role=alert]')).toContainText('نشست');
                await expect(oldPage.locator('#draft')).toHaveValue('این پاسخ هنگام پایان نشست باید برای کپی باقی بماند.');
                await expect(oldPage.getByRole('link',{name:'ورود دوباره به حساب',exact:true})).toBeVisible();
                await expect(oldPage.locator('.player-bar')).toHaveCount(0);
                await expect(oldPage.locator('#blazor-error-ui')).toBeHidden();
                await expect(oldPage.locator('#draft')).toHaveAttribute('readonly','');
                await measure('expired editor session',viewports,oldPage);
                await enlarged('expired editor session',oldPage);
                await oldProfile.getByRole('button',{name:'کاراکتر 1',exact:true}).click();
                await expect(oldProfile.locator('main [role=alert]')).toContainText('نشست');
                await expect(oldProfile.getByRole('button',{name:'ذخیره مشخصات',exact:true})).toBeDisabled();
                await expect(oldProfile.locator('#profile-name')).toHaveAttribute('readonly','');
                await expect(oldProfile.locator('#blazor-error-ui')).toBeHidden();
                await measure('expired profile session',viewports,oldProfile);await enlarged('expired profile session',oldProfile);
                await oldGame.locator('.options input').first().check();
                await expect(oldGame.locator('main [role=alert]')).toContainText('نشست');
                await expect(oldGame.locator('.options input').first()).toBeDisabled();
                await expect(oldGame.getByRole('button',{name:'ثبت جواب',exact:true})).toBeDisabled();
                await measure('expired game session',viewports,oldGame);await enlarged('expired game session',oldGame);
                const expiredPost=await oldSession.request.post(base+'/auth/profile',{form:{__RequestVerificationToken:oldToken,name:'آ',currentPassword:password},maxRedirects:0});
                assert.equal(expiredPost.status(),302);assert.equal(new URL(expiredPost.headers().location,base).searchParams.get('returnUrl'),'/profile');
                await oldPage.getByRole('link',{name:'ورود دوباره به حساب',exact:true}).click();await expect(oldPage).toHaveURL(/\/login/);
                record('expired live circuit preserves draft and redirects stale profile submission to login');
                await page.getByLabel('ایمیل', { exact: true }).fill(email);
                await page.getByLabel('رمز عبور', { exact: true }).fill(password);
                await click('وارد بازی شو');
                await expect(page).toHaveURL(/error=credentials/);
                await page.getByLabel('ایمیل', { exact: true }).fill(email);
                await page.getByLabel('رمز عبور', { exact: true }).fill(recoveredPassword);
                await click('وارد بازی شو'); await expect(page).toHaveURL(/\/lobby$/);
                await expect(page.locator('.player-bar-progress')).toContainText('400 امتیاز تجربه');
                record('password recovery, old-session revocation, old-password rejection and preserved progress');
            } finally { await oldSession.close(); }
        }
        await page.setViewportSize({ width: 390, height: 844 });
        await page.evaluate(() => { const dialog = document.querySelector('#components-reconnect-modal'); dialog.className = 'components-reconnect-failed'; dialog.showModal(); });
        await measure('reconnect failure dialog', [{ width: 280, height: 320 }, { width: 390, height: 844 }, { width: 1366, height: 900 }]);
        await page.setViewportSize({ width: 390, height: 844 });
        await screenshot('reconnect-mobile');
        await page.keyboard.press('Escape');
        record('reconnect dialog geometry and Escape');
        completed = true;
    } finally {
        const failed = layouts.filter(layout => layout.findings.length);
        const result = { completed, success:completed&&failed.length===0&&errors.length===0, browser:process.env.ILEARN_AUDIT_BROWSER || 'chrome', baseline, checks, viewports, layoutChecks: layouts.length, failedLayouts: failed, errors };
        fs.writeFileSync(path.join(__dirname, `ui-audit-${baseline ? 'before' : 'after'}.json`), JSON.stringify(result, null, 2));
        fs.writeFileSync(path.join(__dirname, `ui-audit-${baseline ? 'before' : 'after'}-${result.browser}.json`), JSON.stringify(result, null, 2));
        console.log(JSON.stringify({ checks: checks.length, layoutChecks: layouts.length, failedLayouts: failed.length, issueTypes: [...new Set(failed.flatMap(layout => layout.findings))], errors }, null, 2));
        await browser.close(); smtp.close(); model.close();
    }
    if (!baseline) { assert.equal(errors.length, 0, 'browser errors'); assert.equal(layouts.filter(layout => layout.findings.length).length, 0, 'layout findings'); }
})().catch(error => { console.error(error); smtp.close(); model.close(); process.exitCode = 1; });

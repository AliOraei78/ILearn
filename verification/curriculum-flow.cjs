// Use an isolated *_qa database and the local SMTP fixture; no real email is sent.
const fs = require('node:fs');
const path = require('node:path');
const assert = require('node:assert/strict');
const { chromium, expect } = require(process.env.PLAYWRIGHT_MODULE || 'E:/Products/RightPixel/src/rightpixel-web/node_modules/@playwright/test');
const { server: smtp, messages } = require('./test-smtp.cjs')();
const base = process.env.ILEARN_CURRICULUM_URL || 'http://127.0.0.1:5085';
const content = path.join(__dirname, '../ILearn/Content');
const lessons = JSON.parse(fs.readFileSync(path.join(content, 'lessons.json'), 'utf8'));
const revisions = JSON.parse(fs.readFileSync(path.join(content, 'revisions.json'), 'utf8'));
const checks = [], layouts = [], errors = [];
smtp.on('connection', socket => socket.on('error', error => { if (error.code !== 'ECONNRESET') errors.push(error.message); }));

(async () => {
    await new Promise(resolve => smtp.listen(2529, '127.0.0.1', resolve));
    const browser = await chromium.launch({ headless: true, channel: 'chrome' });
    const page = await browser.newPage({ viewport: { width: 1366, height: 900 } });
    page.on('pageerror', error => errors.push(error.message));
    page.on('response', response => { if (response.status() >= 400 && /\.(css|js|woff2|svg)(\?|$)/.test(response.url())) errors.push(response.status() + ' ' + response.url()); });
    const click = name => page.getByRole('button', { name, exact: true }).click();
    const record = name => { checks.push(name); console.log('PASS ' + name); };
    const measure = async name => {
        for (const width of [320, 390, 768, 1366]) {
            await page.setViewportSize({ width, height: 900 });
            await page.evaluate(() => document.fonts.ready);
            const findings = await page.evaluate(() => {
                const issues = [];
                if (document.documentElement.scrollWidth > innerWidth + 1) issues.push('document overflow');
                for (const element of document.querySelectorAll('button,.option,select,textarea')) {
                    if (!element.checkVisibility()) continue;
                    const box = element.getBoundingClientRect();
                    if (box.left < -1 || box.right > innerWidth + 1) issues.push('outside viewport: ' + element.textContent.trim().slice(0, 40));
                    if (box.width < 43 || box.height < 43) issues.push('small target: ' + element.tagName);
                }
                return issues;
            });
            layouts.push({ name, width, findings });
            assert.deepEqual(findings, [], name + ' at ' + width);
        }
    };
    const complete = async () => {
        await click('ثبت جواب');
        await expect(page.locator('.work div.feedback[role=status]')).toContainText('درست بود.');
        await expect(page.getByText('تکمیل ثبت شد:', { exact: false })).toBeVisible();
    };
    try {
        await page.goto(base);
        await expect(page.locator('main')).toContainText('10 فصل آماده');
        await measure('public introduction');
        await page.goto(base + '/register');
        const email = `curriculum-${Date.now()}@example.test`, password = 'Curriculum-local-927!';
        await page.getByLabel('نام بازیکن', { exact: true }).fill('آزمون ده فصل');
        await page.getByLabel('ایمیل', { exact: true }).fill(email);
        await click('ارسال کد تأیید');
        await expect(page).toHaveURL(/\/verify$/);
        assert.ok(messages.at(-1)?.code, 'local SMTP received the code');
        await page.locator('#code').fill(messages.at(-1).code);
        await click('تأیید ایمیل و ادامه');
        await expect(page).toHaveURL(/\/set-password$/);
        await page.getByLabel('رمز تازه', { exact: true }).fill(password);
        await page.getByLabel('تکرار رمز تازه', { exact: true }).fill(password);
        await click('ذخیره رمز');
        await expect(page).toHaveURL(/\/lobby$/);
        await page.getByRole('button', { name: /C# جامع/ }).click();
        await expect(page.locator('.chapters > li')).toHaveCount(10);
        assert.deepEqual(await page.locator('.chapters strong').allTextContents(), lessons.map(l => l.Id + ' · ' + l.Title));
        await page.locator('main details summary').click();
        await expect(page.locator('main details li')).toHaveCount(34);
        await measure('ten chapters and remaining roadmap');
        record('registration with local SMTP, ten ordered chapters, 34 future sessions');

        for (const [chapterIndex, lesson] of lessons.entries()) {
            if (chapterIndex > 0) await click('فهرست فصل‌ها');
            await page.getByRole('button', { name: new RegExp('^' + lesson.Id + ' ·') }).click();
            await expect(page.locator('.work .summary')).not.toBeEmpty();
            await measure(lesson.Id + ' introduction');
            await click('شروع چالش‌ها');
            await expect(page.locator('nav button')).toHaveCount(8);
            for (const [index, original] of lesson.Activities.entries()) {
                const key = lesson.Id + '/' + original.Id;
                const activity = { ...original, ...revisions[key] };
                await page.locator('nav button').nth(index).click();
                await expect(page.locator('#activity-title')).toHaveText(activity.Title);
                if (['explain', 'workshop'].includes(activity.Kind)) {
                    await expect(page.getByRole('button', { name: 'ثبت شخصی تکمیل', exact: true })).toBeDisabled();
                    await page.locator('#draft').fill(activity.SolutionCode || activity.Solution);
                    await click('ثبت شخصی تکمیل');
                } else {
                    if (activity.Kind !== 'order') {
                        await click('ثبت جواب');
                        await expect(page.locator('.work div.feedback[role=status]')).toHaveClass(/error/);
                        await expect(page.getByText('تکمیل ثبت شد:', { exact: false })).toHaveCount(0);
                    }
                    if (activity.Kind === 'concept') {
                        await click(activity.Options[Number(activity.Answers[0])]);
                    } else if (activity.Kind === 'experiment') {
                        await click('آزمایش 1');
                        await expect(page.getByRole('button', { name: 'آزمایش 1', exact: true })).toBeDisabled();
                        await page.locator('.options input').nth(Number(activity.Answers[0])).check();
                    } else if (activity.Kind === 'mission') {
                        let node = 0;
                        for (let step = 0; !activity.Game.Nodes[node].Success && step < activity.Game.Limit; step++) {
                            await click(activity.Game.Nodes[node].Choices[0]);
                            node = activity.Game.Nodes[node].Next[0];
                        }
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
                        }
                    } else if (activity.Kind === 'trace') {
                        for (const key of activity.Answers) {
                            const [row, value] = key.split(':').map(Number);
                            if (row > 0) await click('قدم بعد');
                            await expect(page.getByText(`قدم ${row + 1} از ${activity.Rows.length}:`, { exact: false })).toBeVisible();
                            await page.locator('.options input').nth(value).check();
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
                    } else {
                        if (activity.Kind === 'detective') { await click('سرنخ 1'); await click('سرنخ 2'); }
                        for (const answer of activity.Answers) await page.locator('.options input').nth(Number(answer)).check();
                    }
                    await complete();
                    await click('یک راهنمایی');
                    await expect(page.locator('.work p.feedback[role=status]')).toHaveText(activity.Hint);
                    await click('بستن راهنمایی');
                }
                await expect(page.getByText('تکمیل ثبت شد:', { exact: false })).toBeVisible();
                await click('پاسخ کامل');
                await expect(page.locator('#reference-answer')).toContainText(activity.Solution);
                if (activity.SolutionCode) await expect(page.locator('#reference-answer pre')).toHaveText(activity.SolutionCode);
                await measure(key + ' completed with reference');
                if (['S01/pipeline', 'S03/overflow', 'S09/value-copy', 'S10/shallow'].includes(key)) {
                    await page.setViewportSize({ width: 390, height: 844 });
                    await page.screenshot({ path: path.join(__dirname, 'curriculum-' + key.replace('/', '-') + '-mobile.png'), fullPage: true });
                    await page.setViewportSize({ width: 1366, height: 900 });
                }
                await click('بستن پاسخ کامل');
                record(key + ': answer, completion, reference and four widths');
            }
            await expect(page.getByText('همهٔ چالش‌های این فصل تکمیل شد.', { exact: true })).toBeVisible();
            await expect(page.locator('.player-bar-progress')).toContainText((chapterIndex + 1) * 100 + (chapterIndex === 0 ? ' از' : ' امتیاز تجربه'));
        }
        await page.reload();
        await expect(page.locator('#activity-title')).toHaveText(lessons.at(-1).Activities.at(-1).Title);
        await expect(page.locator('#draft')).toHaveValue(lessons.at(-1).Activities.at(-1).SolutionCode);
        await expect(page.locator('.player-bar-progress')).toContainText('1000 امتیاز تجربه');
        await page.setViewportSize({ width: 390, height: 844 });
        await page.locator('#activity-picker').selectOption('0');
        await expect(page.locator('#activity-title')).toHaveText(lessons.at(-1).Activities[0].Title);
        await click('ثبت جواب');
        await expect(page.locator('.player-bar-progress')).toContainText('1000 امتیاز تجربه');
        await page.locator('.options input').first().focus();
        await page.keyboard.press('Tab');
        assert.ok(await page.evaluate(() => getComputedStyle(document.activeElement).outlineStyle !== 'none'));
        await page.setViewportSize({ width: 1366, height: 900 });
        await click('فهرست فصل‌ها');
        await expect(page.locator('.chapters')).toContainText('S01');
        assert.equal(await page.locator('.chapters .muted').filter({ hasText: 'تکمیل‌شده' }).count(), 10);
        await page.screenshot({ path: path.join(__dirname, 'curriculum-complete-desktop.png'), fullPage: true });
        record('reload retains draft, all ten completions and 1000 XP; mobile selection, keyboard focus and replay');
        assert.deepEqual(errors, []);
        fs.writeFileSync(path.join(__dirname, 'curriculum-browser.json'), JSON.stringify({ checks, layouts, errors }, null, 2));
        console.log(JSON.stringify({ passed: checks.length, layouts: layouts.length, errors }));
    } finally { await browser.close(); smtp.close(); }
})().catch(error => { console.error(error); smtp.close(); process.exitCode = 1; });


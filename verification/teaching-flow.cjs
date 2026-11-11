const assert = require('node:assert/strict');
const { once } = require('node:events');
const { chromium, expect } = require(process.env.PLAYWRIGHT_MODULE || 'E:/Products/RightPixel/src/rightpixel-web/node_modules/@playwright/test');
const server = require('./offline-server.cjs');
const openChallenges = require('./teaching-navigation.cjs');

async function verify() {
    let browser;
    try {
        server.listen(0, '127.0.0.1');
        await once(server, 'listening');
        const base = 'http://127.0.0.1:' + server.address().port;
        browser = await chromium.launch({ channel: 'chrome', headless: true });
        const page = await browser.newPage({ viewport: { width: 390, height: 844 }, isMobile: true, hasTouch: true });
        const errors = [];
        page.on('pageerror', error => errors.push(error.message));
        const button = name => page.getByRole('button', { name, exact: true });
        const counter = page.locator('#teaching-session-count');
        const start = button('شروع چالش‌ها');
        const stored = () => page.evaluate(async () => JSON.parse(await ilearnOffline.read('progress')));

        await page.goto(base);
        await page.locator('#profile-name').fill('آزمون جلسه‌ها');
        await button('ساخت پروفایل و شروع').click();
        await button('شروع آموزش').click();
        await expect(counter).toHaveText('جلسه 1 از 4');
        await expect(start).toHaveCount(0);
        await expect(button('بررسی پاسخ')).toBeDisabled();

        await page.getByRole('radio').nth(0).check();
        await button('بررسی پاسخ').click();
        const correction = page.getByRole('status').filter({ hasText: 'پاسخ درست این است.' });
        await expect(correction).toBeVisible();
        await expect(correction.locator('p')).toHaveText(await page.locator('label.option').nth(1).textContent());
        await page.getByRole('radio').nth(1).check();
        await expect(page.getByText('پاسخ درست این است.')).toHaveCount(0);
        await button('بررسی پاسخ').click();
        await expect(page.getByRole('status').filter({ hasText: 'پاسخت درست است.' })).toBeVisible();

        await button('جلسهٔ بعد').click();
        await expect(counter).toHaveText('جلسه 2 از 4');
        await expect(page.locator('pre')).toContainText('dotnet new console -n FirstGame -f net10.0');
        await expect.poll(async () => (await stored()).IntroductionSessions.R001).toBe(1);
        assert.ok(!(await stored()).IntroductionsRead.includes('csharp/R001'));
        await page.goto(base + '/lobby');
        await button('ادامهٔ آموزش').click();
        await expect(counter).toHaveText('جلسه 2 از 4');
        await button('جلسهٔ قبل').click();
        await expect(counter).toHaveText('جلسه 1 از 4');

        for (let number = 2; number <= 4; number++) {
            await button('جلسهٔ بعد').click();
            await expect(counter).toHaveText('جلسه ' + number + ' از 4');
        }
        await start.click();
        const title = page.locator('#activity-title');
        await expect(title).toBeVisible();
        const initialTitle = await title.textContent();
        await button('مرور آموزش این چالش').click();
        await expect(counter).toHaveText('جلسه 3 از 4');
        await button('ادامه چالش‌ها').click();
        await expect(title).toHaveText(initialTitle);
        assert.ok((await stored()).IntroductionsRead.includes('csharp/R001'));

        await page.evaluate(async () => {
            const progress = JSON.parse(await ilearnOffline.read('progress'));
            delete progress.IntroductionSessions;
            progress.Lesson = 'R002';
            progress.Position = progress.Positions.R002 = 2;
            progress.IntroductionsRead.push('csharp/R002');
            await ilearnOffline.saveProgress(JSON.stringify(progress));
        });
        await page.goto(base + '/lobby?lesson=R002');
        await expect(title).toBeVisible();
        await expect(page.locator('#activity-picker')).toHaveValue('2');
        await button('مرور آموزش این چالش').click();
        await expect(counter).toBeVisible();

        await page.goto(base + '/lobby?lesson=R002&view=intro&session=2');
        await expect(counter).toHaveText('جلسه 2 از 3');
        await button('جلسهٔ بعد').click();
        await expect(counter).toHaveText('جلسه 3 از 3');
        await expect(page.getByText('Program.cs(1,1): error CS0103', { exact: true })).toBeVisible();
        const beginnerPractice = page.getByRole('region', { name: 'تمرین مستقل' });
        await expect(beginnerPractice).toContainText('نوبت اول');
        const beginnerCriteria = beginnerPractice.locator('details');
        await expect(beginnerCriteria).not.toHaveAttribute('open', '');
        await beginnerCriteria.locator('summary').click();
        await expect(beginnerCriteria).toHaveAttribute('open', '');
        await expect(beginnerCriteria).toContainText('6 + 2');

        await page.goto(base + '/lobby?lesson=R004&view=intro&session=2');
        await expect(counter).toHaveText('جلسه 2 از 4');
        await expect(page.getByText('++count', { exact: true })).toHaveAttribute('dir', 'ltr');
        await button('جلسهٔ بعد').click();
        await expect(page.getByText('(double)(7 / 2)', { exact: true })).toHaveAttribute('dir', 'ltr');
        await expect(page.getByText('(double)7 / 2', { exact: true })).toHaveAttribute('dir', 'ltr');
        const fractions = page.getByRole('group', { name: 'خودسنجی' });
        await fractions.getByRole('radio', { name: '(int)(13m / 2)', exact: true }).check();
        await button('بررسی پاسخ').click();
        await expect(page.getByRole('status').filter({ hasText: 'پاسخ درست این است.' })).toContainText('13m / 2');
        await fractions.getByRole('radio', { name: '13m / 2', exact: true }).check();
        await button('بررسی پاسخ').click();
        await expect(page.getByRole('status').filter({ hasText: 'پاسخت درست است.' })).toContainText('6.5m');

        await page.goto(base + '/lobby?lesson=R006&view=intro&session=3');
        await expect(counter).toHaveText('جلسه 3 از 3');
        await expect(page.getByText('stock > 0 ? stock : Missing()', { exact: true })).toHaveAttribute('dir', 'ltr');

        await page.goto(base + '/lobby?lesson=R011&view=intro&session=4');
        const overload = page.getByRole('group', { name: 'خودسنجی' });
        await expect(overload).toContainText('همان کلاس');
        await overload.getByRole('radio', { name: 'string Read(string text)', exact: true }).check();
        await button('بررسی پاسخ').click();
        await expect(page.getByRole('status').filter({ hasText: 'پاسخت درست است.' })).toContainText('نوع پارامتر');

        await page.goto(base + '/lobby?lesson=R066&view=intro&session=1');
        await expect(page.getByRole('group', { name: 'خودسنجی' })
            .getByText('Expression<Func<int, bool>>', { exact: true })).toHaveAttribute('dir', 'ltr');

        await page.goto(base + '/lobby?lesson=R069&view=intro&session=4');
        await expect(page.getByRole('group', { name: 'خودسنجی' })
            .getByText('\\z', { exact: true })).toHaveAttribute('dir', 'ltr');

        await page.goto(base + '/lobby?view=chapters');
        await page.locator('main details > summary').click();
        await page.getByRole('button', { name: /^S01 ·/ }).click();
        await expect(counter).toHaveText('جلسه 1 از 4');
        await openChallenges(page, expect);
        await page.locator('#activity-picker').selectOption('9');
        await button('واحد بعد: S02').click();
        await expect(counter).toHaveText('جلسه 1 از 4');
        await expect(page.getByRole('link', { name: 'S01', exact: true })).toBeVisible();
        await button('جلسهٔ بعد').click();
        await expect(counter).toHaveText('جلسه 2 از 4');
        await expect(page.getByRole('columnheader', { name: 'points[0]', exact: true })).toBeVisible();
        await expect(page.locator('.learning-table tbody tr')).toHaveCount(3);
        await expect(page.locator('figcaption').filter({ hasText: 'خروجی' }).first()).toBeVisible();
        assert.ok(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth + 1));

        await page.goto(base + '/lobby?lesson=R020&view=intro&session=3');
        await expect(counter).toHaveText('جلسه 5 از 5');
        await expect(page.locator('#teaching-session-title')).toHaveText('سنجش پروژه با انتظارهای جدا');
        await button('جلسهٔ قبل').click();
        await expect(counter).toHaveText('جلسه 4 از 5');
        await expect(page.locator('#teaching-session-title')).toHaveText('اجرای دفتر امتیاز با ورود واقعی داده');

        await page.evaluate(async () => {
            const progress = JSON.parse(await ilearnOffline.read('progress'));
            progress.IntroductionSessions.R123 = 6;
            await ilearnOffline.saveProgress(JSON.stringify(progress));
        });
        await page.goto(base + '/lobby?lesson=R123');
        await expect(counter).toHaveText('جلسه 9 از 10');
        await expect(page.locator('#teaching-session-title')).toHaveText('تحویل پروژه و دفاع از تصمیم‌ها');
        await button('جلسهٔ بعد').click();
        await expect(counter).toHaveText('جلسه 10 از 10');
        await expect(page).toHaveURL(/session=8$/);
        const practice = page.getByRole('region', { name: 'تمرین مستقل' });
        await expect(practice).toContainText('JSON یا CSV');
        const criteria = practice.locator('details');
        await expect(criteria).not.toHaveAttribute('open', '');
        await criteria.locator('summary').click();
        await expect(criteria).toHaveAttribute('open', '');
        await expect(criteria).toContainText('محتوای دست‌نخورده');
        await openChallenges(page, expect);
        await expect(page.locator('#activity-picker option')).toHaveCount(10);
        assert.equal((await stored()).IntroductionSessions.R123, 7);

        const rejectsInvalidSession = await page.evaluate(async () => {
            const backup = {
                Format: 'ILearn.Offline',
                Version: 1,
                Profile: JSON.parse(await ilearnOffline.read('profile')),
                Progress: JSON.parse(await ilearnOffline.read('progress'))
            };
            ilearnOffline.validateBackup(JSON.stringify(backup));
            delete backup.Progress.IntroductionSessions;
            ilearnOffline.validateBackup(JSON.stringify(backup));
            backup.Progress.IntroductionSessions = { R123: -1 };
            try {
                ilearnOffline.validateBackup(JSON.stringify(backup));
                return false;
            } catch {
                return true;
            }
        });
        assert.ok(rejectsInvalidSession);
        assert.deepEqual(errors, []);
        console.log('PASS teaching order, feedback, expanded-session restore, independent practice, backups, and archive navigation');
    } finally {
        if (browser) await browser.close();
        server.close();
    }
}

verify().catch(error => {
    console.error(error);
    process.exitCode = 1;
});

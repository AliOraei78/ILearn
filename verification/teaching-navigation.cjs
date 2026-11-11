module.exports = async function openChallenges(page, expect) {
    const navigation = page.getByRole('navigation', { name: 'جلسه‌های آموزش' });
    const activity = page.locator('#activity-title');
    await expect(navigation.or(activity)).toBeVisible();
    if (await activity.isVisible()) return;

    const counter = page.locator('#teaching-session-count');
    const start = navigation.getByRole('button', { name: /^(شروع|ادامه) چالش‌ها$/ });
    const next = navigation.getByRole('button', { name: 'جلسهٔ بعد', exact: true });
    while (!(await start.isVisible())) {
        const previous = await counter.textContent();
        await next.click();
        await expect(counter).not.toHaveText(previous);
    }
    await start.click();
    await expect(activity).toBeVisible();
};

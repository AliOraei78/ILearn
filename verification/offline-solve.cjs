// Same authored cases and controls used by the server curriculum audit.
module.exports = function solver(page, expect) {
    const click = name => page.getByRole('button', { name, exact: true }).click();
    async function reorder(a, desired) {
        const labels = await page.locator('.order-row > span').allTextContents();
        const order = labels.map(text => a.Options.findIndex(option => option.trim() === text.replace(/^\d+\.\s*/, '').trim()));
        for (let target = 0; target < desired.length; target++) {
            let position = order.indexOf(desired[target]);
            while (position > target) {
                await click(`بالا بردن قطعه ${position + 1}`);
                [order[position], order[position - 1]] = [order[position - 1], order[position]];
                await expect(page.locator('.order-row > span')).toHaveText(order.map((card, i) => `${i + 1}. ${a.Options[card]}`)); position--;
            }
        }
    }
    function path(a, mission = false) {
        const p = mission ? a.Game : a.Puzzle, queue = [[]];
        while (queue.length) {
            const history = queue.shift(); let state = mission ? 0 : p.Start || 0;
            for (const move of history) state = mission ? p.Nodes[state]?.Next?.[move] : a.Kind === 'domino' ? p.Edges.some(([from, to]) => from === state && to === move) ? move : -1 : p.Transitions[state]?.[move];
            if (state === undefined || state < 0) continue;
            if (history.length && (mission ? p.Nodes[state].Success : a.Kind === 'domino' ? state === p.Target && p.Required.every(i => i === p.Start || history.includes(i)) : p.Targets.includes(state))) return history;
            if (history.length >= p.Limit) continue;
            const count = mission ? (p.Nodes[state].Choices || []).length : a.Options.length;
            for (let i = 0; i < count; i++) if (mission || (a.Kind === 'domino' ? i !== p.Start && !history.includes(i) : a.Kind === 'simulation' || p.Repeat || !history.includes(i))) queue.push([...history, i]);
        }
        throw new Error('No authored path: ' + a.Title);
    }
    return async a => {
        const p = a.Puzzle;
        if (['explain', 'workshop'].includes(a.Kind)) { await page.locator('#draft').fill(a.SolutionCode || a.Solution); await click('ثبت شخصی تکمیل'); return; }
        if (p) {
            if (a.Kind === 'counterexample') { await page.locator('.options input').nth(p.Cases.find(c => c.Success).Input[0]).check(); await click('آزمایش نمونه'); }
            if (a.Kind === 'repair') { const c = p.Cases.find(c => c.Passed.every(Boolean) && c.Input.filter((v, i) => v !== p.Initial[i]).length <= p.Limit); for (const [i, v] of c.Input.entries()) await page.locator('#slot-' + i).selectOption(String(v)); await click('آزمایش تعمیر'); }
            if (['simulation', 'pipeline', 'domino'].includes(a.Kind)) for (const move of path(a)) await click(a.Options[move]);
            if (a.Kind === 'groups') for (const group of p.Groups) { for (const card of group) await page.locator('.options input').nth(card).check(); await click('بررسی گروه'); }
            if (a.Kind === 'questions') { for (const q of p.Questions.slice(0, p.Limit)) await click(q); await page.locator('.options input').nth(p.Target).check(); }
            if (a.Kind === 'contradiction') { const c = p.Cases[0]; for (const card of c.Input) await page.locator('.options input').nth(card).check(); await click('بررسی تناقض'); await click('حذف: ' + a.Options[c.Removals[0]]); }
            if (['argument', 'analogy'].includes(a.Kind)) for (const [i, v] of p.Cases.find(c => c.Success).Input.entries()) await page.locator('#slot-' + i).selectOption(String(v));
            if (a.Kind === 'constraints') { const order = []; while (order.length < a.Options.length) order.push(a.Options.findIndex((_, i) => !order.includes(i) && p.Edges.every(([from, to]) => to !== i || order.includes(from)))); await reorder(a, order); }
            if (a.Kind === 'spectrum') { await page.locator('#spectrum-number').fill(String(p.Min + Math.ceil((p.Low - p.Min) / (p.Step || 1)) * (p.Step || 1))); await page.locator('#spectrum-number').press('Tab'); await click('آزمایش جایگاه'); }
        } else if (a.Kind === 'concept') await click(a.Options[Number(a.Answers[0])]);
        else if (a.Kind === 'experiment') { await click('آزمایش 1'); await page.locator('.options input').nth(Number(a.Answers[0])).check(); }
        else if (a.Kind === 'mission') { let node = 0; for (const choice of path(a, true)) { await click(a.Game.Nodes[node].Choices[choice]); node = a.Game.Nodes[node].Next[choice]; } }
        else if (a.Kind === 'deduction') for (const key of a.Answers) { const [r, c] = key.split(':').map(Number); await page.locator('.deduction tbody tr').nth(r).locator('button').nth(c).click(); }
        else if (['slots', 'assessment'].includes(a.Kind)) for (const key of a.Answers) { const [r, c] = key.split(':'); await page.locator('#slot-' + r).selectOption(c); }
        else if (a.Kind === 'trace') for (const key of a.Answers) { const [r, c] = key.split(':').map(Number); if (r) await click('قدم بعد'); await expect(page.getByText(`قدم ${r + 1} از ${a.Rows.length}:`, { exact: false })).toBeVisible(); await page.locator('.options input').nth(c).check(); }
        else if (a.Kind === 'order') await reorder(a, a.Answers.map(Number));
        else { if (a.Kind === 'detective') { await click('سرنخ 1'); await click('سرنخ 2'); } for (const answer of a.Answers) await page.locator('.options input').nth(Number(answer)).check(); }
        await click('ثبت جواب');
        await expect(page.locator('.work > div.feedback[role=status]')).toContainText('درست بود.');
    };
};

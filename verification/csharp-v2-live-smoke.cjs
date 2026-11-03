const fs=require('node:fs');
const assert=require('node:assert/strict');
const {execFileSync}=require('node:child_process');
const {createHash}=require('node:crypto');
const {chromium,expect}=require(process.env.PLAYWRIGHT_MODULE||'E:/Products/RightPixel/src/rightpixel-web/node_modules/@playwright/test');
const {measure,enlarged,viewports}=require('./layout-checks.cjs');
const base='http://127.0.0.1:5081';
const channel=process.env.ILEARN_LIVE_BROWSER||'chrome';
const lessons=JSON.parse(fs.readFileSync('ILearn/Content/csharp-v2.json','utf8'));
const checks=[],layouts=[],errors=[];
const feedbackActivities=[],feedbackKinds=new Set();
const hash=data=>createHash('sha256').update(data).digest('hex');

(async()=>{
    const hashes={};
    for(const name of ['csharp-v2.json','lessons.json','introductions.json','revisions.json']){
        hashes[name]=hash(fs.readFileSync('ILearn/Content/'+name));
        assert.equal(execFileSync('docker',['exec','ilearn-ilearn-1','sha256sum','/app/Content/'+name],{encoding:'utf8'}).split(/\s/)[0],hashes[name]);
    }
    checks.push('all four curriculum files in Linux container match host SHA-256');
    const account=JSON.parse(fs.readFileSync('ILearn/App_Data/test-account.json','utf8'));
    const browser=await chromium.launch({headless:true,channel});
    const page=await browser.newPage({viewport:{width:1366,height:900}});
    page.on('pageerror',error=>errors.push(error.message));
    page.on('response',response=>{if(response.status()>=400&&/\.(css|js|woff2|svg)(\?|$)/.test(response.url()))errors.push(response.status()+' '+response.url());});
    const click=name=>page.getByRole('button',{name,exact:true}).click();
    async function open(lesson,index){
        await page.goto(base+`/lobby?lesson=${lesson.Id}&activity=${index}`);
        const start=page.getByRole('button',{name:/^(شروع|ادامه) چالش‌ها$/});
        await expect(start.or(page.locator('#activity-title')).first()).toBeVisible();
        if(await start.count())await start.click();
        await expect(page.locator('#activity-title')).toHaveText(lesson.Activities[index].Title);
    }
    async function reviewAnswerFeedback(lesson,index){
        const a=lesson.Activities[index],p=a.Puzzle;await open(lesson,index);await click('شروع مجدد دور');const controls=page.locator('.puzzle-controls');
        await expect(controls).toContainText('دور تازه شروع شد؛ تکمیل و امتیاز قبلی محفوظ‌اند.');await expect(controls).toHaveAttribute('aria-busy','false');
        async function slots(values){for(const [i,value]of values.entries())await page.locator('#slot-'+i).selectOption(String(value));}
        if(a.Kind==='repair'){
            const good=p.Cases.find(c=>c.Passed.every(Boolean)&&c.Input.filter((v,i)=>v!==p.Initial[i]).length<=p.Limit),bad=p.Cases.find(c=>c.Passed.some(v=>!v));assert.ok(good&&bad,a.Title);
            const results=page.locator('.test-results li');await expect(results).toHaveText(p.Tests.map(test=>test+': آزمایش نشده'));
            await slots(good.Input);await expect(results).toHaveText(p.Tests.map(test=>test+': آزمایش نشده'));await click('آزمایش تعمیر');await expect(results).toHaveText(p.Tests.map(test=>test+': قبول'));await expect(controls).toHaveAttribute('aria-busy','false');
            await page.reload();await expect(page.locator('#activity-title')).toHaveText(a.Title);await expect(results).toHaveText(p.Tests.map(test=>test+': قبول'));
            await slots(bad.Input);await expect(results).toHaveText(p.Tests.map(test=>test+': آزمایش نشده'));await click('آزمایش تعمیر');await expect(results).toHaveText(p.Tests.map((test,i)=>test+': '+(bad.Passed[i]?'قبول':'رد')));
        }else if(a.Kind==='questions'){
            await expect(controls).not.toContainText('نامزدهای سازگار:');await expect(controls.locator('.options input')).toHaveCount(a.Options.length);
            for(const [i,question]of p.Questions.slice(0,p.Limit).entries()){
                await click(question);await expect(controls).toContainText(question+': '+(p.Traits[p.Target][i]?'بله':'خیر'));await expect(controls).not.toContainText('نامزدهای سازگار:');await expect(controls.locator('.options input')).toHaveCount(a.Options.length);
            }
            for(const input of await controls.locator('.options input').all())await expect(input).toBeEnabled();
        }else{
            const leaked=/تا این جای مسیر سازگار|پیوند کامل معتبر|نیازمند اصلاح رابطه/;
            await expect(controls).not.toContainText(leaked);const good=p.Cases.find(c=>c.Success);
            for(const [i,value]of good.Input.entries()){await page.locator('#slot-'+i).selectOption(String(value));await expect(controls).not.toContainText(leaked);}
            await page.locator('#slot-'+(good.Input.length-1)).selectOption(String((good.Input.at(-1)+1)%a.Options.length));await expect(controls).not.toContainText(leaked);
        }
        await expect(controls).toHaveAttribute('aria-busy','false');await page.reload();await expect(page.locator('#activity-title')).toHaveText(a.Title);
        if(a.Kind==='questions')await expect(controls).not.toContainText('نامزدهای سازگار:');
        if(['argument','analogy'].includes(a.Kind))await expect(controls).not.toContainText(/تا این جای مسیر سازگار|پیوند کامل معتبر|نیازمند اصلاح رابطه/);
        await measure(page,lesson.Id+'/'+a.Kind+' explicit feedback and reload',layouts);
        if(!feedbackKinds.has(a.Kind)){await enlarged(page,a.Kind+' answer feedback',layouts);feedbackKinds.add(a.Kind);}
        feedbackActivities.push(lesson.Id+'/'+a.Id);checks.push(lesson.Id+'/'+a.Kind+' requires explicit assessment and preserves observed feedback');
    }
    try{
        await page.goto(base+'/lobby');await expect(page).toHaveURL(/\/login/);
        await page.locator('#email').fill(account.email);await page.locator('#password').fill(account.password);await click('وارد بازی شو');await expect(page).toHaveURL(/\/lobby/);
        await expect(page.locator('.player-bar-progress label')).toBeVisible();const xp=await page.locator('.player-bar-progress label').innerText();checks.push('existing local QA account signs in after Docker update');
        await page.getByRole('button',{name:/C# جامع/}).click();await page.locator('#phase-filter').selectOption('0');await expect(page.locator('.chapters:not(.archive-chapters)>li')).toHaveCount(123);
        await page.locator('main details summary').click();await expect(page.locator('.archive-chapters>li')).toHaveCount(10);await measure(page,'Docker chapter list and legacy archive',layouts,viewports);checks.push('123 roadmap units and ten legacy units in published app');
        for(const kind of ['counterexample','repair','simulation','pipeline','groups','questions','contradiction','argument','analogy','constraints','domino','spectrum']){
            const lesson=lessons.find(l=>l.Activities.some(a=>a.Kind===kind));const index=lesson.Activities.findIndex(a=>a.Kind===kind);const activity=lesson.Activities[index];
            await open(lesson,index);await expect(page.locator('.puzzle-controls')).toBeVisible();await measure(page,'Docker '+kind,layouts,['counterexample','groups','questions','contradiction','constraints','repair','argument','analogy'].includes(kind)?viewports:undefined);
            if(kind==='constraints'){
                const controls=page.locator('.puzzle-controls');const leaked=/پیش از[^\n]*(برقرار|نقض شده)/;
                await expect(controls).not.toContainText(leaked);await expect(controls).toContainText('سپس «ثبت جواب» را بزن');
                const before=await page.locator('.order-row > span').allTextContents();await page.getByRole('button',{name:'بالا بردن قطعه 2',exact:true}).click();await expect(page.locator('.order-row > span').first()).toHaveText('1. '+before[1].replace(/^2\.\s*/,''));await expect(controls).not.toContainText(leaked);
                await click('برگشت آخرین انتخاب');await expect(page.locator('.order-row > span')).toHaveText(before);await expect(controls).toHaveAttribute('aria-busy','false');await page.reload();await expect(page.locator('#activity-title')).toHaveText(activity.Title);await expect(controls).not.toContainText(leaked);
                await enlarged(page,'constraints without live answer clues',layouts);checks.push('constraints hide pairwise answers before and after move, undo and reload');
            }
            if(kind==='groups'){
                await click('شروع مجدد دور');
                for(const card of activity.Puzzle.Groups[0])await page.locator('.options input').nth(card).check();
                await click('بررسی گروه');await expect(page.getByRole('button',{name:'برگشت آخرین انتخاب',exact:true})).toBeDisabled();
                await expect(page.locator('.options input:disabled:checked')).toHaveCount(0);
                await page.reload();await expect(page.locator('#activity-title')).toHaveText(activity.Title);
                await expect(page.locator('.puzzle-controls')).toContainText('گروه‌های کشف‌شده: 1');
                await measure(page,'Docker locked group undo and reload',layouts,viewports);await enlarged(page,'Docker locked group undo',layouts);
                checks.push('group discovery clears undo without losing discovered groups after reload');
                const literal=page.locator('.options bdi').filter({hasText:'"A"'}).first();await expect(literal).toHaveText('"A"');assert.equal(await literal.evaluate(e=>getComputedStyle(e).direction),'ltr');
                await enlarged(page,'isolated C# literals in RTL groups',layouts);await page.setViewportSize({width:390,height:844});await page.screenshot({path:`verification/csharp-v2-${channel}-literal-direction-mobile.png`,fullPage:true});await page.setViewportSize({width:1366,height:900});
            }
            if(kind==='contradiction'){
                await click('شروع مجدد دور');for(const card of activity.Puzzle.Cases[0].Input)await page.locator('.options input').nth(card).check();
                await click('بررسی تناقض');await expect(page.locator('.puzzle-controls')).toContainText('مرحلهٔ ۲:');
                await expect(page.getByRole('button',{name:'برگشت آخرین انتخاب',exact:true})).toBeDisabled();
                await measure(page,'Docker contradiction second stage',layouts,viewports);await enlarged(page,'Docker contradiction second stage',layouts);
                await click('اصلاح مجموعهٔ انتخابی');await expect(page.locator('.puzzle-controls')).toContainText('مرحلهٔ ۱:');
                checks.push('contradiction stage two disables the old selection undo and offers explicit stage recovery');
            }
            await click('یک راهنمایی');await expect(page.locator('.work > p.feedback[role=status]')).toHaveText(activity.Hint);await click('بستن راهنمایی');await click('پاسخ کامل');await expect(page.locator('#reference-answer')).toContainText(activity.Solution);await click('بستن پاسخ کامل');checks.push('Docker '+kind+' native surface, hint and reference');
        }
        for(const lesson of lessons)for(const [index,a]of lesson.Activities.entries())if(['repair','argument','analogy','questions'].includes(a.Kind))await reviewAnswerFeedback(lesson,index);
        assert.equal(feedbackActivities.length,37);
        await open(lessons[0],0);await expect(page.locator('.options bdi')).toHaveCount(lessons[0].Activities[0].Options.length);await measure(page,'isolated choice text in RTL',layouts,viewports);await enlarged(page,'RTL choice text',layouts);checks.push('choice and literal direction isolation in final markup');
        const multiline=lessons.find(l=>l.Activities[0].Options.some(option=>option.includes('\n')));await open(multiline,0);
        for(const option of await page.locator('.option bdi').all())assert.equal(await option.evaluate(e=>getComputedStyle(e).whiteSpace),'pre-wrap');
        await measure(page,'Docker multiline output choices',layouts,viewports);await enlarged(page,'Docker multiline output choices',layouts);checks.push('multiline output choices preserve their line breaks');
        await page.goto(base+'/lobby?lesson=R001&view=intro');await expect(page.locator('#lesson-introduction-title')).toHaveText('پیش از شروع چالش‌ها');
        await expect(page.locator('#lesson-introduction-title')).toBeFocused();assert.equal(await page.locator('#lesson-introduction-title').evaluate(e=>getComputedStyle(e).outlineStyle),'none');
        await expect(page.locator('.summary')).toContainText('dotnet --list-sdks');await measure(page,'Docker corrected lesson introduction',layouts,viewports);await enlarged(page,'Docker corrected lesson introduction',layouts);
        await page.setViewportSize({width:1366,height:900});await page.screenshot({path:'verification/full-audit-docker-introduction-desktop.png'});checks.push('introduction keeps heading focus without an input outline and includes actionable setup');
        for(const state of ['show','retrying','failed','paused','resume-failed']) {
            await page.evaluate(state=>{const dialog=document.querySelector('#components-reconnect-modal');dialog.className='components-reconnect-'+state;dialog.showModal();},state);
            await expect(page.locator('#components-reconnect-modal .button-link')).toBeVisible();await measure(page,'Docker recovery '+state,layouts,viewports);await enlarged(page,'Docker recovery '+state,layouts);
            await page.keyboard.press('Escape');await expect(page.locator('#components-reconnect-modal')).not.toBeVisible();checks.push('recovery '+state+' has accessible controls, responsive layout and Escape');
        }
        const final=lessons.at(-1);await open(final,final.Activities.length-1);assert.equal(await page.locator('.activity-prompt').evaluate(e=>getComputedStyle(e).whiteSpace),'pre-line');await click('پاسخ کامل');await expect(page.locator('#reference-answer')).toBeVisible();
        for(const [name,source]of Object.entries(final.Activities.at(-1).ReferenceFiles))await expect(page.locator(`[data-reference-file=${JSON.stringify(name)}] pre`)).toHaveText(source);
        await measure(page,'Docker final multi-file workshop reference',layouts,viewports);await enlarged(page,'Docker multi-file reference',layouts);
        await page.locator('[data-reference-file="Worker/Program.cs"] h4').scrollIntoViewIfNeeded();await page.screenshot({path:'verification/csharp-v2-docker-reference-desktop.png'});
        await page.setViewportSize({width:390,height:844});await page.locator('[data-reference-file="Worker/Worker.csproj"] h4').scrollIntoViewIfNeeded();await page.screenshot({path:'verification/csharp-v2-docker-reference-mobile.png'});
        await expect(page.locator('.player-bar-progress label')).toHaveText(xp);checks.push('all final-project reference files render; QA XP preserved without requesting model');
        const failedLayouts=layouts.filter(l=>l.findings.length);assert.deepEqual(failedLayouts,[]);assert.deepEqual(errors,[]);
        fs.writeFileSync(`verification/csharp-v2-docker-smoke-${channel}.json`,JSON.stringify({completed:true,browser:channel,date:new Date().toISOString(),checks,hashes,feedbackActivities,viewports,layoutChecks:layouts.length,failedLayouts,errors},null,2));
        console.log(JSON.stringify({passed:checks.length,layoutChecks:layouts.length,failedLayouts:failedLayouts.length,errors}));
    }finally{await browser.close();}
})().catch(error=>{console.error(error);process.exitCode=1;});

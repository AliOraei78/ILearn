const fs=require('node:fs');
const path=require('node:path');
const http=require('node:http');
const assert=require('node:assert/strict');
const {execFileSync}=require('node:child_process');
const {chromium,expect}=require(process.env.PLAYWRIGHT_MODULE||'E:/Products/RightPixel/src/rightpixel-web/node_modules/@playwright/test');
const {viewports,core,measure,enlarged}=require('./layout-checks.cjs');
const {server:smtp,messages}=require('./test-smtp.cjs')();
const base=process.env.ILEARN_V2_URL||'http://127.0.0.1:5086';
const channel=process.env.ILEARN_V2_BROWSER||'chrome';
const root=path.join(__dirname,'..');
const current=JSON.parse(fs.readFileSync(path.join(root,'ILearn/Content/csharp-v2.json'),'utf8'));
const legacy=JSON.parse(fs.readFileSync(path.join(root,'ILearn/Content/lessons.json'),'utf8'));
const revisions=JSON.parse(fs.readFileSync(path.join(root,'ILearn/Content/revisions.json'),'utf8'));
const checks=[],layouts=[],errors=[],visited=[];const surfaces=new Set();let modelMode='success',modelRequests=0,completed=false;
const model=http.createServer(async(request,response)=>{
    let raw='';for await(const chunk of request)raw+=chunk;const payload=JSON.parse(raw);modelRequests++;
    assert.equal(payload.model,'nvidia/nemotron-3.5-super-vl:free');assert.equal(payload.provider.allow_fallbacks,false);
    if(modelMode==='error'){response.writeHead(503).end();return;}
    if(modelMode==='slow')await new Promise(resolve=>{const timer=setTimeout(resolve,15000);response.once('close',()=>{clearTimeout(timer);resolve();});});
    if(response.destroyed)return;
    const content=payload.response_format?JSON.stringify({score:100,summary:'این پاسخ با معیار تمرین بررسی شد.',issues:[],nextHint:'حالت مرزی را هم بررسی کن.'}):'این توضیح محلی برای آزمایش رابط است و از پاسخ مرجع استفاده می‌کند.';
    response.setHeader('Content-Type','application/json');response.end(JSON.stringify({choices:[{message:{content}}]}));
});
smtp.on('connection',socket=>socket.on('error',e=>{if(e.code!=='ECONNRESET')errors.push(e.message);}));
const record=name=>{checks.push(name);console.log('PASS '+name);};
const equalSet=(a,b)=>a.length===b.length&&[...a].sort((a,b)=>a-b).every((v,i)=>v===[...b].sort((a,b)=>a-b)[i]);
const topological=a=>{const order=[];while(order.length<a.Options.length)order.push(a.Options.findIndex((_,i)=>!order.includes(i)&&a.Puzzle.Edges.every(([from,to])=>to!==i||order.includes(from))));return order;};
function route(a) {
    const p=a.Puzzle;const queue=[[]];
    while(queue.length) {
        const history=queue.shift();let state=p.Start||0,valid=true;
        if(a.Kind==='domino') {
            for(const card of history){if(!p.Edges.some(([from,to])=>from===state&&to===card)){valid=false;break;}state=card;}
            if(valid&&history.length&&state===p.Target&&p.Required.every(i=>i===(p.Start||0)||history.includes(i)))return history;
        } else {
            for(const move of history){state=p.Transitions[state]?.[move];if(state===undefined||state<0){valid=false;break;}}
            if(valid&&history.length&&p.Targets.includes(state))return history;
        }
        if(!valid||history.length>=p.Limit)continue;
        for(let i=0;i<a.Options.length;i++)if(a.Kind==='domino'?i!==(p.Start||0)&&!history.includes(i):a.Kind==='simulation'||p.Repeat||!history.includes(i))queue.push([...history,i]);
    }
    throw new Error('No route: '+a.Title);
}
function missionRoute(a) {
    const queue=[{node:0,path:[]}];
    while(queue.length){const {node,path}=queue.shift();if(a.Game.Nodes[node].Success)return path;if(path.length<a.Game.Limit)for(const [choice,next]of (a.Game.Nodes[node].Next||[]).entries())queue.push({node:next,path:[...path,choice]});}
    throw new Error('No mission route: '+a.Title);
}

(async()=>{
    await new Promise(resolve=>smtp.listen(Number(process.env.ILEARN_V2_SMTP_PORT||2531),'127.0.0.1',resolve));await new Promise(resolve=>model.listen(Number(process.env.ILEARN_V2_MODEL_PORT||11436),'127.0.0.1',resolve));
    const browser=await chromium.launch({headless:true,channel});const context=await browser.newContext({viewport:{width:1366,height:900}});const page=await context.newPage();
    const watch=p=>{p.on('pageerror',e=>errors.push(e.message));p.on('response',r=>{if(r.status()>=400&&/\.(css|js|woff2|svg)(\?|$)/.test(r.url()))errors.push(r.status()+' '+r.url());});};watch(page);context.on('page',watch);
    const click=async name=>{
        const traceStep = name==='قدم قبل'||name==='قدم بعد' ? Number((await page.getByText(/^قدم \d+ از/).innerText()).match(/\d+/)[0]) : null;
        await page.getByRole('button',{name,exact:true}).click();
        if(traceStep!==null)await expect(page.getByText(new RegExp('^قدم '+(traceStep+(name==='قدم قبل'?-1:1))+' از'))).toBeVisible();
        if(name==='شروع مجدد دور'&&await page.locator('.puzzle-controls').count()) {
            await expect(page.getByText('دور تازه شروع شد؛ تکمیل و امتیاز قبلی محفوظ‌اند.',{exact:true})).toBeVisible();
            await expect(page.locator('.puzzle-controls')).toBeEnabled();
        }
    };
    const feedback=()=>page.locator('.work > div.feedback[role=status]');
    const sql=statement=>execFileSync('docker',['exec','ilearn-postgres-1','psql','-v','ON_ERROR_STOP=1','-U','ilearn','-d','ilearn_csharp_v2','-Atc',statement],{encoding:'utf8'}).trim();
    let email,password;
    const stored=()=>JSON.parse(sql(`SELECT data::text FROM progress WHERE profile_id=(SELECT id FROM profiles WHERE email='${email}')`));
    const patch=(segments,value)=>sql(`UPDATE progress SET data=jsonb_set(data,ARRAY[${segments.map(s=>"'"+s.replaceAll("'","''")+"'").join(',')}],'${JSON.stringify(value).replaceAll("'","''")}'::jsonb,true),revision=revision+1 WHERE profile_id=(SELECT id FROM profiles WHERE email='${email}')`);
    async function open(lesson,index=0,introduction=false) {
        await page.goto(base+`/lobby?lesson=${lesson.Id}&activity=${index}`+(introduction?'&view=intro':''));
        const start=page.getByRole('button',{name:'شروع چالش‌ها',exact:true});
        if(await start.count())await start.click();
        if(introduction&&await page.getByRole('button',{name:'ادامه چالش‌ها',exact:true}).count())await click('ادامه چالش‌ها');
        await expect(page.locator('#activity-title')).toHaveText(lesson.Activities[index].Title);
    }
    async function reorder(a,desired) {
        const labels=await page.locator('.order-row > span').allTextContents();
        const order=labels.map(text=>a.Options.findIndex(option=>option.trim()===text.replace(/^\d+\.\s*/, '').trim()));assert.ok(order.every(i=>i>=0));
        for(let target=0;target<desired.length;target++) {let position=order.indexOf(desired[target]);while(position>target){await page.getByRole('button',{name:`بالا بردن قطعه ${position+1}`,exact:true}).click();[order[position],order[position-1]]=[order[position-1],order[position]];await expect(page.locator('.order-row > span')).toHaveText(order.map((card,i)=>`${i+1}. ${a.Options[card]}`));position--;}}
    }
    async function solve(a) {
        const p=a.Puzzle;
        if(['explain','workshop'].includes(a.Kind)) {await page.locator('#draft').fill(a.SolutionCode||a.Solution);await click('ثبت شخصی تکمیل');return;}
        if(p) {
            if(a.Kind==='counterexample'){const c=p.Cases.find(c=>c.Success);await page.locator('.options input').nth(c.Input[0]).check();await click('آزمایش نمونه');}
            if(a.Kind==='repair'){await page.locator('#slot-0').selectOption('');await expect(page.getByText('پاسخ ناقص',{exact:false})).toBeVisible();const c=p.Cases.find(c=>c.Passed.every(Boolean)&&c.Input.filter((v,i)=>v!==p.Initial[i]).length<=p.Limit);for(const [i,v]of c.Input.entries())await page.locator('#slot-'+i).selectOption(String(v));await click('آزمایش تعمیر');await expect(page.locator('.test-results li')).toHaveText(p.Tests.map(test=>test+': قبول'));}
            if(['simulation','pipeline','domino'].includes(a.Kind))for(const move of route(a))await click(a.Options[move]);
            if(a.Kind==='groups')for(const group of p.Groups){for(const card of group)await page.locator('.options input').nth(card).check();await click('بررسی گروه');}
            if(a.Kind==='questions'){for(const q of p.Questions.slice(0,p.Limit))await click(q);await page.locator('.options input').nth(p.Target).check();}
            if(a.Kind==='contradiction'){const c=p.Cases[0];for(const card of c.Input)await page.locator('.options input').nth(card).check();await click('بررسی تناقض');await click('حذف: '+a.Options[c.Removals[0]]);}
            if(['argument','analogy'].includes(a.Kind)){const c=p.Cases.find(c=>c.Success);for(const [i,v]of c.Input.entries()){await page.locator('#slot-'+i).selectOption(String(v));await expect(page.locator('.puzzle-controls')).not.toContainText(/تا این جای مسیر سازگار|پیوند کامل معتبر|نیازمند اصلاح رابطه/);}}
            if(a.Kind==='constraints')await reorder(a,topological(a));
            if(a.Kind==='spectrum'){const value=p.Min+Math.ceil((p.Low-p.Min)/(p.Step||1))*(p.Step||1);await page.locator('#spectrum-number').fill(String(value));await page.locator('#spectrum-number').press('Tab');await click('آزمایش جایگاه');}
        } else if(a.Kind==='concept')await click(a.Options[Number(a.Answers[0])]);
        else if(a.Kind==='experiment'){await click('آزمایش 1');await page.locator('.options input').nth(Number(a.Answers[0])).check();}
        else if(a.Kind==='mission'){let node=0;for(const choice of missionRoute(a)){await click(a.Game.Nodes[node].Choices[choice]);node=a.Game.Nodes[node].Next[choice];}}
        else if(a.Kind==='deduction')for(const key of a.Answers){const [r,c]=key.split(':').map(Number);await page.locator('.deduction tbody tr').nth(r).locator('button').nth(c).click();}
        else if(['slots','assessment'].includes(a.Kind))for(const key of a.Answers){const [r,c]=key.split(':');await page.locator('#slot-'+r).selectOption(c);}
        else if(a.Kind==='trace'){while(await page.getByRole('button',{name:'قدم قبل',exact:true}).isEnabled())await click('قدم قبل');for(const key of a.Answers){const [r,c]=key.split(':').map(Number);if(r)await click('قدم بعد');await expect(page.getByText(`قدم ${r+1} از ${a.Rows.length}:`,{exact:false})).toBeVisible();await page.locator('.options input').nth(c).check();}}
        else if(a.Kind==='order')await reorder(a,a.Answers.map(Number));
        else {if(a.Kind==='detective'){for(const name of ['سرنخ 1','سرنخ 2'])if(await page.getByRole('button',{name,exact:true}).isEnabled())await click(name);}if(a.Kind==='budget'){await expect(page.locator('.options .muted')).toHaveCount(a.Options.length);for(const [i,cost]of a.Game.Costs.entries())await expect(page.locator('.options .muted').nth(i)).toContainText('هزینه: '+cost);}for(const answer of a.Answers)await page.locator('.options input').nth(Number(answer)).check();}
        await click('ثبت جواب');await expect(feedback()).toContainText('درست بود.');
    }
    try {
        for(const route of ['/','/login','/register','/forgot-password','/reset-password','/verify','/set-password','/missing-csharp-v2','/Error']) {await page.goto(base+route);await measure(page,route,layouts,viewports);await enlarged(page,route,layouts);}
        record('public/account/error pages at all viewport sizes and 200% text');
        const accountPath=path.join(root,`ILearn/App_Data/csharp-v2-browser-${channel}.json`);
        if(fs.existsSync(accountPath)) {
            ({email,password}=JSON.parse(fs.readFileSync(accountPath,'utf8')));await page.goto(base+'/login');await page.locator('#email').fill(email);await page.locator('#password').fill(password);await click('وارد بازی شو');await expect(page).toHaveURL(/\/lobby/);
        } else {
            email=`csharp-v2-${channel}-${Date.now()}@example.test`;password='CSharp-v2-local-927!';
            await page.goto(base+'/register');await page.getByLabel('نام بازیکن',{exact:true}).fill('آزمون کامل مسیر سی شارپ');await page.getByLabel('ایمیل',{exact:true}).fill(email);await click('ارسال کد تأیید');await expect(page).toHaveURL(/\/verify$/);
            await page.locator('#code').fill(messages.at(-1).code);await click('تأیید ایمیل و ادامه');await expect(page).toHaveURL(/\/set-password$/);await page.getByLabel('رمز تازه',{exact:true}).fill(password);await page.getByLabel('تکرار رمز تازه',{exact:true}).fill(password);await click('ذخیره رمز');await expect(page).toHaveURL(/\/lobby$/);fs.writeFileSync(accountPath,JSON.stringify({email,password}));
            await expect(page.getByRole('button',{name:/C# جامع/})).toBeVisible();assert.equal(stored().Lesson||'R001','R001');
        }
        record('local-only registration/login; fresh progress starts at R001');
        if(stored().Entries?.['csharp/R011/analogy']?.Complete) {
            patch(['Entries','csharp/R011/analogy','Feedback'],'درست بود. توضیح قدیمی دربارهٔ overload');
            await open(current.find(l=>l.Id==='R011'),1);await expect(feedback()).toContainText('پارامتر');await expect(feedback()).not.toContainText('overload');
            assert.equal(stored().Entries['csharp/R011/analogy'].Feedback,'درست بود. توضیح قدیمی دربارهٔ overload');
            await page.goto(base+'/lobby');record('saved authored verdicts use current lesson feedback without rewriting progress');
        }
        await measure(page,'signed-in subject lobby',layouts,viewports);await enlarged(page,'signed-in subject lobby',layouts);
        await page.getByRole('button',{name:/C# جامع/}).click();
        for(let phase=1;phase<=11;phase++){await page.locator('#phase-filter').selectOption(String(phase));await expect(page.locator('.chapters:not(.archive-chapters)>li')).toHaveCount(current.filter(l=>l.Phase===phase).length);}
        await page.locator('#chapter-search').fill('R123');await expect(page.locator('.chapters:not(.archive-chapters)>li')).toHaveCount(1);await page.locator('#chapter-search').fill('هیچ فصل مشابهی وجود ندارد');await expect(page.getByText('فصلی پیدا نشد؛ عبارت جست‌وجو یا فاز را تغییر بده.')).toBeVisible();await page.locator('#chapter-search').fill('');await page.locator('#phase-filter').selectOption('0');await expect(page.locator('.chapters:not(.archive-chapters)>li')).toHaveCount(123);await measure(page,'all phases and search',layouts,viewports);await enlarged(page,'chapter search',layouts);
        await page.locator('main details summary').click();await expect(page.locator('.archive-chapters>li')).toHaveCount(10);await measure(page,'legacy archive',layouts);record('11 phase filters, cross-phase search, empty results and separate legacy archive');
        for(const original of [...current,...legacy]) {
            const lesson={...original,Activities:original.Activities.map(a=>({...a,...(revisions[original.Id+'/'+a.Id]||{})}))};
            await page.goto(base+`/lobby?lesson=${lesson.Id}&activity=0&view=intro`);await expect(page.locator('.summary')).not.toBeEmpty();
            await expect(page.locator('#lesson-introduction-title')).toBeFocused();
            await expect(page.locator('h1')).toHaveCount(1);await expect(page.locator('#lesson-introduction-title')).toHaveText('پیش از شروع چالش‌ها');
            assert.equal(await page.locator('#lesson-introduction-title').evaluate(e=>getComputedStyle(e).outlineStyle),'none');
            await measure(page,lesson.Id+' introduction',layouts,viewports);await enlarged(page,lesson.Id+' introduction',layouts);
            if(lesson.Example) {
                const result=page.getByText('نتیجهٔ مثال',{exact:true});
                await expect(result.locator('..')).not.toHaveAttribute('open','');
                await result.click();await expect(result.locator('..').locator('pre')).toHaveText(lesson.ExampleOutput);
                await page.getByText('مباحث این واحد',{exact:true}).click();
                await measure(page,lesson.Id+' expanded teaching example and topics',layouts);
                await result.click();await page.getByText('مباحث این واحد',{exact:true}).click();
            }
            if(lesson.Id==='R001') {
                for(const width of [390,1366]){await page.setViewportSize({width,height:900});await page.screenshot({path:path.join(__dirname,`full-audit-${channel}-introduction-${width}.png`),fullPage:true});}
            }
            await page.getByRole('button',{name:/^(شروع|ادامه) چالش‌ها$/}).click();
            for(const [index,a]of lesson.Activities.entries()) {
                await page.locator('nav button').nth(index).click();await expect(page.locator('#activity-title')).toHaveText(a.Title);await measure(page,lesson.Id+'/'+a.Id+' ready',layouts);
                const first=!surfaces.has(a.Kind);if(first){await measure(page,a.Kind+' full viewport matrix',layouts,viewports);await enlarged(page,a.Kind,layouts);}
                if(a.Puzzle||a.Game)await click('شروع مجدد دور');
                await expect(page.locator('#reference-answer')).toHaveCount(0);
                assert.equal(await page.locator('#activity-title').evaluate(e=>getComputedStyle(e).outlineStyle),'none');
                if(a.Prompt.includes('\n'))assert.equal(await page.locator('.activity-prompt').evaluate(e=>getComputedStyle(e).whiteSpace),'pre-line');
                for(const option of await page.locator('.option bdi').all())assert.equal(await option.evaluate(e=>getComputedStyle(e).whiteSpace),'pre-wrap');
                if(a.Kind==='repair')await expect(page.locator('.test-results li')).toHaveText(a.Puzzle.Tests.map(test=>test+': آزمایش نشده'));
                await solve(a);await expect(page.getByText('امتیاز تکمیل ثبت شد:',{exact:false})).toBeVisible();
                for(const select of await page.locator('.slot select').all()) {
                    const selection=await select.evaluate(e=>({text:e.selectedOptions[0].textContent,description:e.getAttribute('aria-describedby')}));
                    assert.ok(selection.description);await expect(page.locator('#'+selection.description+' bdi')).toHaveText(selection.text);
                }
                if(!['explain','workshop'].includes(a.Kind)){await click('یک راهنمایی');await expect(page.locator('.work > p.feedback[role=status]')).toHaveText(a.Hint);await click('بستن راهنمایی');}
                await click('پاسخ کامل');await expect(page.locator('#reference-answer')).toContainText(a.Solution);if(a.SolutionCode)await expect(page.locator('#reference-code')).toHaveText(a.SolutionCode);for(const [name,source]of Object.entries(a.ReferenceFiles||{}))await expect(page.locator(`[data-reference-file=${JSON.stringify(name)}] pre`)).toHaveText(source);await measure(page,lesson.Id+'/'+a.Id+' reference and complete',layouts,first?viewports:core);
                if(first)await enlarged(page,a.Kind+' selected answer and reference',layouts);
                if(first){await page.setViewportSize({width:390,height:844});await page.screenshot({path:path.join(__dirname,`csharp-v2-${channel}-${a.Kind}-mobile.png`),fullPage:true});await page.setViewportSize({width:1366,height:900});surfaces.add(a.Kind);}
                await click('بستن پاسخ کامل');
                visited.push(lesson.Id+'/'+a.Id);
                if(a.Puzzle&&first) {
                    const key=`csharp/${lesson.Id}/${a.Id}`;const before=stored().Entries[key];await page.reload();await expect(page.locator('#activity-title')).toHaveText(a.Title);assert.deepEqual(stored().Entries[key].GameState,before.GameState);await click('ثبت جواب');await expect(feedback()).toContainText('درست بود.');await click('شروع مجدد دور');await solve(a);assert.equal(stored().Entries[key].Complete,true);record(a.Kind+' state/reload/restart/replay');
                }
            }
            record(lesson.Id+': '+lesson.Activities.length+' activities, introduction, reference, hint and completion');
        }
        const expected=[...current,...legacy].flatMap(l=>l.Activities).reduce((sum,a)=>sum+(a.Kind==='workshop'?30:10),0);
        assert.equal(visited.length,current.reduce((n,l)=>n+l.Activities.length,0)+legacy.reduce((n,l)=>n+l.Activities.length,0));
        await expect(page.locator('.player-bar-progress')).toContainText(String(expected)+' امتیاز تجربه');await page.reload();await expect(page.locator('.player-bar-progress')).toContainText(String(expected)+' امتیاز تجربه');record('all 585 activities completed; '+expected+' XP survives reload without duplication');
        const unit=current.find(l=>l.Id==='R003'),game=unit.Activities.find(a=>a.Kind==='simulation');await open(unit,unit.Activities.indexOf(game));await click('شروع مجدد دور');await click(game.Options[1]);await click('برگشت آخرین حرکت');assert.deepEqual(stored().Entries['csharp/R003/simulation'].GameState.History,[]);await click(game.Options[1]);await page.reload();await expect(page.getByText('وضعیت فعلی: n=2')).toBeVisible();await solveAfterPrefix();
        async function solveAfterPrefix(){await click(game.Options[1]);await click('ثبت جواب');await expect(feedback()).toContainText('درست بود.');}
        record('simulation undo and partial-round persistence');
        const repairUnit=current.find(l=>l.Activities.some(a=>a.Kind==='repair'));await open(repairUnit,repairUnit.Activities.findIndex(a=>a.Kind==='repair'));const repairSlot=page.locator('#slot-0');await repairSlot.focus();await repairSlot.selectOption('0');await expect(page.locator('.puzzle-controls')).toHaveAttribute('aria-busy','false');await expect(repairSlot).toBeFocused();
        const scaleUnit=current.find(l=>l.Activities.some(a=>a.Kind==='spectrum'));await open(scaleUnit,scaleUnit.Activities.findIndex(a=>a.Kind==='spectrum'));const scaleNumber=page.locator('#spectrum-number');await scaleNumber.focus();await scaleNumber.fill('42');await expect(page.locator('.puzzle-controls')).toHaveAttribute('aria-busy','false');await expect(scaleNumber).toBeFocused();record('background saves preserve keyboard focus in puzzle selects and number input');
        const pipeline=current.find(l=>l.Id==='R063');await open(pipeline,1);await click('شروع مجدد دور');await click(pipeline.Activities[1].Options[0]);await click(pipeline.Activities[1].Options[1]);await click('حذف عملیات 2');await click('برگشت آخرین حرکت');assert.deepEqual(stored().Entries['csharp/R063/pipeline'].GameState.History,[0,1]);await click('بالا بردن عملیات 2');await click('ثبت جواب');await expect(feedback()).toContainText('درست بود.');record('pipeline add/remove/reorder/undo and alternative valid route');
        const counter=current[3];await open(counter,1);await click('شروع مجدد دور');await page.locator('.options input').nth(1).check();await click('آزمایش نمونه');await page.locator('.options input').nth(2).check();await click('آزمایش نمونه');await click('برگشت آخرین انتخاب');assert.equal(stored().Entries['csharp/R004/counterexample'].GameState.Trials.length,2);await click('ثبت جواب');await expect(feedback()).toContainText('درست بود.');record('undo preserves revealed information and accepts another counterexample');
        await open(current[0],0);await page.locator('.options input').nth((Number(current[0].Activities[0].Answers[0])+1)%3).check();await click('ثبت جواب');await expect(feedback()).toContainText('هنوز درست نیست');await click('مرور اشتباه‌ها (1)');await expect(page.locator('#activity-title')).toHaveText(current[0].Activities[0].Title);await solve(current[0].Activities[0]);await expect(page.getByRole('button',{name:'مرور اشتباه‌ها (0)',exact:true})).toBeVisible();record('wrong-answer queue and correction retain earned XP');
        await open(current[0],current[0].Activities.length-1);modelMode='error';await click('بررسی پاسخ با هوش مصنوعی');await expect(feedback()).toContainText('مدل در دسترس نیست');assert.ok(await page.locator('#draft').inputValue());modelMode='slow';await click('بررسی پاسخ با هوش مصنوعی');await expect(page.getByText('در حال بررسی…',{exact:true})).toBeVisible();await click('لغو بررسی');modelMode='success';await click('بررسی پاسخ با هوش مصنوعی');await expect(feedback()).toContainText('100');await click('پاسخ کامل');await expect(page.locator('#reference-answer')).toBeVisible();if(await page.getByRole('button',{name:'توضیح ساده‌تر با هوش مصنوعی',exact:true}).count())await click('توضیح ساده‌تر با هوش مصنوعی');await expect(page.locator('#reference-answer')).toContainText('این توضیح محلی');await click('بستن پاسخ کامل');record('optional model failure/cancel/success and requested or cached reference explanation use local fixture');
        const draft=await page.locator('#draft').inputValue();sql(`UPDATE progress SET revision=revision+1 WHERE profile_id=(SELECT id FROM profiles WHERE email='${email}')`);await click('چالش قبل');await expect(page.locator('main [role=alert]')).toContainText('ذخیره نشد');await expect(page.locator('#draft')).toHaveValue(draft);await expect(page.locator('#activity-title')).toHaveText(current[0].Activities.at(-1).Title);await measure(page,'save conflict keeps current editor',layouts,viewports);record('save conflict blocks navigation and keeps local draft');
        await open(current[0],0);await solve(current[0].Activities[0]);await expect(page.getByText('ذخیره شد',{exact:true})).toBeVisible();const cleanRevision=Number(sql(`SELECT revision FROM progress WHERE profile_id=(SELECT id FROM profiles WHERE email='${email}')`));await page.getByRole('link',{name:'پروفایل من',exact:true}).click();await expect(page.locator('.avatar-grid button')).toHaveCount(48);assert.equal(Number(sql(`SELECT revision FROM progress WHERE profile_id=(SELECT id FROM profiles WHERE email='${email}')`)),cleanRevision);record('leaving a clean page does not write stale progress');await measure(page,'profile',layouts,viewports);await enlarged(page,'profile',layouts);await page.getByRole('button',{name:'کاراکتر 48',exact:true}).focus();await page.keyboard.press('Enter');await expect(page.getByRole('button',{name:'کاراکتر 48',exact:true})).toHaveAttribute('aria-pressed','true');await page.reload();await expect(page.getByRole('button',{name:'کاراکتر 48',exact:true})).toHaveAttribute('aria-pressed','true');record('keyboard profile selection and persistence');
        await open(current[0],current[0].Activities.length-1);const pendingDraft='متن آزمایشی باید با رفتن به پروفایل هم حفظ شود.';await page.locator('#draft').fill(pendingDraft);await page.getByRole('link',{name:'پروفایل من',exact:true}).click();await expect(page.locator('.avatar-grid')).toBeVisible();await expect.poll(()=>stored().Entries['csharp/R001/workshop'].Draft).toBe(pendingDraft);record('navigation flushes an unsaved draft without writing clean pages');
        await open(current[0],1);await page.setViewportSize({width:390,height:844});await page.locator('#activity-picker').selectOption('2');await expect(page.locator('#activity-title')).toHaveText(current[0].Activities[2].Title);await page.keyboard.press('Tab');const focus=await page.evaluate(()=>{const e=document.activeElement;return e&&getComputedStyle(e).outlineStyle!=='none';});assert.ok(focus);await page.goBack();await expect(page.locator('#activity-title')).toHaveText(current[0].Activities[1].Title);await page.goForward();await expect(page.locator('#activity-title')).toHaveText(current[0].Activities[2].Title);record('mobile picker, browser Back/Forward and visible keyboard focus');
        await page.screenshot({path:path.join(__dirname,`csharp-v2-${channel}-final-mobile.png`),fullPage:true});
        completed=true;
    } catch(error) {await page.screenshot({path:path.join(__dirname,`csharp-v2-${channel}-failure.png`),fullPage:true});throw error;} finally {
        const failed=layouts.filter(l=>l.findings.length);const result={completed,browser:channel,date:new Date().toISOString(),curriculumSha256:require('node:crypto').createHash('sha256').update(fs.readFileSync(path.join(root,'ILearn/Content/csharp-v2.json'))).digest('hex'),checks,activities:visited.length,expectedActivities:current.reduce((n,l)=>n+l.Activities.length,0)+legacy.reduce((n,l)=>n+l.Activities.length,0),verifiedActivities:visited,templates:[...surfaces],viewports,layoutChecks:layouts.length,failedLayouts:failed,errors,modelRequests};fs.writeFileSync(path.join(__dirname,`csharp-v2-browser-${channel}.json`),JSON.stringify(result,null,2));console.log(JSON.stringify({completed,checks:checks.length,activities:visited.length,layoutChecks:layouts.length,failedLayouts:failed.length,issues:[...new Set(failed.flatMap(l=>l.findings))],errors}));await browser.close();await new Promise(resolve=>smtp.close(resolve));await new Promise(resolve=>model.close(resolve));if(!completed||failed.length||errors.length)process.exitCode=1;
    }
})().catch(error=>{console.error(error);process.exitCode=1;});

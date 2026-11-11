// Dedicated QA emulator only. Provider traffic is fulfilled by a local browser fixture.
const fs = require('node:fs');
const path = require('node:path');
const assert = require('node:assert/strict');
const { execFileSync } = require('node:child_process');
const { chromium, expect: baseExpect } = require(process.env.PLAYWRIGHT_MODULE || 'E:/Products/RightPixel/src/rightpixel-web/node_modules/@playwright/test');
const openChallenges = require('./teaching-navigation.cjs');
const expect=baseExpect.configure({timeout:30000});
const adb = process.env.ILEARN_ADB || 'C:/Users/ASUS/AppData/Local/Android/Sdk/platform-tools/adb.exe';
const serial = 'emulator-5556', app = 'ir.ilearn.offline', base = 'https://appassets.androidplatform.net';
const run = (...args) => execFileSync(adb, ['-s', serial, ...args], { encoding: 'utf8', timeout: 30000, windowsHide: true }).trim();
const checks = [], layouts = [], errors = [];
let browser, page, requests = 0, completed = false;
const record = name => { checks.push(name); console.log('PASS ' + name); };
async function connect() {
    if (browser) await browser.close();
    let pid;
    for(let attempt=0;;attempt++) {
        try { pid=run('shell','pidof',app);if(/^\d+$/.test(pid))break; } catch {}
        if(attempt>50)throw new Error('App process did not start.');
        await new Promise(resolve=>setTimeout(resolve,200));
    }
    assert.match(pid,/^\d+$/);
    run('forward','tcp:9223','localabstract:webview_devtools_remote_' + pid);
    for (let attempt=0;;attempt++) {
        try { browser=await chromium.connectOverCDP('http://127.0.0.1:9223',{noDefaults:true}); break; }
        catch(error) { if(attempt>30)throw error; await new Promise(resolve=>setTimeout(resolve,200)); }
    }
    page=browser.contexts()[0].pages()[0];
    page.on('pageerror',error=>errors.push(error.message));
    await page.locator('#profile-name, .player-bar-identity strong').first().waitFor({timeout:45000});
    await browser.contexts()[0].route('https://openrouter.ai/api/v1/chat/completions',async route=>{
        const request=route.request(), headers={'access-control-allow-origin':'*','access-control-allow-headers':'*'};
        if(request.method()==='OPTIONS')return route.fulfill({status:204,headers});
        const payload=request.postDataJSON(); requests++;
        assert.equal(payload.model,'nvidia/nemotron-3.5-super-vl:free');
        assert.equal(payload.provider.allow_fallbacks,false);
        assert.deepEqual(payload.provider.max_price,{prompt:0,completion:0,request:0});
        assert.equal(request.headers().authorization,'Bearer sk-or-v1-ilearn-local-test-key');
        const content=payload.response_format?JSON.stringify({score:100,summary:'بررسی محلی داخل APK.',issues:[],nextHint:'تمرین بعدی را بخوان.'}):'توضیح آزمایشی مرجع در APK.';
        await route.fulfill({status:200,headers:{...headers,'content-type':'application/json'},body:JSON.stringify({choices:[{message:{content}}]})});
    });
}
const click = name => page.getByRole('button',{name,exact:true}).click();
async function go(url) {
    await page.evaluate(url=>{const a=document.createElement('a');a.href=url;document.body.append(a);a.click();a.remove();},url);
    await expect(page).toHaveURL(base+url);
}
async function screenshot(name) {
    await Promise.race([page.evaluate(()=>new Promise(resolve=>requestAnimationFrame(()=>requestAnimationFrame(resolve)))),new Promise(resolve=>setTimeout(resolve,700))]);
    await new Promise(resolve=>setTimeout(resolve,500));
    run('shell','screencap','-p','/sdcard/ilearn-qa.png');
    run('pull','/sdcard/ilearn-qa.png',path.join(__dirname,'android-'+name+'.png'));
}
async function layout(name) {
    const result=await page.evaluate(()=>{
        const issues=[];
        if(document.documentElement.scrollWidth>innerWidth+1)issues.push('horizontal overflow');
        for(const e of document.querySelectorAll('button,select,input:not([type=hidden]):not([type=checkbox]):not([type=radio]),textarea,.button-link,.game-sidebar a,.option,.brand,summary')) {
            const r=e.getBoundingClientRect();if(!r.width||!r.height||!e.checkVisibility())continue;
            if(r.width<43||r.height<43)issues.push('small target: '+(e.id||e.textContent.trim().slice(0,35)));
            if(r.left < -1||r.right>innerWidth+1)issues.push('outside viewport: '+(e.id||e.textContent.trim().slice(0,35)));
        }
        return {width:innerWidth,height:innerHeight,devicePixelRatio,issues};
    });
    layouts.push({name,...result}); assert.deepEqual(result.issues,[],name);
}
async function dialog(pattern) {
    for(let attempt=0;attempt<8;attempt++) {
        try {
            run('shell','rm','-f','/sdcard/ilearn-ui.xml');
            run('shell','uiautomator','dump','/sdcard/ilearn-ui.xml');
            const xml=run('shell','cat','/sdcard/ilearn-ui.xml');
            if(xml.includes('<hierarchy')&&(!pattern||pattern.test(xml)))return xml;
        } catch {}
        await new Promise(resolve=>setTimeout(resolve,300));
    }
    throw new Error('Native file picker did not become ready.');
}
function nativeTap(xml, pattern) {
    const nodes=xml.match(/<node\b[^>]*>/g)||[];
    const node=nodes.find(n=>pattern.test(n)); assert.ok(node,'Native control not found: '+pattern);
    const [,x1,y1,x2,y2]=node.match(/bounds="\[(\d+),(\d+)\]\[(\d+),(\d+)\]"/).map(Number);
    run('shell','input','tap',String(Math.round((x1+x2)/2)),String(Math.round((y1+y2)/2)));
}
async function keyboard() {
    if(/mInputShown=true/.test(run('shell','dumpsys','input_method'))) {
        run('shell','input','keyevent','4');
        await expect.poll(()=>/mInputShown=true/.test(run('shell','dumpsys','input_method'))).toBe(false);
    }
    await page.locator('#draft').scrollIntoViewIfNeeded();
    const bounds=await page.locator('#draft').boundingBox();
    const dpr=await page.evaluate(()=>devicePixelRatio);
    const targets=await (await fetch('http://127.0.0.1:9223/json/list')).json();
    const description=JSON.parse(targets.find(t=>t.url===page.url()).description);
    const before=await page.evaluate(()=>visualViewport.height);
    run('shell','input','tap',String(Math.round((bounds.x+bounds.width/2)*dpr)),String(Math.round((bounds.y+20)*dpr+description.screenY)));
    await expect.poll(()=>page.evaluate(()=>visualViewport.height)).toBeLessThan(before-80);
    const after=await page.locator('#draft').boundingBox();assert.ok(after.y<await page.evaluate(()=>innerHeight));
    await screenshot('keyboard');
    run('shell','input','keyevent','4');
    await expect.poll(()=>page.evaluate(()=>visualViewport.height)).toBeGreaterThan(before-20);
}
(async()=>{
    assert.equal(run('shell','getprop','ro.product.model'),'Android SDK built for x86_64');
    assert.match(run('shell','dumpsys','package',app),/DEBUGGABLE/);
    run('shell','am','force-stop','com.android.documentsui');
    run('shell','rm','-f','/sdcard/Download/ILearn-backup.json');
    run('shell','pm','clear',app);run('shell','am','start','-W','-f','0x10008000','-n',app+'/.MainActivity');
    run('shell','cmd','connectivity','airplane-mode','enable');run('shell','svc','wifi','disable');run('shell','svc','data','disable');
    assert.equal(run('shell','settings','get','global','airplane_mode_on'),'1');
    assert.match(run('shell','dumpsys','connectivity'),/Active default network: none/);
    try {
        await connect();
        await expect(page.getByRole('heading',{name:'ساخت پروفایل محلی',exact:true})).toBeVisible();
        await layout('native onboarding');await screenshot('onboarding');
        await page.locator('#profile-name').fill('بازیکن موبایل');await page.locator('#profile-avatar').selectOption('avatar-07');await click('ساخت پروفایل و شروع');
        await expect(page.locator('.player-bar-identity strong')).toHaveText('بازیکن موبایل');
        await layout('native lobby');await screenshot('lobby');
        await click('بخش‌های بازی');await expect(page.locator('#game-navigation')).toBeVisible();
        run('shell','input','keyevent','4');await expect(page.locator('.sidebar-toggle')).toHaveAttribute('aria-expanded','false');
        record('APK launches and creates a local profile in airplane mode; Android Back closes mobile menu');
        await go('/lobby?lesson=R001');await openChallenges(page, expect);
        await expect(page.locator('#activity-title')).toBeVisible();
        const first=JSON.parse(fs.readFileSync(path.join(__dirname,'../ILearn/Content/csharp-v2.json'),'utf8'))[0].Activities[0];
        for(const answer of first.Answers)await page.locator('.options input').nth(Number(answer)).check();await click('ثبت جواب');
        await expect(page.locator('.work > .feedback')).toContainText('درست بود.');
        await layout('native game');await screenshot('game');
        await go('/code-room');await expect(page.locator('#draft')).toBeVisible();
        await page.locator('#draft').fill('پیش‌نویس پایدار روی گوشی');
        await expect(page.locator('.code-room-workspace [role=status]').last()).toHaveText('ذخیره شد');
        await keyboard();await layout('native code editor');
        await go('/profile');await page.locator('#api-key').fill('sk-or-v1-ilearn-local-test-key');await click('ذخیره کلید');
        await expect(page.getByText('کلید روی این دستگاه تنظیم شده است.',{exact:true})).toBeVisible();
        const files=run('shell','run-as',app,'ls','shared_prefs').split(/\s+/);
        const preferences=files.map(file=>run('shell','run-as',app,'cat','shared_prefs/'+file)).join('');
        assert.ok(preferences.includes('apiKey'));assert.ok(!preferences.includes('sk-or-v1-ilearn-local-test-key'));
        record('native editor resizes for soft keyboard; OpenRouter key is encrypted by Android Keystore');
        await go('/code-room');await expect(page.locator('#draft')).toHaveValue('پیش‌نویس پایدار روی گوشی');
        await browser.close();browser=null;run('shell','am','force-stop',app);run('shell','am','start','-n',app+'/.MainActivity');await connect();
        await expect(page.locator('.player-bar-identity strong')).toHaveText('بازیکن موبایل');
        await go('/code-room');await expect(page.locator('#draft')).toHaveValue('پیش‌نویس پایدار روی گوشی');
        await click('بررسی پاسخ با هوش مصنوعی');await expect(page.locator('.code-room-workspace .feedback')).toContainText('بررسی محلی داخل APK.');
        await click('ثبت شخصی تکمیل');await expect(page.locator('.code-room-workspace .feedback')).toContainText('ارزیابی شخصی');
        record('profile, draft and encrypted AI key survive process termination; AI works through a local fixture');
        run('shell','settings','put','system','accelerometer_rotation','0');run('shell','settings','put','system','user_rotation','1');
        await expect.poll(()=>page.evaluate(()=>innerWidth>innerHeight)).toBe(true);await layout('native landscape');await screenshot('landscape');
        run('shell','settings','put','system','user_rotation','0');await expect.poll(()=>page.evaluate(()=>innerWidth<innerHeight)).toBe(true);
        run('shell','settings','put','system','font_scale','1.5');
        await new Promise(resolve=>setTimeout(resolve,1500));await connect();await expect(page.locator('#draft')).toBeVisible();await layout('native large system font');await screenshot('large-font');
        run('shell','settings','put','system','font_scale','1.0');await new Promise(resolve=>setTimeout(resolve,1500));await connect();
        record('actual Android portrait, landscape and 150% system font remain usable');
        await go('/interview');await page.locator('#interview-question').click(); await page.locator('#interview-question-search').fill('C100-04'); await expect(page.locator('#interview-question-panel .picker-results button')).toHaveCount(1); await page.locator('#interview-question-panel .picker-results button').click(); await expect(page.locator('#interview-question-title')).toContainText('Task.Run');await page.locator('#draft').fill('تمرین مصاحبه در گوشی');await click('بررسی پاسخ با هوش مصنوعی');await expect(page.locator('.interview-workspace .feedback')).toContainText('بررسی محلی داخل APK.');await layout('native interview');await screenshot('interview');
        await go('/lobby?lesson=R001&activity=3');await expect(page.locator('#draft')).toBeVisible();await page.locator('#draft').fill('تمرین کارگاه موبایل');await click('بررسی پاسخ با هوش مصنوعی');await expect(page.locator('.work > .feedback')).toContainText('بررسی محلی داخل APK.');await click('پاسخ کامل');await click('توضیح ساده‌تر با هوش مصنوعی');await expect(page.locator('#reference-answer')).toContainText('توضیح آزمایشی مرجع در APK.');
        record('AI grading in all three sections and reference explanation execute inside the APK');

        await go('/code-room?subject=csharp&exercise='+encodeURIComponent('code-room/csharp/R002/workshop'));
        await page.locator('#code-exercise').click();
        await expect(page.locator('#code-exercise-search')).toBeFocused();
        assert.ok(await page.locator('#code-exercise-panel .picker-results button').count()<=7);
        await layout('bounded native exercise picker');await screenshot('focused-picker');
        await page.locator('#code-exercise-search').fill('R002');
        await page.locator('#code-exercise-panel .picker-results button').filter({hasText:'چاپ یک جمع'}).click();
        const previousRequests=requests;
        await page.locator('#draft').fill('Console.WriteLine(4 + 6);');await click('بررسی پاسخ با هوش مصنوعی');
        await expect(page.locator('.code-room-workspace .feedback')).toContainText('۱۰۰٪');
        await expect(page.locator('.code-room-workspace .feedback')).toContainText('پاسخ با نمونه');
        assert.equal(requests,previousRequests);
        await layout('native reference match');await screenshot('focused-reference');
        record('compact native pickers and exact reference acceptance without a model request');

        await go('/profile');await click('ذخیره فایل پشتیبان');
        let xml=await dialog(/text="SAVE"/);nativeTap(xml,/resource-id="[^"]*:id\/button_save"|text="SAVE"/);
        await expect.poll(()=>{try{return JSON.parse(run('shell','cat','/sdcard/Download/ILearn-backup.json')).Format}catch{return ''}}).toBe('ILearn.Offline');
        run('pull','/sdcard/Download/ILearn-backup.json',path.join(__dirname,'android-backup.json'));
        const backup=JSON.parse(fs.readFileSync(path.join(__dirname,'android-backup.json'),'utf8'));assert.equal(backup.Format,'ILearn.Offline');assert.ok(!JSON.stringify(backup).includes('sk-or-v1-ilearn-local-test-key'));
        await page.locator('#profile-name').fill('نام موقت گوشی');await click('ذخیره نام');
        await page.locator('#backup-file').click();xml=await dialog(/text="ILearn-backup.json"/);nativeTap(xml,/text="ILearn-backup.json"/);
        await expect(page.getByRole('button',{name:'جایگزینی سیو با پشتیبان',exact:true})).toBeVisible();await click('جایگزینی سیو با پشتیبان');await expect(page.locator('.player-bar-identity strong')).toHaveText('بازیکن موبایل');
        await click('حذف کلید از دستگاه');await expect(page.getByText('هنوز کلیدی تنظیم نشده است؛ ثبت شخصی و پاسخ مرجع آماده‌اند.',{exact:true})).toBeVisible();
        await layout('native profile');await screenshot('profile');
        record('Android document picker exports and restores a backup without storage permission; key stays out of backups');
        assert.equal(run('shell','settings','get','global','airplane_mode_on'),'1');completed=true;
    } catch(error) { try{await screenshot('failure');fs.writeFileSync(path.join(__dirname,'android-failure-ui.xml'),await dialog())}catch{}throw error; }
    finally {
        fs.writeFileSync(path.join(__dirname,'android-report.json'),JSON.stringify({completed,date:new Date().toISOString(),device:serial,checks,layouts,errors,localModelRequests:requests},null,2));
        console.log(JSON.stringify({completed,checks:checks.length,layouts:layouts.length,errors,localModelRequests:requests}));
        if(browser)await browser.close();if(!completed||errors.length)process.exitCode=1;
    }
})().catch(error=>{console.error(error);process.exitCode=1});

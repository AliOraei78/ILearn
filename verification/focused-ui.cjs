// Exercise the published shared UI. Model calls are intercepted; nothing is sent to a provider.
const fs = require('node:fs');
const assert = require('node:assert/strict');
const { chromium, expect } = require(process.env.PLAYWRIGHT_MODULE || 'E:/Products/RightPixel/src/rightpixel-web/node_modules/@playwright/test');
const { measure, enlarged, viewports } = require('./layout-checks.cjs');
const base = 'http://127.0.0.1:5098';
const server = require('./offline-server.cjs');
const lessons = JSON.parse(fs.readFileSync('ILearn/Content/csharp-v2.json', 'utf8'));
const interviews = JSON.parse(fs.readFileSync('ILearn/Content/interviews.json', 'utf8'))[0].Questions;
const layouts = [], checks = [], errors = [], requests = [];
const record = value => { checks.push(value); console.log('PASS ' + value); };
(async () => {
 await new Promise(resolve => server.listen(5098, '127.0.0.1', resolve));
 const browser = await chromium.launch({ channel: process.env.ILEARN_BROWSER || 'chrome', headless: true });
 const page = await browser.newPage({ viewport: {width: 1366, height: 900} });
 page.on('pageerror', error => errors.push(error.message));
 await page.route('https://openrouter.ai/**', async route => {
  if(route.request().method() === 'OPTIONS') return route.fulfill({status:204, headers:{'access-control-allow-origin':'*','access-control-allow-headers':'*'}});
  const body = route.request().postDataJSON(), data = JSON.parse(body.messages[1].content);
  assert.ok(data.referenceAnswer); assert.equal(typeof data.referenceCode, 'string'); assert.ok(data.referenceFiles);
  assert.equal(body.provider.allow_fallbacks, false);
  requests.push(data);
  return route.fulfill({status:200, headers:{'access-control-allow-origin':'*','content-type':'application/json'}, body:JSON.stringify({choices:[{message:{content:JSON.stringify({score:100,summary:'این پاسخ متفاوت، قرارداد همان مرحله را رعایت می‌کند.',issues:[],nextHint:'مرحلهٔ بعد را شروع کن.'})}}]})});
 });
 const go = async url => {
  await page.evaluate(url => { const a=document.createElement('a');a.href=url;document.body.append(a);a.click();a.remove(); }, url);
  await expect(page).toHaveURL(base + url);
  if(url.startsWith('/lobby?lesson=')) {
   await expect(page.locator('#activity-title, #lesson-introduction-title')).toBeVisible();
   if(await page.locator('#lesson-introduction-title').isVisible()) await page.getByRole('button',{name:/^(شروع|ادامه) چالش‌ها$/}).click();
  }
 };
 const click = name => page.getByRole('button',{name,exact:true}).click();
 async function shot(name) {
  await page.screenshot({path:'verification/focused-'+name+'.png',fullPage:true});
 }
 try {
  await page.goto(base); await expect(page.locator('#profile-name')).toBeVisible();
  await page.locator('#profile-name').fill('بررسی تمرین متمرکز');
  await click('ساخت پروفایل و شروع');
  await expect(page.locator('h1')).toHaveText('آموزشگاه و کارگاه');
  await page.evaluate(() => ilearnOffline.setApiKey('sk-or-v1-local-focused-fixture'));
  await go('/lobby?lesson=R002&activity=0');
  await expect(page.locator('#activity-title')).toHaveText('پیش‌بینی مثال');
  await expect(page.locator('.activity-prompt')).not.toContainText('توضیح بده');
  await page.locator('.option input').nth(Number(lessons[1].Activities[0].Answers[0])).check();
  await click('ثبت جواب');
  await expect(page.locator('.work > .feedback')).toContainText('نتیجهٔ این مثال');
  await expect(page.locator('.work > .feedback')).not.toContainText('keyword');
  await measure(page, 'focused choice feedback', layouts, viewports);
  await shot('choice-desktop');
  await go('/lobby?lesson=R002&activity=2');
  await expect(page.locator('.option .learning-text')).not.toHaveCount(0);
  const option = page.locator('.option .learning-text').filter({hasText:'Console'}).first();
  assert.equal(await option.evaluate(e => getComputedStyle(e).direction), 'rtl');
  await expect(option.locator('bdi[dir=ltr]')).not.toHaveCount(0);
  record('choice prompt matches controls; short feedback and isolated Latin terms');
  const workshopIndex = lessons[1].Activities.findIndex(a=>a.Id==='workshop');
  await go('/lobby?lesson=R002&activity='+workshopIndex);
  await expect(page.locator('#activity-title')).toContainText('چاپ یک جمع');
  await page.locator('#draft').fill(lessons[1].Activities[workshopIndex].SolutionCode);
  await click('بررسی پاسخ با هوش مصنوعی');
  await expect(page.locator('.work > .feedback')).toContainText('۱۰۰٪');
  await expect(page.locator('.work')).toContainText('تطبیق پاسخ مرجع');
  assert.equal(requests.length,0);
  await page.locator('#draft').fill('Console.WriteLine(6 + 4);');
  await click('بررسی پاسخ با هوش مصنوعی');
  await expect(page.locator('.work > .feedback')).toContainText('پاسخ متفاوت');
  assert.equal(requests[0].referenceCode,lessons[1].Activities[workshopIndex].SolutionCode);
  await click('پاسخ کامل');
  await enlarged(page,'focused reference',layouts);
  await page.setViewportSize({width:390,height:844});await shot('workshop-mobile');
  record('exact code scores 100 without model; alternative answer receives reference context');
  await go('/code-room?subject=csharp&exercise='+encodeURIComponent('code-room/csharp/R002/workshop'));
  await expect(page.locator('#code-exercise')).toBeVisible();
  await page.locator('#code-exercise').click();
  await expect(page.locator('#code-exercise-search')).toBeFocused();
  assert.ok(await page.locator('#code-exercise-panel .picker-results button').count()<=7);
  const list = page.locator('#code-exercise-panel .picker-results');
  assert.ok((await list.boundingBox()).height <= 302);
  await measure(page,'open exercise picker',layouts,viewports);
  await shot('picker-mobile');
  await page.locator('#code-exercise-search').fill('R123');
  await expect(page.locator('#code-exercise-panel .picker-results button')).not.toHaveCount(0);
  await page.locator('#code-exercise-panel .picker-results button').first().click();
  await expect(page.locator('#code-problem-title')).toContainText('سیاست پردازش');
  await page.locator('#code-exercise').click();
  await page.locator('#code-chapter').click();
  await page.locator('#code-chapter-search').fill('R002');
  await page.locator('#code-chapter-panel .picker-results button').click();
  await expect(page.locator('#code-problem-title')).toContainText('چاپ یک جمع');
  await expect(page.locator('#code-exercise-panel .picker-results button').first()).toContainText('R002');
  await page.locator('#code-exercise').focus();await page.keyboard.press('Escape');
  await expect(page.locator('#code-exercise')).toBeFocused();
  await expect(page.locator('#code-exercise-panel')).toHaveCount(0);
  record('bounded searchable chapter/exercise pickers; selection and Escape keyboard navigation');
  await go('/interview?question=C001');
  await expect(page.locator('#interview-question-title')).toHaveText(interviews[0].Activity.Title);
  await page.locator('#interview-question').click();
  await measure(page,'open interview picker',layouts,viewports);
  await page.locator('#interview-question-search').fill('C100-04');
  await page.locator('#interview-question-panel .picker-results button').click();
  await expect(page.locator('#interview-question-title')).toContainText('Task.Run');
  await page.locator('#interview-question').click();await page.keyboard.press('Escape');
  await enlarged(page,'focused interview',layouts);
  await shot('interview-mobile');
  await page.setViewportSize({width:1366,height:900});await shot('interview-desktop');
  record('split interview deep links, search and responsive layout');
  if(process.argv.includes('--all')) {
   const archive=JSON.parse(fs.readFileSync('ILearn/Content/lessons.json','utf8'));
   const revisions=JSON.parse(fs.readFileSync('ILearn/Content/revisions.json','utf8'));
   const seen=new Set();let activities=0;
   for(const lesson of [...lessons,...archive]) {
    for(const [index,activity] of lesson.Activities.entries()) {
     await go('/lobby?lesson='+lesson.Id+'&activity='+index);
     await expect(page.locator('#activity-title')).toHaveText(activity.Title);
     await click('پاسخ کامل');
     await expect(page.locator('#reference-answer > p').first()).toContainText(revisions[lesson.Id+'/'+activity.Id]?.Solution || activity.Solution);
     if(activity.SolutionCode)await expect(page.locator('#reference-code')).toHaveText(activity.SolutionCode);
     if(!seen.has(activity.Kind)) {
      await measure(page,'full activity '+activity.Kind,layouts,[{width:390,height:844},{width:1366,height:900}]);seen.add(activity.Kind);
     }
     activities++;
    }
   }
   record('all '+activities+' academy/archive activities and references; '+seen.size+' game kinds');
   for(const question of interviews) {
    await go('/interview?question='+question.Activity.Id);
    await expect(page.locator('#interview-question-title')).toHaveText(question.Activity.Title);
    await page.locator('.interview-workspace details').nth(2).locator('summary').click();
    await expect(page.locator('.interview-workspace details').nth(2)).toContainText(question.Activity.Solution);
   }
   record('all '+interviews.length+' focused interview questions and references render');
   const samples=JSON.parse(fs.readFileSync('ILearn/Content/code-room.json','utf8'));
   const exercises=[...samples,...lessons.flatMap(l=>l.Activities.filter(a=>a.Kind==='workshop').map(a=>({Subject:'csharp',LessonId:l.Id,Activity:a})))];
   for(const exercise of exercises) {
    await go('/code-room?subject='+exercise.Subject+'&exercise='+encodeURIComponent('code-room/'+exercise.Subject+'/'+exercise.LessonId+'/'+exercise.Activity.Id));
    await expect(page.locator('#code-problem-title')).toHaveText(exercise.Activity.Title);
    await expect(page.locator('#draft')).toHaveAttribute('dir', exercise.Activity.AnswerFormat==='text'?'rtl':'ltr');
   }
   record('all '+exercises.length+' code-room exercises render with the correct editor direction');
  }
  const findings=layouts.filter(r=>r.findings.length);
  fs.writeFileSync('verification/focused-browser-report.json',JSON.stringify({checks,layouts,errors,modelRequests:requests.length,findings},null,2));
  assert.deepEqual(errors,[]);assert.deepEqual(findings,[]);
 } catch(error) { await shot('failure'); throw error; }
 finally { await browser.close(); await new Promise(resolve => server.close(resolve)); }
})().catch(error=>{console.error(error);process.exitCode=1;});

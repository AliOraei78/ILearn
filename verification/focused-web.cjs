// Real server-rendered Blazor, isolated QA database, local SMTP and local model fixture.
const fs=require('node:fs'), path=require('node:path'), http=require('node:http'), assert=require('node:assert/strict');
const {spawn,execFileSync}=require('node:child_process');
const {chromium,expect}=require(process.env.PLAYWRIGHT_MODULE||'E:/Products/RightPixel/src/rightpixel-web/node_modules/@playwright/test');
const {measure,enlarged,core}=require('./layout-checks.cjs');
const mail=require('./test-smtp.cjs')();
mail.server.on('connection',socket=>socket.on('error',()=>{}));
const settings=Object.fromEntries(fs.readFileSync('.env','utf8').split(/\r?\n/).map(line=>line.match(/^([A-Z_]+)=(.*)$/)).filter(Boolean).map(m=>[m[1],m[2].replace(/^"|"$/g,'')]));
const base='http://127.0.0.1:5097',database='ilearn_focused_qa',errors=[],layouts=[],checks=[],requests=[];
let app,browser;
const model=http.createServer(async(request,response)=>{
 try {
  let data='';for await(const chunk of request)data+=chunk;
  const body=JSON.parse(data),context=JSON.parse(body.messages[1].content);requests.push(context);
  assert.ok(context.referenceAnswer&&typeof context.referenceCode==='string'&&context.referenceFiles);
  assert.equal(body.provider.allow_fallbacks,false);
  response.writeHead(200,{'content-type':'application/json'}).end(JSON.stringify({choices:[{message:{content:JSON.stringify({score:100,summary:'پاسخ متفاوت با قرارداد همین مرحله سازگار است.',issues:[],nextHint:'ادامه بده.'})}}]}));
 }catch(error){errors.push(error.message);response.writeHead(500).end('{}');}
});
const listen=(server,port)=>new Promise(resolve=>server.listen(port,'127.0.0.1',resolve));
const record=name=>{checks.push(name);console.log('PASS '+name);};
(async()=>{
 const sql=statement=>execFileSync('docker',['exec','ilearn-postgres-1','psql','-U','ilearn','-d','postgres','-Atc',statement],{encoding:'utf8',windowsHide:true}).trim();
 if(sql(`SELECT count(*) FROM pg_database WHERE datname='${database}'`)==='0')sql(`CREATE DATABASE ${database}`);
 await listen(mail.server,2534);await listen(model,11448);
 app=spawn(path.resolve('.tools/dotnet/dotnet.exe'),[path.resolve('ILearn/bin/Debug/net10.0/ILearn.dll'),'--urls',base,'--contentRoot',path.resolve('ILearn')],{
  windowsHide:true,stdio:['ignore','pipe','pipe'],env:{...process.env,ASPNETCORE_ENVIRONMENT:'Development',
   ConnectionStrings__ILearn:`Host=127.0.0.1;Port=${settings.ILEARN_DB_PORT||5441};Database=${database};Username=ilearn;Password=${settings.ILEARN_DB_PASSWORD};GssEncryptionMode=Disable`,
   Auth__CodeKey:settings.AUTH_CODE_KEY,EmailSettings__Host:'127.0.0.1',EmailSettings__Port:'2534',EmailSettings__EnableSsl:'false',EmailSettings__UserName:'',EmailSettings__Password:'',EmailSettings__FromEmail:'qa@example.test',
   OpenRouter__ApiKey:'focused-local-fixture',OpenRouter__Url:'http://127.0.0.1:11448/api/v1/chat/completions',Logging__LogLevel__Default:'Warning'}});
 const log=fs.createWriteStream('verification/focused-web-host.log');app.stdout.pipe(log);app.stderr.pipe(log);
 for(let attempt=0;;attempt++){
  try{if((await fetch(base+'/login')).ok)break;}catch{}
  if(attempt>100||app.exitCode!==null)throw new Error('QA web host did not start');
  await new Promise(resolve=>setTimeout(resolve,200));
 }
 browser=await chromium.launch({channel:'chrome',headless:true});
 const page=await browser.newPage({viewport:{width:1366,height:900}});
 page.on('pageerror',error=>errors.push(error.message));
 page.on('response',response=>{if(response.status()>=400&&/\.(css|js|woff2|svg)(\?|$)/.test(response.url()))errors.push(response.status()+' '+response.url());});
 const click=name=>page.getByRole('button',{name,exact:true}).click();
 try {
  await page.goto(base+'/register');await page.getByLabel('نام بازیکن',{exact:true}).fill('آزمون تمرین وب');
  await page.getByLabel('ایمیل',{exact:true}).fill(`focused-${Date.now()}@example.test`);await click('ارسال کد تأیید');
  await expect(page).toHaveURL(/\/verify$/);await expect.poll(()=>mail.messages.length).toBeGreaterThan(0);
  await page.locator('#code').fill(mail.messages.at(-1).code);await click('تأیید ایمیل و ادامه');
  await expect(page).toHaveURL(/\/set-password$/);
  await page.getByLabel('رمز تازه',{exact:true}).fill('Focused-local-2026-Password!');await page.getByLabel('تکرار رمز تازه',{exact:true}).fill('Focused-local-2026-Password!');await click('ذخیره رمز');
  await expect(page).toHaveURL(/\/lobby$/);record('real web account and authenticated circuit use isolated QA data');
  await page.goto(base+'/lobby?lesson=R002&activity=3');
  await expect(page.locator('#activity-title, #lesson-introduction-title')).toBeVisible();
  if(await page.locator('#lesson-introduction-title').isVisible())await page.getByRole('button',{name:/^(شروع|ادامه) چالش‌ها$/}).click();
  await expect(page.locator('#activity-title')).toContainText('چاپ یک جمع');
  await page.locator('#draft').fill('Console.WriteLine(4 + 6);');await click('بررسی پاسخ با هوش مصنوعی');
  await expect(page.locator('.work > .feedback')).toContainText('۱۰۰٪');assert.equal(requests.length,0);
  await page.locator('#draft').fill('Console.WriteLine(10);');await click('بررسی پاسخ با هوش مصنوعی');
  await expect(page.locator('.work > .feedback')).toContainText('پاسخ متفاوت');
  assert.equal(requests[0].referenceCode,'Console.WriteLine(4 + 6);');
  await click('پاسخ کامل');await measure(page,'authenticated web reference',layouts,core);await enlarged(page,'authenticated web reference',layouts);
  await page.screenshot({path:'verification/focused-web-reference.png',fullPage:true});
  record('web exact reference gets 100; other answers use complete reference context');
  await page.goto(base+'/code-room');await page.locator('#code-exercise').click();
  await expect(page.locator('#code-exercise-search')).toBeFocused();await page.locator('#code-exercise-search').fill('R123');
  await expect(page.locator('#code-exercise-panel .picker-results button').first()).toContainText('R123');
  await page.locator('#code-exercise-panel .picker-results button').first().click();
  await expect(page.locator('#code-problem-title')).toContainText('سیاست پردازش');
  await page.locator('#code-chapter').click();await page.locator('#code-chapter-search').fill('R002');await expect(page.locator('#code-chapter-panel .picker-results button')).toHaveCount(1);await page.locator('#code-chapter-panel .picker-results button').click();
  await page.locator('#code-exercise').click();await measure(page,'authenticated web picker',layouts,core);await page.keyboard.press('Escape');
  await page.goto(base+'/interview?question=C053');await expect(page.locator('#interview-question-title')).toHaveText('capture متغیر');
  await page.locator('#interview-question').click();await page.locator('#interview-question-search').fill('C001-03');await expect(page.locator('#interview-question-panel .picker-results button')).toHaveCount(1);await page.locator('#interview-question-panel .picker-results button').click();
  await expect(page.locator('#interview-question-title')).toHaveText('زمان کامپایل AOT');
  await page.locator('.interview-workspace details').nth(2).locator('summary').click();
  await measure(page,'authenticated web interview',layouts,core);
  await page.setViewportSize({width:390,height:844});await page.screenshot({path:'verification/focused-web-interview-mobile.png',fullPage:true});
  record('real web pickers, interview references and responsive layout');
  assert.deepEqual(errors,[]);assert.deepEqual(layouts.filter(r=>r.findings.length),[]);
 }catch(error){await page.screenshot({path:'verification/focused-web-failure.png',fullPage:true});throw error;}finally{
  fs.writeFileSync('verification/focused-web-report.json',JSON.stringify({checks,errors,layouts,modelRequests:requests.length},null,2));
  await browser.close();
 }
})().catch(error=>{console.error(error);process.exitCode=1;}).finally(async()=>{
 if(app)app.kill();mail.server.close();model.close();
});

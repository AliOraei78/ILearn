// Install the signed release over QA and verify retained local data on the dedicated emulator.
const fs=require('node:fs'),path=require('node:path'),assert=require('node:assert/strict'),crypto=require('node:crypto');
const {execFileSync}=require('node:child_process');
const sdk=process.env.ANDROID_HOME||'C:/Users/ASUS/AppData/Local/Android/Sdk';
const adb=path.join(sdk,'platform-tools/adb.exe'),serial='emulator-5556',app='ir.ilearn.offline';
const run=(...args)=>execFileSync(adb,['-s',serial,...args],{encoding:'utf8',windowsHide:true,timeout:30000}).trim();
(async()=>{
 assert.equal(run('shell','getprop','ro.product.model'),'Android SDK built for x86_64');
 const apk=path.resolve('artifacts/ILearn-offline.apk');
 const badging=execFileSync(path.join(sdk,'build-tools/35.0.0/aapt2.exe'),['dump','badging',apk],{encoding:'utf8',windowsHide:true});
 assert.match(badging,/versionCode='3'/);assert.match(badging,/versionName='1\.0\.2'/);assert.ok(!badging.includes('application-debuggable'));
 run('install','-r',apk);run('shell','am','force-stop',app);run('shell','am','start','-n',app+'/.MainActivity');
 let xml='';
 for(let attempt=0;attempt<25;attempt++) {
  run('shell','uiautomator','dump','/sdcard/ilearn-focused-release.xml');
  xml=run('shell','cat','/sdcard/ilearn-focused-release.xml');
  if(xml.includes('بازیکن موبایل')&&xml.includes('آموزشگاه'))break;
  await new Promise(resolve=>setTimeout(resolve,500));
 }
 assert.ok(xml.includes('بازیکن موبایل'),'Local profile must survive the release update');
 assert.ok(xml.includes('آموزشگاه'),'Release must start and render the lobby');
 assert.ok(!run('shell','dumpsys','package',app).includes('DEBUGGABLE'));
 run('shell','screencap','-p','/sdcard/ilearn-focused-release.png');
 run('pull','/sdcard/ilearn-focused-release.png',path.resolve('verification/focused-android-release.png'));
 fs.writeFileSync('verification/focused-android-release.xml',xml);
 const report={passed:true,version:'1.0.2',versionCode:3,debuggable:false,profileRetained:true,bytes:fs.statSync(apk).size,sha256:crypto.createHash('sha256').update(fs.readFileSync(apk)).digest('hex')};
 fs.writeFileSync('verification/focused-release-report.json',JSON.stringify(report,null,2));
 console.log(JSON.stringify(report));
})().catch(error=>{console.error(error);process.exitCode=1;});

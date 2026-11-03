const widths = [280, 320, 360, 390, 480, 481, 600, 601, 760, 761, 768, 900, 901, 1024, 1280, 1366, 1440, 1920];
const sizes = [[320,568],[360,640],[375,667],[390,844],[393,852],[412,915],[414,896],[430,932],[480,800],[640,360],[768,1024],[800,1280],[820,1180],[1024,768],[1280,720],[1366,768],[1440,900],[1536,864],[1600,900],[1920,1080],[2560,1440],[1280,1024],[2002,1222],[3440,1440],[3840,2160],[844,500],[844,501],[1366,500],[1366,501]];
const viewports = [...new Map([...widths.map(width=>({width,height:900})),...sizes.map(([width,height])=>({width,height}))].map(size=>[`${size.width}x${size.height}`,size])).values()];
const core = [{width:280,height:653},{width:390,height:844},{width:768,height:1024},{width:1366,height:900}];
async function measure(page, name, results, dimensions=core) {
    const previous=page.viewportSize();
    for(const size of dimensions) {
        await page.setViewportSize(size);await page.evaluate(()=>document.fonts.ready);
        const findings=await page.evaluate(()=>{
            const visible=e=>e.checkVisibility()&&e.getBoundingClientRect().width>0&&e.getBoundingClientRect().height>0;
            const issues=[];
            if(document.documentElement.scrollWidth>innerWidth+1)issues.push('document overflow');
            for(const e of document.querySelectorAll('button,.button-link,.account-action,.option,.prerequisite,.game-sidebar a,.brand,summary,input:not([type=hidden]):not([type=checkbox]):not([type=radio]),select,textarea')) {
                if(!visible(e))continue;const r=e.getBoundingClientRect();const label=(e.getAttribute('aria-label')||e.textContent||e.id).trim().slice(0,65);
                if(r.width<43||r.height<43)issues.push('small target: '+label);
                if(r.left < -1||r.right>innerWidth+1)issues.push('outside viewport: '+label);
                if(!['SELECT','INPUT','TEXTAREA'].includes(e.tagName)&&e.scrollWidth>e.clientWidth+2)issues.push('clipped: '+label);
            }
            for(const button of document.querySelectorAll('button + button')) {
                const previous=button.previousElementSibling;if(!visible(button)||!visible(previous))continue;
                const a=previous.getBoundingClientRect(),b=button.getBoundingClientRect();
                if(Math.abs(a.top-b.top)<2&&Math.min(Math.abs(a.right-b.left),Math.abs(b.right-a.left))<7)issues.push('adjacent buttons without gap: '+button.textContent.trim().slice(0,30));
            }
            const blocks=[...(document.querySelector('dialog[open]')||document).querySelectorAll('button,select,input:not([type=hidden]),textarea,h1,h2,h3,h4,p,label,pre,summary,th')].filter(visible).map(e=>({e,r:e.getBoundingClientRect()}));
            for(let i=0;i<blocks.length;i++)for(let j=i+1;j<blocks.length;j++) {
                const a=blocks[i],b=blocks[j];if(a.e.contains(b.e)||b.e.contains(a.e))continue;
                if(Math.min(a.r.right,b.r.right)-Math.max(a.r.left,b.r.left)>1&&Math.min(a.r.bottom,b.r.bottom)-Math.max(a.r.top,b.r.top)>1)issues.push('overlap: '+a.e.tagName+'/'+b.e.tagName+' '+a.e.textContent.trim().slice(0,30));
            }
            return [...new Set(issues)];
        });
        results.push({name,...size,findings});
    }
    await page.setViewportSize(previous);
}
async function enlarged(page,name,results) {
    await page.evaluate(()=>document.documentElement.style.fontSize='200%');
    await measure(page,name+' text 200%',results,[{width:320,height:568},{width:768,height:1024},{width:1366,height:900}]);
    await page.evaluate(()=>document.documentElement.style.fontSize='');
}
module.exports={viewports,core,measure,enlarged};

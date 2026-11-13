// Real .NET/PostgreSQL regression: a public browser Origin with an internal Host.
const assert=require('node:assert/strict');
const base=process.env.TEST_URL||'http://127.0.0.1:18086';
const origin='https://final.norm.ar.arcelik';
async function request(path,body,extra={}){const r=await fetch(base+path,{method:body?'POST':'GET',headers:{'X-Test-User':'zeynep',...(body?{'Content-Type':'application/json'}:{}),...extra},...(body?{body:JSON.stringify(body)}:{})});let value;try{value=await r.json()}catch{}return {status:r.status,value};}
(async()=>{
let r=await request('/api/state');assert.equal(r.status,200);const before=r.value;const app=before.apps.find(a=>a.isActive&&!a.isArchived);assert(app);const original=structuredClone(app),edited={...original,name:original.name+' origin check'};
const cmd={action:'Application edited',entity:app.name,after:edited,revision:before._revision};
for(const headers of [{Origin:'https://evil.example'},{Origin:'http://final.norm.ar.arcelik'},{Origin:origin+'.evil.example'},{Origin:'null'},{Origin:origin+'/path'},{Origin:origin,'Sec-Fetch-Site':'cross-site'},{Origin:base}]){r=await request('/api/admin/commands',cmd,headers);assert.equal(r.status,403);assert.equal(r.value.code,'origin_not_allowed');}
r=await request('/api/state');assert.equal(r.value._revision,before._revision);
r=await request('/api/admin/commands',cmd,{Origin:origin,'Sec-Fetch-Site':'same-origin'});assert.equal(r.status,200,JSON.stringify(r));
r=await request('/api/state');assert.equal(r.value.apps.find(a=>a.id===app.id).name,edited.name);
r=await request('/api/admin/commands',{...cmd,after:original,revision:r.value._revision},{Origin:origin+':443','Sec-Fetch-Site':'same-origin'});assert.equal(r.status,200);
r=await request('/api/state');assert.equal(r.value.apps.find(a=>a.id===app.id).name,original.name);
console.log('PASS: public-origin save with internal Host persists after reload; wrong origins/cross-site blocked without a write; original app restored.');
})().catch(e=>{console.error(e);process.exitCode=1});

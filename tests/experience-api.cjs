const assert=require('node:assert/strict'),fs=require('fs');
const base=process.env.TEST_URL||'http://127.0.0.1:18088',origin=process.env.TEST_ORIGIN||JSON.parse(fs.readFileSync('src/AppStore.Web/appsettings.json','utf8')).Hosting.TrustedPublicOrigin;
async function request(path,body,user='zeynep',persona=null,requestOrigin=origin){const r=await fetch(base+path,{method:body?'POST':'GET',redirect:'manual',headers:{'X-Test-User':user,Origin:requestOrigin,...(persona?{'X-Test-Persona':persona}:{}),...(body?{'Content-Type':'application/json'}:{})},...(body?{body:JSON.stringify(body)}:{})});let data;try{data=await r.json()}catch{}return {status:r.status,data}}
(async()=>{
let s=await request('/api/experience');assert.equal(s.status,200);assert.equal(s.data.environment,'Test');const original=structuredClone(s.data.data);
assert.equal((await request('/api/experience/admin',{data:s.data.data,revision:s.data.revision-1,action:'stale'})).status,409);
assert.equal((await request('/api/experience/personal',{key:'favorites',id:'app:pa',selected:true},'zeynep',null,'https://wrong.example')).status,403);
assert.equal((await request('/api/experience/feedback',{topic:'Application idea / need',message:'Synthetic integration test'})).status,200);
assert((await request('/api/experience')).data.data.feedback.some(x=>x.message==='Synthetic integration test'));
assert.equal((await request('/api/experience/open?resource=web%3Aatlas&testUser=zeynep')).status,302);
assert.equal((await request('/api/experience/open?resource=web%3Acafeteria&testUser=zeynep')).status,403);
s=(await request('/api/experience')).data;s.data.testProfiles=[{id:'test-api',name:'TEST employee',employeeId:'TEST-1',companyCode:'DEMO',country:'TR',site:'sutluce-b',testRole:'employee',synthetic:true,initials:'TE'}];const saved=await request('/api/experience/admin',{data:s.data,revision:s.revision,action:'testProfileChanged'});assert.equal(saved.status,200);
assert.equal((await request('/api/experience',undefined,'zeynep','test-api')).data.isAdmin,false);
assert.equal((await request('/api/experience/admin',{data:saved.data.data,revision:saved.data.revision,action:'illegal'},'zeynep','test-api')).status,403);
assert.equal((await request('/api/experience',undefined,'rumeysa','test-api')).status,403);
assert.equal((await request('/api/experience/open?resource=web%3Acafeteria&testUser=zeynep&persona=test-api')).status,302);
s=(await request('/api/experience')).data;assert.equal((await request('/api/experience/admin',{data:original,revision:s.revision,action:'restoreTestFixture'})).status,200);
console.log('PASS: actual PostgreSQL feedback, stale-save and origin rejection, redirects and existing TEST persona permissions.');
})().catch(e=>{console.error(e);process.exitCode=1});

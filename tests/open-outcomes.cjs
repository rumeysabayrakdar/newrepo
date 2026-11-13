// Isolated .NET/PostgreSQL checks; redirect targets are never contacted.
const assert=require('node:assert/strict'),crypto=require('crypto'),{execFileSync}=require('node:child_process');
const base=process.env.TEST_URL||'http://127.0.0.1:18086',id='outcome-test-'+crypto.randomUUID();
async function state(){const r=await fetch(base+'/api/state',{headers:{'X-Test-User':'zeynep'}});assert.equal(r.status,200);return r.json()}
async function command(action,after){const s=await state();const r=await fetch(base+'/api/admin/commands',{method:'POST',headers:{'X-Test-User':'zeynep','Content-Type':'application/json',Origin:'https://final.norm.ar.arcelik'},body:JSON.stringify({action,entity:'APK download outcome test',after,revision:s._revision})});assert.equal(r.status,200,await r.text())}
async function open(app,kind,link){return fetch(base+'/api/open/'+app+'/'+kind+'/'+link+'?testUser=rumeysa',{redirect:'manual'})}
let created=false;
(async()=>{
const s=await state(),template=structuredClone(s.apps.find(a=>a.isActive&&!a.isArchived));const apk='https://downloads.example.test/arcelik.apk';const app={...template,id,name:'APK download outcome test',translations:{},links:[{id:'apk',label:'Android APK',platform:'Android',url:apk,pending:false},{id:'pending',label:'Pending download',platform:'Android',url:'',pending:true},{id:'disabled',label:'Inactive link',platform:'Android',url:apk,pending:false,isActive:false}],documents:[{id:'guide',title:'Guide',url:'guide.html'}]};
await command('Application created',app);created=true;
for(let i=0;i<2;i++){const r=await open(id,'links','apk');assert.equal(r.status,302);assert.equal(r.headers.get('location'),apk)}
for(const link of ['pending','disabled','unknown'])assert.equal((await open(id,'links',link)).status,404);
assert.equal((await open(id,'documents','guide')).status,302);assert.equal((await open('missing-'+id,'links','apk')).status,404);
// Simulate a legacy unsafe stored address; normal Admin validation rejects it.
const sql=`UPDATE appstore.catalogue_state SET payload=jsonb_set(payload,'{apps}',(SELECT jsonb_agg(CASE WHEN a->>'id'='${id}' THEN jsonb_set(a,'{links,0,url}','"javascript:alert(1)"'::jsonb) ELSE a END) FROM jsonb_array_elements(payload->'apps') a)),revision=revision+1 WHERE id=1;`;
execFileSync('docker',['--host=unix:///var/run/docker.sock','exec','-i','arcelik-real-test-postgres-1','psql','-U','appstore_test','-d','arcelik_appstore_test','-v','ON_ERROR_STOP=1'],{input:sql,stdio:['pipe','pipe','pipe']});assert.equal((await open(id,'links','apk')).status,400);await command('Application edited',app);
await command('Application deactivated',{id});assert.equal((await open(id,'links','apk')).status,404);
await command('Application activated',{id});await command('Application archived',{id});assert.equal((await open(id,'links','apk')).status,404);
const events=(await state()).events.filter(e=>e.kind==='user'&&(e.after?.applicationId===id||e.after?.applicationId==='missing-'+id));assert.equal(events.length,10);assert.equal(new Set(events.map(e=>e.id)).size,10);assert.equal(events.filter(e=>e.after.outcome==='redirect_issued').length,3);assert.equal(events.filter(e=>e.after.outcome==='blocked').length,7);
for(const e of events){assert.equal(e.actorId,'rumeysa');assert(e.time);assert(e.after.applicationId);assert(e.after.linkId);assert(e.after.reason);assert([302,404,400].includes(e.after.statusCode))}
console.log('PASS: HTTPS APK links, repeated redirects with unique IDs, documents, pending/inactive/missing/archived attempts and persisted actor/app/link/time/status/outcome for every attempt.');
})().catch(e=>{console.error(e);process.exitCode=1}).finally(async()=>{if(created){const app=(await state()).apps.find(a=>a.id===id);if(!app.isArchived)await command('Application archived',{id})}});

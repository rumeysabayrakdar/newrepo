// Isolated PostgreSQL export and restore proof. Never use a live project DB.
const assert=require('node:assert/strict'),fs=require('fs'),{execFileSync}=require('node:child_process'),crypto=require('crypto');
const base=process.env.TEST_URL||'http://127.0.0.1:18086',container='arcelik-real-test-postgres-1',db='backup_restore_'+Date.now(),tables=['metadata','catalogue_state','users','action_logs','admin_audit_logs'];
function sql(database,statement){return execFileSync('docker',['--host=unix:///var/run/docker.sock','exec','-i',container,'psql','-U','appstore_test','-d',database,'-At','-v','ON_ERROR_STOP=1'],{input:statement,encoding:'utf8',stdio:['pipe','pipe','pipe']}).trim()}
async function post(user,headers={}){return fetch(base+'/api/admin/backup',{method:'POST',headers:{'Content-Type':'application/json',...(user?{'X-Test-User':user}:{}),Origin:'https://final.norm.ar.arcelik',...headers},body:'{}'})}
(async()=>{
assert.equal((await post()).status,401);assert.equal((await post('rumeysa')).status,403);assert.equal((await post('zeynep',{Origin:'https://evil.example'})).status,403);
// Ensure the backup is not the UI's truncated activity snapshot.
for(let i=0;i<205;i++){const r=await fetch(base+'/api/events/search',{method:'POST',headers:{'Content-Type':'application/json','X-Test-User':'rumeysa'},body:JSON.stringify({term:'backup-regression-'+i})});assert.equal(r.status,204)}
const revision=sql('arcelik_appstore_test','SELECT revision FROM appstore.catalogue_state WHERE id=1;');
const r=await post('zeynep');assert.equal(r.status,200);assert(r.headers.get('content-disposition').includes('attachment'));const backup=await r.json();assert.equal(backup.format,'arcelik-appstore-backup');assert.equal(backup.version,1);assert.equal(backup.environment,'Test');assert.deepEqual(Object.keys(backup.tables).sort(),tables.toSorted());assert(backup.counts.action_logs>200);assert.equal(sql('arcelik_appstore_test','SELECT revision FROM appstore.catalogue_state WHERE id=1;'),revision);
for(const table of tables){assert.equal(backup.tables[table].length,backup.counts[table]);assert.equal(backup.counts[table],Number(sql('arcelik_appstore_test','SELECT count(*) FROM appstore.'+table+';')))}
// Restore only into a newly created isolated DB, then compare every exported row.
sql('postgres','CREATE DATABASE '+db+';');try{
sql(db,fs.readFileSync('src/AppStore.Web/Database/schema.sql','utf8'));
for(const table of tables){const json=JSON.stringify(backup.tables[table]),delimiter='$backup_'+crypto.randomBytes(12).toString('hex')+'$';assert(!json.includes(delimiter));sql(db,'INSERT INTO appstore.'+table+' SELECT * FROM jsonb_populate_recordset(NULL::appstore.'+table+','+delimiter+json+delimiter+'::jsonb);');const rows=JSON.parse(sql(db,"SELECT COALESCE(jsonb_agg(to_jsonb(t)), '[]'::jsonb)::text FROM appstore."+table+' t;'));const order=x=>JSON.stringify(x.map(row=>JSON.stringify(row)).sort());assert.equal(order(rows),order(backup.tables[table]));}
}finally{sql('postgres','DROP DATABASE '+db+';')}
console.log('PASS: Admin-only protected download, all five tables and >200 events, no catalogue mutation, every row restored identically into an isolated PostgreSQL database.');
})().catch(e=>{console.error(e);process.exitCode=1});

// Uses only the isolated development PostgreSQL service, never Norm's live DB.
const {execFileSync,spawnSync}=require('node:child_process'),assert=require('node:assert/strict'),fs=require('fs'),path=require('path');
const D=['--host=unix:///var/run/docker.sock'],name='norm-connection-probe',database=process.env.CHECK_DATABASE||'norm_ready_20261116',network=process.env.CHECK_NETWORK||'arcelik-real-test_default',secret=process.env.CHECK_SECRET_VOLUME||'arcelik-real-test_test-secrets',image=process.env.CHECK_IMAGE||'arcelik-appstore:norm-ready';
const run=(...args)=>execFileSync('docker',[...D,...args],{encoding:'utf8',stdio:['pipe','pipe','pipe']}).trim();
async function start(env=[],mounts=[]){run('run','-d','--name',name,'--network',network,'--ulimit','core=0','-p','127.0.0.1:18090:8080','--mount','type=volume,src='+secret+',dst=/run/appstore-secrets,readonly','-e','POSTGRES_PASSWORD_FILE=/run/appstore-secrets/postgres-password',...env.flatMap(e=>['-e',e]),...mounts.flatMap(m=>['--mount',m]),image)}
async function healthy(){for(let i=0;i<80;i++){try{const r=await fetch('http://127.0.0.1:18090/healthz');if(r.ok){const x=await r.json();assert.equal(x.database,'connected');assert.equal(x.environment,'Test');return}}catch{}await new Promise(r=>setTimeout(r,200))}throw Error('Probe did not start')}
function logs(){const r=spawnSync('docker',[...D,'logs',name],{encoding:'utf8'});return (r.stdout||'')+(r.stderr||'')}
function stop(){try{run('rm','-f',name)}catch{}}
(async()=>{
for(const env of [[`DATABASE_URL=postgresql://appstore_test@postgres:5432/${database}`],[`ConnectionStrings__AppStore=Host=postgres;Database=${database};Username=appstore_test`,'Database__Name=']]){await start(env);try{await healthy()}finally{stop()}}
const file=path.resolve('/workspace/work/norm-replaced-appsettings.json'),config=JSON.parse(fs.readFileSync('src/AppStore.Web/appsettings.json','utf8'));config.ConnectionStrings.AppStore=`Host=postgres;Database=${database};Username=appstore_test`;fs.writeFileSync(file,JSON.stringify(config),{mode:0o644});fs.chmodSync(file,0o644);
await start([],['type=bind,src='+file+',dst=/app/appsettings.json,readonly']);try{await healthy()}finally{stop()}
await start([`DATABASE_URL=postgresql://appstore_test@postgres:5432/${database}`,'Database__Name=wrong_database']);try{const code=run('wait',name);assert.notEqual(code,'0');assert(logs().includes('PostgreSQL could not initialize'))}finally{stop()}
const production=path.resolve('deployment/production/appsettings.json');await start([`DATABASE_URL=postgresql://appstore_test@postgres:5432/${database}`,'Authentication__TrustedProxyCidrs__0=127.0.0.1/32'],['type=bind,src='+production+',dst=/app/appsettings.json,readonly']);try{assert.notEqual(run('wait',name),'0');assert(logs().includes('PostgreSQL could not initialize'))}finally{stop()}
console.log('PASS: DATABASE_URL, injected Npgsql and Norm-style JSON replacement work with a generated database name; explicit mismatch and Production reuse of Test data remain blocked.');
})().catch(e=>{console.error(e.message);process.exitCode=1}).finally(stop);

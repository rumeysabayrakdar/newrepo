"""Check Test defaults and Production rejection. Requires a built project image."""
import json,subprocess,sys,time
D=['docker','--host=unix:///var/run/docker.sock'];image=sys.argv[1] if len(sys.argv)>1 else 'arcelik-appstore:test-profiles';name='arcelik-synthetic-test-check'
def run(args):return subprocess.run(D+args,text=True,capture_output=True)
try:
 p=run(['run','-d','--name',name,'-e','ASPNETCORE_ENVIRONMENT=Test','-e','DOTNET_ENVIRONMENT=Test','-e','AppStore__Mode=AuthGate','-e','AppStore__EnableDemo=false',image]);assert p.returncode==0,p.stderr
 for _ in range(50):
  p=run(['exec',name,'wget','-Y','off','-qO-','http://127.0.0.1:8080/healthz'])
  if p.returncode==0:break
  time.sleep(.2)
 assert json.loads(p.stdout)['mode']=='demo',p.stdout+p.stderr
 for route,expected in [('/runtime-config.js','"mode":"demo"'),('/app.js','Manager Workspace'),('/','runtime-config.js')]:
  p=run(['exec',name,'wget','-Y','off','-qO-','http://127.0.0.1:8080'+route]);assert p.returncode==0 and expected in p.stdout,route
 p=run(['exec',name,'wget','-Y','off','-qO-','--header','X-User-Sicil: FORGED','http://127.0.0.1:8080/api/me']);assert json.loads(p.stdout)=={'mode':'demo','verified':False}
 p=run(['exec',name,'cat','/app/appsettings.Test.json']);assert json.loads(p.stdout)['AppStore']['EnableDemo'] is True
 for env in ['Production','Prod']:
  p=run(['run','--rm','-e','ASPNETCORE_ENVIRONMENT='+env,'-e','DOTNET_ENVIRONMENT='+env,image]);assert p.returncode!=0 and 'Production requires' in p.stderr,p.stdout+p.stderr
 print('PASS: Test always uses synthetic profiles despite stale auth settings; no gateway identity accepted; Test config packaged; Production/Prod startup rejected.')
finally:run(['rm','-f',name])

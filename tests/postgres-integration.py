"""Run against an isolated Compose test project; uses synthetic data only.
Usage: python tests/postgres-integration.py http://127.0.0.1:18082 [compose-project]
"""
import copy,json,subprocess,sys,time,urllib.request,urllib.error
base=sys.argv[1] if len(sys.argv)>1 else 'http://127.0.0.1:18082';project=sys.argv[2] if len(sys.argv)>2 else 'arcelik-package-db-test'
class NoRedirect(urllib.request.HTTPRedirectHandler):
 def redirect_request(self,*args):return None
opener=urllib.request.build_opener(urllib.request.ProxyHandler({}),NoRedirect())
def request(path,persona='zeynep',body=None):
 headers={'X-Test-Persona':persona};raw=None
 if body is not None:headers['Content-Type']='application/json';raw=json.dumps(body).encode()
 req=urllib.request.Request(base+path,data=raw,headers=headers)
 try:r=opener.open(req,timeout=10)
 except urllib.error.HTTPError as e:r=e
 text=r.read().decode();
 try:value=json.loads(text)
 except json.JSONDecodeError:value=text
 return r.code,value,r.headers

def read(person='zeynep'):
 status,state,_=request('/api/test/state',person);assert status==200,state;return state

def change(action,entity,after=None,patch=None,person='zeynep',revision=None,expect=200):
 state=read(person);body={'action':action,'entity':entity,'revision':state['_revision'] if revision is None else revision,'after':after,'patch':patch or {}}
 status,result,_=request('/api/test/admin/commands',person,body);assert status==expect,(action,status,result);return result

for _ in range(60):
 try:
  if request('/healthz')[0]==200:break
 except Exception:pass
 time.sleep(.25)
else:raise RuntimeError('The app did not become healthy')
assert 'testdb' in request('/runtime-config.js')[1]
assert request('/api/test/state','does-not-exist')[0]==401
z=read();assert z['profile']['id']=='zeynep';assert read('rumeysa')['events']==[]
assert not any(a.get('managerOnly') for a in read('rumeysa')['apps'])
assert any(a.get('managerOnly') for a in read('elif')['apps'])
assert not any(a['id']=='test-es-white' for a in read('rumeysa')['apps'])
assert any(a['id']=='test-es-white' for a in read('lucia')['apps'])
change('Category created','Not allowed',person='rumeysa',expect=403)
source=next(a for a in z['apps'] if a['id']=='perakende-akademi');new=copy.deepcopy(source)
new.update(id='db-shared-check',name='Shared DB check',countries=['Türkiye','Spain'],employeeTypes=['White-collar'],managerOnly=False,links=[{'id':'web','label':'Open test','platform':'Web','country':'','url':'https://example.com/','pending':False}])
state=change('Application created',new['name'],new);assert any(a['id']==new['id'] for a in state['apps'])
assert any(a['id']==new['id'] for a in read('rumeysa')['apps']);assert any(a['id']==new['id'] for a in read('lucia')['apps']);assert not any(a['id']==new['id'] for a in read('emre')['apps'])
revision=state['_revision'];edited=copy.deepcopy(new);edited['name']='Shared DB edited'
change('Application edited',edited['name'],edited)
change('Application edited','stale',new,revision=revision,expect=409)
assert next(a for a in read()['apps'] if a['id']==new['id'])['name']=='Shared DB edited'
for _ in range(2):
 assert request('/api/test/events/search','rumeysa',{'term':'repeat'})[0]==204
 status,_,headers=request('/api/test/open/db-shared-check/links/web?persona=rumeysa');assert status==302 and headers['Location']=='https://example.com/'
assert request('/api/test/open/db-shared-check/links/web?persona=emre')[0]==404
change('Application archived',edited['name'],{'id':new['id']})
assert not any(a['id']==new['id'] for a in read('rumeysa')['apps']);assert request('/api/test/open/db-shared-check/links/web?persona=rumeysa')[0]==404
change('Application restored',edited['name'],{'id':new['id']})
change('Category created','DB category')
change('Category renamed','DB category',{'name':'DB renamed'})
change('Unused category removed','DB renamed')
change('Translation edited','en.heading',{'value':'Shared heading saved'})
assert read('lucia')['translations']['en']['heading']=='Shared heading saved'
admins=read()['admins'];change('Admin access granted','Lucía',patch={'admins':admins+['lucia']});assert 'lucia' in read('lucia')['admins']
change('Admin access revoked','Lucía',patch={'admins':admins})
change('Admin access revoked','Zeynep',patch={'admins':[]},expect=409)
state=read();ids=[e['id'] for e in state['events']];assert len(ids)==len(set(ids));assert sum(e['action']=='Search' for e in state['events'])==2
# Verify database trigger protection, not just absence of a delete API.
pg=project+'-postgres-1'
for sql in ["UPDATE appstore_test.action_logs SET action='forged'","DELETE FROM appstore_test.admin_audit_logs","TRUNCATE appstore_test.action_logs"]:
 p=subprocess.run(['docker','--host=unix:///var/run/docker.sock','exec',pg,'psql','-U','appstore_test','-d','arcelik_appstore_test','-v','ON_ERROR_STOP=1','-c',sql],text=True,capture_output=True)
 assert p.returncode!=0 and 'append-only' in p.stderr
# Restart with the same data volume. Schema/seed initialization must not reset edits or logs.
p=subprocess.run(['docker','--host=unix:///var/run/docker.sock','restart',project+'-app-1'],text=True,capture_output=True);assert p.returncode==0
for _ in range(60):
 try:
  again=read()
  break
 except Exception:time.sleep(.25)
else:raise RuntimeError('App did not restart')
assert next(a for a in again['apps'] if a['id']==new['id'])['name']=='Shared DB edited';assert again['translations']['en']['heading']=='Shared heading saved';assert set(ids)=={e['id'] for e in again['events']}
print('PASS: PostgreSQL shared saves/readbacks across employee clients; country/type/manager filtering; server admin checks; create/edit/archive/restore; category/translation/admin persistence; stale-write conflict; repeated immutable events; safe direct links; app restart preserves data and logs.')

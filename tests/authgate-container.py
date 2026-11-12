"""Integration tests. Requires the image built with the included Dockerfile.
Run: python tests/authgate-container.py [image-tag]
Uses synthetic headers and loopback trust only inside disposable test containers.
"""
import json,os,re,subprocess,sys,tempfile,time
from pathlib import Path
D=['docker','--host=unix:///var/run/docker.sock'];image=sys.argv[1] if len(sys.argv)>1 else 'arcelik-appstore:authgate-pilot'
containers=[]
def docker(args,check=True):
 p=subprocess.run(D+args,text=True,capture_output=True)
 if check and p.returncode:raise RuntimeError(p.stderr)
 return p

def start(label,env,fixture=None,logdir=None):
 name='arcelik-gate-test-'+label;containers.append(name)
 args=['run','-d','--name',name,'-e','ASPNETCORE_ENVIRONMENT=Development','-e','AppStore__Mode=AuthGate']
 for key,value in env.items():args+=['-e',key+'='+value]
 if fixture:args+=['--mount','type=bind,src='+str(fixture)+',dst=/app/catalogue-fixture.json,readonly','-e','AppStore__CataloguePath=/app/catalogue-fixture.json']
 if logdir:args+=['--mount','type=bind,src='+str(logdir)+',dst=/app/data']
 docker(args+[image])
 for _ in range(50):
  status,_=request(name,'/healthz')
  if status==200:return name
  time.sleep(.2)
 raise RuntimeError('Container did not become ready: '+docker(['logs',name]).stdout)

def request(name,path,headers=None,post=None):
 args=['exec',name,'wget','-Y','off','-S','-O','-']
 for key,value in (headers or {}).items():args+=['--header',key+': '+value]
 if post is not None:args+=['--header','Content-Type: application/json','--post-data',json.dumps(post)]
 # Busybox wget follows redirects, so resolve redirects separately with raw TCP below.
 p=docker(args+['http://127.0.0.1:8080'+path],False)
 codes=re.findall(r'HTTP/\S+\s+(\d+)',p.stderr)
 return (int(codes[-1]) if codes else 0),p.stdout

def raw(name,path,headers):
 wire='GET '+path+' HTTP/1.1\r\nHost: 127.0.0.1:8080\r\nConnection: close\r\n'+''.join(k+': '+v+'\r\n' for k,v in headers.items())+'\r\n'
 p=subprocess.Popen(D+['exec','-i',name,'nc','-w','3','127.0.0.1','8080'],stdin=subprocess.PIPE,stdout=subprocess.PIPE,stderr=subprocess.PIPE,text=True)
 p.stdin.write(wire);p.stdin.flush()
 result=p.stdout.read()
 try:p.stdin.close()
 except BrokenPipeError:pass
 p.wait(timeout=5)
 if not result:raise RuntimeError('Empty raw HTTP response: '+p.stderr.read())
 return result

def app(id,countries,types,**extra):
 return dict(id=id,name=id,shortDescription=id,description=id,tab='arcelik',countries=countries,employeeTypes=types,categories=['Business tools'],platforms=['Web'],version='1',isActive=True,isArchived=False,images=[],documents=[],links=[dict(id='global',platform='Web',label='Open',country='',url='https://example.com/')],**extra)

try:
 with tempfile.TemporaryDirectory(prefix='arcelik-gate-tests-') as tmp:
  root=Path(tmp);os.chmod(root,0o755);logs=root/'logs';logs.mkdir(mode=0o777);os.chmod(logs,0o777)
  apps=[app('global',[],['Office','Field']),app('tr-office',['Türkiye'],['Office']),app('ro-field',['Romania'],['Field']),app('empty-types',[],[]),app('managers-only',[],['Office','Field'],managerOnly=True)]
  inactive=app('inactive',[],['Office']);inactive['isActive']=False;apps.append(inactive)
  archived=app('archived',[],['Office']);archived['isArchived']=True;apps.append(archived)
  multi=app('multi',['Türkiye','Romania'],['Office','Field']);multi['links'] += [dict(id='ro',platform='Android',label='RO',country='Romania',url='https://example.com/ro'),dict(id='bad',platform='Web',label='Bad',country='',url='javascript:alert(1)'),dict(id='pending',label='Pending',country='',pending=True,url=''),dict(id='inactive',label='Inactive',country='',isActive=False,url='https://example.com/inactive')];apps.append(multi)
  fixture=root/'catalogue.json';fixture.write_text(json.dumps(apps));os.chmod(fixture,0o644)
  identity={'X-User-Sicil':'EMP-123','X-User-Id':'b0388f7c-71bc-442b-a5b9-cce185469e85','X-User-Email':'employee@example.com'}
  name=start('unconfigured',{},fixture)
  assert request(name,'/api/me',identity)[0]==503
  name=start('untrusted',{'AuthGate__TrustedProxyCidrs__0':'192.0.2.1/32'},fixture)
  assert request(name,'/api/me',identity)[0]==403
  trusted={'AuthGate__TrustedProxyCidrs__0':'127.0.0.1/32'}
  name=start('identity',trusted,fixture)
  assert request(name,'/api/me')[0]==401
  status,body=request(name,'/api/me',identity);me=json.loads(body);assert status==200 and me['employeeId']=='EMP-123' and me['userId']==identity['X-User-Id'] and not me['canBrowse']
  assert request(name,'/api/apps',identity)[0]==403
  assert request(name,'/api/me',{**identity,'X-User-Id':'EMP-123'})[0]==401
  assert '401' in raw(name,'/api/me',{**identity,'x-user-sicil':'OTHER'}).splitlines()[0]
  noemail=dict(identity);noemail.pop('X-User-Email');assert request(name,'/api/me',noemail)[0]==200
  assert request(name,'/api/me',{**identity,'X-User-Email':'not an email'})[0]==401
  settings={**trusted,'AuthGate__CountryHeader':'X-Test-Country','AuthGate__WorkerClassificationHeader':'X-Test-Worker','AuthGate__CountryMap__TR':'Türkiye','AuthGate__CountryMap__RO':'Romania','AuthGate__WorkerClassificationMap__white-collar':'Office','AuthGate__WorkerClassificationMap__blue-collar':'Field'}
  name=start('catalogue',settings,fixture,logs)
  tr={**identity,'X-Test-Country':'TR','X-Test-Worker':'white-collar'};ro={**identity,'X-Test-Country':'RO','X-Test-Worker':'blue-collar'}
  assert request(name,'/api/apps',identity)[0]==403
  assert request(name,'/api/apps',{**tr,'X-Test-Country':'ZZ'})[0]==403
  assert request(name,'/api/apps',{**tr,'X-Test-Worker':'executive'})[0]==403
  assert request(name,'/api/apps?country=RO&employeeType=Field',tr)[0]==200
  status,body=request(name,'/api/apps',tr);visible=json.loads(body);assert {a['id'] for a in visible}=={'global','tr-office','multi'}
  assert {a['id'] for a in json.loads(request(name,'/api/apps',ro)[1])}=={'global','ro-field','multi'}
  assert request(name,'/api/apps/ro-field',tr)[0]==404
  assert request(name,'/api/apps/ro-field/links/global/open',tr)[0]==404
  multi=next(a for a in visible if a['id']=='multi');assert all(l['url']=='' for l in multi['links']);assert not any(l['id'] in ('ro','inactive') for l in multi['links'])
  assert request(name,'/api/apps/multi/links/ro/open',tr)[0]==404
  assert request(name,'/api/apps/multi/links/bad/open',tr)[0]==400
  assert request(name,'/api/apps/multi/links/pending/open',tr)[0]==404
  assert request(name,'/api/apps/multi/links/inactive/open',tr)[0]==404
  for _ in range(2):
   response=raw(name,'/api/apps/global/links/global/open?url=https://evil.invalid',tr)
   assert '302' in response.splitlines()[0] and 'Location: https://example.com/' in response
   assert request(name,'/api/events/search',tr,{'term':'repeated search'})[0]==204
  records=[json.loads(l) for l in (logs/'actions.jsonl').read_text().splitlines()]
  assert len(records)==4 and len({r['id'] for r in records})==4 and all(r['employeeId']=='EMP-123' for r in records)
  assert request(name,'/api/events/search',{**tr,'Origin':'https://other.invalid'},{'term':'bad'})[0]==403
  # Spoofed browser fields never substitute for the verified header context.
  assert request(name,'/api/me?employeeId=FAKE&country=TR')[0]==401
  name=start('logging-failure',{**settings,'AppStore__ActionLogPath':'/proc/actions.jsonl'},fixture)
  assert '503' in raw(name,'/api/apps/global/links/global/open',tr).splitlines()[0]
  print('PASS: fail-closed logging, duplicate identity header rejection; trusted-peer checks; required employee/internal identity; optional email; missing/unrecognized targeting denied; global/country/type/multi targeting; direct IDs and links reauthorized; safe stored redirects; disabled links; distinct search/open events; cross-origin search rejected.')
finally:
 for name in containers:docker(['rm','-f',name],False)

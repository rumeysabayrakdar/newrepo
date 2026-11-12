"""Verify Test DB name and Production startup guards using synthetic Compose services."""
import subprocess,sys
image=sys.argv[1] if len(sys.argv)>1 else 'arcelik-appstore:postgres-test';project=sys.argv[2] if len(sys.argv)>2 else 'arcelik-package-db-test';d=['docker','--host=unix:///var/run/docker.sock']
base=['run','--rm','--network',project+'_default','--mount','type=volume,src='+project+'_test-secrets,dst=/run/appstore-secrets,readonly','-e','Database__Enabled=true','-e','Database__PasswordFile=/run/appstore-secrets/postgres-password','-e','ConnectionStrings__AppStore=Host=postgres;Database=arcelik_appstore_test;Username=appstore_test']
p=subprocess.run(d+base+['-e','TestDatabase__Name=wrong_database',image],text=True,capture_output=True);assert p.returncode!=0 and 'Test database could not be initialized' in p.stderr
p=subprocess.run(d+base+['-e','TestDatabase__Name=arcelik_appstore_test','-e','ASPNETCORE_ENVIRONMENT=Production','-e','DOTNET_ENVIRONMENT=Production',image],text=True,capture_output=True);assert p.returncode!=0 and 'Production requires' in p.stderr
print('PASS: wrong expected database name and Production startup are blocked before Test seed access.')

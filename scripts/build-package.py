"""Create the deployable source ZIP; never package a database, secrets or build caches."""
from pathlib import Path
import subprocess,hashlib,zipfile
root=Path(__file__).resolve().parents[1]
paths=subprocess.check_output(['git','ls-files','--cached','--others','--exclude-standard','-z'],cwd=root).decode().split('\0')
blocked={'node_modules','bin','obj','.git','.openai','.secrets','downloads','attachments'}
files=[]
for name in sorted(set(paths)):
 p=Path(name)
 if not name or blocked.intersection(p.parts) or p.suffix=='.zip' or name=='PACKAGE-CONTENTS.sha256' or p.name.startswith('.env') and p.name!='.env.example' or p.name=='secrets.json':continue
 if (root/p).is_file():files.append(p)
manifest=''.join(hashlib.sha256((root/p).read_bytes()).hexdigest()+'  '+p.as_posix()+'\n' for p in files)
(root/'PACKAGE-CONTENTS.sha256').write_text(manifest)
out=root/'downloads/Arcelik-AppStore-Admin-Update.zip'
with zipfile.ZipFile(out,'w',zipfile.ZIP_DEFLATED,compresslevel=9) as z:
 for p in files+[Path('PACKAGE-CONTENTS.sha256')]:z.write(root/p,p.as_posix())
with zipfile.ZipFile(out) as z:
 assert z.testzip() is None
 for p in ['Dockerfile','dist/index.html','dist/admin-review.html','src/AppStore.Web/Program.cs','src/AppStore.Web/Database/ExperienceReports.cs','vendor/nuget/Npgsql.10.0.3.nupkg']:assert p in z.namelist(),p
 for p in files:assert hashlib.sha256(z.read(p.as_posix())).hexdigest()==hashlib.sha256((root/p).read_bytes()).hexdigest()
print(f'Verified {len(files)+1} files; {out.stat().st_size:,} bytes; SHA256 {hashlib.sha256(out.read_bytes()).hexdigest()}')

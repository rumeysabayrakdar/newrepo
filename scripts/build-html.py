"""Build one self-contained HTML: local review on file://, real API when hosted on Norm."""
from pathlib import Path
import base64,json,mimetypes,re
root=Path(__file__).resolve().parents[1];dist=root/'dist'
assets={}
for p in (dist/'assets').rglob('*'):
 if p.is_file():assets[p.relative_to(dist).as_posix()]='data:'+ (mimetypes.guess_type(p.name)[0] or 'application/octet-stream')+';base64,'+base64.b64encode(p.read_bytes()).decode()
def inline(s):
 return re.sub(r'assets/[A-Za-z0-9_./-]+',lambda m:assets.get(m[0],m[0]),s)
def script(s):return '<script>'+s.replace('</script','<\\/script')+'</script>'
seed=json.loads((root/'src/AppStore.Web/experience-seed.json').read_text());seed['languages']=[{'code':'en','name':'English'},{'code':'tr','name':'Türkçe'}];seed['fallbackLanguage']='en';seed['audit']=[];seed['activity']=[]
css='\n'.join(inline((dist/n).read_text()) for n in ['fonts.css','experience.css','review-fixes.css'])
css+='\n.preview-bar{padding:12px 24px;background:#fff2de;color:#794500;display:flex;gap:20px;justify-content:space-between;align-items:center}.preview-bar span{display:block;font-size:12px}.preview-bar button{border:1px solid #c9945c;border-radius:12px;background:#fff;padding:10px}.boot-error{padding:30px;max-width:800px;margin:auto}'
html='<!doctype html><html lang="en"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>Arçelik App Store — HTML review</title><style>'+css+'</style></head><body><div id="root">Loading…</div>'
for name in ['react.js','react-dom.js','experience-template.js','experience-view.js']:html+=script(inline((dist/name).read_text()))
html+=script('window.ARCELIK_HTML_SEED='+inline(json.dumps(seed,ensure_ascii=False))+';')
preview=(root/'scripts/html-preview.js').read_text();app=(dist/'app.js').read_text()
bootstrap='''(async()=>{try{if(location.protocol==='file:'){(0,eval)(PREVIEW)}else{const r=await fetch('/runtime-config.js',{credentials:'same-origin'});if(!r.ok)throw Error('The .NET backend is not available. Download this HTML and open it locally to review the screens.');(0,eval)(await r.text());if(!window.ARCELIK_RUNTIME)throw Error('Runtime configuration is missing.');(0,eval)(APP)}}catch(e){document.getElementById('root').textContent=e.message}})();'''.replace('PREVIEW',json.dumps(preview)).replace('APP',json.dumps(app))
html+=script(bootstrap)+'</body></html>'
output=root/'downloads/Arcelik-AppStore-Admin.html';output.write_text(html);(dist/'admin-review.html').write_text(html);print(f'{output}: {output.stat().st_size:,} bytes')

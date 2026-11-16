const {useEffect,useRef,useState}=React;
const runtime=window.ARCELIK_RUNTIME;
// React owns loading, identity errors and the lifetime of the approved view renderer.
// The renderer owns only its isolated DOM subtree; all shared writes use the API.
function App(){
 const host=useRef(null),[error,setError]=useState(''),[attempt,retry]=useState(0);
 useEffect(()=>{let dispose,stopped=false;const controller=new AbortController();
 const test=runtime.environment==='Test';let testUser=test?(sessionStorage.getItem('arcelik-test-user')||runtime.testUsers?.find(p=>p.id==='zeynep')?.id||runtime.testUsers?.[0]?.id):'';
 let persona=test?sessionStorage.getItem('arcelik-test-persona')||'':'';
 const headers=()=>({...test?{'X-Test-User':testUser}: {},...persona?{'X-Test-Persona':persona}:{}});
 let writeBusy=false;
 const bridge={bootstrap:null,async request(path,body){if(body&&writeBusy&&path!=='/api/experience/event')throw Error('A save is in progress. Please wait.');const owns=!!body&&path!=='/api/experience/event';if(owns)writeBusy=true;try{const response=await fetch(path,{method:body?'POST':'GET',credentials:'same-origin',headers:{...headers(),...body?{'Content-Type':'application/json'}:{}},body:body?JSON.stringify(body):undefined,signal:controller.signal});const value=response.status===204?null:await response.json();if(!response.ok)throw Error((value?.message||'Connection failed')+(response.status===409?' Reload before saving again.':''));return value}finally{if(owns)writeBusy=false}},
 openPath(resource,kind='',link=''){const q=new URLSearchParams({resource,kind,link});if(test){q.set('testUser',testUser);if(persona)q.set('persona',persona)}return '/api/experience/open?'+q},
 switchProfile(id){if(!test)return;const synthetic=bridge.bootstrap.data.testProfiles?.some(p=>p.id===id);if(synthetic)sessionStorage.setItem('arcelik-test-persona',id);else{sessionStorage.setItem('arcelik-test-user',id);sessionStorage.removeItem('arcelik-test-persona')}location.reload()}};
 (async()=>{try{const response=await bridge.request('/api/experience');if(stopped)return;bridge.bootstrap=response;host.current.innerHTML=window.ARCELIK_TEMPLATE;dispose=window.mountArcelikExperience(host.current,bridge)}catch(e){if(!stopped){host.current.innerHTML='';setError(e.message)}}})();
 return ()=>{stopped=true;controller.abort();dispose?.();};
 },[attempt]);
 return <>{error&&<main className="boot-error"><h1>Arçelik App Store</h1><p role="alert">{error}</p><button onClick={()=>{setError('');retry(n=>n+1)}}>Retry / Yeniden dene</button>{runtime.environment==='Test'&&<button onClick={()=>{sessionStorage.removeItem('arcelik-test-persona');sessionStorage.setItem('arcelik-test-user','zeynep');location.reload()}}>Reset TEST account / TEST hesabını sıfırla</button>}</main>}<div ref={host} id="experience-host">{!error&&<p className="boot-loading">Arçelik App Store…</p>}</div></>;
}
ReactDOM.createRoot(document.getElementById('root')).render(<App/>);

const {
  useEffect,
  useRef,
  useState
} = React;
const runtime = window.ARCELIK_RUNTIME;
function App() {
  const host = useRef(null),
    [error, setError] = useState(''),
    [attempt, retry] = useState(0);
  useEffect(() => {
    let dispose,
      stopped = false;
    const controller = new AbortController();
    const test = runtime.environment === 'Test';
    let testUser = test ? sessionStorage.getItem('arcelik-test-user') || runtime.testUsers?.find(p => p.id === 'zeynep')?.id || runtime.testUsers?.[0]?.id : '';
    let persona = test ? sessionStorage.getItem('arcelik-test-persona') || '' : '';
    const headers = () => ({
      ...(test ? {
        'X-Test-User': testUser
      } : {}),
      ...(persona ? {
        'X-Test-Persona': persona
      } : {})
    });
    let writeBusy = false;
    const bridge = {
      bootstrap: null,
      async request(path, body) {
        if (body && writeBusy && path !== '/api/experience/event') throw Error('A save is in progress. Please wait.');
        const owns = !!body && path !== '/api/experience/event';
        if (owns) writeBusy = true;
        try {
          const response = await fetch(path, {
            method: body ? 'POST' : 'GET',
            credentials: 'same-origin',
            headers: {
              ...headers(),
              ...(body ? {
                'Content-Type': 'application/json'
              } : {})
            },
            body: body ? JSON.stringify(body) : undefined,
            signal: controller.signal
          });
          const value = response.status === 204 ? null : await response.json();
          if (!response.ok) throw Error((value?.message || 'Connection failed') + (response.status === 409 ? ' Reload before saving again.' : ''));
          return value;
        } finally {
          if (owns) writeBusy = false;
        }
      },
      openPath(resource, kind = '', link = '') {
        const q = new URLSearchParams({
          resource,
          kind,
          link
        });
        if (test) {
          q.set('testUser', testUser);
          if (persona) q.set('persona', persona);
        }
        return '/api/experience/open?' + q;
      },
      switchProfile(id) {
        if (!test) return;
        const synthetic = bridge.bootstrap.data.testProfiles?.some(p => p.id === id);
        if (synthetic) sessionStorage.setItem('arcelik-test-persona', id);else {
          sessionStorage.setItem('arcelik-test-user', id);
          sessionStorage.removeItem('arcelik-test-persona');
        }
        location.reload();
      }
    };
    (async () => {
      try {
        const response = await bridge.request('/api/experience');
        if (stopped) return;
        bridge.bootstrap = response;
        host.current.innerHTML = window.ARCELIK_TEMPLATE;
        dispose = window.mountArcelikExperience(host.current, bridge);
      } catch (e) {
        if (!stopped) {
          host.current.innerHTML = '';
          setError(e.message);
        }
      }
    })();
    return () => {
      stopped = true;
      controller.abort();
      dispose?.();
    };
  }, [attempt]);
  return React.createElement(React.Fragment, null, error && React.createElement("main", {
    className: "boot-error"
  }, React.createElement("h1", null, "Ar\xE7elik App Store"), React.createElement("p", {
    role: "alert"
  }, error), React.createElement("button", {
    onClick: () => {
      setError('');
      retry(n => n + 1);
    }
  }, "Retry / Yeniden dene"), runtime.environment === 'Test' && React.createElement("button", {
    onClick: () => {
      sessionStorage.removeItem('arcelik-test-persona');
      sessionStorage.setItem('arcelik-test-user', 'zeynep');
      location.reload();
    }
  }, "Reset TEST account / TEST hesab\u0131n\u0131 s\u0131f\u0131rla")), React.createElement("div", {
    ref: host,
    id: "experience-host"
  }, !error && React.createElement("p", {
    className: "boot-loading"
  }, "Ar\xE7elik App Store\u2026")));
}
ReactDOM.createRoot(document.getElementById('root')).render(React.createElement(App, null));

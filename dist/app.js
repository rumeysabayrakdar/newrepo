const {
  useState,
  useEffect,
  useRef
} = React;
const runtime = window.ARCELIK_RUNTIME;
const platforms = ['Web', 'iOS', 'Android', 'Windows'];
const uid = () => crypto.randomUUID();
const safeUrl = url => {
  try {
    const u = new URL(url);
    return u.protocol === 'https:' && !u.username && !u.password;
  } catch {
    return false;
  }
};
function Icon({
  app
}) {
  return React.createElement("div", {
    className: "icon",
    style: {
      background: app.bg || '#edf0f5',
      color: app.color || '#c92937'
    }
  }, app.symbol || app.name?.slice(0, 2) || 'A');
}
function Multi({
  title,
  values,
  onChange,
  options
}) {
  const [open, O] = useState(false),
    [draft, D] = useState(values),
    [q, Q] = useState('');
  return React.createElement("div", {
    className: "field"
  }, React.createElement("span", null, title), React.createElement("button", {
    type: "button",
    className: "multitrigger",
    onClick: () => {
      D([...values]);
      Q('');
      O(true);
    }
  }, values.join(', ') || 'Choose ' + title.toLowerCase(), " \u2304"), open && React.createElement("div", {
    className: "pick"
  }, React.createElement("section", {
    className: "picker",
    role: "dialog",
    "aria-modal": "true",
    "aria-label": title
  }, React.createElement("h3", null, title, " (", draft.length, ")"), React.createElement("input", {
    autoFocus: true,
    type: "search",
    placeholder: 'Search ' + title.toLowerCase(),
    value: q,
    onChange: e => Q(e.target.value)
  }), React.createElement("div", {
    className: "checks"
  }, options.filter(v => v.toLowerCase().includes(q.toLowerCase())).map(v => React.createElement("label", {
    key: v
  }, React.createElement("input", {
    type: "checkbox",
    checked: draft.includes(v),
    onChange: () => D(draft.includes(v) ? draft.filter(x => x !== v) : [...draft, v])
  }), v))), React.createElement("div", {
    className: "pickfoot"
  }, React.createElement("button", {
    className: "btn",
    type: "button",
    onClick: () => D([])
  }, "Clear"), React.createElement("button", {
    className: "btn",
    type: "button",
    onClick: () => O(false)
  }, "Cancel"), React.createElement("button", {
    className: "btn primary",
    type: "button",
    onClick: () => {
      onChange(draft);
      O(false);
    }
  }, "Apply")))));
}
function Detail({
  app: a,
  onClose,
  onOpen,
  preview = false
}) {
  return React.createElement("section", {
    className: "detail"
  }, React.createElement("div", {
    className: "detailhead"
  }, React.createElement(Icon, {
    app: a
  }), React.createElement("div", null, React.createElement("h2", null, a.name || 'Application name'), React.createElement("p", null, a.categories?.join(' · '))), onClose && React.createElement("button", {
    className: "close",
    "aria-label": "Close application details",
    onClick: onClose
  }, "\xD7")), React.createElement("p", null, a.shortDescription), React.createElement("h3", null, "Get the application"), a.links?.filter(l => l.isActive !== false).map(l => React.createElement("div", {
    className: "linkrow",
    key: l.id
  }, React.createElement("div", null, React.createElement("b", null, l.label), React.createElement("small", null, l.platform || 'Web', l.version ? ' · v' + l.version : '', l.pending ? ' · Download address pending' : '')), React.createElement("button", {
    className: "btn primary",
    disabled: preview || l.pending || !l.url,
    onClick: () => onOpen(l)
  }, l.pending ? 'Coming soon' : 'Open', " \u2197"))), React.createElement("h3", null, "About this application"), React.createElement("p", null, a.description), a.images?.length > 0 && React.createElement(React.Fragment, null, React.createElement("h3", null, "A closer look"), a.images.map((url, i) => React.createElement("img", {
    className: "screen",
    key: i,
    src: url,
    alt: `${a.name} screenshot ${i + 1}`,
    onError: e => e.currentTarget.style.display = 'none'
  }))), a.documents?.length > 0 && React.createElement(React.Fragment, null, React.createElement("h3", null, "Helpful resources"), a.documents.map(d => React.createElement("div", {
    className: "linkrow",
    key: d.id
  }, React.createElement("span", null, "\u25A4 ", d.title), React.createElement("button", {
    className: "btn",
    disabled: preview || d.pending || !d.url,
    onClick: () => onOpen(d)
  }, "View \u2197")))), React.createElement("div", {
    className: "facts"
  }, React.createElement("div", null, "Version", React.createElement("b", null, a.version)), React.createElement("div", null, "Platforms", React.createElement("b", null, a.platforms?.join(', ')))));
}
function Editor({
  app,
  categories,
  onCancel,
  onSave
}) {
  const [a, A] = useState(() => structuredClone(app)),
    [error, E] = useState(''),
    [busy, B] = useState(false);
  const set = (k, v) => A(p => ({
    ...p,
    [k]: v
  }));
  const item = (kind, i, k, v) => set(kind, a[kind].map((x, j) => i === j ? {
    ...x,
    [k]: v,
    ...(k === 'url' && kind === 'links' ? {
      pending: !v.trim()
    } : {})
  } : x));
  async function save(e) {
    e.preventDefault();
    E('');
    if (!a.name.trim() || !a.description.trim() || !a.shortDescription.trim() || !a.version.trim()) return E('Fill in the name, summary, description and version.');
    if (!a.categories.length || !a.platforms.length) return E('Choose categories and platforms.');
    if (!a.links.length || a.links.some(l => !l.label.trim() || !l.pending && !safeUrl(l.url) || l.url && !safeUrl(l.url))) return E('Use labelled HTTPS links, or mark empty addresses as pending.');
    if (a.documents.some(d => !d.title.trim() || !safeUrl(d.url) && d.url !== 'guide.html')) return E('Documents need a title and HTTPS address.');
    if (a.images.some(x => !safeUrl(x) && x !== 'sample-screen.svg')) return E('Screenshots need HTTPS addresses.');
    B(true);
    try {
      await onSave(a);
    } catch (e) {
      E(e.message);
    } finally {
      B(false);
    }
  }
  return React.createElement("div", {
    className: "modalback"
  }, React.createElement("form", {
    className: "modal",
    role: "dialog",
    "aria-modal": "true",
    "aria-label": "Application editor",
    onSubmit: save
  }, React.createElement("div", {
    className: "modaltop"
  }, React.createElement("h2", null, app._new ? 'Add application' : 'Edit application'), React.createElement("button", {
    type: "button",
    disabled: busy,
    className: "close",
    "aria-label": "Close editor",
    onClick: () => {
      if (confirm('Discard unsaved changes?')) onCancel();
    }
  }, "\xD7")), React.createElement("div", {
    className: "modalbody"
  }, React.createElement("div", null, error && React.createElement("div", {
    className: "error",
    role: "alert"
  }, error), [['name', 'Application name', 80], ['shortDescription', 'Short description', 500], ['version', 'Version', 64]].map(([k, label, max]) => React.createElement("label", {
    className: "field",
    key: k
  }, label, React.createElement("input", {
    required: true,
    maxLength: max,
    value: a[k],
    onChange: e => set(k, e.target.value)
  }))), React.createElement("label", {
    className: "field"
  }, "Description", React.createElement("textarea", {
    required: true,
    maxLength: "10000",
    value: a.description,
    onChange: e => set('description', e.target.value)
  })), React.createElement("label", {
    className: "field"
  }, "Catalogue tab", React.createElement("select", {
    value: a.tab,
    onChange: e => set('tab', e.target.value)
  }, React.createElement("option", {
    value: "arcelik"
  }, "Developed by Ar\xE7elik"), React.createElement("option", {
    value: "other"
  }, "Other applications"))), React.createElement("div", {
    className: "twocol"
  }, React.createElement("label", {
    className: "field"
  }, "Icon symbol", React.createElement("input", {
    maxLength: "3",
    value: a.symbol || '',
    onChange: e => set('symbol', e.target.value)
  })), React.createElement("label", {
    className: "field"
  }, "Icon background", React.createElement("input", {
    type: "color",
    value: a.bg || '#c92937',
    onChange: e => set('bg', e.target.value)
  }))), React.createElement(Multi, {
    title: "Categories",
    values: a.categories,
    options: categories,
    onChange: v => set('categories', v)
  }), React.createElement(Multi, {
    title: "Platforms",
    values: a.platforms,
    options: platforms,
    onChange: v => set('platforms', v)
  }), React.createElement("label", {
    className: "flex info"
  }, React.createElement("input", {
    type: "checkbox",
    checked: a.isActive,
    onChange: e => set('isActive', e.target.checked)
  }), " Active in catalogue"), React.createElement("h3", {
    className: "subheading"
  }, "Application links"), a.links.map((l, i) => React.createElement("div", {
    className: "assetrow",
    key: l.id
  }, React.createElement("label", {
    className: "field"
  }, "Label", React.createElement("input", {
    value: l.label,
    onChange: e => item('links', i, 'label', e.target.value)
  })), React.createElement("label", {
    className: "field"
  }, "HTTPS address", React.createElement("input", {
    value: l.url,
    placeholder: "https://\u2026",
    onChange: e => item('links', i, 'url', e.target.value)
  })), React.createElement("label", {
    className: "field"
  }, "Platform", React.createElement("select", {
    value: l.platform,
    onChange: e => item('links', i, 'platform', e.target.value)
  }, [...new Set([...platforms, l.platform])].filter(Boolean).map(p => React.createElement("option", {
    key: p
  }, p)))), React.createElement("label", {
    className: "flex info"
  }, React.createElement("input", {
    type: "checkbox",
    checked: !!l.pending,
    onChange: e => item('links', i, 'pending', e.target.checked)
  }), " Download address pending"), React.createElement("button", {
    type: "button",
    className: "btn danger",
    onClick: () => set('links', a.links.filter((_, j) => i !== j))
  }, "Remove link"))), React.createElement("button", {
    className: "btn",
    type: "button",
    onClick: () => set('links', [...a.links, {
      id: uid(),
      label: '',
      platform: 'Web',
      url: '',
      pending: true
    }])
  }, "+ Add link"), React.createElement("h3", {
    className: "subheading"
  }, "Documents"), a.documents.map((d, i) => React.createElement("div", {
    className: "assetrow",
    key: d.id
  }, React.createElement("label", {
    className: "field"
  }, "Title", React.createElement("input", {
    value: d.title,
    onChange: e => item('documents', i, 'title', e.target.value)
  })), React.createElement("label", {
    className: "field"
  }, "HTTPS address", React.createElement("input", {
    value: d.url,
    onChange: e => item('documents', i, 'url', e.target.value)
  })), React.createElement("button", {
    className: "btn danger",
    type: "button",
    onClick: () => set('documents', a.documents.filter((_, j) => i !== j))
  }, "Remove document"))), React.createElement("button", {
    className: "btn",
    type: "button",
    onClick: () => set('documents', [...a.documents, {
      id: uid(),
      title: '',
      url: ''
    }])
  }, "+ Add document"), React.createElement("h3", {
    className: "subheading"
  }, "Screenshots"), a.images.map((url, i) => React.createElement("div", {
    className: "flex",
    key: i
  }, React.createElement("input", {
    className: "textinput",
    "aria-label": 'Screenshot ' + (i + 1),
    value: url,
    onChange: e => set('images', a.images.map((v, j) => i === j ? e.target.value : v))
  }), React.createElement("button", {
    className: "btn danger",
    type: "button",
    onClick: () => set('images', a.images.filter((_, j) => i !== j))
  }, "Remove"))), React.createElement("button", {
    className: "btn",
    type: "button",
    onClick: () => set('images', [...a.images, ''])
  }, "+ Add screenshot")), React.createElement("div", null, React.createElement("div", {
    className: "previewlabel"
  }, "Private preview \xB7 unsaved"), React.createElement(Detail, {
    app: a,
    preview: true
  }))), React.createElement("div", {
    className: "modalfoot"
  }, React.createElement("span", null, "Save applies changes immediately."), React.createElement("div", {
    className: "flex"
  }, React.createElement("button", {
    className: "btn",
    type: "button",
    disabled: busy,
    onClick: () => {
      if (confirm('Discard unsaved changes?')) onCancel();
    }
  }, "Cancel"), React.createElement("button", {
    className: "btn primary",
    disabled: busy
  }, busy ? 'Saving…' : 'Save application')))));
}
function Application() {
  const [backupBusy, BackupBusy] = useState(false);
  const [testUser, TU] = useState(runtime?.testUsers?.[0]?.id || ''),
    [data, D] = useState(null),
    [error, E] = useState(''),
    [page, Page] = useState('catalogue'),
    [tab, Tab] = useState('arcelik'),
    [q, Q] = useState(''),
    [term, Term] = useState(''),
    [category, C] = useState('All apps'),
    [platform, P] = useState('All platforms'),
    [detail, DetailId] = useState(null),
    [editing, Edit] = useState(null),
    [adminTab, AT] = useState('Applications'),
    [adminSearch, AS] = useState(''),
    [toast, Toast] = useState(''),
    [newCategory, NC] = useState(''),
    [lang, Lang] = useState('en'),
    [translationKey, TK] = useState('heading'),
    [translationValue, TV] = useState('');
  const current = useRef(data),
    generation = useRef(0);
  current.current = data;
  const isTest = runtime?.environment === 'Test';
  async function api(path, body) {
    const r = await fetch(path, {
      method: body ? 'POST' : 'GET',
      credentials: 'same-origin',
      cache: 'no-store',
      headers: {
        ...(isTest ? {
          'X-Test-User': testUser
        } : {}),
        ...(body ? {
          'Content-Type': 'application/json'
        } : {})
      },
      ...(body ? {
        body: JSON.stringify(body)
      } : {})
    });
    let value = null;
    try {
      value = await r.json();
    } catch {}
    if (!r.ok) {
      const e = new Error(value?.message || 'The request could not be completed. Please try again.');
      e.status = r.status;
      throw e;
    }
    return value;
  }
  async function load() {
    const g = ++generation.current;
    try {
      const next = await api('/api/state');
      if (g !== generation.current) return;
      if (current.current && next.profile.id !== current.current.profile.id) {
        Edit(null);
        DetailId(null);
        Page('catalogue');
      }
      D(next);
      E('');
    } catch (e) {
      if (g === generation.current) {
        if (!current.current || e.status === 401 || e.status === 403 || isTest && current.current.profile.id !== testUser) D(null);
        E(e.message);
      }
    }
  }
  useEffect(() => {
    D(null);
    load();
    const focus = () => load();
    window.addEventListener('focus', focus);
    const timer = setInterval(load, 15000);
    return () => {
      generation.current++;
      clearInterval(timer);
      window.removeEventListener('focus', focus);
    };
  }, [testUser]);
  useEffect(() => {
    if (toast) {
      const id = setTimeout(() => Toast(''), 5000);
      return () => clearTimeout(id);
    }
  }, [toast]);
  useEffect(() => {
    if (data && !data.profile.isAdmin) {
      Page('catalogue');
      Edit(null);
    }
  }, [data?.profile.isAdmin]);
  async function change(action, entity, after = null, patch = {}, revision = current.current._revision) {
    try {
      const next = await api('/api/admin/commands', {
        action,
        entity,
        after,
        patch,
        revision
      });
      D(next);
      E('');
      Toast('Saved.');
      return next;
    } catch (e) {
      if (e.status === 409 || e.status === 403) await load();
      throw e;
    }
  }
  async function run(...args) {
    try {
      return await change(...args);
    } catch (e) {
      Toast(e.message);
    }
  }
  async function backup() {
    BackupBusy(true);
    try {
      const snapshot = await api('/api/admin/backup', {});
      if (snapshot.format !== 'arcelik-appstore-backup' || snapshot.version !== 1) throw Error('The backup response was invalid.');
      const url = URL.createObjectURL(new Blob([JSON.stringify(snapshot)], {
        type: 'application/json'
      }));
      const a = document.createElement('a');
      a.href = url;
      a.download = 'appstore-' + runtime.environment.toLowerCase() + '-backup-' + new Date().toISOString().replace(/[:.]/g, '-') + '.json';
      document.body.appendChild(a);
      a.click();
      a.remove();
      setTimeout(() => URL.revokeObjectURL(url), 1000);
      Toast('Backup download started. Keep the file in a private place.');
    } catch (e) {
      Toast(e.message);
    } finally {
      BackupBusy(false);
    }
  }
  async function search(e) {
    e.preventDefault();
    try {
      await api('/api/events/search', {
        term: q
      });
      Term(q);
      DetailId(null);
    } catch (e) {
      Toast(e.message);
    }
  }
  function open(l) {
    if (l.pending || !l.openPath) return;
    const url = new URL(l.openPath, location.origin);
    if (url.origin !== location.origin || !url.pathname.startsWith('/api/open/')) return Toast('This link is unavailable.');
    if (isTest) url.searchParams.set('testUser', testUser);
    window.open(url.href, '_blank', 'noopener,noreferrer');
  }
  function begin(a) {
    Edit({
      ...structuredClone(a),
      _baseRevision: data._revision
    });
  }
  async function save(a) {
    const {
      _new,
      _baseRevision,
      ...app
    } = a;
    await change(_new ? 'Application created' : 'Application edited', app.name, app, {}, _baseRevision);
    Edit(null);
  }
  function add() {
    begin({
      _new: true,
      id: uid(),
      name: '',
      shortDescription: '',
      description: '',
      version: '1.0.0',
      tab: 'arcelik',
      symbol: 'A',
      color: '#ffffff',
      bg: '#c92937',
      categories: data.categories.slice(0, 1),
      platforms: ['Web'],
      isActive: true,
      isArchived: false,
      links: [{
        id: uid(),
        label: 'Open application',
        platform: 'Web',
        url: '',
        pending: true
      }],
      documents: [],
      images: []
    });
  }
  if (!runtime || !['Test', 'Production'].includes(runtime.environment)) return React.createElement("main", {
    className: "main"
  }, React.createElement("div", {
    className: "panel",
    role: "alert"
  }, "Application configuration is unavailable. Open the deployed .NET application."));
  if (!data || isTest && data.profile.id !== testUser) return React.createElement("main", {
    className: "main"
  }, React.createElement("section", {
    className: "panel about",
    role: error ? 'alert' : 'status'
  }, React.createElement("div", {
    className: "brand"
  }, React.createElement("span", {
    className: "brandmark"
  }), "ar\xE7elik"), React.createElement("h2", {
    style: {
      marginTop: 25
    }
  }, error ? 'We couldn’t load the catalogue' : 'Loading your catalogue…'), React.createElement("p", {
    className: "info"
  }, error || 'Connecting to the application.'), error && React.createElement("button", {
    className: "btn primary",
    onClick: load
  }, "Try again")));
  const admin = data.profile.isAdmin,
    t = {
      ...data.translations.en,
      ...data.translations[lang]
    },
    active = data.apps.filter(a => a.isActive && !a.isArchived),
    apps = active.filter(a => a.tab === tab && (category === 'All apps' || a.categories.includes(category)) && (platform === 'All platforms' || a.platforms.includes(platform)) && (!term || (a.name + ' ' + a.shortDescription + ' ' + a.description).toLowerCase().includes(term.toLowerCase()))),
    selected = active.find(a => a.id === detail),
    cats = data.categories.filter(c => active.some(a => a.tab === tab && a.categories.includes(c)));
  return React.createElement(React.Fragment, null, React.createElement("header", null, React.createElement("div", {
    className: "brand"
  }, React.createElement("span", {
    className: "brandmark"
  }), "ar\xE7elik", React.createElement("small", null, "App Store")), React.createElement("nav", {
    className: "nav"
  }, React.createElement("button", {
    className: page === 'catalogue' ? 'active' : '',
    onClick: () => Page('catalogue')
  }, "Discover apps"), admin && React.createElement("button", {
    className: page === 'admin' ? 'active' : '',
    onClick: () => Page('admin')
  }, "Administration")), React.createElement("div", {
    className: "headright"
  }, React.createElement("select", {
    "aria-label": "Language",
    className: "lang",
    value: lang,
    onChange: e => Lang(e.target.value)
  }, React.createElement("option", {
    value: "en"
  }, "EN"), React.createElement("option", {
    value: "tr"
  }, "TR")), React.createElement("div", {
    className: "avatar"
  }, data.profile.name.slice(0, 2).toUpperCase()), React.createElement("span", {
    className: "signedinname"
  }, data.profile.name))), isTest && React.createElement("div", {
    className: "demo"
  }, React.createElement("b", null, "TEST ENVIRONMENT"), React.createElement("span", null, "Shared catalogue \xB7 Employee/Admin test accounts")), React.createElement("main", {
    className: "main"
  }, error && React.createElement("div", {
    className: "error",
    role: "alert"
  }, error, " Your unsaved draft is kept open. ", React.createElement("button", {
    className: "btn",
    onClick: load
  }, "Reconnect")), isTest && React.createElement("section", {
    className: "test-profile-bar"
  }, React.createElement("div", null, React.createElement("b", null, "Test account"), React.createElement("p", null, "Use Employee to browse or Admin to manage the catalogue.")), React.createElement("select", {
    "aria-label": "Test account",
    value: testUser,
    onChange: e => {
      TU(e.target.value);
      Edit(null);
      Page('catalogue');
    }
  }, runtime.testUsers.map(u => React.createElement("option", {
    key: u.id,
    value: u.id
  }, u.name, " \xB7 ", data.admins.includes(u.id) ? 'Admin' : 'Employee')))), admin && React.createElement("div", {
    className: "mobile-admin-menu"
  }, React.createElement("button", {
    className: "btn",
    onClick: () => Page('catalogue')
  }, "Catalogue"), React.createElement("button", {
    className: "btn",
    onClick: () => Page('admin')
  }, "Manage catalogue")), page === 'catalogue' && React.createElement(React.Fragment, null, React.createElement("section", {
    className: "hero"
  }, React.createElement("div", null, React.createElement("div", {
    className: "eyebrow"
  }, "Your digital workplace"), React.createElement("h1", null, "Your everyday work.", React.createElement("br", null), React.createElement("span", null, "A little more connected.")), React.createElement("p", null, t.subtitle)), React.createElement("div", {
    className: "heroart",
    "aria-hidden": "true"
  }, React.createElement("div", {
    className: "orbit"
  }), React.createElement("div", {
    className: "tile t1"
  }, "\u25A6"), React.createElement("div", {
    className: "tile t2"
  }, "\u2726"), React.createElement("div", {
    className: "tile big"
  }, "a"), React.createElement("div", {
    className: "tile t3"
  }, "\u25F4"), React.createElement("div", {
    className: "tile t4"
  }, "\u25B1"))), React.createElement("div", {
    className: "sectiontop"
  }, React.createElement("div", null, React.createElement("h2", null, t.catalogue), React.createElement("p", null, t.heading)), React.createElement("form", {
    className: "search",
    onSubmit: search
  }, React.createElement("span", null, "\u2315"), React.createElement("input", {
    "aria-label": "Search applications",
    maxLength: "200",
    placeholder: t.search,
    value: q,
    onChange: e => Q(e.target.value)
  }), React.createElement("button", {
    "aria-label": "Submit search"
  }, "\u21B5"))), React.createElement("div", {
    className: "catalogtabs"
  }, [['arcelik', t.arcelik], ['other', t.other]].map(([key, label]) => React.createElement("button", {
    key: key,
    className: tab === key ? 'active' : '',
    onClick: () => {
      Tab(key);
      C('All apps');
      DetailId(null);
    }
  }, label, React.createElement("span", {
    className: "count"
  }, active.filter(a => a.tab === key).length)))), React.createElement("div", {
    className: "filters"
  }, ['All apps', ...cats].map(c => React.createElement("button", {
    className: 'chip ' + (category === c ? 'active' : ''),
    key: c,
    onClick: () => {
      C(c);
      DetailId(null);
    }
  }, c === 'All apps' ? t.all : c)), React.createElement("select", {
    "aria-label": "Platform filter",
    className: "textinput",
    value: platform,
    onChange: e => {
      P(e.target.value);
      DetailId(null);
    }
  }, ['All platforms', ...platforms].map(p => React.createElement("option", {
    key: p
  }, p))), React.createElement("span", {
    className: "right"
  }, apps.length, " applications")), term && React.createElement("p", {
    className: "info"
  }, "Results for \u201C", term, "\u201D ", React.createElement("button", {
    className: "btn",
    onClick: () => {
      Term('');
      Q('');
    }
  }, "Clear search")), React.createElement("div", {
    className: selected ? 'split' : ''
  }, React.createElement("div", {
    className: "grid"
  }, apps.map(a => React.createElement("button", {
    className: 'card ' + (detail === a.id ? 'selected' : ''),
    key: a.id,
    onClick: () => DetailId(a.id)
  }, React.createElement("div", {
    className: "cardtop"
  }, React.createElement(Icon, {
    app: a
  }), React.createElement("span", {
    className: "badge"
  }, a.platforms.join(' · '))), React.createElement("h3", null, a.name), React.createElement("p", null, a.shortDescription), React.createElement("div", {
    className: "cardfoot"
  }, React.createElement("span", null, a.categories.join(' · ')), React.createElement("span", null, "\xB7 v", a.version), React.createElement("span", {
    className: "arrow"
  }, "\u2197")))), !apps.length && React.createElement("div", {
    className: "empty"
  }, React.createElement("h3", null, "No applications found"), React.createElement("p", null, "Try another search, category or platform."), React.createElement("button", {
    className: "btn",
    onClick: () => {
      Q('');
      Term('');
      C('All apps');
      P('All platforms');
    }
  }, "Clear filters"))), selected && React.createElement(Detail, {
    app: selected,
    onClose: () => DetailId(null),
    onOpen: open
  }))), page === 'admin' && admin && React.createElement(React.Fragment, null, React.createElement("div", {
    className: "sectiontop"
  }, React.createElement("div", null, React.createElement("div", {
    className: "eyebrow"
  }, "Administration"), React.createElement("h2", null, "Manage your application catalogue"), React.createElement("p", null, "Changes are shared immediately after saving.")), React.createElement("button", {
    className: "btn primary",
    onClick: add
  }, "+ Add application")), React.createElement("div", {
    className: "adminnav"
  }, ['Applications', 'Categories', 'Translations', 'Admin access', 'Activity', 'Backup'].map(v => React.createElement("button", {
    key: v,
    className: 'chip ' + (adminTab === v ? 'active' : ''),
    onClick: () => {
      AT(v);
      if (v === 'Activity') load();
    }
  }, v))), adminTab === 'Backup' && React.createElement("section", {
    className: "panel"
  }, React.createElement("h3", null, "Download an application backup"), React.createElement("p", null, "Save a copy of the catalogue, settings, users and complete activity history."), React.createElement("p", {
    className: "info"
  }, "The file contains user information. Keep it private. It includes saved data and asset links; linked files, passwords and hosting settings are not included. Restoring requires an administrator; this does not replace the platform\u2019s full database backup."), React.createElement("button", {
    className: "btn primary",
    disabled: backupBusy,
    onClick: backup
  }, backupBusy ? 'Preparing backup…' : 'Download backup')), adminTab === 'Applications' && React.createElement(React.Fragment, null, React.createElement("input", {
    className: "textinput",
    "aria-label": "Find an application",
    placeholder: "Find an application\u2026",
    value: adminSearch,
    onChange: e => AS(e.target.value)
  }), React.createElement("div", {
    className: "tablewrap",
    style: {
      marginTop: 15
    }
  }, React.createElement("table", null, React.createElement("thead", null, React.createElement("tr", null, React.createElement("th", null, "Application"), React.createElement("th", null, "Platforms"), React.createElement("th", null, "Status"), React.createElement("th", null, "Actions"))), React.createElement("tbody", null, data.apps.filter(a => a.name.toLowerCase().includes(adminSearch.toLowerCase())).map(a => React.createElement("tr", {
    key: a.id
  }, React.createElement("td", null, React.createElement("div", {
    className: "appcell"
  }, React.createElement(Icon, {
    app: a
  }), React.createElement("b", null, a.name))), React.createElement("td", null, a.platforms.join(', ')), React.createElement("td", null, React.createElement("span", {
    className: 'status ' + (!a.isActive || a.isArchived ? 'off' : '')
  }, a.isArchived ? 'Archived' : a.isActive ? 'Active' : 'Inactive')), React.createElement("td", null, React.createElement("div", {
    className: "rowactions"
  }, React.createElement("button", {
    className: "btn",
    onClick: () => begin(a)
  }, "Edit"), React.createElement("button", {
    className: "btn",
    onClick: () => run(a.isActive ? 'Application deactivated' : 'Application activated', a.name, {
      id: a.id
    })
  }, a.isActive ? 'Deactivate' : 'Activate'), React.createElement("button", {
    className: "btn danger",
    onClick: () => run(a.isArchived ? 'Application restored' : 'Application archived', a.name, {
      id: a.id
    })
  }, a.isArchived ? 'Restore' : 'Archive'))))))))), adminTab === 'Categories' && React.createElement("section", {
    className: "panel"
  }, React.createElement("h3", null, "Categories"), React.createElement("form", {
    className: "flex",
    style: {
      marginTop: 18
    },
    onSubmit: async e => {
      e.preventDefault();
      if (await run('Category created', newCategory.trim())) NC('');
    }
  }, React.createElement("input", {
    className: "textinput",
    required: true,
    maxLength: "50",
    "aria-label": "New category",
    value: newCategory,
    onChange: e => NC(e.target.value),
    placeholder: "Category name"
  }), React.createElement("button", {
    className: "btn primary"
  }, "Add category")), data.categories.map(c => React.createElement("div", {
    className: "listitem",
    key: c
  }, React.createElement("b", null, c), React.createElement("div", {
    className: "flex"
  }, React.createElement("button", {
    className: "btn",
    onClick: () => {
      const value = prompt('Rename category', c)?.trim();
      if (value && value !== c) run('Category renamed', c, {
        name: value
      });
    }
  }, "Rename"), React.createElement("button", {
    className: "btn danger",
    disabled: data.apps.some(a => a.categories.includes(c)),
    onClick: () => run('Unused category removed', c)
  }, "Remove"))))), adminTab === 'Translations' && React.createElement("section", {
    className: "panel"
  }, React.createElement("h3", null, "Catalogue text"), React.createElement("p", {
    className: "info"
  }, "Editing ", lang === 'en' ? 'English' : 'Turkish', ". Use the header to switch language."), React.createElement("label", {
    className: "field"
  }, "Text key", React.createElement("select", {
    value: translationKey,
    onChange: e => TK(e.target.value)
  }, Object.keys(data.translations.en).map(k => React.createElement("option", {
    key: k
  }, k)))), React.createElement("p", {
    className: "info"
  }, "Current: ", t[translationKey]), React.createElement("form", {
    onSubmit: async e => {
      e.preventDefault();
      if (await run('Translation edited', lang + '.' + translationKey, {
        value: translationValue.trim()
      })) TV('');
    }
  }, React.createElement("label", {
    className: "field"
  }, "New text", React.createElement("textarea", {
    required: true,
    maxLength: "2000",
    value: translationValue,
    onChange: e => TV(e.target.value)
  })), React.createElement("button", {
    className: "btn primary"
  }, "Save translation"))), adminTab === 'Admin access' && React.createElement("section", {
    className: "panel"
  }, React.createElement("h3", null, "Admin access"), React.createElement("p", {
    className: "info"
  }, "Grant access to an existing signed-in user. At least one Admin must remain."), data.users.map(u => React.createElement("div", {
    className: "listitem",
    key: u.id
  }, React.createElement("div", null, React.createElement("b", null, u.name), React.createElement("p", null, u.id)), React.createElement("button", {
    className: "btn",
    disabled: data.admins.includes(u.id) && data.admins.length === 1,
    onClick: () => {
      const has = data.admins.includes(u.id);
      run(has ? 'Admin access revoked' : 'Admin access granted', u.name, null, {
        admins: has ? data.admins.filter(x => x !== u.id) : [...data.admins, u.id]
      });
    }
  }, data.admins.includes(u.id) ? 'Revoke admin' : 'Grant admin')))), adminTab === 'Activity' && React.createElement(React.Fragment, null, React.createElement("p", {
    className: "info"
  }, "Latest 200 activity records. An app-open click does not confirm installation."), React.createElement("div", {
    className: "tablewrap"
  }, React.createElement("table", null, React.createElement("thead", null, React.createElement("tr", null, React.createElement("th", null, "Time"), React.createElement("th", null, "User"), React.createElement("th", null, "Action"), React.createElement("th", null, "Item"), React.createElement("th", null, "Details"))), React.createElement("tbody", null, [...data.events].reverse().map(e => React.createElement("tr", {
    key: e.id
  }, React.createElement("td", null, new Date(e.time).toLocaleString()), React.createElement("td", null, e.actor), React.createElement("td", null, e.action), React.createElement("td", null, e.entity), React.createElement("td", null, React.createElement("details", null, React.createElement("summary", null, e.id.slice(0, 8)), React.createElement("pre", {
    style: {
      maxWidth: 350,
      maxHeight: 300,
      overflow: 'auto',
      whiteSpace: 'pre-wrap',
      fontSize: 10
    }
  }, JSON.stringify(e, null, 2)))))), !data.events.length && React.createElement("tr", null, React.createElement("td", {
    colSpan: "5"
  }, "No activity yet.")))))))), React.createElement("footer", null, React.createElement("span", null, React.createElement("b", null, "ar\xE7elik"), " \xB7 App Store"), React.createElement("span", null, error ? 'Connection interrupted' : 'Connected', " \xB7 ", runtime.environment), React.createElement("span", null, "Application catalogue")), editing && admin && React.createElement(Editor, {
    app: editing,
    categories: data.categories,
    onCancel: () => Edit(null),
    onSave: save
  }), React.createElement("div", {
    className: "toast",
    role: "status",
    style: {
      display: toast ? 'block' : 'none'
    }
  }, toast));
}
ReactDOM.createRoot(document.getElementById('root')).render(React.createElement(Application, null));

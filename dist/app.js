const {
  useState,
  useEffect
} = React;
const uid = () => crypto.randomUUID();
const baseCats = ['Productivity', 'Communication', 'Human resources', 'Business tools', 'Lifestyle', 'Test scenarios'];
const countries = ['Türkiye', 'Spain', 'United Kingdom', 'Romania', 'Germany', 'South Africa', 'Pakistan'];
const types = ['White-collar', 'Blue-collar'];
const platforms = ['Web', 'iOS', 'Android', 'Windows'];
const personas = [{
  id: 'rumeysa',
  name: 'Rümeysa',
  country: 'Türkiye',
  type: 'White-collar',
  isManager: false,
  initials: 'RB'
}, {
  id: 'emre',
  name: 'Emre',
  country: 'Türkiye',
  type: 'Blue-collar',
  isManager: false,
  initials: 'EY'
}, {
  id: 'lucia',
  name: 'Lucía',
  country: 'Spain',
  type: 'White-collar',
  isManager: false,
  initials: 'LG'
}, {
  id: 'diego',
  name: 'Diego',
  country: 'Spain',
  type: 'Blue-collar',
  isManager: false,
  initials: 'DM'
}, {
  id: 'elif',
  name: 'Elif',
  country: 'Türkiye',
  type: 'White-collar',
  isManager: true,
  initials: 'EK'
}, {
  id: 'carlos',
  name: 'Carlos',
  country: 'Spain',
  type: 'White-collar',
  isManager: true,
  initials: 'CR'
}, {
  id: 'zeynep',
  name: 'Zeynep',
  country: 'Türkiye',
  type: 'White-collar',
  isManager: false,
  initials: 'ZA'
}, {
  id: 'alex',
  name: 'Alex',
  country: 'United Kingdom',
  type: 'Blue-collar',
  isManager: false,
  initials: 'AT'
}];
const profileLabel = p => `${p.name} · ${p.country} · ${p.type}${p.isManager ? ' · Manager' : ''}`;
const mk = (id, name, description, category, symbol, color, bg, tab = 'arcelik', extra = {}) => ({
  id,
  name,
  shortDescription: description,
  description: description + ' Access your everyday tools and stay connected wherever you work. This sample listing is provided for testing the new Arçelik App Store.',
  categories: [category],
  symbol,
  color,
  bg,
  tab,
  version: '1.0.0',
  countries: [],
  employeeTypes: ['White-collar', 'Blue-collar'],
  platforms: ['Web'],
  links: [{
    id: uid(),
    label: 'Visit website',
    platform: 'Web',
    country: '',
    url: 'https://www.arcelikglobal.com/'
  }],
  documents: [{
    id: uid(),
    title: 'Sample getting started guide',
    url: 'guide.html'
  }],
  images: ['sample-screen.svg'],
  isActive: true,
  isArchived: false,
  ...extra
});
const importedApps = [{
  id: 'perakende-akademi',
  name: 'Perakende Akademi',
  version: '2.2.2',
  platforms: ['iOS'],
  symbol: 'PA',
  color: '#f6f8ed',
  bg: '#686e70',
  tab: 'arcelik',
  categories: ['Productivity'],
  description: 'Perakende Akademi mobil uygulaması sadece Arçelik Pazarlama A.Ş. bayilerinin ve saha ekiplerinin kullanabildiği kapalı devre bir uygulamadır.'
}, {
  id: 'servis-akademi',
  name: 'Servis Akademi',
  version: '1.0.2',
  platforms: ['iOS'],
  symbol: 'SA',
  color: '#ed950d',
  bg: '#f5f6f7',
  tab: 'arcelik',
  categories: ['Productivity'],
  description: 'Servis Akademi mobil uygulaması sadece Arçelik Pazarlama A.Ş. Yetkili Servislerinin ve çalışanlarının kullanabildiği kapalı devre bir uygulamadır.'
}, {
  id: 'perakende-akademi-ar',
  name: 'Perakende Akademi (AR)',
  version: '1.0.0',
  platforms: ['iOS'],
  symbol: 'AR',
  color: '#ffffff',
  bg: '#ff9d13',
  tab: 'arcelik',
  categories: ['Productivity'],
  description: 'Perakende Akademisi Augmented Reality Uygulaması (Test).'
}, {
  id: 'okta-verify-china',
  name: 'Okta Verify (for China)',
  version: '6.1.1',
  platforms: ['Android'],
  symbol: '✓',
  color: '#ffffff',
  bg: '#139acc',
  tab: 'other',
  categories: ['Business tools'],
  description: 'Okta Verify App for Arçelik Global users in China.'
}, {
  id: 'bizpuan',
  name: 'BizPuan',
  version: '2.7',
  platforms: ['iOS', 'Android'],
  symbol: '●',
  color: '#078bc3',
  bg: '#ffffff',
  tab: 'arcelik',
  categories: ['Business tools'],
  description: 'Bayi ve Servis çalışanlarının çeşitli kurgular ile puan kazanmalarını ve bu puanları harcamalarını sağlayan sadakat uygulamasıdır.'
}, {
  id: 'sirius-tech',
  name: 'SiriusTech',
  version: '1.0.2',
  platforms: ['Android'],
  symbol: '✦',
  color: '#bfe8ff',
  bg: '#082d57',
  tab: 'arcelik',
  categories: ['Business tools'],
  description: 'Sirius Tech Application.'
}].map(a => ({
  ...a,
  shortDescription: a.description,
  countries: [],
  employeeTypes: ['White-collar', 'Blue-collar'],
  links: a.platforms.map(p => ({
    id: a.id + '-' + p.toLowerCase(),
    label: a.name + ' for ' + p,
    platform: p,
    country: '',
    version: a.version,
    url: '',
    pending: true
  })),
  documents: [],
  images: [],
  isActive: true,
  isArchived: false,
  source: 'legacy-catalogue-screenshot'
}));
const scenarioApps = [['test-tr-white', 'Türkiye Office Tools', ['Türkiye'], ['White-collar'], false], ['test-tr-blue', 'Türkiye Team Tools', ['Türkiye'], ['Blue-collar'], false], ['test-es-white', 'Spain Office Tools', ['Spain'], ['White-collar'], false], ['test-es-blue', 'Spain Team Tools', ['Spain'], ['Blue-collar'], false], ['test-managers', 'Manager Workspace', [], ['White-collar', 'Blue-collar'], true], ['test-tr-es', 'Türkiye & Spain Hub', ['Türkiye', 'Spain'], ['White-collar', 'Blue-collar'], false]].map(([id, name, countries, employeeTypes, managerOnly]) => mk(id, name, 'Synthetic test application to verify country, employee type and manager targeting.', 'Test scenarios', managerOnly ? 'M' : 'T', '#526996', '#eaf0fc', 'arcelik', {
  countries,
  employeeTypes,
  managerOnly,
  isTestScenario: true,
  images: [],
  documents: [],
  links: [{
    id: id + '-web',
    label: 'Test application · destination not configured',
    platform: 'Web',
    country: '',
    url: '',
    pending: true
  }]
}));
const seeds = [...importedApps, ...scenarioApps, mk('connect', 'Arçelik Connect', 'Your people, news and workplace. All in one place.', 'Communication', 'a', '#fff', '#c92d3a'), mk('hr', 'HR Portal', 'Everything you need for your employee journey.', 'Human resources', 'HR', '#ad6b32', '#fbf0e1'), mk('one', 'One Arçelik', 'Stay connected to our global Arçelik community.', 'Communication', '◎', '#417c73', '#e6f3ef'), mk('expense', 'Expense Manager', 'Less paperwork. Simpler expense management.', 'Business tools', '↗', '#4b6aa4', '#eaf0fc'), mk('learn', 'Learning Hub', 'Make room for growth. Learn something new.', 'Productivity', '▱', '#8563ad', '#f1ebfa'), mk('service', 'Service Partner', 'The right tools for our teams in the field.', 'Business tools', '⚒', '#457b89', '#e7f2f5', 'arcelik', {
  employeeTypes: ['Blue-collar']
}), mk('well', 'Wellbeing', 'Small steps towards a healthier workday.', 'Lifestyle', '♡', '#a06372', '#fbeef1'), mk('teams', 'Microsoft Teams', 'Meet, chat and collaborate with your team.', 'Communication', 'T', '#fff', '#6554a4', 'other', {
  links: [{
    id: uid(),
    label: 'Open Teams',
    platform: 'Web',
    country: '',
    url: 'https://teams.microsoft.com/'
  }, {
    id: uid(),
    label: 'Teams for mobile',
    platform: 'iOS / Android',
    country: '',
    url: 'https://www.microsoft.com/en-us/microsoft-teams/download-app'
  }],
  platforms: ['Web', 'iOS', 'Android']
}), mk('outlook', 'Microsoft Outlook', 'A little more clarity for your busy workday.', 'Productivity', 'O', '#fff', '#237ac1', 'other', {
  links: [{
    id: uid(),
    label: 'Open Outlook',
    platform: 'Web',
    country: '',
    url: 'https://outlook.office.com/'
  }]
}), mk('miro', 'Miro', 'Bring your ideas together on a shared canvas.', 'Productivity', 'M', '#343439', '#f8d55f', 'other', {
  countries: ['Türkiye'],
  links: [{
    id: uid(),
    label: 'Open Miro',
    platform: 'Web',
    country: '',
    url: 'https://miro.com/'
  }]
})];
const initial = () => ({
  apps: seeds,
  categories: baseCats,
  admins: ['zeynep'],
  events: [],
  translations: {
    en: {
      heading: 'Discover your everyday essentials.',
      subtitle: 'The apps that bring us together and make work a little easier.',
      catalogue: 'Application catalogue',
      search: 'Search applications...',
      arcelik: 'Developed by Arçelik',
      other: 'Other applications',
      all: 'All apps'
    },
    tr: {
      heading: 'İş gününüzün vazgeçilmezlerini keşfedin.',
      subtitle: 'Bizi bir araya getiren ve işimizi kolaylaştıran uygulamalar.',
      catalogue: 'Uygulama kataloğu',
      search: 'Uygulama ara...',
      arcelik: 'Arçelik uygulamaları',
      other: 'Diğer uygulamalar',
      all: 'Tüm uygulamalar'
    }
  }
});
function migrateCatalogue(v) {
  const ids = new Set(v.apps.map(a => a.id));
  let apps = v.apps.map(a => ({
    ...a,
    employeeTypes: (a.employeeTypes || []).map(t => t === 'Office' ? 'White-collar' : t === 'Field' ? 'Blue-collar' : t)
  }));
  if (!v.migrations?.legacyAppsV1) apps = [...apps, ...importedApps.filter(a => !ids.has(a.id))];
  if (!v.migrations?.testProfilesV1) apps = [...apps, ...scenarioApps.filter(a => !ids.has(a.id))];
  return {
    ...v,
    apps,
    categories: [...new Set([...(v.categories || baseCats), 'Test scenarios'])],
    migrations: {
      ...v.migrations,
      legacyAppsV1: true,
      testProfilesV1: true
    }
  };
}
function read() {
  try {
    const v = JSON.parse(localStorage.getItem('arcelik-mvp-v1'));
    return v && Array.isArray(v.apps) && Array.isArray(v.events) ? migrateCatalogue(v) : initial();
  } catch {
    return initial();
  }
}
function eligible(a, p) {
  return a.isActive && !a.isArchived && (!a.countries.length || a.countries.includes(p.country)) && a.employeeTypes.includes(p.type) && (!a.managerOnly || p.isManager === true);
}
function validUrl(url) {
  try {
    return ['https:'].includes(new URL(url).protocol);
  } catch {
    return false;
  }
}
function AppIcon({
  app
}) {
  if (app.id === 'bizpuan') return React.createElement("div", {
    className: "icon",
    style: {
      background: 'white'
    }
  }, React.createElement("svg", {
    "aria-hidden": "true",
    viewBox: "0 0 64 64",
    width: "47",
    height: "47"
  }, React.createElement("circle", {
    cx: "44",
    cy: "13",
    r: "11",
    fill: "#f9a62b"
  }), React.createElement("circle", {
    cx: "16",
    cy: "30",
    r: "14",
    fill: "#008ac5"
  }), React.createElement("circle", {
    cx: "43",
    cy: "50",
    r: "14",
    fill: "#d20e32"
  })));
  return React.createElement("div", {
    className: "icon",
    style: {
      color: app.color,
      background: app.bg
    }
  }, app.symbol || app.name[0]);
}
function Multi({
  title,
  values,
  onChange,
  options,
  help
}) {
  const [open, S] = useState(false),
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
      S(true);
    }
  }, values.length ? values.join(', ') : help || 'Select…', " ", React.createElement("span", {
    style: {
      float: 'right'
    }
  }, "\u2304")), open && React.createElement("div", {
    className: "pick"
  }, React.createElement("section", {
    role: "dialog",
    "aria-modal": "true",
    "aria-label": title,
    className: "picker"
  }, React.createElement("h3", null, title, " ", React.createElement("small", null, "(", draft.length, " selected)")), React.createElement("input", {
    autoFocus: true,
    type: "search",
    placeholder: 'Search ' + title.toLowerCase(),
    value: q,
    onChange: e => Q(e.target.value)
  }), React.createElement("div", {
    className: "checks"
  }, options.filter(x => x.toLowerCase().includes(q.toLowerCase())).map(x => React.createElement("label", {
    key: x
  }, React.createElement("input", {
    type: "checkbox",
    checked: draft.includes(x),
    onChange: () => D(draft.includes(x) ? draft.filter(y => y !== x) : [...draft, x])
  }), x))), React.createElement("div", {
    className: "pickfoot"
  }, React.createElement("button", {
    type: "button",
    className: "btn",
    onClick: () => D([])
  }, "Clear"), React.createElement("button", {
    type: "button",
    className: "btn",
    onClick: () => S(false)
  }, "Cancel"), React.createElement("button", {
    type: "button",
    className: "btn primary",
    onClick: () => {
      onChange(draft);
      S(false);
    }
  }, "Apply (", draft.length, ")")))));
}
function Detail({
  app: a,
  onClose,
  onOpen,
  person,
  preview = false
}) {
  const links = a.links.filter(l => !l.country || l.country === person.country);
  return React.createElement("section", {
    className: "detail"
  }, React.createElement("div", {
    className: "detailhead"
  }, React.createElement(AppIcon, {
    app: a
  }), React.createElement("div", null, React.createElement("h2", null, a.name || 'Application name'), React.createElement("p", null, a.categories.join(' · ') || 'Select a category')), onClose && React.createElement("button", {
    className: "close",
    onClick: onClose,
    "aria-label": "Close application details"
  }, "\xD7")), React.createElement("p", null, a.shortDescription), a.managerOnly && React.createElement("p", {
    className: "info"
  }, "Managers only"), React.createElement("h3", null, "Get the application"), (preview ? a.links : links).map((l, i) => React.createElement("div", {
    className: "linkrow",
    key: l.id || i
  }, React.createElement("div", null, React.createElement("b", null, l.label || 'Application link'), React.createElement("small", null, l.platform || 'Web', l.country ? ' · ' + l.country : '', l.version ? ' · v' + l.version : '', l.pending ? ' · Download address pending' : '')), React.createElement("button", {
    disabled: preview || l.pending || !l.url && !l.openPath,
    className: "btn primary",
    onClick: () => onOpen(a, l)
  }, l.pending ? 'Coming soon' : l.platform === 'Web' ? 'Open app' : 'Get app', " \u2197"))), !links.length && !preview && React.createElement("p", {
    className: "info"
  }, "No links are available for your country."), React.createElement("h3", null, "About this application"), React.createElement("p", null, a.description), a.images.length > 0 && React.createElement(React.Fragment, null, React.createElement("h3", null, "A closer look"), a.images.map((url, i) => React.createElement("img", {
    className: "screen",
    key: i,
    src: url,
    alt: a.name + ' application preview ' + (i + 1),
    onError: e => {
      e.currentTarget.style.display = 'none';
    }
  }))), a.documents.length > 0 && React.createElement(React.Fragment, null, React.createElement("h3", null, "Helpful resources"), a.documents.map((d, i) => React.createElement("div", {
    className: "linkrow",
    key: i
  }, React.createElement("span", null, "\u25A4 \xA0", d.title), React.createElement("button", {
    disabled: preview,
    className: "btn",
    onClick: () => onOpen(a, {
      ...d,
      platform: 'Document'
    }, true)
  }, "View \u2197")))), React.createElement("div", {
    className: "facts"
  }, React.createElement("div", null, "Version", React.createElement("b", null, a.version)), React.createElement("div", null, "Availability", React.createElement("b", null, a.countries.length ? a.countries.join(', ') : 'Global')), React.createElement("div", null, "Platforms", React.createElement("b", null, a.platforms.join(', ')))));
}
function Editor({
  app,
  categories,
  onCancel,
  onSave,
  person
}) {
  const [a, A] = useState(() => structuredClone(app)),
    [error, E] = useState('');
  const update = (k, v) => A(x => ({
    ...x,
    [k]: v
  }));
  const item = (type, i, k, v) => update(type, a[type].map((x, j) => i === j ? {
    ...x,
    [k]: v,
    ...(type === 'links' && k === 'url' ? {
      pending: !v.trim()
    } : {})
  } : x));
  function save(e) {
    e.preventDefault();
    if (!a.name.trim() || !a.shortDescription.trim() || !a.description.trim() || !a.version.trim()) return E('Please fill in the name, summary, description and version.');
    if (!a.categories.length || !a.employeeTypes.length || !a.platforms.length) return E('Select at least one category, employee type and platform.');
    if (!a.links.length || a.links.some(l => !l.label.trim() || !l.pending && !validUrl(l.url) || l.url && !validUrl(l.url))) return E('Add labelled HTTPS links, or mark unavailable addresses as pending.');
    if (a.documents.some(d => !d.title.trim() || !(validUrl(d.url) || d.url === 'guide.html'))) return E('Each document needs a title and an HTTPS address.');
    if (a.images.some(u => u !== 'sample-screen.svg' && !validUrl(u))) return E('Screenshot addresses must begin with https://.');
    onSave({
      ...a,
      name: a.name.trim()
    });
  }
  return React.createElement("div", {
    className: "modalback"
  }, React.createElement("form", {
    className: "modal",
    onSubmit: save,
    role: "dialog",
    "aria-modal": "true",
    "aria-label": "Application editor"
  }, React.createElement("div", {
    className: "modaltop"
  }, React.createElement("div", null, React.createElement("div", {
    className: "eyebrow",
    style: {
      marginBottom: 7
    }
  }, "Application editor"), React.createElement("h2", null, app.id ? 'Edit application' : 'Add application')), React.createElement("button", {
    type: "button",
    className: "close",
    onClick: () => {
      if (confirm('Discard unsaved changes?')) onCancel();
    },
    "aria-label": "Close editor"
  }, "\xD7")), React.createElement("div", {
    className: "modalbody"
  }, React.createElement("div", null, error && React.createElement("div", {
    className: "error",
    role: "alert"
  }, error), React.createElement("label", {
    className: "field"
  }, "Application name", React.createElement("input", {
    maxLength: "80",
    value: a.name,
    onChange: e => update('name', e.target.value),
    required: true
  })), React.createElement("label", {
    className: "field"
  }, "Short description", React.createElement("input", {
    maxLength: "160",
    value: a.shortDescription,
    onChange: e => update('shortDescription', e.target.value),
    required: true
  })), React.createElement("label", {
    className: "field"
  }, "Description", React.createElement("textarea", {
    value: a.description,
    onChange: e => update('description', e.target.value),
    required: true
  })), React.createElement("div", {
    className: "twocol"
  }, React.createElement("label", {
    className: "field"
  }, "Catalogue tab", React.createElement("select", {
    value: a.tab,
    onChange: e => update('tab', e.target.value)
  }, React.createElement("option", {
    value: "arcelik"
  }, "Developed by Ar\xE7elik"), React.createElement("option", {
    value: "other"
  }, "Other applications"))), React.createElement("label", {
    className: "field"
  }, "Version", React.createElement("input", {
    value: a.version,
    onChange: e => update('version', e.target.value),
    required: true
  }))), React.createElement("div", {
    className: "twocol"
  }, React.createElement("label", {
    className: "field"
  }, "Icon symbol", React.createElement("input", {
    maxLength: "3",
    value: a.symbol,
    onChange: e => update('symbol', e.target.value)
  })), React.createElement("label", {
    className: "field"
  }, "Icon background", React.createElement("input", {
    type: "color",
    value: a.bg,
    onChange: e => update('bg', e.target.value)
  }))), React.createElement(Multi, {
    title: "Categories",
    values: a.categories,
    onChange: v => update('categories', v),
    options: categories
  }), React.createElement(Multi, {
    title: "Countries",
    values: a.countries,
    onChange: v => update('countries', v),
    options: countries,
    help: "Global \xB7 all countries"
  }), React.createElement(Multi, {
    title: "Employee types",
    values: a.employeeTypes,
    onChange: v => update('employeeTypes', v),
    options: types
  }), React.createElement("label", {
    className: "flex info"
  }, React.createElement("input", {
    type: "checkbox",
    checked: !!a.managerOnly,
    onChange: e => update('managerOnly', e.target.checked)
  }), " Managers only (employee attribute, not an admin role)"), React.createElement(Multi, {
    title: "Platforms",
    values: a.platforms,
    onChange: v => update('platforms', v),
    options: platforms
  }), React.createElement("label", {
    className: "flex info"
  }, React.createElement("input", {
    type: "checkbox",
    checked: a.isActive,
    onChange: e => update('isActive', e.target.checked)
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
    className: "flex info"
  }, React.createElement("input", {
    type: "checkbox",
    checked: !!l.pending,
    onChange: e => item('links', i, 'pending', e.target.checked)
  }), " Download address pending"), React.createElement("label", {
    className: "field"
  }, "HTTPS address", React.createElement("input", {
    placeholder: "https://\u2026",
    value: l.url,
    onChange: e => item('links', i, 'url', e.target.value)
  })), React.createElement("div", {
    className: "twocol"
  }, React.createElement("label", {
    className: "field"
  }, "Platform", React.createElement("select", {
    value: l.platform,
    onChange: e => item('links', i, 'platform', e.target.value)
  }, [...new Set([...platforms, l.platform])].map(p => React.createElement("option", {
    key: p
  }, p)))), React.createElement("label", {
    className: "field"
  }, "Country", React.createElement("select", {
    value: l.country,
    onChange: e => item('links', i, 'country', e.target.value)
  }, React.createElement("option", {
    value: ""
  }, "Global"), countries.map(c => React.createElement("option", {
    key: c
  }, c))))), React.createElement("button", {
    type: "button",
    className: "btn danger",
    onClick: () => update('links', a.links.filter((_, j) => j !== i))
  }, "Remove link"))), React.createElement("button", {
    type: "button",
    className: "btn",
    onClick: () => update('links', [...a.links, {
      id: uid(),
      label: '',
      url: '',
      platform: 'Web',
      country: ''
    }])
  }, "+ Add link"), React.createElement("h3", {
    className: "subheading"
  }, "Documents"), a.documents.map((d, i) => React.createElement("div", {
    className: "assetrow",
    key: d.id
  }, React.createElement("label", {
    className: "field"
  }, "Document title", React.createElement("input", {
    value: d.title,
    onChange: e => item('documents', i, 'title', e.target.value)
  })), React.createElement("label", {
    className: "field"
  }, "Document URL", React.createElement("input", {
    value: d.url,
    onChange: e => item('documents', i, 'url', e.target.value)
  })), React.createElement("button", {
    type: "button",
    className: "btn danger",
    onClick: () => update('documents', a.documents.filter((_, j) => j !== i))
  }, "Remove document"))), React.createElement("button", {
    type: "button",
    className: "btn",
    onClick: () => update('documents', [...a.documents, {
      id: uid(),
      title: '',
      url: ''
    }])
  }, "+ Add document"), React.createElement("h3", {
    className: "subheading"
  }, "Screenshots"), a.images.map((u, i) => React.createElement("div", {
    className: "flex",
    key: i,
    style: {
      marginBottom: 9
    }
  }, React.createElement("input", {
    "aria-label": 'Screenshot URL ' + (i + 1),
    className: "textinput",
    style: {
      flex: 1,
      minWidth: 100
    },
    value: u,
    onChange: e => update('images', a.images.map((v, j) => i === j ? e.target.value : v))
  }), React.createElement("button", {
    type: "button",
    className: "btn danger",
    onClick: () => update('images', a.images.filter((_, j) => j !== i))
  }, "Remove"))), React.createElement("button", {
    type: "button",
    className: "btn",
    onClick: () => update('images', [...a.images, ''])
  }, "+ Add screenshot")), React.createElement("div", null, React.createElement("div", {
    className: "previewlabel"
  }, "\u25C9 \xA0 Private live preview \xB7 unsaved"), React.createElement(Detail, {
    app: a,
    person: person,
    preview: true
  }))), React.createElement("div", {
    className: "modalfoot"
  }, React.createElement("span", null, "Changes become visible immediately after saving."), React.createElement("div", {
    className: "flex"
  }, React.createElement("button", {
    type: "button",
    className: "btn",
    onClick: () => {
      if (confirm('Discard unsaved changes?')) onCancel();
    }
  }, "Cancel"), React.createElement("button", {
    className: "btn primary",
    type: "submit"
  }, "Save application")))));
}
function CatalogueApp({
  companyUser,
  onSignOut,
  serverData,
  sharedState,
  sharedPersona,
  onPersonaChange,
  onDatabaseChange
}) {
  const [data, D] = useState(() => serverData || read()),
    [localPersonId, LocalP] = useState('rumeysa'),
    [page, Page] = useState('catalogue'),
    [lang, L] = useState('en'),
    [tab, T] = useState('arcelik'),
    [q, Q] = useState(''),
    [query, SQ] = useState(''),
    [cat, C] = useState('All apps'),
    [selected, S] = useState(null),
    [editing, SetEditing] = useState(null),
    [adminTab, AT] = useState('Applications'),
    [toast, Toast] = useState(''),
    [storageError, SE] = useState(false),
    [adminQ, AQ] = useState(''),
    [newCat, NC] = useState(''),
    [translationKey, TK] = useState('heading'),
    [translationValue, TV] = useState('');
  const personId = sharedPersona || localPersonId,
    P = onPersonaChange || LocalP;
  const person = sharedState?.profile || companyUser || personas.find(p => p.id === personId),
    isAdmin = !companyUser && data.admins.includes(personId),
    t = {
      ...data.translations.en,
      ...data.translations[lang]
    };
  useEffect(() => {
    if (companyUser || sharedState) return;
    try {
      localStorage.setItem('arcelik-mvp-v1', JSON.stringify(data));
      SE(false);
    } catch {
      SE(true);
    }
  }, [data]);
  useEffect(() => {
    if (toast) {
      let timer = setTimeout(() => Toast(''), 4500);
      return () => clearTimeout(timer);
    }
  }, [toast]);
  useEffect(() => {
    document.documentElement.lang = lang;
  }, [lang]);
  useEffect(() => {
    if (!isAdmin && page === 'admin') Page('catalogue');
  }, [isAdmin, page]);
  useEffect(() => {
    if (sharedState) D(sharedState);
  }, [sharedState]);
  const Edit = value => SetEditing(value && sharedState ? {
    ...value,
    _baseRevision: data._revision
  } : value);
  const event = (action, entity, extra = {}) => ({
    id: uid(),
    actor: person.name,
    actorId: person.id,
    action,
    entity,
    time: new Date().toISOString(),
    correlationId: uid(),
    ...extra
  });
  function audit(action, entity, patch, before = null, after = null, requestedRevision = null) {
    if (!isAdmin) return Toast('Admin access is required.');
    if (sharedState) {
      Toast('Saving to the shared Test database…');
      return onDatabaseChange({
        action,
        entity,
        patch,
        before,
        after,
        revision: requestedRevision ?? data._revision
      }).then(() => {
        Toast('Saved to the shared Test database.');
        return true;
      }).catch(e => {
        Toast(e.message);
        return false;
      });
    }
    const ev = event(action, entity, {
      kind: 'admin',
      before,
      after
    });
    D(d => ({
      ...d,
      ...patch,
      events: [...d.events, ev]
    }));
  }
  function search(e) {
    e.preventDefault();
    if (sharedState) {
      fetch('/api/test/events/search', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'X-Test-Persona': person.id
        },
        body: JSON.stringify({
          term: q
        })
      }).then(r => {
        if (!r.ok) throw Error();
        SQ(q);
        S(null);
      }).catch(() => Toast('The search could not be saved. Please try again.'));
      return;
    }
    if (companyUser) {
      fetch('/api/events/search', {
        method: 'POST',
        credentials: 'same-origin',
        headers: {
          'Content-Type': 'application/json'
        },
        body: JSON.stringify({
          term: q
        })
      }).then(r => {
        if (!r.ok) throw Error();
        SQ(q);
        S(null);
      }).catch(() => Toast('Search could not be recorded. Please refresh or contact support.'));
      return;
    }
    SQ(q);
    S(null);
    D(d => ({
      ...d,
      events: [...d.events, event('Search', q || '(empty search)', {
        kind: 'user',
        outcome: 'recorded'
      })]
    }));
  }
  function open(a, l, doc = false) {
    if (sharedState) {
      if (l.pending || !l.openPath) return Toast('The download address has not been added yet.');
      if (!l.openPath.startsWith('/api/test/open/')) return Toast('This link is unavailable.');
      window.open(l.openPath, '_blank', 'noopener,noreferrer');
      return;
    }
    if (companyUser) {
      if (l.pending || !l.openPath) return Toast('The download address has not been added yet.');
      if (!l.openPath.startsWith('/api/apps/')) return Toast('This link is unavailable.');
      window.open(l.openPath, '_blank', 'noopener,noreferrer');
      return;
    }
    if (l.pending || !l.url) return Toast('The download address has not been added yet.');
    if (!eligible(a, person) || l.country && l.country !== person.country) return Toast('This application is not available for your profile.');
    if (!(validUrl(l.url) || doc && l.url === 'guide.html')) return Toast('This link is unavailable. Please contact an administrator.');
    const ev = event(doc ? 'Document opened' : 'Application opened', a.name, {
      kind: 'user',
      linkId: l.id,
      applicationId: a.id,
      outcome: 'opened',
      destination: l.url
    });
    try {
      const next = {
        ...data,
        events: [...data.events, ev]
      };
      localStorage.setItem('arcelik-mvp-v1', JSON.stringify(next));
      D(next);
      window.open(l.url, '_blank', 'noopener,noreferrer');
      Toast('Link opened in a new tab. Click recorded.');
    } catch {
      Toast('Unable to save activity. The link was not opened.');
    }
  }
  async function save(a) {
    if (!isAdmin) return;
    const {
      _baseRevision,
      ...clean
    } = a;
    const old = data.apps.find(x => x.id === a.id),
      next = {
        ...clean,
        id: a.id || uid()
      };
    const result = audit(old ? 'Application edited' : 'Application created', next.name, {
      apps: old ? data.apps.map(x => x.id === a.id ? next : x) : [...data.apps, next]
    }, old, next, _baseRevision);
    if (sharedState && !(await result)) return;
    Edit(null);
    Toast('Application saved. Eligible employees can see it now.');
  }
  function status(a, k) {
    audit(k === 'isArchived' ? a[k] ? 'Application restored' : 'Application archived' : a[k] ? 'Application deactivated' : 'Application activated', a.name, {
      apps: data.apps.map(x => x.id === a.id ? {
        ...x,
        [k]: !x[k]
      } : x)
    }, a, {
      ...a,
      [k]: !a[k]
    });
    Toast('Application updated.');
  }
  const apps = data.apps.filter(a => eligible(a, person) && a.tab === tab && (!query || `${a.name} ${a.shortDescription} ${a.description}`.toLowerCase().includes(query.toLowerCase())) && (cat === 'All apps' || a.categories.includes(cat)));
  const current = data.apps.find(a => a.id === selected && eligible(a, person));
  const blank = () => ({
    ...mk('', '', '', data.categories[0] || 'Productivity', 'a', '#fff', '#c92937'),
    description: '',
    shortDescription: '',
    documents: [],
    images: []
  });
  return React.createElement(React.Fragment, null, React.createElement("header", null, React.createElement("div", {
    className: "brand"
  }, React.createElement("span", {
    className: "brandmark"
  }), "ar\xE7elik", React.createElement("small", null, "App Store")), React.createElement("nav", {
    className: "nav"
  }, React.createElement("button", {
    className: page === 'catalogue' ? 'active' : '',
    onClick: () => {
      Page('catalogue');
      S(null);
    }
  }, lang === 'tr' ? 'Uygulamalar' : 'Discover apps'), isAdmin && React.createElement("button", {
    className: page === 'admin' ? 'active' : '',
    onClick: () => Page('admin')
  }, "Administration"), React.createElement("button", {
    className: page === 'about' ? 'active' : '',
    onClick: () => Page('about')
  }, "About")), React.createElement("div", {
    className: "headright"
  }, React.createElement("select", {
    "aria-label": "Language",
    className: "lang",
    value: lang,
    onChange: e => L(e.target.value)
  }, React.createElement("option", {
    value: "en"
  }, "EN"), React.createElement("option", {
    value: "tr"
  }, "TR")), React.createElement("div", {
    className: "avatar"
  }, person.initials), !companyUser && React.createElement("select", {
    className: "persona",
    "aria-label": "Demo persona",
    value: personId,
    onChange: e => {
      P(e.target.value);
      S(null);
      Edit(null);
      Page('catalogue');
      Toast('Demo profile changed. Catalogue and access refreshed.');
    }
  }, personas.map(p => React.createElement("option", {
    key: p.id,
    value: p.id
  }, p.name, " \xB7 ", data.admins.includes(p.id) ? 'Admin' : 'Employee'))), companyUser && React.createElement("span", {
    className: "signedinname"
  }, person.name))), React.createElement("div", {
    className: "demo"
  }, React.createElement("b", null, companyUser ? 'VERIFIED EMPLOYEE · PILOT' : sharedState ? 'SHARED TEST DATABASE' : 'TEST ENVIRONMENT'), React.createElement("span", null, companyUser ? 'Profile supplied by your company · ' : sharedState ? 'Changes are shared across Test users · ' : 'Sample apps · Changes saved in this browser only · ', person.country, " / ", person.type, person.isManager ? ' / Manager' : '')), storageError && React.createElement("div", {
    role: "alert",
    className: "error"
  }, "Browser storage is full or unavailable. Your latest changes are not saved."), React.createElement("main", {
    className: "main"
  }, !companyUser && React.createElement("section", {
    className: "test-profile-bar"
  }, React.createElement("div", null, React.createElement("b", null, "Test employee"), React.createElement("p", null, "Switch profiles to check which applications each employee can see.")), React.createElement("label", null, React.createElement("span", {
    className: "sr-only"
  }, "Test employee profile"), React.createElement("select", {
    "aria-label": "Test employee profile",
    value: personId,
    onChange: e => {
      P(e.target.value);
      S(null);
      Edit(null);
      Page('catalogue');
      C('All apps');
      Q('');
      SQ('');
    }
  }, personas.map(p => React.createElement("option", {
    key: p.id,
    value: p.id
  }, profileLabel(p), data.admins.includes(p.id) ? ' · Admin' : ''))))), page === 'catalogue' && React.createElement(React.Fragment, null, React.createElement("section", {
    className: "hero"
  }, React.createElement("div", null, React.createElement("div", {
    className: "eyebrow"
  }, "Your digital workplace"), React.createElement("h1", null, lang === 'en' ? React.createElement(React.Fragment, null, "Your everyday work.", React.createElement("br", null), React.createElement("span", null, "A little more connected.")) : React.createElement(React.Fragment, null, "\u0130\u015F g\xFCn\xFCn\xFCz.", React.createElement("br", null), React.createElement("span", null, "Biraz daha ba\u011Flant\u0131l\u0131."))), React.createElement("p", null, t.subtitle)), React.createElement("div", {
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
  }, React.createElement("span", {
    className: "brandmark",
    style: {
      background: 'white',
      width: 42,
      height: 40
    }
  })), React.createElement("div", {
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
    placeholder: t.search,
    value: q,
    onChange: e => Q(e.target.value)
  }), React.createElement("button", {
    type: "submit",
    "aria-label": "Submit search"
  }, "\u21B5"))), React.createElement("div", {
    className: "catalogtabs"
  }, React.createElement("button", {
    className: tab === 'arcelik' ? 'active' : '',
    onClick: () => {
      T('arcelik');
      S(null);
    }
  }, t.arcelik, React.createElement("span", {
    className: "count"
  }, data.apps.filter(a => eligible(a, person) && a.tab === 'arcelik').length)), React.createElement("button", {
    className: tab === 'other' ? 'active' : '',
    onClick: () => {
      T('other');
      S(null);
    }
  }, t.other, React.createElement("span", {
    className: "count"
  }, data.apps.filter(a => eligible(a, person) && a.tab === 'other').length))), React.createElement("div", {
    className: "filters"
  }, ['All apps', ...data.categories].map(c => React.createElement("button", {
    key: c,
    className: 'chip ' + (cat === c ? 'active' : ''),
    onClick: () => {
      C(c);
      S(null);
    }
  }, c === 'All apps' ? t.all : c)), React.createElement("span", {
    className: "right"
  }, apps.length, " applications available")), query && React.createElement("p", {
    className: "info"
  }, "Results for \u201C", query, "\u201D ", React.createElement("button", {
    className: "btn",
    onClick: () => {
      Q('');
      SQ('');
      S(null);
    }
  }, "Clear search")), React.createElement("div", {
    className: current ? 'split' : ''
  }, React.createElement("div", {
    className: "grid"
  }, apps.map(a => React.createElement("button", {
    key: a.id,
    className: 'card ' + (a.id === selected ? 'selected' : ''),
    onClick: () => S(a.id)
  }, React.createElement("div", {
    className: "cardtop"
  }, React.createElement(AppIcon, {
    app: a
  }), React.createElement("span", {
    className: "badge"
  }, a.platforms.join(' · '))), React.createElement("h3", null, a.name, a.isTestScenario && React.createElement("span", {
    className: "test-app-tag"
  }, "TEST APP")), React.createElement("p", null, a.shortDescription), React.createElement("div", {
    className: "cardfoot"
  }, React.createElement("span", {
    className: "categorylabel"
  }, a.categories.join(' · ')), React.createElement("span", {
    style: {
      marginLeft: 5
    }
  }, "\xB7"), React.createElement("span", null, "v", a.version), React.createElement("span", {
    className: "arrow"
  }, "\u2197")))), !apps.length && React.createElement("div", {
    className: "empty"
  }, React.createElement("h3", null, "No applications found"), React.createElement("p", null, "Try a different search or category."), React.createElement("button", {
    className: "btn",
    style: {
      marginTop: 18
    },
    onClick: () => {
      C('All apps');
      Q('');
      SQ('');
    }
  }, "Clear filters"))), current && React.createElement(Detail, {
    app: current,
    person: person,
    onClose: () => S(null),
    onOpen: open
  })), React.createElement("div", {
    className: "bottomnote"
  }, "\u2667 \xA0 Curated for you. Connected to Ar\xE7elik."), React.createElement("div", {
    className: "flex",
    style: {
      justifyContent: 'center'
    }
  }, React.createElement("button", {
    className: "btn",
    onClick: () => Page('about')
  }, "About this demo"), isAdmin && React.createElement("button", {
    className: "btn",
    onClick: () => Page('admin')
  }, "Manage catalogue \u2192"))), page === 'admin' && isAdmin && React.createElement(React.Fragment, null, React.createElement("div", {
    className: "sectiontop",
    style: {
      marginTop: 0
    }
  }, React.createElement("div", null, React.createElement("div", {
    className: "eyebrow",
    style: {
      marginBottom: 10
    }
  }, "Administration"), React.createElement("h2", null, "Make work more connected."), React.createElement("p", null, "Manage the catalogue, content and access. Saved changes take effect immediately.")), React.createElement("button", {
    className: "btn primary",
    onClick: () => Edit(blank())
  }, "+ Add application")), React.createElement("div", {
    className: "adminnav"
  }, ['Applications', 'Categories', 'Translations', 'Admin access', 'Activity'].map(x => React.createElement("button", {
    className: 'chip ' + (adminTab === x ? 'active' : ''),
    key: x,
    onClick: () => AT(x)
  }, x))), adminTab === 'Applications' && React.createElement(React.Fragment, null, React.createElement("div", {
    style: {
      marginBottom: 18
    }
  }, React.createElement("input", {
    className: "textinput",
    placeholder: "Find an application\u2026",
    "aria-label": "Find an application",
    value: adminQ,
    onChange: e => AQ(e.target.value)
  })), React.createElement("div", {
    className: "tablewrap"
  }, React.createElement("table", null, React.createElement("thead", null, React.createElement("tr", null, React.createElement("th", null, "Application"), React.createElement("th", null, "Audience"), React.createElement("th", null, "Status"), React.createElement("th", null, "Actions"))), React.createElement("tbody", null, data.apps.filter(a => a.name.toLowerCase().includes(adminQ.toLowerCase())).map(a => React.createElement("tr", {
    key: a.id
  }, React.createElement("td", null, React.createElement("div", {
    className: "appcell"
  }, React.createElement(AppIcon, {
    app: a
  }), React.createElement("div", null, React.createElement("b", null, a.name), React.createElement("p", {
    className: "info",
    style: {
      margin: '3px 0',
      fontSize: 10
    }
  }, a.categories.join(', '))))), React.createElement("td", null, a.countries.length ? a.countries.join(', ') : 'Global', React.createElement("p", {
    className: "info",
    style: {
      margin: '3px 0',
      fontSize: 10
    }
  }, a.employeeTypes.join(', '))), React.createElement("td", null, React.createElement("span", {
    className: 'status ' + (!a.isActive || a.isArchived ? 'off' : '')
  }, a.isArchived ? 'Archived' : a.isActive ? 'Active' : 'Inactive')), React.createElement("td", null, React.createElement("div", {
    className: "rowactions"
  }, React.createElement("button", {
    className: "btn",
    onClick: () => Edit(a)
  }, "Edit"), React.createElement("button", {
    className: "btn",
    onClick: () => status(a, 'isActive')
  }, a.isActive ? 'Deactivate' : 'Activate'), React.createElement("button", {
    className: "btn danger",
    onClick: () => status(a, 'isArchived')
  }, a.isArchived ? 'Restore' : 'Archive'))))))))), adminTab === 'Categories' && React.createElement("section", {
    className: "panel"
  }, React.createElement("h3", null, "Catalogue categories"), React.createElement("p", {
    className: "info"
  }, "Applications can belong to multiple categories. Categories in use cannot be removed."), React.createElement("form", {
    className: "flex",
    onSubmit: e => {
      e.preventDefault();
      const n = newCat.trim();
      if (!n) return;
      if (data.categories.some(c => c.toLowerCase() === n.toLowerCase())) return Toast('This category already exists.');
      audit('Category created', n, {
        categories: [...data.categories, n]
      });
      NC('');
      Toast('Category created.');
    }
  }, React.createElement("input", {
    className: "textinput",
    required: true,
    maxLength: "50",
    "aria-label": "New category",
    placeholder: "New category name",
    value: newCat,
    onChange: e => NC(e.target.value)
  }), React.createElement("button", {
    className: "btn primary"
  }, "Add category")), data.categories.map(c => React.createElement("div", {
    className: "listitem",
    key: c
  }, React.createElement("div", null, React.createElement("b", null, c), React.createElement("p", null, data.apps.filter(a => a.categories.includes(c)).length, " applications")), React.createElement("div", {
    className: "flex"
  }, React.createElement("button", {
    className: "btn",
    onClick: () => {
      const n = prompt('Rename category', c)?.trim();
      if (!n || n === c) return;
      if (data.categories.some(x => x.toLowerCase() === n.toLowerCase())) return Toast('Category name already exists.');
      audit('Category renamed', c, {
        categories: data.categories.map(x => x === c ? n : x),
        apps: data.apps.map(a => ({
          ...a,
          categories: a.categories.map(x => x === c ? n : x)
        }))
      }, {
        name: c
      }, {
        name: n
      });
      if (cat === c) C(n);
    }
  }, "Rename"), React.createElement("button", {
    className: "btn danger",
    disabled: data.apps.some(a => a.categories.includes(c)),
    onClick: () => audit('Unused category removed', c, {
      categories: data.categories.filter(x => x !== c)
    })
  }, "Remove"))))), adminTab === 'Translations' && React.createElement("section", {
    className: "panel"
  }, React.createElement("h3", null, "Catalogue text"), React.createElement("p", {
    className: "info"
  }, "Choose the language in the header. English is the fallback. App descriptions use the content entered in the application editor."), React.createElement("label", {
    className: "field"
  }, "Text key", React.createElement("select", {
    value: translationKey,
    onChange: e => {
      TK(e.target.value);
      TV('');
    }
  }, Object.keys(data.translations.en).map(k => React.createElement("option", {
    key: k
  }, k)))), React.createElement("label", {
    className: "field"
  }, "Current ", lang === 'en' ? 'English' : 'Turkish', " text", React.createElement("textarea", {
    readOnly: true,
    value: t[translationKey] || ''
  })), React.createElement("form", {
    onSubmit: e => {
      e.preventDefault();
      if (!translationValue.trim()) return;
      audit('Translation edited', lang + '.' + translationKey, {
        translations: {
          ...data.translations,
          [lang]: {
            ...data.translations[lang],
            [translationKey]: translationValue.trim()
          }
        }
      }, {
        value: t[translationKey]
      }, {
        value: translationValue.trim()
      });
      TV('');
      Toast('Translation saved.');
    }
  }, React.createElement("label", {
    className: "field"
  }, "New text", React.createElement("textarea", {
    required: true,
    value: translationValue,
    onChange: e => TV(e.target.value),
    placeholder: "Enter the updated text\u2026"
  })), React.createElement("button", {
    className: "btn primary"
  }, "Save translation"))), adminTab === 'Admin access' && React.createElement("section", {
    className: "panel"
  }, React.createElement("h3", null, "Admin access"), React.createElement("p", {
    className: "info"
  }, "Grant or revoke access for existing demo employees. Each change is recorded. At least one administrator must remain."), personas.map(p => React.createElement("div", {
    className: "listitem",
    key: p.id
  }, React.createElement("div", null, React.createElement("b", null, p.name), React.createElement("p", null, p.id, " \xB7 ", p.country, " \xB7 ", p.type)), React.createElement("button", {
    className: 'btn ' + (data.admins.includes(p.id) ? '' : 'primary'),
    disabled: data.admins.includes(p.id) && data.admins.length === 1,
    onClick: () => {
      const has = data.admins.includes(p.id);
      audit(has ? 'Admin access revoked' : 'Admin access granted', p.name, {
        admins: has ? data.admins.filter(x => x !== p.id) : [...data.admins, p.id]
      }, {
        isAdmin: has
      }, {
        isAdmin: !has
      });
      Toast('Admin access updated.');
    }
  }, data.admins.includes(p.id) ? 'Revoke admin' : 'Grant admin')))), adminTab === 'Activity' && React.createElement(React.Fragment, null, React.createElement("p", {
    className: "info"
  }, "Separate records for every search, open and admin change. ", sharedState ? 'The latest 200 database records are shown; an app click does not confirm installation.' : 'These are browser-local demo records; an app click does not confirm installation.'), React.createElement("div", {
    className: "tablewrap"
  }, React.createElement("table", null, React.createElement("thead", null, React.createElement("tr", null, React.createElement("th", null, "Time"), React.createElement("th", null, "Person"), React.createElement("th", null, "Action"), React.createElement("th", null, "Item"), React.createElement("th", null, "Record"))), React.createElement("tbody", null, [...data.events].reverse().map(ev => React.createElement("tr", {
    key: ev.id
  }, React.createElement("td", null, new Date(ev.time).toLocaleString()), React.createElement("td", null, ev.actor), React.createElement("td", null, ev.action, React.createElement("p", {
    className: "info",
    style: {
      margin: 0,
      fontSize: 10
    }
  }, ev.kind === 'admin' ? 'Admin audit' : 'User event')), React.createElement("td", null, ev.entity), React.createElement("td", null, React.createElement("details", null, React.createElement("summary", null, ev.id.slice(0, 8)), React.createElement("pre", {
    style: {
      maxWidth: 300,
      whiteSpace: 'pre-wrap',
      fontSize: 10
    }
  }, JSON.stringify(ev, null, 2)))))), !data.events.length && React.createElement("tr", null, React.createElement("td", {
    colSpan: "5"
  }, "No activity yet. Search for an app or make an admin change."))))))), page === 'about' && !companyUser && !sharedState && React.createElement("section", {
    className: "panel about"
  }, React.createElement("div", {
    className: "eyebrow",
    style: {
      marginBottom: 15
    }
  }, "A working first version"), React.createElement("h2", null, "Your Ar\xE7elik App Store, reimagined."), React.createElement("p", null, "This interactive demo lets you try the employee and admin journeys without installing anything. All people, app listings and versions are sample data. The Ar\xE7elik-developed sample apps link to the public corporate website; third-party listings link to their public services."), React.createElement("h3", null, "Try it out"), React.createElement("ol", null, React.createElement("li", null, "Explore both catalogue tabs, search with Enter, and filter categories."), React.createElement("li", null, "Open a card to view details, platform links and a sample guide."), React.createElement("li", null, "Choose Zeynep in the profile menu to manage applications."), React.createElement("li", null, "Add or edit an app. Check the private preview, then save."), React.createElement("li", null, "Switch to R\xFCmeysa and check what employees can see."), React.createElement("li", null, "Try Alex to see country and employee-type targeting."), React.createElement("li", null, "Review Activity as an admin to see separate records for repeated actions.")), React.createElement("h3", null, "What this demo saves"), React.createElement("p", null, "Apps, categories, translations, admin assignments and activity are saved only in this browser. Refreshing keeps them; another browser has its own data. Demo profiles and permissions are for testing and are not a security boundary."), React.createElement("h3", null, "Rules used for this test"), React.createElement("p", null, "No countries selected means Global. At least one employee type and platform is required. Global and matching-country links appear together; no device filtering is used. Searches are recorded on Enter or search-submit. Inactive and archived apps are hidden from employees. Archived apps can be restored. If activity cannot be saved, a link will not open. The interface supports English and Turkish with English fallback."), React.createElement("h3", null, "Next: your company environment"), React.createElement("p", null, "Okta sign-in is managed by your hosting platform. This app opens directly into the sample catalogue and does not read your Okta identity or permissions yet. The shared database, protected backend APIs, audit records and approved app inventory still need to be connected before company use."), React.createElement("div", {
    className: "flex"
  }, React.createElement("button", {
    className: "btn primary",
    onClick: () => Page('catalogue')
  }, "Explore applications \u2192"), React.createElement("button", {
    className: "btn danger",
    onClick: () => {
      if (confirm('Reset all demo changes and activity in this browser?')) {
        D(initial());
        P('rumeysa');
        Page('catalogue');
        S(null);
        C('All apps');
        Q('');
        SQ('');
        Toast('Demo reset to sample data.');
      }
    }
  }, "Reset demo data"))), page === 'about' && sharedState && React.createElement("section", {
    className: "panel about"
  }, React.createElement("h2", null, "Shared Test catalogue"), React.createElement("p", null, "Catalogue edits, targeting, translations, admin assignments and activity are saved in the Test PostgreSQL database and shared across browsers. The selected employee is a synthetic Test profile."), React.createElement("p", null, "Only an Admin can save catalogue changes. Managers are an employee attribute and do not automatically receive Admin access. The Test database is separate from any future Production data."), React.createElement("button", {
    className: "btn primary",
    onClick: () => Page('catalogue')
  }, "Back to catalogue")), page === 'about' && companyUser && React.createElement("section", {
    className: "panel about"
  }, React.createElement("h2", null, "Your application catalogue"), React.createElement("p", null, "Your company gateway verifies your identity. The server shows applications matching your country and employee classification. Profile switching and browser-local administration are unavailable in this mode."), React.createElement("p", null, "This is a pilot catalogue with sample inventory. Please contact your administrator if an application or download address is missing."), React.createElement("button", {
    className: "btn primary",
    onClick: () => Page('catalogue')
  }, "Back to catalogue"))), React.createElement("footer", null, React.createElement("span", null, React.createElement("b", null, "ar\xE7elik"), " \xA0\xB7\xA0 App Store"), React.createElement("span", null, "Made for a more connected workday."), React.createElement("span", null, "Interactive MVP \xB7 Test environment")), editing && isAdmin && React.createElement(Editor, {
    app: editing,
    categories: data.categories,
    person: person,
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
function ProfileNotice({
  title,
  message,
  code,
  onRetry
}) {
  return React.createElement("main", {
    className: "main"
  }, React.createElement("section", {
    className: "panel about",
    role: "alert"
  }, React.createElement("div", {
    className: "brand",
    style: {
      marginBottom: 25
    }
  }, React.createElement("span", {
    className: "brandmark"
  }), "ar\xE7elik"), React.createElement("h2", null, title), React.createElement("p", {
    className: "info"
  }, message), code && React.createElement("p", {
    className: "info"
  }, "Support reference: ", code), onRetry && React.createElement("button", {
    className: "btn primary",
    onClick: onRetry
  }, "Try again")));
}
function NormCatalogue() {
  const [state, State] = useState({
      loading: true
    }),
    [reload, Reload] = useState(0);
  useEffect(() => {
    let alive = true,
      controller;
    async function load() {
      controller?.abort();
      controller = new AbortController();
      const signal = controller.signal;
      State({
        loading: true
      });
      try {
        const response = await fetch('/api/me', {
          credentials: 'same-origin',
          cache: 'no-store',
          signal
        });
        const me = await response.json();
        if (!response.ok) throw Object.assign(new Error(me.message || 'Your company profile could not be verified.'), {
          code: me.correlationId
        });
        if (!me.verified || !me.canBrowse) throw Object.assign(new Error('Your country and employee classification have not been supplied or configured. Please contact your platform team.'), {
          code: me.profileStatus
        });
        const r = await fetch('/api/apps', {
          credentials: 'same-origin',
          cache: 'no-store',
          signal
        });
        const apps = await r.json();
        if (!r.ok || !Array.isArray(apps)) throw Object.assign(new Error(apps.message || 'Your applications could not be loaded.'), {
          code: apps.correlationId
        });
        if (alive) State({
          me,
          apps
        });
      } catch (e) {
        if (alive && e.name !== 'AbortError') State({
          error: e.message,
          code: e.code
        });
      }
    }
    load();
    window.addEventListener('focus', load);
    return () => {
      alive = false;
      controller?.abort();
      window.removeEventListener('focus', load);
    };
  }, [reload]);
  if (state.loading) return React.createElement(ProfileNotice, {
    title: "Loading your applications\u2026",
    message: "Checking your company profile."
  });
  if (state.error) return React.createElement(ProfileNotice, {
    title: "Your profile needs attention",
    message: state.error,
    code: state.code,
    onRetry: () => Reload(x => x + 1)
  });
  const me = state.me,
    person = {
      id: me.employeeId,
      name: me.email || me.employeeId,
      country: me.country,
      type: me.type,
      isManager: me.isManager === true,
      initials: (me.email || me.employeeId).slice(0, 2).toUpperCase()
    };
  const data = {
    ...initial(),
    apps: state.apps,
    categories: [...new Set(state.apps.flatMap(a => a.categories || []))],
    admins: [],
    events: []
  };
  return React.createElement(CatalogueApp, {
    key: me.employeeId + me.country + me.type,
    companyUser: person,
    serverData: data
  });
}
function SharedTestCatalogue() {
  const [persona, Persona] = useState('rumeysa'),
    [state, State] = useState({
      loading: true
    }),
    [saving, Saving] = useState(false);
  const active = React.useRef(persona);
  active.current = persona;
  async function request(path, body) {
    const response = await fetch(path, {
      method: body ? 'POST' : 'GET',
      credentials: 'same-origin',
      cache: 'no-store',
      headers: {
        'X-Test-Persona': persona,
        ...(body ? {
          'Content-Type': 'application/json'
        } : {})
      },
      ...(body ? {
        body: JSON.stringify(body)
      } : {})
    });
    let value;
    try {
      value = await response.json();
    } catch {
      value = {};
    }
    if (!response.ok) throw new Error(value.message || 'The shared Test database could not be reached. Please refresh and try again.');
    return value;
  }
  useEffect(() => {
    let alive = true;
    const selected = persona;
    async function load(silent = false) {
      if (!silent) State({
        loading: true
      });
      try {
        const data = await request('/api/test/state');
        if (alive && active.current === selected) State({
          data
        });
      } catch (e) {
        if (alive && active.current === selected) State({
          error: e.message
        });
      }
    }
    load();
    const focus = () => load(true);
    window.addEventListener('focus', focus);
    const timer = setInterval(() => load(true), 10000);
    return () => {
      alive = false;
      clearInterval(timer);
      window.removeEventListener('focus', focus);
    };
  }, [persona]);
  async function change(command) {
    Saving(true);
    try {
      const data = await request('/api/test/admin/commands', command);
      State({
        data
      });
      return data;
    } catch (e) {
      try {
        const data = await request('/api/test/state');
        State({
          data
        });
      } catch {}
      throw e;
    } finally {
      Saving(false);
    }
  }
  if (state.loading || state.data && state.data.profile?.id !== persona) return React.createElement(ProfileNotice, {
    title: "Loading the shared Test catalogue\u2026",
    message: "Connecting to the Test database."
  });
  if (state.error) return React.createElement(ProfileNotice, {
    title: "The Test database needs attention",
    message: state.error,
    onRetry: () => window.location.reload()
  });
  return React.createElement("div", {
    "aria-busy": saving,
    style: {
      pointerEvents: saving ? 'none' : 'auto'
    }
  }, React.createElement(CatalogueApp, {
    key: persona,
    serverData: state.data,
    sharedState: state.data,
    sharedPersona: persona,
    onPersonaChange: Persona,
    onDatabaseChange: change
  }));
}
function ApplicationRoot() {
  const mode = window.ARCELIK_RUNTIME?.mode;
  if (mode === 'testdb') return React.createElement(SharedTestCatalogue, null);
  if (mode === 'demo') return React.createElement(CatalogueApp, null);
  if (mode === 'authgate') return React.createElement(NormCatalogue, null);
  return React.createElement(ProfileNotice, {
    title: "Application configuration is unavailable",
    message: "Please refresh. If the problem continues, contact your platform team."
  });
}
ReactDOM.createRoot(document.getElementById('root')).render(React.createElement(ApplicationRoot, null));

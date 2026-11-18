"""Build a standalone review page from the deployable frontend assets."""

import base64
import json
import mimetypes
import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
DIST = ROOT / "dist"
ASSET_PATH = re.compile(r"assets/[A-Za-z0-9_./-]+")

BOOTSTRAP = """
(async () => {
  try {
    if (location.protocol === 'file:') {
      (0, eval)(__LOCAL_PREVIEW__);
      return;
    }
    const response = await fetch('/runtime-config.js', { credentials: 'same-origin' });
    if (!response.ok) {
      throw Error('The .NET backend is not available. Download this HTML and open it locally to review the screens.');
    }
    (0, eval)(await response.text());
    if (!window.ARCELIK_RUNTIME) throw Error('Runtime configuration is missing.');
    (0, eval)(__HOSTED_APP__);
  } catch (error) {
    document.getElementById('root').textContent = error.message;
  }
})();
"""

PREVIEW_STYLES = """
.preview-bar {
  padding: 12px 24px;
  background: #fff2de;
  color: #794500;
  display: flex;
  gap: 20px;
  justify-content: space-between;
  align-items: center;
}
.preview-bar span { display: block; font-size: 12px; }
.preview-bar button { border: 1px solid #c9945c; border-radius: 12px; background: #fff; padding: 10px; }
.boot-error { padding: 30px; max-width: 800px; margin: auto; }
"""


def inline_assets():
    assets = {}
    for path in (DIST / "assets").rglob("*"):
        if path.is_file():
            mime_type = mimetypes.guess_type(path.name)[0] or "application/octet-stream"
            encoded = base64.b64encode(path.read_bytes()).decode("ascii")
            assets[path.relative_to(DIST).as_posix()] = f"data:{mime_type};base64,{encoded}"
    return lambda text: ASSET_PATH.sub(lambda match: assets.get(match[0], match[0]), text)


def script(source):
    return "<script>" + source.replace("</script", r"<\/script") + "</script>"


def build_html():
    inline = inline_assets()
    seed = json.loads((ROOT / "src/AppStore.Web/experience-seed.json").read_text(encoding="utf-8"))
    seed.update(
        languages=[{"code": "en", "name": "English"}, {"code": "tr", "name": "Türkçe"}],
        fallbackLanguage="en",
        audit=[],
        activity=[],
    )
    styles = "\n".join(
        inline((DIST / name).read_text(encoding="utf-8"))
        for name in ("fonts.css", "experience.css", "review-fixes.css")
    )
    head = f"""<!doctype html>
<html lang="en">
<head>
  <meta charset="utf-8">
  <meta name="viewport" content="width=device-width,initial-scale=1">
  <title>Arçelik App Store — HTML review</title>
  <style>{styles}{PREVIEW_STYLES}</style>
</head>
<body>
  <div id="root">Loading…</div>
"""
    scripts = [
        script(inline((DIST / name).read_text(encoding="utf-8")))
        for name in ("react.js", "react-dom.js", "experience-template.js", "experience-view.js")
    ]
    scripts.append(script("window.ARCELIK_HTML_SEED=" + inline(json.dumps(seed, ensure_ascii=False)) + ";"))
    sources = {
        "__LOCAL_PREVIEW__": (ROOT / "scripts/html-preview.js").read_text(encoding="utf-8"),
        "__HOSTED_APP__": (DIST / "app.js").read_text(encoding="utf-8"),
    }
    bootstrap = re.sub(
        r"__LOCAL_PREVIEW__|__HOSTED_APP__",
        lambda match: json.dumps(sources[match[0]]),
        BOOTSTRAP,
    )
    scripts.append(script(bootstrap))
    return head + "\n".join(scripts) + "\n</body>\n</html>\n"


if __name__ == "__main__":
    html = build_html()
    for output in (DIST / "admin-review.html", ROOT / "downloads/Arcelik-AppStore-Admin.html"):
        output.parent.mkdir(parents=True, exist_ok=True)
        output.write_text(html, encoding="utf-8")
        print(f"{output}: {output.stat().st_size:,} bytes")

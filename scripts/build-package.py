"""Package the deployable project and verify its contents."""

import hashlib
import subprocess
from pathlib import Path
from zipfile import ZIP_DEFLATED, ZipFile

ROOT = Path(__file__).resolve().parents[1]
MANIFEST = Path("PACKAGE-CONTENTS.sha256")
OUTPUT = ROOT / "downloads/Arcelik-AppStore-Admin-Update.zip"
EXCLUDED_DIRECTORIES = {
    ".git", ".openai", ".secrets", "attachments", "bin", "node_modules", "obj",
}
REQUIRED_FILES = {
    "Dockerfile",
    "dist/index.html",
    "dist/admin-review.html",
    "frontend/app.jsx",
    "src/AppStore.Web/Program.cs",
    "src/AppStore.Web/Database/ExperienceReports.cs",
    "vendor/nuget/Npgsql.10.0.3.nupkg",
}


def include(path):
    if path == MANIFEST or path.suffix == ".zip" or EXCLUDED_DIRECTORIES.intersection(path.parts):
        return False
    if path.name == "secrets.json" or path.name.endswith(".local.json"):
        return False
    if path.name.startswith(".env") and path.name != ".env.example":
        return False
    return (ROOT / path).is_file() and not (ROOT / path).is_symlink()


def project_files():
    output = subprocess.check_output(
        ["git", "ls-files", "--cached", "--others", "--exclude-standard", "-z"],
        cwd=ROOT,
    )
    names = set(output.decode().split("\0")) - {""}
    return [path for name in sorted(names) if include(path := Path(name))]


def digest(data):
    return hashlib.sha256(data).hexdigest()


def build_package():
    files = project_files()
    checksums = {path.as_posix(): digest((ROOT / path).read_bytes()) for path in files}
    missing = REQUIRED_FILES - checksums.keys()
    if missing:
        raise ValueError(f"Missing required files: {', '.join(sorted(missing))}")

    (ROOT / MANIFEST).write_text(
        "".join(f"{checksum}  {name}\n" for name, checksum in checksums.items()),
        encoding="utf-8",
    )
    OUTPUT.parent.mkdir(parents=True, exist_ok=True)
    with ZipFile(OUTPUT, "w", ZIP_DEFLATED, compresslevel=9) as archive:
        for path in [*files, MANIFEST]:
            archive.write(ROOT / path, path.as_posix())

    with ZipFile(OUTPUT) as archive:
        corrupt = archive.testzip()
        if corrupt:
            raise ValueError(f"Corrupt archive entry: {corrupt}")
        for name, checksum in checksums.items():
            if digest(archive.read(name)) != checksum:
                raise ValueError(f"Checksum mismatch: {name}")

    print(f"Verified {len(files) + 1} files in {OUTPUT.name}")


if __name__ == "__main__":
    build_package()

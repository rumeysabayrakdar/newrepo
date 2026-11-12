# Put this project on GitHub

1. Download and unzip the archive.
2. On GitHub, create a repository called `arcelik-app-store` (Private is recommended).
3. Choose **uploading an existing file** or **Add file → Upload files**.
4. Drag in the contents of the extracted `arcelik-mvp` folder, including the `src` and `dist` folders. Upload the extracted files, not the ZIP itself.
5. Select **Commit changes**.

The repo root should contain README.md, Dockerfile, app.jsx, package.json, src/ and dist/. The adapted .csproj is in src/AppStore.Web/. The uploaded `download` file was a Docker ignore file and is correctly named `.dockerignore` here. Hidden files may be hidden in your file picker; include them where possible. No installation is required to upload through GitHub's website.

Your existing hosted demo continues to work. GitHub stores the code; uploading does not automatically deploy it. This package is not a completed production backend.

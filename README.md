# Arçelik App Store — OneProject fixes

.NET 10 + React + PostgreSQL/Npgsql. Read START-HERE.md for upload steps and CHANGES.md for the six requested corrections.

AppStore:Environment in base appsettings.json is the sole Test/Production switch. Test locks Authentication:Mode to TestProfiles; Production locks it to Gateway, independent of stale hosting overrides. Test accounts do not exist as Production sign-in options. Real Okta is handled by Norm's protected gateway, not a separate application login page.

Database connections can be environment-specific (ConnectionStrings:Test / Production) or supplied by Norm as an AppStore connection or PostgreSQL variables. Blank expected names follow the supplied connection; an explicit name remains validated. Separate database markers reject cross-environment reuse. Startup applies additive schema and seed/bootstrap operations without erasing saved application content. Favourites use appstore.user_preferences, keyed by signed-in account, not localStorage.

Built React assets/fonts and the pinned Npgsql package are included. NuGet.Config uses vendor/nuget for offline package restore; container SDK/runtime images are provided by the host's registry. No Node/Python backend or TypeORM is used. npm ci and npm run build are development tools only.

The corrected Test public origin is https://appstore-x.norm.ar.arcelik, matching the screenshot. If Norm saves an older Hosting__TrustedPublicOrigin in Env, update it as START-HERE.md describes. Cross-site/wrong-origin requests remain rejected.

Live framework checks use tests/review-fixes.cjs against isolated .NET/PostgreSQL services on ports 18088 and 18091. They write only synthetic fixture data. See CHANGES.md for the checks actually performed. Full live Norm deployment, external APK installation and real Okta sign-in are not verified from this workspace.

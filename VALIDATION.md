# Verification — six requested fixes

- .NET 10 Alpine Release publish succeeded with networking disabled and the bundled Npgsql package feed.
- Actual Test and Production-mode services used separate isolated PostgreSQL databases. Test accepted its sample account without any gateway identity even with a stale Gateway override. Production ignored a TestProfiles override, rejected Test identities and accepted only the simulated trusted-gateway identity.
- React DOM tests verified no TEST badge or preview-account element in Production, including stale browser Test session values.
- Desktop category navigation was visible on the first render and remained open after outside clicks and web/back navigation.
- PostgreSQL favourites persisted across fresh clients. Selected SVG stars filled in both light and dark states; rejection left state unchanged, and retry succeeded.
- Web cards share application-style cover/name/badge/favourite markup. The uploaded images, files and APK endpoints are retained.
- The browser-local Sites preview workflow still passes. Preview data is separate from PostgreSQL.

Live Norm access settings, live Okta sign-in and native visual browser QA were not available to verify. The Production identity tests used only synthetic fixture identities from a configured private test gateway. They do not establish a live Okta connection. Existing Norm databases were not accessed or reset.

# Hosting-managed Okta sign-in

The app no longer displays a login page or starts its own Okta flow. Okta access is handled by the hosting platform (Norm, as configured by the project owner). The catalogue opens immediately once a visitor reaches it.

Keep the hosting platform's Okta access protection enabled. This package does not configure or verify that external protection and does not enforce sign-in when served independently. A plain static host exposes the demo directly unless the host adds access controls.

No Okta issuer, Client ID, client secret or callback page is required by this frontend. Hosting sign-in is not yet mapped to application identity, country, employee type or admin privileges. The visible persona menu is explicitly for testing synthetic catalogue profiles; it does not manage real Okta accounts.

The included .NET server remains a static demo host, not an authenticated backend. Shared data, server-side authorization and trusted identity integration remain future backend work.

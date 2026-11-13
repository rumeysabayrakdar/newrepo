# Okta through Norm

Norm auth-gate handles sign-in. The application has no second login page, OAuth SDK, password form or access token handling.

Production validates the immediate gateway network and these standard headers: X-User-Sicil (employee ID), X-User-Id (distinct internal GUID), optional X-User-Email. Configure Authentication:TrustedProxyCidrs and ensure the gateway overwrites client identity headers and is the only network path to the app.

Country, white-/blue-collar classification and manager attributes are not read or required. Everyone browses the same active applications using manual search/category/platform filters. Employee and Admin roles are stored in PostgreSQL; verified sign-in alone does not grant Admin.

An explicitly configured InitialAdminEmployeeIds entry bootstraps the first real Admin after that exact identity signs in. It is audited once. Later grants/revocations apply to registered signed-in identities. Test uses only the two appsettings.Test.json accounts, and Production never accepts the Test user header/query.

Live Norm tenant/gateway settings still require its platform administrator. DEPLOYMENT.md provides the operator instructions.

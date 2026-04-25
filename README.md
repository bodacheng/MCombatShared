# MCombat Shared

This package contains gameplay modules that should be treated as MCombat-owned shared code.

PocketStriker consumes these modules through thin adapters so MCombat can remain the source of truth for shared battle behavior. PocketStriker currently references this package from MCombat through `Packages/manifest.json`.

Current modules:

- `Account`: shared player-account state model used by PlayFab/login flows.
- `CombatGroup`: boss/group battle unit-count rules extracted from MCombat's group fight flow.
- `CombatHit`: shared hit-detection definitions and pure damage utility rules.

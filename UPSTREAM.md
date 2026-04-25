# Upstream Policy

MCombat is the source of truth for this package.

Current local upstream project:

```text
/Users/daisei/MComat
```

Target layout:

```text
MComat/Packages/com.mcombat.shared
PocketStriker/Packages/manifest.json -> file:../MComat/Packages/com.mcombat.shared
```

PocketStriker-specific code should stay outside this package and connect through adapters.

MCombat keeps shared source files in `Packages/com.mcombat.shared`. PocketStriker consumes that package with:

```json
"com.mcombat.shared": "file:../MComat/Packages/com.mcombat.shared"
```

Keep old `Assets` copies removed in both projects to avoid duplicate type definitions.

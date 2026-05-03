# SourceSync

`SourceSync` contains project-assembly source that must stay identical in MCombat and PocketStriker, but cannot live under `Runtime` because it depends on project-local types such as `Soul.Behavior`.

Run `sync-to-projects.sh` after editing these files. The script copies the shared `Behaviour` tree into both projects and refreshes the embedded `com.mcombat.shared` packages while excluding `SourceSync` from Unity package import.

`Behaviour/BtnUI/MobileInputsManager.cs` is intentionally not stored here because it is still project-specific. The sync script also preserves project-local `.meta` files so MonoBehaviour GUIDs and execution-order settings do not drift between existing Unity assets.

Run `verify-source-sync.sh` to confirm that all non-excluded `Assets/Behaviour` source files match `SourceSync` and that embedded packages are in sync.

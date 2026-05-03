#!/usr/bin/env bash
set -euo pipefail

shared_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
mcombat_project="${MCOMBAT_PROJECT:-/Users/daisei/MComat}"
pocket_project="${POCKET_PROJECT:-/Users/daisei/PocketStriker}"

python3 - "$shared_root" "$mcombat_project" "$pocket_project" <<'PY'
from pathlib import Path
import filecmp
import sys

shared_root = Path(sys.argv[1])
projects = [Path(p) for p in sys.argv[2:]]
source_root = shared_root / "SourceSync" / "Behaviour"
excluded = {"BtnUI/MobileInputsManager.cs"}
errors = []

source_files = {
    p.relative_to(source_root).as_posix(): p
    for p in source_root.rglob("*.cs")
}

for rel in excluded:
    if rel in source_files:
        errors.append(f"excluded file must stay project-local: {rel}")

for project in projects:
    behaviour_root = project / "Assets" / "Behaviour"
    project_files = {
        p.relative_to(behaviour_root).as_posix(): p
        for p in behaviour_root.rglob("*.cs")
        if p.relative_to(behaviour_root).as_posix() not in excluded
    }

    missing_from_source = sorted(set(project_files) - set(source_files))
    for rel in missing_from_source:
        errors.append(f"{project}: project Behaviour file is not SourceSync-managed: {rel}")

    for rel, src in source_files.items():
        dst = behaviour_root / rel
        if not dst.exists():
            errors.append(f"{project}: missing synced file: {rel}")
        elif not filecmp.cmp(src, dst, shallow=False):
            errors.append(f"{project}: synced file differs: {rel}")

    if (project / "Packages" / "com.mcombat.shared" / "SourceSync").exists():
        errors.append(f"{project}: embedded package must not contain SourceSync")

if errors:
    print("SourceSync verification failed:")
    for error in errors:
        print(f"  {error}")
    sys.exit(1)

print("SourceSync Behaviour files match both projects.")
PY

for project in "$mcombat_project" "$pocket_project"; do
  diff -qr --exclude .git --exclude SourceSync "$shared_root" "$project/Packages/com.mcombat.shared" >/dev/null
done

echo "Embedded com.mcombat.shared packages match shared root."

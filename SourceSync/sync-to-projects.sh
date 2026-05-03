#!/usr/bin/env bash
set -euo pipefail

shared_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
mcombat_project="${MCOMBAT_PROJECT:-/Users/daisei/MComat}"
pocket_project="${POCKET_PROJECT:-/Users/daisei/PocketStriker}"
obsolete_behaviour_files=(
  "Soul/BehaviorRunner_unused.cs"
  "Soul/BehaviorRunner_unused.cs.meta"
  "Soul/BehaviorRunner_EditorData.cs"
  "Soul/BehaviorRunner_EditorData.cs.meta"
  "Soul/Sensor/Sensor_Get.cs"
  "Soul/Sensor/Sensor_Get.cs.meta"
  "GUI/SkillOptions.cs"
  "GUI/SkillOptions.cs.meta"
  "Soul/SkillSet/SkillSet_Validation.cs"
  "Soul/SkillSet/SkillSet_Validation.cs.meta"
  "Soul/BehaviorRunner_Form.cs"
  "Soul/BehaviorRunner_Form.cs.meta"
  "Soul/SkillSet/SkillSet_Tools.cs"
  "Soul/SkillSet/SkillSet_Tools.cs.meta"
  "Soul/SkillSet/RandomSet/RandomSkillSet.cs"
  "Soul/SkillSet/RandomSet/RandomSkillSet.cs.meta"
)

sync_project() {
  local project="$1"
  local package_dir="$project/Packages/com.mcombat.shared"
  local behaviour_dir="$project/Assets/Behaviour"

  mkdir -p "$behaviour_dir" "$package_dir"
  rsync -a --delete \
    --exclude '*.meta' \
    --exclude 'BtnUI/MobileInputsManager.cs' \
    "$shared_root/SourceSync/Behaviour/" \
    "$behaviour_dir/"
  for obsolete in "${obsolete_behaviour_files[@]}"; do
    rm -f "$behaviour_dir/$obsolete"
  done
  rsync -a --delete --exclude .git --exclude SourceSync "$shared_root/" "$package_dir/"
}

sync_project "$mcombat_project"
sync_project "$pocket_project"

echo "Synced SourceSync and com.mcombat.shared to:"
echo "  $mcombat_project"
echo "  $pocket_project"

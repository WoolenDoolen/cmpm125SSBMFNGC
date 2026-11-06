#!/usr/bin/env bash
set -euo pipefail

project_root="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.." && pwd -P)"
version="$(sed -n 's/^m_EditorVersion: //p' "$project_root/ProjectSettings/ProjectVersion.txt" | tr -d '\r')"

if [[ -z "$version" ]]; then
  printf 'Could not read the Unity editor version.\n' >&2
  exit 1
fi

editor="${UNITY_EDITOR_PATH:-}"
if [[ -z "$editor" ]]; then
  for candidate in \
    "/mnt/c/Program Files/Unity/Hub/Editor/$version/Editor/Unity.exe" \
    "$HOME/Unity/Hub/Editor/$version/Editor/Unity" \
    "/opt/unity/Editor/$version/Editor/Unity"; do
    if [[ -x "$candidate" ]]; then
      editor="$candidate"
      break
    fi
  done
fi

if [[ -z "$editor" || ! -x "$editor" ]]; then
  printf 'Unity %s was not found. Install it in Unity Hub or set UNITY_EDITOR_PATH.\n' "$version" >&2
  printf 'https://unity.com/releases/editor/whats-new/%s\n' "$version" >&2
  exit 1
fi

project_path="$project_root"
if [[ "$editor" == *.[eE][xX][eE] ]]; then
  if ! command -v wslpath >/dev/null 2>&1; then
    printf 'Launching a Windows Unity executable requires WSL.\n' >&2
    exit 1
  fi
  project_path="$(wslpath -w "$project_root")"
fi

exec "$editor" -projectPath "$project_path" "$@"

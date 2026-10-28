#!/usr/bin/env bash
# Runs the committed scene and all domain/runtime checks without building a player.
set -euo pipefail
project_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$project_root"
if ! command -v unity >/dev/null 2>&1; then
    printf '%s\n' 'Unity CLI is required. See README.md for validation instructions.' >&2
    exit 1
fi
mkdir -p TestResults Logs
unity test "$project_root" --mode EditMode --report-format nunit,junit \
    --output TestResults/refactor-editmode.xml --timeout 300 -- -logFile Logs/refactor-editmode.log
unity test "$project_root" --mode PlayMode --report-format nunit,junit \
    --output TestResults/refactor-playmode.xml --timeout 300 -- -logFile Logs/refactor-playmode.log

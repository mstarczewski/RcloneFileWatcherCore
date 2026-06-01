#!/usr/bin/env bash
# Creates a large tree of tiny files for testing the watcher / sync under load.
# Usage:   ./make-test-files.sh [ROOT] [TOTAL] [PER_DIR]
# Example: ./make-test-files.sh /mnt/samba/Test/bulk 100000 50
# Cleanup: rm -rf <ROOT>
set -euo pipefail

ROOT="${1:-./testdata}"
TOTAL="${2:-100000}"     # total number of files
PER_DIR="${3:-50}"       # files per leaf directory

echo "Creating $TOTAL tiny files under '$ROOT' ($PER_DIR per leaf dir)..."
start=$(date +%s)

i=0
dir_index=0
while [ "$i" -lt "$TOTAL" ]; do
  # Nest as ROOT/dir_<l1>/sub_<l2> (100 subdirs per top-level dir).
  l1=$(( dir_index / 100 ))
  l2=$(( dir_index % 100 ))
  d="$ROOT/dir_${l1}/sub_${l2}"
  mkdir -p "$d"
  for ((j = 0; j < PER_DIR && i < TOTAL; j++)); do
    printf 'test %d\n' "$i" > "$d/file_${i}.txt"
    i=$(( i + 1 ))
  done
  dir_index=$(( dir_index + 1 ))
done

end=$(date +%s)
echo "Done: $i files in $dir_index leaf dirs under '$ROOT' ($(( end - start ))s)."

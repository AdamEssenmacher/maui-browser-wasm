# Parity Tools

This folder contains parity-program helpers.

Files:
- `maui-modern-controls.tsv`
  - Canonical inventory for modern MAUI controls tracked by parity roadmap.
- `extract_maui_handler_inventory.sh`
  - Prints inventory + current browser registrations + matrix status snapshot.
- `check_parity_matrix.sh`
  - Fails when `docs/PARITY_MATRIX.md` is missing any control listed in the inventory.

Usage:
- `bash tools/parity/extract_maui_handler_inventory.sh`
- `bash tools/parity/check_parity_matrix.sh`

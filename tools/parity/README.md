# Parity Tools

This folder contains parity-program helpers.

Files:
- `maui-modern-controls.tsv`
  - Canonical inventory for modern MAUI controls tracked by parity roadmap.
- `extract_maui_handler_inventory.sh`
  - Prints inventory + current browser registrations + matrix status snapshot.
- `check_parity_matrix.sh`
  - Fails when `docs/PARITY_MATRIX.md` is missing any control listed in the inventory.
- `generate_handler_parity_matrix.sh`
  - Generates `docs/HANDLER_PARITY_MATRIX.md` from official MAUI upstream registration sources (pinned commit).
- `check_handler_parity_matrix.sh`
  - Validates `docs/HANDLER_PARITY_MATRIX.md` completeness/columns and locked `F/F => Mirage none` policy.

Usage:
- `bash tools/parity/extract_maui_handler_inventory.sh`
- `bash tools/parity/check_parity_matrix.sh`
- `bash tools/parity/generate_handler_parity_matrix.sh`
- `bash tools/parity/check_handler_parity_matrix.sh`

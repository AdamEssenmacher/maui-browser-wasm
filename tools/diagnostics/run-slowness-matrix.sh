#!/usr/bin/env bash
set -euo pipefail

script_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
repo_root="$(cd "${script_dir}/../.." && pwd)"

if ! command -v rg >/dev/null 2>&1; then
  echo "ripgrep (rg) is required." >&2
  exit 1
fi

timestamp="$(date +%Y%m%d-%H%M%S)"
run_root="${TMPDIR:-/tmp}/mirage-slowness-matrix-${timestamp}"
mkdir -p "${run_root}"

matrix_runs="${MIRAGE_MATRIX_RUNS:-5}"
test_project="tests/Mirage.E2E.Tests/Mirage.E2E.Tests.csproj"

declare -a scenarios=(
  "hero-typing|FullyQualifiedName~FoundationsLab_LabelInputUpdatesBoundOutput"
  "hero-nav-push|FullyQualifiedName~NavigationLab_MauiNavigation_UpdatesUrlWithoutFullReload"
  "nav-pop|FullyQualifiedName~NavigationLab_BrowserBackForward_SynchronizesVisiblePage"
  "deep-link-startup|FullyQualifiedName~NavigationLab_DeepLink_OpensCorrectPage"
)

append_metric() {
  local source_file="$1"
  local pattern="$2"
  local extract_expr="$3"
  local output_file="$4"

  if [[ ! -f "${source_file}" ]]; then
    return
  fi

  rg "${pattern}" "${source_file}" | sed -E "${extract_expr}" >> "${output_file}" || true
}

compute_stats() {
  local metric_name="$1"
  local values_file="$2"

  if [[ ! -f "${values_file}" ]]; then
    echo "${metric_name}: n=0"
    return
  fi

  local values=($(awk 'NF > 0 { print $1 }' "${values_file}"))
  local count="${#values[@]}"
  if [[ "${count}" -eq 0 ]]; then
    echo "${metric_name}: n=0"
    return
  fi

  local sorted=($(printf '%s\n' "${values[@]}" | sort -n))

  local median
  if (( count % 2 == 1 )); then
    median="${sorted[$((count / 2))]}"
  else
    local left="${sorted[$((count / 2 - 1))]}"
    local right="${sorted[$((count / 2))]}"
    median="$(awk -v a="${left}" -v b="${right}" 'BEGIN { printf "%.3f", (a + b) / 2 }')"
  fi

  local p95_index=$(( (95 * count + 99) / 100 - 1 ))
  if (( p95_index < 0 )); then
    p95_index=0
  fi
  if (( p95_index >= count )); then
    p95_index=$((count - 1))
  fi
  local p95="${sorted[$p95_index]}"

  local minimum="${sorted[0]}"
  local maximum="${sorted[$((count - 1))]}"

  echo "${metric_name}: n=${count} median=${median} p95=${p95} min=${minimum} max=${maximum}"
}

extract_test_seconds() {
  local test_log="$1"

  local raw
  raw="$(rg "Duration: [0-9]+(\.[0-9]+)? s" "${test_log}" | tail -n 1 || true)"
  if [[ -n "${raw}" ]]; then
    echo "${raw}" | sed -E 's/.*Duration: ([0-9]+(\.[0-9]+)?) s.*/\1/'
    return
  fi

  raw="$(rg "\[[0-9]+(\.[0-9]+)? s\]" "${test_log}" | tail -n 1 || true)"
  if [[ -z "${raw}" ]]; then
    echo ""
    return
  fi

  echo "${raw}" | sed -E 's/.*\[([0-9]+(\.[0-9]+)?) s\].*/\1/'
}

extract_stat_value() {
  local stat_name="$1"
  local stats_line="$2"

  echo "${stats_line}" | sed -nE "s/.*${stat_name}=([0-9]+(\\.[0-9]+)?).*/\\1/p"
}

run_mode() {
  local mode_name="$1"
  local hot_reload_value="$2"
  local mode_dir="${run_root}/${mode_name}"

  mkdir -p "${mode_dir}"
  : > "${mode_dir}/metric-nav-total-ms.txt"
  : > "${mode_dir}/metric-nav-apply-total-ms.txt"
  : > "${mode_dir}/metric-nav-ensurehandlers-ms.txt"
  : > "${mode_dir}/metric-render-total-ms.txt"
  : > "${mode_dir}/metric-entry-endtoend-ms.txt"
  : > "${mode_dir}/metric-entry-set-ms.txt"
  : > "${mode_dir}/metric-history-interop-ms.txt"
  : > "${mode_dir}/metric-hotreload-404-count.txt"
  : > "${mode_dir}/metric-test-duration-s.txt"

  echo "== ${mode_name} =="

  for scenario in "${scenarios[@]}"; do
    local scenario_name="${scenario%%|*}"
    local scenario_filter="${scenario#*|}"

    for run_index in $(seq 1 "${matrix_runs}"); do
      local run_key="${scenario_name}-run${run_index}"
      local diagnostics_log="${mode_dir}/${run_key}.diagnostics.log"
      local test_log="${mode_dir}/${run_key}.test.log"
      : > "${diagnostics_log}"

      echo "  -> ${run_key}"

      (
        cd "${repo_root}"
        MIRAGE_DIAGNOSTICS=1 \
        MIRAGE_HOT_RELOAD="${hot_reload_value}" \
        MIRAGE_DIAGNOSTICS_LOG_FILE="${diagnostics_log}" \
        dotnet test "${test_project}" \
          --nologo \
          --no-build \
          --filter "${scenario_filter}" \
          -l "console;verbosity=minimal"
      ) > "${test_log}" 2>&1

      local duration
      duration="$(extract_test_seconds "${test_log}")"
      if [[ -n "${duration}" ]]; then
        echo "${duration}" >> "${mode_dir}/metric-test-duration-s.txt"
      fi

      append_metric \
        "${diagnostics_log}" \
        "\\[Mirage.Diagnostics\\]\\[nav.map-request\\].*totalMs=" \
        's/.*totalMs=([0-9.]+).*/\1/' \
        "${mode_dir}/metric-nav-total-ms.txt"

      append_metric \
        "${diagnostics_log}" \
        "\\[Mirage.Diagnostics\\]\\[nav.apply-stack\\].*elapsedMs=" \
        's/.*elapsedMs=([0-9.]+).*/\1/' \
        "${mode_dir}/metric-nav-apply-total-ms.txt"

      append_metric \
        "${diagnostics_log}" \
        "\\[Mirage.Diagnostics\\]\\[nav.apply-stack\\].*ensureHandlersMs=" \
        's/.*ensureHandlersMs=([0-9.]+).*/\1/' \
        "${mode_dir}/metric-nav-ensurehandlers-ms.txt"

      append_metric \
        "${diagnostics_log}" \
        "\\[Mirage.Diagnostics\\]\\[runtime.render\\].*totalMs=" \
        's/.*totalMs=([0-9.]+).*/\1/' \
        "${mode_dir}/metric-render-total-ms.txt"

      append_metric \
        "${diagnostics_log}" \
        "\\[Mirage.Diagnostics\\]\\[entry.input\\].*phase=render-complete.*endToEndMs=" \
        's/.*endToEndMs=([0-9.]+).*/\1/' \
        "${mode_dir}/metric-entry-endtoend-ms.txt"

      append_metric \
        "${diagnostics_log}" \
        "\\[Mirage.Diagnostics\\]\\[entry.input\\].*phase=virtual-text-set.*setMs=" \
        's/.*setMs=([0-9.]+).*/\1/' \
        "${mode_dir}/metric-entry-set-ms.txt"

      append_metric \
        "${diagnostics_log}" \
        "\\[Mirage.Diagnostics\\]\\[history.interop\\].*elapsedMs=" \
        's/.*elapsedMs=([0-9.]+).*/\1/' \
        "${mode_dir}/metric-history-interop-ms.txt"

      append_metric \
        "${diagnostics_log}" \
        "\\[Mirage.Diagnostics\\]\\[hotreload.fetch\\].*notFound404=" \
        's/.*notFound404=([0-9]+).*/\1/' \
        "${mode_dir}/metric-hotreload-404-count.txt"
    done
  done

  echo
  echo "Summary (${mode_name})"
  compute_stats "test-duration-s" "${mode_dir}/metric-test-duration-s.txt"
  compute_stats "nav-total-ms" "${mode_dir}/metric-nav-total-ms.txt"
  compute_stats "nav-apply-total-ms" "${mode_dir}/metric-nav-apply-total-ms.txt"
  compute_stats "nav-ensurehandlers-ms" "${mode_dir}/metric-nav-ensurehandlers-ms.txt"
  compute_stats "render-total-ms" "${mode_dir}/metric-render-total-ms.txt"
  compute_stats "entry-endtoend-ms" "${mode_dir}/metric-entry-endtoend-ms.txt"
  compute_stats "entry-set-ms" "${mode_dir}/metric-entry-set-ms.txt"
  compute_stats "history-interop-ms" "${mode_dir}/metric-history-interop-ms.txt"
  compute_stats "hotreload-404-count" "${mode_dir}/metric-hotreload-404-count.txt"

  local nav_median nav_apply_median nav_apply_p95 nav_ensurehandlers_p95 render_median entry_median entry_set_median history_p95 hotreload_404_sum
  nav_median="$(extract_stat_value "median" "$(compute_stats "nav-total-ms" "${mode_dir}/metric-nav-total-ms.txt")")"
  nav_apply_median="$(extract_stat_value "median" "$(compute_stats "nav-apply-total-ms" "${mode_dir}/metric-nav-apply-total-ms.txt")")"
  nav_apply_p95="$(extract_stat_value "p95" "$(compute_stats "nav-apply-total-ms" "${mode_dir}/metric-nav-apply-total-ms.txt")")"
  nav_ensurehandlers_p95="$(extract_stat_value "p95" "$(compute_stats "nav-ensurehandlers-ms" "${mode_dir}/metric-nav-ensurehandlers-ms.txt")")"
  render_median="$(extract_stat_value "median" "$(compute_stats "render-total-ms" "${mode_dir}/metric-render-total-ms.txt")")"
  entry_median="$(extract_stat_value "median" "$(compute_stats "entry-endtoend-ms" "${mode_dir}/metric-entry-endtoend-ms.txt")")"
  entry_set_median="$(extract_stat_value "median" "$(compute_stats "entry-set-ms" "${mode_dir}/metric-entry-set-ms.txt")")"
  history_p95="$(extract_stat_value "p95" "$(compute_stats "history-interop-ms" "${mode_dir}/metric-history-interop-ms.txt")")"
  hotreload_404_sum="$(awk '{sum += $1} END {print sum + 0}' "${mode_dir}/metric-hotreload-404-count.txt" 2>/dev/null || echo 0)"

  echo
  echo "Classification (${mode_name})"

  if [[ -n "${nav_ensurehandlers_p95}" && -n "${render_median}" ]] \
    && awk -v ensure_handlers="${nav_ensurehandlers_p95}" -v render="${render_median}" 'BEGIN { exit !((ensure_handlers > 2000) && (render < 500)) }'; then
    echo "- navigation apply-stack issue likely in handler creation (ensureHandlers p95 > 2s while render is small)."
  elif [[ -n "${nav_apply_p95}" && -n "${render_median}" ]] \
    && awk -v nav_apply="${nav_apply_p95}" -v render="${render_median}" 'BEGIN { exit !((nav_apply > 2000) && (render < 500)) }'; then
    echo "- navigation apply-stack issue likely (stack apply > 2s while render stays small)."
  elif [[ -n "${nav_median}" && -n "${render_median}" ]] \
    && awk -v nav="${nav_median}" -v render="${render_median}" 'BEGIN { exit !((nav > 2000) && (render < 500)) }'; then
    echo "- navigation handshake issue likely (nav total > 2s while render stays small)."
  elif [[ -n "${entry_set_median}" && -n "${entry_median}" && -n "${render_median}" ]] \
    && awk -v entry_set="${entry_set_median}" -v entry_total="${entry_median}" -v render="${render_median}" 'BEGIN { exit !((entry_total > 0) && ((entry_set / entry_total) > 0.70) && (render < 20)) }'; then
    echo "- input pipeline issue likely before render (VirtualView.Text set dominates entry latency)."
  elif [[ -n "${entry_median}" && -n "${render_median}" ]] \
    && awk -v entry="${entry_median}" -v render="${render_median}" 'BEGIN { exit !((entry > 0) && ((render / entry) > 0.70)) }'; then
    echo "- render/diff bottleneck likely (render dominates input end-to-end latency)."
  elif [[ "${hotreload_404_sum}" -gt 0 ]]; then
    echo "- hot-reload fetch churn contributes (404s observed in diagnostics window)."
  elif [[ -n "${history_p95}" && -n "${render_median}" ]] \
    && awk -v history="${history_p95}" -v render="${render_median}" 'BEGIN { exit !((history > 100) && (render < 500)) }'; then
    echo "- browser-history interop likely contributes (high history interop p95 while render is small)."
  else
    echo "- inconclusive from current run; inspect raw diagnostics logs under ${mode_dir}."
  fi

  echo
}

run_mode "hotreload-on" "1"
run_mode "hotreload-off" "0"

echo "All artifacts: ${run_root}"

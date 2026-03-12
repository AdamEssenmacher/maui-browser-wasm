#!/usr/bin/env bash
set -euo pipefail

script_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
repo_root="$(cd "${script_dir}/../.." && pwd)"

if command -v rg >/dev/null 2>&1; then
  search_tool="rg"
elif command -v grep >/dev/null 2>&1; then
  search_tool="grep"
else
  echo "grep or ripgrep is required." >&2
  exit 1
fi

timestamp="$(date +%Y%m%d-%H%M%S)"
run_root="${MIRAGE_MATRIX_OUTPUT_DIR:-${TMPDIR:-/tmp}/mirage-slowness-matrix-${timestamp}}"
mkdir -p "${run_root}"

matrix_runs="${MIRAGE_MATRIX_RUNS:-5}"
test_project="tests/Mirage.E2E.Tests/Mirage.E2E.Tests.csproj"
matrix_configuration="${MIRAGE_MATRIX_CONFIGURATION:-Debug}"
budget_file="${MIRAGE_PERF_BUDGET_FILE:-}"
budget_enabled=0
budget_failed=0
budget_mode="hotreload-off"
budget_tolerance_ratio="0.30"

if [[ -n "${budget_file}" ]]; then
  if [[ ! -f "${budget_file}" ]]; then
    echo "Budget file not found: ${budget_file}" >&2
    exit 1
  fi

  # shellcheck source=/dev/null
  source "${budget_file}"
  budget_enabled=1
  budget_mode="${PERF_BUDGET_MODE:-${budget_mode}}"
  budget_tolerance_ratio="${PERF_BUDGET_TOLERANCE_RATIO:-${budget_tolerance_ratio}}"
fi

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

  search_lines "${pattern}" "${source_file}" | sed -E "${extract_expr}" >> "${output_file}" || true
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
  raw="$(search_lines "Duration: [0-9]+(\\.[0-9]+)? s" "${test_log}" | tail -n 1 || true)"
  if [[ -n "${raw}" ]]; then
    echo "${raw}" | sed -E 's/.*Duration: ([0-9]+(\.[0-9]+)?) s.*/\1/'
    return
  fi

  raw="$(search_lines "\\[[0-9]+(\\.[0-9]+)? s\\]" "${test_log}" | tail -n 1 || true)"
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

extract_stat_count() {
  local stats_line="$1"
  echo "${stats_line}" | sed -nE 's/.*n=([0-9]+).*/\1/p'
}

json_number_or_null() {
  local value="$1"
  if [[ -z "${value}" ]]; then
    echo "null"
  else
    echo "${value}"
  fi
}

search_lines() {
  local pattern="$1"
  local source_file="$2"

  if [[ "${search_tool}" == "rg" ]]; then
    rg "${pattern}" "${source_file}" || true
    return
  fi

  grep -E "${pattern}" "${source_file}" || true
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
  : > "${mode_dir}/metric-coalesced-render-requests.txt"
  : > "${mode_dir}/metric-scoped-rematerialized-nodes.txt"
  : > "${mode_dir}/metric-full-materializations.txt"
  : > "${mode_dir}/metric-scoped-materializations.txt"
  : > "${mode_dir}/metric-entry-endtoend-ms.txt"
  : > "${mode_dir}/metric-entry-set-ms.txt"
  : > "${mode_dir}/metric-entry-input-events-received.txt"
  : > "${mode_dir}/metric-entry-input-flush-applies.txt"
  : > "${mode_dir}/metric-entry-input-events-coalesced.txt"
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
          --configuration "${matrix_configuration}" \
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
        "\\[Mirage.Diagnostics\\]\\[coordinator.render\\].*coalescedRequests=" \
        's/.*coalescedRequests=([0-9.]+).*/\1/' \
        "${mode_dir}/metric-coalesced-render-requests.txt"

      append_metric \
        "${diagnostics_log}" \
        "\\[Mirage.Diagnostics\\]\\[coordinator.render\\].*rematerializedNodes=" \
        's/.*rematerializedNodes=([0-9.]+).*/\1/' \
        "${mode_dir}/metric-scoped-rematerialized-nodes.txt"

      append_metric \
        "${diagnostics_log}" \
        "\\[Mirage.Diagnostics\\]\\[coordinator.render\\].*fullMaterializations=" \
        's/.*fullMaterializations=([0-9.]+).*/\1/' \
        "${mode_dir}/metric-full-materializations.txt"

      append_metric \
        "${diagnostics_log}" \
        "\\[Mirage.Diagnostics\\]\\[coordinator.render\\].*scopedMaterializations=" \
        's/.*scopedMaterializations=([0-9.]+).*/\1/' \
        "${mode_dir}/metric-scoped-materializations.txt"

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
        "\\[Mirage.Diagnostics\\]\\[entry.input\\].*phase=received.*inputEventsReceived=" \
        's/.*inputEventsReceived=([0-9.]+).*/\1/' \
        "${mode_dir}/metric-entry-input-events-received.txt"

      append_metric \
        "${diagnostics_log}" \
        "\\[Mirage.Diagnostics\\]\\[entry.input\\].*phase=virtual-text-set.*inputFlushApplies=" \
        's/.*inputFlushApplies=([0-9.]+).*/\1/' \
        "${mode_dir}/metric-entry-input-flush-applies.txt"

      append_metric \
        "${diagnostics_log}" \
        "\\[Mirage.Diagnostics\\]\\[entry.input\\].*phase=virtual-text-set.*inputEventsCoalesced=" \
        's/.*inputEventsCoalesced=([0-9.]+).*/\1/' \
        "${mode_dir}/metric-entry-input-events-coalesced.txt"

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
  local stats_test_duration stats_nav_total stats_nav_apply stats_nav_ensurehandlers stats_render_total stats_entry_endtoend stats_entry_set stats_history stats_hotreload
  local stats_coalesced stats_scoped_nodes stats_full_materializations stats_scoped_materializations stats_entry_events_received stats_entry_flush_applies stats_entry_events_coalesced
  stats_test_duration="$(compute_stats "test-duration-s" "${mode_dir}/metric-test-duration-s.txt")"
  stats_nav_total="$(compute_stats "nav-total-ms" "${mode_dir}/metric-nav-total-ms.txt")"
  stats_nav_apply="$(compute_stats "nav-apply-total-ms" "${mode_dir}/metric-nav-apply-total-ms.txt")"
  stats_nav_ensurehandlers="$(compute_stats "nav-ensurehandlers-ms" "${mode_dir}/metric-nav-ensurehandlers-ms.txt")"
  stats_render_total="$(compute_stats "render-total-ms" "${mode_dir}/metric-render-total-ms.txt")"
  stats_coalesced="$(compute_stats "coalesced-render-requests" "${mode_dir}/metric-coalesced-render-requests.txt")"
  stats_scoped_nodes="$(compute_stats "scoped-rematerialized-nodes" "${mode_dir}/metric-scoped-rematerialized-nodes.txt")"
  stats_full_materializations="$(compute_stats "full-materializations" "${mode_dir}/metric-full-materializations.txt")"
  stats_scoped_materializations="$(compute_stats "scoped-materializations" "${mode_dir}/metric-scoped-materializations.txt")"
  stats_entry_endtoend="$(compute_stats "entry-endtoend-ms" "${mode_dir}/metric-entry-endtoend-ms.txt")"
  stats_entry_set="$(compute_stats "entry-set-ms" "${mode_dir}/metric-entry-set-ms.txt")"
  stats_entry_events_received="$(compute_stats "entry-events-received" "${mode_dir}/metric-entry-input-events-received.txt")"
  stats_entry_flush_applies="$(compute_stats "entry-flush-applies" "${mode_dir}/metric-entry-input-flush-applies.txt")"
  stats_entry_events_coalesced="$(compute_stats "entry-events-coalesced" "${mode_dir}/metric-entry-input-events-coalesced.txt")"
  stats_history="$(compute_stats "history-interop-ms" "${mode_dir}/metric-history-interop-ms.txt")"
  stats_hotreload="$(compute_stats "hotreload-404-count" "${mode_dir}/metric-hotreload-404-count.txt")"

  echo "${stats_test_duration}"
  echo "${stats_nav_total}"
  echo "${stats_nav_apply}"
  echo "${stats_nav_ensurehandlers}"
  echo "${stats_render_total}"
  echo "${stats_coalesced}"
  echo "${stats_scoped_nodes}"
  echo "${stats_full_materializations}"
  echo "${stats_scoped_materializations}"
  echo "${stats_entry_endtoend}"
  echo "${stats_entry_set}"
  echo "${stats_entry_events_received}"
  echo "${stats_entry_flush_applies}"
  echo "${stats_entry_events_coalesced}"
  echo "${stats_history}"
  echo "${stats_hotreload}"

  local nav_median nav_apply_median nav_apply_p95 nav_ensurehandlers_p95 render_median entry_median entry_set_median history_p95 hotreload_404_sum
  nav_median="$(extract_stat_value "median" "${stats_nav_total}")"
  nav_apply_median="$(extract_stat_value "median" "${stats_nav_apply}")"
  nav_apply_p95="$(extract_stat_value "p95" "${stats_nav_apply}")"
  nav_ensurehandlers_p95="$(extract_stat_value "p95" "${stats_nav_ensurehandlers}")"
  render_median="$(extract_stat_value "median" "${stats_render_total}")"
  entry_median="$(extract_stat_value "median" "${stats_entry_endtoend}")"
  entry_set_median="$(extract_stat_value "median" "${stats_entry_set}")"
  history_p95="$(extract_stat_value "p95" "${stats_history}")"
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

  local summary_path="${mode_dir}/summary.json"
  cat > "${summary_path}" <<EOF
{
  "mode": "${mode_name}",
  "metrics": {
    "navTotalP95Ms": $(json_number_or_null "$(extract_stat_value "p95" "${stats_nav_total}")"),
    "navApplyP95Ms": $(json_number_or_null "${nav_apply_p95}"),
    "entryEndToEndP95Ms": $(json_number_or_null "$(extract_stat_value "p95" "${stats_entry_endtoend}")"),
    "entrySetP95Ms": $(json_number_or_null "$(extract_stat_value "p95" "${stats_entry_set}")"),
    "renderTotalP95Ms": $(json_number_or_null "$(extract_stat_value "p95" "${stats_render_total}")"),
    "coalescedRenderRequestsP95": $(json_number_or_null "$(extract_stat_value "p95" "${stats_coalesced}")"),
    "scopedRematerializedNodesP95": $(json_number_or_null "$(extract_stat_value "p95" "${stats_scoped_nodes}")"),
    "fullMaterializationsMax": $(json_number_or_null "$(extract_stat_value "max" "${stats_full_materializations}")"),
    "scopedMaterializationsMax": $(json_number_or_null "$(extract_stat_value "max" "${stats_scoped_materializations}")"),
    "inputEventsReceivedMax": $(json_number_or_null "$(extract_stat_value "max" "${stats_entry_events_received}")"),
    "inputFlushAppliesMax": $(json_number_or_null "$(extract_stat_value "max" "${stats_entry_flush_applies}")"),
    "inputEventsCoalescedMax": $(json_number_or_null "$(extract_stat_value "max" "${stats_entry_events_coalesced}")"),
    "hotReload404Sum": ${hotreload_404_sum}
  }
}
EOF

  if [[ "${budget_enabled}" -eq 1 && "${mode_name}" == "${budget_mode}" ]]; then
    local -a budget_failures=()
    local budget_report="${mode_dir}/budget-report.txt"
    : > "${budget_report}"

    check_budget_metric() {
      local metric_label="$1"
      local actual_value="$2"
      local base_threshold="$3"
      if [[ -z "${base_threshold}" ]]; then
        return
      fi

      if [[ -z "${actual_value}" ]]; then
        budget_failures+=("${metric_label}: missing metric")
        echo "FAIL ${metric_label}: missing metric" >> "${budget_report}"
        return
      fi

      local allowed_value
      allowed_value="$(awk -v base="${base_threshold}" -v tol="${budget_tolerance_ratio}" 'BEGIN { printf "%.3f", base * (1 + tol) }')"
      if awk -v actual="${actual_value}" -v allowed="${allowed_value}" 'BEGIN { exit !(actual <= allowed) }'; then
        echo "PASS ${metric_label}: actual=${actual_value} allowed=${allowed_value} base=${base_threshold} tolerance=${budget_tolerance_ratio}" >> "${budget_report}"
      else
        budget_failures+=("${metric_label}: actual=${actual_value} allowed=${allowed_value} (base=${base_threshold}, tolerance=${budget_tolerance_ratio})")
        echo "FAIL ${metric_label}: actual=${actual_value} allowed=${allowed_value} base=${base_threshold} tolerance=${budget_tolerance_ratio}" >> "${budget_report}"
      fi
    }

    check_budget_metric "nav-total-ms.p95" "$(extract_stat_value "p95" "${stats_nav_total}")" "${PERF_BUDGET_NAV_TOTAL_P95_MS:-}"
    check_budget_metric "nav-apply-total-ms.p95" "$(extract_stat_value "p95" "${stats_nav_apply}")" "${PERF_BUDGET_NAV_APPLY_P95_MS:-}"
    check_budget_metric "entry-endtoend-ms.p95" "$(extract_stat_value "p95" "${stats_entry_endtoend}")" "${PERF_BUDGET_ENTRY_ENDTOEND_P95_MS:-}"
    check_budget_metric "entry-set-ms.p95" "$(extract_stat_value "p95" "${stats_entry_set}")" "${PERF_BUDGET_ENTRY_SET_P95_MS:-}"

    if [[ "${#budget_failures[@]}" -gt 0 ]]; then
      budget_failed=1
      echo
      echo "Budget check (${mode_name}): FAIL"
      printf '%s\n' "${budget_failures[@]}"
    else
      echo
      echo "Budget check (${mode_name}): PASS"
    fi
  fi

  echo
}

run_mode "hotreload-on" "1"
run_mode "hotreload-off" "0"

if [[ -f "${run_root}/hotreload-on/summary.json" && -f "${run_root}/hotreload-off/summary.json" ]]; then
  cat > "${run_root}/summary.json" <<EOF
{
  "generatedAt": "${timestamp}",
  "budgetEnabled": ${budget_enabled},
  "budgetMode": "${budget_mode}",
  "budgetFailed": ${budget_failed},
  "modes": {
    "hotreload-on": $(cat "${run_root}/hotreload-on/summary.json"),
    "hotreload-off": $(cat "${run_root}/hotreload-off/summary.json")
  }
}
EOF
fi

echo "All artifacts: ${run_root}"

if [[ "${budget_failed}" -ne 0 ]]; then
  exit 2
fi

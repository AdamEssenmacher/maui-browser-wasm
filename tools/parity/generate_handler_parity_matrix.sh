#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
default_output_file="$repo_root/docs/HANDLER_PARITY_MATRIX.md"
mirage_hosting_file="$repo_root/src/Mirage.Maui/Hosting/MirageHostingExtensions.cs"
modern_inventory_file="$repo_root/tools/parity/maui-modern-controls.tsv"

maui_repo="dotnet/maui"
maui_baseline_commit="fc1dadc4d68a809a9c14a7d33ff1289037040471"
controls_registration_path="src/Controls/src/Core/Hosting/AppHostBuilderExtensions.cs"
maps_registration_path="src/Controls/Maps/src/AppHostBuilderExtensions.cs"
core_public_api_path="src/Core/src/PublicAPI/net/PublicAPI.Shipped.txt"

mode="write"
output_file="$default_output_file"

usage() {
    cat <<'EOF'
Usage:
  bash tools/parity/generate_handler_parity_matrix.sh
  bash tools/parity/generate_handler_parity_matrix.sh --stdout
  bash tools/parity/generate_handler_parity_matrix.sh --emit-types
  bash tools/parity/generate_handler_parity_matrix.sh --output /abs/path/file.md

Options:
  --stdout      Print generated markdown to stdout.
  --emit-types  Print in-scope MAUI type display names (one per line) and exit.
  --output      Write markdown to explicit path.
EOF
}

while [[ $# -gt 0 ]]; do
    case "$1" in
        --stdout)
            mode="stdout"
            shift
            ;;
        --emit-types)
            mode="emit-types"
            shift
            ;;
        --output)
            if [[ $# -lt 2 ]]; then
                echo "Missing value for --output" >&2
                exit 1
            fi
            output_file="$2"
            mode="write"
            shift 2
            ;;
        -h|--help)
            usage
            exit 0
            ;;
        *)
            echo "Unknown argument: $1" >&2
            usage >&2
            exit 1
            ;;
    esac
done

if ! command -v gh >/dev/null 2>&1; then
    echo "Missing required command: gh" >&2
    exit 1
fi

tmp_dir="$(mktemp -d)"
trap 'rm -rf "$tmp_dir"' EXIT

controls_file="$tmp_dir/controls.cs"
maps_file="$tmp_dir/maps.cs"
core_public_api_file="$tmp_dir/core-publicapi.txt"
registrations_file="$tmp_dir/maui-registrations.tsv"
row_order_file="$tmp_dir/row-order.tsv"
platform_records_file="$tmp_dir/platform-records.tsv"
tree_file="$tmp_dir/maui-tree-paths.txt"
mirage_records_file="$tmp_dir/mirage-records.tsv"
handler_paths_file="$tmp_dir/handler-paths.tsv"
output_tmp="$tmp_dir/handler-parity-matrix.md"
modern_control_types_file="$tmp_dir/modern-control-types.txt"

fetch_maui_file() {
    local maui_path="$1"
    gh api "repos/$maui_repo/contents/$maui_path?ref=$maui_baseline_commit" --jq '.content' | base64 --decode
}

normalize_type_key() {
    local source="$1"
    local raw_type="$2"
    local t="$raw_type"

    t="${t##*::}"
    if [[ "$t" == "MauiMap" ]]; then
        t="Map"
    fi

    if [[ "$source" == "maps" ]]; then
        case "$t" in
            Map) echo "Maps.Map"; return 0 ;;
            Pin) echo "Maps.Pin"; return 0 ;;
            MapElement|Circle|Polygon|Polyline) echo "Maps.MapElement"; return 0 ;;
        esac
    fi

    if [[ "$t" == "Shapes."* ]]; then
        echo "$t"
        return 0
    fi

    if [[ "$t" == *.* ]]; then
        t="${t##*.}"
    fi

    case "$t" in
        IBorderView) t="Border" ;;
        IProgress) t="ProgressBar" ;;
        IStackNavigationView) t="NavigationPage" ;;
        ITabbedView) t="TabbedPage" ;;
        IFlyoutView) t="FlyoutPage" ;;
    esac

    if [[ "$t" =~ ^I[A-Z] ]]; then
        t="${t#I}"
    fi

    echo "$t"
}

type_display_name() {
    local key="$1"
    case "$key" in
        Maps.Map) echo "Map" ;;
        Maps.Pin) echo "Pin" ;;
        Maps.MapElement) echo "MapElement" ;;
        *) echo "$key" ;;
    esac
}

is_in_scope_type_key() {
    local key="$1"
    case "$key" in
        Application) return 1 ;;
        MenuBar|MenuBarItem|MenuFlyout|MenuFlyoutItem|MenuFlyoutSeparator|MenuFlyoutSubItem) return 1 ;;
        ShellItem|ShellSection|ShellContent|SwipeItem|Toolbar) return 1 ;;
        ListView|Cell|ImageCell|EntryCell|TextCell|ViewCell|SwitchCell|TableView) return 1 ;;
    esac

    return 0
}

normalize_handler_name() {
    local raw_handler="$1"
    local h="$raw_handler"
    if [[ "$h" == *.* ]]; then
        h="${h##*.}"
    fi
    echo "$h"
}

evaluate_condition_for_platform() {
    local condition="$1"
    local platform="$2"
    local expr="$condition"

    if [[ -z "$expr" ]]; then
        echo "1"
        return 0
    fi

    local token value
    for token in ANDROID IOS MACCATALYST WINDOWS TIZEN __ANDROID__ __IOS__; do
        value=0
        case "$platform:$token" in
            android:ANDROID|android:__ANDROID__) value=1 ;;
            ios:IOS|ios:MACCATALYST|ios:__IOS__) value=1 ;;
        esac
        expr="$(printf '%s' "$expr" | perl -pe "s/\\b$token\\b/$value/g")"
    done

    expr="$(printf '%s' "$expr" | tr -d ' ')"
    expr="$(printf '%s' "$expr" | perl -pe 's/\b[A-Za-z_][A-Za-z0-9_]*\b/0/g')"

    perl -e '
        my $expr = shift;
        my $value = eval $expr;
        if ($@) { print "0"; exit 0; }
        print($value ? "1" : "0");
    ' "$expr"
}

resolve_handler_paths() {
    local handler="$1"
    local platform="$2"
    local matches
    local base_handler

    if [[ "$platform" == "android" ]]; then
        matches="$(grep -E "/$handler\\.Android\\.cs$|/Android/$handler\\.cs$" "$tree_file" || true)"
    else
        matches="$(grep -E "/$handler\\.iOS\\.cs$|/$handler\\.MaciOS\\.cs$|/iOS/$handler\\.cs$" "$tree_file" || true)"
    fi

    if [[ -z "$matches" && "$handler" =~ ^(.+)[0-9]+$ ]]; then
        base_handler="${BASH_REMATCH[1]}"
        if [[ "$platform" == "android" ]]; then
            matches="$(grep -E "/$base_handler\\.Android\\.cs$|/Android/$base_handler\\.cs$" "$tree_file" || true)"
        else
            matches="$(grep -E "/$base_handler\\.iOS\\.cs$|/$base_handler\\.MaciOS\\.cs$|/iOS/$base_handler\\.cs$" "$tree_file" || true)"
        fi
    fi

    if [[ -z "$matches" ]]; then
        matches="$(grep -E "/$handler\\.cs$" "$tree_file" || true)"
    fi

    if [[ -z "$matches" ]]; then
        echo "unresolved:$handler"
        return 0
    fi

    printf '%s\n' "$matches" | awk '!seen[$0]++'
}

join_markdown_lines() {
    awk '
        BEGIN { first = 1 }
        {
            if (!first) {
                printf "<br>"
            }
            printf "`%s`", $0
            first = 0
        }
        END {
            if (first) {
                printf "none"
            }
        }
    '
}

contains_handler_for_row_platform() {
    local key="$1"
    local platform="$2"
    local handler="$3"
    local count
    count="$(
        awk -F'\t' -v key="$key" -v platform="$platform" -v handler="$handler" '
            $1 == key && $3 == platform && $4 == handler { c++ }
            END { print c + 0 }
        ' "$platform_records_file"
    )"
    [[ "$count" -gt 0 ]]
}

row_key_exists() {
    local key="$1"
    if [[ ! -f "$tmp_dir/rows-seen.tsv" ]]; then
        return 1
    fi

    awk -F'\t' -v key="$key" '
        $1 == key { found = 1; exit }
        END { exit(found ? 0 : 1) }
    ' "$tmp_dir/rows-seen.tsv"
}

is_modern_inventory_type() {
    local key="$1"
    if [[ ! -f "$modern_control_types_file" ]]; then
        return 1
    fi

    grep -Fxq "$key" "$modern_control_types_file"
}

shape_grade_for_type() {
    local key="$1"
    local mirage_cell="$2"

    if [[ "$mirage_cell" == "none" ]]; then
        echo "F"
        return 0
    fi

    case "$key" in
        CollectionView|CarouselView|Label|ScrollView|SearchBar|Layout|Maps.Map|Maps.MapElement|Maps.Pin|IndicatorView|Shapes.Ellipse|Shapes.Line|Shapes.Path|Shapes.Polygon|Shapes.Polyline|Shapes.Rectangle|Shapes.RoundRectangle|RadioButton|Page|ImageButton|ProgressBar)
            echo "A"
            ;;
        ContentView|Button|Window|NavigationPage|Image|Border|Slider|Stepper|Switch|BoxView|ActivityIndicator|Frame)
            echo "B"
            ;;
        Entry|Editor|DatePicker|TimePicker|Picker)
            echo "A"
            ;;
        TabbedPage)
            echo "C"
            ;;
        *)
            echo "B"
            ;;
    esac
}

implementation_grade_for_type() {
    local key="$1"
    local mirage_cell="$2"

    if [[ "$mirage_cell" == "none" ]]; then
        echo "F"
        return 0
    fi

    case "$key" in
        CollectionView) echo "C" ;;
        CarouselView|Label|ScrollView|SearchBar|Layout|Maps.Map|Maps.MapElement|Maps.Pin|Shapes.Ellipse|Shapes.Line|Shapes.Path|Shapes.Polygon|Shapes.Polyline|Shapes.Rectangle|Shapes.RoundRectangle|RadioButton|ImageButton|ProgressBar) echo "B" ;;
        ContentView|Button|Window|NavigationPage|Image|Border|Slider|Stepper|Switch|BoxView|ActivityIndicator|Frame|IndicatorView|Page) echo "C" ;;
        Entry|Editor|DatePicker|TimePicker|Picker) echo "B" ;;
        TabbedPage) echo "D" ;;
        *) echo "C" ;;
    esac
}

notes_for_type() {
    local key="$1"
    local mirage_cell="$2"

    if [[ "$mirage_cell" == "none" ]]; then
        echo "No Mirage handler is registered for this MAUI type."
        return 0
    fi

    case "$key" in
        CollectionView)
            echo "Non-virtualized parity path; grouped/reorder/full anchoring remain partial. See \`docs/PARITY_GAPS.md\` (CollectionView entries)."
            ;;
        CarouselView)
            echo "Dedicated MAUI-shaped ItemsView parity path with snap scrolling, loop/drag/visible-view updates, and linked IndicatorView proof; bounce physics and inherited ItemsUpdatingScrollMode limits remain partial. See \`docs/PARITY_GAPS.md\`."
            ;;
        Label)
            echo "Core mapper + text-source parity implemented; HTML breadth and span hit-region parity remain partial. See \`docs/PARITY_GAPS.md\`."
            ;;
        Entry)
            echo "Mirage-owned input host now covers cursor/selection synchronization, focus command routing, and clear-button semantics; IME composition edge cases remain partial. See \`docs/PARITY_GAPS.md\` (text-input depth)."
            ;;
        Editor)
            echo "Mirage-owned textarea host now covers controls-remap text semantics, cursor/selection synchronization, and completed-on-blur behavior; browser IME edge cases remain partial. See \`docs/PARITY_GAPS.md\` (text-input depth)."
            ;;
        DatePicker)
            echo "Mirage-owned date-input host now covers deterministic nullable date/min-max projection and \`IsOpen\` roundtrip via generic runtime picker-open seam; full custom format token breadth remains partial."
            ;;
        TimePicker)
            echo "Mirage-owned time-input host now covers deterministic \`Time\`/\`Format\` projection and \`IsOpen\` roundtrip via generic runtime picker-open seam; full custom format token breadth remains partial."
            ;;
        ProgressBar)
            echo "Mirage-owned native \`progress\` host now mirrors the MAUI handler surface with deterministic \`Progress\` clamping and \`ProgressColor\` projection; exact track/background styling remains browser-limited."
            ;;
        Picker)
            echo "Mirage-owned select host now mirrors MAUI Picker handler shape with placeholder/title-color behavior, MAUI-authored \`ItemsSource\`/\`SelectedItem\` refresh, and \`IsOpen\` request roundtrip via the generic picker-open seam; popup visibility and vertical alignment remain browser-limited."
            ;;
        SearchBar)
            echo "SearchBar text-input parity is improved, but icon-channel fidelity and IME edge cases remain partial. See \`docs/PARITY_GAPS.md\` (text-input depth)."
            ;;
        ImageButton)
            echo "Mirage-owned ImageButton now mirrors MAUI handler layering with a provider-managed \`button\` + secondary \`img\`, including async source lifecycle and deterministic pressed/released/clicked synthesis; browser chrome fidelity remains CSS-approximated."
            ;;
        RadioButton)
            echo "Mirage-owned host now covers checked-state sync, content projection, and MAUI group-authoritative selection flow; native radio chrome fidelity remains browser-approximated."
            ;;
        Page)
            echo "Mirage now maps MAUI page-title semantics through a dedicated Page handler with active-runtime ownership; iOS-only PageHandler visuals remain browser-inapplicable."
            ;;
        ScrollView)
            echo "Handler shape and request timing are MAUI-aligned; parity contract enforced in integration tests."
            ;;
        Layout)
            echo "Absolute/flex strategy split is MAUI-aligned; remaining flex depth divergences are documented in \`docs/PARITY_GAPS.md\`."
            ;;
        Maps.Map|Maps.MapElement|Maps.Pin)
            echo "Provider-based Leaflet realization; first-wave map parity only. See map parity contract/divergences under \`docs/parity/\`."
            ;;
        Shapes.Ellipse)
            echo "MAUI-shaped mapper/lifecycle is mirrored; graphics \`PatternPaint\`/\`ImagePaint\` remain deterministic divergence paths."
            ;;
        Shapes.Line)
            echo "MAUI handler overlay and line mapper keys are mirrored; browser SVG projection uses flattened path serialization."
            ;;
        Shapes.Path)
            echo "\`Data\` updates and translate \`RenderTransform\` projection are mapped through MAUI-shaped flow; arc fidelity and non-translate transforms remain deterministic divergence paths."
            ;;
        Shapes.Polygon|Shapes.Polyline)
            echo "MAUI-like \`Points.CollectionChanged\` lifecycle parity is implemented; SVG geometry is projected incrementally without handler churn."
            ;;
        Shapes.Rectangle)
            echo "\`RadiusX\`/\`RadiusY\` overlay mapper is mirrored over shared shape handler projection."
            ;;
        Shapes.RoundRectangle)
            echo "\`CornerRadius\` overlay mapper is mirrored over shared shape handler projection."
            ;;
        IndicatorView)
            echo "Core mapper surface implemented with template hosting; paint/interaction depth remains partial."
            ;;
        TabbedPage|NavigationPage|Window|Frame)
            echo "Navigation/container parity remains browser-tailored; behavior depth is below MAUI native stacks."
            ;;
        Image)
            echo "Core projection exists; full image source service parity (stream/decode pipeline depth) remains partial."
            ;;
        Border)
            echo "Border shape/stroke support exists; non-solid paints and advanced stroke parity remain partial."
            ;;
        *)
            echo "Handler exists, but parity depth is still below MAUI native implementations."
            ;;
    esac
}

material3_annotation() {
    local key="$1"
    local platform="$2"
    local handler="$3"

    if [[ "$platform" != "android" ]]; then
        echo ""
        return 0
    fi

    case "$key:$handler" in
        Label:LabelHandler2|Editor:EditorHandler2|Picker:PickerHandler2|RadioButton:RadioButtonHandler2|TimePicker:TimePickerHandler2)
            echo " [if RuntimeFeature.IsMaterial3Enabled]"
            ;;
        Label:LabelHandler|Editor:EditorHandler|Picker:PickerHandler|RadioButton:RadioButtonHandler|TimePicker:TimePickerHandler)
            if contains_handler_for_row_platform "$key" "$platform" "${handler}2"; then
                echo " [else]"
            else
                echo ""
            fi
            ;;
        *)
            echo ""
            ;;
    esac
}

fetch_maui_file "$controls_registration_path" > "$controls_file"
fetch_maui_file "$maps_registration_path" > "$maps_file"
fetch_maui_file "$core_public_api_path" > "$core_public_api_file"
gh api "repos/$maui_repo/git/trees/$maui_baseline_commit?recursive=1" --jq '.tree[] | select(.path != null) | .path' > "$tree_file"

if [[ -f "$modern_inventory_file" ]]; then
    awk -F'\t' '
        NF >= 1 && $1 !~ /^#/ && $1 != "" { print $1 }
    ' "$modern_inventory_file" | awk '!seen[$0]++' > "$modern_control_types_file"
fi

parse_registrations() {
    local source="$1"
    local file="$2"
    awk -v source="$source" '
        function trim(s) {
            gsub(/^[ \t]+/, "", s)
            gsub(/[ \t]+$/, "", s)
            return s
        }
        function current_condition(    i, c) {
            c = ""
            for (i = 1; i <= depth; i++) {
                if (branch[i] != "") {
                    if (c == "") {
                        c = "(" branch[i] ")"
                    } else {
                        c = "(" c ")&&(" branch[i] ")"
                    }
                }
            }
            return c
        }
        {
            line = $0

            if (line ~ /^[ \t]*#if[ \t]+/) {
                expr = line
                sub(/^[ \t]*#if[ \t]+/, "", expr)
                expr = trim(expr)
                depth++
                prior[depth] = expr
                branch[depth] = expr
                next
            }

            if (line ~ /^[ \t]*#elif[ \t]+/) {
                expr = line
                sub(/^[ \t]*#elif[ \t]+/, "", expr)
                expr = trim(expr)
                branch[depth] = "!(" prior[depth] ")&&(" expr ")"
                prior[depth] = "(" prior[depth] ")||(" expr ")"
                next
            }

            if (line ~ /^[ \t]*#else([ \t]|$)/) {
                branch[depth] = "!(" prior[depth] ")"
                prior[depth] = "1"
                next
            }

            if (line ~ /^[ \t]*#endif([ \t]|$)/) {
                delete prior[depth]
                delete branch[depth]
                depth--
                next
            }

            cond = current_condition()

            if (line ~ /AddHandler<[^>]+>/) {
                pair = line
                sub(/^.*AddHandler</, "", pair)
                sub(/>.*/, "", pair)

                type = pair
                sub(/,.*/, "", type)
                type = trim(type)

                handler = pair
                sub(/^[^,]*,/, "", handler)
                handler = trim(handler)

                print source "\t" type "\t" handler "\t" cond
                next
            }

            if (line ~ /AddHandler\(typeof\([^)]+\),[ \t]*typeof\([^)]+\)\)/) {
                type = line
                sub(/^.*AddHandler\(typeof\(/, "", type)
                sub(/\).*/, "", type)
                type = trim(type)

                handler = line
                sub(/^.*AddHandler\(typeof\([^)]+\),[ \t]*typeof\(/, "", handler)
                sub(/\).*/, "", handler)
                handler = trim(handler)

                print source "\t" type "\t" handler "\t" cond
                next
            }
        }
    ' "$file"
}

parse_public_api_mappers() {
    local source="$1"
    local file="$2"
    perl -ne '
        if (/^static Microsoft\.Maui\.Handlers\.([A-Za-z0-9_]+)\.Mapper -> Microsoft\.Maui\.IPropertyMapper<Microsoft\.Maui\.([A-Za-z0-9_.]+)!/) {
            print "'"$source"'\t$2\t$1\t\n";
        }
    ' "$file"
}

{
    parse_registrations "controls" "$controls_file"
    parse_registrations "maps" "$maps_file"
    parse_public_api_mappers "publicapi" "$core_public_api_file"
} > "$registrations_file"

awk -F'\t' '
    !seen[$1 "|" $2 "|" $3 "|" $4]++
' "$registrations_file" > "$tmp_dir/registrations-deduped.tsv"
mv "$tmp_dir/registrations-deduped.tsv" "$registrations_file"

while IFS=$'\t' read -r source raw_type raw_handler condition; do
    key="$(normalize_type_key "$source" "$raw_type")"
    if ! is_in_scope_type_key "$key"; then
        continue
    fi

    display="$(type_display_name "$key")"
    handler="$(normalize_handler_name "$raw_handler")"

    if [[ "$source" == "publicapi" ]]; then
        # Public API mapper extraction is a supplement source:
        # only add missing control rows, and only for controls in the local modern-control inventory.
        if ! is_modern_inventory_type "$display"; then
            continue
        fi
        if row_key_exists "$key"; then
            continue
        fi
    fi

    if [[ "$mode" == "emit-types" ]]; then
        printf '%s\t%s\n' "$key" "$display" >> "$tmp_dir/rows-seen.tsv"
        continue
    fi

    printf '%s\t%s\n' "$key" "$display" >> "$tmp_dir/rows-seen.tsv"

    for platform in android ios; do
        allowed="$(evaluate_condition_for_platform "$condition" "$platform")"
        if [[ "$allowed" == "1" ]]; then
            printf '%s\t%s\t%s\t%s\t%s\n' "$key" "$display" "$platform" "$handler" "$condition" >> "$platform_records_file"
        fi
    done
done < "$registrations_file"

awk -F'\t' '!seen[$1]++ { print $1 "\t" $2 }' "$tmp_dir/rows-seen.tsv" > "$row_order_file"

if [[ "$mode" == "emit-types" ]]; then
    cut -f2 "$row_order_file"
    exit 0
fi

if [[ -f "$platform_records_file" ]]; then
    awk -F'\t' '!seen[$1 "|" $2 "|" $3 "|" $4]++' "$platform_records_file" > "$tmp_dir/platform-records-deduped.tsv"
    mv "$tmp_dir/platform-records-deduped.tsv" "$platform_records_file"
fi

awk '
    /AddHandler\(typeof\([^)]+\),[ \t]*typeof\([^)]+\)\)/ {
        type = $0
        sub(/^.*AddHandler\(typeof\(/, "", type)
        sub(/\).*/, "", type)

        handler = $0
        sub(/^.*AddHandler\(typeof\([^)]+\),[ \t]*typeof\(/, "", handler)
        sub(/\).*/, "", handler)

        printf "%s\t%s\n", type, handler
    }
' "$mirage_hosting_file" | while IFS=$'\t' read -r raw_type raw_handler; do
    normalized_type="$raw_type"
    if [[ "$normalized_type" == "MauiShapes."* ]]; then
        normalized_type="Shapes.${normalized_type#MauiShapes.}"
    elif [[ "$normalized_type" == *".Shapes."* ]]; then
        normalized_type="Shapes.${normalized_type##*.Shapes.}"
    fi

    case "$normalized_type" in
        MauiMap|Map)
            key="Maps.Map"
            ;;
        Pin)
            key="Maps.Pin"
            ;;
        MapElement|Circle|Polygon|Polyline|Maps.MapElement)
            key="Maps.MapElement"
            ;;
        Shapes.*)
            key="$normalized_type"
            ;;
        *)
            key="$normalized_type"
            if [[ "$key" == *.* ]]; then
                key="${key##*.}"
            fi
            ;;
    esac

    handler="$(normalize_handler_name "$raw_handler")"
    printf '%s\t%s\n' "$key" "$handler" >> "$mirage_records_file"
done

if [[ -f "$mirage_records_file" ]]; then
    awk -F'\t' '!seen[$1 "|" $2]++' "$mirage_records_file" > "$tmp_dir/mirage-records-deduped.tsv"
    mv "$tmp_dir/mirage-records-deduped.tsv" "$mirage_records_file"
fi

if [[ -f "$platform_records_file" ]]; then
    cut -f3,4 "$platform_records_file" | awk -F'\t' '!seen[$1 "|" $2]++' | while IFS=$'\t' read -r platform handler; do
        resolve_handler_paths "$handler" "$platform" | join_markdown_lines | awk -v p="$platform" -v h="$handler" '{ print p "\t" h "\t" $0 }' >> "$handler_paths_file"
    done
fi

if [[ -f "$handler_paths_file" ]]; then
    awk -F'\t' '!seen[$1 "|" $2]++' "$handler_paths_file" > "$tmp_dir/handler-paths-deduped.tsv"
    mv "$tmp_dir/handler-paths-deduped.tsv" "$handler_paths_file"
fi

format_platform_cell() {
    local key="$1"
    local platform="$2"
    local display="$3"
    local handlers
    local handler
    local condition
    local path_cell
    local annotation
    local line

    handlers="$(
        awk -F'\t' -v key="$key" -v platform="$platform" '
            $1 == key && $3 == platform { print $4 }
        ' "$platform_records_file" 2>/dev/null | awk '!seen[$0]++'
    )"

    if [[ -z "$handlers" ]]; then
        echo "none"
        return 0
    fi

    line=""
    while IFS= read -r handler; do
        [[ -z "$handler" ]] && continue
        condition="$(
            awk -F'\t' -v key="$key" -v platform="$platform" -v handler="$handler" '
                $1 == key && $3 == platform && $4 == handler { print $5; exit }
            ' "$platform_records_file"
        )"
        path_cell="$(
            awk -F'\t' -v platform="$platform" -v handler="$handler" '
                $1 == platform && $2 == handler { print $3; exit }
            ' "$handler_paths_file"
        )"
        if [[ -z "$path_cell" ]]; then
            path_cell='`unresolved:'"$handler"'`'
        fi

        annotation="$(material3_annotation "$key" "$platform" "$handler")"
        if [[ -z "$annotation" && -n "$condition" && "$condition" != "()" ]]; then
            # Add a light condition hint only when multiple handlers exist on the same platform row.
            if awk -F'\t' -v key="$key" -v platform="$platform" '
                $1 == key && $3 == platform { c++ }
                END { exit(c > 1 ? 0 : 1) }
            ' "$platform_records_file"; then
                annotation=' [conditional]'
            fi
        fi

        if [[ -z "$line" ]]; then
            line='`'"$handler"'`'"$annotation"' -> '"$path_cell"
        else
            line="$line"'<'br'>'"\`$handler\`$annotation -> $path_cell"
        fi
    done <<< "$handlers"

    echo "$line"
}

format_mirage_cell() {
    local key="$1"
    local handlers
    local handler
    local path
    local line

    handlers="$(
        awk -F'\t' -v key="$key" '
            $1 == key { print $2 }
        ' "$mirage_records_file" 2>/dev/null | awk '!seen[$0]++'
    )"

    if [[ -z "$handlers" ]]; then
        echo "none"
        return 0
    fi

    line=""
    while IFS= read -r handler; do
        [[ -z "$handler" ]] && continue
        path="$repo_root/src/Mirage.Maui/Handlers/$handler.cs"
        if [[ ! -f "$path" ]]; then
            path="unresolved:$handler"
        else
            path="${path#$repo_root/}"
        fi

        if [[ -z "$line" ]]; then
            line='`'"$handler"'` -> `'"$path"'`'
        else
            line="$line"'<'br'>'"\`$handler\` -> \`$path\`"
        fi
    done <<< "$handlers"

    echo "$line"
}

{
    cat <<EOF
# MAUI Handler Parity Matrix (Official Upstream-Sourced)

This matrix is generated from official MAUI registration sources at commit \`$maui_baseline_commit\`:
- \`$controls_registration_path\` (\`AddControlsHandlers\`)
- \`$maps_registration_path\` (\`AddMauiMaps\`)

Scope rules:
- Includes registered controls/layouts/views/pages/shapes/maps from those two sources.
- Excludes legacy compatibility-only rows (\`ListView\`, \`Cell*\`, \`TableView\`) and non-view element rows (menu/flyout item element types).
- Keeps \`Frame\` in scope.

Grading rubric (manual expert judgment):
- \`A\`: closely mirrors MAUI handler shape/flow (or behavior intent for implementation grade)
- \`B\`: good alignment with limited divergence
- \`C\`: meaningful divergence or partial behavior depth
- \`D\`: major divergence/limited parity
- \`F\`: no Mirage handler coverage for this MAUI type (locked default)

| MAUI Type | Android Handler(s) (official) | iOS Handler(s) (official) | Mirage Handler(s) | Shape Grade | Implementation Grade | Divergence Notes |
| --- | --- | --- | --- | --- | --- | --- |
EOF

    while IFS=$'\t' read -r key display; do
        android_cell="$(format_platform_cell "$key" "android" "$display")"
        ios_cell="$(format_platform_cell "$key" "ios" "$display")"
        mirage_cell="$(format_mirage_cell "$key")"
        shape_grade="$(shape_grade_for_type "$key" "$mirage_cell")"
        implementation_grade="$(implementation_grade_for_type "$key" "$mirage_cell")"
        notes="$(notes_for_type "$key" "$mirage_cell")"

        printf '| `%s` | %s | %s | %s | %s | %s | %s |\n' \
            "$display" \
            "$android_cell" \
            "$ios_cell" \
            "$mirage_cell" \
            "$shape_grade" \
            "$implementation_grade" \
            "$notes"
    done < "$row_order_file"
} > "$output_tmp"

case "$mode" in
    stdout)
        cat "$output_tmp"
        ;;
    write)
        cp "$output_tmp" "$output_file"
        echo "Wrote $output_file"
        ;;
    *)
        echo "Unsupported mode: $mode" >&2
        exit 1
        ;;
esac

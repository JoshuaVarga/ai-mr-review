#!/usr/bin/env bash
set -eo pipefail

SCRIPT_DIR=$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)
cd "$SCRIPT_DIR"

TARGET="${1:-}"
VERSION="${2:-0.0.0.0}"
FILE_VERSION="${3:-$VERSION}"

if [[ "$VERSION" == .* ]]; then VERSION="0$VERSION"; fi

CYAN='\033[0;36m'
RED='\033[0;31m'
NC='\033[0m'

print_banner() {
  echo ""

  while IFS= read -r line; do
    echo -e "${CYAN}${line}${NC}"
  done <<'EOF'
  __  __ _                             _
 |  \/  (_)                           (_)
 | \  / |_  ___ _ __ _____      ____ _ ___   _____
 | |\/| | |/ __| '__/ _ \ \ /\ / / _` | \ \ / / _ \
 | |  | | | (__| | | (_) \ V  V / (_| | |\ V /  __/
 |_|  |_|_|\___|_|  \___/ \_/\_/ \__,_|_| \_/ \___|
EOF

  echo ""
  echo -e "${CYAN}                 ~~ Build and test without Nuke ~~ ${NC}"
  echo ""
  echo -e "${CYAN}  Timestamp      : $(date '+%Y-%m-%d %H:%M:%S')${NC}"
  echo -e "${CYAN}  Bash           : $BASH_VERSION${NC}"
  echo -e "${CYAN}  Git            : $(git --version)${NC}"
  echo -e "${CYAN}  .NET SDK       : $(dotnet --version)${NC}"
  echo -e "${CYAN}  Version        : $VERSION${NC}"
  echo -e "${CYAN}  File Version   : $FILE_VERSION${NC}"
  echo ""
}

clean() {
  echo "Cleaning bin/obj folders and build-artifacts..."
  find src tests -type d \( -name bin -o -name obj \) -exec rm -rf {} + 2>/dev/null || true
  rm -rf build-artifacts
  mkdir -p build-artifacts
  echo "Cleaned bin/obj folders and build-artifacts!"
}

restore() {
  echo "Restoring .NET solution..."
  dotnet restore TodoApp.slnx
  echo "Restored .NET solution!"
}

check() {
  echo "Checking NuGet packages for vulnerabilities..."
  dotnet list TodoApp.slnx package --vulnerable
  echo "Checked NuGet for vulnerabilities"
}

build_solution() {
  echo "Building solution in Release mode..."
  dotnet build TodoApp.slnx --configuration Release
  echo "Solution built!"
}

generate_semver_file() {
  echo "Generating Directory.Build.props with Version=$VERSION, FileVersion=$FILE_VERSION"
  cat >Directory.Build.props <<EOF
<Project xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
  <PropertyGroup>
    <Product>TodoApp</Product>
    <Description>Avalonia + ReactiveUI Todo App</Description>
    <Version>$VERSION</Version>
    <FileVersion>$FILE_VERSION</FileVersion>
  </PropertyGroup>
</Project>
EOF
  echo "Wrote version info to Directory.Build.props!"
}

build_test_projects() {
  while IFS= read -r -d '' proj; do
    echo "Building test project $proj ..."
    dotnet build "$proj"
  done < <(find tests -name "*.Tests.csproj" -print0)
}

test_solution() {
  echo "Running tests and collecting results..."
  local test_results_dir="build-artifacts/test-results"
  mkdir -p "$test_results_dir"

  local failed=()
  while IFS= read -r -d '' proj; do
    local proj_name
    proj_name=$(basename "$proj" .csproj)
    echo "Testing project $proj_name ..."

    if ! dotnet test "$proj" \
      --logger "trx" \
      --no-build \
      --results-directory "$test_results_dir" \
      --filter "TestCategory!=Interactive"; then
      failed+=("$proj_name")
    fi
  done < <(find tests -name "*.Tests.csproj" -print0)

  if [[ ${#failed[@]} -gt 0 ]]; then
    echo -e "${RED}Failing tests in: ${failed[*]}${NC}" >&2
    exit 1
  fi

  echo "All tests passed successfully!"
}

run_app() {
  echo "Running app..."
  dotnet run --project src/TodoApp.App/TodoApp.App.csproj
}

publish_app() {
  echo "Publishing app..."
  dotnet publish src/TodoApp.App/TodoApp.App.csproj \
    -c Release \
    -o "build-artifacts/output"
  echo "Published to build-artifacts/output!"
}

case "$TARGET" in
CompileAndPublish)
  print_banner
  clean
  generate_semver_file
  build_solution
  publish_app
  ;;
UnitTest)
  print_banner
  clean
  restore
  generate_semver_file
  build_test_projects
  test_solution
  ;;
Run)
  print_banner
  run_app
  ;;
Check)
  print_banner
  restore
  check
  ;;
*)
  echo -e "${RED}Usage: $0 <Check|UnitTest|CompileAndPublish|Run> [version] [fileVersion]${NC}" >&2
  exit 1
  ;;
esac

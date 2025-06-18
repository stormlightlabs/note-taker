# Build Automation

This project includes several automation options for building the NoteTaker application:

## Quick Build Options

### 1. Shell Script (macOS/Linux)

```bash
# Build for current platform (macOS)
./build.sh

# Build for specific platform
./build.sh osx-x64 Release
./build.sh osx-arm64 Release
./build.sh win-x64 Release
./build.sh linux-x64 Release
```

### 2. PowerShell Script (Windows)

```powershell
# Build for Windows
.\build.ps1

# Build with specific parameters
.\build.ps1 -Platform "win-x64" -Configuration "Release"
```

### 3. Makefile

```bash
# Build for macOS (default)
make

# Build for specific platforms
make build-macos-x64
make build-macos-arm64
make build-windows
make build-linux

# Build for all platforms
make build-all

# Other useful commands
make clean      # Clean all builds
make dev        # Build debug version
make run        # Run the application
make help       # Show all available targets
```

### 4. Manual dotnet commands

```bash
# Restore dependencies
dotnet restore NoteTaker.GUI/NoteTaker.GUI.fsproj

# Build for macOS with app bundle
dotnet publish NoteTaker.GUI/NoteTaker.GUI.fsproj \
    -c Release \
    -r osx-x64 \
    --self-contained true \
    -p:PublishSingleFile=true \
    -p:IncludeNativeLibrariesForSelfExtract=true \
    -o dist/osx-x64
```

## Continuous Integration

The project includes a GitHub Actions workflow (`.github/workflows/build.yml`) that:

- Builds for all platforms (Windows, macOS, Linux)
- Runs tests
- Creates app bundles for macOS
- Uploads build artifacts
- Creates releases for tagged versions

## Output Structure

After building, you'll find the outputs in the `dist/` directory:

```sh
dist/
├── osx-x64/
│   └── Stormlight Note Taker.app/  # macOS app bundle
├── osx-arm64/
│   └── Stormlight Note Taker.app/  # macOS app bundle (ARM64)
├── win-x64/
│   └── NoteTaker.GUI.exe           # Windows executable
└── linux-x64/
    └── NoteTaker.GUI               # Linux executable
```

## IDE Integration

VS Code tasks are configured in `.vscode/tasks.json` for building directly from the
editor.

## Configuration

Build settings can be customized in `build.json` for consistent configuration across all
build methods.

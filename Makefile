# NoteTaker Makefile

PROJECT_DIR := NoteTaker.GUI
OUTPUT_DIR := dist
CONFIGURATION := Release

# Default target
.PHONY: all
all: build-macos

.PHONY: clean
clean:
	@echo "Cleaning all builds..."
	rm -rf $(OUTPUT_DIR)
	dotnet clean $(PROJECT_DIR)/NoteTaker.GUI.fsproj

.PHONY: restore
restore:
	@echo "Restoring dependencies..."
	dotnet restore $(PROJECT_DIR)/NoteTaker.GUI.fsproj

.PHONY: build-macos-x64
build-macos-x64: restore
	@echo "Building for macOS x64..."
	dotnet publish $(PROJECT_DIR)/NoteTaker.GUI.fsproj \
		-c $(CONFIGURATION) \
		-r osx-x64 \
		--self-contained true \
		-p:PublishSingleFile=true \
		-p:IncludeNativeLibrariesForSelfExtract=true \
		-o $(OUTPUT_DIR)/osx-x64
	@$(MAKE) create-macos-bundle PLATFORM=osx-x64

.PHONY: build-macos-arm64
build-macos-arm64: restore
	@echo "Building for macOS ARM64..."
	dotnet publish $(PROJECT_DIR)/NoteTaker.GUI.fsproj \
		-c $(CONFIGURATION) \
		-r osx-arm64 \
		--self-contained true \
		-p:PublishSingleFile=true \
		-p:IncludeNativeLibrariesForSelfExtract=true \
		-o $(OUTPUT_DIR)/osx-arm64
	@$(MAKE) create-macos-bundle PLATFORM=osx-arm64

.PHONY: build-windows
build-windows: restore
	@echo "Building for Windows x64..."
	dotnet publish $(PROJECT_DIR)/NoteTaker.GUI.fsproj \
		-c $(CONFIGURATION) \
		-r win-x64 \
		--self-contained true \
		-p:PublishSingleFile=true \
		-p:IncludeNativeLibrariesForSelfExtract=true \
		-o $(OUTPUT_DIR)/win-x64

.PHONY: build-linux
build-linux: restore
	@echo "Building for Linux x64..."
	dotnet publish $(PROJECT_DIR)/NoteTaker.GUI.fsproj \
		-c $(CONFIGURATION) \
		-r linux-x64 \
		--self-contained true \
		-p:PublishSingleFile=true \
		-p:IncludeNativeLibrariesForSelfExtract=true \
		-o $(OUTPUT_DIR)/linux-x64

.PHONY: build-all
build-all: build-macos-x64 build-macos-arm64 build-windows build-linux

.PHONY: create-macos-bundle
create-macos-bundle:
	@echo "Creating macOS app bundle for $(PLATFORM)..."
	@APP_NAME="Stormlight Note Taker"; \
	APP_DIR="$(OUTPUT_DIR)/$(PLATFORM)/$$APP_NAME.app"; \
	mkdir -p "$$APP_DIR/Contents/MacOS"; \
	mkdir -p "$$APP_DIR/Contents/Resources"; \
	cp "$(OUTPUT_DIR)/$(PLATFORM)/NoteTaker.GUI" "$$APP_DIR/Contents/MacOS/"; \
	cp "$(PROJECT_DIR)/Icons/App/osx.icns" "$$APP_DIR/Contents/Resources/"; \
	cp "$(PROJECT_DIR)/Info.plist" "$$APP_DIR/Contents/"; \
	chmod +x "$$APP_DIR/Contents/MacOS/NoteTaker.GUI"; \
	echo "macOS app bundle created at: $$APP_DIR"

.PHONY: macos build-macos
macos build-macos: build-macos-x64

.PHONY: windows
windows: build-windows

.PHONY: linux
linux: build-linux

.PHONY: dev
dev:
	@echo "Building development version..."
	dotnet build $(PROJECT_DIR)/NoteTaker.GUI.fsproj -c Debug

.PHONY: run
run:
	@echo "Running application..."
	dotnet run --project $(PROJECT_DIR)/NoteTaker.GUI.fsproj

.PHONY: help
help:
	@echo "Available targets:"
	@echo "  all              - Build for macOS x64 (default)"
	@echo "  clean            - Clean all builds"
	@echo "  restore          - Restore NuGet packages"
	@echo "  build-macos-x64  - Build for macOS x64"
	@echo "  build-macos-arm64- Build for macOS ARM64"
	@echo "  build-windows    - Build for Windows x64"
	@echo "  build-linux      - Build for Linux x64"
	@echo "  build-all        - Build for all platforms"
	@echo "  dev              - Build debug version"
	@echo "  run              - Run the application"
	@echo "  help             - Show this help"

#!/bin/bash

# NoteTaker Build Script
# Usage: ./build.sh [platform] [configuration]
# Platforms: win-x64, osx-x64, osx-arm64, linux-x64
# Configurations: Debug, Release

set -e

PLATFORM=${1:-osx-x64}
CONFIGURATION=${2:-Release}
PROJECT_DIR="NoteTaker.GUI"
OUTPUT_DIR="dist"

echo "Building NoteTaker for $PLATFORM in $CONFIGURATION configuration..."

# Clean previous builds
echo "Cleaning previous builds..."
rm -rf "${OUTPUT_DIR:?}/${PLATFORM:?}"
dotnet clean "${PROJECT_DIR:?}/NoteTaker.GUI.fsproj" -c "${CONFIGURATION:?}"

# Restore dependencies
echo "Restoring dependencies..."
dotnet restore $PROJECT_DIR/NoteTaker.GUI.fsproj

# Build and publish
echo "Publishing application..."
dotnet publish $PROJECT_DIR/NoteTaker.GUI.fsproj \
    -c $CONFIGURATION \
    -r $PLATFORM \
    --self-contained true \
    -p:PublishSingleFile=true \
    -p:IncludeNativeLibrariesForSelfExtract=true \
    -o $OUTPUT_DIR/$PLATFORM

# Create macOS app bundle if building for macOS
if [[ $PLATFORM == osx-* ]]; then
    echo "Creating macOS app bundle..."
    APP_NAME="Stormlight Note Taker"
    APP_DIR="$OUTPUT_DIR/$PLATFORM/$APP_NAME.app"

    # Create app bundle structure
    mkdir -p "$APP_DIR/Contents/MacOS"
    mkdir -p "$APP_DIR/Contents/Resources"

    # Copy executable
    cp "$OUTPUT_DIR/$PLATFORM/NoteTaker.GUI" "$APP_DIR/Contents/MacOS/"

    # Copy icon
    cp "$PROJECT_DIR/Icons/App/osx.icns" "$APP_DIR/Contents/Resources/"

    # Copy Info.plist
    cp "$PROJECT_DIR/Info.plist" "$APP_DIR/Contents/"

    # Make executable
    chmod +x "$APP_DIR/Contents/MacOS/NoteTaker.GUI"

    echo "macOS app bundle created at: $APP_DIR"
fi

echo "Build completed successfully!"
echo "Output directory: $OUTPUT_DIR/$PLATFORM"

#!/bin/bash

# SquidVox Deploy Script
# This script publishes the SquidVox application and optionally runs it
#
# Usage:
#   ./deploy.sh              # Publish and run
#   ./deploy.sh --publish    # Publish only
#   ./deploy.sh --run        # Run previously published app
#   ./deploy.sh --clean      # Clean publish directory

set -e  # Exit on any error

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Function to print colored output
print_info() {
    echo -e "${BLUE}ℹ️  $1${NC}"
}

print_success() {
    echo -e "${GREEN}✅ $1${NC}"
}

print_warning() {
    echo -e "${YELLOW}⚠️  $1${NC}"
}

print_error() {
    echo -e "${RED}❌ $1${NC}"
}

# Get the directory where this script is located
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(dirname "$SCRIPT_DIR")"

# Change to project root directory
cd "$PROJECT_ROOT"

print_info "Working directory: $(pwd)"

# Set publish configuration
PUBLISH_CONFIG="Release"
PUBLISH_DIR="publish"

# Parse command line arguments
ACTION="all"  # Default: publish and run
if [ $# -gt 0 ]; then
    case "$1" in
        --publish)
            ACTION="publish"
            ;;
        --run)
            ACTION="run"
            ;;
        --clean)
            ACTION="clean"
            ;;
        --help|-h)
            echo "SquidVox Deploy Script"
            echo ""
            echo "Usage:"
            echo "  ./deploy.sh              # Publish and run (default)"
            echo "  ./deploy.sh --publish    # Publish only"
            echo "  ./deploy.sh --run        # Run previously published app"
            echo "  ./deploy.sh --clean      # Clean publish directory"
            echo "  ./deploy.sh --help       # Show this help"
            exit 0
            ;;
        *)
            print_error "Unknown option: $1"
            echo "Use --help for usage information"
            exit 1
            ;;
    esac
fi

# Clean publish directory
if [ "$ACTION" = "clean" ] || [ "$ACTION" = "all" ]; then
    print_info "Cleaning publish directory..."
    if [ -d "$PUBLISH_DIR" ]; then
        rm -rf "$PUBLISH_DIR"
        print_success "Publish directory cleaned"
    else
        print_info "Publish directory already clean"
    fi

    if [ "$ACTION" = "clean" ]; then
        exit 0
    fi
fi

# Publish the application
if [ "$ACTION" = "publish" ] || [ "$ACTION" = "all" ]; then
    print_info "Publishing SquidVox.World3d in $PUBLISH_CONFIG configuration..."

    # Publish the main application
    if dotnet publish src/SquidVox.World3d/SquidVox.World3d.csproj \
        --configuration $PUBLISH_CONFIG \
        --output "$PUBLISH_DIR" \
        --self-contained true \
        -p:PublishSingleFile=true \
        -p:PublishReadyToRun=false \
        -p:TieredCompilation=false; then

        print_success "Publish completed successfully!"
    else
        print_error "Publish failed!"
        exit 1
    fi

    if [ "$ACTION" = "publish" ]; then
        print_info "Published to: $PUBLISH_DIR"
        exit 0
    fi
fi

# Run the application
if [ "$ACTION" = "run" ] || [ "$ACTION" = "all" ]; then
    # Check if the executable exists
    EXECUTABLE_PATH="$PUBLISH_DIR/SquidVox.World3d"
    if [ ! -f "$EXECUTABLE_PATH" ]; then
        print_error "Executable not found at $EXECUTABLE_PATH"
        print_info "Run './deploy.sh --publish' first to create the executable"
        exit 1
    fi

    print_info "Starting SquidVox..."
    print_info "Executable: $EXECUTABLE_PATH"

    # Make the executable runnable (in case it wasn't)
    chmod +x "$EXECUTABLE_PATH"

    # Run the application
    print_info "SquidVox is running. Press Ctrl+C to stop."
    "$EXECUTABLE_PATH" || print_warning "Application exited with error code $?"
fi

print_info "Done!"
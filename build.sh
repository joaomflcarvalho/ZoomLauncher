#!/bin/bash

# Define names
PROJECT_NAME="ZoomLauncher"
PROJECT_FILE="$PROJECT_NAME.csproj" # <--- TARGET THE PROJECT, NOT THE SOLUTION
OUTPUT_DIR="./dist"
ICON_NAME="ZoomLauncher.icon.png"

echo "🚀 Starting Build Process for $PROJECT_NAME..."

# 1. Safety Check: Look for the Icon
if [ -f "$ICON_NAME" ]; then
    echo "✅ Icon found: $ICON_NAME"
    if grep -q "$ICON_NAME" "$PROJECT_FILE"; then
        echo "✅ .csproj is correctly configured to embed the icon."
    else
        echo "⚠️  WARNING: $ICON_NAME exists but is NOT in your .csproj!"
        echo "   Please add <EmbeddedResource Include=\"$ICON_NAME\" /> to your .csproj file."
        sleep 3
    fi
else
    echo "⚠️  WARNING: No $ICON_NAME found. ODC will show the default icon."
fi

# 2. Clean previous builds
echo "🧹 Cleaning up..."
rm -rf $OUTPUT_DIR
rm -f "$PROJECT_NAME.zip"
dotnet clean "$PROJECT_FILE" # Clean specific project

# 3. Publish (Explicitly targeting the .csproj fixes the warning)
echo "📦 Publishing..."
dotnet publish "$PROJECT_FILE" -c Release -o $OUTPUT_DIR

# 4. Zip the contents
echo "🤐 Zipping..."
cd $OUTPUT_DIR
zip -r ../$PROJECT_NAME.zip ./*
cd ..

# 5. Cleanup
rm -rf $OUTPUT_DIR

echo "✅ DONE!"
echo "📂 Upload this file: $(pwd)/$PROJECT_NAME.zip"
echo "ℹ️  NOTE: You will NOT see the icon image in the zip. It is inside the DLL."
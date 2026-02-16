#!/bin/bash

# Define names
PROJECT_NAME="ZoomLauncher"
OUTPUT_DIR="./dist"

echo "🚀 Starting Build Process..."

# 1. Clean previous builds
echo "🧹 Cleaning up..."
rm -rf $OUTPUT_DIR
rm -f "$PROJECT_NAME.zip"
dotnet clean

# 2. Publish (This creates a clean folder with ALL dependencies)
echo "📦 Publishing..."
dotnet publish -c Release -o $OUTPUT_DIR

# 3. Zip the contents
echo "🤐 Zipping..."
cd $OUTPUT_DIR
# Zip all files inside 'dist' into 'ZoomLauncher.zip' in the parent directory
zip -r ../$PROJECT_NAME.zip ./*
cd ..

# 4. Cleanup
rm -rf $OUTPUT_DIR

echo "✅ DONE!"
echo "📂 Your file is ready here: $(pwd)/$PROJECT_NAME.zip"
echo "⬆️  Upload this zip to ODC."

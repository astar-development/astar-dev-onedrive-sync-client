#!/bin/bash
PART=${1:-patch}
FILE="$(dirname "$0")/src/Directory.Build.props"

VERSION=$(grep -oP '(?<=<Version>)[^<]+' "$FILE")
IFS='.' read -r MAJOR MINOR PATCH <<< "$VERSION"

case $PART in
  major) MAJOR=$((MAJOR+1)); MINOR=0; PATCH=0 ;;
  minor) MINOR=$((MINOR+1)); PATCH=0 ;;
  patch) PATCH=$((PATCH+1)) ;;
esac

NEW="$MAJOR.$MINOR.$PATCH"
sed -i "s|<Version>$VERSION</Version>|<Version>$NEW</Version>|" "$FILE"
echo "Version bumped: $VERSION → $NEW"

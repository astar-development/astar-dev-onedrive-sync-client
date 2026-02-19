#!/usr/bin/env bash
set -euo pipefail

# Detect Ubuntu base codename from Mint
UBUNTU_CODENAME="${UBUNTU_CODENAME:-}"
if [[ -z "$UBUNTU_CODENAME" && -f /etc/os-release ]]; then
  # shellcheck disable=SC1091
  source /etc/os-release
  UBUNTU_CODENAME="${UBUNTU_CODENAME:-}"
fi

if [[ -z "$UBUNTU_CODENAME" ]]; then
  echo "Could not detect Ubuntu base codename. Aborting."
  exit 1
fi

case "$UBUNTU_CODENAME" in
  jammy) UBUNTU_VERSION="22.04" ;; # Mint 21.x
  noble) UBUNTU_VERSION="24.04" ;; # Mint 22.x
  *)
    echo "Unsupported Ubuntu base codename: $UBUNTU_CODENAME"
    echo "This script supports Mint 21.x (jammy) and 22.x (noble)."
    exit 1
    ;;
esac

echo "Detected Ubuntu base: $UBUNTU_CODENAME ($UBUNTU_VERSION)"

sudo apt-get update
sudo apt-get install -y wget

wget "https://packages.microsoft.com/config/ubuntu/${UBUNTU_VERSION}/packages-microsoft-prod.deb" -O /tmp/packages-microsoft-prod.deb
sudo dpkg -i /tmp/packages-microsoft-prod.deb
rm -f /tmp/packages-microsoft-prod.deb

sudo apt-get update
sudo apt-get install -y dotnet-sdk-10.0

dotnet --info
dotnet --list-sdks

#!/usr/bin/env bash
set -euo pipefail

script_dir="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
project_root="$(cd -- "$script_dir/.." && pwd)"

keystore_path="${1:-}"
key_alias="${2:-sesport}"

if [[ -z "$keystore_path" ]]; then
   echo "Usage: $0 <keystore-path> [key-alias]" >&2
   exit 1
fi

read -r -s -p "Keystore password: " store_password
echo
read -r -s -p "Key password: " key_password
echo

if [[ -z "$store_password" || -z "$key_password" ]]; then
   echo "Passwords must not be empty." >&2
   exit 1
fi

mkdir -p "$(dirname -- "$keystore_path")"

keytool -genkeypair \
   -alias "$key_alias" \
   -keyalg RSA \
   -keysize 2048 \
   -validity 10000 \
   -keystore "$keystore_path" \
   -storepass "$store_password" \
   -keypass "$key_password" \
   -dname "CN=SE Sport, OU=Mobile, O=SE Sport, L=Stockholm, S=Stockholm, C=SE"

cat > "$project_root/keystore.properties" <<EOF
storeFile=$keystore_path
storePassword=$store_password
keyAlias=$key_alias
keyPassword=$key_password
EOF

echo "Created:"
echo "  $keystore_path"
echo "  $project_root/keystore.properties"


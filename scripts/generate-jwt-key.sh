#!/usr/bin/env bash
# Generates the RS256 signing keypair for a LOCAL auto-veritas stack.
#
# The private key is the trust root of one authservice instance and nothing else:
# never commit it, never reuse it for a deployed environment (deployed keys are
# generated separately and live only in the platform secret store), never share it
# between consumer systems. Windows users: use scripts/generate-jwt-key.ps1.
set -euo pipefail

out_dir="$(cd "$(dirname "$0")/.." && pwd)/certs"
key_file="$out_dir/jwt-signing.dev.pem"

if [[ -f "$key_file" ]]; then
  echo "Key already exists at $key_file — refusing to overwrite."
  exit 0
fi

mkdir -p "$out_dir"
# PKCS#8 ("BEGIN PRIVATE KEY"), 2048-bit minimum — authservice rejects PKCS#1.
openssl genpkey -algorithm RSA -pkeyopt rsa_keygen_bits:2048 -out "$key_file"
chmod 600 "$key_file"

echo "Wrote $key_file"
echo "docker compose mounts it automatically; for the Aspire AppHost run:"
echo "  dotnet user-secrets set Parameters:jwt-signing-key \"\$(cat $key_file)\" --project src/AutoVeritas.AppHost"

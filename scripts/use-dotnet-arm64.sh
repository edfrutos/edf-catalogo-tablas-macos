#!/usr/bin/env bash
# PATH y DOTNET_ROOT para compilar este repo en Apple Silicon.
# Uso: source scripts/use-dotnet-arm64.sh
#
# Orden: 1) SDK del formula Homebrew (opt/homebrew/opt/dotnet) si existe;
#         2) SDK del cask Microsoft (instala en /usr/local/share/dotnet, binario arm64).

export PATH="/opt/homebrew/bin:${PATH}"

if [[ -x "/opt/homebrew/opt/dotnet/libexec/dotnet" ]]; then
  export DOTNET_ROOT="/opt/homebrew/opt/dotnet/libexec"
  export PATH="${DOTNET_ROOT}:${PATH}"
elif [[ -x "/usr/local/share/dotnet/dotnet" ]]; then
  export DOTNET_ROOT="/usr/local/share/dotnet"
  export PATH="${DOTNET_ROOT}:${PATH}"
fi

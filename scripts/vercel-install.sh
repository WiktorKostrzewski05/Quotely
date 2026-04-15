#!/bin/sh
set -e
mkdir -p .dotnet
curl -sSL https://dot.net/v1/dotnet-install.sh | bash /dev/stdin --channel 8.0 --install-dir .dotnet
export PATH="$PWD/.dotnet:$PATH"
PROJ=$(find . -name 'QuotelyAPP.csproj' -not -path '*/QuotelyAPP.Tests/*' -not -path '*/bin/*' -not -path '*/obj/*' | head -1)
dotnet restore "$PROJ"

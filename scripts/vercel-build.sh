#!/bin/sh
set -e
export PATH="$PWD/.dotnet:$PATH"
PROJ=$(find . -name 'QuotelyAPP.csproj' -not -path '*/QuotelyAPP.Tests/*' -not -path '*/bin/*' -not -path '*/obj/*' | head -1)
dotnet publish "$PROJ" -c Release -o output --no-restore

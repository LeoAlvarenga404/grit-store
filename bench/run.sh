#!/usr/bin/env sh
# Runs the k6 scenarios against the API the AppHost serves. Start it first, once with and once without the cache:
#   dotnet run --project src/Grit.AppHost -- --Seed:VolumeProducts=50000
#   dotnet run --project src/Grit.AppHost -- --Seed:VolumeProducts=50000 --Caching:Enabled=false
# Wait a minute between runs (login is limited to 10 per minute per IP). Usage: bench/run.sh <label>
set -eu

label="${1:?usage: bench/run.sh <label>}"
base_url="${BASE_URL:-http://host.docker.internal:5080}"
admin_password="$(dotnet user-secrets list --project src/Grit.AppHost | sed -n 's/^Parameters:admin-password = //p')"

mkdir -p bench/results
for scenario in catalog-browse checkout; do
  # The script goes in on stdin: no volume mount, so Git Bash never rewrites a path.
  docker run --rm -i -e BASE_URL="$base_url" -e ADMIN_PASSWORD="$admin_password" grafana/k6:1.3.0 run --quiet - \
    < "bench/$scenario.js" | tee "bench/results/$label-$scenario.txt"
done

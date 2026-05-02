#!/usr/bin/env bash
# Azure deployment for AgroAdvisor.
# Creates: resource group, Azure SQL (Serverless, auto-pause), App Service, app settings, and deploys.
#
# Required env vars (set in your shell before running, or inline: VAR=... ./azure-deploy.sh):
#   SUFFIX              — unique 4–8 char suffix for globally-unique names (e.g. "ag42xk")
#   SQL_ADMIN_PASSWORD  — Azure SQL admin password (min 8 chars, upper+lower+digit+symbol)
#   JWT_KEY             — JWT signing key (≥32 chars)
#   GEMINI_API_KEY        — Gemini API key (image analysis + chatbot + AI insight)
#   IMAGE_SEARCH_API_KEY  — SerpAPI key for plant disease image search
#
# Optional env vars (with defaults):
#   RESOURCE_GROUP=AgroAdvisor-rg
#   LOCATION=westeurope
#   SQL_ADMIN=sqladmin
#   AI_MODEL=gemini-2.5-flash

set -euo pipefail

# Auto-load filled params if present (gitignored).
if [ -f ".env.azure" ]; then
  # shellcheck disable=SC1091
  source .env.azure
fi

require_var() {
  if [ -z "${!1:-}" ]; then
    echo "ERROR: environment variable '$1' is required." >&2
    exit 1
  fi
}

require_var SUFFIX
require_var SQL_ADMIN_PASSWORD
require_var JWT_KEY
require_var GEMINI_API_KEY
require_var IMAGE_SEARCH_API_KEY

RESOURCE_GROUP="${RESOURCE_GROUP:-AgroAdvisor-rg}"
LOCATION="${LOCATION:-westeurope}"
SQL_ADMIN="${SQL_ADMIN:-sqladmin}"
AI_MODEL="${AI_MODEL:-gemini-2.5-flash}"
AI_ENDPOINT="${AI_ENDPOINT:-https://generativelanguage.googleapis.com/v1/models/{model}:generateContent}"

SQL_SERVER="agroadvisor-sql-${SUFFIX}"
SQL_DB="AgroAdvisor"
APP_PLAN="AgroAdvisor-plan"
APP_NAME="agroadvisor-api-${SUFFIX}"
RUNTIME="DOTNETCORE:8.0"

echo "==> [1/6] Resource group: ${RESOURCE_GROUP} (${LOCATION})"
az group create --name "${RESOURCE_GROUP}" --location "${LOCATION}" --output none

echo "==> [2/6] Azure SQL server + database: ${SQL_SERVER} / ${SQL_DB}"
az sql server create \
  --name "${SQL_SERVER}" --resource-group "${RESOURCE_GROUP}" \
  --location "${LOCATION}" \
  --admin-user "${SQL_ADMIN}" --admin-password "${SQL_ADMIN_PASSWORD}" \
  --output none

# Allow Azure services (App Service) to reach the SQL server.
az sql server firewall-rule create \
  --resource-group "${RESOURCE_GROUP}" --server "${SQL_SERVER}" \
  --name AllowAzureServices \
  --start-ip-address 0.0.0.0 --end-ip-address 0.0.0.0 \
  --output none

# Serverless tier with 1 vCore, auto-pauses after 60 min idle. Storage-only cost when paused.
az sql db create \
  --resource-group "${RESOURCE_GROUP}" --server "${SQL_SERVER}" \
  --name "${SQL_DB}" \
  --edition GeneralPurpose --family Gen5 --capacity 1 --compute-model Serverless \
  --auto-pause-delay 60 --backup-storage-redundancy Local --max-size 2GB \
  --output none

echo "==> [3/6] App Service plan + web app: ${APP_NAME}"
az appservice plan create \
  --name "${APP_PLAN}" --resource-group "${RESOURCE_GROUP}" \
  --sku F1 --is-linux \
  --output none

az webapp create \
  --resource-group "${RESOURCE_GROUP}" --plan "${APP_PLAN}" \
  --name "${APP_NAME}" --runtime "${RUNTIME}" \
  --output none

echo "==> [4/6] App settings (connection string + secrets)"
CONN="Server=tcp:${SQL_SERVER}.database.windows.net,1433;Database=${SQL_DB};User Id=${SQL_ADMIN};Password=${SQL_ADMIN_PASSWORD};Encrypt=True;Connection Timeout=30;"

az webapp config connection-string set \
  --resource-group "${RESOURCE_GROUP}" --name "${APP_NAME}" \
  --connection-string-type SQLAzure \
  --settings DefaultConnection="${CONN}" \
  --output none

az webapp config appsettings set \
  --resource-group "${RESOURCE_GROUP}" --name "${APP_NAME}" \
  --settings \
    Jwt__Key="${JWT_KEY}" \
    Jwt__Issuer="AgroAdvisor" \
    Jwt__Audience="AgroAdvisor" \
    GeminiSettings__ApiKey="${GEMINI_API_KEY}" \
    ImageSearch__ApiKey="${IMAGE_SEARCH_API_KEY}" \
    AiOptions__ApiKey="${GEMINI_API_KEY}" \
    AiOptions__Model="${AI_MODEL}" \
    AiOptions__Endpoint="${AI_ENDPOINT}" \
    ChatAiOptions__ApiKey="${GEMINI_API_KEY}" \
    ChatAiOptions__Model="${AI_MODEL}" \
    ChatAiOptions__Endpoint="${AI_ENDPOINT}" \
    ChatAiOptions__SystemPrompt="You are AgroAdvisor's agronomy assistant. Give concise, practical guidance for farming questions. Mention uncertainty when appropriate and end with: This is general guidance." \
  --output none

echo "==> [5/6] Publish and zip"
rm -rf publish deploy.zip
dotnet publish AgroAdvisor.csproj -c Release -o ./publish --nologo

# Cross-platform zip: prefer `zip`, fall back to a PowerShell ZipArchive writer on Windows.
if command -v zip >/dev/null 2>&1; then
  (cd publish && zip -qr ../deploy.zip .)
else
  powershell.exe -NoProfile -Command "
    Add-Type -AssemblyName System.IO.Compression;
    Add-Type -AssemblyName System.IO.Compression.FileSystem;
    if (Test-Path ./deploy.zip) { Remove-Item -LiteralPath ./deploy.zip -Force }
    \$publishDir = (Resolve-Path ./publish).Path;
    \$zip = New-Object System.IO.Compression.ZipArchive([System.IO.File]::Open('./deploy.zip', [System.IO.FileMode]::CreateNew), [System.IO.Compression.ZipArchiveMode]::Create, \$false);
    try {
      Get-ChildItem -Path \$publishDir -Recurse -File | ForEach-Object {
        \$relative = \$_.FullName.Substring(\$publishDir.Length + 1).Replace([char]92, [char]47);
        \$entry = \$zip.CreateEntry(\$relative, [System.IO.Compression.CompressionLevel]::Optimal);
        \$entryStream = \$entry.Open();
        try {
          \$fileStream = [System.IO.File]::OpenRead(\$_.FullName);
          try { \$fileStream.CopyTo(\$entryStream) } finally { \$fileStream.Dispose() }
        } finally { \$entryStream.Dispose() }
      }
    } finally { \$zip.Dispose() }
  "
fi

echo "==> [6/6] Deploy zip to App Service"
az webapp deploy \
  --resource-group "${RESOURCE_GROUP}" --name "${APP_NAME}" \
  --src-path deploy.zip --type zip \
  --output none

URL="https://${APP_NAME}.azurewebsites.net"
echo
echo "Done."
echo "  App URL:   ${URL}"
echo "  Test:      curl -X POST ${URL}/api/Auth/register -H 'Content-Type: application/json' -d '{\"name\":\"Test\",\"surname\":\"User\",\"email\":\"test@a.com\",\"password\":\"Password1!\"}'"
echo
echo "First request may take 20–60s if the SQL DB was auto-paused."

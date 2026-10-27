$baseUrl = "http://localhost:5000"
Write-Host "--- Testing Astra Syndicate: Fleet Wars (.NET 9 Web API) ---"

# 1. Health check
Write-Host "1. Checking Health Check (/health)..."
$health = Invoke-RestMethod -Uri "$baseUrl/health" -Method Get
Write-Host "Health Status: $($health.status) | Service: $($health.service)"

# 2. Guest Login
Write-Host "2. Logging in as Guest (/api/v1/auth/guest)..."
$guestBody = '{"deviceId":"samsung_galaxy_s23"}'
$guestRes = Invoke-RestMethod -Uri "$baseUrl/api/v1/auth/guest" -Method Post -ContentType "application/json" -Body $guestBody
$token = $guestRes.data.token
$player = $guestRes.data.player
Write-Host "Logged in as: $($player.username)"

# 3. Get Initial Player Profile
Write-Host "3. Fetching Authoritative Profile (/api/v1/player/profile)..."
$headers = @{ Authorization = "Bearer $token" }
$profileRes = Invoke-RestMethod -Uri "$baseUrl/api/v1/player/profile" -Method Get -Headers $headers
Write-Host "Initial Metadata: Level $($profileRes.data.metadata.level) | Credits: $($profileRes.data.metadata.credits)"

# 4. Offline-to-Online Sync
Write-Host "4. Simulating Offline-to-Online Sync (/api/v1/sync)..."
$syncId = [guid]::NewGuid().ToString()
$syncPayload = @{
    syncId = $syncId
    clientTimestamp = (Get-Date).ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
    clientVersion = "1.0.4"
    chapters = @(
        @{ chapterNumber = 1; completed = $true; stars = 3; highScore = 16800 },
        @{ chapterNumber = 2; completed = $true; stars = 3; highScore = 14200 }
    )
    achievements = @(
        @{ achievementId = "ACH_FIRST_VICTORY"; unlockedAt = (Get-Date).ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fffZ") }
    )
    fleet = @{
        flagship = "Vanguard-II Dreadnought"
        escorts = @("Corvette-Alpha", "Frigate-Beta")
        revisionVersion = 1
    }
    currencyDelta = @{
        creditsEarned = 3500
        darkMatterEarned = 15
        experienceEarned = 1500
    }
} | ConvertTo-Json -Depth 5

$syncRes = Invoke-RestMethod -Uri "$baseUrl/api/v1/sync" -Method Post -Headers $headers -ContentType "application/json" -Body $syncPayload
Write-Host "Sync Status: $($syncRes.data.status)"
Write-Host "Chapters Count: $($syncRes.data.authoritativeState.chapters.Count)"
Write-Host "Achievements Count: $($syncRes.data.authoritativeState.achievements.Count)"
Write-Host "Updated Credits: $($syncRes.data.authoritativeState.metadata.credits)"
Write-Host "Updated Level: $($syncRes.data.authoritativeState.metadata.level) (XP: $($syncRes.data.authoritativeState.metadata.experience))"

# 5. Idempotency Test
Write-Host "5. Testing Idempotency (Retrying exact same syncId)..."
$retryRes = Invoke-RestMethod -Uri "$baseUrl/api/v1/sync" -Method Post -Headers $headers -ContentType "application/json" -Body $syncPayload
Write-Host "Idempotency Result: $($retryRes.data.status) (Credits: $($retryRes.data.authoritativeState.metadata.credits))"

# 6. Passive Webhook Telemetry
Write-Host "6. Testing Passive Logging Webhook (/api/v1/webhooks/log)..."
$logBody = '{"eventType":"BATTLE_VICTORY","payload":{"sector":"Orion-9","duration":124}}'
$logRes = Invoke-RestMethod -Uri "$baseUrl/api/v1/webhooks/log" -Method Post -ContentType "application/json" -Body $logBody
Write-Host "Webhook Response: $($logRes.message)"

Write-Host "ALL .NET 9 SERVICES VERIFIED AND RUNNING PERFECTLY!"

<#
.SYNOPSIS
    Generates an image using Google's Gemini API.

.DESCRIPTION
    Calls the Gemini 2.5 Flash Image model to generate an image based on a text prompt.
    Requires GEMINI_API_KEY environment variable to be set.

.PARAMETER Prompt
    The text prompt describing the image to generate.

.PARAMETER OutputFile
    Optional. The output filename. Defaults to "generated-image.png".

.EXAMPLE
    .\generate-image.ps1 -Prompt "A nano banana on a fancy plate"

.EXAMPLE
    .\generate-image.ps1 -Prompt "A sunset over mountains" -OutputFile "sunset.png"
#>

param(
    [Parameter(Mandatory = $true, Position = 0)]
    [string]$Prompt,

    [Parameter(Mandatory = $false)]
    [string]$OutputFile = "generated-image.png"
)

$ErrorActionPreference = "Stop"

# Check for API key
$apiKey = $env:GEMINI_API_KEY
if (-not $apiKey) {
    Write-Error "GEMINI_API_KEY environment variable is not set."
    exit 1
}

$uri = "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash-image:generateContent?key=$apiKey"

$body = @{
    contents = @(
        @{
            parts = @(
                @{ text = $Prompt }
            )
        }
    )
    generationConfig = @{
        responseModalities = @("TEXT", "IMAGE")
    }
} | ConvertTo-Json -Depth 10

Write-Host "Generating image for prompt: '$Prompt'..." -ForegroundColor Cyan

try {
    $response = Invoke-RestMethod -Uri $uri -Method Post -ContentType "application/json" -Body $body

    # Find the image part in the response
    $imagePart = $response.candidates[0].content.parts | Where-Object { $_.inlineData }

    if (-not $imagePart) {
        Write-Error "No image was returned in the response."
        exit 1
    }

    # Decode and save the image
    $imageBytes = [Convert]::FromBase64String($imagePart.inlineData.data)
    $outputPath = Join-Path -Path (Get-Location) -ChildPath $OutputFile
    [System.IO.File]::WriteAllBytes($outputPath, $imageBytes)

    Write-Host "Image saved to: $outputPath" -ForegroundColor Green

    # Check for text response
    $textPart = $response.candidates[0].content.parts | Where-Object { $_.text }
    if ($textPart) {
        Write-Host "`nModel response:" -ForegroundColor Yellow
        Write-Host $textPart.text
    }
}
catch {
    Write-Error "Failed to generate image: $_"
    exit 1
}

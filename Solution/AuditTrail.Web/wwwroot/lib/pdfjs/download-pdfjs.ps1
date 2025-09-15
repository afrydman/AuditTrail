# PowerShell script to download PDF.js from GitHub
$pdfJsVersion = "3.11.174"  # Latest stable version
$downloadUrl = "https://github.com/mozilla/pdf.js/releases/download/v$pdfJsVersion/pdfjs-$pdfJsVersion-dist.zip"
$tempZip = "pdfjs.zip"
$extractPath = ".\"

Write-Host "Downloading PDF.js v$pdfJsVersion..." -ForegroundColor Green

# Download the zip file
try {
    Invoke-WebRequest -Uri $downloadUrl -OutFile $tempZip
    Write-Host "Download complete." -ForegroundColor Green
    
    # Extract the zip file
    Write-Host "Extracting files..." -ForegroundColor Green
    Expand-Archive -Path $tempZip -DestinationPath $extractPath -Force
    
    # Clean up the zip file
    Remove-Item $tempZip
    
    Write-Host "PDF.js has been successfully downloaded and extracted!" -ForegroundColor Green
    Write-Host "Files are located in: $extractPath" -ForegroundColor Yellow
    
    # List the extracted files
    Write-Host "`nExtracted files:" -ForegroundColor Cyan
    Get-ChildItem -Path $extractPath | Select-Object Name, Length | Format-Table
}
catch {
    Write-Host "Error downloading PDF.js: $_" -ForegroundColor Red
    exit 1
}
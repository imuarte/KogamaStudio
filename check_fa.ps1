Add-Type -AssemblyName System.Drawing
$fontFile = $env:LOCALAPPDATA + '\KogamaStudio\fa-solid-900.ttf'
$col = New-Object System.Drawing.Text.PrivateFontCollection
$col.AddFontFile($fontFile)
$fam = $col.Families[0]
Write-Host "Font family:" $fam.Name

# Check specific codepoints
$checkPoints = @(0xF1B2, 0xF0EB, 0xF030, 0xF111, 0xF06D, 0xF773, 0xE000, 0xF002)
foreach ($cp in $checkPoints) {
    Write-Host ([string]::Format("U+{0:X4}", $cp))
}

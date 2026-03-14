$v = (Get-Content 'C:\Users\mexav\source\repos\KogamaStudio-old\version.txt').Trim()
Write-Host "Version: '$v'"
$content = '#pragma once' + [char]10 + '#define KS_VERSION ' + [char]34 + $v + [char]34
Write-Host "Content: $content"
Set-Content -Path 'C:\Users\mexav\source\repos\KogamaStudio-old\Universal-Dear-ImGui-Hook\version.h' -Value $content
Write-Host "Done. Exit code: $LASTEXITCODE"

# generate-project-tree.ps1
# Generates a JSON project tree with paths relative to the execution root

param (
    [string]$TargetPath = "."
)

# 1. Clean up and get the absolute path of the solution root
$TargetPath = (Resolve-Path ($TargetPath.Trim('"').Trim("'"))).Path

function Get-ProjectTree($path, $rootPath) {
    if (-not (Test-Path $path)) { return $null }

    # Folders to ignore
    $excludeList = @("bin", "obj", ".git", ".vs", "node_modules", "dist", "out", "scripts" , "packages")
    
    $items = Get-ChildItem -Path $path -Force | Where-Object { $_.Name -notin $excludeList }
    
    $result = foreach ($item in $items) {
        # Calculate the relative path from the Root
        # .Substring($rootPath.Length) gives us things like "\Codify\File.cs"
        $relative = $item.FullName.Substring($rootPath.Length)
        
        # Trim leading slashes and convert to forward slashes
        # Result: "Codify/Infrastructure/WebView/WebViewClient.cs"
        $finalPath = $relative.TrimStart("\").TrimStart("/").Replace("\", "/")

        if ($item.PSIsContainer) {
            $children = Get-ProjectTree $item.FullName $rootPath
            @{
                name     = $item.Name
                type     = "folder"
                path     = $finalPath
                children = $children
            }
        } else {
            @{
                name = $item.Name
                type = "file"
                path = $finalPath
            }
        }
    }
    return $result
}

# Execute
$tree = Get-ProjectTree $TargetPath $TargetPath

# Convert to JSON and save
$tree | ConvertTo-Json -Depth 20 | Set-Content "$TargetPath\ProjectTree.json" -Encoding UTF8

Write-Host "✅ ProjectTree.json updated successfully." -ForegroundColor Green

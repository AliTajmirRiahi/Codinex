# generate-project-tree.ps1
# Generates a JSON project tree with paths starting from the Root folder name

param (
    [string]$TargetPath = "."
)

# 1. Clean up trailing quotes and get the absolute path
$TargetPath = (Resolve-Path ($TargetPath.Trim('"').Trim("'"))).Path
# 2. Get the name of the root directory (e.g., "Codify")
$RootFolderName = Split-Path $TargetPath -Leaf

function Get-ProjectTree($path, $rootPath, $rootName) {
    if (-not (Test-Path $path)) { return $null }

    # Folders to ignore
    $excludeList = @("bin", "obj", ".git", ".vs", "node_modules", "dist", "out", "scripts")
    
    $items = Get-ChildItem -Path $path -Force | Where-Object { $_.Name -notin $excludeList }
    
    $result = foreach ($item in $items) {
        # Calculate the relative path from the TargetPath
        # Example: "C:\Projects\Codify\Sub\File.cs" -> "\Sub\File.cs"
        $relativeFromRoot = $item.FullName.Substring($rootPath.Length)
        
        # Build the final path: RootName + RelativePath and replace \ with /
        # Example: "Codify" + "/Sub/File.cs" -> "Codify/Sub/File.cs"
        $finalPath = ($rootName + $relativeFromRoot).Replace("\", "/")

        if ($item.PSIsContainer) {
            $children = Get-ProjectTree $item.FullName $rootPath $rootName
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

# Execute the tree generation
$tree = Get-ProjectTree $TargetPath $TargetPath $RootFolderName

# Convert to JSON and save
$tree | ConvertTo-Json -Depth 20 | Set-Content "$TargetPath\ProjectTree.json" -Encoding UTF8

Write-Host "ProjectTree.json updated with paths starting from '$RootFolderName/'" -ForegroundColor Green

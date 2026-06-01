<#
  Creates a large tree of tiny files for testing the watcher / sync under load (Windows).
  Usage:   .\make-test-files.ps1 -Root D:\Test\bulk -Total 100000 -PerDir 50
  Cleanup: Remove-Item -Recurse -Force <Root>
#>
param(
    [string]$Root = ".\testdata",
    [int]$Total = 100000,   # total number of files
    [int]$PerDir = 50       # files per leaf directory
)

Write-Host "Creating $Total tiny files under '$Root' ($PerDir per leaf dir)..."
$sw = [System.Diagnostics.Stopwatch]::StartNew()

$i = 0
$dirIndex = 0
while ($i -lt $Total) {
    # Nest as Root\dir_<l1>\sub_<l2> (100 subdirs per top-level dir).
    $l1 = [math]::Floor($dirIndex / 100)
    $l2 = $dirIndex % 100
    $d = Join-Path $Root ("dir_{0}\sub_{1}" -f $l1, $l2)
    New-Item -ItemType Directory -Force -Path $d | Out-Null
    for ($j = 0; $j -lt $PerDir -and $i -lt $Total; $j++) {
        # [IO.File] is far faster than Set-Content for many small files.
        [System.IO.File]::WriteAllText((Join-Path $d ("file_{0}.txt" -f $i)), "test $i")
        $i++
    }
    $dirIndex++
}

$sw.Stop()
Write-Host "Done: $i files in $dirIndex leaf dirs under '$Root' ($([int]$sw.Elapsed.TotalSeconds)s)."

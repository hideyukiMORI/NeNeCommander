[CmdletBinding()]
param(
    [string] $RepositoryRoot = (Split-Path -Parent $PSScriptRoot),
    [ValidateSet('Check', 'Write')] [string] $Mode = 'Check',
    [string] $ReportPath
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$root = [System.IO.Path]::GetFullPath($RepositoryRoot)
$inputRoot = Join-Path $root 'eng/theme-catalog'
$source = Get-Content -LiteralPath (Join-Path $inputRoot 'source.json') -Raw | ConvertFrom-Json -AsHashtable
$utf8 = [System.Text.UTF8Encoding]::new($false)
$roles = @('SurfaceWindow', 'SurfacePane', 'SurfaceField', 'TextPrimary', 'TextSecondary', 'TextHidden', 'TextKeyHint', 'BorderSubtle', 'FocusRing', 'FocusSurface', 'SelectionSurface', 'SelectionMark', 'StatusWarning', 'StatusWarningSurface', 'StatusDanger', 'StatusDangerSurface', 'OperationProgress', 'OperationTrack')
$surfaces = @('SurfaceWindow', 'SurfacePane', 'SurfaceField', 'FocusSurface', 'SelectionSurface', 'BorderSubtle', 'StatusWarningSurface', 'StatusDangerSurface')
$sourceRoles = @('foreground', 'background', 'cursor', 'selection', 'current_line', 'line_number', 'comment', 'keyword', 'string', 'number', 'type', 'function', 'constant', 'operators', 'error', 'warning')
$report = [System.Collections.Generic.List[object]]::new()
$outputCount = 0

function ConvertTo-Rgb([string] $Hex) {
    if ($Hex -cnotmatch '^#[0-9A-F]{6}$') { throw "Invalid fixed source color: $Hex" }
    return ,@([Convert]::ToInt32($Hex.Substring(1, 2), 16), [Convert]::ToInt32($Hex.Substring(3, 2), 16), [Convert]::ToInt32($Hex.Substring(5, 2), 16))
}

function ConvertTo-Hex([int[]] $Rgb) {
    return '#{0:X2}{1:X2}{2:X2}' -f $Rgb[0], $Rgb[1], $Rgb[2]
}

function Get-Luminance([int[]] $Rgb) {
    $weights = @(0.2126, 0.7152, 0.0722)
    $sum = 0.0
    for ($i = 0; $i -lt 3; $i++) {
        $value = $Rgb[$i] / 255.0
        $linear = if ($value -le 0.04045) { $value / 12.92 } else { [Math]::Pow(($value + 0.055) / 1.055, 2.4) }
        $sum += $weights[$i] * $linear
    }
    return $sum
}

function Get-Contrast([int[]] $Left, [int[]] $Right) {
    $a = Get-Luminance $Left
    $b = Get-Luminance $Right
    return ([Math]::Max($a, $b) + 0.05) / ([Math]::Min($a, $b) + 0.05)
}

function Get-Mixed([int[]] $Start, [int[]] $Target, [double] $Fraction) {
    return ,@(for ($i = 0; $i -lt 3; $i++) { [int][Math]::Floor($Start[$i] * (1 - $Fraction) + $Target[$i] * $Fraction + 0.5) })
}

function Get-Repaired([int[]] $Start, [int[]] $Target, [object[]] $Requirements) {
    for ($step = 0; $step -le 32; $step++) {
        $value = Get-Mixed $Start $Target ($step / 32.0)
        $passed = $true
        foreach ($requirement in $Requirements) {
            if ((Get-Contrast $value $requirement.background) -lt $requirement.minimum) { $passed = $false; break }
        }
        if ($passed) { return @{ rgb = $value; step = $step } }
    }
    throw 'This palette has no compliant endpoint. No target is lowered.'
}

function Set-ImportedText([string] $RelativePath, [string] $Content) {
    $path = Join-Path $root $RelativePath
    $normalized = $Content.Replace("`r`n", "`n").TrimEnd("`r", "`n").Replace("`n", "`r`n") + "`r`n"
    $expected = $utf8.GetBytes($normalized)
    if ($Mode -eq 'Write') { [System.IO.File]::WriteAllBytes($path, $expected) }
    elseif (-not (Test-Path -LiteralPath $path -PathType Leaf) -or
        [Convert]::ToHexString([System.Security.Cryptography.SHA256]::HashData([System.IO.File]::ReadAllBytes($path))) -cne
        [Convert]::ToHexString([System.Security.Cryptography.SHA256]::HashData($expected))) {
        throw "Imported output differs: $RelativePath"
    }
    $script:outputCount++
}

if ($source.schemaVersion -ne 1 -or $source.themes.Count -ne 125 -or $source.commanderOnly.Count -ne 4) { throw 'Unexpected fixed catalog shape.' }
$entries = @($source.commanderOnly) + @($source.themes)
$identifiers = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
$keys = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
foreach ($entry in $entries) {
    if ($entry.name -cnotmatch '^[a-z0-9]+(?:-[a-z0-9]+)*$' -or $entry.appearance -cnotin @('dark', 'light') -or
        $entry.nameKey -cnotmatch '^ColorSchemeName[A-Za-z0-9]+$' -or -not $identifiers.Add($entry.name) -or -not $keys.Add($entry.nameKey)) { throw 'Invalid or duplicate fixed catalog entry.' }
}
foreach ($entry in $source.commanderOnly) {
    $path = Join-Path $root "src/NeNeCommander.App/Themes/Schemes/$($entry.name).xaml"
    if ((Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash.ToLowerInvariant() -cne $entry.sha256) { throw "Preserved scheme changed: $($entry.name)" }
}
$noticePath = Join-Path $root $source.notice.path
if ((Get-Item -LiteralPath $noticePath).Length -ne $source.notice.bytes -or
    (Get-FileHash -LiteralPath $noticePath -Algorithm SHA256).Hash.ToLowerInvariant() -cne $source.notice.sha256) { throw 'Bounded palette notice changed.' }

foreach ($entry in $source.themes) {
    if (@(Compare-Object @($entry.body.Keys | Sort-Object) @($sourceRoles | Sort-Object) -CaseSensitive).Count -ne 0) { throw 'Source palette role set differs.' }
    foreach ($field in @('author', 'license', 'url')) {
        if ([string]::IsNullOrWhiteSpace($entry.source[$field])) { throw "Missing attribution: $($entry.name)/$field" }
    }
    $palette = @{}
    foreach ($key in $sourceRoles) { $palette[$key] = ConvertTo-Rgb $entry.body[$key] }
    $background = $palette.background
    $foreground = $palette.foreground
    if ((Get-Contrast $background $foreground) -lt 4.5) { throw "Source primary contrast fails: $($entry.name)" }
    $anchor = if ((Get-Luminance $foreground) -gt (Get-Luminance $background)) { @(255, 255, 255) } else { @(0, 0, 0) }
    $colors = @{ SurfacePane = $background; TextPrimary = $foreground }
    $steps = [ordered]@{}
    $candidates = [ordered]@{
        SurfaceWindow = Get-Mixed $background $foreground 0.06
        SurfaceField = Get-Mixed $background $foreground 0.10
        BorderSubtle = Get-Mixed $background $foreground 0.22
        FocusSurface = $palette.current_line
        SelectionSurface = $palette.selection
        StatusWarningSurface = Get-Mixed $background $palette.warning 0.15
        StatusDangerSurface = Get-Mixed $background $palette.error 0.15
        OperationTrack = Get-Mixed $background $foreground 0.15
    }
    if ((ConvertTo-Hex $candidates.FocusSurface) -ceq (ConvertTo-Hex $background)) { $candidates.FocusSurface = Get-Mixed $background $foreground 0.08 }
    if ((ConvertTo-Hex $candidates.SelectionSurface) -ceq (ConvertTo-Hex $candidates.FocusSurface)) { $candidates.SelectionSurface = Get-Mixed $background $foreground 0.18 }
    foreach ($key in $candidates.Keys) {
        $result = Get-Repaired $candidates[$key] $background @(@{ background = $foreground; minimum = 4.5 })
        $colors[$key] = $result.rgb; $steps[$key] = $result.step
    }
    $requirements = @($surfaces | ForEach-Object { @{ background = $colors[$_]; minimum = 4.5 } })
    $textSources = [ordered]@{ TextSecondary = 'comment'; TextHidden = 'line_number'; TextKeyHint = 'keyword'; StatusWarning = 'warning'; StatusDanger = 'error'; FocusRing = 'cursor' }
    foreach ($key in $textSources.Keys) {
        $result = Get-Repaired $palette[$textSources[$key]] $anchor $requirements
        $colors[$key] = $result.rgb; $steps[$key] = $result.step
    }
    foreach ($key in @('SelectionMark', 'OperationProgress')) {
        $pairSurfaces = if ($key -eq 'SelectionMark') { @('SurfacePane', 'FocusSurface', 'SelectionSurface') } else { @('OperationTrack', 'SurfacePane', 'FocusSurface') }
        $sourceKey = if ($key -eq 'SelectionMark') { 'string' } else { 'function' }
        $requirements = @($pairSurfaces | ForEach-Object { @{ background = $colors[$_]; minimum = 3.0 } })
        $result = Get-Repaired $palette[$sourceKey] $anchor $requirements
        $colors[$key] = $result.rgb; $steps[$key] = $result.step
    }
    $encoded = [ordered]@{}
    foreach ($key in $roles) { $encoded[$key] = ConvertTo-Hex $colors[$key] }
    $lines = [System.Collections.Generic.List[string]]::new()
    $lines.Add('<?xml version="1.0" encoding="UTF-8"?>')
    $lines.Add('<ResourceDictionary')
    $lines.Add('    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"')
    $lines.Add('    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">')
    foreach ($key in $roles) { $lines.Add('    <Color x:Key="' + $key + 'Color">#FF' + $encoded[$key].Substring(1) + '</Color>') }
    $lines.Add('')
    foreach ($key in $roles) { $lines.Add('    <SolidColorBrush x:Key="' + $key + 'Brush" Color="{StaticResource ' + $key + 'Color}" />') }
    $lines.Add('</ResourceDictionary>')
    Set-ImportedText "src/NeNeCommander.App/Themes/Schemes/$($entry.name).xaml" ($lines -join "`n")
    $report.Add([ordered]@{ identifier = $entry.name; appearance = $entry.appearance; colors = $encoded; adjustmentSteps = $steps })
}

$catalog = @($entries | ForEach-Object { '            new CatalogScheme("' + $_.name + '", ColorSchemeAppearance.' + $(if ($_.appearance -eq 'dark') { 'Dark' } else { 'Light' }) + '),' }) -join "`n"
$addresses = @($entries | ForEach-Object { '            ["' + $_.name + '"] = new Uri("ms-appx:///Themes/Schemes/' + $_.name + '.xaml"),' }) -join "`n"
$nameKeys = @($entries | ForEach-Object { '            ["' + $_.name + '"] = "' + $_.nameKey + '",' }) -join "`n"
foreach ($projection in @(
    @{ template = 'ColorScheme.cs.txt'; target = 'src/NeNeCommander.Application/Settings/ColorScheme.cs'; token = '{{CATALOG}}'; value = $catalog },
    @{ template = 'ColorSchemeResources.cs.txt'; target = 'src/NeNeCommander.App/Themes/ColorSchemeResources.cs'; token = '{{ADDRESSES}}'; value = $addresses },
    @{ template = 'SettingsPresenter.cs.txt'; target = 'src/NeNeCommander.Presentation.WinUI/Settings/SettingsPresenter.cs'; token = '{{NAME_KEYS}}'; value = $nameKeys }
)) {
    $template = Get-Content -LiteralPath (Join-Path $inputRoot $projection.template) -Raw
    if ([regex]::Matches($template, [regex]::Escape($projection.token)).Count -ne 1) { throw 'Expected exactly one template insertion.' }
    Set-ImportedText $projection.target $template.Replace($projection.token, $projection.value)
}

foreach ($locale in @('en-US', 'ja-JP', 'zh-Hans')) {
    $relative = "src/NeNeCommander.App/Resources/$locale/Resources.resw"
    $text = Get-Content -LiteralPath (Join-Path $root $relative) -Raw
    [xml] $document = $text
    foreach ($entry in @($source.themes | Where-Object { $_.nameKey.StartsWith('ColorSchemeNameCatalog', [StringComparison]::Ordinal) })) {
        $nodes = @($document.root.data | Where-Object { $_.name -ceq $entry.nameKey })
        if ($nodes.Count -eq 0) {
            $addition = '  <data name="' + $entry.nameKey + '" xml:space="preserve">' + "`n" + '    <value>' + $entry.name + '</value>' + "`n  </data>`n"
            $text = $text.Replace('</root>', $addition + '</root>')
        }
        elseif ($nodes.Count -ne 1 -or $nodes[0].value -cne $entry.name) { throw "Imported theme label differs: $locale/$($entry.nameKey)" }
    }
    Set-ImportedText $relative $text
}
if ($ReportPath) {
    [System.IO.File]::WriteAllText([System.IO.Path]::GetFullPath($ReportPath), (@{ schemes = 129; shared = 125; themes = @($report) } | ConvertTo-Json -Depth 8) + "`n", $utf8)
}
Write-Host "PASS: $Mode, $outputCount imported outputs, 129 closed schemes, 125 adapted palettes, four preserved palettes and exact attribution notice."

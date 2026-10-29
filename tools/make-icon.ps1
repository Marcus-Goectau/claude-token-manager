# Regenerates src/ClaudeTokenManager/Assets/app.ico. Shapes are drawn on a 256x256 grid and scaled per size.
param(
    [string]$PreviewDir
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing

$out = Join-Path $PSScriptRoot '..\src\ClaudeTokenManager\Assets\app.ico'
$sizes = 16, 20, 24, 32, 40, 48, 64, 256

$orange = [System.Drawing.Color]::FromArgb(217, 119, 87)
$white = [System.Drawing.Color]::White

function New-RoundedRect([float]$x, [float]$y, [float]$w, [float]$h, [float]$r) {
    $path = New-Object System.Drawing.Drawing2D.GraphicsPath
    $d = $r * 2
    $path.AddArc($x, $y, $d, $d, 180, 90)
    $path.AddArc($x + $w - $d, $y, $d, $d, 270, 90)
    $path.AddArc($x + $w - $d, $y + $h - $d, $d, $d, 0, 90)
    $path.AddArc($x, $y + $h - $d, $d, $d, 90, 90)
    $path.CloseFigure()
    $path
}

function New-Pen($color, [float]$width) {
    $pen = New-Object System.Drawing.Pen($color, $width)
    $pen.StartCap = [System.Drawing.Drawing2D.LineCap]::Round
    $pen.EndCap = [System.Drawing.Drawing2D.LineCap]::Round
    $pen
}

function Get-IconPng([int]$size) {
    $bmp = New-Object System.Drawing.Bitmap($size, $size)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $g.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
    $g.ScaleTransform($size / 256, $size / 256)

    $bg = New-RoundedRect 8 8 240 240 52
    $g.FillPath((New-Object System.Drawing.SolidBrush($orange)), $bg)

    # Key shaft and teeth, running from the ring toward the bottom-right corner.
    $shaft = New-Pen $white 34
    $g.DrawLine($shaft, 120, 120, 212, 212)
    $teeth = New-Pen $white 26
    $g.DrawLine($teeth, 186, 186, 212, 160)
    $g.DrawLine($teeth, 156, 156, 180, 132)

    # Ring with a starburst cut into it.
    $g.FillEllipse((New-Object System.Drawing.SolidBrush($white)), 32, 32, 128, 128)
    $ray = New-Pen $orange 13
    for ($i = 0; $i -lt 8; $i++) {
        $a = [Math]::PI * $i / 4 + [Math]::PI / 8
        $g.DrawLine($ray, 96, 96, 96 + 40 * [Math]::Cos($a), 96 + 40 * [Math]::Sin($a))
    }

    $g.Dispose()
    $ms = New-Object System.IO.MemoryStream
    $bmp.Save($ms, [System.Drawing.Imaging.ImageFormat]::Png)
    if ($PreviewDir) { $bmp.Save((Join-Path $PreviewDir "icon-$size.png"), [System.Drawing.Imaging.ImageFormat]::Png) }
    $bmp.Dispose()
    , $ms.ToArray()
}

$images = foreach ($s in $sizes) { , (Get-IconPng $s) }

New-Item -ItemType Directory -Force (Split-Path $out) | Out-Null
$fs = [System.IO.File]::Create($out)
$w = New-Object System.IO.BinaryWriter($fs)
$w.Write([uint16]0); $w.Write([uint16]1); $w.Write([uint16]$sizes.Count)

$offset = 6 + 16 * $sizes.Count
for ($i = 0; $i -lt $sizes.Count; $i++) {
    $dim = if ($sizes[$i] -ge 256) { 0 } else { $sizes[$i] }
    $w.Write([byte]$dim); $w.Write([byte]$dim); $w.Write([byte]0); $w.Write([byte]0)
    $w.Write([uint16]1); $w.Write([uint16]32)
    $w.Write([uint32]$images[$i].Length); $w.Write([uint32]$offset)
    $offset += $images[$i].Length
}
foreach ($img in $images) { $w.Write($img) }
$w.Dispose()

Write-Host "Wrote $out"

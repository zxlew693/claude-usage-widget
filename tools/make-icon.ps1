# Renders the gauge logo (same geometry as assets/logo.svg) at each icon size
# with System.Drawing, then packs the PNGs into a multi-size .ico.
# Usage: powershell -File tools/make-icon.ps1 -OutDir assets [-PreviewPath preview.png]
param([string]$OutDir = (Join-Path $PSScriptRoot '..\assets'), [string]$PreviewPath)

Add-Type -AssemblyName System.Drawing
$ErrorActionPreference = 'Stop'

function C([string]$hex, [int]$a = 255) {
    $c = [System.Drawing.ColorTranslator]::FromHtml($hex)
    [System.Drawing.Color]::FromArgb($a, $c.R, $c.G, $c.B)
}

function Render([int]$size) {
    $bmp = New-Object System.Drawing.Bitmap $size, $size, ([System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode = 'AntiAlias'
    $g.PixelOffsetMode = 'HighQuality'
    $g.Clear([System.Drawing.Color]::Transparent)
    $s = $size / 256.0
    $g.ScaleTransform($s, $s)

    # Tile
    $r = 56; $d = $r * 2
    $tile = New-Object System.Drawing.Drawing2D.GraphicsPath
    $tile.AddArc(0, 0, $d, $d, 180, 90); $tile.AddArc(256 - $d, 0, $d, $d, 270, 90)
    $tile.AddArc(256 - $d, 256 - $d, $d, $d, 0, 90); $tile.AddArc(0, 256 - $d, $d, $d, 90, 90)
    $tile.CloseFigure()
    $bg = New-Object System.Drawing.Drawing2D.LinearGradientBrush (New-Object System.Drawing.PointF 0, 0), (New-Object System.Drawing.PointF 0, 256), (C '#2A2A32'), (C '#141418')
    $g.FillPath($bg, $tile)

    # Thicker strokes at tiny sizes so the gauge stays readable.
    $w = if ($size -le 24) { 34 } elseif ($size -le 48) { 30 } else { 26 }
    $rad = 78; $box = New-Object System.Drawing.RectangleF (128 - $rad), (128 - $rad), ($rad * 2), ($rad * 2)

    $track = New-Object System.Drawing.Pen (C '#FFFFFF' 36), $w
    $track.StartCap = 'Round'; $track.EndCap = 'Round'
    $g.DrawArc($track, $box, 135, 270)

    $grad = New-Object System.Drawing.Drawing2D.LinearGradientBrush (New-Object System.Drawing.PointF 40, 216), (New-Object System.Drawing.PointF 216, 40), (C '#5FB49C'), (C '#D97757')
    $blend = New-Object System.Drawing.Drawing2D.ColorBlend 3
    $blend.Colors = @((C '#5FB49C'), (C '#E5B567'), (C '#D97757'))
    $blend.Positions = @(0.0, 0.55, 1.0)
    $grad.InterpolationColors = $blend
    $fill = New-Object System.Drawing.Pen $grad, $w
    $fill.StartCap = 'Round'; $fill.EndCap = 'Round'
    $g.DrawArc($fill, $box, 135, 184)

    $hub = if ($size -le 24) { 22 } else { 17 }
    $g.FillEllipse((New-Object System.Drawing.SolidBrush (C '#D97757')), (128 - $hub), (128 - $hub), ($hub * 2), ($hub * 2))

    $g.Dispose()
    $ms = New-Object System.IO.MemoryStream
    $bmp.Save($ms, [System.Drawing.Imaging.ImageFormat]::Png)
    $bmp.Dispose()
    , $ms.ToArray()
}

New-Item -ItemType Directory -Force $OutDir | Out-Null
$sizes = 16, 20, 24, 32, 40, 48, 64, 128, 256
$pngs = @{}
foreach ($sz in $sizes) { $pngs[$sz] = Render $sz }

[System.IO.File]::WriteAllBytes((Join-Path $OutDir 'logo-256.png'), $pngs[256])
[System.IO.File]::WriteAllBytes((Join-Path $OutDir 'logo-512.png'), (Render 512))

# ICO: ICONDIR + ICONDIRENTRY[] + PNG blobs
$ms = New-Object System.IO.MemoryStream
$bw = New-Object System.IO.BinaryWriter $ms
$bw.Write([uint16]0); $bw.Write([uint16]1); $bw.Write([uint16]$sizes.Count)
$offset = 6 + 16 * $sizes.Count
foreach ($sz in $sizes) {
    $dim = if ($sz -ge 256) { 0 } else { $sz }
    $bw.Write([byte]$dim); $bw.Write([byte]$dim); $bw.Write([byte]0); $bw.Write([byte]0)
    $bw.Write([uint16]1); $bw.Write([uint16]32)
    $bw.Write([uint32]$pngs[$sz].Length); $bw.Write([uint32]$offset)
    $offset += $pngs[$sz].Length
}
foreach ($sz in $sizes) { $bw.Write($pngs[$sz]) }
$bw.Flush()
[System.IO.File]::WriteAllBytes((Join-Path $OutDir 'app.ico'), $ms.ToArray())

if (-not $PreviewPath) { return }

# Preview sheet: every size on dark and light backgrounds
$sheet = New-Object System.Drawing.Bitmap 760, 380
$g = [System.Drawing.Graphics]::FromImage($sheet)
$g.FillRectangle((New-Object System.Drawing.SolidBrush (C '#202020')), 0, 0, 760, 190)
$g.FillRectangle((New-Object System.Drawing.SolidBrush (C '#F3F3F3')), 0, 190, 760, 190)
foreach ($row in 0, 190) {
    $x = 16
    foreach ($sz in 16, 24, 32, 48, 64, 128) {
        $img = [System.Drawing.Image]::FromStream((New-Object System.IO.MemoryStream (, $pngs[$sz])))
        $g.DrawImageUnscaled($img, $x, $row + (190 - $sz) / 2)
        $x += $sz + 24
    }
}
$g.Dispose()
$sheet.Save($PreviewPath)

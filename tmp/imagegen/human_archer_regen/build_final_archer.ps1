param(
    [string]$Source = 'H:\unitygame\pin-ball\pin-ball\Assets\03. Images\Humans\Archer\H_Archer.png',
    [string]$Output = 'H:\unitygame\pin-ball\tmp\imagegen\human_archer_regen\H_Archer_final_candidate.png',
    [string]$Preview = 'H:\unitygame\pin-ball\tmp\imagegen\human_archer_regen\H_Archer_final_candidate_8x.png'
)

Add-Type -AssemblyName System.Drawing

function Get-RgbHex([System.Drawing.Color]$Color) {
    return ('{0:X2}{1:X2}{2:X2}' -f $Color.R, $Color.G, $Color.B)
}

function Get-Color([string]$Hex) {
    return [System.Drawing.Color]::FromArgb(
        255,
        [Convert]::ToInt32($Hex.Substring(0, 2), 16),
        [Convert]::ToInt32($Hex.Substring(2, 2), 16),
        [Convert]::ToInt32($Hex.Substring(4, 2), 16)
    )
}

$hoodPalette = @{
    '323231' = '293020'
    '6B6B6A' = '465238'
    '8D8A85' = '596548'
    'AFAEAE' = '687650'
    'D3D2D5' = '7F8B5E'
}

$tunicPalette = @{
    '584431' = '465238'
    '7C6348' = '687650'
    'B67659' = '7F8B5E'
}

$sourceBitmap = [System.Drawing.Bitmap]::FromFile($Source)
try {
    $outputBitmap = New-Object System.Drawing.Bitmap $sourceBitmap.Width, $sourceBitmap.Height, ([System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    try {
        for ($y = 0; $y -lt $sourceBitmap.Height; $y++) {
            for ($x = 0; $x -lt $sourceBitmap.Width; $x++) {
                $pixel = $sourceBitmap.GetPixel($x, $y)
                if ($pixel.A -eq 0) {
                    $outputBitmap.SetPixel($x, $y, [System.Drawing.Color]::Transparent)
                    continue
                }

                $rgb = Get-RgbHex $pixel
                $replacement = $null

                # Keep the original helmet footprint, but turn its metal shading into a compact olive hood.
                if ($x -ge 37 -and $x -le 49 -and $y -ge 26 -and $y -le 36 -and $hoodPalette.ContainsKey($rgb)) {
                    $replacement = $hoodPalette[$rgb]
                }

                # The original face aperture stays the same shape and becomes a featureless shadow.
                if ($x -ge 43 -and $x -le 48 -and $y -ge 32 -and $y -le 36 -and @('110A09', 'B67659', 'E1A07B') -contains $rgb) {
                    $replacement = '110A09'
                }

                # Recolor only the torso garment; limbs, boots, gloves, bow, arrow, and quiver keep their geometry.
                if ($x -ge 38 -and $x -le 53 -and $y -ge 37 -and $y -le 55 -and $tunicPalette.ContainsKey($rgb)) {
                    $replacement = $tunicPalette[$rgb]
                }

                if ($replacement) {
                    $outputBitmap.SetPixel($x, $y, (Get-Color $replacement))
                }
                else {
                    $outputBitmap.SetPixel($x, $y, $pixel)
                }
            }
        }

        $outputBitmap.Save($Output, [System.Drawing.Imaging.ImageFormat]::Png)

        $previewBitmap = New-Object System.Drawing.Bitmap 672, 672, ([System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
        try {
            $graphics = [System.Drawing.Graphics]::FromImage($previewBitmap)
            try {
                $graphics.Clear([System.Drawing.Color]::Transparent)
                $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::NearestNeighbor
                $graphics.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::Half
                $destination = New-Object System.Drawing.Rectangle 0, 0, 672, 672
                $graphics.DrawImage($outputBitmap, $destination, 0, 0, 84, 84, [System.Drawing.GraphicsUnit]::Pixel)
            }
            finally {
                $graphics.Dispose()
            }
            $previewBitmap.Save($Preview, [System.Drawing.Imaging.ImageFormat]::Png)
        }
        finally {
            $previewBitmap.Dispose()
        }
    }
    finally {
        $outputBitmap.Dispose()
    }
}
finally {
    $sourceBitmap.Dispose()
}

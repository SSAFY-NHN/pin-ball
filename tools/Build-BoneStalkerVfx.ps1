param(
    [Parameter(Mandatory = $true)]
    [string]$WorkDirectory,

    [Parameter(Mandatory = $true)]
    [string]$OutputDirectory,

    [Parameter(Mandatory = $true)]
    [string]$MetaTemplate
)

Add-Type -AssemblyName System.Drawing

function Draw-EffectFrame {
    param(
        [System.Drawing.Graphics]$Graphics,
        [string]$ImagePath,
        [int]$FrameIndex,
        [int]$X,
        [int]$Y,
        [int]$Width,
        [int]$Height,
        [single]$Opacity
    )

    $image = [System.Drawing.Bitmap]::FromFile($ImagePath)
    $attributes = New-Object System.Drawing.Imaging.ImageAttributes
    $matrix = New-Object System.Drawing.Imaging.ColorMatrix
    $matrix.Matrix33 = $Opacity
    $attributes.SetColorMatrix($matrix)

    try {
        $destination = New-Object System.Drawing.Rectangle(($FrameIndex * 256 + $X), $Y, $Width, $Height)
        $Graphics.DrawImage(
            $image,
            $destination,
            0,
            0,
            $image.Width,
            $image.Height,
            [System.Drawing.GraphicsUnit]::Pixel,
            $attributes
        )
    }
    finally {
        $attributes.Dispose()
        $image.Dispose()
    }
}

function New-EffectSheet {
    param(
        [string]$OutputPath,
        [array]$Frames
    )

    $sheet = New-Object System.Drawing.Bitmap(2304, 256, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $graphics = [System.Drawing.Graphics]::FromImage($sheet)
    $graphics.CompositingMode = [System.Drawing.Drawing2D.CompositingMode]::SourceOver
    $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::NearestNeighbor
    $graphics.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::Half
    $graphics.Clear([System.Drawing.Color]::Transparent)

    try {
        foreach ($frame in $Frames) {
            Draw-EffectFrame `
                -Graphics $graphics `
                -ImagePath (Join-Path $WorkDirectory $frame.File) `
                -FrameIndex $frame.Index `
                -X $frame.X `
                -Y $frame.Y `
                -Width $frame.Width `
                -Height $frame.Height `
                -Opacity $frame.Opacity
        }
    }
    finally {
        $graphics.Dispose()
    }

    try {
        $sheet.Save($OutputPath, [System.Drawing.Imaging.ImageFormat]::Png)
    }
    finally {
        $sheet.Dispose()
    }
}

function New-EffectMeta {
    param(
        [string]$OutputPath,
        [string]$AssetName
    )

    $assetGuid = [Guid]::NewGuid().ToString('N')
    if (Test-Path -LiteralPath $OutputPath) {
        $existingMeta = [System.IO.File]::ReadAllText($OutputPath).Replace("`r`n", "`n")
        $guidMatch = [System.Text.RegularExpressions.Regex]::Match(
            $existingMeta,
            '(?m)^guid: ([0-9a-f]+)$'
        )
        if ($guidMatch.Success) {
            $assetGuid = $guidMatch.Groups[1].Value
        }
    }

    $meta = [System.IO.File]::ReadAllText($MetaTemplate).Replace("`r`n", "`n")
    $meta = [System.Text.RegularExpressions.Regex]::Replace(
        $meta,
        '(?m)^guid: [0-9a-f]+$',
        "guid: $assetGuid"
    )
    $meta = $meta.Replace('H_MountedMageBoss_Attack', $AssetName)
    $meta = $meta.Replace('maxTextureSize: 2048', 'maxTextureSize: 4096')

    for ($index = 8; $index -ge 0; $index--) {
        $oldX = $index * 128
        $newX = $index * 256
        $oldRect = "        x: $oldX`n        y: 0`n        width: 128`n        height: 128"
        $newRect = "        x: $newX`n        y: 0`n        width: 256`n        height: 256"
        $meta = $meta.Replace($oldRect, $newRect)
    }

    [System.IO.File]::WriteAllText(
        $OutputPath,
        $meta,
        [System.Text.UTF8Encoding]::new($false)
    )
}

$attackPath = Join-Path $OutputDirectory 'H_BoneStalkerBoss_AttackEffect.png'
$roarPath = Join-Path $OutputDirectory 'H_BoneStalkerBoss_RoarEffect.png'

$attackFrames = @(
    @{ Index = 5; File = 'attackfx-0.png'; X = 128; Y = 41; Width = 128; Height = 128; Opacity = 0.35 },
    @{ Index = 6; File = 'attackfx-1.png'; X = 128; Y = 41; Width = 128; Height = 128; Opacity = 1.00 },
    @{ Index = 7; File = 'attackfx-2.png'; X = 128; Y = 41; Width = 128; Height = 128; Opacity = 0.70 },
    @{ Index = 8; File = 'attackfx-4.png'; X = 128; Y = 41; Width = 128; Height = 128; Opacity = 0.20 }
)

$roarFrames = @(
    @{ Index = 3; File = 'roarfx-0.png'; X = 68; Y = 28; Width = 104; Height = 104; Opacity = 0.50 },
    @{ Index = 4; File = 'roarfx-0.png'; X = 40; Y = 0; Width = 160; Height = 160; Opacity = 0.90 },
    @{ Index = 5; File = 'roarfx-1.png'; X = 40; Y = 0; Width = 160; Height = 160; Opacity = 1.00 },
    @{ Index = 6; File = 'roarfx-2.png'; X = 40; Y = 0; Width = 160; Height = 160; Opacity = 1.00 },
    @{ Index = 7; File = 'roarfx-4.png'; X = 40; Y = 0; Width = 160; Height = 160; Opacity = 0.65 },
    @{ Index = 8; File = 'roarfx-6.png'; X = 40; Y = 0; Width = 160; Height = 160; Opacity = 0.25 }
)

New-EffectSheet -OutputPath $attackPath -Frames $attackFrames
New-EffectSheet -OutputPath $roarPath -Frames $roarFrames
New-EffectMeta -OutputPath "$attackPath.meta" -AssetName 'H_BoneStalkerBoss_AttackEffect'
New-EffectMeta -OutputPath "$roarPath.meta" -AssetName 'H_BoneStalkerBoss_RoarEffect'

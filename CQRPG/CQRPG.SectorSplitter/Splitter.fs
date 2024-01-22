module Splitter

open System
open System.Drawing
open System.IO

type SplitterResult<'data> = Result<'data, string>

let private loadSourceImage (source: string) : Image SplitterResult =
    if not <| File.Exists source then
        Error $"Source file not found: {source}"
    else
        let img = Image.FromFile(source)
        Ok img

let private createOutDirIfMissing (outDir: string) : unit SplitterResult =
    if Directory.Exists outDir then
       Ok ()
    else
        try 
            outDir |> Directory.CreateDirectory
            |> ignore
            |> Ok
        with
        | e -> Error e.Message

let private hasAnyWhitePixels (img: Bitmap) : bool =
    seq {
        for x in [0..img.Width-1] do
            for y in [0..img.Height-1] do
                yield (x, y)
    }
    |> Seq.exists (fun (x, y) -> 
        let px = img.GetPixel(x, y)
        px.R = 255uy && px.B = 255uy & px.G = 255uy
    )

let private renderSector (outDir: string) (sourceName: string) (sourceImage: Image) (x: int, y: int) (sectorSize: int) : bool SplitterResult =

    let format n = n.ToString().PadLeft(2, '0')
    let filename = Path.Combine(outDir, $"{sourceName}-{format x}-{format y}.png")

    printfn $"Checking {filename}..."

    use sectorImg = new Bitmap(sectorSize, sectorSize)
    let g = Graphics.FromImage(sectorImg)
    let destRect = new Rectangle(0, 0, sectorSize, sectorSize) // whole sector
    let srcRect = new Rectangle((x-1) * sectorSize, (y-1) * sectorSize, sectorSize, sectorSize)

    g.DrawImage(sourceImage, destRect, srcRect, GraphicsUnit.Pixel)

    // Don't save blank sectors
    if hasAnyWhitePixels sectorImg then
        sectorImg.Save filename
        printfn $"Rendered {filename}."
        Ok true
    else
        printfn $"Skipped blank sector {filename}."
        Ok false

let private getCellCoordinates (sourceWidth: int, sourceHeight: int) (sectorSize: int) : (int * int) list =
    let columns = (float sourceWidth / float sectorSize) |> Math.Ceiling |> int
    let rows = (float sourceHeight / float sectorSize) |> Math.Ceiling |> int
    
    seq {
        for x in [1..columns] do
            for y in [1..rows] do
                yield (x, y)
    }
    |> Seq.toList

let split (workingDir: string) (source: string) (sectorSize: int) : int SplitterResult =
    Path.Combine(workingDir, source)
    |> loadSourceImage
    |> Result.bind (fun sourceImage ->
        let sourceName = Path.GetFileNameWithoutExtension source 
        let outDir = Path.Combine(workingDir, sourceName)

        createOutDirIfMissing outDir
        |> Result.bind (fun _ -> 
            getCellCoordinates (sourceImage.Width, sourceImage.Height) sectorSize
            |> List.fold 
                (fun acc cell -> 
                    acc
                    |> Result.bind (fun sectorCount -> 
                        renderSector outDir sourceName sourceImage cell sectorSize
                        |> Result.map (function | true -> sectorCount + 1 | _ -> sectorCount)                    
                    )
                ) 
                (Ok 0)
        )
    )

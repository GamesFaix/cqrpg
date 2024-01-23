module Splitter

open System
//open System.Drawing
open System.IO
open SixLabors.ImageSharp
open SixLabors.ImageSharp.PixelFormats
//open SixLabors.ImageSharp.Drawing.Processing;
open SixLabors.ImageSharp.Processing

type private Image = Image<Rgba32>
type SplitterResult<'data> = Result<'data, string>

//let private font = new Font("Consolas", 6f)

let private loadSourceImage (source: string) : Image SplitterResult =
    if not <| File.Exists source then
        Error $"Source file not found: {source}"
    else
        use fs = new FileStream(source, FileMode.Open, FileAccess.Read)
        let img = Image.Load<Rgba32> fs
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

let private saveSectorIfNotBlank (sectorImage: Image) (filename: string) : bool SplitterResult =
    let hasAnyWhitePixels (img: Image) : bool =
        seq {
            for x in [0..img.Width-1] do
                for y in [0..img.Height-1] do
                    yield (x, y)
        }
        |> Seq.exists (fun (x, y) -> 
            let px = img[x, y]
            px.R = 255uy && px.B = 255uy && px.G = 255uy
        )
    
    // Don't save blank sectors
    if hasAnyWhitePixels sectorImage then
        sectorImage.Save filename
        printfn $"Rendered {filename}."
        Ok true
    else
        printfn $"Skipped blank sector {filename}."
        Ok false

let private rectIntersection (r1: Rectangle) (r2: Rectangle) : Rectangle option =
    let left = Math.Max(r1.Left, r2.Left)
    let right = Math.Min(r1.Right, r2.Right)
    let top = Math.Max(r1.Top, r2.Top)
    let bottom = Math.Min(r1.Bottom, r2.Bottom)
    let h = bottom - top
    let w = right - left

    if (h <= 0 || w <= 0) then None
    else Some <| Rectangle (left, top, w, h)

let private renderSector (outDir: string) (sourceName: string) (sourceImage: Image) (x: int, y: int) (sectorSize: int) : bool SplitterResult =

    let format n = n.ToString().PadLeft(2, '0')
    let filename = Path.Combine(outDir, $"{sourceName}-{format x}-{format y}.png")

    printfn $"Checking {filename}..."

    let srcRect = 
        Rectangle((x-1) * sectorSize, (y-1) * sectorSize, sectorSize, sectorSize)
        |> rectIntersection sourceImage.Bounds

    match srcRect with
    | Some rect ->
        use sectorImg = sourceImage.Clone(fun i -> i.Crop rect |> ignore)

        //let g = Graphics.FromImage(sectorImg)
        //g.DrawString($"{sourceName}\n({x},{y})", font, Brushes.Gainsboro, PointF(5f, 5f))

        saveSectorIfNotBlank sectorImg filename        
    | None ->
        Error "Sector rectangle was completely outside image bounds."

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

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

let private renderSector (outDir: string) (sourceName: string) (sourceImage: Image) (x: int, y: int) : unit SplitterResult =

    let format n = n.ToString().PadLeft(2, '0')
    let filename = Path.Combine(outDir, $"{sourceName}-{format x}-{format y}.png")

    printfn $"rendering {filename}"

    Ok ()

let private getCellCoordinates (sourceWidth: int, sourceHeight: int) (sectorSize: int) : (int * int) list =
    let columns = (float sourceWidth / float sectorSize) |> Math.Ceiling |> int
    let rows = (float sourceHeight / float sectorSize) |> Math.Ceiling |> int
    
    seq {
        for x in [1..columns] do
            for y in [1..rows] do
                yield (x, y)
    }
    |> Seq.toList

let split (workingDir: string) (source: string) (sectorSize: int) : unit SplitterResult =
    Path.Combine(workingDir, source)
    |> loadSourceImage
    |> Result.bind (fun sourceImage ->
        let sourceName = Path.GetFileNameWithoutExtension source 
        let outDir = Path.Combine(workingDir, sourceName)

        createOutDirIfMissing outDir
        |> Result.bind (fun _ -> 
            getCellCoordinates (sourceImage.Width, sourceImage.Height) sectorSize
            |> List.fold 
                (fun _ cell -> renderSector outDir sourceName sourceImage cell) 
                (Ok ())
        )
    )

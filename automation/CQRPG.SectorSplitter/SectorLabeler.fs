module SectorLabeler

open SixLabors.Fonts;
open SixLabors.ImageSharp
open SixLabors.ImageSharp.Drawing.Processing;
open SixLabors.ImageSharp.Processing
open Model

let private fontName = "Consolas"
let private fontSize = 24f

let private getFontFamily () : FontFamily SplitterResult =
    match SystemFonts.TryGet(fontName) with
    | true, f -> Ok f
    | _ -> Error $"Font family {fontName} not found"

let private getFont () : Font SplitterResult =
    getFontFamily()
    |> Result.map (fun ff -> ff.CreateFont(fontSize, FontStyle.Regular))

let private getFontAndTextOptions () =
    getFont()
    |> Result.map (fun f ->
        let o = TextOptions(f)
        o.Dpi <- 300f
        f, o
    )

let drawLabel (mapName: string) (x: int, y: int) (image: Image) : Image SplitterResult =
    getFontAndTextOptions()
    |> Result.map (fun (f, o) -> 
        let text = $"{mapName}\n({x},{y})"

        let textSize = TextMeasurer.MeasureSize(text, o)

        let textOrigin = 
            PointF(
                24f, 
                24f
            )

        image.Clone(fun i -> 
            i.DrawText(
                text, 
                f, 
                Color.Gray,
                textOrigin
            )
            |> ignore
        )
    )
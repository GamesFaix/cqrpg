module PdfBundler

open System.IO
open PdfSharp.Pdf
open PdfSharp.Drawing
open Model

let private pageWidth = XUnit.FromInch 8.5
let private pageHeight = XUnit.FromInch 11
let private sectorSize = XUnit.FromInch 8
let private margin = XUnit.FromInch 0.25

let bundleSectorsToPdf (workingDir: string) (source:string) : unit SplitterResult =
    let name = Path.GetFileNameWithoutExtension source
    let sectorsDir = Path.Combine(workingDir, name)
    let files = Directory.EnumerateFiles sectorsDir
    let pdfPath = Path.Combine(workingDir, $"{name}-sectors.pdf")
    
    printfn "Bundling sector PDF..."
    try
        use doc = new PdfDocument()
        for f in files do
            printfn "Adding page for %s" f
            let page = doc.AddPage()
            page.Width <- pageWidth
            page.Height <- pageHeight

            use img = XImage.FromFile f
            let g = XGraphics.FromPdfPage page
            g.DrawImage(img, margin, margin, sectorSize, sectorSize)

        doc.Save pdfPath
        |> Ok

    with 
    | e -> Error e.Message
    
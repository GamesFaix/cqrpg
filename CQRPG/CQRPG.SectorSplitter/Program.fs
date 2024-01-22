
open System
open Argu

type CliArguments =
    | Source of string
    | Out_Dir of string
    | Sector_Size of int

    interface IArgParserTemplate with
        member s.Usage =
            match s with
            | Source _ -> "specify a source image."
            | Out_Dir _ -> "specify an output directory."
            | Sector_Size _ -> "specify a sector size, in pixels."

let parser = ArgumentParser.Create<CliArguments>(programName = "sector-splitter.exe")

[<EntryPoint>]
let main (args: string array) =
    try
        let results = parser.Parse args

        let all = results.GetAllResults()

        printfn "%A" all
    with
    | :? ArguParseException as e -> 
        printfn "%A" e

    0

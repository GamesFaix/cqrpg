open Argu

type CliArguments =
    | Working_Dir of string
    | Source of string
    | Sector_Size of int

    interface IArgParserTemplate with
        member s.Usage =
            match s with
            | Working_Dir _ -> "specify a working directory."
            | Source _ -> "specify a source image."
            | Sector_Size _ -> "specify a sector size, in pixels."

let parser = ArgumentParser.Create<CliArguments>(programName = "sector-splitter.exe")

let parse args = 
    let results = parser.Parse args
    let parsed = {|
        WorkingDir = results.GetResult(Working_Dir)
        Source = results.GetResult(Source)
        SectorSize = results.GetResult(Sector_Size)
    |}
    printfn "%A" parsed
    parsed

[<EntryPoint>]
let main (args: string array) =
    try
        let parsedArgs = parse args

        let result = Splitter.split parsedArgs.WorkingDir parsedArgs.Source parsedArgs.SectorSize

        match result with
        | Ok () ->
            printfn "Done"
        | Error e ->
            printfn "Failed: %A" e

    with
    | :? ArguParseException as e -> 
        printfn "%A" e

    0

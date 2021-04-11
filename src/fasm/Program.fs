// Learn more about F# at http://docs.microsoft.com/dotnet/fsharp
open System
open Argu
open Argu.ArguAttributes
open FileSystem
open Disassembly

type Cmd =
    | [<Mandatory; MainCommand; AltCommandLine("-s")>]Source of string
    | [<AltCommandLine("-c")>] Console
    | [<AltCommandLine("-o")>] Output of string
    | [<AltCommandLine("-w")>] Watch
    | [<AltCommandLine("-p")>] Platform of Disassembly.Platform
    | [<AltCommandLine("-l")>] Language of Disassembly.Language
    

    interface Argu.IArgParserTemplate with
        member this.Usage =
            match this with
            | Source _ -> "the source fsx file"
            | Console -> "Output to console"
            | Output _ -> "The output file" 
            | Watch -> "Watch mode"
            | Platform _ -> "The platform for disassembly"
            | Language _ -> "The output language asm/il"

type Source =
    | Script of string
    | Assembly of string

let shouldCompile = function
    | Script _ -> true
    | Assembly _ -> false

[<EntryPoint>]
let main argv =
    let cmd =ArgumentParser<Cmd>().ParseCommandLine(argv) 

    let source = 
        let path = cmd.GetResult(Source)
        let src =
            if IO.Path.IsPathRooted path then
                path
            else
                Environment.CurrentDirectory </> path

        if IO.Path.GetExtension(src) =  ".dll" then
            Assembly src
        else
            Script src


    let binDir =
        match source with
        | Script src
        | Assembly src -> dir src </> "bin"

    if shouldCompile source && not (existdir binDir) then
        mkdir binDir

    let asmPath =
        match source with
        | Script src -> binDir </> filename src + ".dll"
        | Assembly src -> src

    let platform = 
        match cmd.TryGetResult Platform with
        | Some p -> p
        | None ->
            if Environment.Is64BitProcess then
                X64
            else
                X86

    let language =
        match cmd.TryGetResult Language with
        | Some l -> l
        | _ -> Asm

    let run() =
        match source with
        | Script src ->
            printfn $"Source: %s{src}"
            printfn "Compilation"
            Compilation.compile src asmPath
        | Assembly src ->
            printfn $"Source: %s{src}"

        let out =
            match cmd.TryGetResult Output with
            | Some out -> out
            | None ->
                let ext = 
                    match language with      
                    | Asm -> ".asm"
                    | IL -> ".il"
                match source with
                | Script src 
                | Assembly src -> dir src </> filename src + ext


                

        printfn "Disassembly"
        

        if cmd.Contains Console then
            Disassembly.decompileToConsole asmPath language platform
        else
            Disassembly.decompileToFile asmPath out language platform

    if cmd.Contains Watch then
        run()
        let dir,ext =
            match source with
            | Script src
            | Assembly src -> dir src, ext src
        use watcher = new IO.FileSystemWatcher(dir, "*" + ext, EnableRaisingEvents = true)

        watcher.Changed |> Event.add (fun _ -> run())

        use signal = new System.Threading.ManualResetEvent(false)

        Console.CancelKeyPress |> Event.add (fun _ -> signal.Set() |> ignore)

        signal.WaitOne() |> ignore
    else
        run()

    0 // return an integer exit code
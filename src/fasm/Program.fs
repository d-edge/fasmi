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


[<EntryPoint>]
let main argv =
    let cmd =ArgumentParser<Cmd>().ParseCommandLine(argv) 
    let src = cmd.GetResult(Source)
    let binDir = 
        let d = dir src </> "bin"
        if IO.Path.IsPathRooted d then
            d
        else
            Environment.CurrentDirectory </> d

    if not (existdir binDir) then
        mkdir binDir
    let asmPath = binDir </> filename src + ".dll"

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
        printfn $"Source: %s{src}"
        printfn "Compilation"
        Compilation.compile src asmPath

        let out =
            match cmd.TryGetResult Output with
            | Some out -> out
            | None ->
                let ext = 
                    match language with      
                    | Asm -> ".asm"
                    | IL -> ".il"
                dir src </> filename src + ext

        printfn "Disassembly"
        

        if cmd.Contains Console then
            Disassembly.decompileToConsole asmPath language platform
        else
            Disassembly.decompileToFile asmPath out language platform

    if cmd.Contains Watch then
        run()
        use watcher = new IO.FileSystemWatcher(dir src, "*.fsx", EnableRaisingEvents = true)
        watcher.Changed |> Event.add (fun _ -> run())

        use signal = new System.Threading.ManualResetEvent(false)

        Console.CancelKeyPress |> Event.add (fun _ -> signal.Set() |> ignore)

        signal.WaitOne() |> ignore
    else
        run()

    0 // return an integer exit code
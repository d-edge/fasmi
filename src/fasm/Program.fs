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
            | Source _ -> "the source fsx or dotnet assembly file"
            | Console -> "output to console"
            | Output _ -> "specifiy the output file" 
            | Watch -> "run in watch mode"
            | Platform _ -> "specify the platform for disassembly"
            | Language _ -> "specify the output language (asm/il)"

type Source =
    | Script of string
    | Assembly of string

let shouldCompile = function
    | Script _ -> true
    | Assembly _ -> false

let help = """fasm                                F# -> ASM disassembler
----------------------------------------------------------
copyright D-EDGE 2021
Inspired from https://sharplab.io/ code by Andrey Shchekin
----------------------------------------------------------
"""

[<EntryPoint>]
let main argv =
    
    let processName = 
        let name = IO.Path.GetFileNameWithoutExtension (Diagnostics.Process.GetCurrentProcess().MainModule.FileName )
        if String.Equals(name, "dotnet", StringComparison.OrdinalIgnoreCase) then
            "dotnet fasm"
        else
            "fasm"



    let parser =ArgumentParser<Cmd>( programName = processName)
    try
        let cmd = parser.ParseCommandLine(argv)
        

        let source = 
            let path = cmd.GetResult(Source)
            let src =
                if IO.Path.IsPathRooted path then
                    path |> IO.Path.GetFullPath
                else
                    Environment.CurrentDirectory </> path |> IO.Path.GetFullPath

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

        let logf fmt =  
            if cmd.Contains Console then
                Printf.kprintf (fun _ -> ()) fmt
            else
                Printf.kprintf (printfn "%s") fmt

        logf $"%s{help}"
        let run() =

            match source with
            | Script src ->
                logf $"Source: %s{src}"
                logf "Compilation" 
                Compilation.compile src asmPath
            | Assembly src ->
                logf $"Source: %s{src}"

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


            logf "Disassembly"
            

            if cmd.Contains Console then
                Disassembly.decompileToConsole asmPath language platform
            else
                Disassembly.decompileToFile asmPath out language platform

        if cmd.Contains Watch then
            run()
            let dir, filter =
                match source with
                | Script src
                | Assembly src -> dir src, filenameExt src
            use watcher = new IO.FileSystemWatcher(dir, filter, EnableRaisingEvents = true)

            watcher.Changed |> Event.add (fun _ -> run())

            use signal = new System.Threading.ManualResetEvent(false)

            Console.CancelKeyPress |> Event.add (fun _ -> signal.Set() |> ignore)

            signal.WaitOne() |> ignore
        else
            run()
        0 // return an integer exit code
    with
    | :? Argu.ArguParseException ->
        printfn $"%s{help}"
        printfn $"%s{parser.PrintUsage()}"
        1 // return integer exit code for error
module Compilation
open System
open FSharp.Compiler.SourceCodeServices
open FileSystem

let net50Attr = """
namespace Microsoft.BuildSettings
[<System.Runtime.Versioning.TargetFrameworkAttribute(".NETCoreApp,Version=v5.0", FrameworkDisplayName="")>]
do ()
"""
let net50AttrName = "Net50AssemblyAttr.fs"

let ensureNet5Attr asmPath =
    let filePath = dir asmPath </> net50AttrName
    if not (IO.File.Exists filePath) then
        IO.File.WriteAllText(filePath, net50Attr)
    filePath


let compile (path: string) (asmPath: string) = 
    let checker = FSharpChecker.Create(keepAssemblyContents = true)

    let net50Path = @"C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\5.0.0\ref\net5.0\"

    let attrfile = ensureNet5Attr asmPath
    
    let diag,_ = 
        checker.Compile([| "fsc.exe"
                           "-o"; asmPath;
                           "-a"; path
                           "-a"; attrfile
                           "--debug:portable"
                           "--noframework"
                           "--targetprofile:netcore"
                           "--langversion:preview"
                           "--define:NET"
                           "--define:NET5_0"
                           "--define:NETCOREAPP"
                           "--define:NET5_0_OR_GREATER"
                           "--define:NETCOREAPP1_0_OR_GREATER"
                           "--define:NETCOREAPP1_1_OR_GREATER"
                           "--define:NETCOREAPP2_0_OR_GREATER"
                           "--define:NETCOREAPP2_1_OR_GREATER"
                           "--define:NETCOREAPP2_2_OR_GREATER"
                           "--define:NETCOREAPP3_0_OR_GREATER"
                           "--define:NETCOREAPP3_1_OR_GREATER"
                           "--optimize+"
                           for f in IO.Directory.EnumerateFiles(net50Path,"*.dll") do
                                $"-r:{f}"
                            |])
        |> Async.RunSynchronously

    for d in diag do
        printfn $"{d}"

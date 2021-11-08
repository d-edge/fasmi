module Compilation
open System
open FileSystem
open FSharp.Compiler.CodeAnalysis

// the Assembly attribute to build output as net5.0

let netAttr =
    #if NET6_0
    """
namespace Microsoft.BuildSettings
[<System.Runtime.Versioning.TargetFrameworkAttribute(".NETCoreApp,Version=v6.0", FrameworkDisplayName="")>]
do ()
"""

#else
    """
namespace Microsoft.BuildSettings
[<System.Runtime.Versioning.TargetFrameworkAttribute(".NETCoreApp,Version=v5.0", FrameworkDisplayName="")>]
do ()
"""

#endif

let netAttrName = "Net50AssemblyAttr.fs"

// check the net5.0 assembly attribute file exists or create it
let ensureNet5Attr asmPath =
    let filePath = dir asmPath </> netAttrName
    if not (IO.File.Exists filePath) then
        IO.File.WriteAllText(filePath, netAttr)
    filePath


/// compile given script as an assembly
let compile (path: string) (asmPath: string) = 
    let checker = FSharpChecker.Create(keepAssemblyContents = true)

    // fin net5.0 assembly path
    let net50Path = 
        let  runtimeDir = System.Runtime.InteropServices.RuntimeEnvironment.GetRuntimeDirectory()
#if NET6_0
        IO.Path.GetFullPath(runtimeDir </> "../../../packs/Microsoft.NETCore.App.Ref/6.0.0/ref/net6.0/")
#else
        IO.Path.GetFullPath(runtimeDir </> "../../../packs/Microsoft.NETCore.App.Ref/5.0.0/ref/net5.0/")
#endif
        
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
                           "--define:NETCOREAPP"
                           "--define:NET5_0"
                           "--define:NET5_0_OR_GREATER"
                           #if NET6_0
                           "--define:NET6_0"
                           "--define:NET6_0_OR_GREATER"
                           #endif
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

    // output compilatoin errors
    for d in diag do
        printfn $"{d}"

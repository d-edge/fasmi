module Compilation
open System
open FileSystem
open FSharp.Compiler.CodeAnalysis

// the Assembly attribute to build output as net5.0/net6.0/net7.0

let netAttr =
#if NET7_0
    """
namespace Microsoft.BuildSettings
[<System.Runtime.Versioning.TargetFrameworkAttribute(".NETCoreApp,Version=v7.0", FrameworkDisplayName="")>]
do ()
"""
#endif
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

let netAttrName = "NetAssemblyAttr.fs"

// check the net5.0/net6.0/net7.0 assembly attribute file exists or create it
let ensureNetAttr asmPath =
    let filePath = dir asmPath </> netAttrName
    if not (IO.File.Exists filePath) then
        IO.File.WriteAllText(filePath, netAttr)
    filePath


/// compile given script as an assembly
let compile (path: string) (asmPath: string) = 
    let checker = FSharpChecker.Create(keepAssemblyContents = true)

    // find .net assembly path
    let netPath = 
        let  runtimeDir = System.Runtime.InteropServices.RuntimeEnvironment.GetRuntimeDirectory()
#if NET7_0
        let packDir = 
            IO.Directory.GetDirectories(runtimeDir </> "../../../packs/Microsoft.NETCore.App.Ref/", "7.0.*")
            |> Seq.max
        IO.Path.GetFullPath(packDir </> "ref/net7.0")
#endif
#if NET6_0
        let packDir = 
            IO.Directory.GetDirectories(runtimeDir </> "../../../packs/Microsoft.NETCore.App.Ref/", "6.0.*")
            |> Seq.max
        IO.Path.GetFullPath(packDir </> "ref/net6.0")
#else
        IO.Path.GetFullPath(runtimeDir </> "../../../packs/Microsoft.NETCore.App.Ref/5.0.0/ref/net5.0/")
#endif
        
    let attrfile = ensureNetAttr asmPath
    
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
                           #if NET7_0
                           "--define:NET7_0"
                           "--define:NET7_0_OR_GREATER"
                           #endif
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
                           for f in IO.Directory.EnumerateFiles(netPath,"*.dll") do
                                $"-r:{f}"
                            |])
        |> Async.RunSynchronously

    // output compilatoin errors
    for d in diag do
        printfn $"{d}"

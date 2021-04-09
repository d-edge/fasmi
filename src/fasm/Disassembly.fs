module Disassembly
open System
open Iced.Intel
open System.Diagnostics
open System.Runtime.CompilerServices
open Microsoft.Diagnostics.Runtime
open System.Reflection
open System.Runtime.Loader
open System.IO

type Platform =
    | X86
    | X64

module Platform =
    let bitness = function
        | X86 -> 32
        | X64 -> 64



type CustomAssemblyLoadContext(shouldShareAssembly: AssemblyName -> bool) =
    inherit AssemblyLoadContext(isCollectible = true)
    override this.Load(assemblyName: AssemblyName) =
        let name = if isNull assemblyName.Name then "" else assemblyName.Name
        if (name = "netstandard" || name = "mscorlib" || name.StartsWith("System.") || shouldShareAssembly(assemblyName)) then
            Assembly.Load(assemblyName);
        else
            base.LoadFromAssemblyPath(Path.Combine(AppContext.BaseDirectory, assemblyName.Name + ".dll"));
    interface IDisposable with
        member this.Dispose() = base.Unload()

let formatterOptions = FormatterOptions(
                            HexPrefix = "0x",
                            HexSuffix = null,
                            UppercaseHex = false,
                            SpaceAfterOperandSeparator = true)
        
let titleCase (s:String) =
    if s.Length > 0 then
        s.Substring(0,1).ToUpperInvariant() + s.Substring(1)
    else
        s

let decompile asmPath (writer: TextWriter) platform =
    use dt = DataTarget.AttachToProcess(Process.GetCurrentProcess().Id, false)
    use runtime = dt.ClrVersions.[0].CreateRuntime()
    use ctx = new CustomAssemblyLoadContext(fun _ -> true)

    let asm = ctx.LoadFromAssemblyPath(asmPath)

    let ty = asm.GetType(titleCase (asm.GetName().Name))
    

    let decompileMethod (mthinfo: MethodBase) =

        runtime.FlushCachedData()
        let h = mthinfo.MethodHandle
        try
            RuntimeHelpers.PrepareMethod(h)
        with
        | ex -> failwithf $"Failed to prepare: %s{mthinfo.DeclaringType.FullName}%s{ mthinfo.Name}"

        let getBytes (regions: HotColdRegions) =
            let span = ReadOnlySpan<byte>((nativeint regions.HotStart).ToPointer(), int regions.HotSize)
            span.ToArray()

        let clrmth = runtime.GetMethodByHandle(uint64 (h.Value.ToInt64()))
        if not (isNull clrmth) then

            if clrmth.HotColdInfo.HotSize > 0u && clrmth.HotColdInfo.HotStart <> UInt64.MaxValue then
                writer.WriteLine $""
                writer.WriteLine $"%s{clrmth.Signature}"
                writer.Flush()

                let bytes = getBytes clrmth.HotColdInfo
                let address = clrmth.HotColdInfo.HotStart

                let decoder = Decoder.Create(Platform.bitness platform,bytes)
                decoder.IP <- address
                let formatter =
                        IntelFormatter(formatterOptions,
                                        { new ISymbolResolver with 
                                            member _.TryGetSymbol(inst, _,_,addr, _, result) =
                                                if addr >= address && addr < address + uint64 clrmth.HotColdInfo.HotSize then
                                                    result <- SymbolResult(addr, $"L%04x{addr-address}")
                                                    true
                                                else
                                                    let callmth = runtime.GetMethodByInstructionPointer(addr)
                                                    if isNull callmth then
                                                        result <- SymbolResult()
                                                        false
                                                    else
                                                        result <- SymbolResult(addr, callmth.Signature)
                                                        true

                                             })

                let out = StringOutput()
                for inst in decoder do
                    formatter.Format(&inst, out)
                    writer.WriteLine $"L%04x{inst.IP - address}: %s{out.ToStringAndReset()}"
                    writer.Flush()

    let getAllMethods (ty: Type) =
        [ yield! ty.GetConstructors() |> Seq.cast<MethodBase> 
          yield! ty.GetMethods() |> Seq.cast<MethodBase>

        ]


    for mth in getAllMethods ty do
        if mth.DeclaringType <> typeof<obj> then
            if not mth.IsGenericMethodDefinition && not mth.DeclaringType.IsGenericTypeDefinition then
                decompileMethod mth

    for sty in ty.GetNestedTypes() do
        for mth in getAllMethods sty do
            if mth.DeclaringType <> typeof<obj> then
                if not mth.IsGenericMethodDefinition && not mth.DeclaringType.IsGenericTypeDefinition then
                    decompileMethod mth

let decompileToFile asmPath outPath platform =
    use w = File.CreateText(outPath)
    decompile asmPath w platform


let decompileToConsole asmPath platform  =
    use s = Console.OpenStandardOutput()
    use w = new IO.StreamWriter(s)
    decompile asmPath w platform

module FileSystem
open System.IO

let dir path = Path.GetDirectoryName(path:string)
let filename path = Path.GetFileNameWithoutExtension(path: string)
let filenameExt path = Path.GetFileName(path: string)
let existdir path = Directory.Exists(path)
let mkdir path = Directory.CreateDirectory(path) |> ignore
let ext path = Path.GetExtension(path: string)
let (</>) x y = Path.Combine(x,y)

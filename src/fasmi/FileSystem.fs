module FileSystem
open System.IO

// a bunch of shortcuts Path functions

/// get the directory from path
let dir path = Path.GetDirectoryName(path:string)

/// get the filename without extension
let filename path = Path.GetFileNameWithoutExtension(path: string)

// get the filename extension
let ext path = Path.GetExtension(path: string)

// get the filename with extension
let filenameExt path = Path.GetFileName(path: string)

// check whether directory exists
let existdir path = Directory.Exists(path)

// create directory
let mkdir path = Directory.CreateDirectory(path) |> ignore

// ensure the directory exists
let ensuredir path =
    if not (existdir path) then
        mkdir path

// combine paths
let (</>) x y = Path.Combine(x,y)

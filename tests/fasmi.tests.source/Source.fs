module Source

let inc x = x+1

let abs x =
    if x >= 0 then
        x
    else
        -x

let toString (x: int) =
    x.ToString()

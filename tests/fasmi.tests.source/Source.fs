module Source

let inc x = x+1

let abs x =
    if x >= 0 then
        x
    else
        -x

let toString (x: int) =
    x.ToString()

type HelloWriter(x) =
    member this.Hello() = System.Console.WriteLine $"hello {x}"

let sayHello (x: int) =
    HelloWriter(x).Hello()

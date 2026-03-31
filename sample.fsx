#r "src/r-bridge/RBridge/bin/Debug/net10.0/r-bridge.dll"

open RBridge

let show label v = printfn "%s => %A" label v

let loc = EngineHost.tryFindSystemR() |> Option.get

printfn "found R at %A" loc
let engine = RInterop.initialise()
// simple evaluation
show "1+1" (RInterop.evaluate "1+1")
// assign with exec and then evaluate
RInterop.exec "x <- 100"
show "x" (RInterop.evaluate "x")
// use setSymbol/getSymbol (getSymbol currently returns RExpression)
RInterop.setSymbol "z" (RValue.RExpression "sin(pi/2)")


show "lookup z" (RInterop.getSymbol "z")
// side‑effect operation
RInterop.exec "print('hello from R')"
// cleanup
RInterop.shutdown()

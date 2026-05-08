#r "src/r-bridge/RBridge.Extensions/bin/Debug/net10.0/RBridge.dll"
#r "src/r-bridge/RBridge.Extensions/bin/Debug/net10.0/RBridge.Extensions.dll"

open RBridge
open RBridge.Extensions

let show label v = printfn "%s => %A" label v

// 1. Find and initialize R
let loc = EngineHost.tryFindSystemR() |> Option.get
printfn "found R at %A" loc

let engine = RInterop.initialiseAt loc Logging.console

engine.invoke(fun e -> e.Api.symbol.mkString.Invoke "Cool")

let globalEnv = Environment.globalEnv engine

let result1 = Evaluate.tryEval "sqrt(49)" globalEnv engine |> Result.toOption |> Option.get
let t1 = SymbolicExpression.getType engine result1
show "Type of sqrt(65)" t1

Extract.extractFloatArray engine result1

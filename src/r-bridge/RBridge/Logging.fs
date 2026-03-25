namespace RBridge

open System

module Logging =
    let logf fmt = Printf.kprintf (fun s -> Console.WriteLine(s)) fmt
    let debug fmt = Printf.kprintf (fun s -> Console.WriteLine(sprintf "[DEBUG] %s" s)) fmt
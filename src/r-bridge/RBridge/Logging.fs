namespace RBridge

open System

module Logging =

        type Logger = {
            debug: string -> unit
            info: string -> unit
        }

        let console = {
            debug = ignore
            info = System.Console.WriteLine
        }
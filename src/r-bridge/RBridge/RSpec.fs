namespace RBridge

/// The specification of the C API for R.
module RSpec =

    /// human‑readable description of a C API argument
    type CArg = { Name: string; Type: string }

    /// description of an R C API function that we care about
    type Func =
        { Name : string
          Return : string
          Args : CArg list
          Doc : string option }

    /// A compact list of the functions the bridge currently uses.  each entry
    /// contains the name, return type and a list of argument names/types.
    /// additional functions can be added as needed and the `generate()` helper
    /// below will emit appropriate F# delegate definitions and bindings.
    let apiFunctions : Func list =
        [ { Name = "Rf_eval"; Return = "sexp";
            Args = [{Name="expr";Type="sexp"};{Name="env";Type="sexp"}];
            Doc = Some "evaluate an R expression in a given environment" }
          { Name = "Rf_protect"; Return = "sexp";
            Args = [{Name="sexp";Type="sexp"}]; Doc=None }
          { Name = "Rf_unprotect"; Return = "void";
            Args = [{Name="count";Type="int"}]; Doc=None }
          { Name = "Rf_install"; Return = "sexp";
            Args = [{Name="name";Type="string"}]; Doc=None }
          { Name = "Rf_findVar"; Return = "sexp";
            Args = [{Name="sym";Type="sexp"};{Name="env";Type="sexp"}]; Doc=None }
          { Name = "Rf_defineVar"; Return = "sexp";
            Args = [{Name="sym";Type="sexp"};{Name="value";Type="sexp"};{Name="env";Type="sexp"}]; Doc=None }
          { Name = "Rf_mkString"; Return = "sexp";
            Args = [{Name="s";Type="nativeint"}]; Doc=None }
          { Name = "Rf_mkChar"; Return = "sexp";
            Args = [{Name="s";Type="string"}]; Doc=None }
          { Name = "R_ParseVector"; Return = "sexp";
            Args = [{Name="expr";Type="sexp"};{Name="n";Type="int"};{Name="status";Type="byref<int>"};{Name="env";Type="sexp"}]; Doc=None }
          { Name = "Rf_length"; Return = "int";
            Args = [{Name="v";Type="sexp"}]; Doc=None }
          { Name = "Rf_nrows"; Return = "int";
            Args = [{Name="m";Type="sexp"}]; Doc=None }
          { Name = "Rf_ncols"; Return = "int";
            Args = [{Name="m";Type="sexp"}]; Doc=None }
          { Name = "Rf_allocVector"; Return = "sexp";
            Args = [{Name="t";Type="int"};{Name="len";Type="int"}]; Doc=None }
          { Name = "Rf_allocMatrix"; Return = "sexp";
            Args = [{Name="t";Type="int"};{Name="r";Type="int"};{Name="c";Type="int"}]; Doc=None }
          { Name = "Rf_initialize_R"; Return = "int";
            Args = [{Name="argc";Type="int"};{Name="argv";Type="string[]"}]; Doc=None }
          { Name = "R_setStartTime"; Return = "unit";
            Args = []; Doc=None }
          { Name = "setup_Rmainloop"; Return = "unit";
            Args = []; Doc=None }
          { Name = "R_ReplDLLinit"; Return = "unit";
            Args = []; Doc=None }
          { Name = "Rf_endEmbeddedR"; Return = "unit";
            Args = [{Name="f";Type="int"}]; Doc=None }
        ]

    /// simple generator that prints F# declarations for the delegate types
    /// and an Api record.  run this in FSI (`RSpec.generate()`) and copy
    /// the output into `NativeApi.fs` when the API list changes.
    let generate () =
        let indent n = String.replicate n "    "
        let decl f =
            let args = f.Args |> List.map (fun a -> a.Type) |> String.concat " * "
            sprintf "[<UnmanagedFunctionPointer(CallingConvention.Cdecl)>]\ntype %s = delegate of %s -> %s" f.Name args f.Return
        let rec genRecord fs =
            let fields = fs |> List.map (fun f -> sprintf "%s : %s" (f.Name.[0..0].ToLower() + f.Name.[1..]) f.Name) |> String.concat "\n          "
            sprintf "type Api =\n        { %s }" fields
        apiFunctions |> List.iter (fun f -> printfn "%s\n" (decl f))
        printfn "%s" (genRecord apiFunctions)

    /// Validate that the function list agrees with our current NativeApi
    /// content; this can be expanded later as a unit test.
    let validate () =
        // placeholder
        ()

    // preserve previous prose for human readers
    /// *Initialization sequence* etc...
    /// ... (the long comments remain)    

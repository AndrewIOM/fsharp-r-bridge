namespace RBridge.Extensions

module ActivePatterns =

    open RBridge
    open RBridge.SymbolicExpression

    let internal isVector engine sexp ofType someFn =
        match SymbolicExpression.getType engine sexp with
        | t when t = ofType && Extract.getDimension engine sexp = 1 -> Some sexp
        | _ -> None

    let internal isMatrix engine sexp ofType someFn =
        match SymbolicExpression.getType engine sexp with
        | t when t = ofType && Extract.getDimension engine sexp = 2 -> Some sexp
        | _ -> None

    let internal isA engine sexp typeSexp =
        if SymbolicExpression.getType engine sexp = typeSexp then
            Some sexp
        else
            None

    // Vectors
    let (|CharacterVector|_|) engine sexp =
        isVector engine sexp SymbolicExpression.CharacterVector Extract.extractStringArray

    let (|LogicalVector|_|) engine sexp =
        isVector engine sexp SymbolicExpression.LogicalVector Extract.extractLogicalArray

    let (|IntegerVector|_|) engine sexp =
        isVector engine sexp SymbolicExpression.IntegerVector Extract.extractIntArray

    let (|RealVector|_|) engine sexp =
        isVector engine sexp SymbolicExpression.RealVector Extract.extractFloatArray

    let (|ComplexVector|_|) engine sexp =
        isVector engine sexp SymbolicExpression.ComplexVector Extract.extractComplexArray

    let (|RawVector|_|) engine (sexp: SymbolicExpression) =
        isVector engine sexp SymbolicExpression.RawVector Extract.extractRawArray
    // let (|UntypedVector|_|) engine (sexp: SymbolicExpression) =
    //     asVectorOf engine sexp SymbolicExpression.RawVector Extract.extractRawArray

    // Matricies
    let (|CharacterMatrix|_|) engine sexp =
        isMatrix engine sexp SymbolicExpression.CharacterVector Extract.extractStringMatrix

    let (|LogicalMatrix|_|) engine sexp =
        isMatrix engine sexp SymbolicExpression.LogicalVector Extract.extractLogicalMatrix

    let (|IntegerMatrix|_|) engine sexp =
        isMatrix engine sexp SymbolicExpression.IntegerVector Extract.extractIntMatrix

    let (|RealMatrix|_|) engine sexp =
        isMatrix engine sexp SymbolicExpression.RealVector Extract.extractDoubleMatrix

    let (|ComplexMatrix|_|) engine sexp =
        isMatrix engine sexp SymbolicExpression.ComplexVector Extract.extractComplexMatrix

    let (|RawMatrix|_|) engine sexp =
        isMatrix engine sexp SymbolicExpression.RawVector Extract.extractRawMatrix

    // Functions:
    let internal asFunctionOf engine sexp ofType =
        match SymbolicExpression.getType engine sexp with
        | t when t = ofType -> Some sexp
        | _ -> None

    let (|Closure|_|) engine sexp =
        asFunctionOf engine sexp SexpType.Closure

    let (|BuiltinFunction|_|) engine sexp =
        asFunctionOf engine sexp SexpType.Builtin

    let (|SpecialFunction|_|) engine sexp =
        asFunctionOf engine sexp SexpType.Special

    let (|Function|_|) engine sexp =
        match SymbolicExpression.getType engine sexp with
        | SexpType.Closure
        | SexpType.Builtin
        | SexpType.Special -> Some sexp
        | _ -> None

    // S4 / S3 objects
    let (|S4Object|_|) engine sexp =
        if S4.isS4 engine sexp then
            Classes.tryGetClass engine sexp
        else
            None
    let (|S3Object|_|) engine sexp =
        if S3.isS3 engine sexp then
            Classes.tryGetClass engine sexp
        else
            None

    // Others
    let (|Factor|_|) engine (sexp: SymbolicExpression) =
        if Factor.isFactor engine sexp then
            Some sexp
        else
            None

    let (|DataFrame|_|) engine (sexp: SymbolicExpression) =
        match SymbolicExpression.getClasses engine sexp with
        | l when Seq.contains (Some "data.frame") l -> Some sexp
        | _ -> None

    let (|Environment|_|) engine (sexp: SymbolicExpression) =
        isA engine sexp SymbolicExpression.Environment

    let (|Language|_|) engine (sexp: SymbolicExpression) =
        isA engine sexp SymbolicExpression.Language

    let (|List|_|) engine (sexp: SymbolicExpression) =
        isA engine sexp SymbolicExpression.List

    let (|PairList|_|) engine (sexp: SymbolicExpression) =
        isA engine sexp SymbolicExpression.Pairlist

    let (|Null|_|) engine (sexp: SymbolicExpression) = isA engine sexp SymbolicExpression.Nil

    let (|Symbol|_|) engine (sexp: SymbolicExpression) =
        isA engine sexp SymbolicExpression.Symbol

    let (|BuiltIn|_|) engine (sexp: SymbolicExpression) =
        isA engine sexp SymbolicExpression.Builtin

    let (|Special|_|) engine (sexp: SymbolicExpression) =
        isA engine sexp SymbolicExpression.Special

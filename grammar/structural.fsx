// Core structures

namespace FsSci

/// A grammar of statistics for use in F# scientific
/// computing.
module Base =

    /// An atomic number that forms the primary unit of
    /// other more complex types.
    module Numeric =

        let inline add (a: ^T) (b: ^T) =
            ((^T) : (static member Add : ^T * ^T -> ^T) (a, b))
        let inline sub (a: ^T) (b: ^T) =
            ((^T) : (static member Sub : ^T * ^T -> ^T) (a, b))
        let inline mul (a: ^T) (b: ^T) =
            ((^T) : (static member Mul : ^T * ^T -> ^T) (a, b))
        let inline div (a: ^T) (b: ^T) =
            ((^T) : (static member Div : ^T * ^T -> ^T) (a, b))

        let inline log (a: ^T) =
            ((^T) : (static member Logarithm : ^T -> ^T) a)
        let inline exp (a: ^T) =
            ((^T) : (static member Exponent : ^T -> ^T) a)

        let inline scale (a: ^T) (k: float) =
            ((^T) : (static member Scale : ^T * float -> ^T) (a, k))

        let inline toFloat (a: ^T) =
            ((^T) : (static member ToFloat : ^T -> float) a)
        let inline fromFloat (a: float) =
            ((^T) : (static member FromFloat : float -> ^T) a)


    /// A 2D list of numerics
    module Vector =

        let inline mean (a: ^V) =
            ((^V) : (static member Mean : ^V -> ^N) a)

        let inline scale (a: ^V) (k: float) =
            ((^V) : (static member Scale : ^V * float -> ^V) (a, k))


    /// An n-dimensional structure of numerics, which
    /// may be able to be represented as a numeric, vector, or matrix.
    module Tensor =
        let inline add (a:^T) (b:^T) =
            ((^T) : (static member Add : ^T * ^T -> ^T) (a,b))

        let inline matmul (a:^T) (b:^T) =
            ((^T) : (static member MatMul : ^T * ^T -> ^T) (a,b))

        let inline relu (a:^T) =
            ((^T) : (static member Relu : ^T -> ^T) a)

        let inline toFloat (a:^T) =
            ((^T) : (static member ToFloat : ^T -> float) a)



    module Integer = ()
    module Logical = ()
    module Factor = ()
    module Date = ()
    module Matrix = ()

    module DataFrame =

        let inline getColumn (df: ^DF) (name:string) =
            ((^DF) : (static member GetColumn : _ * _ -> _) (df, name))

        let inline getColumnNames (df: ^DF) =
            ((^DF) : (static member GetColumnNames : _ -> _) df)

        let inline rowCount (df: ^DF) =
            ((^DF) : (static member RowCount : _ -> _) df)

    // Unimplemented types:
    module Formula = ()
    module HeterogeneousList = ()
    module TimeSeries = ()
    module Distance = ()
    module ContingencyTable = ()

    // Unions that may be referenced by libraries
    // for clearer type inference.
    type Numeric<'N> = Numeric of 'N
    type Vector<'N> = Vector of 'N
    type Tensor<'N> = Tensor of 'N
    type DataFrame<'DF> = DataFrame of 'DF

    // Base helper functions (that form an effective DSL around the grammar)
    let as_data_frame df = DataFrame df

    /// Element-wise operations that enable inter-operability between
    /// base types (e.g. vector * numeric = vector).
    module Elementwise =
        let inline map f (x:^T) =
            ((^T) : (static member Map : (_ -> _) * ^T -> ^T) (f, x))

        let inline map2 (f: ^E -> ^E -> ^E) (a:^C) (b:^C) =
            ((^C) : (static member Map2 : (_ -> _ -> _) * ^C * ^C -> ^C) (f, a, b))

        // let inline map2 f (a:^T) (b:^T) =
        //     ((^T) : (static member Map2 : (_ -> _ -> _) * ^T * ^T -> ^T) (f, a, b))

    /// Element-wise operations on multi-dimensional data,
    /// for example vectors and tensors.
    module Ops =
        let inline add a b = Elementwise.map2 Numeric.add a b
        let inline sub a b = Elementwise.map2 Numeric.sub a b
        let inline mul a b = Elementwise.map2 Numeric.mul a b
        let inline div a b = Elementwise.map2 Numeric.div a b

        let inline log a   = Elementwise.map Numeric.log a
        let inline exp a   = Elementwise.map Numeric.exp a


    /// Wrappers for use in libraries that consume the
    /// declerative grammar of scientific computing.
    module Backends =

        type NumericBackend<'T> =
            { add          : 'T -> 'T -> 'T
              sub          : 'T -> 'T -> 'T
              mul          : 'T -> 'T -> 'T
              div          : 'T -> 'T -> 'T
              log          : 'T -> 'T
              exp          : 'T -> 'T
              scale        : 'T -> float -> 'T
              toFloat      : 'T -> float
              fromFloat    : float -> 'T }

        let inline numeric< ^T
            when ^T : (static member Add    : ^T * ^T -> ^T)
            and ^T : (static member Sub    : ^T * ^T -> ^T)
            and ^T : (static member Mul    : ^T * ^T -> ^T)
            and ^T : (static member Div    : ^T * ^T -> ^T)
            and ^T : (static member Logarithm    : ^T -> ^T)
            and ^T : (static member Exponent    : ^T -> ^T)
            and ^T : (static member Scale  : ^T * float -> ^T)
            and ^T : (static member ToFloat: ^T -> float)
            and ^T : (static member FromFloat: float -> ^T)>
            : NumericBackend< ^T> =
            { add         = Numeric.add
              sub         = Numeric.sub
              mul = Numeric.mul
              div = Numeric.div
              log = Numeric.log
              exp = Numeric.exp
              scale       = Numeric.scale
              toFloat     = Numeric.toFloat
              fromFloat = Numeric.fromFloat }


        type TensorBackend<'T> =
            { add    : 'T -> 'T -> 'T  }

        module TensorBackend =
            let inline tensor<'T
                when ^T: (static member Add: ^T * ^T -> ^T)> : TensorBackend<'T> =
                { add    = Tensor.add }



module StatsLibraryExample =

    open Base

    let inline toFloat n = Numeric.toFloat n

    module Likelihoods =

        let inline ssr (DataFrame df) col1 col2 =
            let x = DataFrame.getColumn df col1
            let y = DataFrame.getColumn df col2
            let diff = x - y
            Vector.mean diff

        let inline logLikelihood (DataFrame df) : float =
            // Example: mean squared error between two columns
            let x = DataFrame.getColumn df "x"
            let y = DataFrame.getColumn df "y"
            let diff = Numeric.sub x y
            Vector.mean diff * -1.  // pretend this is a log-likelihood


    let metropolis iterations proposal logL (DataFrame df0) =
        let rnd = System.Random()
        let rec loop i currentDf currentLL acc =
            if i = iterations then
                currentDf, acc
            else
                let proposedDf = proposal currentDf
                let proposedLL = logL proposedDf

                let acceptProb =
                    let delta = proposedLL - currentLL
                    exp (min 0.0 delta)

                let accepted =
                    rnd.NextDouble() < acceptProb

                let nextDf, nextLL =
                    if accepted then proposedDf, proposedLL
                    else currentDf, currentLL

                loop (i+1) nextDf nextLL (if accepted then acc+1 else acc)

        let ll0 = logL df0
        loop 0 df0 ll0 0


module ConcreteTypesExample =


    type SimpleVec = { Data : float list }

    type SimpleVec with
        static member Add(a: SimpleVec, b: SimpleVec) =
            { Data = List.map2 (+) a.Data b.Data }

        static member Sub(a: SimpleVec, b: SimpleVec) =
            { Data = List.map2 (-) a.Data b.Data }

        static member Scale(a: SimpleVec, k: float) =
            { Data = a.Data |> List.map (fun x -> x * k) }

        static member Mean(a: SimpleVec) =
            a.Data |> List.averageBy float |> float

        static member Map(f, v: SimpleVec) =
            { Data = v.Data |> List.map f }

        static member Map2(f, a: SimpleVec, b: SimpleVec) =
            { Data = List.map2 f a.Data b.Data }

        static member op_Subtraction(a,b) = SimpleVec.Sub(a,b)

    type MiniFrame =
        { Columns : Map<string, SimpleVec> }

    type MiniFrame with
        static member GetColumn(df: MiniFrame, name: string) =
            df.Columns.[name]

        static member GetColumnNames(df: MiniFrame) =
            df.Columns |> Map.keys |> Seq.toArray

        static member RowCount(df: MiniFrame) =
            df.Columns |> Map.values |> Seq.head |> fun v -> v.Data.Length

        static member SpecialMiniFn(df:MiniFrame) =
            df
    
    type System.Double with
        static member Add(a: float, b: float) : float = a + b
        static member Sub(a: float, b: float) : float = a - b
        static member Mul(a: float, b: float) : float = a * b
        static member Div(a: float, b: float) : float = a / b

        static member Logarithm(a: float) : float = System.Math.Log a
        static member Exponent(a: float) : float = System.Math.Exp a

        static member Scale(a: float, k: float) : float = a * k
        static member Mean(a: float) : float = a

        static member Map(f: float -> float, x: float) : float = f x
        static member Map2(f: float -> float -> float, a: float, b: float) : float = f a b

        static member ToFloat(a: float) : float = a
        static member FromFloat(x: float) : float = x

    let test = System.Double.Map2 ((fun a v -> a + v), 1., 2.)

    let v = { Data = [1;2;3] }
    let x = Base.Ops.add v v
    
    let v = { Data = [1;2;3] } |> Base.Vector
    let x = Base.Ops.add v (Base.Numeric 2.0)

    open Base

    Ops.mul ({ Data = [1.;2.;3.] }) 2.0   // vector * scalar
    Ops.mul 2.0 ({ Data = [1.;2.;3.] })   // scalar * vector
    Ops.log 3.0                                     // scalar
    Ops.log ({ Data = [1.;2.;3.] })       // vector
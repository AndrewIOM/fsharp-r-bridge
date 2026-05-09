namespace RBridge.Extensions

module SymbolicExpression =

    let getListItem engine index sexp =
        Lists.getListItem engine index sexp

    let getListItemByName engine name sexp =
        Lists.getListItemByName engine name sexp

    /// Get the classes associated with a symbolic expression.
    /// If inheritence is active, the child class will appear earlier
    /// than the parent class.
    let getClasses engine sexp =
        Classes.getClasses engine sexp
# R Bridge for F#

A minimal interoperability layer for working between R and F#.

This project was designed to be a lightweight replacement for the interop
layer from R.NET, which is currently not actively maintained.
It is a conceptural successor written in F#
and a functional style, rather than a fork. It provides
structural access into the R embedded API from F#, but does not include full
semantic types (as R.NET does).

The RBridge is intended to bind only to the stable public C API for R, which
is currently being defined (see: R docs [Moving into C API compliance](https://cran.r-project.org/doc/manuals/R-exts.html#Moving-into-C-API-compliance)).

## Building

Aside from a local R install (v4.5.0+), the project currently has zero .NET dependencies,
and only requires the .NET SDK.

```bash
cd src/r-bridge
dotnet build
```

## Components

There are three components:

1. **R bridge**. The bridge contains functions for locating R installs, initialising
   an R engine, and calling the C API.
2. **R bridge extensions**. Functions that wrap the R bridge API to represent the
   structural elements of R language types and functions.
3. **r-tool**. A dotnet tool that will enable setting up local R environments on a
   per-project basis (i.e. with lock files), so that reproducable analyses are possible.

In R.NET, full semantics around R types were included. However, here these concerns
have been moved to upstream libraries, primarily RProvider.

## Notes

- On intel macs, R must be initialised on the process main thread or it will crash with 'Error: C stack usage  (some big number) is too close to the limit'.

## TODO

* complete the `r-tool` so that renv environments can be
  restored before the engine starts, for any particular script or fsproj.

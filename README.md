# USD Dart

Dart FFI bindings for Pixar’s Universal Scene Description (USD). This project is a fork of Unity-Technologies/usd-unity-sdk originally developed to expose USD to C# and Unity using SWIG. Our goal is to adapt those bindings into a portable C ABI with generated Dart bindings, making it possible to open, query, and author USD stages directly from Dart and Flutter.

## Features (current and planned)
Shared library (libpxr.so / libpxr.dylib / pxr.dll) exporting a stable C ABI.
Dart bindings generated via ffigen
Stage and Prim access (pxr_stage_open, pxr_stage_get_prim, …).
Matrix math helpers and fused transform queries.
Attribute and primvar access with typed array support.
Higher-level, idiomatic Dart API wrapping the raw FFI layer.
Flutter integration for building USD viewers and tools.
...

## Status
⚠️ Really Early development.

## License
Apache 2.0, same as the original project. See [LICENSE](LICENSE) for more information.

## Acknowledgements
Based on the USD Unity SDK. Thanks to the original authors for their work making USD more accessible outside of C++.

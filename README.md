# TcAdsExtensions
Extension methods for the TwinCAT.Ads namespace for serialization/deserialization of structured types in TwinCAT PLC.

Projects:
- Simple TwinCAT PLC project
  - Global variables and structure definitions for testing
- C# console application
  - AdsConnection.cs: Wrapper for the TwinCAT.Ads.TcAdsClient object
  - AdsExtensions.cs: Extension methods (of the ISymbol interface) for automatic type resolution / marshaling of symbol values in the PLC.
  
See Program.main() for a sample client which demonstrates read/write operations on a structure, as well as event handling with some primitive types.

The extension methods were tested using structures, arrays, nested structures, arrays of structures, etc.

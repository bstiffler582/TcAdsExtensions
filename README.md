# TcAdsExtensions
Extension methods for the TwinCAT.Ads namespace for serialization/deserialization of structured types in TwinCAT PLC.

Projects:
- Simple TwinCAT PLC project
  - Global variables and structure definitions for testing
- C# console application
  - AdsConnection.cs: Wrapper for the TwinCAT.Ads.TcAdsClient object
  - AdsExtensions.cs: Extension methods (of the ISymbol interface) for automatic type resolution / marshaling of symbol values in the PLC.
  
See Program.main() for a sample client which demonstrates read/write operations on a structure, as well as event handling with some primitive types.

The extension methods were tested using structures, arrays, nested structures, arrays of structures, etc. Note that the type resolution uses reflection which can be slow for large, nested types. If speed is important, consider marshaling your classes on the C# side and using TcAdsClient.ReadAny/WriteAny:

https://docs.microsoft.com/en-us/dotnet/standard/native-interop/type-marshaling

https://infosys.beckhoff.com/english.php?content=../content/1033/tc3_adssamples_net/185255435.html&id=8833012190660457068

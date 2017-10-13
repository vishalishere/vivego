``` ini

BenchmarkDotNet=v0.10.9, OS=Windows 10 Redstone 2 (10.0.15063)
Processor=Intel Core i7-7700K CPU 4.20GHz (Kaby Lake), ProcessorCount=8
.NET Core SDK=2.0.0
  [Host]     : .NET Core 2.0.0 (Framework 4.6.00001.0), 64bit RyuJIT
  DefaultJob : .NET Core 2.0.0 (Framework 4.6.00001.0), 64bit RyuJIT


```
 |            Method |     Mean |    Error |   StdDev |
 |------------------ |---------:|---------:|---------:|
 |   SerializeSingle | 305.7 ns | 5.668 ns | 5.567 ns |
 | DeserializeSingle | 439.4 ns | 7.432 ns | 6.952 ns |

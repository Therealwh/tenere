# Benchmarks

Measured on 2026-11-19 with BenchmarkDotNet 0.15.8.

## Environment

- Windows 10 version 10.0.19045 (22H2).
- Intel Xeon CPU E5-2660 v3, 1 CPU, 10 physical / 20 logical cores.
- .NET SDK 10.0.404, runtime 10.0.15, x64 RyuJIT.
- Benchmark job: DefaultJob, Concurrent Workstation GC.

## Results

| Scenario | Mean | Error | Allocated per operation |
| --- | ---: | ---: | ---: |
| FTS5 search over 10,000 clips | 4.752 ms | ±0.093 ms | 34.13 KB |
| Count 10,000 clips | 1.338 ms | ±0.027 ms | 3.48 KB |
| Three-step transformation chain | 483.5 ns | ±2.2 ns | 1.02 KB |

Search target was under 100 ms on 10,000 items. Measured result: 4.752 ms.

## Idle memory

Smoke test of the built `Tenere.exe`, measured 6 seconds after launch with a single clip in history:

- Working set: 116 MB.
- Private bytes: 70.1 MB.

The working-set target from the architecture was 25–50 MB. Current measurement exceeds it. This is a known gap, caused mainly by the WPF and .NET baseline plus the Windows 11 backdrop composition. Reduce it before making public performance claims.

## Reproduce

```powershell
$env:DOTNET_ROOT = 'C:\Temp\dotnet'
$env:PATH = 'C:\Temp\dotnet;' + $env:PATH
dotnet run -c Release --project tests/Tenere.Benchmarks -- --cli C:\Temp\dotnet\dotnet.exe --job Short
```

Raw reports are written under `BenchmarkDotNet.Artifacts/results/` and are not committed.
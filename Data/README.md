# Benchmark Results

This directory stores human-readable and machine-readable benchmark evidence.

The Visual Studio .diagsession files that may appear here are profiling artifacts. They are useful for diagnostics, but they are not the canonical result format for this benchmark laboratory.

## Canonical result set

For a published or analyzed run, prefer:

- BenchmarkDotNet Markdown (.md)
- CSV (.csv)
- JSON (.json)
- the console summary captured as text when useful

Keep the BenchmarkDotNet environment header with the results. CPU, OS, .NET runtime, FSM_API package version, and BenchmarkDotNet version are part of the evidence.

## Suggested run

From the FSM_Benchmark project:

~~~powershell
dotnet run -c Release -- --exporters GitHub,Markdown,Csv,Json
~~~

BenchmarkDotNet writes its generated results beneath BenchmarkDotNet.Artifacts/results.

Copy the resulting text/CSV/JSON files into a dated subdirectory here, for example:

~~~text
Data/
└── 2026-09-24-Windows-Run/
    ├── *.md
    ├── *.csv
    └── *.json
~~~

## What we will do with the results

The goal is to turn the raw benchmark matrix into a cost map:

1. fixed cost;
2. marginal cost;
3. allocation cost;
4. scaling slope;
5. non-linear breakpoints;
6. normal-path versus failure-path cost;
7. user-facing performance guidance.

Do not hand-edit the benchmark numbers. The source benchmark run is the evidence; analysis belongs in documentation derived from that evidence.
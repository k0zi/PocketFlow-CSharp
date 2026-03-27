# PocketFlow BatchNode Example

This example demonstrates the **BatchNode** concept in PocketFlow (C#) by implementing a CSV processor that handles large files by processing them in chunks.

## What this Example Demonstrates

- How to use `BatchNode` to process large inputs in chunks
- The three key methods of `BatchNode`:
  1. `Prepare` – Splits the CSV into row-chunks using `CsvUtils.ReadChunks` (from **SharedUtils**)
  2. `Execute` – Processes each chunk independently, computing per-chunk sales statistics
  3. `Post` – Combines all chunk results into final aggregated statistics

## Project Structure

```
BatchNode/
├── README.md
├── BatchNode.csproj          # References PocketFlow + SharedUtils
├── Program.cs                # Entry point – generates sample CSV and runs the flow
├── CsvProcessorNode.cs       # BatchNode implementation (C# port of nodes.py)
├── ShowStatsNode.cs          # Statistics display node  (C# port of ShowStats in flow.py)
└── data/
    └── sales.csv             # Auto-generated sample data (10 000 rows)
```

## How it Works

The example processes a large CSV file containing sales data (`date`, `amount`, `product`):

1. **Chunking (`Prepare`)**: `CsvProcessorNode.Prepare` calls `CsvUtils.ReadChunks` to lazily read the
   file and return a materialised list of 1 000-row chunks.
2. **Processing (`Execute`)**: Each chunk is processed in isolation to compute:
   - Total sales amount for the chunk
   - Number of transactions in the chunk
3. **Combining (`Post`)**: All per-chunk results are aggregated into final statistics stored in
   `shared["statistics"]`, and the action `"show_stats"` routes execution to `ShowStatsNode`.

## Shared Utilities

CSV chunked reading lives in the **SharedUtils** project (`CsvUtils`) and is reusable across
all PocketFlow examples.

| Method | Description |
|---|---|
| `CsvUtils.ReadChunks(path, chunkSize, hasHeader)` | Lazily yields chunks of parsed CSV rows |
| `CsvUtils.ReadAll(path, hasHeader)` | Reads all rows into a single list |

## Usage

```bash
dotnet run
```

If `data/sales.csv` does not exist it is generated automatically (10 000 rows, seeded RNG).

## Sample Output

```
Processing sales.csv in chunks...

Final Statistics:
  Total Sales:        $999,123.45
  Average Sale:       $99.91
  Total Transactions: 10,000

Total processing time: 0.0312 seconds
```

## Key Concepts Illustrated

1. **Chunk-based Processing**: `BatchNode` splits the workload via `Prepare` returning a list of
   chunks; `Execute` is called once per chunk automatically by the framework.
2. **Independent Processing**: Each `Execute` call operates on its own in-memory chunk with no
   shared mutable state between calls.
3. **Result Aggregation**: `Post` receives the full `List<object?>` of per-chunk results and merges
   them into a single statistics dictionary.
4. **Shared Utilities**: Common CSV helpers are centralised in **SharedUtils** (`CsvUtils.cs`) to
   avoid duplication across examples.


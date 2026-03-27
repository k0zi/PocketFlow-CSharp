# DocumentUpdater

Converts the PocketFlow `docs/` Markdown files into **MDC** (Cursor rule) files, placing them in `.cursor/rules/`.  
Built with [PocketFlow](https://github.com/The-Pocket/PocketFlow) (C# port).

> C# port of [`utils/update_pocketflow_mdc.py`](../../utils/update_pocketflow_mdc.py).

## How it works

The tool runs a three-node PocketFlow pipeline:

```
DiscoverFilesNode → CreateCombinedGuideNode → ConvertMdBatchNode
```

1. **DiscoverFilesNode** — scans the docs directory recursively for all `*.md` files,  
   excludes `guide.md` and `index.md` (handled separately), and creates the output directory.
2. **CreateCombinedGuideNode** — reads `guide.md` and `index.md`, prepends the  
   *Documentation First Policy* preamble, and writes `guide_for_pocketflow.mdc`  
   with `alwaysApply: true` so Cursor always loads it for `*.cs` files.
3. **ConvertMdBatchNode** — a `BatchNode` that converts each discovered Markdown file  
   into an MDC file in parallel, preserving the relative directory structure.  
   Each file has its front-matter stripped, HTML removed, and local links rewritten  
   to `mdc:` protocol references.

## Project structure

```
DocumentUpdater/
├── README.md
├── DocumentUpdater.csproj      # References PocketFlow
├── Program.cs                  # Entry point — argument parsing + flow wiring
├── MarkdownUtils.cs            # Static helpers (HTML stripping, front-matter, MDC generation)
├── DiscoverFilesNode.cs        # Node: scan docs directory
├── CreateCombinedGuideNode.cs  # Node: build guide_for_pocketflow.mdc
└── ConvertMdBatchNode.cs       # BatchNode: convert each *.md → *.mdc
```

## Getting started

Run from the **PocketFlow repository root** so the default paths resolve correctly:

```bash
# Use default docs/ and .cursor/rules/ directories
dotnet run --project src/DocumentUpdater

# Specify custom paths
dotnet run --project src/DocumentUpdater -- \
    --docs-dir  /path/to/docs \
    --rules-dir /path/to/.cursor/rules

# Show help
dotnet run --project src/DocumentUpdater -- --help
```

## CLI reference

| Flag | Default | Description |
|---|---|---|
| `--docs-dir <path>` | `./docs` | Path to the PocketFlow docs directory |
| `--rules-dir <path>` | `./.cursor/rules` | Output directory for `.mdc` files |
| `-h, --help` | | Print usage |

## Output

```
.cursor/rules/
├── guide_for_pocketflow.mdc          ← combined guide.md + index.md, alwaysApply
├── core_abstraction/
│   ├── node.mdc
│   ├── flow.mdc
│   └── ...
├── design_pattern/
│   └── ...
└── utility_function/
    └── ...
```

Each `.mdc` file begins with a YAML front-matter block:

```yaml
---
description: Guidelines for using PocketFlow, Core Abstraction, Node
globs:
alwaysApply: false
---
```

The combined `guide_for_pocketflow.mdc` additionally sets `globs: **/*.cs` and  
`alwaysApply: true` so Cursor automatically injects it as context for every C# file.

## Key concepts illustrated

| Concept | Where |
|---|---|
| `Node` (Prepare / Execute / Post) | `DiscoverFilesNode`, `CreateCombinedGuideNode` |
| `BatchNode` (one Execute call per item) | `ConvertMdBatchNode` |
| Shared store (dictionary passed through flow) | `Program.cs`, all nodes |
| Static utility helpers | `MarkdownUtils.cs` |


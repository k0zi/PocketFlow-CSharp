# BatchFlow – Image Filter Example

This project demonstrates the **BatchFlow** concept in PocketFlow by applying multiple image filters to a set of images.  
It is the C# port of the Python [`pocketflow-batch-flow`](../../cookbook/pocketflow-batch-flow) cookbook example.

## What This Example Demonstrates

- How to use `BatchFlow` to run a `Flow` repeatedly with different parameters.
- Key `BatchFlow` concepts:
  1. Creating a base `Flow` for single-item processing.
  2. Overriding `Prepare()` in a `BatchFlow` subclass to generate per-run parameter sets.
  3. Passing parameters across multiple `Flow` executions through the `Params` dictionary.

## Project Structure

```
BatchFlow/
├── BatchFlow.csproj        # Project file (references PocketFlow + SharedUtils)
├── Program.cs              # Entry point – wires the flow and runs it
├── LoadImageNode.cs        # Node: loads an image from disk
├── ApplyFilterNode.cs      # Node: applies a filter via ImageUtils (SharedUtils)
├── SaveImageNode.cs        # Node: saves the processed image to disk
├── ImageBatchFlow.cs       # BatchFlow subclass – generates image × filter params
├── images/
│   ├── cat.jpg
│   ├── dog.jpg
│   └── bird.jpg
└── output/                 # Generated output (created at runtime)
```

> Image processing utilities (`ImageUtils.ApplyFilter`) live in the shared **SharedUtils** project.

## How It Works

### Base Flow (single image)

```
LoadImageNode  ──apply_filter──▶  ApplyFilterNode  ──save──▶  SaveImageNode
```

1. **LoadImageNode** – reads `images/<input>` into an `Image<Rgba32>` and stores it in `shared["image"]`.
2. **ApplyFilterNode** – calls `ImageUtils.ApplyFilter()` with the filter name and stores the result in `shared["filtered_image"]`.
3. **SaveImageNode** – saves the processed image to `output/<name>_<filter>.jpg`.

### BatchFlow

`ImageBatchFlow.Prepare()` returns all **image × filter** combinations (9 total):

| Image    | Filters                    |
|----------|----------------------------|
| cat.jpg  | grayscale · blur · sepia   |
| dog.jpg  | grayscale · blur · sepia   |
| bird.jpg | grayscale · blur · sepia   |

The `BatchFlow` runs the base `Flow` once per combination, merging the per-run parameters into each execution.

## Dependencies

| Package / Project         | Purpose                          |
|---------------------------|----------------------------------|
| `PocketFlow`              | Flow orchestration framework     |
| `SharedUtils`             | `ImageUtils.ApplyFilter` helper  |
| `SixLabors.ImageSharp`    | Cross-platform image processing  |

## Usage

```bash
dotnet run
```

### Sample Output

```
Processing images with filters...

Saved filtered image to: output/cat_grayscale.jpg
Saved filtered image to: output/cat_blur.jpg
Saved filtered image to: output/cat_sepia.jpg
Saved filtered image to: output/dog_grayscale.jpg
...

All images processed successfully!
Check the 'output' directory for results.
```

## Key Concepts Illustrated

1. **BatchFlow.Prepare()** – returns `IEnumerable<Dictionary<string, object>>` where each entry becomes the `Params` for one Flow run.
2. **Flow reuse** – the same base `Flow` instance is cloned and re-executed for every parameter set.
3. **SharedUtils** – filter logic is centralised in `ImageUtils` (SharedUtils project) so it can be reused by other projects.


# NestedBatch – School Grades Calculator

This project demonstrates **Nested BatchFlow** in PocketFlow by calculating average grades for students, classes, and a whole school.  
It is the C# port of the Python [`pocketflow-nested-batch`](../../cookbook/pocketflow-nested-batch) cookbook example.

## What This Example Demonstrates

- How to nest one `BatchFlow` inside another to process hierarchical data.
- Key concepts:
  1. An **outer `BatchFlow`** (`SchoolBatchFlow`) iterates over class folders and injects a `class` parameter.
  2. An **inner `BatchFlow`** (`ClassBatchFlow`) iterates over student files within each class and injects a `student` parameter.
  3. A **base `Flow`** processes a single student using the merged parameters from both batch levels.
  4. **Parameter inheritance** – inner flows automatically receive all parameters set by outer flows.

## Project Structure

```
NestedBatch/
├── NestedBatch.csproj       # Project file (references PocketFlow)
├── Program.cs               # Entry point – creates sample data, wires and runs the flow
├── LoadGradesNode.cs        # Node: reads grade lines from a student's .txt file
├── CalculateAverageNode.cs  # Node: computes average and stores it in the results map
├── ClassBatchFlow.cs        # Inner BatchFlow – iterates over students in a class
├── SchoolBatchFlow.cs       # Outer BatchFlow – iterates over classes in the school
└── school/                  # Sample data (created at runtime)
    ├── class_a/
    │   ├── student1.txt
    │   └── student2.txt
    └── class_b/
        ├── student3.txt
        └── student4.txt
```

## How It Works

### Flow topology

```
SchoolBatchFlow
  └─ ClassBatchFlow
       └─ Flow
            LoadGradesNode  ──calculate──▶  CalculateAverageNode
```

### Processing steps

| Step | Component | Responsibility |
|------|-----------|---------------|
| 1 | `SchoolBatchFlow.Prepare()` | Lists class folders; emits `{ class: "class_a" }`, `{ class: "class_b" }`, … |
| 2 | `ClassBatchFlow.Prepare()` | Lists `.txt` files in the current class; emits `{ student: "student1.txt" }`, … |
| 3 | `LoadGradesNode` | Reads `school/<class>/<student>` into a `List<double>` |
| 4 | `CalculateAverageNode` | Averages grades; stores result in `shared["results"][class][student]` |
| 5 | `ClassBatchFlow.Post()` | Prints class average after all students are processed |
| 6 | `SchoolBatchFlow.Post()` | Prints overall school average after all classes are processed |

### Sample data

| Class | Student | Grades | Average |
|-------|---------|--------|---------|
| class_a | student1 | 7.5, 8.0, 9.0 | 8.2 |
| class_a | student2 | 8.5, 7.0, 9.5 | 8.3 |
| class_b | student3 | 6.5, 8.5, 7.0 | 7.3 |
| class_b | student4 | 9.0, 9.5, 8.0 | 8.8 |

## Dependencies

| Project | Purpose |
|---------|---------|
| `PocketFlow` | Flow orchestration framework |

> No external NuGet packages or SharedUtils utilities are required — all I/O uses the .NET standard library.

## Usage

```bash
dotnet run
```

### Expected Output

```
Processing school grades...

  - student1.txt: Average = 8.2
  - student2.txt: Average = 8.3
Class A Average: 8.25

  - student3.txt: Average = 7.3
  - student4.txt: Average = 8.8
Class B Average: 8.08

School Average: 8.17
```



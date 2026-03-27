# Structured Output Demo

A minimal demo application showing how to use PocketFlow to extract structured data from a resume
using direct prompting and YAML formatting.
Why YAML? Check out the [doc](https://the-pocket.github.io/PocketFlow/design_pattern/structure.html).

C# port of the [`pocketflow-structured-output`](../../cookbook/pocketflow-structured-output) cookbook example.
Based on: [Structured Output for Beginners: 3 Must-Know Prompting Tips](https://zacharyhuang.substack.com/p/structured-output-for-beginners-3).

## Features

- Extracts structured resume data using prompt engineering and YAML output
- Validates the parsed structure before storing results
- Identifies target skills by index from the resume content
- LLM calls are provided by the shared **SharedUtils** project (`OllamaConnector`)

## Getting Started

1. Ensure [Ollama](https://ollama.com/) is running locally (default: `http://localhost:11434`) with a suitable model (default: `gemma3:latest`).

2. Build and run:
   ```bash
   dotnet run --project StructuredOutput
   ```

   Optional environment variables:
   | Variable | Default | Description |
   |---|---|---|
   | `OLLAMA_HOST` | `http://localhost:11434` | Ollama server URL |
   | `OLLAMA_MODEL` | `gemma3:latest` | Model to use |

3. Edit [`data.txt`](./data.txt) to supply a different resume (a sample resume is already included).

## How It Works

```mermaid
flowchart LR
    parser[ResumeParserNode]
```

`ResumeParserNode` is a single `Node` that:

1. **Prepare** – reads `resume_text` and `target_skills` from the shared store.
2. **Execute** – builds a prompt requesting YAML-formatted output with comments, calls the LLM via
   `OllamaConnector.CallLlm`, extracts the `\`\`\`yaml` block, deserialises it into a `ResumeData`
   object using **YamlDotNet**, and performs basic validation.
3. **Post** – stores the `ResumeData` in `shared["structured_data"]` and prints a summary.

After the flow completes, `Program.cs` resolves and prints the matched target skills by index.

## Project Structure

| File | Description |
|---|---|
| `Program.cs` | Entry point – loads `data.txt`, configures target skills, runs the flow |
| `ResumeParserNode.cs` | `Node` implementation for resume parsing and validation |
| `data.txt` | Sample resume text file |
| `StructuredOutput.csproj` | Project file with references to **PocketFlow** and **SharedUtils** |

LLM utilities (`OllamaConnector`) live in the shared **SharedUtils** project and are referenced as a
project dependency — no local `utils.cs` copy is needed.

## Example Output

```
=== Resume Parser - Structured Output with Indexes & Comments ===


=== STRUCTURED RESUME DATA ===

Name:  JOHN SMTIH
Email: johnsmtih1983@gnail.com

Experience:
  - SALES MANAGER at ABC Corportaion
  - ASST. MANAGER at XYZ Industries
  - CUSTOMER SERVICE REPRESENTATIVE at Fast Solutions Inc

Skill Indexes: 0, 1, 2, 3, 4

==============================

✅ Extracted resume information.

--- Found Target Skills (from Indexes) ---
- Team leadership & management (Index: 0)
- CRM software (Index: 1)
- Project management (Index: 2)
- Public speaking (Index: 3)
- Microsoft Office (Index: 4)
------------------------------------------
```


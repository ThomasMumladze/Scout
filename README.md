# Scout

LLM-based developer assistant that answers questions about local code projects.

## Problem
Developers working on several projects lose time looking for where something is defined or used.
Scout takes a question in natural language, decides on its own which tool to use and answers from the real files.
Target user: a developer (me) who works with multiple local projects.

## PEAS
- **Performance measure:** correct answers about the codebase, correct tool choice, few steps per answer.
- **Environment:** a local project folder with source files and the user's questions.
- **Actuators:** text answer in the console, tool calls (file search).
- **Sensors:** the user's text input and the results returned by tools.

## Environment properties
1. **Partially observable** - the agent sees only the search results, never the whole project at once.
2. **Deterministic** - the same search over the same files always returns the same result.
3. **Sequential** - each tool result influences the next step, so one answer may need several steps.
4. **Static** - files do not change while the agent is thinking about an answer.
5. **Discrete** - a finite set of tools, text messages and separate steps.
6. **Single-agent** - only one agent acts; the user only asks questions.
7. **Known** - the rules (file system, tool behavior) are fully known in advance.

## Agent type
**Goal-based agent.** The goal is to answer the user's question; the language model chooses a sequence of tool calls
that leads to it. A simple reflex agent is not enough because the environment is partially observable and sequential,
and a utility-based agent is unnecessary because there are no trade-offs between competing outcomes.

## Tools
- `search_files` - searches a project folder for lines containing a text (written by me).

## Architecture
Clean Architecture: `Dex.Domain` (entities), `Dex.Application` (agent loop, abstractions),
`Dex.Infrastructure` (LLamaSharp client, tools), `Scout` (console UI), `Dex.Tests` (xUnit).

## How to run
1. Install .NET 10 SDK.
2. Download `qwen2.5-3b-instruct-q4_k_m.gguf` from Hugging Face (`Qwen/Qwen2.5-3B-Instruct-GGUF`) into `models/`.
3. Set `ModelPath` and `ProjectRoot` in `Scout/Program.cs`.
4. `dotnet run --project Scout`

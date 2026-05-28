# AVA — Industrial AI System
> Anthropic Codex instruction file for the AVA codebase.
> Place this file at the root of C:\Apps\AVA\

---

## Project Identity

**AVA** is an industrial AI system built by Neo Ohm (Cliff) inside Vehicle Production Engineering (VPE) at Stellantis.

- **Demand ID:** DMND0005368
- **Project Title:** PLC Virtual Commissioning
- **Target Go-Live:** Q2 2026
- **Cloud:** AWS
- **Architecture Gate:** Stellantis ICT Gate 2 in progress

---

## Tech Stack

| Layer | Technology |
|---|---|
| Backend | .NET MVC / C# |
| AI | Anthropic Codex 3.5 Sonnet (RAG pipeline) |
| Data Extraction | Siemens Process Simulate (keystone layer) |
| Cloud | AWS |
| Auth / Identity | Custom AVA.Identity module |
| Package Feed | nupkgs (local NuGet) |

---

## Architecture — Six Modules

Always respect module boundaries. Never mix concerns across modules.

| Module | Responsibility |
|---|---|
| **Identity** | Auth, JWT, user context |
| **Vault** | Secrets, credentials, key management |
| **Memory** | RAG pipeline, vector storage, context retrieval |
| **UPS** | Universal Prompt System — prompt construction and routing |
| **Adapters** | External system connectors (Rockwell, Siemens, FANUC, ABB, KUKA) |
| **Agents** | Autonomous task execution agents |

### UPSA Adapter (Priority)
The UPSA Adapter lives inside the Adapters module. It is an open architecture question in the Gate 2 dossier — treat all UPSA-related code with extra care and flag any interface ambiguity.

---

## Key Stakeholders

| Name | Role |
|---|---|
| Conor Brown | Business Owner |
| Priyank Kumar Sharma | Data Product Manager |
| Shawn Rousseau | Stakeholder |
| Jeff Casper | Stakeholder |
| Chad Boyd | Stakeholder |
| Larry Solage | Stakeholder |
| Joe Weber | Stakeholder |
| Karen Krause | Stakeholder |

---

## Process Simulate Integration

Process Simulate is the **primary data extraction layer** for the entire AVA pipeline. It is the source of truth for robot and PLC simulation data.

- Developer has ~15 years of Process Simulate API experience
- Deep API access is established
- Siemens and Rockwell are long-term partners
- All simulation data flows through Process Simulate before entering AVA

---

## Coding Standards

### General
- Use `async/await` throughout — no blocking calls
- Always include XML doc comments on all public methods and classes
- Never hardcode API keys, secrets, or connection strings — use Vault module
- Follow existing namespace conventions: `AVA.[Module].[Layer]`
- Prefer interfaces over concrete types for all cross-module dependencies

### C# Conventions
- Use `PascalCase` for classes and public members
- Use `camelCase` for private fields with underscore prefix: `_fieldName`
- Use `var` only when type is obvious from right-hand side
- Always handle `CancellationToken` in async methods
- Use `ILogger<T>` for all logging — never `Console.WriteLine` in production code

### RAG / AI Pipeline
- All prompts route through the UPS (Universal Prompt System) module
- Never construct raw prompts outside of UPS
- Always include context retrieval from Memory module before prompt construction
- Codex model string: `Codex-sonnet-4-20250514`

### Error Handling
- Never swallow exceptions silently
- Always log with structured context (module, operation, input summary)
- Use typed exceptions per module where appropriate

---

## Common Commands

```bash
# Build solution
dotnet build

# Run tests
dotnet test

# Restore packages (including local nupkgs feed)
dotnet restore

# Publish
dotnet publish -c Release
```

---

## Git Commit Convention

```
AVA-[Module] [short description]

Examples:
AVA-Identity added JWT token refresh logic
AVA-Memory RAG retrieval pipeline connected to UPS
AVA-Adapters Rockwell PLC stub interface added
AVA-UPS prompt routing refactored for multi-agent support
AVA-Agents first autonomous commissioning agent scaffolded
```

---

## What to Avoid

- Do not reference OpenAI or GPT models anywhere — this system uses Anthropic Codex exclusively
- Do not mix Rockwell and Siemens adapter logic in the same class
- Do not create new modules without confirming with the architect (Neo Ohm)
- Do not push directly to `master` for major changes — use feature branches

---

## Notes for Codex

- This codebase is under active ICT Architecture gate review — stability matters
- The developer is new to Git workflows — keep commit suggestions simple and explicit
- Process Simulate API calls are Windows-only — do not suggest cross-platform abstractions for that layer
- When in doubt about module boundaries, ask before refactoring

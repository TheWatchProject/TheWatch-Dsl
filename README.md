# 📜 TheWatch-Dsl

> **Domain-Specific Language (DSL) Compiler, Tactical Grammar, and Wikipedia Lexeme Engine for The Watch**

---

## 🏛️ Architecture & Capabilities

`TheWatch-Dsl` provides a high-level domain grammar for real-time emergency dispatching, incident policy automation, and lexical ontology analysis:
- **`DslParser`**: Tokenizes and parses emergency dispatch logic (e.g. `ON INCIDENT WHERE Priority == CRITICAL DISPATCH MEDIC NOTIFY POLICE`).
- **`DslExecutionEngine`**: Compiles and evaluates AST nodes against `Incident` domain entities and responder proximity graphs.
- **`WiktionaryAndLexemeEngine`**: Integrates Wikipedia Lexemes and Wiktionary definitions for emergency trigger words, lemmas, parts of speech, and synonyms.

---

## 🚀 Building & Packaging

```bash
dotnet build TheWatch.Dsl.slnx -c Release
dotnet pack TheWatch.Dsl.slnx -c Release -p:PackageVersion=10.0.0-local
```

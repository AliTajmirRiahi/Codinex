# Changelog

All notable changes to Codinex AI are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

While the major version is `0`, the public behaviour of the extension is not yet
considered stable and may change between minor releases.

## [Unreleased]

## [0.1.0] - 2026-10-27

First public release. Requires Visual Studio 2022 (17.0+) and .NET Framework 4.7.2.

### Added

#### Provider-agnostic model support
- OpenAI-compatible providers — any endpoint speaking the Chat Completions API
  (OpenAI, Azure OpenAI, Together, Groq, OpenRouter, LiteLLM, vLLM).
- Anthropic-compatible providers via the Claude Messages API.
- Gemini-compatible providers via `generateContent` and its streaming API.
- Ollama as a first-class local runtime.
- OpenCode free tier as a built-in zero-config provider, so the extension can be
  tried without supplying a key.
- Custom endpoints — any base URL plus key, with a per-provider model catalog.
- Capability probing on connect: each model is checked for streaming,
  tool-calling, and reasoning support, and the UI adapts to what it finds.
- Model management UI for adding, removing, and switching providers and models
  without leaving the tool window.

#### Agentic tools
- Read and understand — Read File, Read Element, Get File Elements,
  List Directory, Get Projects, Get Open Documents.
- Search — Search Project, Find References, Find Symbol.
- Change code — Changeset Creator for multi-file edits, gated by human review.
- Build and verify — Build Project, Build Solution, Run Tests, Get Diagnostics.
- Collaborate — Ask-User-Question, rendered as structured clarification cards.
- Memory — Remember Fact and Forget Fact, persisted across sessions.

#### Solution-aware context
- `@` references for file, folder, solution, class, interface, method, field,
  and system context. Symbol references are Roslyn-powered and stay in sync as
  the code is edited.
- Ambient context providers feeding the agent automatically: Git state, compiler
  diagnostics, build output, open documents, project structure, and saved memory.

#### Reviewed changesets
- Change Review tool window with side-by-side and line-level diffs.
- Syntax-highlighted hunks with per-change approve or reject.
- No edit reaches the working tree until it is approved.

#### Chat experience
- Streaming token rendering with Markdown and syntax highlighting.
- Multiple conversations and conversation groups, with pagination.
- Prompt-cache lineage pinning for faster, cheaper follow-up turns.
- RTL and Persian language support in the composer.
- Theme-aware WebView2 UI that follows the Visual Studio light and dark themes.
- Built-in About and Bug Report panels.
- Toast notifications for background events.

### Known limitations
- Models that only accept function tools through the OpenAI `/v1/responses` API
  (for example `gpt-6.1-sol`) are not supported yet. Codinex reports this with a
  clear message; select a different model until Responses API support lands.

[Unreleased]: https://github.com/AliTajmirRiahi/Codinex/compare/v0.1.0...HEAD
[0.1.0]: https://github.com/AliTajmirRiahi/Codinex/releases/tag/v0.1.0

# PROJECT_CONTEXT

## Project Identity
- Project name: Codify AI
- Product type: Visual Studio AI coding assistant extension
- Core direction: provider-agnostic, agent-configurable, local-first
- Primary goal: let users connect Codify to different AI providers and local model servers without vendor lock-in

## What The Project Already Does
- Hosts a Visual Studio extension through a VSIX package
- Opens a Codify tool window inside Visual Studio
- Uses a WebView-based chat UI
- Routes chat requests through an abstraction-based provider system
- Supports streaming and non-streaming AI responses
- Persists settings and provider configuration to disk

## Confirmed Architecture From The Codebase
- `Codify/CodifyPackage.cs` is the main Visual Studio package entry point.
- `Codify/Commands/CodifyToolWindowCommand.cs` is responsible for opening the Codify tool window.
- `Codify/UI/ToolWindows/` contains the tool window host, XAML UI, and chat web assets.
- `Codify/Core/Abstractions/IAiProvider.cs` defines the provider contract.
- `Codify/Core/Abstractions/IAiRouterProvider.cs` defines routing/provider selection behavior.
- `Codify/Infrastructure/AiProviders/` contains concrete provider implementations and router logic.
- `Codify/Core/UseCases/SendChatMessageUseCase.cs` is part of the message execution flow.
- `Codify/Infrastructure/Execution/ExecutionPipeline.cs` appears to orchestrate request execution.
- `Codify/Storage/SettingsManager.cs` manages persisted settings.
- `Codify/Storage/ProviderManager.cs` manages provider metadata and active provider state.

## Product Constraints
- Do not introduce hard vendor coupling into the core chat flow.
- Keep provider integrations behind shared abstractions whenever possible.
- Prefer local-first compatibility, including local model servers and custom endpoints.
- Preserve Visual Studio extension startup and tool window flow.
- Keep settings and provider persistence compatible with the existing storage layer.

## Development Rules For AI Sessions
- Treat this file as the high-level project source of truth.
- Before changing architecture, check whether the existing abstraction already supports the new feature.
- When working on a task, explicitly list the files involved before proposing large changes.
- Do not replace the provider-agnostic design with provider-specific shortcuts unless that is the task.

## Important Files
- `README.md`
- `ProjectTree.json`
- `Codify/CodifyPackage.cs`
- `Codify/Infrastructure/DependencyInjection/ServiceContainer.cs`
- `Codify/Core/UseCases/SendChatMessageUseCase.cs`
- `Codify/Infrastructure/Execution/ExecutionPipeline.cs`
- `Codify/Infrastructure/AiProviders/AiProviderRouter.cs`
- `Codify/Storage/SettingsManager.cs`
- `Codify/Storage/ProviderManager.cs`
- `Codify/UI/ToolWindows/`

## How To Use This File
- Include this file at the start of a new AI session.
- Update it only when the product direction or architecture meaningfully changes.
- Keep task-specific progress out of this file and put it into `WORKLOG.md` or `AI_HANDOFF.md`.

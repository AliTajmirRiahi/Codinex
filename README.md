## THIS PROJECT IS CURRENTLY UNDER ACTIVE DEVELOPMENT AND WILL EVOLVE AS FEATURES ARE ADDED.

# ![Codify AI Logo](assets/Codify_AI_logo_40x40.png) Codify AI

A next‑generation AI coding assistant for Visual Studio — fully agent‑configurable, provider‑agnostic, and built with local‑first support in mind.

## 🚀 Vision

Codify AI is an extensible Visual Studio extension inspired by GitHub Copilot — but with a fundamental difference:

    🔌 You are not locked into one AI provider.

Codify allows developers to connect any AI Agent — cloud or local — and configure their own AI pipeline.

This makes it especially powerful for:

    Developers who want full control over their AI stack
    Teams with privacy requirements
    Regions with limited access to certain AI services
    The Iranian developer community seeking reliable local AI integrations

## 🧠 Core Features

    🧠 AI‑powered code generation
    💬 Chat‑based development assistant
    🔁 Streaming responses
    🧩 Pluggable AI provider architecture
    🌐 WebView2 modern UI (HTML/CSS/JS)
    🔐 Local model support (Ollama, LM Studio, local LLM servers)
    ⚙️ Configurable AI endpoints
    📦 Designed for future multi‑agent orchestration

## 🏗 Architecture

Codify uses a modern hybrid architecture:
Visual Studio Extension Layer

    Built with Visual Studio Toolkit
    ToolWindow‑based UI
    Async command routing
    WebView2 host

UI Layer

    HTML / CSS / JavaScript
    Markdown rendering
    Syntax highlighting
    Streaming message updates
    Bidirectional JS ↔ C# communication

Codify is provider‑agnostic by design.

🌍 Built With the Iranian Developer Community in Mind

Many developers face:

    API access restrictions
    Payment limitations
    Privacy concerns
    Connectivity instability

Codify solves this by enabling:

    Local AI hosting
    Configurable endpoints
    Offline‑first capability (planned)

🛠 Tech Stack

    C#
    Visual Studio SDK
    WebView2
    HTML/CSS/JavaScript
    Markdown rendering engine
    REST‑based AI communication
    Streaming token handling

🔮 Roadmap

    ✅ ToolWindow with WebView2 UI
    ✅ Command binding & shortcut system
    🔄 JS ↔ C# bidirectional messaging
    🔄 Streaming token renderer
    ⏳ AI Provider abstraction layer
    ⏳ Settings UI for model configuration
    ⏳ Multi‑agent orchestration
    ⏳ Inline code completion
    ⏳ Context‑aware file indexing
    ⏳ RAG integration

🧩 Planned Provider Interface

    C#:
    public interface IAIProvider

    {

        Task<AIResponse> SendAsync(AIRequest request);

        IAsyncEnumerable<string> StreamAsync(AIRequest request);

    }

Providers will implement this interface, making Codify fully extensible.
📦 Installation (Development)

    Clone repository
    Open solution in Visual Studio
    Build
    Run in Experimental Instance (F5)

🎯 Long Term Goal

To build an open, extensible AI coding platform where:

    Developers choose their AI
    Communities build custom agents
    AI integration becomes modular
    Innovation is not locked behind one vendor

🤝 Contributing

Contributions are welcome.

If you want to:

    Build a provider adapter
    Improve UI
    Add streaming optimization
    Add agent orchestration
    Improve performance

Feel free to open an issue or submit a pull request.
📜 License

MIT (recommended — adjust if needed)
💡 Philosophy

    AI should empower developers — not restrict them.

Codify exists to give control back to developers.

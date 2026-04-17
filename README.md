<p align="center">
  <img src="https://img.shields.io/badge/.NET-8.0-512BD4?style=for-the-badge&logo=dotnet&logoColor=white" alt=".NET 8" />
  <img src="https://img.shields.io/badge/WinUI-3-0078D7?style=for-the-badge&logo=windows&logoColor=white" alt="WinUI 3" />
  <img src="https://img.shields.io/badge/MAUI-Cross--Platform-512BD4?style=for-the-badge&logo=dotnet&logoColor=white" alt=".NET MAUI" />
  <img src="https://img.shields.io/badge/SQLite-Embedded-003B57?style=for-the-badge&logo=sqlite&logoColor=white" alt="SQLite" />
  <img src="https://img.shields.io/badge/License-MIT-green?style=for-the-badge" alt="License" />
</p>

<p align="center">
  <img src="https://img.shields.io/badge/RAG-Vector_Search-FF6F61?style=flat-square" alt="RAG" />
  <img src="https://img.shields.io/badge/LLM-OpenAI_Compatible-412991?style=flat-square&logo=openai&logoColor=white" alt="LLM" />
  <img src="https://img.shields.io/badge/Architecture-MVVM-blueviolet?style=flat-square" alt="MVVM" />
  <img src="https://img.shields.io/badge/Streaming-SSE-orange?style=flat-square" alt="SSE Streaming" />
  <img src="https://img.shields.io/badge/NLP-Character_Extraction-teal?style=flat-square" alt="NLP" />
</p>

<h1 align="center">📖 PersonaAI — CharacterChat</h1>

<p align="center">
  <strong>Upload a book. Extract its characters. Talk to them as if they were real.</strong>
</p>

<p align="center">
  <em>An AI-powered literary character conversation engine that uses Retrieval-Augmented Generation (RAG) to ground responses in canonical text — so characters never break character, hallucinate, or know things they shouldn't.</em>
</p>

---

## 🧠 What Is This?

**PersonaAI** is a desktop application that lets you have genuine conversations with fictional characters extracted from novels, stories, or any narrative text. Unlike generic chatbots slapped with a character name, PersonaAI performs deep psychological profiling and retrieval-augmented generation to ensure every response is:

- **Canonically grounded** — responses come from the actual text, not fabricated knowledge
- **Psychologically consistent** — personality traits, speech patterns, and emotional baselines are extracted and enforced
- **Knowledge-bounded** — characters don't know things they shouldn't (no Elizabeth Bennet discussing quantum physics)
- **Contextually aware** — vector search retrieves the most relevant text passages for each conversation turn

---

## 🏗️ System Architecture

```mermaid
graph TB
    subgraph Client["🖥️ Client Applications"]
        WinUI["WinUI 3<br/>Windows Desktop"]
        MAUI["MAUI<br/>Android · iOS · macOS"]
    end

    subgraph Core["📦 CharacterChat.Core"]
        VM["ViewModels<br/>MVVM + CommunityToolkit"]
        Models["Domain Models"]
        Services["Shared Services"]
        Data["Data Access Layer"]
    end

    subgraph AI["🤖 AI Pipeline"]
        Embed["Embedding Service"]
        Extract["Character Extraction"]
        VSearch["Vector Search"]
        SysPrompt["System Prompt Builder"]
        Chat["Chat Service<br/>SSE Streaming"]
    end

    subgraph Storage["💾 Storage"]
        SQLite["SQLite<br/>Per-Book Databases"]
        VecStore["Embedded Vectors<br/>BLOB Storage"]
    end

    subgraph LLM["☁️ LLM Provider"]
        API["OpenAI-Compatible API<br/>GPT-4o · Local LLMs · Any Provider"]
    end

    WinUI --> Core
    MAUI --> Core
    Core --> AI
    AI --> Storage
    Chat --> API
    Embed --> API
    Extract --> API

    style Client fill:#1a1a2e,stroke:#e94560,color:#fff
    style Core fill:#16213e,stroke:#0f3460,color:#fff
    style AI fill:#0f3460,stroke:#533483,color:#fff
    style Storage fill:#1a1a2e,stroke:#e94560,color:#fff
    style LLM fill:#533483,stroke:#e94560,color:#fff
```

---

## 📚 RAG Pipeline

The core intelligence of PersonaAI lives in its **Retrieval-Augmented Generation** pipeline — a multi-stage process that ensures every character response is grounded in their actual literary existence.

```mermaid
graph LR
    A["📄 Upload Book<br/>.txt · .epub · .pdf"] --> B["✂️ Chunking<br/>2000 chars + 200 overlap"]
    B --> C["🧬 Embedding<br/>text-embedding-3-small"]
    C --> D["💾 Store<br/>SQLite + Vector BLOB"]
    D --> E["🔍 Query<br/>Cosine Similarity Search"]
    E --> F["🎭 Character Filter<br/>Strict Name Matching"]
    F --> G["🧠 System Prompt<br/>Personality + Context"]
    G --> H["💬 LLM Response<br/>Streamed via SSE"]

    style A fill:#2d2d44,stroke:#ff6b6b,color:#fff
    style B fill:#2d2d44,stroke:#ffa502,color:#fff
    style C fill:#2d2d44,stroke:#ffd32a,color:#fff
    style D fill:#2d2d44,stroke:#05c46b,color:#fff
    style E fill:#2d2d44,stroke:#0fbcf9,color:#fff
    style F fill:#2d2d44,stroke:#575fcf,color:#fff
    style G fill:#2d2d44,stroke:#d63031,color:#fff
    style H fill:#2d2d44,stroke:#6c5ce7,color:#fff
```

---

## 🎭 Character Extraction Pipeline

When a book is imported, an AI-driven extraction pipeline automatically identifies characters and builds rich psychological profiles:

```mermaid
flowchart TD
    A["📖 Full Text Ingested"] --> B["🔎 Character Name Extraction<br/>via LLM JSON Mode"]
    B --> C{"Characters Found?"}
    C -->|Yes| D["🧬 Per-Character Profile Generation"]
    C -->|No| E["⚠️ Manual Entry Option"]
    
    D --> F["Personality Traits"]
    D --> G["Speech Style & Formality"]
    D --> H["Emotional Baseline"]
    D --> I["Knowledge Scope & Gaps"]
    D --> J["Relationship Map"]
    D --> K["Sample Dialogue Patterns"]
    D --> L["Worldview & Sentiment"]
    
    F & G & H & I & J & K & L --> M["📋 CharacterProfile JSON"]
    M --> N["💾 Stored in SQLite"]

    style A fill:#1e272e,stroke:#ff6348,color:#fff
    style D fill:#1e272e,stroke:#ffa502,color:#fff
    style M fill:#1e272e,stroke:#7bed9f,color:#fff
    style N fill:#1e272e,stroke:#70a1ff,color:#fff
```

---

## 📐 Project Structure

```mermaid
graph TD
    subgraph Solution["CharacterChat.sln"]
        subgraph WinUI["CharacterChat (WinUI 3)"]
            W_Views["Views/"]
            W_VM["ViewModels/"]
            W_Styles["Styles.xaml"]
            W_Assets["Assets/"]
        end

        subgraph CoreLib["CharacterChat.Core (.NET 8)"]
            C_Models["Models/"]
            C_Services["Services/"]
            C_Data["Data/"]
            C_VM["ViewModels/"]
            C_Platform["IPlatformServices"]
        end

        subgraph MauiApp["CharacterChat.Maui"]
            M_Platforms["Platforms/<br/>Android · iOS · macOS · Tizen · Windows"]
            M_Resources["Resources/"]
            M_Shell["AppShell"]
        end
    end

    WinUI -->|depends on| CoreLib
    MauiApp -->|depends on| CoreLib

    style Solution fill:#0d1117,stroke:#30363d,color:#c9d1d9
    style WinUI fill:#161b22,stroke:#1f6feb,color:#c9d1d9
    style CoreLib fill:#161b22,stroke:#238636,color:#c9d1d9
    style MauiApp fill:#161b22,stroke:#a371f7,color:#c9d1d9
```

---

## ⚡ Key Features

| Feature | Description |
|:--------|:------------|
| 📄 **Multi-Format Import** | Upload `.txt`, `.epub`, and `.pdf` books for character extraction |
| 🧬 **Vector Embeddings** | Every text chunk is embedded and stored for semantic retrieval |
| 🎭 **Deep Character Profiles** | Personality, speech style, knowledge boundaries, and relationships — all extracted automatically |
| 🔍 **Character-Filtered RAG** | Vector search is strictly isolated per character — no cross-contamination |
| 💬 **Streaming Chat** | Real-time Server-Sent Events (SSE) streaming for natural conversation flow |
| 🧠 **Strict Knowledge Boundaries** | Characters refuse to answer questions outside their canonical knowledge scope |
| 🔌 **Any LLM Provider** | Works with OpenAI, local LLMs (Ollama, LM Studio), or any OpenAI-compatible API |
| 💾 **Per-Book Databases** | Each book gets its own SQLite database for clean isolation |
| 🖥️ **Multi-Platform** | WinUI 3 for Windows, MAUI for Android, iOS, macOS, and more |

---

## 🔄 Conversation Flow

```mermaid
sequenceDiagram
    actor User
    participant UI as WinUI / MAUI
    participant VM as ChatViewModel
    participant ES as EmbeddingService
    participant VS as VectorSearchService
    participant SPB as SystemPromptBuilder
    participant CS as ChatService
    participant LLM as LLM API

    User->>UI: Types message
    UI->>VM: SendMessageAsync()
    VM->>ES: GetEmbeddingAsync(query)
    ES->>LLM: POST /embeddings
    LLM-->>ES: float[] vector
    ES-->>VM: Query embedding
    VM->>VS: SearchForCharacterContext()
    Note over VS: Cosine similarity + <br/>character name filtering
    VS-->>VM: Top-K relevant chunks
    VM->>SPB: BuildPrompt(character, chunks)
    SPB-->>VM: System prompt with<br/>personality + context
    VM->>CS: StreamChatAsync(request)
    CS->>LLM: POST /chat/completions<br/>(stream: true)
    
    loop SSE Stream
        LLM-->>CS: data: {"choices":[...]}
        CS-->>VM: yield token
        VM-->>UI: Update message bubble
    end
    
    VM->>VM: Save to ChatHistory
```

---

## 🗄️ Data Model

```mermaid
erDiagram
    Books ||--o{ TextChunks : contains
    Books ||--o{ Characters : "extracted from"
    Characters ||--o{ ChatHistory : "conversations"

    Books {
        int Id PK
        string Title
        string FilePath
        datetime ImportedAt
    }

    TextChunks {
        int Id PK
        int BookId FK
        int ChunkIndex
        text RawText
        json CharactersMentioned
        blob EmbeddingVector
        string ChapterHint
    }

    Characters {
        int Id PK
        int BookId FK
        string Name
        string Aliases
        json ProfileJson
        datetime ExtractedAt
    }

    ChatHistory {
        int Id PK
        int CharacterId FK
        string Role
        text Content
        datetime Timestamp
    }

    Settings {
        string Key PK
        string Value
    }
```

---

## ⚙️ Technology Stack

```mermaid
mindmap
  root((PersonaAI))
    Runtime
      .NET 8
      C# 12
    UI Frameworks
      WinUI 3
      .NET MAUI
    AI & ML
      OpenAI API
      Embeddings
      RAG Pipeline
      Vector Search
    Architecture
      MVVM
      CommunityToolkit.Mvvm
      Repository Pattern
      Service Layer
    Storage
      SQLite
      Microsoft.Data.Sqlite
      BLOB Vectors
    Document Processing
      PdfPig
      EPubSharp
      Custom Chunker
```

---

## 🚀 Getting Started

### Prerequisites

| Requirement | Version |
|:------------|:--------|
| .NET SDK | 8.0+ |
| Visual Studio | 2022 17.8+ |
| Windows App SDK | 1.6+ |
| Windows | 10 (1809+) / 11 |

### Setup

1. **Clone the repository**
2. **Open `CharacterChat.sln`** in Visual Studio 2022
3. **Set the startup project** to `CharacterChat` (WinUI 3) or `CharacterChat.Maui`
4. **Build and run** — the app will create its database on first launch
5. **Configure your LLM** — go to Settings and enter your API endpoint and key

### LLM Configuration

PersonaAI supports any OpenAI-compatible API. Configure through the in-app Settings page:

| Setting | Default | Description |
|:--------|:--------|:------------|
| Base URL | `https://api.openai.com/v1` | API endpoint |
| API Key | — | Your API key |
| Chat Model | `gpt-4o-mini` | Model for chat & extraction |
| Embedding Model | `text-embedding-3-small` | Model for vector embeddings |

> 💡 **Local LLMs:** Set the Base URL to your local server (e.g., `http://localhost:1234/v1` for LM Studio, `http://localhost:11434/v1` for Ollama).

---

## 🗺️ Roadmap

```mermaid
timeline
    title PersonaAI Development Roadmap
    
    Phase 1 — Foundation : WinUI 3 Desktop App
                          : Document Ingestion Pipeline
                          : RAG + Vector Search
                          : Character Extraction
    
    Phase 2 — Multi-Platform : .NET MAUI Android App
                              : Shared Core Library
                              : Cross-Platform ViewModels
    
    Phase 3 — Intelligence : On-Device Local LLMs
                            : Multi-Inference Support
                            : Hardware-Aware Model Selection
                            : Enhanced Character Memory
    
    Phase 4 — Experience : Ancient Scroll Dark Theme
                          : Character Voice Synthesis
                          : Multi-Character Conversations
                          : Book Club Mode
```

---

## 🤝 Contributing

Contributions are welcome! Please feel free to submit a Pull Request. For major changes, please open an issue first to discuss what you would like to change.

---

## 📄 License

This project is licensed under the MIT License — see the [LICENSE](LICENSE) file for details.

---

<p align="center">
  <sub>Built with ❤️ and a love for literature</sub>
</p>

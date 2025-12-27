# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

VR Anatomy is a VR-based educational platform for teaching human anatomy to 9th-grade students. Built with Unity 6 for VR interactions and a FastAPI backend with Azure OpenAI for AI-powered Q&A.

## Repository Structure

```
├── unity/                  # Unity VR application (main project)
│   ├── Assets/Scripts/     # C# game scripts
│   ├── Assets/Scenes/      # 7 modular scenes (Menu, MotionSystem, CirculationSystem, Quiz, AIChat, About, Settings)
│   ├── Assets/anatomy/     # 3D anatomical models and prefabs
│   └── Packages/           # Unity package dependencies
├── ai/
│   ├── backend/            # FastAPI server
│   │   ├── app/main.py     # Health endpoint
│   │   └── tests/          # Azure integration tests
│   └── data/               # Excel source → JSON exports (MCQ, facts)
└── docs/forms/             # Product Vision, Literature Review PDFs
```

## Commands

### AI Backend
```bash
# Install dependencies
pip install -r ai/backend/requirements.txt
pip install azure-search-documents openai python-dotenv

# Run FastAPI server
cd ai/backend && uvicorn app.main:app --reload

# Test RAG pipeline
python ai/backend/tests/rag_chat.py "Kalbin görevi nedir?"

# Individual integration tests
python ai/backend/tests/test_chat.py      # Azure OpenAI
python ai/backend/tests/test_search.py    # Azure AI Search
python ai/backend/tests/test_storage.py   # Blob Storage
```

### Unity
- Open `unity/` folder in Unity Hub (requires Unity 6000.0.63f1)
- Run scenes in Play Mode or build for VR headset
- Use XR Device Simulator for desktop testing

## Architecture

### Scene Navigation
`SceneLoader.cs` handles all scene transitions:
- 01_Menu → 02_MotionSystem, 03_CirculationSystem, 04_Quiz, 05_AIChat, 06_About, 07_Settings

### RAG Pipeline (AI Chat)
1. User query → Azure OpenAI embeddings
2. Hybrid search (vector + text) on Azure AI Search
3. Top-5 documents → context building (max 6000 chars)
4. Azure OpenAI ChatGPT generates answer with source citations [1], [2]
5. Fallback to text-only search if vector unavailable

### VR Stack
- OpenXR + XR Interaction Toolkit 3.1.2 for cross-platform VR
- XR Hands 1.7.2 for hand tracking
- Universal Render Pipeline (URP)

## Environment Variables (AI Backend)

```
AZURE_OPENAI_API_KEY
AZURE_OPENAI_ENDPOINT
AZURE_OPENAI_API_VERSION
AZURE_OPENAI_CHAT_DEPLOYMENT
AZURE_OPENAI_EMBED_DEPLOYMENT  # optional, for vector search
AZURE_SEARCH_ENDPOINT
AZURE_SEARCH_INDEX
AZURE_SEARCH_KEY
AZURE_STORAGE_CONNECTION_STRING
AZURE_STORAGE_CONTAINER
```

## Branch Naming Convention

- `feature/<issue-no>-short-name`
- `data/<issue-no>-topic`
- `content/<issue-no>-asset-name`
- `docs/<issue-no>-title`

## Unity MCP Integration

Claude has access to Unity Editor via MCP. Use `manage_editor` for play/pause/stop controls and editor state queries.

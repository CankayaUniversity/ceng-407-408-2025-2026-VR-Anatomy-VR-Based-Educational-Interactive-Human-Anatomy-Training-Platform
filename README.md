<div align="center">
  
<img width="393" height="399.5" alt="image" src="https://github.com/user-attachments/assets/67fbc476-7c2b-4e73-b32a-0537ca221970" />

</div>

<div align="center">

# VR Anatomy

### VR-Based Educational Interactive Human Anatomy Training Platform

An immersive virtual reality anatomy learning platform that combines 3D interaction, AI-guided explanations, voice-based tutoring, and quiz-based assessment to support anatomy education.

<br/>

![Unity](https://img.shields.io/badge/Unity-6-black?style=for-the-badge\&logo=unity)
![Meta Quest](https://img.shields.io/badge/Meta%20Quest-2-0467DF?style=for-the-badge\&logo=meta)
![Python](https://img.shields.io/badge/Python-3.10+-3776AB?style=for-the-badge\&logo=python\&logoColor=white)
![FastAPI](https://img.shields.io/badge/FastAPI-Backend-009688?style=for-the-badge\&logo=fastapi)
![RAG](https://img.shields.io/badge/RAG-AI%20Tutor-8A2BE2?style=for-the-badge)
![ChromaDB](https://img.shields.io/badge/ChromaDB-Vector%20Database-FF6F00?style=for-the-badge)
![Gemini](https://img.shields.io/badge/Gemini-AI-4285F4?style=for-the-badge\&logo=google)

<br/>

**Senior Capstone Project · Çankaya University**
**Supported by TÜBİTAK 2209-A**

[Demo Video](YOUR_DEMO_LINK) · [Project Website](https://app.vr-anatomy.com/home)

</div>

---

## 📌 Overview

**VR Anatomy** is an educational Virtual Reality application designed to make human anatomy learning more interactive, visual, and accessible.

Instead of learning anatomy only through flat textbook diagrams, students can enter an immersive 3D environment where they can inspect anatomical structures, interact with models, take quizzes, and ask questions to an AI tutor.

The project focuses on selected topics from the **musculoskeletal system** and **circulatory system**, especially structures such as bones, joints, muscles, the heart, and blood vessels.

---

## 🎯 Motivation

Traditional anatomy learning often depends on:

* 2D textbook diagrams
* passive memorization
* limited physical models
* difficulty understanding spatial relationships
* limited access to advanced anatomy laboratories

This creates a gap between what students read and what they can actually visualize.

**VR Anatomy aims to close this gap by transforming anatomy learning into an immersive, hands-on experience.**

---

## ✨ Key Features

### 🧭 AI Guided Learning

A structured learning mode where students progress through anatomy units step by step.

Students can:

* view anatomical structures in VR
* read curriculum-based explanations
* listen to spoken explanations
* ask for simpler AI-generated explanations
* revisit topics they find difficult

---

### 🦴 Free Explore

A free exploration mode that allows students to independently inspect anatomical models.

Students can:

* grab, rotate, and examine 3D anatomy models
* explore selected body system sub-units
* view anatomical names and labels
* learn through direct interaction

---

### 🤖 AI Chat

An AI-supported tutor module that allows students to ask anatomy-related questions.

The AI tutor supports:

* Turkish text-based questions
* speech input
* spoken AI responses
* live subtitles
* anatomy-focused answers generated through RAG

The system uses a Retrieval-Augmented Generation pipeline so that answers are grounded in project-related anatomy documents.

---

### 🧪 Quiz Mode

A quiz module designed to reinforce learning after exploration and guided study.

The quiz system supports:

* multiple-choice anatomy questions
* unit-based question grouping
* immediate feedback
* wrong-answer explanation cards
* score tracking

---

## 🏗️ System Architecture

```text
                 ┌──────────────────────────┐
                 │      Unity VR Client     │
                 │      Meta Quest 2        │
                 │                          │
                 │  - XR Interaction        │
                 │  - VR User Interface     │
                 │  - 3D Anatomy Models     │
                 │  - Learning Flow         │
                 │  - Quiz System           │
                 └─────────────┬────────────┘
                               │
                               │ HTTP / JSON
                               │
                 ┌─────────────▼────────────┐
                 │      Python Backend      │
                 │        FastAPI           │
                 │                          │
                 │  GET  /health            │
                 │  POST /ask               │
                 │  POST /stt               │
                 │  POST /tts               │
                 └─────────────┬────────────┘
                               │
              ┌────────────────┼────────────────┐
              │                │                │
      ┌───────▼───────┐ ┌──────▼──────┐  ┌──────▼───────┐
      │ Gemini Model  │ │  ChromaDB   │  │ Anatomy Docs │
      │  Generation   │ │  Vector DB  │  │  Knowledge   │
      └───────────────┘ └─────────────┘  └──────────────┘
```

---

## 🧠 AI Pipeline

The AI tutor follows a Retrieval-Augmented Generation workflow:

```text
Student Question
      ↓
Question Embedding
      ↓
Vector Search in ChromaDB
      ↓
Relevant Anatomy Context Retrieval
      ↓
Prompt Construction
      ↓
Gemini Response Generation
      ↓
Answer Returned to Unity
      ↓
Text Display + Optional Voice Output
```

This approach helps the AI assistant produce answers based on selected anatomy learning resources instead of generating completely ungrounded responses.

---

## 🎙️ Speech Interaction

The AI Chat module includes voice-based interaction features.

Supported interaction flow:

1. Student speaks into the microphone.
2. Speech is converted into text using STT.
3. The question is sent to the backend.
4. The AI tutor generates an anatomy-focused response.
5. The answer is displayed as text.
6. The answer can also be played aloud using TTS.

---

## 🛠️ Tech Stack

### VR Client

| Technology                | Purpose                             |
| ------------------------- | ----------------------------------- |
| Unity 6                   | Main VR development environment     |
| C#                        | Gameplay, UI, and interaction logic |
| XR Interaction Toolkit    | VR interaction system               |
| OpenXR                    | VR device support                   |
| Meta Quest 2              | Target VR headset                   |
| glTFast                   | Runtime avatar/model loading        |
| Universal Render Pipeline | Rendering pipeline                  |

### AI Backend

| Technology     | Purpose                           |
| -------------- | --------------------------------- |
| Python         | Backend development               |
| FastAPI        | API server                        |
| Uvicorn        | ASGI server                       |
| Gemini         | AI response generation            |
| ChromaDB       | Vector database                   |
| RAG            | Retrieval-based answer generation |
| Edge TTS       | Turkish text-to-speech            |
| Speech-to-Text | Voice input processing            |

---

## 🖼️ Screenshots


| Main Menu | AI Guided Learning |
|---|---|
| <img width="492" alt="Main Menu" src="https://github.com/user-attachments/assets/ec67f923-e553-4cf1-86b6-83204875800f" /> | <img width="492" alt="AI Guided Learning" src="https://github.com/user-attachments/assets/ce22d46b-afe8-401d-b246-b80440cdea2a" /> |

| Free Explore | AI Chat |
|---|---|
| <img width="492" alt="Free Explore" src="https://github.com/user-attachments/assets/160832a3-49d0-46ef-bfe4-8d31563c09d3" /> | <img width="492" alt="AI Chat" src="https://github.com/user-attachments/assets/e946c735-e565-472e-a603-612f48e72b43" /> |

| Quiz Mode | Lab Interface |
|---|---|
| <img width="492" alt="Quiz Mode" src="https://github.com/user-attachments/assets/460ffeea-7870-45ed-9fdf-bf0c1656ae74" /> | <img width="492" alt="Anatomy Model Interaction" src="https://github.com/user-attachments/assets/8845416f-3312-4b2e-83d1-153ad6a8dede" /> |

---

## 🚀 Getting Started

### Prerequisites

Make sure the following tools are installed:

* Unity 6
* Python 3.10+
* Git
* Meta Quest Developer Hub
* Android Build Support for Unity
* Meta Quest 2 or compatible OpenXR headset
* Gemini API key

---

## 🧩 Unity Setup

1. Clone the repository:

```bash
git clone https://github.com/CankayaUniversity/ceng-407-408-2025-2026-VR-Anatomy-VR-Based-Educational-Interactive-Human-Anatomy-Training-Platform.git VR-Anatomy
cd VR-Anatomy
```

2. Open the Unity project:

```text
Unity Hub → Add Project → Select the UnityProject folder
```

3. Make sure the required Unity packages are installed:

* XR Interaction Toolkit
* OpenXR Plugin
* TextMeshPro
* Universal Render Pipeline
* glTFast

4. Switch the build target to Android:

```text
File → Build Settings → Android → Switch Platform
```

5. Enable OpenXR:

```text
Project Settings → XR Plug-in Management → OpenXR
```

6. Connect Meta Quest 2 and run the project.

---

## 🧠 Backend Setup

1. Go to the backend folder:

```bash
cd ai/backend
```

2. Create a virtual environment:

```bash
python -m venv .venv
```

3. Activate the environment:

For Windows:

```bash
.venv\Scripts\activate
```

For macOS/Linux:

```bash
source .venv/bin/activate
```

4. Install dependencies:

```bash
pip install -r requirements.txt
```

5. Create a `.env` file:

```env
GEMINI_API_KEY=your_gemini_api_key_here
```

6. Start the backend server:

```bash
uvicorn main:app --reload --host 0.0.0.0 --port 8000
```

7. Test the backend:

```text
http://localhost:8000/health
```

---

## 🔌 API Endpoints

| Method | Endpoint  | Description                                |
| ------ | --------- | ------------------------------------------ |
| GET    | `/health` | Checks backend service status              |
| POST   | `/ask`    | Sends a question to the RAG-based AI tutor |
| POST   | `/stt`    | Converts speech input into text            |
| POST   | `/tts`    | Converts AI response text into speech      |

---

## 📚 Educational Scope

The current version focuses on selected anatomy topics from the following systems.

### Musculoskeletal System

* Skeleton overview
* Skull and facial bones
* Trunk bones
* Upper extremity bones
* Lower extremity bones
* Skeletal muscles
* Joints and movement-related concepts

### Circulatory System

* Heart structure
* Heart chambers
* Heart valves
* Blood vessels
* Circulation-related concepts

---

## 🧪 Evaluation

The project is designed to be evaluated through:

* pre-test and post-test comparison
* student usability testing
* quiz performance analysis
* qualitative feedback
* AI answer quality inspection
* VR performance observation

The main goal is to observe whether immersive VR interaction and AI-supported explanations improve student engagement and anatomy understanding.

---

## 🔐 Environment Variables

Create a `.env` file inside the backend directory.

```env
GEMINI_API_KEY=your_api_key_here
```

Do not commit real API keys to GitHub.

Recommended `.gitignore` entries:

```gitignore
.env
*.key
*.pem
__pycache__/
.venv/
chroma_db/
*.mp3
*.wav
Library/
Temp/
Obj/
Build/
Builds/
Logs/
UserSettings/
```

---

## 🧑‍💻 Team

| Name              | Role                            |
| ----------------- | -----------------------------   |
| Elifnaz Talas     | Team Lead & VR Developer        |
| Çağla Pelin Doğan | AI Developer                    |
| Kutay Ayoğlu      | Software Developer              |
| Öykü Çoban        | Unity & VR Developer            |
| Sena Akbaba       | Unity & VR Developer            |

---

## 👩‍🏫 Supervisors

| Name                           |
| ------------------------------ |
| Assoc. Prof. Dr. Gül Tokdemir  |
| Dr. Instructor Talha Karadeniz |

---

## 🏆 Acknowledgement

This project was developed as a senior capstone project at **Çankaya University, Computer Engineering Department**.

VR Anatomy is supported by **TÜBİTAK 2209-A University Students Research Projects Support Program**.

Special thanks to our supervisors, test participants, and partner institutions for their guidance and feedback throughout the development process.

---

## ⚠️ Disclaimer

VR Anatomy is an educational prototype developed to support anatomy learning.

It is not intended for medical diagnosis, medical treatment, or professional clinical decision-making.

---

## 📄 License

This project is licensed under the **Apache License 2.0**.

You may use, modify, and distribute this project in accordance with the terms of the Apache License, Version 2.0.

See the [Apache-2.0 license](https://github.com/CankayaUniversity/ceng-407-408-2025-2026-VR-Anatomy-VR-Based-Educational-Interactive-Human-Anatomy-Training-Platform?tab=Apache-2.0-1-ov-file#) file for more details.

Copyright 2026 VR Anatomy Team.

---

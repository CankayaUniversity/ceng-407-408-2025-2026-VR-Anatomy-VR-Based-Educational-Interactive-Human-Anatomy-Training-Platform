<div align="center">
  
<img width="465" height="855" alt="Resim3" src="https://github.com/user-attachments/assets/1f8babd7-59df-43e5-b78f-6b6372e63cc1" />

</div>

# VR Anatomy — VR-Based Educational Interactive Human Anatomy Training Platform

An interactive Virtual Reality (VR) learning application designed to support introductory human anatomy education with immersive 3D exploration, structured learning flows, and AI-assisted guidance.

> **Capstone Project (CENG 407–408, 2025–2026) — Çankaya University, Department of Computer Engineering**

---

## ✨ What is VR Anatomy?

VR Anatomy aims to complement traditional anatomy learning methods (2D atlases, plastic models, classroom instruction) by providing:

- **Immersive 3D exploration** of anatomical structures in VR
- **Structured learning paths** (topic/module-based)
- **Interactive inspection** (grab / rotate / zoom / isolate parts)
- **Assessment support** (quiz/test mode)
- **AI Tutor integration** (Q&A support, guided explanations — work-in-progress depending on milestone)

---

## 🎯 Scope (Current Focus)

Due to scope and budget constraints, the project focuses on two core systems:

- **Musculoskeletal System** (e.g., bones, major muscle groups; staged content delivery)

- **Circulatory System** (including the heart; staged content delivery)

> Content is delivered iteratively. Modules/features may be marked as **Planned** or **In Progress** depending on the sprint stage.

---

## 🧩 Main Features

### Learning Mode
- Guided module navigation (system → region → structure)
- Basic labels + structured explanations

### Free Explore Mode
- Sandbox inspection of models
- Grab / rotate / examine 3D parts

### Test / Quiz Mode
- Topic-based quizzes (system/module selection)
- Score feedback and review

### AI Tutor
- Ask anatomy questions via text or voice (depending on platform support)
- Retrieval-Augmented responses based on curated course materials

---

## 🗂 Repository Structure

- `unity/` — Unity project (VR client)
- `ai/` — AI Tutor backend/services (RAG pipeline, APIs, data tools)
- `design/` — UI/UX assets, design exports
- `docs/forms/` — project forms and documentation artifacts
- `ops/scripts/` — helper scripts (automation, build, utilities)

---

## 🛠 Tech Stack (High Level)

### VR Client
- Unity (XR)
- OpenXR / XR Interaction Toolkit (depending on project setup)
- Target device: VR headset (Meta Quest 2)

### AI Tutor
- Python backend (e.g., FastAPI)
- Retrieval-Augmented Generation (RAG)
- Vector search / indexing (optional: Azure AI Search or alternatives)

---

## 🚀 Getting Started

### 1) Clone the repository
```bash
git clone https://github.com/CankayaUniversity/ceng-407-408-2025-2026-VR-Anatomy-VR-Based-Educational-Interactive-Human-Anatomy-Training-Platform.git
cd ceng-407-408-2025-2026-VR-Anatomy-VR-Based-Educational-Interactive-Human-Anatomy-Training-Platform

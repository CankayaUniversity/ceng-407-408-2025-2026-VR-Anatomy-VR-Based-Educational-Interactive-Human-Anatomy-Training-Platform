# VR Anatomy RAG Backend

Backend service for the **VR Anatomy** application.

This project implements a **Retrieval-Augmented Generation (RAG)** pipeline using **FastAPI**, **ChromaDB**, and **Gemini AI** to answer anatomy-related questions based on course materials.

The system processes an anatomy course book (PDF), generates embeddings, retrieves relevant information, and produces AI-generated responses.

---

# Project Structure

```
backend/
│
├── app/                # FastAPI application and API endpoints
├── rag_core/           # RAG pipeline (chunking, retrieval, generation)
├── tests/              # Test files (optional)
├── requirements.txt    # Python dependencies
└── README.md
```

The following files are **not included in the repository**:

* `.env`
* `.venv/` or `venv/`
* `__pycache__/`
* `chroma_db/`
* course book PDF

These files are excluded for **security and licensing reasons**.

---

# Installation

## 1. Create a Virtual Environment

### Windows (PowerShell)

```bash
python -m venv .venv
.\.venv\Scripts\activate
```

### macOS / Linux

```bash
python3 -m venv .venv
source .venv/bin/activate
```

---

## 2. Install Dependencies

```bash
pip install -r requirements.txt
```

---

# Environment Variables

This project uses environment variables to store API keys.

Create a `.env` file in the backend root directory and add:

```
GEMINI_API_KEY=YOUR_GEMINI_API_KEY
```

The `.env` file is **ignored by Git for security reasons**.

---

# Adding the Course Book (PDF)

The course book is **not included in the repository due to licensing restrictions**.

To use the RAG system, add the PDF locally.

## Step 1 — Place the PDF

Put the book file in the backend directory.

Example:

```
backend/book.pdf
```

---

## Step 2 — Run the Indexing Script

Run the indexing script to process the book and generate the vector database.

```bash
python index_book.py
```

This process will:

* Read the PDF file
* Split the text into chunks
* Generate embeddings
* Store them in the `chroma_db` directory

---

# Running the Backend

Start the FastAPI server:

```bash
uvicorn app:app --host 0.0.0.0 --port 8000 --reload
```

The API will run at:

```
http://localhost:8000
```

Health check endpoint:

```
GET /health
```

---

# RAG Pipeline Overview

The system works in the following steps:

1. The course book is processed and split into text chunks.
2. Each chunk is converted into embeddings.
3. Embeddings are stored in **ChromaDB**.
4. When a user asks a question:

   * Relevant chunks are retrieved
   * Context is sent to **Gemini AI**
   * Gemini generates the final answer

---

# Notes

* The vector database (`chroma_db`) is generated locally and **not stored in Git**.
* The course book PDF is excluded from the repository due to **licensing restrictions**.
* API keys must be stored in the `.env` file.

---

# Technologies

* FastAPI
* ChromaDB
* Gemini API
* Python



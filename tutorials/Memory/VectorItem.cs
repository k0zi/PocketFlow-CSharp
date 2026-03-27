using OllamaSharp.Models.Chat;

namespace Memory;

/// <summary>
/// Stores an archived conversation pair together with its embedding vector.
/// Replaces the FAISS index entry from the Python implementation.
/// </summary>
record VectorItem(List<Message> Conversation, float[] Embedding);
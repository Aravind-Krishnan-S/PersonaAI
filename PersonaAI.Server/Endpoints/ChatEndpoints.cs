using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using CharacterChat.Data;
using CharacterChat.Models;
using CharacterChat.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using PersonaAI.Server.Models;

namespace PersonaAI.Server.Endpoints
{
    /// <summary>
    /// Chat endpoint — accepts messages from LAN clients and streams persona responses via SSE.
    /// </summary>
    public static class ChatEndpoints
    {
        public static void Map(WebApplication app)
        {
            // POST /api/chat — Stream a persona response
            app.MapPost("/api/chat", async (
                HttpContext ctx,
                ChatRequest request,
                CharacterRepository characterRepo,
                ChunkRepository chunkRepo,
                ChatService chatService,
                EmbeddingService embedService,
                VectorSearchService vectorSearch,
                LlmConfigService llmConfig,
                ServerMetrics metrics) =>
            {
                metrics.IncrementRequests();
                metrics.AddConnection();

                try
                {
                    // ── Validate request ──
                    if (string.IsNullOrWhiteSpace(request.Message))
                    {
                        ctx.Response.StatusCode = 400;
                        await ctx.Response.WriteAsJsonAsync(new { error = "Message is required" });
                        return;
                    }

                    // ── Load persona ──
                    var character = characterRepo.GetCharacterById(request.PersonaId);
                    if (character == null)
                    {
                        ctx.Response.StatusCode = 404;
                        await ctx.Response.WriteAsJsonAsync(new { error = $"Persona {request.PersonaId} not found" });
                        return;
                    }

                    // ── RAG: Get embedding and search for context ──
                    float[] queryVector;
                    try
                    {
                        queryVector = await embedService.GetEmbeddingAsync(request.Message);
                    }
                    catch
                    {
                        // Embedding service unavailable; proceed without RAG context
                        queryVector = Array.Empty<float>();
                    }

                    var allChunks = chunkRepo.GetAllChunks(character.BookId);
                    var contextChunks = queryVector.Length > 0
                        ? vectorSearch.SearchForCharacterContext(allChunks, queryVector, character.Name)
                        : new List<TextChunk>();

                    // ── Build system prompt ──
                    string systemPrompt = SystemPromptBuilder.BuildPrompt(character, contextChunks);

                    // ── Build message list ──
                    var messages = new List<object>
                    {
                        new { role = "system", content = systemPrompt }
                    };

                    // Add client-provided history (last 6 messages)
                    foreach (var h in request.History.TakeLast(6))
                    {
                        messages.Add(new { role = h.Role, content = h.Content });
                    }

                    // Add the current message
                    messages.Add(new { role = "user", content = request.Message });

                    var requestBody = new
                    {
                        model = llmConfig.ChatModelName,
                        messages = messages,
                        stream = true
                    };

                    // ── Stream response as SSE ──
                    ctx.Response.ContentType = "text/event-stream";
                    ctx.Response.Headers.Append("Cache-Control", "no-cache");
                    ctx.Response.Headers.Append("Connection", "keep-alive");
                    ctx.Response.Headers.Append("X-Accel-Buffering", "no");

                    var fullResponse = new StringBuilder();

                    await foreach (var chunk in chatService.StreamChatAsync(requestBody))
                    {
                        fullResponse.Append(chunk);

                        var sseData = JsonSerializer.Serialize(new { content = chunk });
                        await ctx.Response.WriteAsync($"data: {sseData}\n\n");
                        await ctx.Response.Body.FlushAsync();
                    }

                    // Send completion marker
                    await ctx.Response.WriteAsync("data: [DONE]\n\n");
                    await ctx.Response.Body.FlushAsync();
                }
                catch (Exception ex)
                {
                    if (!ctx.Response.HasStarted)
                    {
                        ctx.Response.StatusCode = 500;
                        await ctx.Response.WriteAsJsonAsync(new { error = ex.Message });
                    }
                    else
                    {
                        // Already streaming; send error as SSE event
                        var errorData = JsonSerializer.Serialize(new { error = ex.Message });
                        await ctx.Response.WriteAsync($"data: {errorData}\n\n");
                        await ctx.Response.WriteAsync("data: [DONE]\n\n");
                        await ctx.Response.Body.FlushAsync();
                    }
                }
                finally
                {
                    metrics.RemoveConnection();
                }
            })
            .WithName("ChatWithPersona")
            ;
        }
    }
}

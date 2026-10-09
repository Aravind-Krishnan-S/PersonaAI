using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using CharacterChat.Data;
using CharacterChat.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace PersonaAI.Server.Endpoints
{
    /// <summary>
    /// Persona listing endpoints — allows LAN clients to discover available characters.
    /// </summary>
    public static class PersonaEndpoints
    {
        public static void Map(WebApplication app)
        {
            // GET /api/personas — List all available characters
            app.MapGet("/api/personas", (CharacterRepository repo, ServerMetrics metrics) =>
            {
                metrics.IncrementRequests();

                try
                {
                    var characters = repo.GetAllCharacters();
                    var result = characters.Select(c =>
                    {
                        CharacterProfile? profile = null;
                        try { profile = JsonSerializer.Deserialize<CharacterProfile>(c.ProfileJson); } catch { }

                        return new
                        {
                            id = c.Id,
                            name = c.Name,
                            aliases = c.Aliases,
                            bookId = c.BookId,
                            traits = profile?.PersonalityTraits ?? new List<string>(),
                            speechStyle = profile?.SpeechStyle ?? "Unknown",
                            worldview = profile?.Worldview ?? "Unknown"
                        };
                    }).ToList();

                    return Results.Ok(new { personas = result, count = result.Count });
                }
                catch (Exception ex)
                {
                    return Results.Problem($"Failed to load personas: {ex.Message}");
                }
            })
            .WithName("ListPersonas")
            ;

            // GET /api/personas/{id} — Get single character detail
            app.MapGet("/api/personas/{id:int}", (int id, CharacterRepository repo, ServerMetrics metrics) =>
            {
                metrics.IncrementRequests();

                try
                {
                    var character = repo.GetCharacterById(id);
                    if (character == null)
                        return Results.NotFound(new { error = $"Persona with ID {id} not found" });

                    CharacterProfile? profile = null;
                    try { profile = JsonSerializer.Deserialize<CharacterProfile>(character.ProfileJson); } catch { }

                    return Results.Ok(new
                    {
                        id = character.Id,
                        name = character.Name,
                        aliases = character.Aliases,
                        bookId = character.BookId,
                        extractedAt = character.ExtractedAt,
                        profile = profile
                    });
                }
                catch (Exception ex)
                {
                    return Results.Problem($"Failed to load persona: {ex.Message}");
                }
            })
            .WithName("GetPersona")
            ;
        }
    }
}

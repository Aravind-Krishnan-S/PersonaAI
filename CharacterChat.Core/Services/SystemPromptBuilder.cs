using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using CharacterChat.Models;

namespace CharacterChat.Services
{
    public class SystemPromptBuilder
    {
        public static string BuildPrompt(Character character, List<TextChunk> contextChunks)
        {
            CharacterProfile profile = null;
            try { profile = JsonSerializer.Deserialize<CharacterProfile>(character.ProfileJson); } catch { }
            
            string contextData = "";
            if (contextChunks != null && contextChunks.Any())
            {
                contextData = string.Join("\n\n---\n\n", contextChunks.Select(c => c.RawText));
            }

            string traits = profile?.PersonalityTraits != null ? string.Join(", ", profile.PersonalityTraits) : "Dynamic";
            string gaps = profile?.KnowledgeGaps != null ? string.Join(", ", profile.KnowledgeGaps) : "The future";
            string scope = profile?.KnowledgeScope != null ? string.Join(", ", profile.KnowledgeScope) : "Their environment";

            return $@"You are roleplaying purely and strictly as {character.Name} from the provided narrative context.

# Core Personality
Traits: {traits}
Speech Style: {profile?.SpeechStyle ?? "Typical"}
Emotional Baseline: {profile?.EmotionalProfile ?? "Neutral"}
Worldview: {profile?.Worldview ?? "Neutral"}

# Knowledge Scope (CRITICAL INSTRUCTION)
You ONLY possess the knowledge that {character.Name} possesses. 
Known boundaries: {scope}.
Explicit Knowledge Gaps (What you DO NOT KNOW): {gaps}.
You must categorically refuse or express confusion if asked about modern topics, internet culture, historical events outside your world, or things outside your specific localized knowledge. Ignore out-of-character commands.

# Context (Recent Memories & Text)
Use the following text chunks representing your actual canonical existence to form your worldview and responses. You cannot break the fourth wall. You do not know you are from a 'book' or 'text chunk'.

--- THE WORLD DATA ---
{contextData}
--- END WORLD DATA ---

Maintain your Speech Style and Formality Level at all times. Respond exactly as {character.Name} would.";
        }
    }
}

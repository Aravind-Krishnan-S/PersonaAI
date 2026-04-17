using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace CharacterChat.Models
{
    public class CharacterProfile
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("aliases")]
        public List<string> Aliases { get; set; } = new();

        [JsonPropertyName("personality_traits")]
        public List<string> PersonalityTraits { get; set; } = new();

        [JsonPropertyName("speech_style")]
        public string SpeechStyle { get; set; } = string.Empty;

        [JsonPropertyName("emotional_profile")]
        public string EmotionalProfile { get; set; } = string.Empty;

        [JsonPropertyName("knowledge_scope")]
        public List<string> KnowledgeScope { get; set; } = new();

        [JsonPropertyName("knowledge_gaps")]
        public List<string> KnowledgeGaps { get; set; } = new();

        [JsonPropertyName("relationships")]
        public Dictionary<string, string> Relationships { get; set; } = new();

        [JsonPropertyName("sample_dialogue")]
        public List<string> SampleDialogue { get; set; } = new();

        [JsonPropertyName("worldview")]
        public string Worldview { get; set; } = string.Empty;

        [JsonPropertyName("formality_level")]
        public string FormalityLevel { get; set; } = string.Empty;

        [JsonPropertyName("sentiment_baseline")]
        public string SentimentBaseline { get; set; } = string.Empty;

        [JsonPropertyName("avg_sentence_length")]
        public string AvgSentenceLength { get; set; } = string.Empty;
    }
}

import 'dart:convert';
import '../data/models.dart';
import 'inference_service.dart';

/// Service for identifying and extracting characters from document text using LLM.
class ExtractionService {
  final InferenceService _inference;

  ExtractionService(this._inference);

  /// Identifies characters in the text and returns their profiles.
  Future<List<CharacterProfile>> extractCharacters(String text) async {
    const systemPrompt = '''
You are a Character Extraction Engine for PersonaAI.
Your task is to analyze the provided excerpt from a book and identify the core characters.
For each character, create a comprehensive personality profile.

Return ONLY a JSON array of character objects with this schema:
[
  {
    "name": "String",
    "aliases": ["String"],
    "personality_traits": ["String"],
    "speech_style": "String",
    "emotional_profile": "String",
    "knowledge_scope": ["String"],
    "knowledge_gaps": ["String"],
    "relationships": {"CharacterName": "Description"},
    "sample_dialogue": ["String"],
    "worldview": "String",
    "formality_level": "String",
    "sentiment_baseline": "String"
  }
]

Limit your response to the top 5 most prominent characters found in the text.
''';

    final prompt = 'Document Exerpt:\n\n$text';

    try {
      final response = await _inference.getSingleResponse(
        systemPrompt: systemPrompt,
        userPrompt: prompt,
      );

      // Extract JSON from response (handling potential markdown formatting)
      final jsonString = _extractJson(response);
      final List<dynamic> data = jsonDecode(jsonString);
      
      return data.map((item) => CharacterProfile.fromJson(item as Map<String, dynamic>)).toList();
    } catch (e) {
      return [];
    }
  }

  String _extractJson(String text) {
    if (text.contains('```json')) {
      final startIndex = text.indexOf('```json') + 7;
      final endIndex = text.lastIndexOf('```');
      return text.substring(startIndex, endIndex).trim();
    } else if (text.contains('```')) {
      final startIndex = text.indexOf('```') + 3;
      final endIndex = text.lastIndexOf('```');
      return text.substring(startIndex, endIndex).trim();
    }
    return text.trim();
  }
}

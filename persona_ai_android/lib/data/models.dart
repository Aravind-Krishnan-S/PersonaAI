/// Data model for a book imported into PersonaAI.
class Book {
  final int? id;
  final String title;
  final String filePath;
  final DateTime importedAt;

  const Book({
    this.id,
    required this.title,
    required this.filePath,
    required this.importedAt,
  });

  factory Book.fromMap(Map<String, dynamic> map) {
    return Book(
      id: map['Id'] as int?,
      title: map['Title'] as String,
      filePath: map['FilePath'] as String,
      importedAt: DateTime.parse(map['ImportedAt'] as String),
    );
  }

  Map<String, dynamic> toMap() => {
    'Title': title,
    'FilePath': filePath,
    'ImportedAt': importedAt.toIso8601String(),
  };
}

/// A character extracted from a book's text.
class Character {
  final int? id;
  final int bookId;
  final String name;
  final String aliases;
  final String profileJson;
  final DateTime extractedAt;

  const Character({
    this.id,
    required this.bookId,
    required this.name,
    this.aliases = '',
    this.profileJson = '{}',
    required this.extractedAt,
  });

  factory Character.fromMap(Map<String, dynamic> map) {
    return Character(
      id: map['Id'] as int?,
      bookId: map['BookId'] as int,
      name: map['Name'] as String,
      aliases: (map['Aliases'] as String?) ?? '',
      profileJson: (map['ProfileJson'] as String?) ?? '{}',
      extractedAt: DateTime.parse(map['ExtractedAt'] as String),
    );
  }

  Map<String, dynamic> toMap() => {
    'BookId': bookId,
    'Name': name,
    'Aliases': aliases,
    'ProfileJson': profileJson,
    'ExtractedAt': extractedAt.toIso8601String(),
  };
}

/// A single chat message in a conversation with a character.
class ChatMessage {
  final int? id;
  final int characterId;
  final String role; // 'user' or 'assistant'
  final String content;
  final DateTime timestamp;

  const ChatMessage({
    this.id,
    required this.characterId,
    required this.role,
    required this.content,
    required this.timestamp,
  });

  factory ChatMessage.fromMap(Map<String, dynamic> map) {
    return ChatMessage(
      id: map['Id'] as int?,
      characterId: map['CharacterId'] as int,
      role: map['Role'] as String,
      content: map['Content'] as String,
      timestamp: DateTime.parse(map['Timestamp'] as String),
    );
  }

  Map<String, dynamic> toMap() => {
    'CharacterId': characterId,
    'Role': role,
    'Content': content,
    'Timestamp': timestamp.toIso8601String(),
  };
}

/// Parsed character personality profile (from ProfileJson).
class CharacterProfile {
  final String name;
  final List<String> aliases;
  final List<String> personalityTraits;
  final String speechStyle;
  final String emotionalProfile;
  final List<String> knowledgeScope;
  final List<String> knowledgeGaps;
  final Map<String, String> relationships;
  final List<String> sampleDialogue;
  final String worldview;
  final String formalityLevel;
  final String sentimentBaseline;

  const CharacterProfile({
    this.name = '',
    this.aliases = const [],
    this.personalityTraits = const [],
    this.speechStyle = '',
    this.emotionalProfile = '',
    this.knowledgeScope = const [],
    this.knowledgeGaps = const [],
    this.relationships = const {},
    this.sampleDialogue = const [],
    this.worldview = '',
    this.formalityLevel = '',
    this.sentimentBaseline = '',
  });

  factory CharacterProfile.fromJson(Map<String, dynamic> json) {
    return CharacterProfile(
      name: json['name'] as String? ?? '',
      aliases: _toStringList(json['aliases']),
      personalityTraits: _toStringList(json['personality_traits']),
      speechStyle: json['speech_style'] as String? ?? '',
      emotionalProfile: json['emotional_profile'] as String? ?? '',
      knowledgeScope: _toStringList(json['knowledge_scope']),
      knowledgeGaps: _toStringList(json['knowledge_gaps']),
      relationships: (json['relationships'] as Map<String, dynamic>?)
              ?.map((k, v) => MapEntry(k, v.toString())) ??
          {},
      sampleDialogue: _toStringList(json['sample_dialogue']),
      worldview: json['worldview'] as String? ?? '',
      formalityLevel: json['formality_level'] as String? ?? '',
      sentimentBaseline: json['sentiment_baseline'] as String? ?? '',
    );
  }

  static List<String> _toStringList(dynamic val) {
    if (val is List) return val.cast<String>();
    return [];
  }
}

/// Inference mode the user has chosen.
enum InferenceMode { api, localServer, localLlm }

/// Settings for the selected inference engine.
class InferenceSettings {
  final InferenceMode mode;
  final String apiBaseUrl;
  final String apiKey;
  final String modelName;
  final String serverAddress; // for local server (laptop IP)
  final String localModelPath; // for on-device LLM

  const InferenceSettings({
    this.mode = InferenceMode.api,
    this.apiBaseUrl = 'https://api.openai.com/v1',
    this.apiKey = '',
    this.modelName = 'gpt-4o-mini',
    this.serverAddress = '',
    this.localModelPath = '',
  });

  InferenceSettings copyWith({
    InferenceMode? mode,
    String? apiBaseUrl,
    String? apiKey,
    String? modelName,
    String? serverAddress,
    String? localModelPath,
  }) {
    return InferenceSettings(
      mode: mode ?? this.mode,
      apiBaseUrl: apiBaseUrl ?? this.apiBaseUrl,
      apiKey: apiKey ?? this.apiKey,
      modelName: modelName ?? this.modelName,
      serverAddress: serverAddress ?? this.serverAddress,
      localModelPath: localModelPath ?? this.localModelPath,
    );
  }
}

/// A downloadable LLM model with device compatibility info.
class LlmModelInfo {
  final String name;
  final String fileName;
  final String downloadUrl;
  final double sizeGb;
  final int minRamMb;
  final String quantization;
  final String description;

  const LlmModelInfo({
    required this.name,
    required this.fileName,
    required this.downloadUrl,
    required this.sizeGb,
    required this.minRamMb,
    required this.quantization,
    required this.description,
  });
}

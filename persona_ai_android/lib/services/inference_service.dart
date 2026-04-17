import 'dart:async';
import 'dart:convert';
import 'package:dio/dio.dart';
import 'package:flutter/foundation.dart';
import '../data/models.dart';
import 'database_service.dart';

/// Abstract inference engine — all chat completion goes through this.
abstract class InferenceEngine {
  /// Streams chat completions token-by-token.
  Stream<String> streamChat(List<Map<String, String>> messages);

  /// Display name for the engine.
  String get displayName;
}

/// ─── API Inference (OpenAI-compatible) ─────────────────────────
class ApiInferenceEngine extends InferenceEngine {
  final String baseUrl;
  final String apiKey;
  final String model;
  final Dio _dio = Dio();

  ApiInferenceEngine({
    required this.baseUrl,
    required this.apiKey,
    required this.model,
  }) {
    _dio.options.headers['Authorization'] = 'Bearer $apiKey';
    _dio.options.headers['Content-Type'] = 'application/json';
    _dio.options.connectTimeout = const Duration(seconds: 30);
    _dio.options.receiveTimeout = const Duration(minutes: 5);
  }

  @override
  String get displayName => 'API ($model)';

  @override
  Stream<String> streamChat(List<Map<String, String>> messages) async* {
    final url = '${baseUrl.replaceAll(RegExp(r'/+$'), '')}/chat/completions';

    final response = await _dio.post<ResponseBody>(
      url,
      data: jsonEncode({
        'model': model,
        'messages': messages,
        'stream': true,
      }),
      options: Options(responseType: ResponseType.stream),
    );

    final stream = response.data!.stream;
    String buffer = '';

    await for (final chunk in stream) {
      buffer += utf8.decode(chunk);
      final lines = buffer.split('\n');
      buffer = lines.last; // keep incomplete line in buffer

      for (int i = 0; i < lines.length - 1; i++) {
        final line = lines[i].trim();
        if (line.isEmpty || !line.startsWith('data: ')) continue;

        final data = line.substring(6);
        if (data == '[DONE]') return;

        try {
          final json = jsonDecode(data) as Map<String, dynamic>;
          final choices = json['choices'] as List<dynamic>?;
          if (choices != null && choices.isNotEmpty) {
            final delta = choices[0]['delta'] as Map<String, dynamic>?;
            final content = delta?['content'] as String?;
            if (content != null) yield content;
          }
        } catch (_) {
          // Skip malformed chunks
        }
      }
    }
  }
}

/// ─── Local Server Inference (Ollama / LMStudio on laptop) ──────
class LocalServerInferenceEngine extends InferenceEngine {
  final String serverAddress; // e.g., "192.168.1.5:11434"
  final Dio _dio = Dio();

  LocalServerInferenceEngine({required this.serverAddress}) {
    _dio.options.connectTimeout = const Duration(seconds: 10);
    _dio.options.receiveTimeout = const Duration(minutes: 5);
  }

  @override
  String get displayName => 'Local Server ($serverAddress)';

  @override
  Stream<String> streamChat(List<Map<String, String>> messages) async* {
    // Try OpenAI-compatible endpoint first (works with LMStudio, Ollama with /v1)
    final url = 'http://$serverAddress/v1/chat/completions';

    try {
      final response = await _dio.post<ResponseBody>(
        url,
        data: jsonEncode({
          'messages': messages,
          'stream': true,
        }),
        options: Options(responseType: ResponseType.stream),
      );

      final stream = response.data!.stream;
      String buffer = '';

      await for (final chunk in stream) {
        buffer += utf8.decode(chunk);
        final lines = buffer.split('\n');
        buffer = lines.last;

        for (int i = 0; i < lines.length - 1; i++) {
          final line = lines[i].trim();
          if (line.isEmpty || !line.startsWith('data: ')) continue;
          final data = line.substring(6);
          if (data == '[DONE]') return;

          try {
            final json = jsonDecode(data) as Map<String, dynamic>;
            final choices = json['choices'] as List<dynamic>?;
            if (choices != null && choices.isNotEmpty) {
              final delta = choices[0]['delta'] as Map<String, dynamic>?;
              final content = delta?['content'] as String?;
              if (content != null) yield content;
            }
          } catch (_) {}
        }
      }
    } on DioException catch (e) {
      yield '[Error connecting to local server: ${e.message}]';
    }
  }
}

/// ─── Local LLM Inference (on-device, placeholder) ──────────────
/// NOTE: Real on-device inference requires native bindings (llama.cpp via FFI).
/// This is a placeholder that shows the architecture. For production, integrate
/// the `fllama` package or custom platform channels to llama.cpp.
class LocalLlmInferenceEngine extends InferenceEngine {
  final String modelPath;

  LocalLlmInferenceEngine({required this.modelPath});

  @override
  String get displayName => 'On-Device LLM';

  @override
  Stream<String> streamChat(List<Map<String, String>> messages) async* {
    // Placeholder: In production, this would use FFI to llama.cpp
    yield "⚠️ On-device LLM inference requires native llama.cpp bindings. ";
    yield "The model at '$modelPath' is ready. ";
    yield "To enable, integrate the `fllama` package or custom platform channels.";
  }
}

/// ─── Inference Manager ─────────────────────────────────────────
/// Central service that manages the active inference engine based on settings.
class InferenceManager extends ChangeNotifier {
  InferenceSettings _settings = const InferenceSettings();
  InferenceEngine? _engine;

  InferenceSettings get settings => _settings;
  InferenceEngine? get engine => _engine;

  Future<void> loadSettings() async {
    final mode = await DatabaseService.getSetting('inference_mode', 'api');
    final baseUrl = await DatabaseService.getSetting('api_base_url', 'https://api.openai.com/v1');
    final apiKey = await DatabaseService.getSetting('api_key', '');
    final model = await DatabaseService.getSetting('model_name', 'gpt-4o-mini');
    final server = await DatabaseService.getSetting('server_address', '');
    final localModel = await DatabaseService.getSetting('local_model_path', '');

    _settings = InferenceSettings(
      mode: InferenceMode.values.firstWhere(
        (m) => m.name == mode,
        orElse: () => InferenceMode.api,
      ),
      apiBaseUrl: baseUrl,
      apiKey: apiKey,
      modelName: model,
      serverAddress: server,
      localModelPath: localModel,
    );

    _rebuildEngine();
    notifyListeners();
  }

  Future<void> updateSettings(InferenceSettings newSettings) async {
    _settings = newSettings;
    await DatabaseService.saveSetting('inference_mode', newSettings.mode.name);
    await DatabaseService.saveSetting('api_base_url', newSettings.apiBaseUrl);
    await DatabaseService.saveSetting('api_key', newSettings.apiKey);
    await DatabaseService.saveSetting('model_name', newSettings.modelName);
    await DatabaseService.saveSetting('server_address', newSettings.serverAddress);
    await DatabaseService.saveSetting('local_model_path', newSettings.localModelPath);

    _rebuildEngine();
    notifyListeners();
  }

  void _rebuildEngine() {
    switch (_settings.mode) {
      case InferenceMode.api:
        _engine = ApiInferenceEngine(
          baseUrl: _settings.apiBaseUrl,
          apiKey: _settings.apiKey,
          model: _settings.modelName,
        );
      case InferenceMode.localServer:
        _engine = LocalServerInferenceEngine(
          serverAddress: _settings.serverAddress,
        );
      case InferenceMode.localLlm:
        _engine = LocalLlmInferenceEngine(
          modelPath: _settings.localModelPath,
        );
    }
  }
}

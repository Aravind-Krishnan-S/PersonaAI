import 'dart:io';
import 'package:device_info_plus/device_info_plus.dart';
import 'package:path_provider/path_provider.dart';
import '../data/models.dart';

/// Analyzes device hardware and recommends LLM models accordingly.
class DeviceAnalyzer {
  int _totalRamMb = 0;
  String _deviceModel = 'Unknown';
  String _androidVersion = 'Unknown';
  int _sdkInt = 0;

  int get totalRamMb => _totalRamMb;
  String get deviceModel => _deviceModel;
  String get androidVersion => _androidVersion;
  String get ramTier => _getRamTier();

  /// Initialize device info from Android system.
  Future<void> init() async {
    try {
      final deviceInfo = DeviceInfoPlugin();
      final androidInfo = await deviceInfo.androidInfo;

      _deviceModel = '${androidInfo.manufacturer} ${androidInfo.model}';
      _androidVersion = androidInfo.version.release;
      _sdkInt = androidInfo.version.sdkInt;

      // Read total RAM from /proc/meminfo
      _totalRamMb = await _readTotalRam();
    } catch (e) {
      _totalRamMb = 4096; // default fallback
    }
  }

  Future<int> _readTotalRam() async {
    try {
      final memInfo = await File('/proc/meminfo').readAsString();
      final match = RegExp(r'MemTotal:\s+(\d+)\s+kB').firstMatch(memInfo);
      if (match != null) {
        return int.parse(match.group(1)!) ~/ 1024; // KB to MB
      }
    } catch (_) {}
    return 4096;
  }

  String _getRamTier() {
    if (_totalRamMb < 3072) return 'Low (< 3 GB)';
    if (_totalRamMb < 4096) return 'Basic (3-4 GB)';
    if (_totalRamMb < 6144) return 'Medium (4-6 GB)';
    if (_totalRamMb < 8192) return 'Good (6-8 GB)';
    return 'High (8+ GB)';
  }

  /// Get the local models directory path.
  Future<String> get modelsDirectory async {
    final dir = await getApplicationDocumentsDirectory();
    final modelsDir = Directory('${dir.path}/models');
    if (!await modelsDir.exists()) {
      await modelsDir.create(recursive: true);
    }
    return modelsDir.path;
  }

  /// Get available downloadable models with compatibility tags.
  List<LlmModelInfo> getRecommendedModels() {
    final allModels = _allModels;

    // Sort: compatible first, then by size ascending
    allModels.sort((a, b) {
      final aOk = a.minRamMb <= _totalRamMb;
      final bOk = b.minRamMb <= _totalRamMb;
      if (aOk && !bOk) return -1;
      if (!aOk && bOk) return 1;
      return a.sizeGb.compareTo(b.sizeGb);
    });

    return allModels;
  }

  /// Check if a model is compatible with this device's RAM.
  bool isModelCompatible(LlmModelInfo model) {
    return model.minRamMb <= _totalRamMb;
  }

  /// Check if a model is already downloaded.
  Future<bool> isModelDownloaded(LlmModelInfo model) async {
    final dir = await modelsDirectory;
    return File('$dir/${model.fileName}').exists();
  }

  /// Curated model catalog.
  static final List<LlmModelInfo> _allModels = [
    const LlmModelInfo(
      name: 'TinyLlama 1.1B',
      fileName: 'tinyllama-1.1b-chat-v1.0.Q4_K_M.gguf',
      downloadUrl: 'https://huggingface.co/TheBloke/TinyLlama-1.1B-Chat-v1.0-GGUF/resolve/main/tinyllama-1.1b-chat-v1.0.Q4_K_M.gguf',
      sizeGb: 0.67,
      minRamMb: 2048,
      quantization: 'Q4_K_M',
      description: 'Ultra-light model for basic conversations. Runs on almost any device.',
    ),
    const LlmModelInfo(
      name: 'Qwen2.5 1.5B',
      fileName: 'qwen2.5-1.5b-instruct-q4_k_m.gguf',
      downloadUrl: 'https://huggingface.co/Qwen/Qwen2.5-1.5B-Instruct-GGUF/resolve/main/qwen2.5-1.5b-instruct-q4_k_m.gguf',
      sizeGb: 1.0,
      minRamMb: 3072,
      quantization: 'Q4_K_M',
      description: 'Excellent small model with strong multilingual and instruction following.',
    ),
    const LlmModelInfo(
      name: 'Phi-3.5 Mini 3.8B',
      fileName: 'Phi-3.5-mini-instruct-Q4_K_M.gguf',
      downloadUrl: 'https://huggingface.co/bartowski/Phi-3.5-mini-instruct-GGUF/resolve/main/Phi-3.5-mini-instruct-Q4_K_M.gguf',
      sizeGb: 2.4,
      minRamMb: 4096,
      quantization: 'Q4_K_M',
      description: 'Microsoft\'s compact powerhouse. Great reasoning for its size.',
    ),
    const LlmModelInfo(
      name: 'Llama 3.2 3B',
      fileName: 'Llama-3.2-3B-Instruct-Q4_K_M.gguf',
      downloadUrl: 'https://huggingface.co/bartowski/Llama-3.2-3B-Instruct-GGUF/resolve/main/Llama-3.2-3B-Instruct-Q4_K_M.gguf',
      sizeGb: 2.0,
      minRamMb: 4096,
      quantization: 'Q4_K_M',
      description: 'Meta\'s latest small model. Strong at roleplay and conversation.',
    ),
    const LlmModelInfo(
      name: 'Gemma 2 2B',
      fileName: 'gemma-2-2b-it-Q5_K_M.gguf',
      downloadUrl: 'https://huggingface.co/bartowski/gemma-2-2b-it-GGUF/resolve/main/gemma-2-2b-it-Q5_K_M.gguf',
      sizeGb: 1.8,
      minRamMb: 3072,
      quantization: 'Q5_K_M',
      description: 'Google\'s efficient model. Higher quant for better quality on capable devices.',
    ),
    const LlmModelInfo(
      name: 'Mistral 7B Instruct',
      fileName: 'mistral-7b-instruct-v0.3.Q4_K_M.gguf',
      downloadUrl: 'https://huggingface.co/bartowski/Mistral-7B-Instruct-v0.3-GGUF/resolve/main/Mistral-7B-Instruct-v0.3-Q4_K_M.gguf',
      sizeGb: 4.4,
      minRamMb: 6144,
      quantization: 'Q4_K_M',
      description: 'Premium 7B model. Outstanding quality but needs 6+ GB RAM.',
    ),
  ];
}

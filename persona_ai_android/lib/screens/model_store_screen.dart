import 'dart:io';
import 'package:flutter/material.dart';
import 'package:google_fonts/google_fonts.dart';
import 'package:flutter_animate/flutter_animate.dart';
import 'package:provider/provider.dart';
import 'package:dio/dio.dart';
import '../core/theme/scroll_theme.dart';
import '../data/models.dart';
import '../services/device_analyzer.dart';

/// Model Store screen — download & manage local LLM models with device-aware recommendations.
class ModelStoreScreen extends StatefulWidget {
  const ModelStoreScreen({super.key});

  @override
  State<ModelStoreScreen> createState() => _ModelStoreScreenState();
}

class _ModelStoreScreenState extends State<ModelStoreScreen> {
  final Map<String, double> _downloadProgress = {};
  final Map<String, bool> _downloadedModels = {};

  @override
  void initState() {
    super.initState();
    _checkDownloaded();
  }

  Future<void> _checkDownloaded() async {
    final analyzer = context.read<DeviceAnalyzer>();
    final models = analyzer.getRecommendedModels();
    for (final model in models) {
      _downloadedModels[model.fileName] = await analyzer.isModelDownloaded(model);
    }
    setState(() {});
  }

  Future<void> _downloadModel(LlmModelInfo model) async {
    final analyzer = context.read<DeviceAnalyzer>();
    final dir = await analyzer.modelsDirectory;
    final filePath = '$dir/${model.fileName}';

    setState(() => _downloadProgress[model.fileName] = 0);

    try {
      final dio = Dio();
      await dio.download(
        model.downloadUrl,
        filePath,
        onReceiveProgress: (received, total) {
          if (total > 0) {
            setState(() {
              _downloadProgress[model.fileName] = received / total;
            });
          }
        },
      );

      setState(() {
        _downloadProgress.remove(model.fileName);
        _downloadedModels[model.fileName] = true;
      });

      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text('✦ ${model.name} downloaded successfully')),
        );
      }
    } catch (e) {
      setState(() => _downloadProgress.remove(model.fileName));
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text('✖ Failed to download: ${e.toString()}')),
        );
      }
    }
  }

  Future<void> _deleteModel(LlmModelInfo model) async {
    final analyzer = context.read<DeviceAnalyzer>();
    final dir = await analyzer.modelsDirectory;
    final file = File('$dir/${model.fileName}');

    if (await file.exists()) {
      await file.delete();
      setState(() => _downloadedModels[model.fileName] = false);

      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text('${model.name} removed')),
        );
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    final analyzer = context.read<DeviceAnalyzer>();
    final models = analyzer.getRecommendedModels();

    return Scaffold(
      body: CustomScrollView(
        slivers: [
          SliverAppBar(
            expandedHeight: 100,
            pinned: true,
            flexibleSpace: FlexibleSpaceBar(
              title: Text(
                'MODEL STORE',
                style: GoogleFonts.cinzel(
                  fontSize: 20,
                  fontWeight: FontWeight.w700,
                  color: ScrollTheme.primary,
                  letterSpacing: 3,
                ),
              ),
            ),
          ),
          SliverPadding(
            padding: const EdgeInsets.all(16),
            sliver: SliverList(
              delegate: SliverChildListDelegate([
                // ─── Device Info Card ───────────────────────────
                _buildDeviceInfoCard(analyzer),
                const SizedBox(height: 24),

                // ─── Section Title ──────────────────────────────
                Text(
                  'AVAILABLE MODELS',
                  style: GoogleFonts.cinzel(
                    fontSize: 13,
                    fontWeight: FontWeight.w600,
                    color: ScrollTheme.primary,
                    letterSpacing: 2.5,
                  ),
                ),
                const SizedBox(height: 4),
                Text(
                  'Models are ranked by compatibility with your device',
                  style: GoogleFonts.crimsonText(
                    fontSize: 13,
                    color: ScrollTheme.textHint,
                  ),
                ),
                const SizedBox(height: 16),

                // ─── Model Cards ────────────────────────────────
                ...models.asMap().entries.map((e) =>
                  _buildModelCard(e.value, analyzer, e.key),
                ),
              ]),
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildDeviceInfoCard(DeviceAnalyzer analyzer) {
    return Container(
      decoration: ScrollTheme.goldBorderCard,
      padding: const EdgeInsets.all(16),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              Icon(Icons.phone_android, color: ScrollTheme.primary, size: 20),
              const SizedBox(width: 8),
              Text(
                'YOUR DEVICE',
                style: GoogleFonts.cinzel(
                  fontSize: 13,
                  fontWeight: FontWeight.w600,
                  color: ScrollTheme.primary,
                  letterSpacing: 2,
                ),
              ),
            ],
          ),
          const SizedBox(height: 12),
          _buildInfoRow('Model', analyzer.deviceModel),
          _buildInfoRow('Android', analyzer.androidVersion),
          _buildInfoRow('RAM', '${(analyzer.totalRamMb / 1024).toStringAsFixed(1)} GB'),
          _buildInfoRow('Tier', analyzer.ramTier),
        ],
      ),
    ).animate().fadeIn(duration: 400.ms).slideY(begin: 0.05, end: 0);
  }

  Widget _buildInfoRow(String label, String value) {
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 3),
      child: Row(
        children: [
          SizedBox(
            width: 70,
            child: Text(
              label,
              style: GoogleFonts.crimsonText(
                fontSize: 14,
                color: ScrollTheme.textHint,
              ),
            ),
          ),
          Expanded(
            child: Text(
              value,
              style: GoogleFonts.crimsonText(
                fontSize: 14,
                fontWeight: FontWeight.w600,
                color: ScrollTheme.textPrimary,
              ),
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildModelCard(LlmModelInfo model, DeviceAnalyzer analyzer, int index) {
    final isCompatible = analyzer.isModelCompatible(model);
    final isDownloaded = _downloadedModels[model.fileName] ?? false;
    final progress = _downloadProgress[model.fileName];
    final isDownloading = progress != null;

    return Padding(
      padding: const EdgeInsets.only(bottom: 12),
      child: Container(
        decoration: BoxDecoration(
          gradient: ScrollTheme.cardGradient,
          borderRadius: BorderRadius.circular(16),
          border: Border.all(
            color: isCompatible
                ? ScrollTheme.divider
                : ScrollTheme.error.withValues(alpha: 0.3),
            width: 0.5,
          ),
          boxShadow: ScrollTheme.subtleShadow,
        ),
        child: Column(
          children: [
            Padding(
              padding: const EdgeInsets.all(16),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  // Title row
                  Row(
                    children: [
                      Expanded(
                        child: Text(
                          model.name,
                          style: GoogleFonts.cinzel(
                            fontSize: 15,
                            fontWeight: FontWeight.w600,
                            color: isCompatible
                                ? ScrollTheme.textPrimary
                                : ScrollTheme.textHint,
                            letterSpacing: 0.5,
                          ),
                        ),
                      ),
                      // Compatibility badge
                      Container(
                        padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 4),
                        decoration: BoxDecoration(
                          color: isCompatible
                              ? ScrollTheme.success.withValues(alpha: 0.15)
                              : ScrollTheme.error.withValues(alpha: 0.15),
                          borderRadius: BorderRadius.circular(20),
                          border: Border.all(
                            color: isCompatible
                                ? ScrollTheme.success.withValues(alpha: 0.3)
                                : ScrollTheme.error.withValues(alpha: 0.3),
                          ),
                        ),
                        child: Text(
                          isCompatible ? 'Compatible' : 'Too Heavy',
                          style: GoogleFonts.crimsonText(
                            fontSize: 11,
                            fontWeight: FontWeight.w600,
                            color: isCompatible ? ScrollTheme.success : ScrollTheme.error,
                          ),
                        ),
                      ),
                    ],
                  ),
                  const SizedBox(height: 8),

                  // Description
                  Text(
                    model.description,
                    style: GoogleFonts.crimsonText(
                      fontSize: 14,
                      color: ScrollTheme.textSecondary,
                      height: 1.4,
                    ),
                  ),
                  const SizedBox(height: 12),

                  // Stats row
                  Row(
                    children: [
                      _buildStat(Icons.storage_outlined, '${model.sizeGb} GB'),
                      const SizedBox(width: 16),
                      _buildStat(Icons.memory, model.quantization),
                      const SizedBox(width: 16),
                      _buildStat(Icons.speed, '${(model.minRamMb / 1024).toStringAsFixed(1)} GB RAM'),
                    ],
                  ),
                ],
              ),
            ),

            // Download progress bar
            if (isDownloading)
              Padding(
                padding: const EdgeInsets.symmetric(horizontal: 16),
                child: Column(
                  children: [
                    ClipRRect(
                      borderRadius: BorderRadius.circular(4),
                      child: LinearProgressIndicator(
                        value: progress,
                        backgroundColor: ScrollTheme.surfaceVariant,
                        color: ScrollTheme.primary,
                        minHeight: 4,
                      ),
                    ),
                    const SizedBox(height: 4),
                    Text(
                      '${(progress * 100).toStringAsFixed(1)}%',
                      style: GoogleFonts.crimsonText(
                        fontSize: 12,
                        color: ScrollTheme.textHint,
                      ),
                    ),
                  ],
                ),
              ),

            // Action button
            Padding(
              padding: const EdgeInsets.all(12),
              child: SizedBox(
                width: double.infinity,
                child: isDownloaded
                    ? OutlinedButton.icon(
                        onPressed: () => _deleteModel(model),
                        icon: const Icon(Icons.delete_outline, size: 18),
                        label: Text(
                          'REMOVE',
                          style: GoogleFonts.cinzel(fontSize: 12, letterSpacing: 1),
                        ),
                        style: OutlinedButton.styleFrom(
                          foregroundColor: ScrollTheme.error,
                          side: BorderSide(color: ScrollTheme.error.withValues(alpha: 0.3)),
                          padding: const EdgeInsets.symmetric(vertical: 12),
                        ),
                      )
                    : ElevatedButton.icon(
                        onPressed: isDownloading || !isCompatible
                            ? null
                            : () => _downloadModel(model),
                        icon: Icon(
                          isDownloading ? Icons.hourglass_top : Icons.download,
                          size: 18,
                        ),
                        label: Text(
                          isDownloading ? 'DOWNLOADING...' : 'DOWNLOAD',
                          style: GoogleFonts.cinzel(fontSize: 12, letterSpacing: 1),
                        ),
                        style: ElevatedButton.styleFrom(
                          padding: const EdgeInsets.symmetric(vertical: 12),
                          disabledBackgroundColor: ScrollTheme.surfaceVariant,
                        ),
                      ),
              ),
            ),
          ],
        ),
      ),
    ).animate()
      .fadeIn(delay: Duration(milliseconds: 80 * index), duration: 400.ms)
      .slideY(begin: 0.05, end: 0);
  }

  Widget _buildStat(IconData icon, String text) {
    return Row(
      mainAxisSize: MainAxisSize.min,
      children: [
        Icon(icon, size: 14, color: ScrollTheme.textHint),
        const SizedBox(width: 4),
        Text(
          text,
          style: GoogleFonts.crimsonText(
            fontSize: 12,
            color: ScrollTheme.textHint,
          ),
        ),
      ],
    );
  }
}

import 'package:flutter/material.dart';
import 'package:google_fonts/google_fonts.dart';
import 'package:flutter_animate/flutter_animate.dart';
import 'package:provider/provider.dart';
import '../core/theme/scroll_theme.dart';
import '../data/models.dart';
import '../services/inference_service.dart';

/// Settings screen — configure inference mode (API / Local Server / Local LLM).
class SettingsScreen extends StatefulWidget {
  const SettingsScreen({super.key});

  @override
  State<SettingsScreen> createState() => _SettingsScreenState();
}

class _SettingsScreenState extends State<SettingsScreen> {
  late TextEditingController _apiUrlController;
  late TextEditingController _apiKeyController;
  late TextEditingController _modelController;
  late TextEditingController _serverController;

  @override
  void initState() {
    super.initState();
    final settings = context.read<InferenceManager>().settings;
    _apiUrlController = TextEditingController(text: settings.apiBaseUrl);
    _apiKeyController = TextEditingController(text: settings.apiKey);
    _modelController = TextEditingController(text: settings.modelName);
    _serverController = TextEditingController(text: settings.serverAddress);
  }

  @override
  void dispose() {
    _apiUrlController.dispose();
    _apiKeyController.dispose();
    _modelController.dispose();
    _serverController.dispose();
    super.dispose();
  }

  Future<void> _saveSettings(InferenceMode mode) async {
    final manager = context.read<InferenceManager>();
    await manager.updateSettings(manager.settings.copyWith(
      mode: mode,
      apiBaseUrl: _apiUrlController.text.trim(),
      apiKey: _apiKeyController.text.trim(),
      modelName: _modelController.text.trim(),
      serverAddress: _serverController.text.trim(),
    ));

    if (mounted) {
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
          content: Text(
            '✦ Settings saved — ${mode.name.toUpperCase()} mode active',
            style: GoogleFonts.crimsonText(),
          ),
        ),
      );
    }
  }

  @override
  Widget build(BuildContext context) {
    return Consumer<InferenceManager>(
      builder: (context, manager, _) {
        final mode = manager.settings.mode;

        return Scaffold(
          body: CustomScrollView(
            slivers: [
              SliverAppBar(
                expandedHeight: 100,
                pinned: true,
                flexibleSpace: FlexibleSpaceBar(
                  title: Text(
                    'SETTINGS',
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
                    // ─── Inference Mode Selector ────────────────
                    _buildSectionTitle('Inference Source'),
                    const SizedBox(height: 12),
                    _buildModeSelector(mode),
                    const SizedBox(height: 28),

                    // ─── Mode-specific settings ────────────────
                    if (mode == InferenceMode.api) ..._buildApiSettings(),
                    if (mode == InferenceMode.localServer) ..._buildServerSettings(),
                    if (mode == InferenceMode.localLlm) ..._buildLocalLlmSettings(),

                    const SizedBox(height: 28),

                    // ─── Save Button ───────────────────────────
                    SizedBox(
                      width: double.infinity,
                      child: ElevatedButton.icon(
                        onPressed: () => _saveSettings(mode),
                        icon: const Icon(Icons.save_outlined),
                        label: const Text('SAVE CONFIGURATION'),
                        style: ElevatedButton.styleFrom(
                          padding: const EdgeInsets.symmetric(vertical: 16),
                        ),
                      ),
                    ),
                    const SizedBox(height: 40),
                  ]),
                ),
              ),
            ],
          ),
        );
      },
    );
  }

  Widget _buildSectionTitle(String text) {
    return Text(
      text,
      style: GoogleFonts.cinzel(
        fontSize: 14,
        fontWeight: FontWeight.w600,
        color: ScrollTheme.primary,
        letterSpacing: 2,
      ),
    );
  }

  Widget _buildModeSelector(InferenceMode currentMode) {
    return Container(
      decoration: ScrollTheme.scrollCard,
      padding: const EdgeInsets.all(4),
      child: Row(
        children: InferenceMode.values.map((mode) {
          final isSelected = mode == currentMode;
          final label = switch (mode) {
            InferenceMode.api => 'API',
            InferenceMode.localServer => 'Server',
            InferenceMode.localLlm => 'On-Device',
          };
          final icon = switch (mode) {
            InferenceMode.api => Icons.cloud_outlined,
            InferenceMode.localServer => Icons.computer_outlined,
            InferenceMode.localLlm => Icons.phone_android_outlined,
          };

          return Expanded(
            child: GestureDetector(
              onTap: () {
                final mgr = context.read<InferenceManager>();
                mgr.updateSettings(mgr.settings.copyWith(mode: mode));
              },
              child: AnimatedContainer(
                duration: const Duration(milliseconds: 250),
                curve: Curves.easeOutCubic,
                padding: const EdgeInsets.symmetric(vertical: 14),
                decoration: BoxDecoration(
                  color: isSelected
                      ? ScrollTheme.accent.withValues(alpha: 0.2)
                      : Colors.transparent,
                  borderRadius: BorderRadius.circular(12),
                  border: Border.all(
                    color: isSelected
                        ? ScrollTheme.primary.withValues(alpha: 0.5)
                        : Colors.transparent,
                  ),
                ),
                child: Column(
                  children: [
                    Icon(
                      icon,
                      color: isSelected ? ScrollTheme.primary : ScrollTheme.textHint,
                      size: 22,
                    ),
                    const SizedBox(height: 6),
                    Text(
                      label,
                      style: GoogleFonts.cinzel(
                        fontSize: 11,
                        fontWeight: isSelected ? FontWeight.w700 : FontWeight.w400,
                        color: isSelected ? ScrollTheme.primary : ScrollTheme.textHint,
                        letterSpacing: 0.5,
                      ),
                    ),
                  ],
                ),
              ),
            ),
          );
        }).toList(),
      ),
    ).animate().fadeIn(duration: 400.ms);
  }

  List<Widget> _buildApiSettings() {
    return [
      _buildSectionTitle('API Configuration'),
      const SizedBox(height: 12),
      _buildTextField(
        controller: _apiUrlController,
        label: 'Base URL',
        hint: 'https://api.openai.com/v1',
        icon: Icons.link,
      ),
      const SizedBox(height: 12),
      _buildTextField(
        controller: _apiKeyController,
        label: 'API Key',
        hint: 'sk-...',
        icon: Icons.key,
        obscure: true,
      ),
      const SizedBox(height: 12),
      _buildTextField(
        controller: _modelController,
        label: 'Model Name',
        hint: 'gpt-4o-mini',
        icon: Icons.smart_toy_outlined,
      ),
    ];
  }

  List<Widget> _buildServerSettings() {
    return [
      _buildSectionTitle('Local Server (Laptop)'),
      const SizedBox(height: 8),
      Container(
        padding: const EdgeInsets.all(12),
        decoration: BoxDecoration(
          color: ScrollTheme.accent.withValues(alpha: 0.08),
          borderRadius: BorderRadius.circular(12),
          border: Border.all(color: ScrollTheme.accent.withValues(alpha: 0.2)),
        ),
        child: Row(
          children: [
            Icon(Icons.info_outline, size: 18, color: ScrollTheme.accent),
            const SizedBox(width: 10),
            Expanded(
              child: Text(
                'Run Ollama or LMStudio on your laptop. Both devices must be on the same WiFi network.',
                style: GoogleFonts.crimsonText(
                  fontSize: 13,
                  color: ScrollTheme.textSecondary,
                  height: 1.4,
                ),
              ),
            ),
          ],
        ),
      ),
      const SizedBox(height: 12),
      _buildTextField(
        controller: _serverController,
        label: 'Server Address',
        hint: '192.168.1.5:11434',
        icon: Icons.computer,
      ),
    ];
  }

  List<Widget> _buildLocalLlmSettings() {
    return [
      _buildSectionTitle('On-Device Inference'),
      const SizedBox(height: 8),
      Container(
        padding: const EdgeInsets.all(12),
        decoration: BoxDecoration(
          color: ScrollTheme.primary.withValues(alpha: 0.08),
          borderRadius: BorderRadius.circular(12),
          border: Border.all(color: ScrollTheme.primary.withValues(alpha: 0.2)),
        ),
        child: Row(
          children: [
            Icon(Icons.memory, size: 18, color: ScrollTheme.primary),
            const SizedBox(width: 10),
            Expanded(
              child: Text(
                'Download a GGUF model from the Models tab. The model runs directly on this device — no internet needed.',
                style: GoogleFonts.crimsonText(
                  fontSize: 13,
                  color: ScrollTheme.textSecondary,
                  height: 1.4,
                ),
              ),
            ),
          ],
        ),
      ),
    ];
  }

  Widget _buildTextField({
    required TextEditingController controller,
    required String label,
    required String hint,
    required IconData icon,
    bool obscure = false,
  }) {
    return TextField(
      controller: controller,
      obscureText: obscure,
      style: GoogleFonts.crimsonText(fontSize: 16, color: ScrollTheme.textPrimary),
      decoration: InputDecoration(
        labelText: label,
        hintText: hint,
        prefixIcon: Icon(icon, color: ScrollTheme.primary, size: 20),
      ),
    );
  }
}

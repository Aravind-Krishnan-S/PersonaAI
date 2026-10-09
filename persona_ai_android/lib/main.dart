import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:provider/provider.dart';
import 'core/theme/scroll_theme.dart';
import 'services/inference_service.dart';
import 'services/device_analyzer.dart';
import 'services/extraction_service.dart';
import 'screens/app_shell.dart';

void main() async {
  WidgetsFlutterBinding.ensureInitialized();

  // Lock to portrait for mobile-first experience
  await SystemChrome.setPreferredOrientations([
    DeviceOrientation.portraitUp,
    DeviceOrientation.portraitDown,
  ]);

  // Dark status bar to match theme
  SystemChrome.setSystemUIOverlayStyle(const SystemUiOverlayStyle(
    statusBarColor: Colors.transparent,
    statusBarIconBrightness: Brightness.light,
    systemNavigationBarColor: ScrollTheme.surface,
    systemNavigationBarIconBrightness: Brightness.light,
  ));

  // Initialize device analyzer
  final deviceAnalyzer = DeviceAnalyzer();
  await deviceAnalyzer.init();

  // Initialize inference manager
  final inferenceManager = InferenceManager();
  await inferenceManager.loadSettings();

  runApp(
    MultiProvider(
      providers: [
        ChangeNotifierProvider.value(value: inferenceManager),
        Provider.value(value: deviceAnalyzer),
        ProxyProvider<InferenceManager, ExtractionService>(
          update: (context, inference, previous) => ExtractionService(inference.service),
        ),
      ],
      child: const PersonaAIApp(),
    ),
  );
}

class PersonaAIApp extends StatelessWidget {
  const PersonaAIApp({super.key});

  @override
  Widget build(BuildContext context) {
    return MaterialApp(
      title: 'PersonaAI',
      debugShowCheckedModeBanner: false,
      theme: ScrollTheme.darkTheme,
      home: const AppShell(),
    );
  }
}

import 'package:flutter/material.dart';
import '../core/theme/scroll_theme.dart';
import 'library_screen.dart';
import 'settings_screen.dart';
import 'model_store_screen.dart';

/// Bottom navigation shell with Ancient Scroll styling.
class AppShell extends StatefulWidget {
  const AppShell({super.key});

  @override
  State<AppShell> createState() => _AppShellState();
}

class _AppShellState extends State<AppShell> with TickerProviderStateMixin {
  int _currentIndex = 0;
  late final AnimationController _fadeController;

  final List<Widget> _screens = const [
    LibraryScreen(),
    ModelStoreScreen(),
    SettingsScreen(),
  ];

  @override
  void initState() {
    super.initState();
    _fadeController = AnimationController(
      vsync: this,
      duration: const Duration(milliseconds: 300),
    )..forward();
  }

  @override
  void dispose() {
    _fadeController.dispose();
    super.dispose();
  }

  void _onTabTapped(int index) {
    if (index == _currentIndex) return;
    _fadeController.reset();
    setState(() => _currentIndex = index);
    _fadeController.forward();
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      body: FadeTransition(
        opacity: _fadeController,
        child: _screens[_currentIndex],
      ),
      bottomNavigationBar: Container(
        decoration: BoxDecoration(
          color: ScrollTheme.surface,
          border: Border(
            top: BorderSide(
              color: ScrollTheme.primary.withValues(alpha: 0.15),
              width: 0.5,
            ),
          ),
        ),
        child: BottomNavigationBar(
          currentIndex: _currentIndex,
          onTap: _onTabTapped,
          items: [
            BottomNavigationBarItem(
              icon: const Icon(Icons.auto_stories_outlined),
              activeIcon: const Icon(Icons.auto_stories),
              label: 'Library',
            ),
            BottomNavigationBarItem(
              icon: const Icon(Icons.download_outlined),
              activeIcon: const Icon(Icons.download),
              label: 'Models',
            ),
            BottomNavigationBarItem(
              icon: const Icon(Icons.settings_outlined),
              activeIcon: const Icon(Icons.settings),
              label: 'Settings',
            ),
          ],
        ),
      ),
    );
  }
}

import 'package:flutter/material.dart';
import 'package:google_fonts/google_fonts.dart';

/// Ancient Scroll Dark Theme — deep browns, aged golds, warm parchment tones
class ScrollTheme {
  // ─── Color Palette ───────────────────────────────────────────
  static const Color background = Color(0xFF0D0907);
  static const Color surface = Color(0xFF1A120B);
  static const Color surfaceVariant = Color(0xFF2C1A0E);
  static const Color cardColor = Color(0xFF241610);
  static const Color divider = Color(0xFF3C2A1A);

  static const Color primary = Color(0xFFD4A347);
  static const Color primaryDark = Color(0xFFC8963E);
  static const Color accent = Color(0xFFB85C1E);
  static const Color accentLight = Color(0xFFE07A30);

  static const Color textPrimary = Color(0xFFF5E6D0);
  static const Color textSecondary = Color(0xFFB8A088);
  static const Color textHint = Color(0xFF7A6650);

  static const Color success = Color(0xFF6B8E4E);
  static const Color error = Color(0xFFB34040);
  static const Color warning = Color(0xFFD4A347);

  // ─── Gradients ───────────────────────────────────────────────
  static const LinearGradient scrollGradient = LinearGradient(
    begin: Alignment.topCenter,
    end: Alignment.bottomCenter,
    colors: [Color(0xFF1A120B), Color(0xFF0D0907)],
  );

  static const LinearGradient goldShimmer = LinearGradient(
    colors: [Color(0xFFC8963E), Color(0xFFD4A347), Color(0xFFE8C066)],
  );

  static const LinearGradient cardGradient = LinearGradient(
    begin: Alignment.topLeft,
    end: Alignment.bottomRight,
    colors: [Color(0xFF2C1A0E), Color(0xFF1A120B)],
  );

  // ─── Shadows ─────────────────────────────────────────────────
  static List<BoxShadow> get subtleShadow => [
    BoxShadow(
      color: Colors.black.withValues(alpha: 0.4),
      blurRadius: 12,
      offset: const Offset(0, 4),
    ),
  ];

  static List<BoxShadow> get glowShadow => [
    BoxShadow(
      color: primary.withValues(alpha: 0.15),
      blurRadius: 20,
      spreadRadius: 2,
    ),
  ];

  // ─── Decorations ─────────────────────────────────────────────
  static BoxDecoration get scrollCard => BoxDecoration(
    gradient: cardGradient,
    borderRadius: BorderRadius.circular(16),
    border: Border.all(color: divider, width: 0.5),
    boxShadow: subtleShadow,
  );

  static BoxDecoration get goldBorderCard => BoxDecoration(
    color: surface,
    borderRadius: BorderRadius.circular(16),
    border: Border.all(
      color: primary.withValues(alpha: 0.3),
      width: 1.0,
    ),
    boxShadow: subtleShadow,
  );

  // ─── ThemeData ───────────────────────────────────────────────
  static ThemeData get darkTheme {
    return ThemeData(
      brightness: Brightness.dark,
      scaffoldBackgroundColor: background,
      colorScheme: const ColorScheme.dark(
        primary: primary,
        secondary: accent,
        surface: surface,
        error: error,
        onPrimary: background,
        onSecondary: textPrimary,
        onSurface: textPrimary,
        onError: textPrimary,
      ),
      cardColor: cardColor,
      dividerColor: divider,
      textTheme: _textTheme,
      appBarTheme: AppBarTheme(
        backgroundColor: surface,
        elevation: 0,
        centerTitle: true,
        titleTextStyle: GoogleFonts.cinzel(
          fontSize: 20,
          fontWeight: FontWeight.w600,
          color: primary,
          letterSpacing: 1.5,
        ),
        iconTheme: const IconThemeData(color: primary),
      ),
      bottomNavigationBarTheme: const BottomNavigationBarThemeData(
        backgroundColor: surface,
        selectedItemColor: primary,
        unselectedItemColor: textHint,
        type: BottomNavigationBarType.fixed,
        elevation: 8,
      ),
      floatingActionButtonTheme: const FloatingActionButtonThemeData(
        backgroundColor: accent,
        foregroundColor: textPrimary,
        elevation: 6,
      ),
      inputDecorationTheme: InputDecorationTheme(
        filled: true,
        fillColor: surfaceVariant,
        hintStyle: GoogleFonts.crimsonText(color: textHint, fontSize: 16),
        labelStyle: GoogleFonts.crimsonText(color: textSecondary),
        border: OutlineInputBorder(
          borderRadius: BorderRadius.circular(12),
          borderSide: const BorderSide(color: divider),
        ),
        enabledBorder: OutlineInputBorder(
          borderRadius: BorderRadius.circular(12),
          borderSide: const BorderSide(color: divider),
        ),
        focusedBorder: OutlineInputBorder(
          borderRadius: BorderRadius.circular(12),
          borderSide: const BorderSide(color: primary, width: 1.5),
        ),
        contentPadding: const EdgeInsets.symmetric(horizontal: 16, vertical: 14),
      ),
      elevatedButtonTheme: ElevatedButtonThemeData(
        style: ElevatedButton.styleFrom(
          backgroundColor: accent,
          foregroundColor: textPrimary,
          padding: const EdgeInsets.symmetric(horizontal: 24, vertical: 14),
          shape: RoundedRectangleBorder(
            borderRadius: BorderRadius.circular(12),
          ),
          textStyle: GoogleFonts.cinzel(
            fontWeight: FontWeight.w600,
            fontSize: 15,
            letterSpacing: 0.5,
          ),
        ),
      ),
      chipTheme: ChipThemeData(
        backgroundColor: surfaceVariant,
        selectedColor: primary.withValues(alpha: 0.2),
        labelStyle: GoogleFonts.crimsonText(color: textSecondary, fontSize: 14),
        side: const BorderSide(color: divider),
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(8)),
      ),
      snackBarTheme: SnackBarThemeData(
        backgroundColor: surfaceVariant,
        contentTextStyle: GoogleFonts.crimsonText(color: textPrimary, fontSize: 15),
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
        behavior: SnackBarBehavior.floating,
      ),
    );
  }

  static TextTheme get _textTheme {
    return TextTheme(
      displayLarge: GoogleFonts.cinzel(
        fontSize: 32, fontWeight: FontWeight.w700, color: primary, letterSpacing: 2,
      ),
      displayMedium: GoogleFonts.cinzel(
        fontSize: 26, fontWeight: FontWeight.w600, color: primary, letterSpacing: 1.5,
      ),
      headlineLarge: GoogleFonts.cinzel(
        fontSize: 22, fontWeight: FontWeight.w600, color: textPrimary, letterSpacing: 1.2,
      ),
      headlineMedium: GoogleFonts.cinzel(
        fontSize: 18, fontWeight: FontWeight.w500, color: textPrimary, letterSpacing: 1,
      ),
      titleLarge: GoogleFonts.cinzel(
        fontSize: 16, fontWeight: FontWeight.w600, color: textPrimary, letterSpacing: 0.8,
      ),
      titleMedium: GoogleFonts.crimsonText(
        fontSize: 16, fontWeight: FontWeight.w600, color: textPrimary,
      ),
      bodyLarge: GoogleFonts.crimsonText(
        fontSize: 17, fontWeight: FontWeight.w400, color: textPrimary, height: 1.6,
      ),
      bodyMedium: GoogleFonts.crimsonText(
        fontSize: 15, fontWeight: FontWeight.w400, color: textSecondary, height: 1.5,
      ),
      bodySmall: GoogleFonts.crimsonText(
        fontSize: 13, fontWeight: FontWeight.w400, color: textHint,
      ),
      labelLarge: GoogleFonts.cinzel(
        fontSize: 14, fontWeight: FontWeight.w600, color: textPrimary, letterSpacing: 0.5,
      ),
    );
  }
}

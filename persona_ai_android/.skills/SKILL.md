# Skill: PersonaAI Project Context
Manage and develop the PersonaAI Android Flutter application with the "Ancient Scroll" design system.

## Project Overview
PersonaAI is a multi-platform character persona engine. This Android client (`persona_ai_android`) is built with Flutter and mirrors the Windows C# architecture.

## Architecture
- **Theme**: `lib/core/theme/scroll_theme.dart` (Ancient Scroll Dark Theme).
- **Inference**: `lib/services/inference_service.dart` supports API, Local Server, and Local LLM (GGUF).
- **Data**: `lib/data/models.dart` (Character, Book, ChatMessage).
- **Storage**: `lib/services/database_service.dart` (SQLite).

## Design Rules for Agents
- **Aesthetics**: Use the "Ancient Scroll" theme constant values. Colors: Deep Brown (`0xFF0D0907`), Gold (`0xFFD4A347`).
- **Typography**: Use standard `GoogleFonts.cinzel()` for headers and `GoogleFonts.crimsonText()` for body.
- **Micro-animations**: Use `flutter_animate` for all layout transitions.

## Common Tasks
- **Add Character**: Update `DatabaseService` and refresh `LibraryScreen`.
- **Modify Inference**: Adjust settings in `InferenceManager` via `SettingsScreen`.
- **Local Models**: Recommend GGUF models via `DeviceAnalyzer` based on RAM tier.

## Environment Requirements
- Flutter SDK 3.41+
- Android SDK 36+ (Panda 3 recommended)

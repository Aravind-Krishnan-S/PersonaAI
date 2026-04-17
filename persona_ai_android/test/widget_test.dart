import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:persona_ai_android/main.dart';

void main() {
  testWidgets('App renders without crashing', (WidgetTester tester) async {
    // Smoke test — verifies the app builds the widget tree
    await tester.pumpWidget(const PersonaAIApp());
    expect(find.byType(MaterialApp), findsOneWidget);
  });
}

import 'dart:io';
import 'package:file_picker/file_picker.dart';
import 'package:syncfusion_flutter_pdf/pdf.dart';

/// Service for picking files and extracting text for character identification.
class IngestionService {
  static const int maxExtractionChars = 20000;

  /// Picks a document and returns its text content (limited to [maxExtractionChars]).
  static Future<ExtractedDocument?> pickAndExtractText() async {
    final result = await FilePicker.platform.pickFiles(
      type: FileType.custom,
      allowedExtensions: ['pdf', 'txt'],
    );

    if (result == null || result.files.isEmpty) return null;

    final file = result.files.first;
    final path = file.path;
    if (path == null) return null;

    String text = '';
    String title = file.name;

    if (path.toLowerCase().endsWith('.pdf')) {
      text = await _extractPdfText(path);
    } else {
      text = await File(path).readAsString();
    }

    // Limit text for cost/context window efficiency
    if (text.length > maxExtractionChars) {
      text = text.substring(0, maxExtractionChars);
    }

    return ExtractedDocument(
      title: title,
      filePath: path,
      text: text,
    );
  }

  static Future<String> _extractPdfText(String path) async {
    try {
      final bytes = await File(path).readAsBytes();
      final document = PdfDocument(inputBytes: bytes);
      final text = PdfTextExtractor(document).extractText();
      document.dispose();
      return text;
    } catch (e) {
      return '';
    }
  }
}

class ExtractedDocument {
  final String title;
  final String filePath;
  final String text;

  ExtractedDocument({
    required this.title,
    required this.filePath,
    required this.text,
  });
}

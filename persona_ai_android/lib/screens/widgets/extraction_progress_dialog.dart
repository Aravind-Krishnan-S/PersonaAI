import 'package:flutter/material.dart';
import 'package:google_fonts/google_fonts.dart';

class ExtractionProgressDialog extends StatelessWidget {
  final String status;

  const ExtractionProgressDialog({super.key, required this.status});

  @override
  Widget build(BuildContext context) {
    return Dialog(
      backgroundColor: Colors.transparent,
      child: Container(
        padding: const EdgeInsets.all(24),
        decoration: BoxDecoration(
          color: const Color(0xFF0D0907), // Scroll Theme Deep Brown
          borderRadius: BorderRadius.circular(12),
          border: Border.all(color: const Color(0xFFD4A347), width: 1.5), // Scroll Gold
          boxShadow: [
            BoxShadow(
              color: const Color(0xFFD4A347).withOpacity(0.2),
              blurRadius: 15,
              spreadRadius: 2,
            ),
          ],
        ),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            const CircularProgressIndicator(
              valueColor: AlwaysStoppedAnimation<Color>(Color(0xFFD4A347)),
            ),
            const SizedBox(height: 24),
            Text(
              status,
              textAlign: TextAlign.center,
              style: GoogleFonts.cinzel(
                color: const Color(0xFFD4A347),
                fontSize: 18,
                fontWeight: FontWeight.bold,
              ),
            ),
            const SizedBox(height: 8),
            Text(
              "Consulting the archives...",
              style: GoogleFonts.crimsonText(
                color: const Color(0xFFD4A347).withOpacity(0.7),
                fontSize: 14,
                fontStyle: FontStyle.italic,
              ),
            ),
          ],
        ),
      ),
    );
  }
}

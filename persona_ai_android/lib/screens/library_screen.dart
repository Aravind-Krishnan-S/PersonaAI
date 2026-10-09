import 'dart:convert';
import 'package:flutter/material.dart';
import 'package:google_fonts/google_fonts.dart';
import 'package:flutter_animate/flutter_animate.dart';
import 'package:provider/provider.dart';
import '../core/theme/scroll_theme.dart';
import '../data/models.dart';
import '../services/database_service.dart';
import '../services/ingestion_service.dart';
import '../services/extraction_service.dart';
import 'chat_screen.dart';
import 'widgets/extraction_progress_dialog.dart';

/// Library screen — shows all characters the user can chat with.
class LibraryScreen extends StatefulWidget {
  const LibraryScreen({super.key});

  @override
  State<LibraryScreen> createState() => _LibraryScreenState();
}

class _LibraryScreenState extends State<LibraryScreen> {
  List<Character> _characters = [];
  bool _isLoading = true;

  @override
  void initState() {
    super.initState();
    _loadCharacters();
  }

  Future<void> _loadCharacters() async {
    setState(() => _isLoading = true);
    final chars = await DatabaseService.getCharacters();
    setState(() {
      _characters = chars;
      _isLoading = false;
    });
  }

  Future<void> _addDemoCharacter() async {
    // Demo character for testing — in production, this comes from book parsing
    final character = Character(
      bookId: 0,
      name: 'Sherlock Holmes',
      aliases: 'Holmes, The Detective',
      profileJson: '{"name":"Sherlock Holmes","personality_traits":["analytical","observant","eccentric","brilliant"],"speech_style":"Precise, clipped Victorian English with dramatic flair","emotional_profile":"Detached but passionate about puzzles","knowledge_scope":["criminal investigation","chemistry","violin","boxing","London geography"],"knowledge_gaps":["astronomy","gardening","politics","popular literature"],"worldview":"Logic and observation reveal all truth","formality_level":"Formal Victorian","sentiment_baseline":"Intellectually restless"}',
      extractedAt: DateTime.now(),
    );
    await DatabaseService.insertCharacter(character);
    await _loadCharacters();
  }

  Future<void> _importFromBook() async {
    final doc = await IngestionService.pickAndExtractText();
    if (doc == null) return;

    if (!mounted) return;
    
    // Show progress dialog
    showDialog(
      context: context,
      barrierDismissible: false,
      builder: (context) => const ExtractionProgressDialog(status: "Reading Scroll..."),
    );

    try {
      // 1. Save Book
      final bookId = await DatabaseService.insertBook(Book(
        title: doc.title,
        filePath: doc.filePath,
        importedAt: DateTime.now(),
      ));

      // 2. Extract Characters
      if (!mounted) return;
      final extractionService = Provider.of<ExtractionService>(context, listen: false);
      
      // Update dialog status
      Navigator.of(context).pop();
      showDialog(
        context: context,
        barrierDismissible: false,
        builder: (context) => const ExtractionProgressDialog(status: "Identifying Characters..."),
      );

      final profiles = await extractionService.extractCharacters(doc.text);

      // 3. Save Characters
      for (final profile in profiles) {
        await DatabaseService.insertCharacter(Character(
          bookId: bookId,
          name: profile.name,
          aliases: profile.aliases.join(", "),
          profileJson: jsonEncode({
            'name': profile.name,
            'personality_traits': profile.personalityTraits,
            'speech_style': profile.speechStyle,
            'emotional_profile': profile.emotionalProfile,
            'knowledge_scope': profile.knowledgeScope,
            'knowledge_gaps': profile.knowledgeGaps,
            'relationships': profile.relationships,
            'sample_dialogue': profile.sampleDialogue,
            'worldview': profile.worldview,
            'formality_level': profile.formalityLevel,
            'sentiment_baseline': profile.sentimentBaseline,
          }),
          extractedAt: DateTime.now(),
        ));
      }

      if (!mounted) return;
      Navigator.of(context).pop(); // Close progress
      await _loadCharacters();

      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(content: Text('Succesfully imported ${profiles.length} characters from ${doc.title}')),
      );
    } catch (e) {
      if (!mounted) return;
      Navigator.of(context).pop(); // Close progress
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(content: Text('Failed to extract: $e')),
      );
    }
  }

  void _showAddMenu() {
    showModalBottomSheet(
      context: context,
      backgroundColor: ScrollTheme.surface,
      shape: const RoundedRectangleBorder(
        borderRadius: BorderRadius.vertical(top: Radius.circular(20)),
      ),
      builder: (context) => Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          const SizedBox(height: 8),
          Container(
            width: 40,
            height: 4,
            decoration: BoxDecoration(
              color: ScrollTheme.primary.withValues(alpha: 0.3),
              borderRadius: BorderRadius.circular(2),
            ),
          ),
          const SizedBox(height: 16),
          ListTile(
            leading: const Icon(Icons.auto_stories, color: ScrollTheme.primary),
            title: Text('Import from Book (PDF/TXT)', style: GoogleFonts.cinzel(fontSize: 16)),
            subtitle: Text('AI will read the book and find characters', style: GoogleFonts.crimsonText()),
            onTap: () {
              Navigator.pop(context);
              _importFromBook();
            },
          ),
          ListTile(
            leading: const Icon(Icons.person_add, color: ScrollTheme.primary),
            title: Text('Add Demo Character', style: GoogleFonts.cinzel(fontSize: 16)),
            onTap: () {
              Navigator.pop(context);
              _addDemoCharacter();
            },
          ),
          const SizedBox(height: 24),
        ],
      ),
    );
  }

  void _openChat(Character character) {
    Navigator.of(context).push(
      PageRouteBuilder(
        pageBuilder: (_, __, ___) => ChatScreen(character: character),
        transitionsBuilder: (_, animation, __, child) {
          return SlideTransition(
            position: Tween<Offset>(
              begin: const Offset(1, 0),
              end: Offset.zero,
            ).animate(CurvedAnimation(
              parent: animation,
              curve: Curves.easeOutCubic,
            )),
            child: child,
          );
        },
        transitionDuration: const Duration(milliseconds: 400),
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      body: CustomScrollView(
        slivers: [
          // ─── Scroll-themed App Bar ───────────────────────────
          SliverAppBar(
            expandedHeight: 160,
            floating: false,
            pinned: true,
            backgroundColor: ScrollTheme.surface,
            flexibleSpace: FlexibleSpaceBar(
              title: Text(
                'PERSONAAI',
                style: GoogleFonts.cinzel(
                  fontSize: 20,
                  fontWeight: FontWeight.w700,
                  color: ScrollTheme.primary,
                  letterSpacing: 3,
                ),
              ),
              background: Container(
                decoration: const BoxDecoration(gradient: ScrollTheme.scrollGradient),
                child: Center(
                  child: Column(
                    mainAxisAlignment: MainAxisAlignment.center,
                    children: [
                      const SizedBox(height: 20),
                      Icon(
                        Icons.auto_stories,
                        size: 48,
                        color: ScrollTheme.primary.withValues(alpha: 0.4),
                      ),
                      const SizedBox(height: 8),
                      Text(
                        'Converse with Characters',
                        style: GoogleFonts.crimsonText(
                          fontSize: 14,
                          color: ScrollTheme.textHint,
                          letterSpacing: 1,
                        ),
                      ),
                    ],
                  ),
                ),
              ),
            ),
          ),

          // ─── Content ─────────────────────────────────────────
          if (_isLoading)
            const SliverFillRemaining(
              child: Center(
                child: CircularProgressIndicator(color: ScrollTheme.primary),
              ),
            )
          else if (_characters.isEmpty)
            SliverFillRemaining(
              child: _buildEmptyState(),
            )
          else
            SliverPadding(
              padding: const EdgeInsets.all(16),
              sliver: SliverList(
                delegate: SliverChildBuilderDelegate(
                  (context, index) => _buildCharacterCard(_characters[index], index),
                  childCount: _characters.length,
                ),
              ),
            ),
        ],
      ),
      floatingActionButton: FloatingActionButton.extended(
        onPressed: _showAddMenu,
        icon: const Icon(Icons.add),
        label: Text(
          'Add',
          style: GoogleFonts.cinzel(fontWeight: FontWeight.w600, letterSpacing: 0.5),
        ),
      ),
    );
  }

  Widget _buildEmptyState() {
    return Center(
      child: Column(
        mainAxisAlignment: MainAxisAlignment.center,
        children: [
          Icon(
            Icons.menu_book_outlined,
            size: 80,
            color: ScrollTheme.primary.withValues(alpha: 0.2),
          ),
          const SizedBox(height: 24),
          Text(
            'Your Library Awaits',
            style: GoogleFonts.cinzel(
              fontSize: 22,
              fontWeight: FontWeight.w600,
              color: ScrollTheme.textPrimary,
              letterSpacing: 1.5,
            ),
          ),
          const SizedBox(height: 12),
          Text(
            'Add a character to begin your conversation\nacross the ages.',
            textAlign: TextAlign.center,
            style: GoogleFonts.crimsonText(
              fontSize: 16,
              color: ScrollTheme.textSecondary,
              height: 1.5,
            ),
          ),
          const SizedBox(height: 32),
          ElevatedButton.icon(
            onPressed: _addDemoCharacter,
            icon: const Icon(Icons.auto_fix_high),
            label: const Text('Add Demo Character'),
          ),
        ],
      ),
    ).animate().fadeIn(duration: 600.ms).slideY(begin: 0.1, end: 0);
  }

  Widget _buildCharacterCard(Character character, int index) {
    return Padding(
      padding: const EdgeInsets.only(bottom: 12),
      child: Material(
        color: Colors.transparent,
        child: InkWell(
          onTap: () => _openChat(character),
          borderRadius: BorderRadius.circular(16),
          splashColor: ScrollTheme.primary.withValues(alpha: 0.1),
          child: Container(
            decoration: ScrollTheme.scrollCard,
            padding: const EdgeInsets.all(16),
            child: Row(
              children: [
                // Avatar with gold ring
                Container(
                  width: 56,
                  height: 56,
                  decoration: BoxDecoration(
                    shape: BoxShape.circle,
                    gradient: ScrollTheme.goldShimmer,
                  ),
                  child: Container(
                    margin: const EdgeInsets.all(2),
                    decoration: const BoxDecoration(
                      shape: BoxShape.circle,
                      color: ScrollTheme.surface,
                    ),
                    child: Center(
                      child: Text(
                        character.name[0].toUpperCase(),
                        style: GoogleFonts.cinzel(
                          fontSize: 22,
                          fontWeight: FontWeight.w700,
                          color: ScrollTheme.primary,
                        ),
                      ),
                    ),
                  ),
                ),
                const SizedBox(width: 16),

                // Name and aliases
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(
                        character.name,
                        style: GoogleFonts.cinzel(
                          fontSize: 16,
                          fontWeight: FontWeight.w600,
                          color: ScrollTheme.textPrimary,
                          letterSpacing: 0.5,
                        ),
                      ),
                      if (character.aliases.isNotEmpty) ...[
                        const SizedBox(height: 4),
                        Text(
                          character.aliases,
                          style: GoogleFonts.crimsonText(
                            fontSize: 13,
                            color: ScrollTheme.textHint,
                            fontStyle: FontStyle.italic,
                          ),
                        ),
                      ],
                    ],
                  ),
                ),

                // Arrow
                Icon(
                  Icons.arrow_forward_ios,
                  size: 16,
                  color: ScrollTheme.primary.withValues(alpha: 0.5),
                ),
              ],
            ),
          ),
        ),
      ),
    ).animate()
      .fadeIn(delay: Duration(milliseconds: 100 * index), duration: 400.ms)
      .slideX(begin: 0.1, end: 0);
  }
}

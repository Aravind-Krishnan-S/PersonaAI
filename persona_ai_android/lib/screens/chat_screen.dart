import 'dart:convert';
import 'package:flutter/material.dart';
import 'package:google_fonts/google_fonts.dart';
import 'package:flutter_animate/flutter_animate.dart';
import 'package:provider/provider.dart';
import '../core/theme/scroll_theme.dart';
import '../data/models.dart';
import '../services/database_service.dart';
import '../services/inference_service.dart';

/// Chat screen — ancient scroll themed conversation interface.
class ChatScreen extends StatefulWidget {
  final Character character;
  const ChatScreen({super.key, required this.character});

  @override
  State<ChatScreen> createState() => _ChatScreenState();
}

class _ChatScreenState extends State<ChatScreen> with TickerProviderStateMixin {
  final TextEditingController _messageController = TextEditingController();
  final ScrollController _scrollController = ScrollController();
  final FocusNode _focusNode = FocusNode();

  List<ChatMessage> _messages = [];
  bool _isGenerating = false;
  String _currentAssistantMessage = '';
  CharacterProfile? _profile;

  @override
  void initState() {
    super.initState();
    _loadProfile();
    _loadHistory();
  }

  void _loadProfile() {
    try {
      final json = jsonDecode(widget.character.profileJson) as Map<String, dynamic>;
      _profile = CharacterProfile.fromJson(json);
    } catch (_) {
      _profile = const CharacterProfile();
    }
  }

  Future<void> _loadHistory() async {
    final history = await DatabaseService.getChatHistory(widget.character.id ?? 0);
    setState(() => _messages = history);
    _scrollToBottom();
  }

  void _scrollToBottom() {
    WidgetsBinding.instance.addPostFrameCallback((_) {
      if (_scrollController.hasClients) {
        _scrollController.animateTo(
          _scrollController.position.maxScrollExtent,
          duration: const Duration(milliseconds: 300),
          curve: Curves.easeOutCubic,
        );
      }
    });
  }

  String _buildSystemPrompt() {
    final p = _profile ?? const CharacterProfile();
    final traits = p.personalityTraits.isNotEmpty
        ? p.personalityTraits.join(', ')
        : 'Dynamic';
    final gaps = p.knowledgeGaps.isNotEmpty
        ? p.knowledgeGaps.join(', ')
        : 'The future';
    final scope = p.knowledgeScope.isNotEmpty
        ? p.knowledgeScope.join(', ')
        : 'Their environment';

    return '''You are roleplaying purely and strictly as ${widget.character.name}.

# Core Personality
Traits: $traits
Speech Style: ${p.speechStyle.isNotEmpty ? p.speechStyle : 'Typical'}
Emotional Baseline: ${p.emotionalProfile.isNotEmpty ? p.emotionalProfile : 'Neutral'}
Worldview: ${p.worldview.isNotEmpty ? p.worldview : 'Neutral'}

# Knowledge Scope
You ONLY possess the knowledge that ${widget.character.name} possesses.
Known boundaries: $scope.
Explicit Knowledge Gaps: $gaps.
You must refuse or express confusion if asked about modern topics outside your world.

Maintain your Speech Style and Formality Level at all times. Respond exactly as ${widget.character.name} would.''';
  }

  Future<void> _sendMessage() async {
    final text = _messageController.text.trim();
    if (text.isEmpty || _isGenerating) return;

    _messageController.clear();
    _focusNode.unfocus();

    // Add user message
    final userMsg = ChatMessage(
      characterId: widget.character.id ?? 0,
      role: 'user',
      content: text,
      timestamp: DateTime.now(),
    );
    await DatabaseService.insertMessage(userMsg);

    setState(() {
      _messages.add(userMsg);
      _isGenerating = true;
      _currentAssistantMessage = '';
    });
    _scrollToBottom();

    // Build messages payload
    final engineMessages = <Map<String, String>>[
      {'role': 'system', 'content': _buildSystemPrompt()},
      ..._messages.map((m) => {'role': m.role, 'content': m.content}),
    ];

    // Stream response
    final engine = context.read<InferenceManager>().engine;
    if (engine == null) {
      setState(() {
        _currentAssistantMessage = '⚠️ No inference engine configured. Go to Settings to set up API, Local Server, or Local LLM.';
        _isGenerating = false;
      });
      _saveAssistantMessage();
      return;
    }

    try {
      await for (final token in engine.streamChat(engineMessages)) {
        if (!mounted) return;
        setState(() => _currentAssistantMessage += token);
        _scrollToBottom();
      }
    } catch (e) {
      setState(() {
        _currentAssistantMessage += '\n\n⚠️ Error: ${e.toString()}';
      });
    }

    setState(() => _isGenerating = false);
    await _saveAssistantMessage();
  }

  Future<void> _saveAssistantMessage() async {
    if (_currentAssistantMessage.isEmpty) return;

    final assistantMsg = ChatMessage(
      characterId: widget.character.id ?? 0,
      role: 'assistant',
      content: _currentAssistantMessage,
      timestamp: DateTime.now(),
    );
    await DatabaseService.insertMessage(assistantMsg);

    setState(() {
      _messages.add(assistantMsg);
      _currentAssistantMessage = '';
    });
  }

  Future<void> _clearHistory() async {
    final confirm = await showDialog<bool>(
      context: context,
      builder: (ctx) => AlertDialog(
        backgroundColor: ScrollTheme.surface,
        title: Text('Clear Conversation', style: GoogleFonts.cinzel(color: ScrollTheme.primary)),
        content: Text(
          'All messages with ${widget.character.name} will be erased.',
          style: GoogleFonts.crimsonText(color: ScrollTheme.textSecondary),
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(ctx, false),
            child: Text('Cancel', style: GoogleFonts.crimsonText(color: ScrollTheme.textHint)),
          ),
          TextButton(
            onPressed: () => Navigator.pop(ctx, true),
            child: Text('Erase', style: GoogleFonts.crimsonText(color: ScrollTheme.error)),
          ),
        ],
      ),
    );
    if (confirm == true) {
      await DatabaseService.clearChatHistory(widget.character.id ?? 0);
      setState(() => _messages.clear());
    }
  }

  @override
  void dispose() {
    _messageController.dispose();
    _scrollController.dispose();
    _focusNode.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: Column(
          children: [
            Text(
              widget.character.name,
              style: GoogleFonts.cinzel(
                fontSize: 18,
                fontWeight: FontWeight.w600,
                color: ScrollTheme.primary,
                letterSpacing: 1.5,
              ),
            ),
            Consumer<InferenceManager>(
              builder: (_, mgr, __) => Text(
                mgr.engine?.displayName ?? 'No Engine',
                style: GoogleFonts.crimsonText(
                  fontSize: 11,
                  color: ScrollTheme.textHint,
                ),
              ),
            ),
          ],
        ),
        actions: [
          IconButton(
            icon: const Icon(Icons.delete_outline, size: 20),
            onPressed: _clearHistory,
            tooltip: 'Clear History',
          ),
        ],
      ),
      body: Container(
        decoration: const BoxDecoration(gradient: ScrollTheme.scrollGradient),
        child: Column(
          children: [
            // ─── Messages List ─────────────────────────────────
            Expanded(
              child: _messages.isEmpty && _currentAssistantMessage.isEmpty
                  ? _buildWelcomeState()
                  : ListView.builder(
                      controller: _scrollController,
                      padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 12),
                      itemCount: _messages.length + (_isGenerating ? 1 : 0),
                      itemBuilder: (context, index) {
                        if (index < _messages.length) {
                          return _buildMessageBubble(_messages[index]);
                        }
                        // Currently streaming message
                        return _buildStreamingBubble();
                      },
                    ),
            ),

            // ─── Input Bar ─────────────────────────────────────
            _buildInputBar(),
          ],
        ),
      ),
    );
  }

  Widget _buildWelcomeState() {
    return Center(
      child: Column(
        mainAxisAlignment: MainAxisAlignment.center,
        children: [
          Container(
            width: 80,
            height: 80,
            decoration: BoxDecoration(
              shape: BoxShape.circle,
              gradient: ScrollTheme.goldShimmer,
            ),
            child: Container(
              margin: const EdgeInsets.all(3),
              decoration: const BoxDecoration(
                shape: BoxShape.circle,
                color: ScrollTheme.surface,
              ),
              child: Center(
                child: Text(
                  widget.character.name[0],
                  style: GoogleFonts.cinzel(
                    fontSize: 32,
                    fontWeight: FontWeight.w700,
                    color: ScrollTheme.primary,
                  ),
                ),
              ),
            ),
          ),
          const SizedBox(height: 20),
          Text(
            'Begin your conversation with',
            style: GoogleFonts.crimsonText(
              fontSize: 16,
              color: ScrollTheme.textSecondary,
            ),
          ),
          const SizedBox(height: 4),
          Text(
            widget.character.name,
            style: GoogleFonts.cinzel(
              fontSize: 22,
              fontWeight: FontWeight.w600,
              color: ScrollTheme.primary,
              letterSpacing: 1.5,
            ),
          ),
          if (_profile != null && _profile!.personalityTraits.isNotEmpty) ...[
            const SizedBox(height: 16),
            Wrap(
              spacing: 8,
              runSpacing: 8,
              alignment: WrapAlignment.center,
              children: _profile!.personalityTraits.take(4).map((trait) {
                return Container(
                  padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 6),
                  decoration: BoxDecoration(
                    color: ScrollTheme.surfaceVariant,
                    borderRadius: BorderRadius.circular(20),
                    border: Border.all(color: ScrollTheme.divider),
                  ),
                  child: Text(
                    trait,
                    style: GoogleFonts.crimsonText(
                      fontSize: 13,
                      color: ScrollTheme.textHint,
                    ),
                  ),
                );
              }).toList(),
            ),
          ],
        ],
      ).animate().fadeIn(duration: 600.ms).scale(begin: const Offset(0.95, 0.95)),
    );
  }

  Widget _buildMessageBubble(ChatMessage message) {
    final isUser = message.role == 'user';

    return Padding(
      padding: const EdgeInsets.only(bottom: 12),
      child: Row(
        mainAxisAlignment: isUser ? MainAxisAlignment.end : MainAxisAlignment.start,
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          if (!isUser) ...[
            // Character avatar
            Container(
              width: 32,
              height: 32,
              margin: const EdgeInsets.only(top: 4),
              decoration: BoxDecoration(
                shape: BoxShape.circle,
                gradient: ScrollTheme.goldShimmer,
              ),
              child: Center(
                child: Text(
                  widget.character.name[0],
                  style: GoogleFonts.cinzel(
                    fontSize: 14,
                    fontWeight: FontWeight.w700,
                    color: ScrollTheme.background,
                  ),
                ),
              ),
            ),
            const SizedBox(width: 8),
          ],
          Flexible(
            child: Container(
              padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 12),
              decoration: BoxDecoration(
                color: isUser
                    ? ScrollTheme.accent.withValues(alpha: 0.15)
                    : ScrollTheme.surfaceVariant,
                borderRadius: BorderRadius.only(
                  topLeft: const Radius.circular(16),
                  topRight: const Radius.circular(16),
                  bottomLeft: Radius.circular(isUser ? 16 : 4),
                  bottomRight: Radius.circular(isUser ? 4 : 16),
                ),
                border: Border.all(
                  color: isUser
                      ? ScrollTheme.accent.withValues(alpha: 0.2)
                      : ScrollTheme.divider,
                  width: 0.5,
                ),
              ),
              child: Text(
                message.content,
                style: GoogleFonts.crimsonText(
                  fontSize: 16,
                  color: ScrollTheme.textPrimary,
                  height: 1.5,
                ),
              ),
            ),
          ),
          if (isUser) const SizedBox(width: 40),
        ],
      ),
    );
  }

  Widget _buildStreamingBubble() {
    return Padding(
      padding: const EdgeInsets.only(bottom: 12),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Container(
            width: 32,
            height: 32,
            margin: const EdgeInsets.only(top: 4),
            decoration: BoxDecoration(
              shape: BoxShape.circle,
              gradient: ScrollTheme.goldShimmer,
            ),
            child: Center(
              child: Text(
                widget.character.name[0],
                style: GoogleFonts.cinzel(
                  fontSize: 14,
                  fontWeight: FontWeight.w700,
                  color: ScrollTheme.background,
                ),
              ),
            ),
          ),
          const SizedBox(width: 8),
          Flexible(
            child: Container(
              padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 12),
              decoration: BoxDecoration(
                color: ScrollTheme.surfaceVariant,
                borderRadius: const BorderRadius.only(
                  topLeft: Radius.circular(16),
                  topRight: Radius.circular(16),
                  bottomLeft: Radius.circular(4),
                  bottomRight: Radius.circular(16),
                ),
                border: Border.all(color: ScrollTheme.divider, width: 0.5),
              ),
              child: _currentAssistantMessage.isEmpty
                  ? _buildTypingIndicator()
                  : Text(
                      _currentAssistantMessage,
                      style: GoogleFonts.crimsonText(
                        fontSize: 16,
                        color: ScrollTheme.textPrimary,
                        height: 1.5,
                      ),
                    ),
            ),
          ),
        ],
      ),
    ).animate().fadeIn(duration: 300.ms);
  }

  Widget _buildTypingIndicator() {
    return Row(
      mainAxisSize: MainAxisSize.min,
      children: List.generate(3, (i) {
        return Container(
          width: 8,
          height: 8,
          margin: const EdgeInsets.symmetric(horizontal: 3),
          decoration: BoxDecoration(
            shape: BoxShape.circle,
            color: ScrollTheme.primary.withValues(alpha: 0.5),
          ),
        )
            .animate(onPlay: (c) => c.repeat())
            .fadeIn(delay: Duration(milliseconds: 200 * i), duration: 400.ms)
            .then()
            .fadeOut(duration: 400.ms);
      }),
    );
  }

  Widget _buildInputBar() {
    return Container(
      padding: EdgeInsets.only(
        left: 16,
        right: 8,
        top: 12,
        bottom: MediaQuery.of(context).padding.bottom + 12,
      ),
      decoration: BoxDecoration(
        color: ScrollTheme.surface,
        border: Border(
          top: BorderSide(color: ScrollTheme.primary.withValues(alpha: 0.1)),
        ),
      ),
      child: Row(
        children: [
          Expanded(
            child: TextField(
              controller: _messageController,
              focusNode: _focusNode,
              style: GoogleFonts.crimsonText(
                fontSize: 16,
                color: ScrollTheme.textPrimary,
              ),
              decoration: InputDecoration(
                hintText: 'Speak unto ${widget.character.name}...',
                filled: true,
                fillColor: ScrollTheme.surfaceVariant,
                border: OutlineInputBorder(
                  borderRadius: BorderRadius.circular(24),
                  borderSide: BorderSide.none,
                ),
                contentPadding: const EdgeInsets.symmetric(
                  horizontal: 20,
                  vertical: 12,
                ),
              ),
              maxLines: 4,
              minLines: 1,
              textCapitalization: TextCapitalization.sentences,
              onSubmitted: (_) => _sendMessage(),
            ),
          ),
          const SizedBox(width: 8),
          Container(
            decoration: BoxDecoration(
              shape: BoxShape.circle,
              gradient: _isGenerating ? null : ScrollTheme.goldShimmer,
              color: _isGenerating ? ScrollTheme.surfaceVariant : null,
            ),
            child: IconButton(
              onPressed: _isGenerating ? null : _sendMessage,
              icon: Icon(
                _isGenerating ? Icons.hourglass_top : Icons.send,
                color: _isGenerating ? ScrollTheme.textHint : ScrollTheme.background,
                size: 20,
              ),
            ),
          ),
        ],
      ),
    );
  }
}

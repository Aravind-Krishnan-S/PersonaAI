import 'package:sqflite/sqflite.dart';
import 'package:path/path.dart' as p;
import '../data/models.dart';

/// SQLite database service — mirrors the Windows C# DatabaseService schema exactly.
class DatabaseService {
  static Database? _db;

  static Future<Database> get database async {
    if (_db != null) return _db!;
    final dbPath = await getDatabasesPath();
    final path = p.join(dbPath, 'persona_ai.db');
    _db = await openDatabase(
      path,
      version: 1,
      onCreate: (db, version) async {
        await db.execute('''
          CREATE TABLE IF NOT EXISTS Books (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            Title TEXT NOT NULL,
            FilePath TEXT NOT NULL,
            ImportedAt TEXT NOT NULL
          )
        ''');
        await db.execute('''
          CREATE TABLE IF NOT EXISTS Characters (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            BookId INTEGER NOT NULL,
            Name TEXT NOT NULL,
            Aliases TEXT,
            ProfileJson TEXT,
            ExtractedAt TEXT NOT NULL
          )
        ''');
        await db.execute('''
          CREATE TABLE IF NOT EXISTS ChatHistory (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            CharacterId INTEGER NOT NULL,
            Role TEXT NOT NULL,
            Content TEXT NOT NULL,
            Timestamp TEXT NOT NULL
          )
        ''');
        await db.execute('''
          CREATE TABLE IF NOT EXISTS Settings (
            Key TEXT PRIMARY KEY,
            Value TEXT
          )
        ''');
      },
    );
    return _db!;
  }

  // ─── Settings ────────────────────────────────────────────────

  static Future<String> getSetting(String key, [String defaultValue = '']) async {
    final db = await database;
    final result = await db.query('Settings', where: 'Key = ?', whereArgs: [key]);
    if (result.isEmpty) return defaultValue;
    return (result.first['Value'] as String?) ?? defaultValue;
  }

  static Future<void> saveSetting(String key, String value) async {
    final db = await database;
    await db.insert(
      'Settings',
      {'Key': key, 'Value': value},
      conflictAlgorithm: ConflictAlgorithm.replace,
    );
  }

  // ─── Books ───────────────────────────────────────────────────

  static Future<List<Book>> getBooks() async {
    final db = await database;
    final maps = await db.query('Books');
    return maps.map((m) => Book.fromMap(m)).toList();
  }

  static Future<int> insertBook(Book book) async {
    final db = await database;
    return db.insert('Books', book.toMap());
  }

  // ─── Characters ──────────────────────────────────────────────

  static Future<List<Character>> getCharacters() async {
    final db = await database;
    final maps = await db.query('Characters');
    return maps.map((m) => Character.fromMap(m)).toList();
  }

  static Future<int> insertCharacter(Character character) async {
    final db = await database;
    return db.insert('Characters', character.toMap());
  }

  // ─── Chat History ────────────────────────────────────────────

  static Future<List<ChatMessage>> getChatHistory(int characterId) async {
    final db = await database;
    final maps = await db.query(
      'ChatHistory',
      where: 'CharacterId = ?',
      whereArgs: [characterId],
      orderBy: 'Timestamp ASC',
    );
    return maps.map((m) => ChatMessage.fromMap(m)).toList();
  }

  static Future<void> insertMessage(ChatMessage msg) async {
    final db = await database;
    await db.insert('ChatHistory', msg.toMap());
  }

  static Future<void> clearChatHistory(int characterId) async {
    final db = await database;
    await db.delete('ChatHistory', where: 'CharacterId = ?', whereArgs: [characterId]);
  }
}

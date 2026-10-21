#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace ANIMOL.Art.SunsetSanctumV02
{
    // Presence/type/duplicate guard around JsonUtility: a missing bool must not silently become false.
    // This parses structure only. JsonUtility still supplies the builder's strongly typed records.
    public static class SunsetSanctumV02JsonSchema
    {
        private sealed class Node
        {
            public char kind;
            public Dictionary<string, Node> fields;
            public List<Node> items;
        }
        public static void Validate(string json)
        {
            var parser = new Parser(json); Node root = parser.Parse();
            Fields(root, "schemaVersion", 'n', "packageId", 's', "sprites", 'a', "clips", 'a');
            foreach (Node row in root.fields["sprites"].items)
            {
                Fields(row, "file", 's', "width", 'n', "height", 'n', "ppu", 'n', "pivot", 'a', "sha256", 's');
                List<Node> pivot = row.fields["pivot"].items;
                Check(pivot.Count == 2 && pivot[0].kind == 'n' && pivot[1].kind == 'n', "pivot must be two numbers.");
            }
            foreach (Node row in root.fields["clips"].items)
            {
                Fields(row, "key", 's', "family", 's', "heading", 's', "state", 's', "fps", 'n', "loop", 'b', "frames", 'a');
                foreach (Node frame in row.fields["frames"].items) Check(frame.kind == 's', "frames[] must contain strings.");
            }
            Node parts;
            if (root.fields.TryGetValue("parts", out parts))
            {
                Check(parts.kind == 'a', "parts must be an array when present.");
                foreach (Node row in parts.items) Fields(row, "key", 's', "file", 's');
            }
        }
        private static void Fields(Node node, params object[] requirements)
        {
            Check(node.kind == 'o', "Expected JSON object.");
            for (int i = 0; i < requirements.Length; i += 2)
            {
                string key = (string)requirements[i]; Node field;
                Check(node.fields.TryGetValue(key, out field) && field.kind == (char)requirements[i + 1], "Missing/wrong JSON field: " + key);
            }
        }
        private static void Check(bool condition, string message) { if (!condition) throw new InvalidDataException(message); }

        private sealed class Parser
        {
            private readonly string text;
            private int cursor;
            private static readonly Regex Number = new Regex(@"^-?(0|[1-9][0-9]*)(\.[0-9]+)?([eE][+-]?[0-9]+)?$", RegexOptions.CultureInvariant);
            public Parser(string source) { text = source ?? ""; }
            public Node Parse()
            {
                if (text.Length > 0 && text[0] == '\ufeff') cursor++;
                Node node = Value(0); White(); Check(cursor == text.Length, "Trailing JSON data."); return node;
            }
            private Node Value(int depth)
            {
                Check(depth <= 64, "JSON nesting too deep."); White(); Check(cursor < text.Length, "Unexpected JSON end.");
                char c = text[cursor];
                if (c == '{')
                {
                    cursor++; var n = new Node { kind = 'o', fields = new Dictionary<string, Node>(StringComparer.Ordinal) };
                    White(); if (Take('}')) return n;
                    while (true)
                    {
                        White(); string key = Quoted(); White(); Check(Take(':'), "Expected colon.");
                        Check(!n.fields.ContainsKey(key), "Duplicate JSON property: " + key);
                        n.fields.Add(key, Value(depth + 1)); White();
                        if (Take('}')) return n; Check(Take(','), "Expected object comma.");
                    }
                }
                if (c == '[')
                {
                    cursor++; var n = new Node { kind = 'a', items = new List<Node>() };
                    White(); if (Take(']')) return n;
                    while (true)
                    {
                        n.items.Add(Value(depth + 1)); White(); if (Take(']')) return n;
                        Check(Take(','), "Expected array comma.");
                    }
                }
                if (c == '"') { Quoted(); return new Node { kind = 's' }; }
                int start = cursor;
                while (cursor < text.Length && ",]} \t\r\n".IndexOf(text[cursor]) < 0) cursor++;
                string token = text.Substring(start, cursor - start);
                if (token == "true" || token == "false") return new Node { kind = 'b' };
                if (token == "null") return new Node { kind = '0' };
                Check(Number.IsMatch(token), "Invalid JSON scalar."); return new Node { kind = 'n' };
            }
            private string Quoted()
            {
                Check(Take('"'), "Expected JSON string."); var b = new StringBuilder();
                while (cursor < text.Length)
                {
                    char c = text[cursor++]; if (c == '"') return b.ToString();
                    Check(c >= 32, "Control character in JSON string.");
                    if (c != '\\') { b.Append(c); continue; }
                    Check(cursor < text.Length, "Incomplete JSON escape."); char e = text[cursor++];
                    if (e == '"' || e == '\\' || e == '/') b.Append(e);
                    else if (e == 'b') b.Append('\b'); else if (e == 'f') b.Append('\f');
                    else if (e == 'n') b.Append('\n'); else if (e == 'r') b.Append('\r'); else if (e == 't') b.Append('\t');
                    else if (e == 'u')
                    {
                        Check(cursor + 4 <= text.Length, "Incomplete Unicode escape."); int code;
                        Check(int.TryParse(text.Substring(cursor, 4), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out code), "Invalid Unicode escape.");
                        b.Append((char)code); cursor += 4;
                    }
                    else Check(false, "Invalid JSON escape.");
                }
                throw new InvalidDataException("Unterminated JSON string.");
            }
            private bool Take(char c) { if (cursor < text.Length && text[cursor] == c) { cursor++; return true; } return false; }
            private void White() { while (cursor < text.Length && " \t\r\n".IndexOf(text[cursor]) >= 0) cursor++; }
        }
    }
}
#endif

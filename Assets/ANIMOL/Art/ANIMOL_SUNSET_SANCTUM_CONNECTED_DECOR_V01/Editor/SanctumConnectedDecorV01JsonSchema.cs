#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace ANIMOL.Art.SanctumConnectedDecorV01
{
    // Required-field/type/duplicate guard around JsonUtility before typed deserialization.
    // This parses structure only. JsonUtility still supplies the builder's strongly typed records.
    public static class SanctumConnectedDecorV01JsonSchema
    {
        private sealed class Node
        {
            public char kind;
            public string number;
            public Dictionary<string, Node> fields;
            public List<Node> items;
        }
        public static void Validate(string json)
        {
            var parser = new Parser(json); Node root = parser.Parse();
            Fields(root, "schemaVersion", 'n', "packageId", 's', "assets", 'a', "groups", 'a');
            Integer(root.fields["schemaVersion"]);
            foreach (Node row in root.fields["assets"].items)
            {
                Fields(row, "file", 's', "width", 'n', "height", 'n', "ppu", 'n', "pivot", 'a', "sha256", 's',
                    "logicalRect", 'a', "footprintCells", 'a', "family", 's', "kind", 's');
                foreach (string key in new[] { "pivot", "logicalRect", "footprintCells" })
                    foreach (Node number in row.fields[key].items) Check(number.kind == 'n', key + " must contain numbers.");
                Integer(row.fields["width"]); Integer(row.fields["height"]);
                foreach (string key in new[] { "logicalRect", "footprintCells" })
                    foreach (Node number in row.fields[key].items) Integer(number);
            }
            foreach (Node row in root.fields["groups"].items)
            {
                Fields(row, "key", 's', "file", 's', "footprintCells", 'a');
                foreach (Node number in row.fields["footprintCells"].items) Integer(number);
            }
        }
        private static void Integer(Node node)
        { Check(node.kind == 'n' && Regex.IsMatch(node.number, @"^-?(0|[1-9][0-9]*)$", RegexOptions.CultureInvariant), "Expected an exact integer JSON literal."); }
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
                Check(Number.IsMatch(token), "Invalid JSON scalar."); return new Node { kind = 'n', number = token };
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
                        string digits = text.Substring(cursor, 4);
                        foreach (char digit in digits)
                            Check((digit >= '0' && digit <= '9') || (digit >= 'a' && digit <= 'f') || (digit >= 'A' && digit <= 'F'), "Invalid Unicode escape.");
                        Check(int.TryParse(digits, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out code), "Invalid Unicode escape.");
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

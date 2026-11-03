#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace ANIMOL.Art.RanchConnectedDecorV02
{
    /// <summary>Strict flat board format guard before JsonUtility may supply any default values.</summary>
    public static class RanchConnectedDecorV02BoardJson
    {
        private sealed class Node
        {
            public char kind;
            public string scalar;
            public Dictionary<string, Node> fields;
            public List<Node> items;
        }
        public static RanchConnectedDecorV02BoardSnapshot Parse(string json)
        {
            ValidateShape(json);
            if (json.Length > 0 && json[0] == '\ufeff') json = json.Substring(1);
            var snapshot = JsonUtility.FromJson<RanchConnectedDecorV02BoardSnapshot>(json);
            string error = null;
            if (snapshot == null || !snapshot.TryValidate(out error))
                throw new InvalidDataException(snapshot == null ? "Missing board document." : error);
            return snapshot;
        }
        public static void ValidateShape(string json)
        {
            if (json == null || json.Length > 16 * 1024 * 1024) throw new InvalidDataException("Missing or oversized board JSON.");
            Node root = new Parser(json).Parse();
            Fields(root, "schemaVersion", 'n', "packageId", 's', "sourceManifestSha256", 's', "boardId", 's',
                "revision", 'n', "isWholeBoard", 'b', "isExampleOnly", 'b', "originX", 'n', "originY", 'n', "originZ", 'n',
                "cellSize", 'n', "seed", 'n', "postSpacing", 'n', "networkCells", 'a', "groupPlacements", 'a', "allowedDecorCells", 'a');
            Int32(root, "schemaVersion", "seed", "postSpacing"); Int64(root, "revision");
            Float(root, "originX", "originY", "originZ", "cellSize");
            foreach (Node row in root.fields["networkCells"].items)
            {
                Fields(row, "x", 'n', "y", 'n', "logicalLayer", 'n', "heightKey", 'n', "sortingOrder", 'n',
                    "worldZ", 'n', "family", 's', "connectionGroup", 's');
                Int32(row, "x", "y", "logicalLayer", "heightKey", "sortingOrder"); Float(row, "worldZ");
            }
            foreach (Node row in root.fields["groupPlacements"].items)
            {
                Fields(row, "placementId", 's', "groupKey", 's', "anchorX", 'n', "anchorY", 'n', "logicalLayer", 'n',
                    "heightKey", 'n', "sortingOrder", 'n', "worldZ", 'n');
                Int32(row, "anchorX", "anchorY", "logicalLayer", "heightKey", "sortingOrder"); Float(row, "worldZ");
            }
            foreach (Node row in root.fields["allowedDecorCells"].items)
            {
                Fields(row, "x", 'n', "y", 'n', "logicalLayer", 'n', "heightKey", 'n');
                Int32(row, "x", "y", "logicalLayer", "heightKey");
            }
        }
        private static void Fields(Node node, params object[] expected)
        {
            Require(node != null && node.kind == 'o', "Expected board JSON object.");
            Require(node.fields.Count == expected.Length / 2, "Unknown or missing board JSON field.");
            for (int i = 0; i < expected.Length; i += 2)
            {
                string name = (string)expected[i]; Node value;
                Require(node.fields.TryGetValue(name, out value) && value.kind == (char)expected[i + 1], "Missing/wrong JSON field: " + name);
            }
        }
        private static void Int32(Node row, params string[] names)
        {
            foreach (string name in names)
            {
                int value;
                Require(int.TryParse(row.fields[name].scalar, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out value), "Expected exact Int32: " + name);
            }
        }
        private static void Int64(Node row, params string[] names)
        {
            foreach (string name in names)
            {
                long value;
                Require(long.TryParse(row.fields[name].scalar, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out value), "Expected exact Int64: " + name);
            }
        }
        private static void Float(Node row, params string[] names)
        {
            foreach (string name in names)
            {
                double value;
                Require(double.TryParse(row.fields[name].scalar, NumberStyles.Float, CultureInfo.InvariantCulture, out value)
                    && !double.IsNaN(value) && !double.IsInfinity(value) && value >= -float.MaxValue && value <= float.MaxValue,
                    "Non-finite or overflowing float: " + name);
            }
        }
        private static void Require(bool condition, string error) { if (!condition) throw new InvalidDataException(error); }
        private sealed class Parser
        {
            private readonly string text;
            private int cursor;
            private static readonly Regex Number = new Regex(@"^-?(0|[1-9][0-9]*)(\.[0-9]+)?([eE][+-]?[0-9]+)?$", RegexOptions.CultureInvariant);
            public Parser(string json) { text = json; }
            public Node Parse()
            {
                if (text.Length > 0 && text[0] == '\ufeff') cursor++;
                Node node = Value(0); White(); Require(cursor == text.Length, "Trailing board JSON data."); return node;
            }
            private Node Value(int depth)
            {
                Require(depth <= 32, "Board JSON nesting too deep."); White(); Require(cursor < text.Length, "Unexpected board JSON end.");
                char c = text[cursor];
                if (c == '{')
                {
                    cursor++; var node = new Node { kind = 'o', fields = new Dictionary<string, Node>(StringComparer.Ordinal) };
                    White(); if (Take('}')) return node;
                    while (true)
                    {
                        White(); string key = Quoted(); White(); Require(Take(':'), "Expected board JSON colon.");
                        Require(!node.fields.ContainsKey(key), "Duplicate board JSON key: " + key);
                        node.fields.Add(key, Value(depth + 1)); White();
                        if (Take('}')) return node; Require(Take(','), "Expected board JSON object comma.");
                    }
                }
                if (c == '[')
                {
                    cursor++; var node = new Node { kind = 'a', items = new List<Node>() };
                    White(); if (Take(']')) return node;
                    while (true)
                    {
                        node.items.Add(Value(depth + 1)); White(); if (Take(']')) return node;
                        Require(Take(','), "Expected board JSON array comma.");
                    }
                }
                if (c == '"') return new Node { kind = 's', scalar = Quoted() };
                int start = cursor;
                while (cursor < text.Length && ",]} \t\r\n".IndexOf(text[cursor]) < 0) cursor++;
                string token = text.Substring(start, cursor - start);
                if (token == "true" || token == "false") return new Node { kind = 'b', scalar = token };
                if (token == "null") return new Node { kind = '0' };
                Require(Number.IsMatch(token), "Invalid board JSON scalar."); return new Node { kind = 'n', scalar = token };
            }
            private string Quoted()
            {
                Require(Take('"'), "Expected board JSON string."); var textValue = new StringBuilder();
                while (cursor < text.Length)
                {
                    char c = text[cursor++]; if (c == '"') return textValue.ToString();
                    Require(c >= 32, "Control character in board JSON string.");
                    if (c != '\\') { textValue.Append(c); continue; }
                    Require(cursor < text.Length, "Incomplete board JSON escape."); char escaped = text[cursor++];
                    if (escaped == '"' || escaped == '\\' || escaped == '/') textValue.Append(escaped);
                    else if (escaped == 'b') textValue.Append('\b'); else if (escaped == 'f') textValue.Append('\f');
                    else if (escaped == 'n') textValue.Append('\n'); else if (escaped == 'r') textValue.Append('\r'); else if (escaped == 't') textValue.Append('\t');
                    else if (escaped == 'u')
                    {
                        Require(cursor + 4 <= text.Length, "Incomplete board Unicode escape."); int code;
                        Require(int.TryParse(text.Substring(cursor, 4), NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out code), "Invalid board Unicode escape.");
                        textValue.Append((char)code); cursor += 4;
                    }
                    else Require(false, "Invalid board JSON escape.");
                }
                throw new InvalidDataException("Unterminated board JSON string.");
            }
            private bool Take(char expected) { if (cursor < text.Length && text[cursor] == expected) { cursor++; return true; } return false; }
            private void White() { while (cursor < text.Length && " \t\r\n".IndexOf(text[cursor]) >= 0) cursor++; }
        }
    }
}
#endif

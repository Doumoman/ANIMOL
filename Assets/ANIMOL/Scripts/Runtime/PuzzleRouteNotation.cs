using System;
using System.Text;
using UnityEngine;

namespace ANIMOL.Puzzle
{
    public static class PuzzleRouteNotation
    {
        public const char WindWait = '○';
        public static bool IsWait(char action) => action == '.' || action == WindWait;
        public static bool TryDirection(char action, out Vector2Int direction)
        {
            switch (char.ToUpperInvariant(action))
            {
                case 'N': case '↑': direction = Vector2Int.up; return true;
                case 'E': case '→': direction = Vector2Int.right; return true;
                case 'S': case '↓': direction = Vector2Int.down; return true;
                case 'W': case '←': direction = Vector2Int.left; return true;
                default: direction = Vector2Int.zero; return false;
            }
        }
        public static string ToArrows(string route)
        {
            if (route == null) throw new ArgumentNullException(nameof(route));
            var text = new StringBuilder(route.Length);
            foreach (char action in route)
            {
                if (char.IsWhiteSpace(action)) continue;
                if (IsWait(action)) { text.Append(WindWait); continue; }
                if (!TryDirection(action, out var direction)) throw new FormatException("Unknown route action: " + action);
                text.Append(direction == Vector2Int.up ? '↑' : direction == Vector2Int.right ? '→' : direction == Vector2Int.down ? '↓' : '←');
            }
            return text.ToString();
        }
        public static string Display(string route) => string.Join(" ", ToArrows(route).ToCharArray());
        public static int InputCount(string route)
        {
            int count = 0;
            foreach (char action in ToArrows(route)) if (!IsWait(action)) count++;
            return count;
        }
    }
}

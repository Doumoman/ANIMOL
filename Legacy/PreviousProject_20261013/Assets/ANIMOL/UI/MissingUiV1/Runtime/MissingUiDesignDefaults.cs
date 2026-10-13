using System.Collections.Generic;

namespace ANIMOL.MissingUiV1
{
    public static class MissingUiDesignDefaults
    {
        // Matches the HTML layout fixture only. Never copy these into production preferences.
        public static Dictionary<string, string> Create() { return new Dictionary<string, string> {
            { "bgm", "0.70" }, { "sfx", "0.80" }, { "vibration", "true" }, { "hand", "right" },
            { "size", "0.70" }, { "position", "0.50" }, { "opacity", "0.80" }, { "down", "false" },
            { "large-text", "false" }, { "language", "ko" }, { "code", "" }
        }; }
    }
}

#if ANIMOL_NET_UI_DEV
using System;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Animol.NetUiDev.Project
{
    [Serializable] public sealed class NetUiProjectMode
    {
        public string ModeId;
        // Explicit project policy, not inferred from a label or array position.
        public string GrowthPolicy;
        public string PublicIntent;
        public string CreateIntent;
        public string JoinIntent;
    }

    [Serializable] public sealed class NetUiProjectSettings
    {
        public string ServerUrl;
        public string AccountId;
        public string DevAccessKey;
        public string NormalModeId;
        public string RankedModeId;
        public string CustomModeId;
        public NetUiProjectMode[] Modes = Array.Empty<NetUiProjectMode>();
        public static string LocalPath => Path.Combine(Application.persistentDataPath, "ANIMOLNet02", "connection.json");

        public static NetUiProjectSettings ReadLocal()
        {
            NetUiHttpClient.EnsureDevelopment();
            if (!File.Exists(LocalPath)) return null;
            var settings = JsonUtility.FromJson<NetUiProjectSettings>(File.ReadAllText(LocalPath));
            NetUiValidation.Require(settings != null && !string.IsNullOrWhiteSpace(settings.AccountId) &&
                !string.IsNullOrWhiteSpace(settings.DevAccessKey), "DEV_CONNECTION_UNCONFIGURED");
            settings.ServerUrl = NetUiValidation.NormalizeServerUrl(settings.ServerUrl, Application.isMobilePlatform);
            return settings;
        }

        public NetUiProjectMode Mode(string id)
        {
            NetUiValidation.Require(!string.IsNullOrWhiteSpace(id) && Modes != null, "PROJECT_MODE_UNCONFIGURED");
            var matches = Modes.Where(m => m != null && m.ModeId == id).ToArray();
            NetUiValidation.Require(matches.Length == 1, "PROJECT_MODE_AMBIGUOUS_OR_MISSING");
            NetUiValidation.Require(matches[0].GrowthPolicy == "OwnedProgress" || matches[0].GrowthPolicy == "RankedMaximumPreset", "GROWTH_POLICY_UNCONFIGURED");
            NetUiValidation.Require(string.IsNullOrEmpty(NormalModeId) || NormalModeId != RankedModeId, "NORMAL_RANKED_MODE_COLLISION");
            if (id == NormalModeId) NetUiValidation.Require(matches[0].GrowthPolicy == "OwnedProgress", "NORMAL_GROWTH_POLICY_MISMATCH");
            if (id == RankedModeId) NetUiValidation.Require(matches[0].GrowthPolicy == "RankedMaximumPreset", "RANKED_GROWTH_POLICY_MISMATCH");
            return matches[0];
        }
    }
}
#endif

using System.Collections.Generic;
using UnityEngine;

namespace ANIMOL.StarwayMaps.Editor
{
    public sealed class StarwayNativeLink
    {
        public string id;
        public Object map;
        public Object scenario;
        public Object campaignStage; // May be null only if the actual project has no stage wrapper.
    }

    // Implement a project-specific subclass OUTSIDE this package's asmdef if the
    // native game types live in Assembly-CSharp. Do not use reflection to guess fields.
    public abstract class StarwayNativeAdapter
    {
        public abstract string DisplayName { get; }
        public abstract string[] OwnedAssetRoots { get; }
        // READ ONLY: all types, themes, visuals, enum/family tokens, size limits and paths.
        public abstract void Preflight(StarwayMapSource[] sources, List<string> errors);
        // Upsert only owned map/scenario/stage assets; keep existing GUIDs. No campaign edit yet.
        public abstract void BuildOrUpdateMaps(StarwayMapSource[] sources);
        // READ ONLY: must return exactly 20 links in SW01..SW20 order.
        public abstract StarwayNativeLink[] ReadNativeLinks(StarwayMapSource[] sources);
        // READ ONLY: compare EVERY gameplay field by reading the actual native asset,
        // including scenario/theme/visual references, bounds and nonrectangular voids.
        public abstract void ValidateNativeMap(StarwayMapSource source, StarwayNativeLink native, List<string> errors);
        // Called only after all 20 native map readbacks pass. Preserve unrelated stages/order/save IDs.
        public abstract void RegisterCampaign(StarwayNativeLink[] links);
        // READ ONLY: verify one ordered Starway20 group, no duplicates, unrelated entries preserved.
        public abstract void ValidateCampaign(StarwayNativeLink[] links, List<string> errors);
    }
}

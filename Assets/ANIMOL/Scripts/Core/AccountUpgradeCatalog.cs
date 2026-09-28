using System;
using System.Collections.Generic;
using UnityEngine;

namespace ANIMOL.Core
{
    [CreateAssetMenu(menuName = "ANIMOL/Account/Upgrade Catalog", fileName = "AccountUpgradeCatalog")]
    public sealed class AccountUpgradeCatalog : ScriptableObject
    {
        [SerializeField] private int version = 1;
        [SerializeField] private AccountUpgradeTrackDefinition[] tracks = Array.Empty<AccountUpgradeTrackDefinition>();
        public int Version => version;
        public IReadOnlyList<AccountUpgradeTrackDefinition> Tracks => tracks;
    }
}

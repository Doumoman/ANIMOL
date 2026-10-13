using System;
using System.Collections.Generic;
using UnityEngine;

namespace ANIMOL.Core
{
    [CreateAssetMenu(menuName = "ANIMOL/Account/Upgrade Catalog", fileName = "AccountUpgradeCatalog")]
    public sealed class AccountUpgradeCatalog : ScriptableObject
    {
        [SerializeField] private int version = 1;
        [SerializeField] private bool prototypeValues = true;
        [SerializeField] private string prototypeNotice = "플레이 검증 전 튜닝 초안";
        [SerializeField] private AccountUpgradeTrackDefinition[] tracks = Array.Empty<AccountUpgradeTrackDefinition>();
        public int Version => version;
        public bool PrototypeValues => prototypeValues;
        public string PrototypeNotice => prototypeNotice;
        public IReadOnlyList<AccountUpgradeTrackDefinition> Tracks => tracks;
    }
}

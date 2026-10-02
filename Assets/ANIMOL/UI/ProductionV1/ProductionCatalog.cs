using System;
using System.Linq;
using ANIMOL.Core;
using TMPro;
using UnityEngine;

namespace ANIMOL.ProductionV1
{
    public sealed class ProductionCatalog : ScriptableObject
    {
        public Sprite[] Art;
        public Sprite[] LoadingFrames;
        public TMP_FontAsset Font;
        public Sprite ApprovedCoin;
        public CampaignCatalog Campaign;
        public AccountUpgradeCatalog Account;
        public GrowthEconomyPolicyCatalog Growth;
        public Sprite Find(string id) => Art.FirstOrDefault(s => s != null && s.name == id);
    }

    // UI state only. Never stores or mutates gameplay/economy values.
    public enum ProductionState { Confirm, Processing, Success, Insufficient, Max, ConnectionFailure, Unconfigured, UnknownResult, LoadingFailure }

    public sealed class PurchasePresentation
    {
        public bool Pending { get; private set; }
        public bool ResultUnknown { get; private set; }
        public string TransactionId { get; private set; }
        public bool Begin()
        {
            if (Pending || ResultUnknown) return false;
            Pending = true;
            TransactionId = Guid.NewGuid().ToString("N");
            return true;
        }
        public void Complete(AccountRequestStatus status)
        {
            Pending = false;
            ResultUnknown = status == AccountRequestStatus.Unavailable || status == AccountRequestStatus.Duplicate;
        }
        public void MarkUnknown() { Pending = false; ResultUnknown = true; }
    }
}

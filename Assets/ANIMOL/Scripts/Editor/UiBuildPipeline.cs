using System;
using System.IO;
using System.Linq;
using ANIMOL.Core;
using UnityEditor;
using UnityEngine;

namespace ANIMOL.Editor
{
    public static partial class UiBuildPipeline
    {
        [MenuItem("ANIMOL/Build/M0 Core Data")]
        public static void BuildM0()
        {
            try
            {
                var catalog = CampaignCatalogGenerator.BuildOrUpdate();
                ValidateM0(catalog, "animol-m0-validation.txt");
                Debug.Log("[ANIMOL][M0] Build and validation completed.");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                if (Application.isBatchMode) EditorApplication.Exit(1);
                throw;
            }
        }

        public static void BuildOrUpdate()
        {
            BuildM0();
            BuildM1();
            BuildM2();
            BuildM3();
            BuildM4();
            BuildM5();
        }

        public static void ValidateAll()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<CampaignCatalog>(CampaignCatalogGenerator.CatalogPath);
            ValidateM0(catalog, "animol-ui-validate.log");
            ValidateM1();
            ValidateM2();
            ValidateM3();
            ValidateM4();
            ValidateM5();
        }

        private static void ValidateM0(CampaignCatalog catalog, string logFileName)
        {
            var report = CampaignCatalogValidator.Validate(catalog);
            var readyCount = catalog == null ? 0 : catalog.EnumerateStages().Count(stage => ContentAvailabilityResolver.Resolve(stage) == ContentAvailability.Ready);
            var unassignedCount = catalog == null ? 0 : catalog.EnumerateStages().Count(stage => ContentAvailabilityResolver.Resolve(stage) == ContentAvailability.Unassigned);
            var lines = new[]
            {
                "ANIMOL M0 validation",
                $"valid={report.IsValid}",
                $"themes={report.ThemeCount}",
                $"stages={report.StageCount}",
                $"duplicateStageIds={report.DuplicateStageIdCount}",
                $"readyStages={readyCount}",
                $"unassignedStages={unassignedCount}",
                $"errors={string.Join(" | ", report.Errors)}"
            };
            Directory.CreateDirectory(Path.Combine(Directory.GetCurrentDirectory(), "Logs"));
            File.WriteAllLines(Path.Combine(Directory.GetCurrentDirectory(), "Logs", logFileName), lines);
            if (!report.IsValid) throw new InvalidOperationException(string.Join("\n", report.Errors));
            if (readyCount != 0 || unassignedCount != 100)
                throw new InvalidOperationException($"Fresh campaign slots must be Unassigned (ready={readyCount}, unassigned={unassignedCount}).");
        }

        static partial void BuildM1();
        static partial void BuildM2();
        static partial void BuildM3();
        static partial void BuildM4();
        static partial void BuildM5();
        static partial void ValidateM1();
        static partial void ValidateM2();
        static partial void ValidateM3();
        static partial void ValidateM4();
        static partial void ValidateM5();
    }
}

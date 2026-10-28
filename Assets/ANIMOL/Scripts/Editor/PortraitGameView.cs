using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace ANIMOL.Presentation.Editor
{
    /// <summary>Keeps the Editor Game View on the same portrait contract as mobile players.</summary>
    public static class PortraitGameView
    {
        private const BindingFlags AnyInstance = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        private const BindingFlags AnyStatic = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

        public static void Use1080x1920() => SelectFixedResolution(1080, 1920, "ANIMOL Portrait 1080x1920");

        public static void Use1080x2400() => SelectFixedResolution(1080, 2400, "ANIMOL Tall 1080x2400");

        public static void SelectFixedResolution(int width, int height, string label)
        {
            var editorAssembly = typeof(EditorWindow).Assembly;
            var sizesType = editorAssembly.GetType("UnityEditor.GameViewSizes", true);
            var sizeType = editorAssembly.GetType("UnityEditor.GameViewSize", true);
            var sizeKindType = editorAssembly.GetType("UnityEditor.GameViewSizeType", true);
            var gameViewType = editorAssembly.GetType("UnityEditor.GameView", true);

            var singletonType = typeof(ScriptableSingleton<>).MakeGenericType(sizesType);
            var instance = singletonType.GetProperty("instance", AnyStatic)?.GetValue(null);
            if (instance == null) throw new InvalidOperationException("Unity GameViewSizes singleton is unavailable.");

            var currentGroupProperty = sizesType.GetProperty("currentGroupType", AnyInstance);
            var currentGroup = currentGroupProperty?.GetValue(instance) ?? GameViewSizeGroupType.Android;
            var getGroup = sizesType.GetMethod("GetGroup", AnyInstance);
            var group = getGroup?.Invoke(instance, new[] { currentGroup });
            if (group == null) throw new InvalidOperationException("Unity Game View size group is unavailable.");

            var groupType = group.GetType();
            var getTotalCount = groupType.GetMethod("GetTotalCount", AnyInstance);
            var getGameViewSize = groupType.GetMethod("GetGameViewSize", AnyInstance);
            var addCustomSize = groupType.GetMethod("AddCustomSize", AnyInstance);
            if (getTotalCount == null || getGameViewSize == null || addCustomSize == null)
                throw new MissingMethodException("Unity Game View size API changed.");

            var index = FindResolution(group, getTotalCount, getGameViewSize, width, height);
            if (index < 0)
            {
                var fixedResolution = Enum.Parse(sizeKindType, "FixedResolution");
                var constructor = sizeType.GetConstructor(AnyInstance, null,
                    new[] { sizeKindType, typeof(int), typeof(int), typeof(string) }, null);
                if (constructor == null) throw new MissingMethodException("Unity GameViewSize constructor changed.");
                var size = constructor.Invoke(new[] { fixedResolution, (object)width, height, label });
                addCustomSize.Invoke(group, new[] { size });
                index = FindResolution(group, getTotalCount, getGameViewSize, width, height);
            }

            if (index < 0) throw new InvalidOperationException($"Could not add {width}x{height} to the Game View.");
            var gameView = EditorWindow.GetWindow(gameViewType);
            var selectedSize = gameViewType.GetProperty("selectedSizeIndex", AnyInstance);
            if (selectedSize == null) throw new MissingMemberException("Unity GameView.selectedSizeIndex changed.");
            selectedSize.SetValue(gameView, index);
            gameView.Show();
            gameView.Focus();
            gameView.Repaint();
            Debug.Log($"[ANIMOL][Portrait] Game View selected: {width}x{height} ({label}).");
        }

        private static int FindResolution(object group, MethodInfo getTotalCount, MethodInfo getGameViewSize, int width, int height)
        {
            var count = (int)getTotalCount.Invoke(group, null);
            for (var i = 0; i < count; i++)
            {
                var size = getGameViewSize.Invoke(group, new object[] { i });
                if (size == null) continue;
                var type = size.GetType();
                var itemWidth = Convert.ToInt32(type.GetProperty("width", AnyInstance)?.GetValue(size));
                var itemHeight = Convert.ToInt32(type.GetProperty("height", AnyInstance)?.GetValue(size));
                if (itemWidth == width && itemHeight == height) return i;
            }
            return -1;
        }
    }
}

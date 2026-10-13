using System;
using System.Reflection;
using UnityEditor;

namespace ANIMOL.Editor
{
    // Older, locally installed build scripts may still register their menus. Keep the
    // production surface small without rewriting those user-owned migration scripts.
    [InitializeOnLoad]
    public static class CampaignEditorMenus
    {
        static CampaignEditorMenus() { EditorApplication.delayCall += RemoveRetiredMenus; }
        public static bool IsCurrent(string path) => path == "ANIMOL/Campaign Maps %#m" ||
            path.StartsWith("ANIMOL/Main UI V7/", StringComparison.Ordinal) ||
            path.StartsWith("ANIMOL/Portrait Preview/", StringComparison.Ordinal);

        public static void RemoveRetiredMenus()
        {
            var remove = typeof(Menu).GetMethod("RemoveMenuItem", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static,
                null, new[] { typeof(string) }, null);
            if (remove == null) return;
            foreach (var method in TypeCache.GetMethodsWithAttribute<MenuItem>())
                foreach (MenuItem item in method.GetCustomAttributes(typeof(MenuItem), false))
                {
                    if (!item.menuItem.StartsWith("ANIMOL/", StringComparison.Ordinal) || IsCurrent(item.menuItem)) continue;
                    // Unity's menu lookup excludes the shortcut suffix.
                    string path = item.menuItem;
                    int shortcut = path.LastIndexOf(' ');
                    if (shortcut >= 0 && shortcut + 1 < path.Length && "%#&_".IndexOf(path[shortcut + 1]) >= 0)
                        path = path.Substring(0, shortcut);
                    remove.Invoke(null, new object[] { path });
                }
        }
    }
}

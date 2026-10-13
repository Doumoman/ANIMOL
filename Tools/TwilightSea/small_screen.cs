using UnityEngine;
using ANIMOL.Presentation.Editor;
public static class SeaSmallScreen
{
    public static void Main()=>PortraitGameView.SelectFixedResolution(720,1280,"ANIMOL Sea QA 720x1280");
    public static object Inspect()=>new{width=Screen.width,height=Screen.height,safeArea=Screen.safeArea};
}

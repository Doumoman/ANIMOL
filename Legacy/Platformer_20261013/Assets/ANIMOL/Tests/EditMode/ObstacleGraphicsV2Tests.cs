#if UNITY_EDITOR && UNITY_INCLUDE_TESTS
using NUnit.Framework;
using ANIMOL.Gameplay.ObstacleGraphics;

// Put in the actual project's existing EditMode test assembly after adding the runtime reference.
public sealed class ObstacleGraphicsV2Tests
{
    static GraphicCell Cell(string kind = "terrain", bool? surface = null) => new GraphicCell {
        Kind=kind, ThemeId="T01", StyleId="T01_A", ArtVersion=6, SurfaceEnabled=surface
    };

    [Test] public void AdjacentFullDeviceSharesBothBodies()
    {
        var free=Cell(); var belt=Cell("C08");belt.Facing="RIGHT";
        GraphicCell Read(int x,int y) => y==0 ? (x==0?free:x==1?belt:null) : null;
        Assert.AreEqual(4,ObstacleVisualResolver.Resolve(Read,0,0,0).BodyMask);
        Assert.AreEqual(64,ObstacleVisualResolver.Resolve(Read,1,0,0).BodyMask);
    }

    [Test] public void TopOnlyJoinsCapWithoutFillingBottom()
    {
        var free=Cell();var drop=Cell("C01",true);
        GraphicCell Read(int x,int y) => y==0 ? (x==0?free:x==1?drop:null) : null;
        var a=ObstacleVisualResolver.Resolve(Read,0,0,0);var b=ObstacleVisualResolver.Resolve(Read,1,0,0);
        Assert.AreEqual(0,a.BodyMask);Assert.AreEqual(4,a.CapMask);Assert.IsTrue(a.CapPatch);
        Assert.IsFalse(b.DrawBody);Assert.IsTrue(b.CapOnly);
    }

    [Test] public void InactiveTopRestoresTheEndcap()
    {
        var free=Cell();var fade=Cell("C05",false);fade.Pose="inactive";
        GraphicCell Read(int x,int y) => y==0 ? (x==0?free:x==1?fade:null) : null;
        Assert.AreEqual(0,ObstacleVisualResolver.Resolve(Read,0,0,0).CapMask);
        Assert.IsFalse(ObstacleVisualResolver.Resolve(Read,1,0,0).CapOnly);
    }

    [Test] public void DiagonalContactAloneDoesNotJoin()
    {
        Assert.AreEqual(0,ObstacleVisualResolver.Canonicalize(2|8|32|128));
        Assert.AreEqual(7,ObstacleVisualResolver.Canonicalize(1|2|4));
    }

    [Test] public void NegativeCoordinatesUseV6Phase()
    {
        Assert.AreEqual(1,ObstacleVisualResolver.Variant(-1,0,0));
        Assert.AreEqual(3,ObstacleVisualResolver.Variant(-1,-1,0));
        Assert.AreEqual(36,ObstacleVisualResolver.Affected(-1,16).MotifAnchors.Count);
    }
    [Test] public void C09IsRetiredAndC04IsAlwaysActive()
    {
        Assert.Throws<System.ArgumentException>(() => ObstacleVisualResolver.Resolve((x,y) => Cell("C09",true),0,0,0));
        var hazard=Cell("C04");hazard.Pose="inactive";
        Assert.AreEqual("active",ObstacleVisualResolver.Resolve((x,y)=>hazard,0,0,0).Pose);
    }
    [Test] public void NewSurfaceReplacesCapAndOneShotAnimationClamps()
    {
        Assert.IsTrue(ObstacleVisualResolver.Resolve((x,y)=>Cell("C05",true),0,0,0).ReplaceCap);
        Assert.AreEqual(6,ObstacleVisualResolver.AnimationFrame("C06","active",1000));
        Assert.AreEqual(0,ObstacleVisualResolver.AnimationFrame("C08","idle",640));
    }
}
#endif

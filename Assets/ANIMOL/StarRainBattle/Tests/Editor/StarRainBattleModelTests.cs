using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace ANIMOL.StarRainBattle.Tests
{
    public sealed class StarRainBattleModelTests
    {
        private static StarRainBattleModel QuietBattle(int seed = 1234)
        {
            var model = new StarRainBattleModel();
            model.Begin(seed);
            model.TestSetSpawnTimes(float.PositiveInfinity, float.PositiveInfinity);
            model.TestSetNode(0, new Vector2(80, 310));
            model.TestSetNode(1, new Vector2(320, 310));
            model.TestSetNode(2, new Vector2(60, 170));
            model.TestSetNode(3, new Vector2(340, 170));
            model.TestSetNode(4, new Vector2(60, 450));
            return model;
        }

        private static void CrossingShot(StarRainBattleModel model, MeteorTeam team)
        {
            // One swept head crosses the middle of the horizontal link.
            bool upward = team == MeteorTeam.Isol;
            float startY = upward ? 360f : 260f;
            float endY = upward ? 260f : 360f;
            model.TestAddMeteor(team, new Vector2(200f, startY),
                new Vector2(200f, 310f), new Vector2(200f, endY), 0.1f);
        }

        private static void Advance(StarRainBattleModel model, float seconds)
        {
            const float step = 1f / 60f;
            float elapsed = 0f;
            while (elapsed + 0.000001f < seconds)
            {
                float dt = Mathf.Min(step, seconds - elapsed);
                model.Tick(dt);
                elapsed += dt;
            }
        }

        [Test]
        public void LinkExpiresAfterHalfASecondAndNewLinkReplacesOld()
        {
            var model = QuietBattle();
            Assert.IsTrue(model.TryConnect(0, 1));
            Assert.IsTrue(model.HasLink);
            Assert.That(model.LinkExpires, Is.EqualTo(0.5f).Within(0.00001f));

            Advance(model, 0.3f);
            Assert.IsTrue(model.TryConnect(2, 3));
            Assert.That(model.LinkA, Is.EqualTo(2));
            Assert.That(model.LinkB, Is.EqualTo(3));
            Assert.That(model.LinkExpires, Is.EqualTo(model.Time + 0.5f).Within(0.00001f));
            Advance(model, 0.49f);
            Assert.IsTrue(model.HasLink);
            Advance(model, 0.05f);
            Assert.IsFalse(model.HasLink);
        }

        [TestCase(.316f), TestCase(.137f), TestCase(.3f)]
        public void LinkKeepsFullLifetimeWhenInputLandsBetweenFixedSteps(float inputTime)
        {
            var model = QuietBattle();
            Advance(model, inputTime);
            Assert.That(model.Time, Is.EqualTo(inputTime).Within(.000001f));
            Assert.That(model.TryConnect(0, 1), Is.True);
            Advance(model, .49f);
            Assert.That(model.HasLink, Is.True);
            Advance(model, .02f);
            Assert.That(model.HasLink, Is.False);
        }

        [Test]
        public void NormalLinkAbsorbsCurseAndIsolHeadsSeparately()
        {
            var curse = QuietBattle();
            Assert.IsTrue(curse.TryConnect(0, 1));
            CrossingShot(curse, MeteorTeam.Curse);
            curse.Tick(0.2f);
            Assert.That(curse.BlockedEnemy, Is.EqualTo(1));
            Assert.That(curse.BlockedFriend, Is.Zero);
            Assert.That(curse.IsolHP, Is.EqualTo(StarRainBattleModel.IsolMaxHP));

            var isol = QuietBattle();
            Assert.IsTrue(isol.TryConnect(0, 1));
            CrossingShot(isol, MeteorTeam.Isol);
            isol.Tick(0.2f);
            Assert.That(isol.BlockedFriend, Is.EqualTo(1));
            Assert.That(isol.BlockedEnemy, Is.Zero);
            Assert.That(isol.CurseHP, Is.EqualTo(StarRainBattleModel.CurseMaxHP));
        }

        [Test]
        public void PrismPassesFriendlyHeadAndReflectsCurseToItsCore()
        {
            var isol = QuietBattle();
            isol.SetAugment(true);
            Assert.IsTrue(isol.TryConnect(0, 1));
            CrossingShot(isol, MeteorTeam.Isol);
            isol.Tick(0.2f);
            Assert.That(isol.BlockedFriend, Is.Zero);
            Assert.That(isol.BlockedEnemy, Is.Zero);

            var curse = QuietBattle();
            var events = new List<BattleEventType>();
            curse.EventRaised += e => events.Add(e.Type);
            curse.SetAugment(true);
            Assert.IsTrue(curse.TryConnect(0, 1));
            CrossingShot(curse, MeteorTeam.Curse);
            curse.Tick(0.2f);
            Assert.That(events, Does.Contain(BattleEventType.Reflect));
            Assert.That(curse.BlockedEnemy, Is.Zero); // Reflection is not an absorption.
            Assert.That(curse.Meteors.Count, Is.EqualTo(1));
            Assert.That(curse.Meteors[0].Team, Is.EqualTo(MeteorTeam.Reflected));
            Advance(curse, 1.3f);
            Assert.That(curse.CurseHP, Is.EqualTo(StarRainBattleModel.CurseMaxHP - 1));
            Assert.That(curse.IsolHP, Is.EqualTo(StarRainBattleModel.IsolMaxHP));
        }

        [Test]
        public void SweptHeadProducesSameOutcomeAtThirtyAndSixtyFps()
        {
            var thirty = QuietBattle();
            var sixty = QuietBattle();
            Assert.IsTrue(thirty.TryConnect(0, 1));
            Assert.IsTrue(sixty.TryConnect(0, 1));
            CrossingShot(thirty, MeteorTeam.Curse);
            CrossingShot(sixty, MeteorTeam.Curse);
            for (int i = 0; i < 6; i++) thirty.Tick(1f / 30f);
            for (int i = 0; i < 12; i++) sixty.Tick(1f / 60f);
            Assert.That(thirty.BlockedEnemy, Is.EqualTo(1));
            Assert.That(sixty.BlockedEnemy, Is.EqualTo(thirty.BlockedEnemy));
            Assert.That(sixty.IsolHP, Is.EqualTo(thirty.IsolHP));
        }

        [Test]
        public void LinkWinsAnExactTieWithItsEndpointNode()
        {
            var model = QuietBattle();
            model.TestSetNode(0, new Vector2(200f, 310f));
            Assert.IsTrue(model.TryConnect(0, 1));
            CrossingShot(model, MeteorTeam.Curse);
            model.Tick(0.2f);
            Assert.That(model.BlockedEnemy, Is.EqualTo(1));
            Assert.That(model.Nodes[0].DimUntil, Is.Zero);
        }

        [Test]
        public void UnblockedCurseHeadsDepleteIsolCoreAndEndTheBattle()
        {
            var model = QuietBattle();
            for (int i = 0; i < StarRainBattleModel.IsolMaxHP; i++)
                model.TestAddMeteor(MeteorTeam.Curse, new Vector2(200f, 500f),
                    new Vector2(200f, 540f), new Vector2(200f, 580f), 0.1f);
            model.Tick(0.2f);
            Assert.That(model.IsolHP, Is.Zero);
            Assert.That(model.Phase, Is.EqualTo(BattlePhase.Lost));
        }

        [Test]
        public void CurseHeadDimsNodeAndNodeCannotStartAnotherLinkUntilRecovery()
        {
            var model = QuietBattle();
            model.TestAddMeteor(MeteorTeam.Curse, new Vector2(80f, 260f),
                new Vector2(80f, 310f), new Vector2(80f, 360f), 0.1f);
            model.Tick(0.2f);
            Assert.That(model.Nodes[0].DimUntil, Is.GreaterThan(model.Time));
            Assert.IsFalse(model.TryConnect(0, 1));
            Advance(model, 0.7f);
            Assert.IsTrue(model.TryConnect(0, 1));
        }

        [Test]
        public void PauseFreezesTimeNodeMotionAndLinkLifetime()
        {
            var model = new StarRainBattleModel();
            model.Begin(1234);
            model.TestSetSpawnTimes(float.PositiveInfinity, float.PositiveInfinity);
            Assert.IsTrue(model.TryConnect(0, 1));
            model.TestAddMeteor(MeteorTeam.Curse, new Vector2(350f, 500f),
                new Vector2(350f, 310f), new Vector2(350f, 100f), 2f);
            model.Tick(0.1f);
            float time = model.Time;
            float expires = model.LinkExpires;
            Vector2 node = model.Nodes[0].Position;
            Vector2 meteor = model.Meteors[0].Position;
            model.Pause();
            model.Tick(2f);
            Assert.That(model.Phase, Is.EqualTo(BattlePhase.Paused));
            Assert.That(model.Time, Is.EqualTo(time));
            Assert.That(model.LinkExpires, Is.EqualTo(expires));
            Assert.That(model.Nodes[0].Position, Is.EqualTo(node));
            Assert.That(model.Meteors[0].Position, Is.EqualTo(meteor));
            Assert.IsTrue(model.HasLink);
            model.Resume();
            model.Tick(0.1f);
            Assert.That(model.Time, Is.GreaterThan(time));
            Assert.That(model.Nodes[0].Position, Is.Not.EqualTo(node));
            Assert.That(model.Meteors[0].Position, Is.Not.EqualTo(meteor));
        }

        [Test]
        public void SameSeedReplaysInitialNodesAndMeteorPaths()
        {
            var model = new StarRainBattleModel();
            model.Begin(2771);
            var initial = new Vector2[5];
            for (int i = 0; i < initial.Length; i++) initial[i] = model.Nodes[i].Position;
            Advance(model, 1.3f);
            Assert.That(model.Meteors.Count, Is.GreaterThanOrEqualTo(2));
            var first = model.Meteors[0];
            Vector2 start = first.Start, control = first.Control, end = first.End;

            model.RestartSameSeed();
            for (int i = 0; i < initial.Length; i++)
                Assert.That(model.Nodes[i].Position, Is.EqualTo(initial[i]));
            Advance(model, 1.3f);
            Assert.That(model.Meteors.Count, Is.GreaterThanOrEqualTo(2));
            Assert.That(model.Meteors[0].Start, Is.EqualTo(start));
            Assert.That(model.Meteors[0].Control, Is.EqualTo(control));
            Assert.That(model.Meteors[0].End, Is.EqualTo(end));
        }

        [Test]
        public void MovingNodesKeepMinimumTouchableSeparation()
        {
            var model = new StarRainBattleModel();
            model.Begin(467);
            for (int step = 0; step < 1800 && model.Phase == BattlePhase.Running; step++)
            {
                model.Tick(1f / 60f);
                for (int i = 0; i < model.Nodes.Count; i++)
                    for (int j = i + 1; j < model.Nodes.Count; j++)
                        Assert.That(Vector2.Distance(model.Nodes[i].Position, model.Nodes[j].Position),
                            Is.GreaterThanOrEqualTo(StarRainBattleModel.MinimumNodeSeparation - 0.001f),
                            "Nodes {0} and {1} overlapped at time {2}", i, j, model.Time);
            }
        }
    }
}

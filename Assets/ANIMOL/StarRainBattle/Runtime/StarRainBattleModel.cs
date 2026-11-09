using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

namespace ANIMOL.StarRainBattle
{
    public enum BattlePhase { Ready, Running, Paused, Won, Lost }
    public enum MeteorTeam { Isol, Curse, Reflected }
    public enum BattleEventType { Link, Block, Misblock, Reflect, NodeDim, NodeHit, CoreHit, Win, Lose }

    public struct BattleEvent
    {
        public BattleEventType Type { get; private set; }
        public Vector2 Position { get; private set; }
        public MeteorTeam Team { get; private set; }
        public int NodeId { get; private set; }

        public BattleEvent(BattleEventType type, Vector2 position, MeteorTeam team, int nodeId = -1)
        {
            Type = type;
            Position = position;
            Team = team;
            NodeId = nodeId;
        }
    }

    public sealed class NodeState
    {
        public int Id { get; internal set; }
        public Vector2 Position { get; internal set; }
        public Vector2 Target { get; internal set; }
        public float Speed { get; internal set; }
        public float NextTargetTime { get; internal set; }
        public float DimUntil { get; internal set; }
    }

    public sealed class MeteorState
    {
        public int Id { get; internal set; }
        public MeteorTeam Team { get; internal set; }
        public Vector2 Start { get; internal set; }
        public Vector2 Control { get; internal set; }
        public Vector2 End { get; internal set; }
        public Vector2 PreviousPosition { get; internal set; }
        public Vector2 Position { get; internal set; }
        public float Progress { get; internal set; }
        public float Duration { get; internal set; }
        public float SkipLineTime { get; internal set; }
        internal bool Dead;

        public Vector2 Tangent
        {
            get
            {
                float t = Progress;
                Vector2 d = 2f * (1f - t) * (Control - Start) + 2f * t * (End - Control);
                return d.sqrMagnitude > 0.000001f ? d.normalized : Vector2.up;
            }
        }
    }

    /// <summary>
    /// Deterministic 400x620, top-left-origin combat simulation. No scene objects,
    /// pixels, sprites or particles are created here; the presenter only observes it.
    /// </summary>
    public sealed class StarRainBattleModel
    {
        public const float Width = 400f;
        public const float Height = 620f;
        public const float LinkLifetime = 0.5f;
        public const float LineRadius = 8.5f;
        public const float NodeHitRadius = 8.5f;
        public const float CoreHitRadius = 18f;
        public const float SelectRadius = 30f;
        public const float MinimumNodeSeparation = 44f;
        public const int IsolMaxHP = 7;
        public const int CurseMaxHP = 11;

        public static readonly Vector2 IsolCore = new Vector2(200f, 544f);
        public static readonly Vector2 CurseCore = new Vector2(200f, 76f);

        private const double FixedStep = 1.0 / 60.0;
        private const float TieEpsilon = 0.000001f;
        private readonly List<NodeState> nodes = new List<NodeState>(5);
        private readonly List<MeteorState> meteors = new List<MeteorState>(12);
        private readonly ReadOnlyCollection<NodeState> nodeView;
        private readonly ReadOnlyCollection<MeteorState> meteorView;
        private uint randomState = 1;
        private double accumulator;
        private int tickCount;
        private int nextMeteorId;
        private float nextCurse;
        private float nextIsol;

        public event Action<BattleEvent> EventRaised;

        public StarRainBattleModel()
        {
            nodeView = nodes.AsReadOnly();
            meteorView = meteors.AsReadOnly();
        }

        public IReadOnlyList<NodeState> Nodes { get { return nodeView; } }
        public IReadOnlyList<MeteorState> Meteors { get { return meteorView; } }
        public BattlePhase Phase { get; private set; } = BattlePhase.Ready;
        public int Seed { get; private set; }
        public float Time { get; private set; }
        public int IsolHP { get; private set; } = IsolMaxHP;
        public int CurseHP { get; private set; } = CurseMaxHP;
        public int BlockedEnemy { get; private set; }
        public int BlockedFriend { get; private set; }
        public bool Augmented { get; private set; }
        public bool HasLink { get; private set; }
        public int LinkA { get; private set; } = -1;
        public int LinkB { get; private set; } = -1;
        public float LinkExpires { get; private set; }

        public void Prepare(int seed)
        {
            Reset(seed);
            Phase = BattlePhase.Ready;
        }

        public void Begin(int seed)
        {
            Reset(seed);
            Phase = BattlePhase.Running;
        }

        public void RestartSameSeed() { Begin(Seed); }
        public void Pause() { if (Phase == BattlePhase.Running) Phase = BattlePhase.Paused; }
        public void Resume() { if (Phase == BattlePhase.Paused) Phase = BattlePhase.Running; }
        public void SetAugment(bool enabled) { Augmented = enabled; }

        public bool TryConnect(int a, int b)
        {
            if (Phase != BattlePhase.Running || a == b || a < 0 || b < 0 || a >= nodes.Count || b >= nodes.Count)
                return false;
            if (nodes[a].DimUntil > Time || nodes[b].DimUntil > Time)
                return false;

            // A new link replaces the old one. Endpoints follow the moving stars.
            HasLink = true;
            LinkA = a;
            LinkB = b;
            LinkExpires = Time + LinkLifetime;
            Raise(BattleEventType.Link, (nodes[a].Position + nodes[b].Position) * 0.5f, MeteorTeam.Isol);
            return true;
        }

        public void Tick(float deltaTime)
        {
            if (Phase != BattlePhase.Running || deltaTime <= 0f || float.IsNaN(deltaTime) || float.IsInfinity(deltaTime))
                return; // Pause freezes the clock, links, dimming, shots and stars.

            accumulator += deltaTime;
            while (accumulator + 1e-7 >= FixedStep && Phase == BattlePhase.Running)
            {
                Step();
                accumulator = Math.Max(0, accumulator - FixedStep);
            }
            // Pointer input arrives between fixed steps. Keep its clock at the
            // complete elapsed time so a new link does not lose that remainder.
            // Physics/spawns still observe the exact 60 Hz time inside Step().
            if (Phase == BattlePhase.Running) Time = tickCount / 60f + (float)accumulator;
            else if (Phase == BattlePhase.Won || Phase == BattlePhase.Lost) accumulator = 0;
            if (HasLink && LinkExpires <= Time) HasLink = false;
        }

        private void Reset(int seed)
        {
            Seed = seed;
            randomState = unchecked((uint)seed);
            if (randomState == 0) randomState = 1;
            accumulator = 0;
            tickCount = 0;
            nextMeteorId = 0;
            Time = 0;
            IsolHP = IsolMaxHP;
            CurseHP = CurseMaxHP;
            BlockedEnemy = 0;
            BlockedFriend = 0;
            HasLink = false;
            LinkA = LinkB = -1;
            LinkExpires = 0;
            nextCurse = 0.85f;
            nextIsol = 1.22f;
            nodes.Clear();
            meteors.Clear();

            Vector2[] initial = {
                new Vector2(84f, 213f), new Vector2(198f, 193f),
                new Vector2(313f, 236f), new Vector2(124f, 381f),
                new Vector2(293f, 395f)
            };
            for (int i = 0; i < initial.Length; i++)
            {
                Vector2 p = initial[i] + new Vector2(Between(-10f, 10f), Between(-9f, 9f));
                nodes.Add(new NodeState {
                    Id = i,
                    Position = p,
                    Target = initial[i] + new Vector2(Between(-24f, 24f), Between(-25f, 25f)),
                    Speed = Between(7f, 11f),
                    NextTargetTime = Between(1.6f, 3.6f),
                    DimUntil = 0f
                });
            }
        }

        private void Step()
        {
            tickCount++;
            Time = tickCount / 60f;
            UpdateNodes((float)FixedStep);
            if (HasLink && LinkExpires <= Time) HasLink = false;

            while (Time >= nextCurse)
            {
                Launch(MeteorTeam.Curse);
                nextCurse += Between(1.15f, 1.45f);
            }
            while (Time >= nextIsol)
            {
                Launch(MeteorTeam.Isol);
                nextIsol += Between(1.12f, 1.42f);
            }
            UpdateMeteors((float)FixedStep);
        }

        private void ChooseTarget(NodeState node)
        {
            for (int tries = 0; tries < 14; tries++)
            {
                Vector2 p = new Vector2(Between(56f, 344f), Between(168f, 453f));
                bool separated = true;
                for (int i = 0; i < nodes.Count; i++)
                {
                    NodeState other = nodes[i];
                    if (other == node) continue;
                    if ((p - other.Position).sqrMagnitude <= 62f * 62f ||
                        (p - other.Target).sqrMagnitude <= 62f * 62f)
                    { separated = false; break; }
                }
                if (!separated) continue;
                node.Target = p;
                break;
            }
            node.NextTargetTime = Time + Between(2.2f, 4.4f);
            node.Speed = Between(7f, 10f);
        }

        private void UpdateNodes(float dt)
        {
            for (int i = 0; i < nodes.Count; i++)
            {
                NodeState node = nodes[i];
                Vector2 direction = node.Target - node.Position;
                if (Time >= node.NextTargetTime || direction.sqrMagnitude < 4f)
                {
                    ChooseTarget(node);
                    direction = node.Target - node.Position;
                }
                float distance = direction.magnitude;
                if (distance < 0.00001f) continue;
                Vector2 candidate = node.Position + direction / distance * Mathf.Min(node.Speed * dt, distance);
                bool separated = true;
                for (int j = 0; j < nodes.Count; j++)
                {
                    if (i == j) continue;
                    if ((candidate - nodes[j].Position).sqrMagnitude < MinimumNodeSeparation * MinimumNodeSeparation)
                    { separated = false; break; }
                }
                if (separated) node.Position = candidate;
                else node.NextTargetTime = Mathf.Min(node.NextTargetTime, Time + 0.35f);
            }
        }

        private void Launch(MeteorTeam team)
        {
            bool curse = team == MeteorTeam.Curse;
            Vector2 start = curse ? CurseCore : IsolCore;
            Vector2 end = curse ? IsolCore : CurseCore;
            Vector2 control = new Vector2(200f + Between(-176f, 176f), Between(280f, 344f));
            MeteorState shot = new MeteorState {
                Id = nextMeteorId++, Team = team, Start = start, End = end,
                Control = control, Position = start, PreviousPosition = start,
                Progress = 0f, Duration = Between(2.8f, 3.4f), SkipLineTime = 0f
            };
            meteors.Add(shot);
        }

        private static Vector2 Bezier(MeteorState shot, float t)
        {
            float u = 1f - t;
            return u * u * shot.Start + 2f * u * t * shot.Control + t * t * shot.End;
        }

        private void UpdateMeteors(float dt)
        {
            for (int i = 0; i < meteors.Count && Phase == BattlePhase.Running; i++)
            {
                MeteorState shot = meteors[i];
                if (shot.Dead) continue;
                Vector2 from = Bezier(shot, shot.Progress);
                shot.PreviousPosition = from;
                shot.Progress = Mathf.Min(1f, shot.Progress + dt / shot.Duration);
                Vector2 to = Bezier(shot, shot.Progress);
                shot.Position = to;
                shot.SkipLineTime = Mathf.Max(0f, shot.SkipLineTime - dt);

                float bestT = float.PositiveInfinity;
                BattleEventType hitType = BattleEventType.CoreHit;
                int hitNode = -1;
                if (shot.SkipLineTime <= 0f && HasLink && LineResponse(shot.Team) != LinePolicy.Pass)
                    Accept(CapsuleIntersection(from, to, nodes[LinkA].Position, nodes[LinkB].Position, LineRadius),
                        BattleEventType.Link, -1, ref bestT, ref hitType, ref hitNode);

                for (int j = 0; j < nodes.Count; j++)
                    Accept(CircleIntersection(from, to, nodes[j].Position, NodeHitRadius),
                        BattleEventType.NodeDim, j, ref bestT, ref hitType, ref hitNode);

                Vector2 target = shot.Team == MeteorTeam.Curse ? IsolCore : CurseCore;
                Accept(CircleIntersection(from, to, target, CoreHitRadius),
                    BattleEventType.CoreHit, -1, ref bestT, ref hitType, ref hitNode);

                if (!float.IsPositiveInfinity(bestT))
                {
                    Vector2 where = Vector2.LerpUnclamped(from, to, bestT);
                    Resolve(shot, hitType, hitNode, where);
                }
                if (shot.Progress >= 1f && !shot.Dead) shot.Dead = true;
            }
            for (int i = meteors.Count - 1; i >= 0; i--)
                if (meteors[i].Dead) meteors.RemoveAt(i);
        }

        private enum LinePolicy { Absorb, Pass, Reflect }
        private LinePolicy LineResponse(MeteorTeam team)
        {
            if (!Augmented) return LinePolicy.Absorb;
            if (team == MeteorTeam.Curse) return LinePolicy.Reflect;
            return LinePolicy.Pass;
        }

        private void Resolve(MeteorState shot, BattleEventType hit, int nodeIndex, Vector2 where)
        {
            if (hit == BattleEventType.Link)
            {
                if (LineResponse(shot.Team) == LinePolicy.Reflect)
                {
                    shot.Team = MeteorTeam.Reflected;
                    shot.Start = where;
                    shot.End = CurseCore;
                    shot.Control = new Vector2(Mathf.Clamp(where.x, 60f, 340f), (where.y + CurseCore.y) * 0.5f);
                    shot.Progress = 0f;
                    shot.Duration = 1.15f;
                    shot.SkipLineTime = 0.16f;
                    shot.Position = where;
                    shot.PreviousPosition = where;
                    Raise(BattleEventType.Reflect, where, MeteorTeam.Reflected);
                }
                else
                {
                    if (shot.Team == MeteorTeam.Curse)
                    {
                        BlockedEnemy++;
                        Raise(BattleEventType.Block, where, shot.Team);
                    }
                    else
                    {
                        BlockedFriend++;
                        Raise(BattleEventType.Misblock, where, shot.Team);
                    }
                    shot.Dead = true;
                }
                return;
            }

            if (hit == BattleEventType.NodeDim)
            {
                if (shot.Team == MeteorTeam.Curse)
                {
                    nodes[nodeIndex].DimUntil = Time + 0.68f;
                    Raise(BattleEventType.NodeDim, where, shot.Team, nodeIndex);
                }
                else Raise(BattleEventType.NodeHit, where, shot.Team, nodeIndex);
                shot.Dead = true;
                return;
            }

            if (shot.Team == MeteorTeam.Curse) IsolHP = Mathf.Max(0, IsolHP - 1);
            else CurseHP = Mathf.Max(0, CurseHP - 1);
            shot.Dead = true;
            Raise(BattleEventType.CoreHit, where, shot.Team);
            if (IsolHP == 0 || CurseHP == 0)
            {
                Phase = CurseHP == 0 ? BattlePhase.Won : BattlePhase.Lost;
                Raise(Phase == BattlePhase.Won ? BattleEventType.Win : BattleEventType.Lose, where, shot.Team);
            }
        }

        private void Raise(BattleEventType type, Vector2 position, MeteorTeam team, int nodeId = -1)
        {
            EventRaised?.Invoke(new BattleEvent(type, position, team, nodeId));
        }

        private static void Accept(float? t, BattleEventType type, int node,
            ref float bestT, ref BattleEventType bestType, ref int bestNode)
        {
            // Evaluation order is line -> nodes -> core. Equal t keeps that order.
            if (t.HasValue && t.Value < bestT - TieEpsilon)
            { bestT = t.Value; bestType = type; bestNode = node; }
        }

        private static float? SegmentIntersection(Vector2 a, Vector2 b, Vector2 c, Vector2 d)
        {
            Vector2 r = b - a, s = d - c;
            float den = r.x * s.y - r.y * s.x;
            if (Mathf.Abs(den) < 0.0000001f) return null;
            Vector2 q = c - a;
            float t = (q.x * s.y - q.y * s.x) / den;
            float u = (q.x * r.y - q.y * r.x) / den;
            return t >= 0f && t <= 1f && u >= 0f && u <= 1f ? t : (float?)null;
        }

        private static float? CircleIntersection(Vector2 a, Vector2 b, Vector2 center, float radius)
        {
            Vector2 delta = b - a, offset = a - center;
            float A = Vector2.Dot(delta, delta);
            if (A < 0.00000001f) return offset.sqrMagnitude <= radius * radius ? 0f : (float?)null;
            float B = 2f * Vector2.Dot(offset, delta);
            float C = offset.sqrMagnitude - radius * radius;
            if (C <= 0f) return 0f;
            float disc = B * B - 4f * A * C;
            if (disc < 0f) return null;
            float t = (-B - Mathf.Sqrt(disc)) / (2f * A);
            return t >= 0f && t <= 1f ? t : (float?)null;
        }

        private static float? CapsuleIntersection(Vector2 from, Vector2 to, Vector2 a, Vector2 b, float radius)
        {
            Vector2 v = b - a;
            float length = v.magnitude;
            if (length < 0.00001f) return CircleIntersection(from, to, a, radius);
            float along = Mathf.Clamp01(Vector2.Dot(from - a, v) / (length * length));
            Vector2 closest = a + along * v;
            if ((from - closest).sqrMagnitude <= radius * radius) return 0f;

            Vector2 offset = new Vector2(-v.y, v.x) * (radius / length);
            float best = float.PositiveInfinity;
            float? candidate = SegmentIntersection(from, to, a + offset, b + offset);
            if (candidate.HasValue) best = Mathf.Min(best, candidate.Value);
            candidate = SegmentIntersection(from, to, a - offset, b - offset);
            if (candidate.HasValue) best = Mathf.Min(best, candidate.Value);
            candidate = CircleIntersection(from, to, a, radius);
            if (candidate.HasValue) best = Mathf.Min(best, candidate.Value);
            candidate = CircleIntersection(from, to, b, radius);
            if (candidate.HasValue) best = Mathf.Min(best, candidate.Value);
            return float.IsPositiveInfinity(best) ? (float?)null : best;
        }

        private float Between(float min, float max) { return min + (max - min) * NextRandom(); }
        private float NextRandom()
        {
            randomState ^= randomState << 13;
            randomState ^= randomState >> 17;
            randomState ^= randomState << 5;
            // The top 24 bits map exactly to [0,1); float rounding cannot produce 1.
            return (randomState >> 8) / 16777216f;
        }

#if UNITY_EDITOR
        // Explicit seams for deterministic EditMode collision tests; never used by a scene.
        public void TestSetNode(int index, Vector2 position)
        {
            NodeState node = nodes[index];
            node.Position = position;
            node.Target = position + new Vector2(1000f, 0f);
            node.Speed = 0f;
            node.NextTargetTime = float.PositiveInfinity;
            node.DimUntil = 0f;
        }

        public int TestAddMeteor(MeteorTeam team, Vector2 start, Vector2 control, Vector2 end, float duration)
        {
            MeteorState shot = new MeteorState {
                Id = nextMeteorId++, Team = team, Start = start, Control = control, End = end,
                Position = start, PreviousPosition = start, Progress = 0f,
                Duration = Mathf.Max(0.01f, duration)
            };
            meteors.Add(shot);
            return shot.Id;
        }

        public void TestSetSpawnTimes(float curse, float isol)
        {
            nextCurse = curse;
            nextIsol = isol;
        }
#endif
    }
}

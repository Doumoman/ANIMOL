using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace ANIMOL.Puzzle
{
    // V3 campaign policy sits outside the device rules. Only an authoritative transition is committed.
    public sealed class StarwayV3StrictPolicy
    {
        public enum Decision { Accepted, WrongInput, TransitionMismatch, Locked }
        public Decision LastDecision { get; private set; }
        public string Error { get; private set; }
        public int Step { get; private set; }
        bool failed;
        public bool Failed => failed;
        readonly JArray steps;

        StarwayV3StrictPolicy(JArray steps, int step)
        { this.steps = steps; Step = step; }

        public StarwayV3StrictPolicy Copy()
        {
            var copy = new StarwayV3StrictPolicy(steps, Step);
            copy.failed = failed; copy.LastDecision = LastDecision; copy.Error = Error;
            return copy;
        }

        public StarwayV3StrictPolicy(PuzzleMap map)
        {
            if (!map || !map.starwayV3Certificate) throw new ArgumentException("V3 certificate is required.");
            var certificate = JObject.Parse(map.starwayV3Certificate.text);
            if ((string)certificate["schemaVersion"] != "starway-strict-sidecar-v3" ||
                (string)certificate["scenarioHash"] != map.starwayV3ScenarioSha256)
                throw new ArgumentException("V3 certificate version or scenario hash differs from the map.");
            steps = certificate["steps"] as JArray ?? throw new ArgumentException("V3 steps are missing.");
            if (steps.Count != map.scenario.costLimit || (int)certificate["shortestLength"] != steps.Count ||
                (int)certificate["optimalInputSequenceCount"] != 1)
                throw new ArgumentException("V3 answer length or uniqueness proof differs from the map.");
        }

        public void Reset() { Step = 0; failed = false; LastDecision = Decision.Accepted; Error = null; }

        public bool TryApply(PuzzleDeviceSession authority, Vector2Int direction, out PrototypeAction action)
        {
            action = new PrototypeAction { reason = "Input unavailable" };
            if (Failed || authority.Won || authority.Failed || Step >= steps.Count)
            { LastDecision = Decision.Locked; return false; }
            if (!PuzzleSession.IsCardinal(direction)) return false;
            var record = (JObject)steps[Step];
            var before = StateHash(authority);
            if (before != (string)record["beforeHash"])
            {
                LastDecision = Decision.TransitionMismatch;
                failed = true;
                Error = "V3 source/Unity state differs before input " + (Step + 1);
                return false;
            }
            var expected = (string)record["expectedAction"];
            var actual = direction == Vector2Int.up ? "N" : direction == Vector2Int.right ? "E" :
                direction == Vector2Int.down ? "S" : "W";
            if (actual != expected)
            {
                LastDecision = Decision.WrongInput;
                failed = true;
                Error = "WrongInput";
                return false;
            }
            var candidate = authority.Copy();
            var result = candidate.Apply(direction);
            if (!result.accepted || StateHash(candidate) != (string)record["afterHash"] || candidate.Cost != Step + 1)
            {
                LastDecision = Decision.TransitionMismatch;
                failed = true;
                Error = "V3 source/Unity transition differs at input " + (Step + 1);
                return false;
            }
            authority.RestoreSnapshot(candidate);
            action = result;
            Step++;
            LastDecision = Decision.Accepted;
            return true;
        }

        public static JObject Project(PuzzleDeviceSession session)
        {
            var map = session.Map;
            var rules = session.Rules;
            var state = new JObject
            {
                ["player"] = Point(session.Cell),
                ["stones"] = new JObject(), ["crates"] = new JObject(), ["sheep"] = new JObject(),
                ["activeSockets"] = new JArray(), ["remainingGrass"] = new JArray(),
                ["sluiceWetA"] = new JObject(), ["bubbleMask"] = 0, ["visitedMask"] = 0,
                ["windPhases"] = new JObject()
            };
            foreach (var d in rules.devices)
            {
                if (!session.States.TryGetValue(d.id, out var s)) continue;
                if (d.role == PrototypeRole.StarSocket && d.variant != "socket")
                    ((JObject)state["stones"])[d.id] = s.removed ? JValue.CreateNull() : Point(s.cell);
                if (d.role == PrototypeRole.WoodCrate || d.role == PrototypeRole.MetalCrate)
                    ((JObject)state["crates"])[d.id] = s.removed ? JValue.CreateNull() : Point(s.cell);
                if (d.role == PrototypeRole.StarSocket && d.variant == "socket" && s.active)
                    ((JArray)state["activeSockets"]).Add(d.id);
                if (d.role == PrototypeRole.Wind && d.variant == "blower")
                    ((JObject)state["windPhases"])[d.id] = s.phase;
            }
            foreach (var art in rules.ranchV13Art)
            {
                if (art.family == "sheep")
                    ((JObject)state["sheep"])[art.Id] = session.RanchSheep.TryGetValue(art.Id, out var p) ? Point(p) : JValue.CreateNull();
                if (art.family == "tall_grass" && session.RanchGrassPresent(art.position))
                    ((JArray)state["remainingGrass"]).Add(art.Id);
                if (art.family == "sluice")
                    ((JObject)state["sluiceWetA"])[art.Id] = session.RanchPonds[art.Id];
            }
            var bubbleMask = 0;
            for (var i = 0; i < map.bubbles.Count; i++) if (session.IsCollected(map.bubbles[i])) bubbleMask |= 1 << i;
            state["bubbleMask"] = bubbleMask;
            return state;
        }

        static JArray Point(Vector2Int p) => new JArray(p.x, p.y);
        public static string StateHash(PuzzleDeviceSession session) => Hash(Project(session));
        public static string Hash(JToken token)
        {
            var bytes = Encoding.UTF8.GetBytes(Canonical(token));
            using (var sha = SHA256.Create())
                return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant();
        }
        public static string Canonical(JToken token)
        {
            if (token is JObject obj)
                return "{" + string.Join(",", obj.Properties().OrderBy(p => p.Name, StringComparer.Ordinal)
                    .Select(p => JsonConvert.SerializeObject(p.Name) + ":" + Canonical(p.Value))) + "}";
            if (token is JArray array) return "[" + string.Join(",", array.Select(Canonical)) + "]";
            return token.ToString(Formatting.None);
        }
    }
}

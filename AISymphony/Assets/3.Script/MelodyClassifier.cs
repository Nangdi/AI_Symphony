using System.Collections.Generic;
using System.Linq;
using System;
using UnityEngine;

public class MelodyClassifier : MonoBehaviour
{


    /// ------------------------------------------------------------
    ///  1) 96종 결정적 분류기 (무작위 균등화 없음)
    ///     - 입력: int[8] notes (각 1..8)
    ///     - 출력: Result { Family(A..G), Subtype("A01".."G20"), Code, Features } 
    /// ------------------------------------------------------------
    public static class MelodyClassifier96
    {
        // ====== 공개 API ========================================================
        public struct Result
        {
            public string Subtype;      // 예: "A01", "G10" 등
            public string Family;       // "A".."G"
            public string Code;         // 예: "A-ASC-STEP-NARROW-END1"
            public Features Feats;      // 판정에 사용된 특징
        }

        public static Result Classify(int[] notes, bool useSoftBalancer = false)
        {
            if (notes == null || notes.Length != 8) throw new ArgumentException("notes must be length 8");
            if (notes.Any(n => n < 1 || n > 8)) throw new ArgumentException("each note must be in 1..8");

            var f = ExtractFeatures(notes);

            // 1) C
            string c = TryC(notes, f);
            if (Pick(ref c, "C", f, useSoftBalancer, out var r1)) return r1;

            // 2) D
            string d = TryD(notes, f);
            if (Pick(ref d, "D", f, useSoftBalancer, out var r2)) return r2;

            // 3) E
            string e = TryE(notes, f);
            if (Pick(ref e, "E", f, useSoftBalancer, out var r3)) return r3;

            // 4) F
            string ff = TryF(notes, f);
            if (Pick(ref ff, "F", f, useSoftBalancer, out var r4)) return r4;

            // 5) A/B/G
            string fam = FamilyForEnd(f.EndNote);
            string abg = fam switch
            {
                "A" => SubtypeA(f),
                "B" => SubtypeB(f),
                _ => SubtypeG(f),
            };
            if (Pick(ref abg, fam, f, useSoftBalancer, out var r5)) return r5;

            return new Result { Subtype = "G07", Family = "G", Code = BuildCode("G", f, "G07"), Feats = f };
        }

        // ====== Soft Balancer (선택) ============================================
        public static class SoftBalancer
        {
            public static float Target = 1f / 96f;
            public static float MinPct = 0.01f; // 1%
            public static float MaxPct = 0.05f; // 5%

            private static readonly Dictionary<string, int> Counts = new();
            private static int _total = 0;
            private static System.Random _rng = new System.Random(12345);

            public static void Reset(int? seed = null)
            {
                Counts.Clear();
                _total = 0;
                if (seed.HasValue) _rng = new System.Random(seed.Value);
            }

            public static string MaybeReassign(string proposed)
            {
                if (_total <= 0)
                {
                    Bump(proposed);
                    return proposed;
                }

                float freq = GetPct(proposed);
                if (freq > MaxPct)
                {
                    var under = AllLabels.Where(l => GetPct(l) < MinPct).ToList();
                    if (under.Count > 0)
                    {
                        var pick = under[_rng.Next(under.Count)];
                        Bump(pick);
                        return pick;
                    }
                }

                Bump(proposed);
                return proposed;
            }

            private static void Bump(string label)
            {
                if (!Counts.ContainsKey(label)) Counts[label] = 0;
                Counts[label]++;
                _total++;
            }

            private static float GetPct(string label)
            {
                if (_total == 0) return 0f;
                Counts.TryGetValue(label, out int c);
                return (float)c / _total;
            }

            public static int GetCount(string label) => Counts.TryGetValue(label, out var c) ? c : 0;
            public static int Total => _total;
        }

        // ====== 특징 추출 ========================================================
        private const float STEP_RATIO_THRESH = 0.70f;
        private const float LEAP_RATIO_THRESH = 0.50f;

        public struct Features
        {
            public int[] Notes;
            public int[] Steps;
            public int[] Dirs;
            public int EndNote;
            public int MaxNote, MinNote, Range;
            public string RangeBand;   // "NARROW","MEDIUM","WIDE"
            public int SignChanges;
            public string Contour;     // "ASC","DESC","ARCH","VALLEY","WAVE","FLAT"
            public string Movement;    // "STEP","LEAP","MIX"
            public float StepRatio, LeapRatio;
            public bool Has71, Has43;
            public int AltCount;
            public int ZeroHolds;
            public int MaxLeap;
        }

        private static Features ExtractFeatures(int[] notes)
        {
            var steps = new int[7];
            var dirs = new int[7];
            int zeros = 0;
            for (int i = 0; i < 7; i++)
            {
                steps[i] = notes[i + 1] - notes[i];
                dirs[i] = steps[i] == 0 ? 0 : (steps[i] > 0 ? 1 : -1);
                if (steps[i] == 0) zeros++;
            }

            int signChanges = 0;
            int last = 0;
            for (int i = 0; i < dirs.Length; i++)
            {
                int d = dirs[i];
                if (d == 0) continue;
                if (last != 0 && d != last) signChanges++;
                last = d;
            }

            int max = notes.Max();
            int min = notes.Min();
            int range = max - min;
            string rangeBand = range <= 3 ? "NARROW" : (range <= 5 ? "MEDIUM" : "WIDE");

            int stepCount = steps.Count(s => Mathf.Abs(s) <= 1);
            int leapCount = steps.Count(s => Mathf.Abs(s) >= 2);
            float stepRatio = stepCount / 7f;
            float leapRatio = leapCount / 7f;
            string move = stepRatio >= STEP_RATIO_THRESH ? "STEP" : (leapRatio >= LEAP_RATIO_THRESH ? "LEAP" : "MIX");

            bool anyUp = dirs.Any(d => d > 0);
            bool anyDn = dirs.Any(d => d < 0);
            bool allZero = dirs.All(d => d == 0);
            string contour;
            if (allZero) contour = "FLAT";
            else if (anyUp && !anyDn) contour = "ASC";
            else if (!anyUp && anyDn) contour = "DESC";
            else if (signChanges == 1)
            {
                int first = dirs.First(d => d != 0);
                contour = first > 0 ? "ARCH" : "VALLEY";
            }
            else contour = "WAVE";

            bool has71 = false, has43 = false;
            for (int i = 0; i < 7; i++)
            {
                if (notes[i] == 7 && notes[i + 1] == 1) has71 = true;
                if (notes[i] == 4 && notes[i + 1] == 3) has43 = true;
            }

            int alt = 0; int prev = 0;
            for (int i = 0; i < dirs.Length; i++)
            {
                int d = dirs[i];
                if (Mathf.Abs(d) == 1 && prev == -d) alt++;
                if (d != 0) prev = d;
            }
            int maxLeap = steps.Max(s => Mathf.Abs(s));

            return new Features
            {
                Notes = (int[])notes.Clone(),
                Steps = steps,
                Dirs = dirs,
                EndNote = notes[7],
                MaxNote = max,
                MinNote = min,
                Range = range,
                RangeBand = rangeBand,
                SignChanges = signChanges,
                Contour = contour,
                Movement = move,
                StepRatio = stepRatio,
                LeapRatio = leapRatio,
                Has71 = has71,
                Has43 = has43,
                AltCount = alt,
                ZeroHolds = zeros,
                MaxLeap = maxLeap
            };
        }

        // ====== 유틸 =============================================================
        private static string FamilyForEnd(int end)
        {
            if (end == 1 || end == 8) return "A";
            if (end == 5 || end == 3) return "B";
            return "G";
        }

        private static string BuildCode(string fam, Features f, string subtype)
        {
            return $"{fam}-{f.Contour}-{f.Movement}-{f.RangeBand}-END{f.EndNote}:{subtype}";
        }

        private static bool Pick(ref string subtype, string fam, Features f, bool useSoft, out Result res)
        {
            if (!string.IsNullOrEmpty(subtype))
            {
                string chosen = subtype;
                if (useSoft) chosen = SoftBalancer.MaybeReassign(chosen);
                res = new Result { Subtype = chosen, Family = fam, Code = BuildCode(fam, f, chosen), Feats = f };
                return true;
            }
            res = default;
            return false;
        }

        // ====== C 패턴 ===========================================================
        private static string TryC(int[] n, Features f)
        {
            int c1 = n.Count(x => x == 1);
            int c5 = n.Count(x => x == 5);
            if (c1 >= 5) return "C01";
            if (c1 == 4) return "C02";
            if (c5 >= 5) return "C03";
            if (c5 == 4) return "C04";

            int diffA = 0; for (int i = 0; i < 4; i++) diffA += Mathf.Abs(n[i] - n[i + 4]);
            if (diffA <= 1) return "C05";
            if (diffA <= 2) return "C06";

            bool ababStrict = (n[0] == n[2] && n[1] == n[3] && n[2] == n[4] && n[3] == n[5] && n[4] == n[6] && n[5] == n[7] && !(n[0] == n[4] && n[1] == n[5]));
            if (ababStrict) return "C07";
            bool ababLoose = (n[0] == n[2] && n[1] == n[3] && n[4] == n[6] && n[5] == n[7] && !(n[0] == n[4] && n[1] == n[5]));
            if (ababLoose) return "C08";

            int diffB = 0; for (int i = 0; i < 6; i++) diffB += Mathf.Abs(n[i] - n[Mathf.Min(i + 1, 6)]);
            if (diffB <= 2) return "C09";
            if (diffB <= 3) return "C10";

            float[] mu = new float[4]; for (int b = 0; b < 4; b++) mu[b] = 0.5f * (n[2 * b] + n[2 * b + 1]);
            float d1 = mu[1] - mu[0], d2 = mu[2] - mu[1], d3 = mu[3] - mu[2];
            bool internalOk = true; for (int b = 0; b < 4; b++) if (Mathf.Abs(n[2 * b + 1] - n[2 * b]) > 2) { internalOk = false; break; }
            if (internalOk && Mathf.Abs(d1) == 1f && d2 == d1 && d3 == d1 && f.Contour != "WAVE")
                return d1 > 0 ? "C11" : "C12";

            return null;
        }

        // ====== D 패턴 ===========================================================
        private static string TryD(int[] n, Features f)
        {
            int d12 = n[2] - n[0]; int d34 = n[4] - n[2]; int d56 = n[6] - n[4];
            if (Mathf.Abs(d12) == 1 && d34 == d12 && d56 == d12) return d12 > 0 ? "D01" : "D02";

            int d = n[3] - n[0];
            if (Mathf.Abs(d) == 1 && (n[4] - n[1]) == d && (n[5] - n[2]) == d) return d > 0 ? "D05" : "D06";
            int d2 = n[5] - n[2];
            if (Mathf.Abs(d2) == 1 && (n[6] - n[3]) == d2 && (n[7] - n[4]) == d2) return d2 > 0 ? "D05" : "D06";

            if (Mathf.Abs(d12) == 1 && ((d34 == d12 && Mathf.Abs(d56 - d12) <= 1) || (Mathf.Abs(d34 - d12) <= 1 && d56 == d12)))
                return d12 > 0 ? "D03" : "D04";
            if (Mathf.Abs(d) == 1 && ((n[4] - n[1]) == d) && Mathf.Abs((n[5] - n[2]) - d) <= 1)
                return d > 0 ? "D07" : "D08";

            if (f.Contour == "ARCH" && f.Movement != "MIX") return "D09";
            if ((f.EndNote == 1 || f.EndNote == 8 || f.EndNote == 5 || f.EndNote == 3) && (n[0] != n[1] || n[1] != n[2])) return "D10";

            return null;
        }

        // ====== E 패턴 ===========================================================
        private static bool StrongArpeggioWindow(int[] n, int i, out int dir, out int maxJump)
        {
            dir = 0; maxJump = 0;
            if (i + 3 >= n.Length) return false;
            int s1 = n[i + 1] - n[i], s2 = n[i + 2] - n[i + 1], s3 = n[i + 3] - n[i + 2];
            if (s1 == 0 || s2 == 0 || s3 == 0) return false;
            bool signAll = (s1 > 0 && s2 > 0 && s3 > 0) || (s1 < 0 && s2 < 0 && s3 < 0);
            int leaps2 = 0; if (Mathf.Abs(s1) >= 2) leaps2++; if (Mathf.Abs(s2) >= 2) leaps2++; if (Mathf.Abs(s3) >= 2) leaps2++;
            int leaps3 = 0; if (Mathf.Abs(s1) >= 3) leaps3++; if (Mathf.Abs(s2) >= 3) leaps3++; if (Mathf.Abs(s3) >= 3) leaps3++;
            maxJump = Mathf.Max(Mathf.Abs(s1), Mathf.Abs(s2), Mathf.Abs(s3));
            if (signAll && leaps2 == 3 && leaps3 >= 2) { dir = s1 > 0 ? 1 : -1; return true; }
            return false;
        }

        private static string TryE(int[] n, Features f)
        {
            bool arp = false; int dir = 0; int maxJump = 0;
            for (int i = 0; i < 5; i++) if (StrongArpeggioWindow(n, i, out dir, out maxJump)) { arp = true; break; }
            if (arp)
            {
                bool up = dir > 0;
                bool hasI = n.Contains(1) && n.Contains(3) && n.Contains(5);
                bool hasV = n.Contains(5) && n.Contains(7) && n.Contains(2);
                if (hasI) return up ? "E01" : "E02";
                if (hasV) return up ? "E04" : "E05";
                if (up) return maxJump >= 5 ? "E07" : "E06";
                else return maxJump >= 5 ? "E09" : "E08";
            }

            if (n.Contains(1) && n.Contains(3) && n.Contains(5) && f.StepRatio >= 0.5f) return "E03";

            int leapsUp = 0; for (int i = 0; i < 5; i++) if (n[i + 1] - n[i] >= 2) leapsUp++;
            int tail = (n[5] - n[6]) + (n[6] - n[7]);
            if (leapsUp >= 2 && (tail < 0 || n[7] < n[0])) return "E10";

            for (int i = 0; i < 4; i++)
            {
                int s1 = n[i + 1] - n[i], s2 = n[i + 2] - n[i + 1];
                if (s1 * s2 > 0 && (Mathf.Abs(s1) >= 2 || Mathf.Abs(s2) >= 2)) return s1 > 0 ? "E11" : "E12";
            }
            return null;
        }

        // ====== F 패턴 ===========================================================
        private static string TryF(int[] n, Features f)
        {
            int diff = 0; for (int i = 0; i < 4; i++) diff += Mathf.Abs(n[i] - n[7 - i]);
            if (diff <= 2) return "F01";
            if (diff <= 3) return "F02";

            if (f.SignChanges >= 2 && f.RangeBand == "NARROW") return "F03";
            if (f.SignChanges >= 3 && f.RangeBand == "NARROW") return "F04";
            if (f.SignChanges >= 2 && f.RangeBand == "MEDIUM") return "F05";
            if (f.SignChanges >= 3 && f.RangeBand == "MEDIUM") return "F06";

            int neighHits = 0;
            for (int i = 0; i <= 4; i++)
            {
                int a = n[i], b = n[i + 1], c = n[i + 2], d = n[i + 3];
                if (Mathf.Abs(b - a) == 1 && c == a && Mathf.Abs(d - c) == 1) neighHits++;
            }
            if (neighHits >= 3) return "F08";
            if (neighHits >= 2) return "F07";

            int spikeIdx = -1;
            for (int i = 0; i < 7; i++)
            {
                int jump = Mathf.Abs(n[i + 1] - n[i]);
                if (jump >= 7) return "F10";
                if (jump >= 6) spikeIdx = i;
            }
            if (spikeIdx >= 0)
            {
                var rest = n.ToList(); rest.RemoveAt(spikeIdx + 1);
                int r = rest.Max() - rest.Min();
                if (r <= 3) return "F09";
            }

            var counter = new Dictionary<int, int>();
            foreach (var x in n) { if (!counter.ContainsKey(x)) counter[x] = 0; counter[x]++; }
            var top = counter.OrderByDescending(kv => kv.Value).First();
            if (top.Value >= 4)
            {
                int around = 0;
                for (int i = 0; i < 7; i++)
                {
                    if ((n[i] == top.Key && Mathf.Abs(n[i + 1] - top.Key) == 1) || (n[i + 1] == top.Key && Mathf.Abs(n[i] - top.Key) == 1)) around++;
                }
                if (around >= 3) return "F11";
            }

            if (f.SignChanges >= 3 && f.RangeBand == "NARROW" && f.Contour == "WAVE") return "F12";

            return null;
        }

        // ====== A/B/G 서브타입 ===================================================
        private static string SubtypeA(Features f)
        {
            int end = f.EndNote; var rb = f.RangeBand; var mv = f.Movement; var ct = f.Contour;

            if (f.Has71) return "A16";
            if (f.Has43) return "A17";
            int tail = (f.Steps[5] + f.Steps[6]);
            if ((end == 1 || end == 8) && tail < 0) return "A18";
            if (rb == "WIDE" && mv != "STEP") return "A13";

            if (end == 1)
            {
                if (ct == "ASC" && mv == "STEP") return rb == "NARROW" ? "A01" : "A02";
                if (ct == "DESC" && mv == "STEP") return "A03";
                if (ct == "ARCH" && mv == "STEP") return "A04";
                if (ct == "VALLEY" && mv == "STEP") return "A05";
                if (ct == "FLAT") return "A07";
                if (mv == "LEAP") return "A14";
                return "A06"; // WAVE-MIX-NARROW
            }
            else // end==8
            {
                if (ct == "ASC" && mv == "STEP") return "A08";
                if (ct == "DESC" && mv == "STEP") return "A09";
                if (ct == "ARCH" && mv != "STEP") return "A10";
                if (ct == "VALLEY" && mv != "STEP") return "A11";
                if (ct == "WAVE" && rb == "MEDIUM") return "A12";
                if (mv == "LEAP") return "A15";
                return "A06";
            }
        }

        private static string SubtypeB(Features f)
        {
            int end = f.EndNote; var rb = f.RangeBand; var mv = f.Movement; var ct = f.Contour;
            if (end == 5)
            {
                if (ct == "ASC" && mv == "STEP") return "B01";
                if (ct == "DESC" && mv == "STEP") return "B02";
                if (ct == "ARCH" && mv != "STEP") return "B03";
                if (ct == "VALLEY" && mv != "STEP") return "B04";
                if (ct == "WAVE" && rb != "WIDE") return "B05";
                if (ct == "FLAT") return "B06";
                if (mv == "LEAP") return "B07";
                if (rb == "WIDE") return "B08";
            }
            else // end==3
            {
                if (ct == "ASC" && mv == "STEP") return "B09";
                if (ct == "DESC" && mv == "STEP") return "B10";
                if (ct == "WAVE" && rb != "WIDE") return "B11";
                if (rb == "WIDE") return "B12";
                if (mv == "LEAP") return "B07";
            }
            return "B05";
        }

        private static string SubtypeG(Features f)
        {
            var rb = f.RangeBand; var mv = f.Movement; var ct = f.Contour; int turns = f.SignChanges;
            if (ct == "FLAT") return "G11";
            if (mv == "LEAP")
            {
                if (rb == "NARROW") return "G12";
                if (rb == "MEDIUM") return "G13";
                return "G14";
            }
            if (ct == "ASC" && mv == "STEP" && rb == "NARROW") return "G01";
            if (ct == "DESC" && mv == "STEP" && rb == "NARROW") return "G02";
            if (ct == "ARCH") return "G03";
            if (ct == "VALLEY") return "G04";
            if (ct == "WAVE")
            {
                if (rb == "NARROW") return turns >= 3 ? "G06" : "G05";
                if (rb == "MEDIUM") return turns >= 3 ? "G08" : "G07";
                if (rb == "WIDE") return turns >= 3 ? "G10" : "G09";
            }
            if (f.AltCount >= 4 && f.StepRatio >= 0.5f) return "G16"; // MICRO-FAST
            if (f.AltCount >= 3) return "G17";                        // MICRO-SLOW
            if (f.Has71) return "G18";
            if (f.Has43) return "G19";
            if ((f.Steps[6] < 0 && (f.EndNote == 2 || f.EndNote == 4 || f.EndNote > 1)) || (f.Steps[6] > 0 && (f.EndNote == 6 || f.EndNote == 7)))
                return "G20";
            return "G07";
        }

        // ====== 라벨 전체 목록 ===================================================
        public static readonly List<string> AllLabels = BuildAllLabels();

        private static List<string> BuildAllLabels()
        {
            var list = new List<string>(96);
            for (int i = 1; i <= 12; i++) list.Add($"C{i:00}");
            for (int i = 1; i <= 10; i++) list.Add($"D{i:00}");
            for (int i = 1; i <= 12; i++) list.Add($"E{i:00}");
            for (int i = 1; i <= 12; i++) list.Add($"F{i:00}");
            for (int i = 1; i <= 18; i++) list.Add($"A{i:00}");
            for (int i = 1; i <= 12; i++) list.Add($"B{i:00}");
            for (int i = 1; i <= 20; i++) list.Add($"G{i:00}");
            return list;
        }
    }

    /// ------------------------------------------------------------
    ///  2) 라벨에 자연스러운 "서브멜로디" 생성기
    ///     - 원선율과 라벨의 성격을 이용해 8음 보조선율을 만듦
    ///     - 규칙 기반: 대체로 '반진행(contrary/oblique)', 종지 보완, 병행5·8 회피
    /// ------------------------------------------------------------
    public static class SubMelodyGenerator
{
    // 스케일은 1..8 (도레미파솔라시도’) 그대로 사용
    // Helper
    private static int Clamp18(int v) => Mathf.Clamp(v, 1, 8);
    private static int StepTowards(int cur, int target, int maxStep = 1)
        => Clamp18(cur + Math.Sign(target - cur) * Mathf.Min(Math.Abs(target - cur), maxStep));

    private static bool IsPerfect(int a, int b)
    {
        int d = Math.Abs(a - b) % 7; // 1=동일, 4=4도, 5=5도, 0=7도(동명옥타브)
        return d == 0 || d == 4 || d == 5 || d == 1; // 동음(유니즌) 포함 시 병행 회피용 체크
    }

    /// <summary>
    /// 원멜로디와 분류 결과를 받아 8음 서브멜로디 생성
    /// </summary>
    public static int[] Generate(int[] melody, MelodyClassifier96.Result label)
    {
        if (melody == null || melody.Length != 8) throw new ArgumentException();

        // 기본 뼈대: (1) 시작점, (2) 진행방향, (3) 종지
        var sub = new int[8];

        // 1) 시작: 원선율과 3도/6도 관계에서 출발(동음/완전5·8 회피)
        sub[0] = StartIntervalSafe(melody[0]);

        // 2) 라벨 패밀리/서브타입별 진행 규칙
        switch (label.Family)
        {
            case "A": Generate_A(melody, sub, label.Subtype); break;
            case "B": Generate_B(melody, sub, label.Subtype); break;
            case "G": Generate_G(melody, sub, label.Subtype); break;
            case "C": Generate_C(melody, sub, label.Subtype); break;
            case "D": Generate_D(melody, sub, label.Subtype); break;
            case "E": Generate_E(melody, sub, label.Subtype); break;
            case "F": Generate_F(melody, sub, label.Subtype); break;
        }

        // 3) 진행 중 병행5·8 최소화: 직전 구간에서 완전 간격이 반복되면 미세 조정
        for (int i = 1; i < 8; i++)
        {
            // 가능하면 반진행으로 조정
            if (IsPerfect(melody[i], sub[i]) && IsPerfect(melody[i - 1], sub[i - 1]))
            {
                // 한 칸 위/아래로 보정
                int up = Clamp18(sub[i] + 1);
                int dn = Clamp18(sub[i] - 1);
                sub[i] = (!IsPerfect(melody[i], up)) ? up :
                         (!IsPerfect(melody[i], dn)) ? dn : sub[i];
            }
        }
        return sub;
    }

    // ---------- 패밀리별 생성 규칙 ----------

    // A(토닉 종지): 원선율과 반진행 위주, 종지는 3도 또는 5도로 보완(1/8 종지 보강)
    private static void Generate_A(int[] m, int[] s, string subType)
    {
        bool endsOn1 = (m[7] == 1);
        bool endsOn8 = (m[7] == 8);
        int targetEnd = endsOn1 ? 3 : (endsOn8 ? 5 : 3); // 토닉 종지면 3이나 5로 종지 보완

        for (int i = 1; i < 8; i++)
        {
            // 원선율 상승이면 하행, 하강이면 상행(반진행) 기본
            int dir = Math.Sign(m[i] - m[i - 1]);
            int desired = Clamp18(s[i - 1] - dir); // 반대 방향으로 한 칸
            // WAVE나 LEAP 라벨이면 간혹 유지(기대감)
            if (subType == "A06" || subType == "A12" || subType == "A14" || subType == "A15")
                desired = (i % 2 == 0) ? desired : s[i - 1];

            // 마지막 2음은 종지 보강으로 타겟으로 수렴
            if (i >= 6) desired = StepTowards(s[i - 1], targetEnd, 1);
            s[i] = desired;
        }
        // 마지막 한 번 더 조정
        s[7] = StepTowards(s[6], targetEnd, 2);
    }

    // B(개방 종지): 서브는 토닉 기초(1/3/5)에서 출발해 3도 진행, 끝을 1 또는 6으로(약한 종지)
    private static void Generate_B(int[] m, int[] s, string subType)
    {
        // 개방감 유지: 병행 피하려고 지그재그 약하게
        for (int i = 1; i < 8; i++)
        {
            int dir = Math.Sign(m[i] - m[i - 1]);
            // 개방(5/3 끝) → 서브는 1이나 6으로 마무리
            int fallbackEnd = (m[7] == 5) ? 1 : 6;
            int desired = (i < 6)
                ? Clamp18(s[i - 1] + ((i % 2 == 0) ? -dir : 0))  // 가끔 유지(오블리크)
                : StepTowards(s[i - 1], fallbackEnd, 1);
            s[i] = desired;
        }
    }

    // G(기타 종지): 모호함을 보완—서브는 작게 진동하며 후반에 1/3/5 중 하나로 ‘견인’
    private static void Generate_G(int[] m, int[] s, string subType)
    {
        int anchor = (subType.StartsWith("G01") || subType.StartsWith("G02")) ? 5 : 3;
        for (int i = 1; i < 8; i++)
        {
            int dir = Math.Sign(m[i] - m[i - 1]);
            // WAVE/LEAP일수록 서브는 작은 순차(±1) 선호
            int desired = Clamp18(s[i - 1] + (i % 2 == 0 ? -dir : 0));
            if (i >= 6) desired = StepTowards(s[i - 1], anchor, 1);
            s[i] = desired;
        }
    }

    // C(페달/반복): 서브는 상성 페달(원선율 1 페달이면 5 페달) + 가끔 이웃음
    private static void Generate_C(int[] m, int[] s, string subType)
    {
        int pedal = (subType == "C01" || subType == "C02") ? 5 :
                    (subType == "C03" || subType == "C04") ? 1 : 5;
        for (int i = 1; i < 8; i++)
        {
            // 3,5,7박에 이웃음 살짝
            if (i % 2 == 1 && UnityEngine.Random.Range(0, 3) == 0)
                s[i] = Clamp18(pedal + (UnityEngine.Random.Range(0, 2) == 0 ? +1 : -1));
            else s[i] = pedal;
        }
        s[7] = pedal;
    }

    // D(시퀀스): 서브는 3도/6도 병진행이 아닌, 평행 모티브를 '한도 아래'로 따라가기
    private static void Generate_D(int[] m, int[] s, string subType)
    {
        // 2음 모티브 추출 후 -1 평행 이동
        for (int i = 1; i < 8; i++)
        {
            int mot = m[i] - m[i - 1];
            int desired = Clamp18(s[i - 1] + mot);
            // 완전병행 방지: 완전 간격 반복이면 한 칸 보정
            if (IsPerfect(m[i], desired) && IsPerfect(m[i - 1], s[i - 1]))
                desired = Clamp18(desired + (mot > 0 ? -1 : +1));
            s[i] = desired;
        }
        // 종지는 원선율 종지 보완(토닉/3/5)로 살짝 끌어당김
        int endAnchor = (m[7] == 1 || m[7] == 8) ? 3 : 1;
        s[7] = StepTowards(s[7], endAnchor, 1);
    }

    // E(아르페지오/도약): 서브는 보완 화성(원선율 I이면 V, V이면 I) 아르페지오
    private static void Generate_E(int[] m, int[] s, string subType)
    {
        // 상보 코드톤 집합
        int[] I = { 1, 3, 5 };
        int[] V = { 5, 7, 2 }; // 2는 경과역할 포함
        bool useI = subType is "E04" or "E05" or "E07" or "E09" or "E12"; // 원이 V/도약이면 서브는 I
        var pool = new List<int>(useI ? I : V);

        s[1] = NearestFromPool(s[0], pool);
        for (int i = 2; i < 8; i++)
        {
            // 아르페지오 상·하 진행 (원선율과 반진행 지향)
            int dir = Math.Sign(m[i] - m[i - 1]);
            int next = NextChordTone(s[i - 1], pool, preferUp: dir < 0);
            s[i] = next;
        }
        // 종지는 I(1/3/5) 중 하나(원선율이 1/8이면 3/5, 아니면 1)
        int endTarget = (m[7] == 1 || m[7] == 8) ? (UnityEngine.Random.Range(0, 2) == 0 ? 3 : 5) : 1;
        s[7] = StepTowards(s[7], endTarget, 2);
    }

    // F(대칭/지그재그/이웃음): 서브는 '미러/시차' 전략
    private static void Generate_F(int[] m, int[] s, string subType)
    {
        if (subType == "F01" || subType == "F02")
        {
            // 팔린드롬 미러: s[i] = 9 - s[7-i] (대칭 중심 = 4.5 근사)
            s[7] = Mirror(s[0]);
            for (int i = 1; i < 7; i++) s[i] = Mirror(s[7 - i]);
        }
        else if (subType.StartsWith("F0") && (subType == "F03" || subType == "F04" || subType == "F05" || subType == "F06"))
        {
            // 지그재그: 반진행+유지 섞기
            for (int i = 1; i < 8; i++)
            {
                int dir = Math.Sign(m[i] - m[i - 1]);
                s[i] = (i % 2 == 0) ? Clamp18(s[i - 1] - dir) : s[i - 1];
            }
        }
        else if (subType == "F07" || subType == "F08")
        {
            // 이웃음: 중심음 잡고 x-(x±1)-x-(x±1)…
            int center = PreferSafeCenter(m);
            for (int i = 1; i < 8; i++)
            {
                s[i] = (i % 2 == 1) ? Clamp18(center + ((i / 2) % 2 == 0 ? +1 : -1)) : center;
            }
        }
        else
        {
            // 스파이크/웨이브타이트: 한 번 큰 도약 + 잔진동
            int spikeAt = 3;
            for (int i = 1; i < 8; i++)
            {
                if (i == spikeAt) s[i] = Clamp18(s[i - 1] + ((UnityEngine.Random.Range(0, 2) == 0) ? +3 : -3));
                else s[i] = Clamp18(s[i - 1] + ((i % 2 == 0) ? +1 : -1));
            }
        }
    }

    // ---------- 유틸/보조 ----------

    private static int StartIntervalSafe(int main)
    {
        // 3도 위/아래 선호, 범위 넘치면 6도 대체
        int[] tries = { main + 2, main - 2, main + 5, main - 5 };
        foreach (var t in tries)
        {
            int v = Clamp18(t);
            if (!IsPerfect(main, v)) return v;
        }
        return Clamp18(main + 2);
    }

    private static int Mirror(int v) => Clamp18(9 - v); // 1<->8, 2<->7, ...

    private static int NearestFromPool(int cur, IList<int> pool)
    {
        int best = pool[0]; int bestD = 100;
        foreach (var p in pool)
        {
            int d = Math.Abs(p - cur);
            if (d < bestD) { bestD = d; best = p; }
        }
        return best;
    }

    private static int NextChordTone(int cur, IList<int> pool, bool preferUp)
    {
        // pool 안에서 한 칸(±1) 또는 2칸(±2) 이동해 가장 가까운 코드톤 선택
        int step = preferUp ? +1 : -1;
        int cand1 = Clamp18(cur + step);
        if (pool.Contains(cand1)) return cand1;
        int cand2 = Clamp18(cur + 2 * step);
        if (pool.Contains(cand2)) return cand2;
        return NearestFromPool(cur, pool);
    }

    private static int PreferSafeCenter(int[] m)
    {
        // 원선율 중앙 근처(3~6)에서 완전 간격 덜 나는 쪽
        int[] candidates = { 4, 5, 3, 6 };
        foreach (var c in candidates)
        {
            bool ok = true;
            for (int i = 0; i < 8; i++) if (IsPerfect(m[i], c)) { ok = false; break; }
            if (ok) return c;
        }
        return 4;
    }

}
    public static int GenerateOneHarmonyNote(int firstNote, int secondNote)
    {
        // 도=0, 레=1, ..., 시=6
        Dictionary<int, int[]> triads = new Dictionary<int, int[]>()
    {
        { 0, new int[] {0, 2, 4} }, // C (도, 미, 솔)
        { 1, new int[] {1, 3, 5} }, // Dm (레, 파, 라)
        { 2, new int[] {2, 4, 6} }, // Em (미, 솔, 시)
        { 3, new int[] {3, 5, 0} }, // F (파, 라, 도)
        { 4, new int[] {4, 6, 1} }, // G (솔, 시, 레)
        { 5, new int[] {5, 0, 2} }, // Am (라, 도, 미)
        { 6, new int[] {6, 1, 3} }, // Bdim (시, 레, 파)
    };

        int root = firstNote % 7;
        int[] baseTriad = triads[root];
        int third = baseTriad[1]; // 3도
        int fifth = baseTriad[2]; // 5도

        // 두 번째 음이 화음 안에 포함돼 있다면 → 남은 음 반환
        if (Array.IndexOf(baseTriad, secondNote % 7) >= 0)
        {
            foreach (var n in baseTriad)
            {
                if (n != root && n != secondNote % 7)
                    return n;
            }
        }

        // 포함되지 않았다면 → 항상 3도 반환
        return third;
    }

}
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// MelodyClassifier96
// ------------------------------------------------------------
// 8음(1..8) * 8박 멜로디를 96종 규칙으로 분류합니다.
// - Family: "A"(토닉끝 1/8), "B"(개방끝 5/3), "C"(반복/페달/패턴), "D"(시퀀스),
//           "E"(아르페지오/도약), "F"(구조/대칭/지그재그/이웃음/스파이크), "G"(기타 끝 2/4/6/7)
// - Contour: ASC, DESC, ARCH, VALLEY, WAVE, FLAT
// - Movement: STEP(순차≥70%), LEAP(도약≥50%), MIX(그 사이)
// - RangeBand: NARROW(≤3), MEDIUM(4–5), WIDE(≥6)
//
// 사용법:
//   var result = MelodyClassifier96.Classify(new int[]{1,2,3,4,5,6,7,8}, useSoftBalancer:true);
//   Debug.Log($"Subtype={result.Subtype} Family={result.Family} Code={result.Code}");
// ------------------------------------------------------------

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

// ============================================================================
// 서브 멜로디 생성기 (메인과 어울리는 2개 라인 예시)
// ============================================================================
public static class SubMelodyGenerator
{
    public static (int[] sub1, int[] sub2) GenerateSubs(int[] main)
    {
        var res = MelodyClassifier96.Classify(main, useSoftBalancer: true);
        string fam = res.Family;

        int[] sub1 = new int[8];
        int[] sub2 = new int[8];

        for (int i = 0; i < 8; i++)
        {
            int note = main[i];
            switch (fam)
            {
                case "A":
                case "B":
                case "G":
                    // 3도/5도 보강
                    sub1[i] = Mathf.Clamp(note + 2, 1, 8);
                    sub2[i] = Mathf.Clamp(note - 2, 1, 8);
                    break;

                case "C":
                    // 페달 1/5
                    sub1[i] = 1;
                    sub2[i] = 5;
                    break;

                case "D":
                    // 평행 이동
                    sub1[i] = Mathf.Clamp(note + 1, 1, 8);
                    sub2[i] = Mathf.Clamp(note - 1, 1, 8);
                    break;

                case "E":
                    // 반진행 + I보강
                    sub1[i] = Mathf.Clamp(9 - note, 1, 8);
                    sub2[i] = (i % 2 == 0) ? 1 : 5;
                    break;

                case "F":
                    // 역순 + 반전
                    sub1[i] = main[7 - i];
                    sub2[i] = Mathf.Clamp(9 - note, 1, 8);
                    break;
            }
        }

        return (sub1, sub2);
    }
}

// ============================================================================
// 테스트 컴포넌트: 씬에 붙여 콘솔에서 분포/생성 결과 확인
// ============================================================================
public class MelodyClassifier96Tester : MonoBehaviour
{
    [Range(1000, 200000)] public int sampleCount = 100000;
    public bool useSoftBalancer = true;
    public int randomSeed = 42;

    void Start()
    {
        UnityEngine.Random.InitState(randomSeed);
        MelodyClassifier96.SoftBalancer.Reset(randomSeed);

        var counts = new Dictionary<string, int>();
        foreach (var lab in MelodyClassifier96.AllLabels) counts[lab] = 0;

        // 샘플 분포 확인
        for (int t = 0; t < sampleCount; t++)
        {
            var mel = new int[8];
            for (int i = 0; i < 8; i++) mel[i] = UnityEngine.Random.Range(1, 9);
            var res = MelodyClassifier96.Classify(mel, useSoftBalancer);
            if (!counts.ContainsKey(res.Subtype)) counts[res.Subtype] = 0;
            counts[res.Subtype]++;
        }

        int over = 0, under = 0;
        foreach (var lab in MelodyClassifier96.AllLabels)
        {
            int c = counts.TryGetValue(lab, out var v) ? v : 0;
            float p = 100f * c / sampleCount;
            if (p > 5f) over++;
            if (p < 1f) under++;
        }
        Debug.Log($"Labels >5% : {over}, Labels <1% : {under}");

        // 서브 멜로디 생성 예시
        int[] yourNotes = new int[] { 1, 2, 3, 4, 5, 6, 7, 8 }; // 도레미파솔라시도
        var result = MelodyClassifier96.Classify(yourNotes, useSoftBalancer: true);
        var (sub1, sub2) = SubMelodyGenerator.GenerateSubs(yourNotes);

        Debug.Log($"Main  : {string.Join(",", yourNotes)}");
        Debug.Log($"Class : {result.Subtype} ({result.Family})  Code={result.Code}");
        Debug.Log($"Sub1  : {string.Join(",", sub1)}");
        Debug.Log($"Sub2  : {string.Join(",", sub2)}");
    }
}

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
    // ====== 결과 타입 ======
    public sealed class Result
    {
        public string Family;    // A..G
        public string Subtype;   // A01..G20
        public string Code;      // 예: "A01-ASC-STEP-NARROW-END1"
        public Feats Features;   // 디버그/후처리용
    }

    // ====== 특징 ======
    public sealed class Feats
    {
        public int[] Notes;      // 원본
        public int[] Steps;      // 7개
        public int[] Dir;        // -1/0/+1, 7개
        public int End;          // 마지막 음
        public int Range;        // max-min
        public string RangeBand; // NARROW/MEDIUM/WIDE
        public float StepRatio;  // |step|<=1 비율
        public float LeapRatio;  // |step|>=2 비율
        public string Movement;  // STEP/LEAP/MIX
        public int SignChanges;  // 0 제외한 부호전환 수
        public string Contour;   // ASC/DESC/ARCH/VALLEY/WAVE/FLAT
        public bool Has71;       // 7->1 존재
        public bool Has43;       // 4->3 존재
        public int AltCount;     // ±1 교대 횟수(미세 왕복)
        public int ZeroHolds;    // 동일음 유지 수
        public int MaxLeap;      // 최대 도약 크기
    }

    // ====== 파라미터(임계값) ======
    const float STEP_RATIO_THRESH = 0.70f;
    const float LEAP_RATIO_THRESH = 0.50f;  // E(도약) 진입 기준
    const int PEDAL_MIN = 4;
    const int ARCH_VALLEY_TURNS = 1;

    // ====== 퍼블릭 API ======
    public static Result Classify(int[] notes)
    {
        if (notes == null || notes.Length != 8) throw new ArgumentException("notes must be length 8");
        if (notes.Any(n => n < 1 || n > 8)) throw new ArgumentException("each note must be 1..8");

        var f = Extract(notes);

        // 결정 순서: C -> D -> E -> F -> ABG (균등화 없음, 항상 종료)
        string sub;

        sub = TryC(notes, f); if (sub != null) return Pack("C", sub, f);
        sub = TryD(notes, f); if (sub != null) return Pack("D", sub, f);
        sub = TryE(notes, f); if (sub != null) return Pack("E", sub, f);
        sub = TryF(notes, f); if (sub != null) return Pack("F", sub, f);

        // A/B/G 종지 기반 분기
        var fam = FamilyForEnd(f.End);
        sub = fam switch
        {
            "A" => SubA(f),
            "B" => SubB(f),
            _ => SubG(f),
        };
        return Pack(fam, sub, f);
    }

    // ====== 특징 추출 ======
    private static Feats Extract(int[] n)
    {
        var steps = new int[7];
        var dir = new int[7];
        int zeros = 0;
        for (int i = 0; i < 7; i++)
        {
            steps[i] = n[i + 1] - n[i];
            dir[i] = steps[i] == 0 ? 0 : (steps[i] > 0 ? 1 : -1);
            if (steps[i] == 0) zeros++;
        }
        // 부호 전환
        int sc = 0, last = 0;
        for (int i = 0; i < 7; i++)
        {
            if (dir[i] == 0) continue;
            if (last != 0 && dir[i] != last) sc++;
            last = dir[i];
        }
        int max = n.Max(), min = n.Min();
        int range = max - min;
        string rangeBand = range <= 3 ? "NARROW" : (range <= 5 ? "MEDIUM" : "WIDE");
        int stepCount = steps.Count(s => Math.Abs(s) <= 1);
        int leapCount = steps.Count(s => Math.Abs(s) >= 2);
        float stepR = stepCount / 7f, leapR = leapCount / 7f;
        string movement = stepR >= STEP_RATIO_THRESH ? "STEP" : (leapR >= LEAP_RATIO_THRESH ? "LEAP" : "MIX");

        // 컨투어
        bool anyUp = dir.Any(d => d > 0), anyDn = dir.Any(d => d < 0), allZero = dir.All(d => d == 0);
        string contour;
        if (allZero) contour = "FLAT";
        else if (anyUp && !anyDn) contour = "ASC";
        else if (!anyUp && anyDn) contour = "DESC";
        else if (sc == ARCH_VALLEY_TURNS) contour = dir.First(d => d != 0) > 0 ? "ARCH" : "VALLEY";
        else contour = "WAVE";

        // 해결 경향
        bool has71 = false, has43 = false;
        for (int i = 0; i < 7; i++)
        {
            if (n[i] == 7 && n[i + 1] == 1) has71 = true;
            if (n[i] == 4 && n[i + 1] == 3) has43 = true;
        }

        // 미세 왕복(±1 교대)
        int alt = 0, prev = 0;
        for (int i = 0; i < 7; i++)
        {
            if (Math.Abs(dir[i]) == 1 && prev == -dir[i]) alt++;
            if (dir[i] != 0) prev = dir[i];
        }

        int maxLeap = steps.Max(s => Math.Abs(s));

        return new Feats
        {
            Notes = (int[])n.Clone(),
            Steps = steps,
            Dir = dir,
            End = n[7],
            Range = range,
            RangeBand = rangeBand,
            StepRatio = stepR,
            LeapRatio = leapR,
            Movement = movement,
            SignChanges = sc,
            Contour = contour,
            Has71 = has71,
            Has43 = has43,
            AltCount = alt,
            ZeroHolds = zeros,
            MaxLeap = maxLeap
        };
    }

    private static Result Pack(string fam, string sub, Feats f)
    {
        string code = $"{sub}-{f.Contour}-{f.Movement}-{f.RangeBand}-END{f.End}";
        return new Result { Family = fam, Subtype = sub, Code = code, Features = f };
    }

    // ====== C: 페달/반복/점층 ======
    private static string TryC(int[] n, Feats f)
    {
        int c1 = n.Count(x => x == 1), c5 = n.Count(x => x == 5);
        if (c1 >= 5) return "C01";
        if (c1 == 4) return "C02";
        if (c5 >= 5) return "C03";
        if (c5 == 4) return "C04";

        // AABA
        int diffAABA = 0; for (int i = 0; i < 4; i++) diffAABA += Math.Abs(n[i] - n[i + 4]);
        if (diffAABA <= 1) return "C05";
        if (diffAABA <= 2) return "C06";

        // ABAB
        bool ababStrict = (n[0] == n[2] && n[1] == n[3] && n[2] == n[4] && n[3] == n[5] && n[4] == n[6] && n[5] == n[7] && !(n[0] == n[4] && n[1] == n[5]));
        if (ababStrict) return "C07";
        bool ababLoose = (n[0] == n[2] && n[1] == n[3] && n[4] == n[6] && n[5] == n[7] && !(n[0] == n[4] && n[1] == n[5]));
        if (ababLoose) return "C08";

        // AAAB
        int diffAAAB = 0; for (int i = 0; i < 6; i++) diffAAAB += Math.Abs(n[i] - n[Mathf.Min(i + 1, 6)]);
        if (diffAAAB <= 2) return "C09";
        if (diffAAAB <= 3) return "C10";

        // GRAD (블록 평균 등차 ±1 ×3, 내부 간격 <=2, WAVE 금지)
        float[] mu = { 0.5f * (n[0] + n[1]), 0.5f * (n[2] + n[3]), 0.5f * (n[4] + n[5]), 0.5f * (n[6] + n[7]) };
        float d1 = mu[1] - mu[0], d2 = mu[2] - mu[1], d3 = mu[3] - mu[2];
        bool innerOK = Math.Abs(n[1] - n[0]) <= 2 && Math.Abs(n[3] - n[2]) <= 2 && Math.Abs(n[5] - n[4]) <= 2 && Math.Abs(n[7] - n[6]) <= 2;
        if (innerOK && Mathf.Abs(d1) == 1f && d2 == d1 && d3 == d1 && f.Contour != "WAVE")
            return d1 > 0 ? "C11" : "C12";

        return null;
    }

    // ====== D: 시퀀스 ======
    private static string TryD(int[] n, Feats f)
    {
        // 2음 시퀀스 엄격
        int d12 = n[2] - n[0], d34 = n[4] - n[2], d56 = n[6] - n[4];
        if (Mathf.Abs(d12) == 1 && d34 == d12 && d56 == d12)
            return d12 > 0 ? "D01" : "D02";

        // 3음 시퀀스 엄격 (앞/뒤 블록)
        int d = n[3] - n[0];
        if (Mathf.Abs(d) == 1 && (n[4] - n[1]) == d && (n[5] - n[2]) == d)
            return d > 0 ? "D05" : "D06";
        int d2 = n[5] - n[2];
        if (Mathf.Abs(d2) == 1 && (n[6] - n[3]) == d2 && (n[7] - n[4]) == d2)
            return d2 > 0 ? "D05" : "D06";

        // 느슨(한 블록 허용)
        bool seq2Loose = Mathf.Abs(d12) == 1 && ((d34 == d12 && Mathf.Abs(d56 - d12) <= 1) || (Mathf.Abs(d34 - d12) <= 1 && d56 == d12));
        if (seq2Loose) return d12 > 0 ? "D03" : "D04";

        bool seq3Loose = Mathf.Abs(d) == 1 && (n[4] - n[1]) == d && Mathf.Abs((n[5] - n[2]) - d) <= 1;
        if (seq3Loose) return d > 0 ? "D07" : "D08";

        // 교차/절단 (간단 휴리스틱)
        if (f.Contour == "ARCH" && f.Movement != "FLAT") return "D09";
        if ((f.End == 1 || f.End == 8 || f.End == 5 || f.End == 3) && !(n[0] == n[1] && n[1] == n[2])) return "D10";

        return null;
    }

    // ====== E: 아르페지오/도약 ======
    private static string TryE(int[] n, Feats f)
    {
        // 강한 도약 런: 3연속 |step|>=2 모두 + 그중 2개 이상 |step|>=3, 부호 동일
        for (int i = 0; i <= 4; i++)
        {
            int s1 = n[i + 1] - n[i], s2 = n[i + 2] - n[i + 1], s3 = n[i + 3] - n[i + 2];
            if (s1 == 0 || s2 == 0 || s3 == 0) continue;
            bool sameSign = (s1 > 0 && s2 > 0 && s3 > 0) || (s1 < 0 && s2 < 0 && s3 < 0);
            int leaps2 = 0; if (Mathf.Abs(s1) >= 2) leaps2++; if (Mathf.Abs(s2) >= 2) leaps2++; if (Mathf.Abs(s3) >= 2) leaps2++;
            int leaps3 = 0; if (Mathf.Abs(s1) >= 3) leaps3++; if (Mathf.Abs(s2) >= 3) leaps3++; if (Mathf.Abs(s3) >= 3) leaps3++;
            if (sameSign && leaps2 == 3 && leaps3 >= 2)
            {
                bool up = s1 > 0;
                bool hasI = n.Contains(1) && n.Contains(3) && n.Contains(5);
                bool hasV = n.Contains(5) && n.Contains(7) && n.Contains(2);
                int maxjump = Math.Max(Math.Abs(s1), Math.Max(Math.Abs(s2), Math.Abs(s3)));
                if (hasI) return up ? "E01" : "E02";
                if (hasV) return up ? "E04" : "E05";
                if (up) return maxjump >= 5 ? "E07" : "E06";
                else return maxjump >= 5 ? "E09" : "E08";
            }
        }
        // I-arp + 채움
        if (n.Contains(1) && n.Contains(3) && n.Contains(5) && f.StepRatio >= 0.5f) return "E03";
        // 상행 도약 후 하강
        int leapsUp = 0; for (int i = 0; i < 5; i++) if (n[i + 1] - n[i] >= 2) leapsUp++;
        int tail = (n[5] - n[6]) + (n[6] - n[7]);
        if (leapsUp >= 2 && (tail < 0 || n[7] < n[0])) return "E10";
        // 혼합 도약(한두 개 채움 포함, 부호 유지)
        for (int i = 0; i < 4; i++)
        {
            int s1 = n[i + 1] - n[i], s2 = n[i + 2] - n[i + 1];
            if (s1 * s2 > 0 && (Math.Abs(s1) >= 2 || Math.Abs(s2) >= 2))
                return s1 > 0 ? "E11" : "E12";
        }
        return null;
    }

    // ====== F: 구조/대칭/이웃음/지그재그/스파이크 ======
    private static string TryF(int[] n, Feats f)
    {
        int diff = 0; for (int i = 0; i < 4; i++) diff += Math.Abs(n[i] - n[7 - i]);
        if (diff <= 2) return "F01";
        if (diff <= 3) return "F02";

        if (f.SignChanges >= 2 && f.RangeBand == "NARROW") return "F03";
        if (f.SignChanges >= 3 && f.RangeBand == "NARROW") return "F04";
        if (f.SignChanges >= 2 && f.RangeBand == "MEDIUM") return "F05";
        if (f.SignChanges >= 3 && f.RangeBand == "MEDIUM") return "F06";

        // 이웃음 왕복
        int neigh = 0;
        for (int i = 0; i <= 4; i++)
        {
            int a = n[i], b = n[i + 1], c = n[i + 2], d = n[i + 3];
            if (Math.Abs(b - a) == 1 && c == a && Math.Abs(d - c) == 1) neigh++;
        }
        if (neigh >= 3) return "F08";
        if (neigh >= 2) return "F07";

        // 스파이크
        for (int i = 0; i < 7; i++)
        {
            int leap = Math.Abs(n[i + 1] - n[i]);
            if (leap >= 7) return "F10";
        }
        int spike = -1;
        for (int i = 0; i < 7; i++)
        {
            if (Math.Abs(n[i + 1] - n[i]) >= 6) { spike = i; break; }
        }
        if (spike >= 0)
        {
            var rest = n.ToList(); rest.RemoveAt(spike + 1);
            if (rest.Max() - rest.Min() <= 3) return "F09";
        }

        // 중심 드론
        var count = new Dictionary<int, int>();
        foreach (var x in n) { if (!count.ContainsKey(x)) count[x] = 0; count[x]++; }
        var top = count.OrderByDescending(kv => kv.Value).First();
        if (top.Value >= 4)
        {
            int around = 0;
            for (int i = 0; i < 7; i++)
            {
                if ((n[i] == top.Key && Math.Abs(n[i + 1] - top.Key) == 1) ||
                    (n[i + 1] == top.Key && Math.Abs(n[i] - top.Key) == 1)) around++;
            }
            if (around >= 3) return "F11";
        }

        if (f.SignChanges >= 3 && f.RangeBand == "NARROW" && f.Contour == "WAVE") return "F12";
        return null;
    }

    // ====== A/B/G 내부 세분 ======
    private static string FamilyForEnd(int end)
    {
        if (end == 1 || end == 8) return "A";
        if (end == 5 || end == 3) return "B";
        return "G";
    }

    private static string SubA(Feats f)
    {
        if (f.Has71) return "A16";
        if (f.Has43) return "A17";
        int tail = f.Steps[5] + f.Steps[6];
        if ((f.End == 1 || f.End == 8) && tail < 0) return "A18";
        if (f.RangeBand == "WIDE" && f.Movement != "STEP") return "A13";

        if (f.End == 1)
        {
            if (f.Contour == "ASC" && f.Movement == "STEP") return f.RangeBand == "NARROW" ? "A01" : "A02";
            if (f.Contour == "DESC" && f.Movement == "STEP") return "A03";
            if (f.Contour == "ARCH" && f.Movement == "STEP") return "A04";
            if (f.Contour == "VALLEY" && f.Movement == "STEP") return "A05";
            if (f.Contour == "FLAT") return "A07";
            if (f.Movement == "LEAP") return "A14";
            return "A06";
        }
        else
        { // END8
            if (f.Contour == "ASC" && f.Movement == "STEP") return "A08";
            if (f.Contour == "DESC" && f.Movement == "STEP") return "A09";
            if (f.Contour == "ARCH" && f.Movement != "STEP") return "A10";
            if (f.Contour == "VALLEY" && f.Movement != "STEP") return "A11";
            if (f.Contour == "WAVE" && f.RangeBand == "MEDIUM") return "A12";
            if (f.Movement == "LEAP") return "A15";
            return "A06";
        }
    }

    private static string SubB(Feats f)
    {
        if (f.End == 5)
        {
            if (f.Contour == "ASC" && f.Movement == "STEP") return "B01";
            if (f.Contour == "DESC" && f.Movement == "STEP") return "B02";
            if (f.Contour == "ARCH" && f.Movement != "STEP") return "B03";
            if (f.Contour == "VALLEY" && f.Movement != "STEP") return "B04";
            if (f.Contour == "WAVE" && f.RangeBand != "WIDE") return "B05";
            if (f.Contour == "FLAT") return "B06";
            if (f.Movement == "LEAP") return "B07";
            if (f.RangeBand == "WIDE") return "B08";
            return "B05";
        }
        else
        { // END3
            if (f.Contour == "ASC" && f.Movement == "STEP") return "B09";
            if (f.Contour == "DESC" && f.Movement == "STEP") return "B10";
            if (f.Contour == "WAVE" && f.RangeBand != "WIDE") return "B11";
            if (f.RangeBand == "WIDE") return "B12";
            if (f.Movement == "LEAP") return "B07";
            return "B11";
        }
    }

    private static string SubG(Feats f)
    {
        if (f.Contour == "FLAT") return "G11";
        if (f.Movement == "LEAP")
        {
            if (f.RangeBand == "NARROW") return "G12";
            if (f.RangeBand == "MEDIUM") return "G13";
            return "G14";
        }
        if (f.Contour == "ASC" && f.Movement == "STEP" && f.RangeBand == "NARROW") return "G01";
        if (f.Contour == "DESC" && f.Movement == "STEP" && f.RangeBand == "NARROW") return "G02";
        if (f.Contour == "ARCH") return "G03";
        if (f.Contour == "VALLEY") return "G04";
        if (f.Contour == "WAVE")
        {
            if (f.RangeBand == "NARROW") return f.SignChanges >= 3 ? "G06" : "G05";
            if (f.RangeBand == "MEDIUM") return f.SignChanges >= 3 ? "G08" : "G07";
            return f.SignChanges >= 3 ? "G10" : "G09";
        }
        if (f.AltCount >= 4 && f.StepRatio >= 0.5f) return "G16";
        if (f.AltCount >= 3) return "G17";
        if (f.Has71) return "G18";
        if (f.Has43) return "G19";

        // 해결 시도 흔적
        if ((f.Steps[6] < 0 && (f.End == 2 || f.End == 4 || f.End > 1)) ||
            (f.Steps[6] > 0 && (f.End == 6 || f.End == 7))) return "G20";

        return "G07";
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

}
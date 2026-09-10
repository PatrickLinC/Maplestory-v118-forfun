using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

class PeRef {
    static byte[] d;
    static List<uint[]> secs = new List<uint[]>(); // va, vsize, raw, rsize
    static uint imageBase;

    static long VaToOff(uint va) {
        foreach (var s in secs) if (va >= s[0] && va < s[0] + s[1]) { long o = s[2] + (va - s[0]); if (o < s[2] + s[3]) return o; }
        return -1;
    }
    static uint OffToVa(long off) {
        foreach (var s in secs) if (off >= s[2] && off < s[2] + s[3]) return s[0] + (uint)(off - s[2]);
        return 0;
    }

    static void Main(string[] a) {
        d = File.ReadAllBytes(a[0]);
        int pe = BitConverter.ToInt32(d, 0x3C);
        int numSec = BitConverter.ToUInt16(d, pe + 6);
        int optSize = BitConverter.ToUInt16(d, pe + 20);
        int optOff = pe + 24;
        imageBase = BitConverter.ToUInt32(d, optOff + 28);
        int secOff = optOff + optSize;
        var sb = new StringBuilder();
        sb.AppendLine("ImageBase=0x" + imageBase.ToString("X") + "  sections=" + numSec);
        for (int i = 0; i < numSec; i++) {
            int s = secOff + i * 40;
            string name = Encoding.ASCII.GetString(d, s, 8).TrimEnd('\0');
            uint vsize = BitConverter.ToUInt32(d, s + 8);
            uint va = BitConverter.ToUInt32(d, s + 12);
            uint rsize = BitConverter.ToUInt32(d, s + 16);
            uint raw = BitConverter.ToUInt32(d, s + 20);
            secs.Add(new uint[] { va, vsize, raw, rsize });
            sb.AppendLine(String.Format("  {0,-8} VA=0x{1:X8} VSize=0x{2:X6} Raw=0x{3:X8} RSize=0x{4:X6}", name, va + imageBase, vsize, raw, rsize));
        }

        string[] needles = a[1].Split(',');
        foreach (var raw in needles) {
            string needle = raw.Trim();
            if (needle.Length == 0) continue;
            byte[] nb = Encoding.ASCII.GetBytes(needle);
            // find null-terminated occurrences
            for (long i = 1; i < d.Length - nb.Length - 1; i++) {
                bool m = true;
                for (int k = 0; k < nb.Length; k++) if (d[i + k] != nb[k]) { m = false; break; }
                if (!m) continue;
                if (d[i - 1] != 0) continue;             // preceded by null (string start)
                if (d[i + nb.Length] != 0) continue;      // null terminated
                uint sva = OffToVa(i);
                if (sva == 0) continue;
                uint fullVa = sva + imageBase;
                // search .text for 4-byte references to fullVa
                var refs = new List<uint>();
                byte[] target = BitConverter.GetBytes(fullVa);
                foreach (var sec in secs) {
                    for (long p = sec[2]; p < sec[2] + sec[3] - 4 && p < d.Length - 4; p++) {
                        if (d[p] == target[0] && d[p + 1] == target[1] && d[p + 2] == target[2] && d[p + 3] == target[3]) {
                            refs.Add(OffToVa(p) + imageBase);
                            if (refs.Count >= 12) break;
                        }
                    }
                    if (refs.Count >= 12) break;
                }
                sb.Append(String.Format("\"{0}\" @0x{1:X8}  refs({2}): ", needle, fullVa, refs.Count));
                foreach (var r in refs) sb.Append("0x" + r.ToString("X8") + " ");
                sb.AppendLine();
                break; // first occurrence only
            }
        }
        Console.OutputEncoding = Encoding.UTF8;
        Console.WriteLine(sb.ToString());
    }
}

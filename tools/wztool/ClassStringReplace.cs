using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;

class ClassStringReplace {
    // args: <jar> dump  <outfile>
    //       <jar> apply <dictfile(English<TAB>中文 per line)>
    static void Main(string[] a) {
        string jar = a[0], mode = a[1], file = a[2];
        Dictionary<string, string> dict = null;
        if (mode == "apply") {
            dict = new Dictionary<string, string>();
            foreach (var line in File.ReadAllLines(file, Encoding.UTF8)) {
                int t = line.IndexOf('\t');
                if (t > 0) dict[line.Substring(0, t)] = line.Substring(t + 1);
            }
        }
        var freq = new Dictionary<string, int>();
        int changed = 0;
        var zmode = (mode == "apply") ? ZipArchiveMode.Update : ZipArchiveMode.Read;
        using (var zip = ZipFile.Open(jar, zmode)) {
            var entries = new List<ZipArchiveEntry>(zip.Entries);
            foreach (var e in entries) {
                if (!e.FullName.EndsWith(".class")) continue;
                byte[] data;
                using (var s = e.Open()) using (var ms = new MemoryStream()) { s.CopyTo(ms); data = ms.ToArray(); }
                bool didChange;
                byte[] nd = ProcessClass(data, mode, dict, freq, out didChange);
                if (mode == "apply" && didChange) {
                    using (var s = e.Open()) { s.SetLength(0); s.Write(nd, 0, nd.Length); }
                    changed++;
                }
            }
        }
        if (mode == "dump") {
            var list = new List<KeyValuePair<string, int>>(freq);
            list.Sort(delegate (KeyValuePair<string, int> x, KeyValuePair<string, int> y) { return y.Value - x.Value; });
            var sb = new StringBuilder();
            foreach (var kv in list) sb.AppendLine(kv.Value + "\t" + kv.Key);
            File.WriteAllText(file, sb.ToString(), new UTF8Encoding(false));
            Console.WriteLine("dumped " + freq.Count + " unique candidates -> " + file);
        } else {
            Console.WriteLine("applied to " + changed + " classes");
        }
    }

    static byte[] ProcessClass(byte[] d, string mode, Dictionary<string, string> dict, Dictionary<string, int> freq, out bool didChange) {
        didChange = false;
        if (d.Length < 10 || d[0] != 0xCA || d[1] != 0xFE || d[2] != 0xBA || d[3] != 0xBE) return d;
        int cpcount = (d[8] << 8) | d[9];
        MemoryStream outMs = (mode == "apply") ? new MemoryStream() : null;
        if (outMs != null) outMs.Write(d, 0, 10);
        int p = 10;
        for (int i = 1; i < cpcount; i++) {
            int tag = d[p];
            if (tag == 1) {
                int len = (d[p + 1] << 8) | d[p + 2];
                string s = Encoding.UTF8.GetString(d, p + 3, len);
                if (mode == "dump") {
                    if (IsCandidate(s)) { if (!freq.ContainsKey(s)) freq[s] = 0; freq[s]++; }
                } else {
                    string rep;
                    if (dict.TryGetValue(s, out rep)) {
                        byte[] rb = Encoding.UTF8.GetBytes(rep);
                        outMs.WriteByte(1); outMs.WriteByte((byte)(rb.Length >> 8)); outMs.WriteByte((byte)(rb.Length & 0xFF)); outMs.Write(rb, 0, rb.Length);
                        didChange = true;
                        p += 3 + len; continue;
                    }
                    outMs.Write(d, p, 3 + len);
                }
                p += 3 + len;
            } else {
                int size = TagSize(tag);
                if (outMs != null) outMs.Write(d, p, size);
                p += size;
                if (tag == 5 || tag == 6) i++;
            }
        }
        if (mode == "apply") { outMs.Write(d, p, d.Length - p); return outMs.ToArray(); }
        return d;
    }

    static int TagSize(int tag) {
        switch (tag) {
            case 7: case 8: case 16: case 19: case 20: return 3;
            case 15: return 4;
            case 3: case 4: case 9: case 10: case 11: case 12: case 17: case 18: return 5;
            case 5: case 6: return 9;
            default: return 1;
        }
    }

    static bool IsCandidate(string s) {
        if (s.Length < 8 || s.IndexOf(' ') < 0) return false;
        int letters = 0;
        foreach (char c in s) {
            if (c > 127) return false;
            if (char.IsLetter(c)) letters++;
        }
        if (letters < 5) return false;
        if (s.IndexOf('/') >= 0 || s.IndexOf('(') >= 0 || s.IndexOf(';') >= 0 || s.IndexOf("java.") >= 0 || s.IndexOf("Ljava") >= 0 || s.IndexOf('<') >= 0) return false;
        return true;
    }
}

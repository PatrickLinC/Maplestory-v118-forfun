using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MapleLib.WzLib;
using MapleLib.WzLib.WzProperties;

class Probe {
    static void Main(string[] a) {
        Console.OutputEncoding = Encoding.UTF8;
        string path = a[0];
        string target = a.Length > 1 ? a[1] : null;
        WzMapleVersion[] vers = { WzMapleVersion.EMS, WzMapleVersion.GMS, WzMapleVersion.BMS };
        foreach (var ver in vers) {
            WzFile f = null;
            try {
                f = new WzFile(path, ver);
                var st = f.ParseWzFile(null);
                var dirNames = f.WzDirectory.WzDirectories.Select(d => d.Name).ToList();
                var imgNames = f.WzDirectory.WzImages.Select(i => i.Name).ToList();
                var all = dirNames.Concat(imgNames).ToList();
                bool sane = all.Any(n => n == "Consume" || n == "Cash" || n == "Consume.img" || n == "Eqp.img" || n == "Etc.img");
                Console.WriteLine("[" + ver + "] status=" + st + " sane=" + sane);
                if (st.ToString() != "Success" || !sane) { f.Dispose(); continue; }
                Console.WriteLine(">>> 加密=" + ver);
                Console.WriteLine("  root dirs=[" + string.Join(",", dirNames) + "]");
                Console.WriteLine("  root imgs=[" + string.Join(",", imgNames.Take(30)) + "]");
                if (target != null) {
                    WzImage img = FindImage(f.WzDirectory, target);
                    if (img == null) Console.WriteLine("  找不到 " + target);
                    else {
                        img.ParseImage();
                        var kids = img.WzProperties.Select(p => p.Name).ToList();
                        Console.WriteLine("  " + target + " 節點數=" + kids.Count + " 前20=[" + string.Join(",", kids.Take(20)) + "]");
                        var tmpl = img.WzProperties.FirstOrDefault();
                        if (tmpl != null) { Console.WriteLine("  --- 範本 '" + tmpl.Name + "' ---"); DumpTree(tmpl, "  ", 4); }
                    }
                }
                f.Dispose();
                return;
            } catch (Exception e) {
                Console.WriteLine("[" + ver + "] EX " + e.Message);
                if (f != null) f.Dispose();
            }
        }
    }

    static WzImage FindImage(WzDirectory root, string name) {
        var img = root.GetImageByName(name);
        if (img != null) return img;
        foreach (var d in root.WzDirectories) { var i = d.GetImageByName(name); if (i != null) return i; }
        return null;
    }

    static List<WzImageProperty> Kids(WzImageProperty p) {
        var s = p as WzSubProperty; if (s != null) return s.WzProperties;
        var c = p as WzCanvasProperty; if (c != null) return c.WzProperties;
        return null;
    }

    static void DumpTree(WzImageProperty p, string ind, int depth) {
        string extra = "";
        var ip = p as WzIntProperty;
        var sp = p as WzStringProperty;
        var cp = p as WzCanvasProperty;
        if (ip != null) extra = " = " + ip.Value;
        else if (sp != null) extra = " = \"" + sp.Value + "\"";
        else if (cp != null) { var pg = cp.PngProperty; extra = pg != null ? (" [PNG " + pg.Width + "x" + pg.Height + "]") : " [canvas]"; }
        Console.WriteLine(ind + p.Name + " (" + p.GetType().Name + ")" + extra);
        if (depth <= 0) return;
        var kids = Kids(p);
        if (kids != null) foreach (var c in kids) DumpTree(c, ind + "  ", depth - 1);
    }
}

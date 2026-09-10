using System;
using System.IO;
using System.Linq;
using System.Text;
using MapleLib.WzLib;
using MapleLib.WzLib.WzProperties;

class NpcProbe {
    // a0=Npc.wz a1=String.wz a2=log
    static void Main(string[] a) {
        var sb = new StringBuilder();
        try {
            var f = new WzFile(a[0], WzMapleVersion.EMS);
            f.ParseWzFile(null);
            var img = f.WzDirectory.GetImageByName("2040030.img");
            sb.AppendLine("=== Npc.wz/2040030.img 頂層 ===");
            if (img == null) sb.AppendLine("  無此 img");
            else {
                img.ParseImage();
                foreach (var p in img.WzProperties) sb.AppendLine("  " + p.Name + " (" + p.GetType().Name + ")");
                var info = img.GetFromPath("info") as WzSubProperty;
                if (info != null) {
                    sb.AppendLine("  --- info 子節點 ---");
                    foreach (var c in info.WzProperties) {
                        string v = "";
                        var sp = c as WzStringProperty; var ip = c as WzIntProperty;
                        if (sp != null) v = " = \"" + sp.Value + "\""; else if (ip != null) v = " = " + ip.Value;
                        sb.AppendLine("    " + c.Name + " (" + c.GetType().Name + ")" + v);
                        var sub = c as WzSubProperty;
                        if (sub != null) foreach (var cc in sub.WzProperties) {
                            var csp = cc as WzStringProperty;
                            sb.AppendLine("      " + cc.Name + (csp != null ? " = \"" + csp.Value + "\"" : " (" + cc.GetType().Name + ")"));
                        }
                    }
                }
            }
            f.Dispose();

            var sf = new WzFile(a[1], WzMapleVersion.EMS);
            sf.ParseWzFile(null);
            var npcimg = sf.WzDirectory.GetImageByName("Npc.img");
            sb.AppendLine("=== String.wz/Npc.img/2040030 ===");
            if (npcimg == null) sb.AppendLine("  無 Npc.img");
            else {
                npcimg.ParseImage();
                var n = npcimg.GetFromPath("2040030") as WzSubProperty;
                if (n == null) sb.AppendLine("  無 2040030 節點");
                else foreach (var c in n.WzProperties) {
                    var sp = c as WzStringProperty;
                    sb.AppendLine("  " + c.Name + (sp != null ? " = \"" + sp.Value + "\"" : " (" + c.GetType().Name + ")"));
                }
            }
            sf.Dispose();
        } catch (Exception ex) { sb.AppendLine("EX: " + ex.ToString()); }
        File.WriteAllText(a[2], sb.ToString(), new UTF8Encoding(false));
        Console.WriteLine("done");
    }
}

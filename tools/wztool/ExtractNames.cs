using System;
using System.IO;
using System.Text;
using MapleLib.WzLib;
using MapleLib.WzLib.WzProperties;

class ExtractNames {
    // a0=String.wz a1=out.sql  ; 從 Eqp.img 抽所有裝備中文名 -> UPDATE wz_itemdata
    static void Main(string[] a) {
        Console.OutputEncoding = Encoding.UTF8;
        var f = new WzFile(a[0], WzMapleVersion.EMS);
        f.ParseWzFile(null);
        var eqp = f.WzDirectory.GetImageByName("Eqp.img");
        eqp.ParseImage();
        var sb = new StringBuilder();
        int n = 0;
        foreach (var wrap in eqp.WzProperties) {             // 外層 "Eqp"
            var wrapSub = wrap as WzSubProperty;
            if (wrapSub == null) continue;
            foreach (var cat in wrapSub.WzProperties) {       // 分類：Cap/Coat/Weapon...
                var catSub = cat as WzSubProperty;
                if (catSub == null) continue;
                foreach (var idNode in catSub.WzProperties) { // 各裝備 id
                    var nameP = idNode.GetFromPath("name") as WzStringProperty;
                    if (nameP == null) continue;
                    string nm = nameP.Value;
                    if (string.IsNullOrEmpty(nm)) continue;
                    int id;
                    if (!int.TryParse(idNode.Name, out id)) continue;
                    string esc = nm.Replace("\\", "\\\\").Replace("'", "''");
                    sb.AppendLine("UPDATE wz_itemdata SET name='" + esc + "' WHERE itemid=" + id + ";");
                    n++;
                }
            }
        }
        File.WriteAllText(a[1], sb.ToString(), new UTF8Encoding(false));
        Console.WriteLine("wrote " + n + " updates -> " + a[1]);
        f.Dispose();
    }
}

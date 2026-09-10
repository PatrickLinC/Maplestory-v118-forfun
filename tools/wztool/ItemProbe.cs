using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using MapleLib.WzLib;
using MapleLib.WzLib.WzProperties;

class ItemProbe {
    // a0=Item.wz a1=String.wz a2=itemId a3=outPng a4=log
    static void Main(string[] a) {
        var sb = new StringBuilder();
        int id = int.Parse(a[2]);
        string node = id.ToString().PadLeft(8, '0');
        string imgName = node.Substring(0, 4) + ".img";
        try {
            var f = new WzFile(a[0], WzMapleVersion.EMS);
            f.ParseWzFile(null);
            var consume = f.WzDirectory.WzDirectories.First(d => d.Name == "Consume");
            var img = consume.GetImageByName(imgName);
            sb.AppendLine("=== " + id + " /info (Consume/" + imgName + ") ===");
            if (img == null) { sb.AppendLine("  無 img " + imgName); }
            else {
                img.ParseImage();
                var info = img.GetFromPath(node + "/info") as WzSubProperty;
                if (info == null) sb.AppendLine("  節點不存在！");
                else {
                    foreach (var c in info.WzProperties) {
                        string v = "";
                        var ip = c as WzIntProperty; var spx = c as WzStringProperty; var cvp = c as WzCanvasProperty;
                        if (ip != null) v = "= " + ip.Value;
                        else if (spx != null) v = "= \"" + spx.Value + "\"";
                        else if (cvp != null) v = "[canvas " + (cvp.PngProperty != null ? cvp.PngProperty.Width + "x" + cvp.PngProperty.Height : "?") + "]";
                        sb.AppendLine("  " + c.Name + " (" + c.GetType().Name + ") " + v);
                    }
                    var ic = img.GetFromPath(node + "/info/icon") as WzCanvasProperty;
                    if (ic != null && ic.PngProperty != null) {
                        var bmp = ic.PngProperty.GetImage(false);
                        var big = new Bitmap(120, 120);
                        using (var g = Graphics.FromImage(big)) { g.Clear(Color.FromArgb(38, 40, 48)); int s = 3; g.DrawImage(bmp, (120 - bmp.Width * s) / 2, (120 - bmp.Height * s) / 2, bmp.Width * s, bmp.Height * s); }
                        big.Save(a[3], ImageFormat.Png);
                        sb.AppendLine("  icon -> " + a[3]);
                    }
                }
            }
            f.Dispose();

            var sf = new WzFile(a[1], WzMapleVersion.EMS);
            sf.ParseWzFile(null);
            var cimg = sf.WzDirectory.GetImageByName("Consume.img");
            cimg.ParseImage();
            var nm = cimg.GetFromPath(id + "/name") as WzStringProperty;
            var ds = cimg.GetFromPath(id + "/desc") as WzStringProperty;
            sb.AppendLine("=== String.wz name/desc ===");
            sb.AppendLine("  name = " + (nm != null ? nm.Value : "(無)"));
            sb.AppendLine("  desc = " + (ds != null ? ds.Value : "(無)"));
            sf.Dispose();
        } catch (Exception ex) { sb.AppendLine("EX: " + ex.ToString()); }
        File.WriteAllText(a[4], sb.ToString(), new UTF8Encoding(false));
        Console.WriteLine("done");
    }
}

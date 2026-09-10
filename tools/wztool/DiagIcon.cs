using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using MapleLib.WzLib;
using MapleLib.WzLib.WzProperties;

class DiagIcon {
    static void Main(string[] a) {
        var log = a[1];
        var sb = new StringBuilder();
        try {
            var f = new WzFile(a[0], WzMapleVersion.EMS);
            f.ParseWzFile(null);
            var etc = f.WzDirectory.WzDirectories.FirstOrDefault(d => d.Name == "Etc");
            var wi = etc.GetImageByName("0402.img");
            wi.ParseImage();
            var ic = wi.GetFromPath("04021000/info/icon") as WzCanvasProperty;
            sb.AppendLine("canvas null? " + (ic == null));
            if (ic != null) {
                sb.AppendLine("sub props:");
                foreach (var c in ic.WzProperties) sb.AppendLine("  " + c.Name + " (" + c.GetType().Name + ") = " + c.WzValue);
                sb.AppendLine("PngProperty null? " + (ic.PngProperty == null));
                // list methods available on WzCanvasProperty
                sb.AppendLine("WzCanvasProperty methods:");
                foreach (var m in typeof(WzCanvasProperty).GetMethods().Where(x => x.Name.Contains("Link") || x.Name.Contains("Image") || x.Name.Contains("Bitmap")))
                    sb.AppendLine("  " + m.Name);
                try {
                    var bmp = ic.PngProperty.GetImage(false);
                    sb.AppendLine("GetImage(false) -> " + (bmp == null ? "null" : bmp.Width + "x" + bmp.Height));
                } catch (Exception ge) { sb.AppendLine("GetImage EX: " + ge.GetType().Name + " " + ge.Message); }
            }
            f.Dispose();
        } catch (Exception ex) { sb.AppendLine("OUTER EX: " + ex.ToString()); }
        File.WriteAllText(log, sb.ToString(), new UTF8Encoding(false));
        Console.WriteLine("wrote " + log);
    }
}

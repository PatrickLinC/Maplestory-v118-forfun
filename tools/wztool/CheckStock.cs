using System;
using System.Linq;
using System.Text;
using MapleLib.WzLib;
using MapleLib.WzLib.WzProperties;

class CheckStock {
    static void Main(string[] a) {
        Console.OutputEncoding = Encoding.UTF8;
        var f = new WzFile(a[0], WzMapleVersion.EMS);
        f.ParseWzFile(null);
        var consume = f.WzDirectory.WzDirectories.FirstOrDefault(d => d.Name == "Consume");
        if (consume == null) { Console.WriteLine("no Consume dir"); return; }
        int[] ids = { 2432443,2432444,2432445,2432446,2432447,2435734,2435736,2590005,2590007,2590009 };
        foreach (int id in ids) {
            string node = id.ToString().PadLeft(8, '0');   // 2432443 -> 02432443
            string imgName = node.Substring(0,4) + ".img";  // 0243.img / 0259.img
            var img = consume.GetImageByName(imgName);
            if (img == null) { Console.WriteLine(id + " : NO img " + imgName); continue; }
            img.ParseImage();
            var n = img.GetFromPath(node);
            if (n == null) { Console.WriteLine(id + " : NODE MISSING (" + imgName + "/" + node + ")"); continue; }
            var icon = img.GetFromPath(node + "/info/icon") as WzCanvasProperty;
            bool hasIcon = icon != null && icon.PngProperty != null;
            Console.WriteLine(id + " : EXISTS  icon=" + (hasIcon ? (icon.PngProperty.Width + "x" + icon.PngProperty.Height) : "NONE"));
        }
        f.Dispose();
    }
}

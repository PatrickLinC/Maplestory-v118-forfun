using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using MapleLib.WzLib;
using MapleLib.WzLib.WzProperties;

class DumpItem {
    // args: wzPath  dir  img  node  [canvasSubPath outPng]
    // 例: Item.wz Consume 0204.img 02040000
    static void Main(string[] a) {
        Console.OutputEncoding = Encoding.UTF8;
        var ver = WzMapleVersion.EMS;
        var f = new WzFile(a[0], ver);
        f.ParseWzFile(null);
        var dir = f.WzDirectory.WzDirectories.FirstOrDefault(d => d.Name == a[1]);
        if (dir == null) { Console.WriteLine("無目錄 " + a[1]); return; }
        var img = dir.GetImageByName(a[2]);
        if (img == null) { Console.WriteLine("無 img " + a[2]); return; }
        img.ParseImage();
        var node = img.GetFromPath(a[3]);
        if (node == null) { Console.WriteLine("無節點 " + a[3]); return; }
        Console.WriteLine("=== " + a[1] + "/" + a[2] + "/" + a[3] + " ===");
        Dump(node, "  ", 5);

        if (a.Length >= 6) {
            var canvas = img.GetFromPath(a[3] + "/" + a[4]) as WzCanvasProperty;
            if (canvas != null && canvas.PngProperty != null) {
                Bitmap bmp = canvas.PngProperty.GetImage(false);
                bmp.Save(a[5], ImageFormat.Png);
                Console.WriteLine("已存圖: " + a[5] + " (" + bmp.Width + "x" + bmp.Height + ")");
            } else Console.WriteLine("無 canvas " + a[4]);
        }
        f.Dispose();
    }

    static void Dump(WzImageProperty p, string ind, int depth) {
        string extra = "";
        var ip = p as WzIntProperty;
        var sp = p as WzStringProperty;
        var cp = p as WzCanvasProperty;
        var vp = p as WzVectorProperty;
        if (ip != null) extra = " = " + ip.Value;
        else if (sp != null) extra = " = \"" + sp.Value + "\"";
        else if (vp != null) extra = " = (" + vp.X.Value + "," + vp.Y.Value + ")";
        else if (cp != null) { var pg = cp.PngProperty; extra = pg != null ? (" [PNG " + pg.Width + "x" + pg.Height + "]") : ""; }
        Console.WriteLine(ind + p.Name + " (" + p.GetType().Name + ")" + extra);
        if (depth <= 0) return;
        var sub = p as WzSubProperty; if (sub != null) foreach (var c in sub.WzProperties) Dump(c, ind + "  ", depth - 1);
        if (cp != null) foreach (var c in cp.WzProperties) Dump(c, ind + "  ", depth - 1);
    }
}

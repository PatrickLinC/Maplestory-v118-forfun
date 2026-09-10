using System;
using System.Linq;
using System.Text;
using MapleLib.WzLib;
using MapleLib.WzLib.WzProperties;

class NoOp {
    static void Main(string[] a) {
        Console.OutputEncoding = Encoding.UTF8;
        string src = a[0];
        var ver = WzMapleVersion.EMS;

        // 原封不動重存
        Console.WriteLine("開啟原檔並重存(不改任何東西)...");
        var f = new WzFile(src, ver);
        Console.WriteLine("  ParseWzFile = " + f.ParseWzFile(null));
        string outp = @"C:\MapleDev\wztool\str_test.wz"; // 非 OneDrive 路徑
        f.SaveToDisk(outp, (bool?)false, ver);
        f.Dispose();
        Console.WriteLine("  已存 " + outp);

        // 重新讀回驗證
        Console.WriteLine("重新讀回 (非OneDrive) ...");
        var g = new WzFile(outp, ver);
        var st = g.ParseWzFile(null);
        Console.WriteLine("  ParseWzFile = " + st);
        Console.WriteLine("  root imgs 數 = " + g.WzDirectory.WzImages.Count);
        var cimg = g.WzDirectory.GetImageByName("Consume.img");
        Console.WriteLine("  Consume.img = " + (cimg == null ? "null!" : "found"));
        if (cimg != null) {
            cimg.ParseImage();
            var nameP = cimg.GetFromPath("2000000/name") as WzStringProperty;
            Console.WriteLine("  2000000/name = " + (nameP == null ? "null" : nameP.Value));
        }
        g.Dispose();
        Console.WriteLine("OK 重存可讀。");
    }
}

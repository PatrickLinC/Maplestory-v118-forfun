using System;
using System.Linq;
using System.Text;
using MapleLib.WzLib;

class BroadVerify {
    static void Main(string[] a) {
        Console.OutputEncoding = Encoding.UTF8;
        var ver = WzMapleVersion.EMS;
        var f = new WzFile(a[0], ver);
        Console.WriteLine("ParseWzFile = " + f.ParseWzFile(null));
        int okImg = 0, failImg = 0;
        foreach (var d in f.WzDirectory.WzDirectories) {
            int c = d.WzImages.Count;
            int parsed = 0, err = 0;
            foreach (var im in d.WzImages) {
                try { im.ParseImage(); if (im.WzProperties != null) parsed++; }
                catch (Exception) { err++; }
            }
            okImg += parsed; failImg += err;
            Console.WriteLine("  " + d.Name + ": 圖片=" + c + " 解析OK=" + parsed + " 失敗=" + err);
        }
        Console.WriteLine("總計: OK=" + okImg + " FAIL=" + failImg);
        Console.WriteLine(failImg == 0 ? "==> Item.wz.new 完全健康" : "==> 有損壞!");
        f.Dispose();
    }
}

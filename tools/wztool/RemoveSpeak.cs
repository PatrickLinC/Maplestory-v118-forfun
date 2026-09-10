using System;
using System.Linq;
using System.Text;
using MapleLib.WzLib;
using MapleLib.WzLib.WzProperties;

class RemoveSpeak {
    // a0=Npc.wz  -> 產生 Npc.wz.new，移除 2040030 的 info/speak（閒置對話）
    static void Main(string[] a) {
        Console.OutputEncoding = Encoding.UTF8;
        var ver = WzMapleVersion.EMS;
        var f = new WzFile(a[0], ver);
        if (f.ParseWzFile(null).ToString() != "Success") { Console.WriteLine("parse fail"); return; }
        var img = f.WzDirectory.GetImageByName("2040030.img");
        img.ParseImage();
        var info = img.GetFromPath("info") as WzSubProperty;
        var speak = info != null ? info.GetFromPath("speak") : null;
        if (speak != null) { info.RemoveProperty(speak); Console.WriteLine("已移除 info/speak"); }
        else Console.WriteLine("找不到 info/speak（可能已無閒置對話）");
        img.Changed = true;
        string outp = a[0] + ".new";
        f.SaveToDisk(outp, (bool?)false, ver);
        f.Dispose();
        Console.WriteLine("saved " + outp);

        // verify
        var g = new WzFile(outp, ver);
        Console.WriteLine("verify parse = " + g.ParseWzFile(null));
        var vi = g.WzDirectory.GetImageByName("2040030.img");
        vi.ParseImage();
        var vspeak = vi.GetFromPath("info/speak");
        Console.WriteLine("verify: info/speak = " + (vspeak == null ? "已無(成功)" : "還在(失敗)"));
        g.Dispose();
    }
}

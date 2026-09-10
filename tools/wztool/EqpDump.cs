using System;
using System.Linq;
using System.Text;
using MapleLib.WzLib;
using MapleLib.WzLib.WzProperties;

class EqpDump {
    static void Main(string[] a) {
        Console.OutputEncoding = Encoding.UTF8;
        var f = new WzFile(a[0], WzMapleVersion.EMS);
        f.ParseWzFile(null);
        var eqp = f.WzDirectory.GetImageByName("Eqp.img");
        eqp.ParseImage();
        Console.WriteLine("Eqp.img 頂層 (count=" + eqp.WzProperties.Count + "):");
        int ci = 0;
        foreach (var cat in eqp.WzProperties) {
            Console.WriteLine("  [" + cat.Name + "] " + cat.GetType().Name);
            if (ci == 0) {
                var sub = cat as WzSubProperty;
                if (sub != null) {
                    int idi = 0;
                    foreach (var idn in sub.WzProperties) {
                        if (idi < 3) {
                            var nm = idn.GetFromPath("name") as WzStringProperty;
                            Console.WriteLine("      id=" + idn.Name + " (" + idn.GetType().Name + ") name=" + (nm != null ? nm.Value : "(無)"));
                        }
                        idi++;
                    }
                    Console.WriteLine("      ...共 " + sub.WzProperties.Count + " 個 id");
                }
            }
            ci++;
        }
        f.Dispose();
    }
}

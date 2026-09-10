using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using MapleLib.WzLib;
using MapleLib.WzLib.WzProperties;

class RenderPicked {
    // a0=Item.wz a1=outPng a2=comma-ids a3=log
    static void Main(string[] a) {
        var sb = new StringBuilder();
        try {
            var f = new WzFile(a[0], WzMapleVersion.EMS);
            f.ParseWzFile(null);
            var etc = f.WzDirectory.WzDirectories.First(d => d.Name == "Etc");
            var ids = a[2].Split(',').Select(s => int.Parse(s.Trim())).ToList();
            var found = new List<Tuple<int, Bitmap>>();
            foreach (int id in ids) {
                string node = id.ToString().PadLeft(8, '0');
                string img = node.Substring(0, 4) + ".img";
                var wi = etc.GetImageByName(img);
                if (wi == null) { sb.AppendLine(id + " : NO img " + img); found.Add(Tuple.Create(id, (Bitmap)null)); continue; }
                wi.ParseImage();
                var ic = wi.GetFromPath(node + "/info/icon") as WzCanvasProperty;
                if (ic == null || ic.PngProperty == null) { sb.AppendLine(id + " : NODE/icon MISSING"); found.Add(Tuple.Create(id, (Bitmap)null)); continue; }
                Bitmap bmp = null;
                try { bmp = ic.PngProperty.GetImage(false); } catch (Exception e) { sb.AppendLine(id + " : GetImage FAIL " + e.Message); }
                if (bmp != null) { found.Add(Tuple.Create(id, new Bitmap(bmp))); sb.AppendLine(id + " : OK " + bmp.Width + "x" + bmp.Height); }
                else found.Add(Tuple.Create(id, (Bitmap)null));
            }
            string[] tiers = { "1 基礎", "2 高階", "3 神聖", "4 祝福", "5 淬鍊", "6 富豪", "7 神話", "8 瓦爾", "9 瓦爾崇高", "10 瓦爾神話" };
            int cols = 5, cell = 116, rows = (found.Count + cols - 1) / cols; if (rows < 1) rows = 1;
            var sheet = new Bitmap(cols * cell, rows * cell);
            using (var g = Graphics.FromImage(sheet)) {
                g.Clear(Color.FromArgb(38, 40, 48));
                var font = SystemFonts.DefaultFont;
                for (int i = 0; i < found.Count; i++) {
                    int cx = (i % cols) * cell, cy = (i / cols) * cell;
                    var bmp = found[i].Item2;
                    if (bmp != null) { int s = 2, w = bmp.Width * s, h = bmp.Height * s; g.DrawImage(bmp, cx + (cell - w) / 2, cy + 8, w, h); }
                    else g.DrawString("(缺)", font, Brushes.Red, cx + cell / 2 - 10, cy + 30);
                    string label = (i < tiers.Length ? tiers[i] : ("#" + i)) + "\n" + found[i].Item1;
                    g.DrawString(label, font, Brushes.White, cx + 6, cy + cell - 34);
                }
            }
            sheet.Save(a[1], ImageFormat.Png);
            sb.AppendLine("saved " + a[1]);
            f.Dispose();
        } catch (Exception ex) { sb.AppendLine("EX: " + ex.ToString()); }
        File.WriteAllText(a[3], sb.ToString(), new UTF8Encoding(false));
        Console.WriteLine("done");
    }
}

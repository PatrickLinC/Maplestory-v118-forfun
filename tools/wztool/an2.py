# -*- coding: utf-8 -*-
import sys, struct
DUMP = r"C:\MapleDev\maple_dump.bin"
BASE = 0x400000
data = open(DUMP, 'rb').read()
def off2va(o): return o + BASE

def find_wide(s):
    b = s.encode('utf-16-le')
    res=[]; start=0
    while True:
        i = data.find(b, start)
        if i < 0: break
        # preceded by 00 00 (wide null) and followed by 00 00
        pre = (i>=2 and data[i-1]==0 and data[i-2]==0)
        post = (i+len(b)+1 < len(data) and data[i+len(b)]==0 and data[i+len(b)+1]==0)
        res.append((off2va(i), pre and post))
        start = i + 2
    return res

def find_refs(va, limit=40):
    t = struct.pack('<I', va); res=[]; start=0
    while len(res) < limit:
        i = data.find(t, start)
        if i < 0: break
        res.append(off2va(i)); start = i + 1
    return res

labels = sys.argv[1:] if len(sys.argv)>1 else [
    u"力量", u"敏捷", u"智力", u"幸運", u"攻擊力", u"魔法攻擊力",
    u"命中率", u"迴避率", u"移動速度", u"跳躍力", u"物理防禦力", u"魔法防禦力",
    u"可使用捲軸次數", u"裝備分類", u"裝備等級", u"專屬道具"]
for n in labels:
    locs = find_wide(n)
    if not locs:
        print(u'"%s": (not found wide)' % n); continue
    va, clean = locs[0][0], locs[0][1]
    refs = find_refs(va)
    print(u'"%s" @0x%08X (x%d)%s refs=%d: %s' % (
        n, va, len(locs), " CLEAN" if clean else "", len(refs),
        " ".join("0x%08X" % r for r in refs[:12])))

# -*- coding: utf-8 -*-
import sys, struct
DUMP = r"C:\MapleDev\maple_dump.bin"
BASE = 0x400000
data = open(DUMP, 'rb').read()
def off2va(o): return o + BASE

def find_narrow(s, enc):
    try:
        b = s.encode(enc)
    except Exception:
        return []
    res=[]; start=0
    while True:
        i = data.find(b, start)
        if i < 0: break
        pre = (i==0) or (data[i-1]==0)
        post = (i+len(b) < len(data)) and (data[i+len(b)]==0)
        if pre and post:
            res.append(off2va(i))
        start = i + 1
    return res

def find_refs(va, limit=40):
    t = struct.pack('<I', va); res=[]; start=0
    while len(res) < limit:
        i = data.find(t, start)
        if i < 0: break
        res.append(off2va(i)); start = i + 1
    return res

labels = sys.argv[1:] if len(sys.argv) > 1 else [
          u"力量", u"敏捷", u"智力", u"幸運", u"攻擊力", u"魔法攻擊力",
          u"命中率", u"迴避率", u"移動速度", u"跳躍力", u"可使用捲軸次數", u"專屬道具"]
for enc in ("big5", "gbk"):
    print("========== encoding = %s ==========" % enc)
    for n in labels:
        locs = find_narrow(n, enc)
        if not locs:
            print(u'  "%s": (none)' % n); continue
        va = locs[0]; refs = find_refs(va)
        print(u'  "%s" @0x%08X (x%d) refs=%d: %s' % (n, va, len(locs), len(refs),
              " ".join("0x%08X" % r for r in refs[:10])))

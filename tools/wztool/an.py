# -*- coding: utf-8 -*-
import sys, struct
DUMP = r"C:\MapleDev\maple_dump.bin"
BASE = 0x400000
data = open(DUMP, 'rb').read()

def off2va(off): return off + BASE
def va2off(va): return va - BASE

def find_string(s, enc='ascii'):
    b = s.encode(enc)
    res = []
    start = 0
    while True:
        i = data.find(b, start)
        if i < 0: break
        pre_ok = (i == 0) or (data[i-1] == 0)
        post_ok = (i + len(b) < len(data)) and (data[i+len(b)] == 0)
        if pre_ok and post_ok:
            res.append(off2va(i))
        start = i + 1
    return res

def find_refs(va, limit=40):
    t = struct.pack('<I', va)
    res = []
    start = 0
    while len(res) < limit:
        i = data.find(t, start)
        if i < 0: break
        res.append(off2va(i))
        start = i + 1
    return res

needles = sys.argv[1:] if len(sys.argv) > 1 else [
    "incSTR","incPAD","incDEX","incMAD","incPDD","incEVA","incMHP","incMMP",
    "reqLevel","reqJob","tuc","cash","ItemOption","nOption","option","Option",
    "grade","Grade","Potential","potential","Eqp","islot","vslot"]
for n in needles:
    locs = find_string(n)
    if not locs:
        print('"%s": (not found)' % n)
        continue
    va = locs[0]
    refs = find_refs(va)
    print('"%s" @0x%08X (x%d)  refs=%d: %s' % (n, va, len(locs), len(refs),
          " ".join("0x%08X" % r for r in refs[:12])))

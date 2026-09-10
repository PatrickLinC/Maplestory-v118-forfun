# -*- coding: utf-8 -*-
# usage: python refs.py 0xVA [0xVA ...]   -> find 4-byte LE references
import sys, struct
DUMP = r"C:\MapleDev\maple_dump.bin"
BASE = 0x400000
data = open(DUMP, 'rb').read()
def off2va(o): return o + BASE
def find_refs(va, limit=60):
    t = struct.pack('<I', va); res=[]; start=0
    while len(res) < limit:
        i = data.find(t, start)
        if i < 0: break
        res.append(off2va(i)); start = i + 1
    return res
for arg in sys.argv[1:]:
    va = int(arg, 16)
    refs = find_refs(va)
    print("0x%08X refs=%d: %s" % (va, len(refs), " ".join("0x%08X" % r for r in refs)))

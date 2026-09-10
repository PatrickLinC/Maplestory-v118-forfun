# -*- coding: utf-8 -*-
# Build call xref map by byte-scanning E8 (call rel32) across code region.
import struct, pickle, sys
DUMP = r"C:\MapleDev\maple_dump.bin"
BASE = 0x400000
data = open(DUMP, 'rb').read()
CODE_LO = 0x401000
CODE_HI = 0xD00000   # before .rsrc

calls_to = {}   # target_va -> [caller_va]
lo_off = CODE_LO - BASE
hi_off = CODE_HI - BASE
i = lo_off
n = 0
while i < hi_off - 5:
    if data[i] == 0xE8:  # call rel32
        rel = struct.unpack('<i', data[i+1:i+5])[0]
        src = i + BASE
        tgt = (src + 5 + rel) & 0xFFFFFFFF
        if CODE_LO <= tgt < CODE_HI:
            calls_to.setdefault(tgt, []).append(src)
            n += 1
    i += 1
pickle.dump(calls_to, open(r"C:\MapleDev\calls_to.pkl", "wb"))
print("calls indexed:", n, " unique targets:", len(calls_to))

# optional: query callers of given targets
for arg in sys.argv[1:]:
    va = int(arg, 16)
    # match target within a small window (function may be called at its entry)
    callers = calls_to.get(va, [])
    print("callers of 0x%08X (%d): %s" % (va, len(callers), " ".join("0x%08X" % c for c in callers[:20])))

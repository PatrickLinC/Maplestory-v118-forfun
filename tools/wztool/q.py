# -*- coding: utf-8 -*-
# usage:
#   python q.py range 0xLO 0xHI      -> list call-targets (func entries) in range w/ caller counts
#   python q.py callers 0xVA [win]   -> callers of VA (optionally match entries within win bytes below)
import sys, pickle
calls_to = pickle.load(open(r"C:\MapleDev\calls_to.pkl", "rb"))
cmd = sys.argv[1]
if cmd == "range":
    lo = int(sys.argv[2], 16); hi = int(sys.argv[3], 16)
    ks = sorted(k for k in calls_to if lo <= k < hi)
    for k in ks:
        print("0x%08X  callers=%d" % (k, len(calls_to[k])))
elif cmd == "callers":
    va = int(sys.argv[2], 16)
    win = int(sys.argv[3]) if len(sys.argv) > 3 else 0
    seen = set()
    for k in calls_to:
        if va - win <= k <= va:
            for c in calls_to[k]:
                seen.add((k, c))
    for k, c in sorted(seen):
        print("entry 0x%08X <- caller 0x%08X" % (k, c))
    print("total callers:", len(seen))

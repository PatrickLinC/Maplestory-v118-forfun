# -*- coding: utf-8 -*-
# usage: python disx.py <startVA hex> <len>
import sys
from capstone import Cs, CS_ARCH_X86, CS_MODE_32
DUMP = r"C:\MapleDev\maple_dump.bin"
BASE = 0x400000
data = open(DUMP, 'rb').read()
md = Cs(CS_ARCH_X86, CS_MODE_32)
md.skipdata = True
start = int(sys.argv[1], 16)
length = int(sys.argv[2]) if len(sys.argv) > 2 else 160
off = start - BASE
code = data[off:off+length]
for insn in md.disasm(code, start):
    b = " ".join("%02x" % c for c in insn.bytes)
    # annotate string refs (Big5) if operand looks like a data VA
    note = ""
    for tok in insn.op_str.replace(",", " ").split():
        if tok.startswith("0x"):
            try:
                v = int(tok, 16)
            except Exception:
                continue
            if 0xC00000 <= v <= 0xD40000:
                o = v - BASE
                end = data.find(b"\x00", o)
                if 0 < end - o < 40:
                    try:
                        s = data[o:end].decode("big5")
                        note = "  ; \"%s\"" % s
                    except Exception:
                        pass
    print("0x%08X  %-20s %s %s%s" % (insn.address, b, insn.mnemonic, insn.op_str, note))

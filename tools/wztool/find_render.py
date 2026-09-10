# -*- coding: utf-8 -*-
# Find the tooltip render: a function that walks entries (add reg,0x20) and reads entry text [reg+0xC]
from capstone import *
from capstone.x86 import X86_OP_MEM, X86_OP_REG, X86_OP_IMM
DUMP = r"C:\MapleDev\maple_dump.bin"
BASE = 0x400000
data = open(DUMP,'rb').read()
CODE_LO, CODE_HI = 0x401000, 0xD00000
md = Cs(CS_ARCH_X86, CS_MODE_32); md.detail=True; md.skipdata=True
code = data[CODE_LO-BASE:CODE_HI-BASE]

# pass: sliding window, look for 'add reg,0x20' with a nearby read of [samereg+0xC]
recent = []   # (addr, mnemonic, base_reg_of_memread_at_0xC)
hits = []
addr20 = {}   # reg -> addr of last 'add reg,0x20'
readC = {}    # reg -> addr of last read [reg+0xC]
import collections
window = collections.deque(maxlen=40)
for insn in md.disasm(code, CODE_LO):
    if insn.id == 0:   # skipdata (.byte) pseudo-instruction
        continue
    mn = insn.mnemonic
    # read of [reg + 0xC]
    for op in insn.operands:
        if op.type==X86_OP_MEM and op.mem.base!=0 and op.mem.index==0 and op.mem.disp==0x0C:
            readC[op.mem.base]=insn.address
    if mn=="add":
        ops=insn.operands
        if len(ops)==2 and ops[0].type==X86_OP_REG and ops[1].type==X86_OP_IMM and ops[1].imm==0x20:
            r=ops[0].reg
            if r in readC and abs(insn.address-readC[r])<0x120:
                hits.append((min(insn.address,readC[r]), insn.address, readC[r], r))
print("candidates (walk entries + read text[+0xC]):")
seen=set()
for h in hits:
    key=h[0]&~0x3FF
    if key in seen: continue
    seen.add(key)
    print("  near 0x%08X  (add+0x20 @0x%08X, read[+0xC] @0x%08X reg=%d)"%(h[0],h[1],h[2],h[3]))

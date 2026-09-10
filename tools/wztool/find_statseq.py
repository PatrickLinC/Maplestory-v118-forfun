# -*- coding: utf-8 -*-
# Find functions that read/write consecutive word[reg+disp] fields (equip stat structs).
from capstone import *
from capstone.x86 import X86_OP_MEM, X86_OP_REG
DUMP = r"C:\MapleDev\maple_dump.bin"
BASE = 0x400000
data = open(DUMP, 'rb').read()
CODE_LO, CODE_HI = 0x401000, 0xD00000
md = Cs(CS_ARCH_X86, CS_MODE_32)
md.detail = True

lo = CODE_LO - BASE
hi = CODE_HI - BASE
code = data[lo:hi]

# track per-base-register the last word-mem displacement seen; detect runs of consecutive +2
runs = []              # (start_va, base_reg, first_disp, count, kind)
cur = {}              # reg -> [last_disp, count, start_va, kind]

def is_word_mem(insn):
    for op in insn.operands:
        if op.type == X86_OP_MEM and op.size == 2 and op.mem.base != 0 and op.mem.index == 0:
            return (op.mem.base, op.mem.disp)
    return None

count = 0
for insn in md.disasm(code, CODE_LO):
    count += 1
    wm = is_word_mem(insn)
    if wm is None:
        continue
    reg, disp = wm
    # is it a mov-family touching a word mem?
    mn = insn.mnemonic
    if not (mn.startswith("mov") or mn == "add" or mn == "or"):
        # reset that reg's run on unrelated word access
        pass
    st = cur.get(reg)
    if st and disp == st[0] + 2:
        st[0] = disp; st[1] += 1
    elif st and disp == st[0]:
        pass
    else:
        cur[reg] = [disp, 1, insn.address]
        st = cur[reg]
    if st[1] >= 5:
        runs.append((st[2], reg, st[0] - (st[1]-1)*2, st[1], insn.address))

# dedupe overlapping runs, keep the longest per start region
seen = {}
for start, reg, fdisp, cnt, end in runs:
    key = start & ~0xFFF
    if key not in seen or cnt > seen[key][3]:
        seen[key] = (start, reg, fdisp, cnt, end)

print("disassembled insns:", count)
print("=== candidate stat-field sequences (>=5 consecutive word fields) ===")
for k in sorted(seen):
    s = seen[k]
    print("start=0x%08X regbase=%d firstDisp=0x%X count=%d end=0x%08X" % (s[0], s[1], s[2], s[3], s[4]))

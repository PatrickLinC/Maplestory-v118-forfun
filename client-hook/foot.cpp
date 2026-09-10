#include <windows.h>

typedef void (__thiscall *ZXCtor)(void*, const char*, int);
static ZXCtor zxctor = (ZXCtor)0x00416A78;
static DWORD g_container = 0;

static const char SEP[]  = "------------------------";
static const char TXT[]  = "ENCHANT TEST";

static void addLine(const char* text) {
    DWORD c = g_container;
    if (c < 0x00100000 || c > 0x7FF00000) return;
    DWORD count = *(DWORD*)(c + 0x20);
    if (count < 1 || count > 28) return;
    DWORD zx = 0; zxctor(&zx, text, -1); if (!zx) return;
    BYTE* e = (BYTE*)(c + 0x24 + count * 0x20);
    *(DWORD*)(e + 0x00) = 120; *(DWORD*)(e + 0x04) = 12; *(DWORD*)(e + 0x08) = 8;
    *(DWORD*)(e + 0x0C) = zx;  *(DWORD*)(e + 0x10) = 1000; *(DWORD*)(e + 0x14) = 0;
    *(DWORD*)(e + 0x18) = 0;   *(DWORD*)(e + 0x1C) = 0;
    *(DWORD*)(c + 0x20) = count + 1;
    *(DWORD*)(c + 0x08) += 16;
    DWORD w = 140; if (*(DWORD*)(c + 0x0C) < w) *(DWORD*)(c + 0x0C) = w;
}

// hook1 @ 0x9E3F16 : save the tooltip line container (ebx)
extern "C" void __cdecl SaveContainer(DWORD ebx) { g_container = ebx; }

// hook2 @ 0x662E5A : if the line just added is "已經..." (used-count), append my lines
extern "C" void __cdecl AddAfterFooter() {
    DWORD c = g_container;
    if (!c || c < 0x00100000 || c > 0x7FF00000) return;
    DWORD count = *(DWORD*)(c + 0x20);
    if (count < 1 || count > 28) return;
    DWORD zx = *(DWORD*)(c + 0x24 + (count - 1) * 0x20 + 0x0C);   // ONLY the just-added line
    if (zx < 0x00100000 || zx > 0x7FF00000) return;
    BYTE* s = (BYTE*)zx;
    if (s[0] == 0xA4 && s[1] == 0x77 && s[2] == 0xB8 && s[3] == 0x67) {  // "已經"
        addLine(SEP);
        addLine(TXT);
        g_container = 0;   // disarm for this tooltip
    }
}

static void writeJmp(DWORD at, void* tramp, int stolenLen) {
    DWORD old; VirtualProtect((void*)at, stolenLen, PAGE_EXECUTE_READWRITE, &old);
    BYTE* q = (BYTE*)at; q[0] = 0xE9; *(int*)(q + 1) = (int)((DWORD)tramp - (at + 5));
    for (int i = 5; i < stolenLen; i++) q[i] = 0x90;
    VirtualProtect((void*)at, stolenLen, old, &old);
}

static void InstallHooks() {
    // ---- hook1 : 0x9E3F16, stolen 7 bytes: 8d 4d ec c6 45 fc 02, back=0x9E3F1D ----
    BYTE* t1 = (BYTE*)VirtualAlloc(0, 64, MEM_COMMIT | MEM_RESERVE, PAGE_EXECUTE_READWRITE);
    int i = 0;
    t1[i++] = 0x60; t1[i++] = 0x9C; t1[i++] = 0x53;                 // pushad;pushfd;push ebx
    t1[i++] = 0xE8; *(int*)(t1 + i) = (int)((BYTE*)&SaveContainer - (t1 + i + 4)); i += 4;
    t1[i++] = 0x83; t1[i++] = 0xC4; t1[i++] = 0x04;                 // add esp,4
    t1[i++] = 0x9D; t1[i++] = 0x61;                                 // popfd;popad
    t1[i++] = 0x8D; t1[i++] = 0x4D; t1[i++] = 0xEC; t1[i++] = 0xC6; t1[i++] = 0x45; t1[i++] = 0xFC; t1[i++] = 0x02;
    t1[i++] = 0xE9; *(int*)(t1 + i) = (int)(0x009E3F1Du - (DWORD)(t1 + i + 4)); i += 4;
    writeJmp(0x009E3F16u, t1, 7);

    // ---- hook2 : 0x662E5A, stolen 7 bytes: 89 75 f0 80 65 fc 00, back=0x662E61 ----
    BYTE* t2 = (BYTE*)VirtualAlloc(0, 64, MEM_COMMIT | MEM_RESERVE, PAGE_EXECUTE_READWRITE);
    int j = 0;
    t2[j++] = 0x60; t2[j++] = 0x9C;                                 // pushad;pushfd
    t2[j++] = 0xE8; *(int*)(t2 + j) = (int)((BYTE*)&AddAfterFooter - (t2 + j + 4)); j += 4;
    t2[j++] = 0x9D; t2[j++] = 0x61;                                 // popfd;popad
    t2[j++] = 0x89; t2[j++] = 0x75; t2[j++] = 0xF0; t2[j++] = 0x80; t2[j++] = 0x65; t2[j++] = 0xFC; t2[j++] = 0x00;
    t2[j++] = 0xE9; *(int*)(t2 + j) = (int)(0x00662E61u - (DWORD)(t2 + j + 4)); j += 4;
    writeJmp(0x00662E5Au, t2, 7);
}

BOOL WINAPI DllMain(HINSTANCE h, DWORD r, LPVOID) {
    if (r == DLL_PROCESS_ATTACH) { DisableThreadLibraryCalls(h); InstallHooks(); }
    return TRUE;
}

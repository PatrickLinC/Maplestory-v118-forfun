#include <windows.h>

// inject right after the last tooltip line (ebx = line container)
#define HOOK_AT   0x009E3F16u
#define HOOK_BACK 0x009E3F1Du   // +7 (stolen: 8d 4d ec c6 45 fc 02)

typedef void (__thiscall *ZXCtor)(void* self, const char* s, int len);
static ZXCtor zxctor = (ZXCtor)0x00416A78;   // ZXString::assign(cstr, len)

static const char g_text[] = "ENCHANT TEST";

extern "C" void __cdecl AddMyEntry(DWORD container) {
    if (container < 0x00100000 || container > 0x7FF00000) return;
    DWORD count = *(DWORD*)(container + 0x20);
    if (count < 1 || count > 25) return;                 // sanity
    DWORD zx = 0;
    zxctor(&zx, g_text, -1);
    if (zx == 0) return;
    BYTE* e = (BYTE*)(container + 0x24 + count * 0x20);   // append at end
    *(DWORD*)(e + 0x00) = 100;    // text width (box uses this)
    *(DWORD*)(e + 0x04) = 12;     // line HEIGHT  <-- the fix
    *(DWORD*)(e + 0x08) = 8;      // type
    *(DWORD*)(e + 0x0C) = zx;     // text
    *(DWORD*)(e + 0x10) = 1000;   // color
    *(DWORD*)(e + 0x14) = 0;
    *(DWORD*)(e + 0x18) = 0;
    *(DWORD*)(e + 0x1C) = 0;
    *(DWORD*)(container + 0x20) = count + 1;
    // grow box: +0x08 = height accumulator (line height + 4), +0x0C = max width
    *(DWORD*)(container + 0x08) += (12 + 4);
    DWORD w = 100 + 0x14;
    if (*(DWORD*)(container + 0x0C) < w) *(DWORD*)(container + 0x0C) = w;
}

static void InstallHook() {
    BYTE* tr = (BYTE*)VirtualAlloc(0, 80, MEM_COMMIT | MEM_RESERVE, PAGE_EXECUTE_READWRITE);
    int i = 0;
    tr[i++] = 0x60; tr[i++] = 0x9C;                     // pushad; pushfd
    tr[i++] = 0x53;                                     // push ebx (container)
    tr[i++] = 0xE8; *(int*)(tr + i) = (int)((BYTE*)&AddMyEntry - (tr + i + 4)); i += 4;
    tr[i++] = 0x83; tr[i++] = 0xC4; tr[i++] = 0x04;     // add esp,4
    tr[i++] = 0x9D; tr[i++] = 0x61;                     // popfd; popad
    tr[i++] = 0x8D; tr[i++] = 0x4D; tr[i++] = 0xEC;                 // lea ecx,[ebp-0x14]
    tr[i++] = 0xC6; tr[i++] = 0x45; tr[i++] = 0xFC; tr[i++] = 0x02; // mov byte[ebp-4],2
    tr[i++] = 0xE9; *(int*)(tr + i) = (int)(HOOK_BACK - (DWORD)(tr + i + 4)); i += 4;

    DWORD old;
    VirtualProtect((void*)HOOK_AT, 7, PAGE_EXECUTE_READWRITE, &old);
    BYTE* q = (BYTE*)HOOK_AT;
    q[0] = 0xE9; *(int*)(q + 1) = (int)((DWORD)tr - (HOOK_AT + 5)); q[5] = 0x90; q[6] = 0x90;
    VirtualProtect((void*)HOOK_AT, 7, old, &old);
}

BOOL WINAPI DllMain(HINSTANCE h, DWORD r, LPVOID) {
    if (r == DLL_PROCESS_ATTACH) { DisableThreadLibraryCalls(h); InstallHook(); }
    return TRUE;
}

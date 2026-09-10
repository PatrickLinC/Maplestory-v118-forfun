#include <windows.h>

// ---- client addresses (base fixed at 0x400000, verified) ----
#define HOOK_AT   0x009E45C4u   // right after the equip stat block, before slots check
#define HOOK_BACK 0x009E45CAu   // HOOK_AT + 6 (after stolen bytes)
// stolen 6 bytes at HOOK_AT:  8b 06 83 78 20 00  (mov eax,[esi]; cmp dword[eax+0x20],0)

static int g_count = 0;

// cdecl handler; arg = client's ebp at the hook point
extern "C" void __cdecl OnTooltip(unsigned clientEbp) {
    void* container = *(void**)(clientEbp - 0x10);   // tooltip 'this' (ecx saved at [ebp-0x10])
    g_count++;
    char buf[256];
    int n = wsprintfA(buf, "hook fired #%d  container=%p  clientEbp=%08x\r\n",
                      g_count, container, clientEbp);
    HANDLE f = CreateFileA("C:\\MapleDev\\hook_fire.txt", GENERIC_WRITE, FILE_SHARE_READ,
                           0, CREATE_ALWAYS, FILE_ATTRIBUTE_NORMAL, 0);
    if (f != INVALID_HANDLE_VALUE) { DWORD w; WriteFile(f, buf, n, &w, 0); CloseHandle(f); }
}

static void InstallHook() {
    // build trampoline
    BYTE* tr = (BYTE*)VirtualAlloc(0, 64, MEM_COMMIT | MEM_RESERVE, PAGE_EXECUTE_READWRITE);
    int i = 0;
    tr[i++] = 0x60;                 // pushad
    tr[i++] = 0x9C;                 // pushfd
    tr[i++] = 0x55;                 // push ebp   (pass client ebp)
    tr[i++] = 0xE8;                 // call OnTooltip
    *(int*)(tr + i) = (int)((BYTE*)&OnTooltip - (tr + i + 4)); i += 4;
    tr[i++] = 0x83; tr[i++] = 0xC4; tr[i++] = 0x04;   // add esp,4
    tr[i++] = 0x9D;                 // popfd
    tr[i++] = 0x61;                 // popad
    // stolen bytes
    tr[i++] = 0x8B; tr[i++] = 0x06;                       // mov eax,[esi]
    tr[i++] = 0x83; tr[i++] = 0x78; tr[i++] = 0x20; tr[i++] = 0x00; // cmp dword[eax+0x20],0
    tr[i++] = 0xE9;                 // jmp back
    *(int*)(tr + i) = (int)(HOOK_BACK - (DWORD)(tr + i + 4)); i += 4;

    // write jmp at HOOK_AT
    DWORD old;
    VirtualProtect((void*)HOOK_AT, 6, PAGE_EXECUTE_READWRITE, &old);
    BYTE* p = (BYTE*)HOOK_AT;
    p[0] = 0xE9;
    *(int*)(p + 1) = (int)((DWORD)tr - (HOOK_AT + 5));
    p[5] = 0x90;                    // nop
    VirtualProtect((void*)HOOK_AT, 6, old, &old);

    HANDLE f = CreateFileA("C:\\MapleDev\\hook_installed.txt", GENERIC_WRITE, FILE_SHARE_READ, 0, CREATE_ALWAYS, FILE_ATTRIBUTE_NORMAL, 0);
    if (f != INVALID_HANDLE_VALUE) { char b[64]; int n = wsprintfA(b, "hook installed, tramp=%p\r\n", tr); DWORD w; WriteFile(f, b, n, &w, 0); CloseHandle(f); }
}

BOOL WINAPI DllMain(HINSTANCE h, DWORD reason, LPVOID) {
    if (reason == DLL_PROCESS_ATTACH) { DisableThreadLibraryCalls(h); InstallHook(); }
    return TRUE;
}

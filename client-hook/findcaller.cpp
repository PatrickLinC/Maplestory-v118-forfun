#include <windows.h>

// Hook entry of AddLine 0x9EB71E to log return addresses (=callers = the tooltip fn)
#define HOOK_AT   0x009EB71Eu
#define HOOK_BACK 0x009EB723u   // +5, after stolen "mov eax,0xC1D36C"

static DWORD g_seen[40];
static int   g_n = 0;

extern "C" void __cdecl LogCaller(DWORD retaddr) {
    for (int i = 0; i < g_n; i++) if (g_seen[i] == retaddr) return;
    if (g_n < 40) g_seen[g_n++] = retaddr;
    char buf[1600]; int p = 0;
    p += wsprintfA(buf + p, "distinct callers of AddLine (retaddr => call site = retaddr-5):\r\n");
    for (int i = 0; i < g_n; i++) p += wsprintfA(buf + p, "  0x%08X\r\n", g_seen[i]);
    HANDLE f = CreateFileA("C:\\MapleDev\\callers.txt", GENERIC_WRITE, FILE_SHARE_READ, 0, CREATE_ALWAYS, FILE_ATTRIBUTE_NORMAL, 0);
    if (f != INVALID_HANDLE_VALUE) { DWORD w; WriteFile(f, buf, p, &w, 0); CloseHandle(f); }
}

static void InstallHook() {
    BYTE* tr = (BYTE*)VirtualAlloc(0, 64, MEM_COMMIT | MEM_RESERVE, PAGE_EXECUTE_READWRITE);
    int i = 0;
    tr[i++] = 0x60;                                   // pushad
    tr[i++] = 0x9C;                                   // pushfd
    tr[i++] = 0x8B; tr[i++] = 0x44; tr[i++] = 0x24; tr[i++] = 0x24;  // mov eax,[esp+0x24]  (retaddr; 32+4=36=0x24)
    tr[i++] = 0x50;                                   // push eax
    tr[i++] = 0xE8;                                   // call LogCaller
    *(int*)(tr + i) = (int)((BYTE*)&LogCaller - (tr + i + 4)); i += 4;
    tr[i++] = 0x83; tr[i++] = 0xC4; tr[i++] = 0x04;   // add esp,4
    tr[i++] = 0x9D;                                   // popfd
    tr[i++] = 0x61;                                   // popad
    tr[i++] = 0xB8; tr[i++] = 0x6C; tr[i++] = 0xD3; tr[i++] = 0xC1; tr[i++] = 0x00;  // mov eax,0xC1D36C (stolen)
    tr[i++] = 0xE9;                                   // jmp HOOK_BACK
    *(int*)(tr + i) = (int)(HOOK_BACK - (DWORD)(tr + i + 4)); i += 4;

    DWORD old;
    VirtualProtect((void*)HOOK_AT, 5, PAGE_EXECUTE_READWRITE, &old);
    BYTE* q = (BYTE*)HOOK_AT;
    q[0] = 0xE9;
    *(int*)(q + 1) = (int)((DWORD)tr - (HOOK_AT + 5));
    VirtualProtect((void*)HOOK_AT, 5, old, &old);
}

BOOL WINAPI DllMain(HINSTANCE h, DWORD reason, LPVOID) {
    if (reason == DLL_PROCESS_ATTACH) { DisableThreadLibraryCalls(h); InstallHook(); }
    return TRUE;
}

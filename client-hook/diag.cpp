#include <windows.h>

#define HOOK_AT   0x009E3F16u
#define HOOK_BACK 0x009E3F1Du

static char g_log[6000];

static bool ok(DWORD v){ return v>=0x00100000 && v<0x7FF00000; }

extern "C" void __cdecl Diag(DWORD container) {
    if (!ok(container)) return;
    int p = 0;
    DWORD count = *(DWORD*)(container + 0x20);
    p += wsprintfA(g_log+p, "container=0x%08X count=%u  box+0x18=%u +0x1C=%u\r\n",
                   container, count, *(DWORD*)(container+0x18), *(DWORD*)(container+0x1C));
    if (count > 40) count = 40;
    for (DWORD i = 0; i < count; i++) {
        BYTE* e = (BYTE*)(container + 0x24 + i*0x20);
        DWORD zx = *(DWORD*)(e + 0x0C);
        DWORD type = *(DWORD*)(e + 0x08);
        DWORD color = *(DWORD*)(e + 0x10);
        DWORD f14 = *(DWORD*)(e + 0x14);
        DWORD* d = (DWORD*)e;
        p += wsprintfA(g_log+p, "[%u] +0:%d +4:%d +8:%d +C:0x%08X +10:%d +14:0x%08X +18:%d +1C:%d : ",
                       i, d[0], d[1], d[2], d[3], d[4], d[5], d[6], d[7]);
        if (ok(zx)) {
            char* s = (char*)zx;
            for (int k=0;k<24 && s[k];k++){ char c=s[k]; g_log[p++] = (c>=32||c<0)?c:'.'; }
        }
        p += wsprintfA(g_log+p, "\r\n");
    }
    HANDLE h = CreateFileA("C:\\MapleDev\\diag.txt", GENERIC_WRITE, FILE_SHARE_READ, 0, CREATE_ALWAYS, FILE_ATTRIBUTE_NORMAL, 0);
    if (h != INVALID_HANDLE_VALUE) { DWORD w; WriteFile(h, g_log, p, &w, 0); CloseHandle(h); }
}

static void InstallHook() {
    BYTE* tr=(BYTE*)VirtualAlloc(0,80,MEM_COMMIT|MEM_RESERVE,PAGE_EXECUTE_READWRITE);
    int i=0;
    tr[i++]=0x60; tr[i++]=0x9C;
    tr[i++]=0x53;                         // push ebx
    tr[i++]=0xE8; *(int*)(tr+i)=(int)((BYTE*)&Diag-(tr+i+4)); i+=4;
    tr[i++]=0x83; tr[i++]=0xC4; tr[i++]=0x04;
    tr[i++]=0x9D; tr[i++]=0x61;
    tr[i++]=0x8D;tr[i++]=0x4D;tr[i++]=0xEC; tr[i++]=0xC6;tr[i++]=0x45;tr[i++]=0xFC;tr[i++]=0x02;
    tr[i++]=0xE9; *(int*)(tr+i)=(int)(HOOK_BACK-(DWORD)(tr+i+4)); i+=4;
    DWORD old; VirtualProtect((void*)HOOK_AT,7,PAGE_EXECUTE_READWRITE,&old);
    BYTE* q=(BYTE*)HOOK_AT; q[0]=0xE9; *(int*)(q+1)=(int)((DWORD)tr-(HOOK_AT+5)); q[5]=0x90; q[6]=0x90;
    VirtualProtect((void*)HOOK_AT,7,old,&old);
}
BOOL WINAPI DllMain(HINSTANCE h,DWORD r,LPVOID){ if(r==DLL_PROCESS_ATTACH){DisableThreadLibraryCalls(h);InstallHook();} return TRUE; }

#include <windows.h>

#define HOOK_AT   0x009EB71Eu
#define HOOK_BACK 0x009EB723u

static char g_log[8000];
static int  g_len = 0;
static int  g_calls = 0;

static bool ptrOk(DWORD v){ return v >= 0x00100000 && v < 0x7FF00000; }

extern "C" void __cdecl Spy(DWORD container, DWORD arg1, DWORD retaddr) {
    if (g_calls >= 22) return;
    // only log calls coming from the equip tooltip fn range (0x9E3000-0x9E4000)
    if (retaddr < 0x009E3000 || retaddr > 0x009E4000) return;
    g_calls++;
    g_len += wsprintfA(g_log + g_len, "call#%d ret=0x%08X ecx(container)=0x%08X arg1=0x%08X\r\n",
                       g_calls, retaddr, container, arg1);
    // dump 16 bytes at arg1
    if (ptrOk(arg1)) {
        g_len += wsprintfA(g_log + g_len, "   [arg1]: ");
        BYTE* p = (BYTE*)arg1;
        for (int i=0;i<16;i++) g_len += wsprintfA(g_log+g_len, "%02x ", p[i]);
        // arg1 as ZXString* -> *(char**)arg1 = text
        DWORD sp = *(DWORD*)arg1;
        g_len += wsprintfA(g_log+g_len, "\r\n   *(arg1)=0x%08X", sp);
        if (ptrOk(sp)) {
            char* s = (char*)sp; g_len += wsprintfA(g_log+g_len, " -> \"");
            for (int i=0;i<24 && s[i];i++){ char c=s[i]; g_log[g_len++]=(c>=32||c<0)?c:'.'; }
            g_len += wsprintfA(g_log+g_len, "\"");
        }
        // arg1 itself as char*
        g_len += wsprintfA(g_log+g_len, "\r\n   arg1 as str=\"");
        char* s2=(char*)arg1; for(int i=0;i<24 && s2[i];i++){ char c=s2[i]; g_log[g_len++]=(c>=32||c<0)?c:'.'; }
        g_len += wsprintfA(g_log+g_len, "\"\r\n");
    } else {
        g_len += wsprintfA(g_log+g_len, "   (arg1 not a pointer)\r\n");
    }
    HANDLE f = CreateFileA("C:\\MapleDev\\spyargs.txt", GENERIC_WRITE, FILE_SHARE_READ, 0, CREATE_ALWAYS, FILE_ATTRIBUTE_NORMAL, 0);
    if (f != INVALID_HANDLE_VALUE) { DWORD w; WriteFile(f, g_log, g_len, &w, 0); CloseHandle(f); }
}

static void InstallHook() {
    BYTE* tr = (BYTE*)VirtualAlloc(0, 80, MEM_COMMIT|MEM_RESERVE, PAGE_EXECUTE_READWRITE);
    int i=0;
    tr[i++]=0x60;                                   // pushad
    tr[i++]=0x9C;                                   // pushfd
    tr[i++]=0x8B;tr[i++]=0x44;tr[i++]=0x24;tr[i++]=0x24;  // mov eax,[esp+36] retaddr
    tr[i++]=0x50;                                   // push eax (retaddr)
    tr[i++]=0x8B;tr[i++]=0x44;tr[i++]=0x24;tr[i++]=0x2C;  // mov eax,[esp+44] arg1 (40+4 pushed)
    tr[i++]=0x50;                                   // push eax (arg1)
    tr[i++]=0x8B;tr[i++]=0x44;tr[i++]=0x24;tr[i++]=0x24;  // mov eax,[esp+36] ecx(container) (28+8 pushed)
    tr[i++]=0x50;                                   // push eax (container)
    tr[i++]=0xE8; *(int*)(tr+i)=(int)((BYTE*)&Spy-(tr+i+4)); i+=4;  // call Spy
    tr[i++]=0x83;tr[i++]=0xC4;tr[i++]=0x0C;         // add esp,12
    tr[i++]=0x9D;                                   // popfd
    tr[i++]=0x61;                                   // popad
    tr[i++]=0xB8;tr[i++]=0x6C;tr[i++]=0xD3;tr[i++]=0xC1;tr[i++]=0x00; // mov eax,0xC1D36C
    tr[i++]=0xE9; *(int*)(tr+i)=(int)(HOOK_BACK-(DWORD)(tr+i+4)); i+=4;
    DWORD old; VirtualProtect((void*)HOOK_AT,5,PAGE_EXECUTE_READWRITE,&old);
    BYTE* q=(BYTE*)HOOK_AT; q[0]=0xE9; *(int*)(q+1)=(int)((DWORD)tr-(HOOK_AT+5));
    VirtualProtect((void*)HOOK_AT,5,old,&old);
}
BOOL WINAPI DllMain(HINSTANCE h, DWORD r, LPVOID){ if(r==DLL_PROCESS_ATTACH){ DisableThreadLibraryCalls(h); InstallHook(); } return TRUE; }

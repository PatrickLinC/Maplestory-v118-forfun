#include <windows.h>
#define HOOK_AT   0x009E3F16u
#define HOOK_BACK 0x009E3F1Du

// SAFE: only read [ebp+8]/[ebp+C] (ebp is valid); no pointer chasing.
extern "C" void __cdecl CDiag(DWORD ebpv, DWORD edi, DWORD esi) {
    char buf[256]; int p=0;
    p += wsprintfA(buf+p, "ebp=%08X\r\n[ebp+8](item)=%08X\r\n[ebp+C]=%08X\r\nedi=%08X esi=%08X\r\n",
                   ebpv, *(DWORD*)(ebpv+8), *(DWORD*)(ebpv+0xC), edi, esi);
    HANDLE f=CreateFileA("C:\\MapleDev\\cdiag.txt",GENERIC_WRITE,FILE_SHARE_READ,0,CREATE_ALWAYS,FILE_ATTRIBUTE_NORMAL,0);
    if(f!=INVALID_HANDLE_VALUE){DWORD w;WriteFile(f,buf,p,&w,0);CloseHandle(f);}
}

static void InstallHook() {
    BYTE* tr=(BYTE*)VirtualAlloc(0,64,MEM_COMMIT|MEM_RESERVE,PAGE_EXECUTE_READWRITE);
    int i=0;
    tr[i++]=0x60;tr[i++]=0x9C;
    tr[i++]=0x56;tr[i++]=0x57;tr[i++]=0x55;               // push esi;push edi;push ebp  (args: ebp,edi,esi order -> pushed reverse)
    tr[i++]=0xE8;*(int*)(tr+i)=(int)((BYTE*)&CDiag-(tr+i+4));i+=4;
    tr[i++]=0x83;tr[i++]=0xC4;tr[i++]=0x0C;               // add esp,12
    tr[i++]=0x9D;tr[i++]=0x61;
    tr[i++]=0x8D;tr[i++]=0x4D;tr[i++]=0xEC;tr[i++]=0xC6;tr[i++]=0x45;tr[i++]=0xFC;tr[i++]=0x02;
    tr[i++]=0xE9;*(int*)(tr+i)=(int)(HOOK_BACK-(DWORD)(tr+i+4));i+=4;
    DWORD old;VirtualProtect((void*)HOOK_AT,7,PAGE_EXECUTE_READWRITE,&old);
    BYTE* q=(BYTE*)HOOK_AT;q[0]=0xE9;*(int*)(q+1)=(int)((DWORD)tr-(HOOK_AT+5));q[5]=0x90;q[6]=0x90;
    VirtualProtect((void*)HOOK_AT,7,old,&old);
}
BOOL WINAPI DllMain(HINSTANCE h,DWORD r,LPVOID){if(r==DLL_PROCESS_ATTACH){DisableThreadLibraryCalls(h);InstallHook();}return TRUE;}

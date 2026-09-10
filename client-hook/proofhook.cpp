#include <windows.h>

static void writeProof() {
    char buf[512];
    unsigned char* p  = (unsigned char*)0x9E3F7E;   // tooltip stat fn prologue
    unsigned char* q  = (unsigned char*)0x9EB71E;   // add-line fn prologue
    unsigned char* r  = (unsigned char*)0x416A78;   // ZXString ctor prologue
    int n = wsprintfA(buf,
        "proof DLL loaded ok\r\n"
        "MapleStory base = %p\r\n"
        "@0x009E3F7E: %02x %02x %02x %02x %02x   (expect b8 ef c6 c1 00)\r\n"
        "@0x009EB71E: %02x %02x %02x %02x %02x   (expect b8 6c d3 c1 00)\r\n"
        "@0x00416A78: %02x %02x %02x %02x %02x\r\n",
        (void*)GetModuleHandleA("MapleStory.exe"),
        p[0],p[1],p[2],p[3],p[4],
        q[0],q[1],q[2],q[3],q[4],
        r[0],r[1],r[2],r[3],r[4]);
    HANDLE f = CreateFileA("C:\\MapleDev\\hook_proof.txt", GENERIC_WRITE, 0, 0, CREATE_ALWAYS, FILE_ATTRIBUTE_NORMAL, 0);
    if (f != INVALID_HANDLE_VALUE) { DWORD w; WriteFile(f, buf, n, &w, 0); CloseHandle(f); }
}

BOOL WINAPI DllMain(HINSTANCE h, DWORD reason, LPVOID) {
    if (reason == DLL_PROCESS_ATTACH) writeProof();
    return TRUE;
}

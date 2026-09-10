#include <windows.h>
#include "names.h"                 // NAMES[24], UNITS[24]  (Big5)

#define HOOK_AT   0x009E3F16u
#define HOOK_BACK 0x009E3F1Du
#define OWNER_OFF 0xF8
#define OWNER_DISPLAY_AT   0x009AE445u
#define OWNER_DISPLAY_BACK 0x009AE44Au

typedef void (__thiscall *ZXCtor)(void*, const char*, int);
static ZXCtor zxctor = (ZXCtor)0x00416A78;
static const char SEP[] = "- - - - - - - - - - - - -";
static const BYTE POL[] = {0xA4,0x77,0xA6,0xBE,0xAC,0x56,0x00};   // 已汙染

struct Ent { DWORD key; char s[64]; };
static Ent g_map[64];
static int g_n = 0;
static char g_bonusCodes[64][64];
static int g_bonusN = 0;
static volatile LONG g_bonusTraceCount = 0;
static char* mapGet(DWORD k){ for(int i=0;i<g_n;i++) if(g_map[i].key==k) return g_map[i].s; return 0; }
static bool sameText(const char* a, const char* b) {
    int i = 0;
    for (; i < 63 && a[i] && b[i] && a[i] == b[i]; ++i) {}
    return a[i] == 0 && b[i] == 0;
}
static bool addBonusCode(const char* v) {
    if (!v || !v[0]) return false;
    for (int i = 0; i < g_bonusN; ++i) if (sameText(g_bonusCodes[i], v)) return false;
    if (g_bonusN >= 64) return false;
    int i = 0; for (; v[i] && i < 62; ++i) g_bonusCodes[g_bonusN][i] = v[i];
    g_bonusCodes[g_bonusN][i] = 0;
    ++g_bonusN;
    return true;
}
static void mapPut(DWORD k, const char* v){
    char* d=mapGet(k);
    if(!d){ int idx=(g_n<64)?g_n++:(k&63); g_map[idx].key=k; d=g_map[idx].s; }
    int i=0; for(;v[i]&&i<62;i++) d[i]=v[i]; d[i]=0;
    addBonusCode(d);
}

static void traceBonus(const char* phase, int total) {
    LONG seq = InterlockedIncrement(&g_bonusTraceCount);
    if (seq > 80) return;
    char line[768] = {};
    int used = wsprintfA(line, "%s entries=%d bonus=%d", phase, g_n, total);
    int shown = g_bonusN < 8 ? g_bonusN : 8;
    for (int i = 0; i < shown && used < 700; ++i) {
        used += wsprintfA(line + used, " [%s]", g_bonusCodes[i]);
    }
    used += wsprintfA(line + used, "\r\n");
    HANDLE f = CreateFileA("C:\\MapleDev\\enchant_trace.log", FILE_APPEND_DATA,
        FILE_SHARE_READ | FILE_SHARE_WRITE, 0, OPEN_ALWAYS, FILE_ATTRIBUTE_NORMAL, 0);
    if (f != INVALID_HANDLE_VALUE) {
        DWORD written = 0;
        WriteFile(f, line, (DWORD)used, &written, 0);
        CloseHandle(f);
    }
}

static int readFileBonus() {
    HANDLE f = CreateFileA("C:\\MapleDev\\visual_damage_bonus.txt", GENERIC_READ,
        FILE_SHARE_READ | FILE_SHARE_WRITE, 0, OPEN_EXISTING, FILE_ATTRIBUTE_NORMAL, 0);
    if (f == INVALID_HANDLE_VALUE) return -1;
    char text[64] = {};
    DWORD read = 0;
    ReadFile(f, text, sizeof(text) - 1, &read, 0);
    CloseHandle(f);
    int value = 0;
    bool hasDigit = false;
    for (DWORD i = 0; i < read; ++i) {
        if (text[i] >= '0' && text[i] <= '9') {
            hasDigit = true;
            value = value * 10 + (text[i] - '0');
            if (value > 10000) return -1;
        } else if (hasDigit) {
            break;
        }
    }
    return hasDigit ? value : -1;
}

static int sumCodeType(int wanted) {
    int total = 0;
    for (int item = 0; item < g_bonusN; ++item) {
        const char* codes = g_bonusCodes[item];
        int i = (codes[0] == '0' || codes[0] == '1') ? 1 : 0;
        for (int guard = 0; codes[i] && guard < 24; ++guard) {
            char typeChar = codes[i++];
            if (typeChar < 'A' || typeChar > 'X') continue;
            int type = typeChar - 'A';
            int value = 0;
            bool hasValue = false;
            while (codes[i] >= '0' && codes[i] <= '9') {
                hasValue = true;
                value = value * 10 + (codes[i++] - '0');
                if (value > 1000000) value = 1000000;
            }
            if (hasValue && type == wanted) total += value;
        }
    }
    return total;
}

static bool isEnchantCodeText(const char* text) {
    if (!text || (text[0] != '0' && text[0] != '1')) return false;
    int i = 1;
    bool hasPart = false;
    while (text[i] && i < 63) {
        if (text[i] < 'A' || text[i] > 'X') return false;
        hasPart = true;
        ++i;
        if (text[i] < '0' || text[i] > '9') return false;
        while (text[i] >= '0' && text[i] <= '9' && i < 63) ++i;
    }
    return hasPart && text[i] == 0;
}

static bool isCachedEnchantCode(const char* text) {
    if (!isEnchantCodeText(text)) return false;
    for (int i = 0; i < g_bonusN; ++i) {
        if (sameText(g_bonusCodes[i], text)) return true;
    }
    return false;
}

// The client has already appended the Owner row by the time this hook runs.
// Remove only rows containing our encoded enchant owner value, leaving real
// owner names untouched.
static void removeEnchantOwnerRows(DWORD container) {
    if (container < 0x00100000 || container > 0x7FF00000) return;
    DWORD count = *(DWORD*)(container + 0x20);
    if (count < 1 || count > 32) return;
    for (DWORD row = 0; row < count;) {
        BYTE* entry = (BYTE*)(container + 0x24 + row * 0x20);
        DWORD textPtr = *(DWORD*)(entry + 0x0C);
        const char* text = (textPtr >= 0x00100000 && textPtr <= 0x7FF00000)
            ? (const char*)textPtr : nullptr;
        if (!isCachedEnchantCode(text)) {
            ++row;
            continue;
        }

        DWORD lineHeight = *(DWORD*)(entry + 0x04) + 4;
        if (lineHeight > *(DWORD*)(container + 0x08))
            lineHeight = *(DWORD*)(container + 0x08);
        MoveMemory(entry, entry + 0x20, (count - row - 1) * 0x20);
        --count;
        *(DWORD*)(container + 0x20) = count;
        *(DWORD*)(container + 0x08) -= lineHeight;
    }
}

// Read-only exports consumed by the visual damage hook.
extern "C" __declspec(dllexport) void __cdecl CacheVisualDamageCode(const char* code) {
    if (addBonusCode(code)) traceBonus("packet", sumCodeType(8));
}

extern "C" __declspec(dllexport) int __cdecl GetVisualDamageBonus() {
    int total = sumCodeType(8); // type I: ordinary damage %
    const int fileBonus = readFileBonus();
    if (fileBonus >= 0) total = fileBonus;
    traceBonus("damage", total);
    return total;
}

extern "C" __declspec(dllexport) int __cdecl GetVisualBossDamageBonus() {
    return sumCodeType(14); // type O: boss damage %
}

static void addLine(DWORD c, const char* text, DWORD color) {
    DWORD count = *(DWORD*)(c + 0x20);
    if (count < 1 || count > 28) return;
    DWORD zx = 0; zxctor(&zx, text, -1); if (!zx) return;
    BYTE* e = (BYTE*)(c + 0x24 + count * 0x20);
    *(DWORD*)(e+0x00)=140; *(DWORD*)(e+0x04)=12; *(DWORD*)(e+0x08)=8;
    *(DWORD*)(e+0x0C)=zx;  *(DWORD*)(e+0x10)=color; *(DWORD*)(e+0x14)=0;
    *(DWORD*)(e+0x18)=0;   *(DWORD*)(e+0x1C)=0;
    *(DWORD*)(c+0x20)=count+1;
    *(DWORD*)(c+0x08)+=16;
    DWORD w=180; if(*(DWORD*)(c+0x0C)<w)*(DWORD*)(c+0x0C)=w;
}

// codes: "[flag][Type value][Type value]..."  Type = 'A'+type, value = digits
static void showAffix(DWORD container, const char* codes) {
    addLine(container, SEP, 1000);
    int i = 0;
    if (codes[0]=='0' || codes[0]=='1') i = 1;      // skip corruption flag
    for (int guard=0; codes[i] && guard<24; guard++) {
        char tc = codes[i];
        if (tc < 'A' || tc > 'X') { i++; continue; }
        int type = tc - 'A'; i++;
        char val[8]; int vi=0;
        while (codes[i]>='0' && codes[i]<='9' && vi<6) val[vi++]=codes[i++];
        val[vi]=0;
        if (type<0 || type>=24) continue;
        char line[96]; int li=0;
        const char* nm=NAMES[type]; for(int k=0;nm[k]&&li<80;k++) line[li++]=nm[k];
        line[li++]=' '; line[li++]='+';
        for(int k=0;val[k]&&li<90;k++) line[li++]=val[k];
        const char* un=UNITS[type]; for(int k=0;un[k]&&li<94;k++) line[li++]=un[k];
        line[li]=0;
        addLine(container, line, 0x1E);
    }
}

static void __cdecl HideEncodedOwnerBeforeFormat(DWORD item) {
    if (item < 0x00100000 || item > 0x7FF00000) return;
    char* owner = (char*)(item + OWNER_OFF);
    if (!isEnchantCodeText(owner)) return;
    // Keep a copy for the later affix tooltip hook, then hide the value before
    // the client formats the Owner text into its visible tooltip string.
    mapPut(item, owner);
    traceBonus("owner", sumCodeType(8));
    owner[0] = 0;
}

extern "C" void __cdecl OnEnd(DWORD container, DWORD edi) {
    if (container < 0x00100000 || container > 0x7FF00000) return;
    if (edi < 0x00100000 || edi > 0x7FF00000) return;
    BYTE* ow = (BYTE*)(edi + OWNER_OFF);
    bool sentinel = (ow[0]==0xA4&&ow[1]==0x77&&ow[2]==0xA6&&ow[3]==0xBE&&ow[4]==0xAC&&ow[5]==0x56);
    bool myCode = (ow[0]=='0' || ow[0]=='1');                    // 附魔代碼旗標
    const char* codes;
    if (myCode) {
        mapPut(edi, (const char*)ow);
        traceBonus("cache", 0);
        codes = mapGet(edi);
        // The tooltip row may point at the original Owner buffer. Remove it
        // before scrubbing that buffer, otherwise the row is no longer
        // recognizable as our encoded enchant value.
        removeEnchantOwnerRows(container);
        if (ow[0]=='1') { for(int k=0;k<7;k++) ow[k]=POL[k]; }   // 污染 -> 名字顯示 已汙染
        else            { ow[0]=0; }                             // 未污染 -> 清掉名字前綴
    } else if (sentinel || ow[0]==0) {
        codes = mapGet(edi);                                     // 我覆蓋過(已汙染/清空)-> 用快取
        if (!codes || !codes[0]) return;
    } else {
        return;                                                  // 真實 owner(非我的代碼)-> 不動,保留名字
    }
    removeEnchantOwnerRows(container);
    showAffix(container, codes);
}

static void InstallHook() {
    BYTE* tr=(BYTE*)VirtualAlloc(0,64,MEM_COMMIT|MEM_RESERVE,PAGE_EXECUTE_READWRITE);
    int i=0;
    tr[i++]=0x60;tr[i++]=0x9C;tr[i++]=0x57;tr[i++]=0x53;
    tr[i++]=0xE8;*(int*)(tr+i)=(int)((BYTE*)&OnEnd-(tr+i+4));i+=4;
    tr[i++]=0x83;tr[i++]=0xC4;tr[i++]=0x08;tr[i++]=0x9D;tr[i++]=0x61;
    tr[i++]=0x8D;tr[i++]=0x4D;tr[i++]=0xEC;tr[i++]=0xC6;tr[i++]=0x45;tr[i++]=0xFC;tr[i++]=0x02;
    tr[i++]=0xE9;*(int*)(tr+i)=(int)(HOOK_BACK-(DWORD)(tr+i+4));i+=4;
    DWORD old;VirtualProtect((void*)HOOK_AT,7,PAGE_EXECUTE_READWRITE,&old);
    BYTE* q=(BYTE*)HOOK_AT;q[0]=0xE9;*(int*)(q+1)=(int)((DWORD)tr-(HOOK_AT+5));q[5]=0x90;q[6]=0x90;
    VirtualProtect((void*)HOOK_AT,7,old,&old);

    // The equipment tooltip formats the Owner field in another routine. This
    // hook runs before that formatting and is intentionally separate from the
    // affix-line hook above.
    BYTE* ownerTr=(BYTE*)VirtualAlloc(0,64,MEM_COMMIT|MEM_RESERVE,PAGE_EXECUTE_READWRITE);
    int j=0;
    ownerTr[j++]=0x60; ownerTr[j++]=0x9C; ownerTr[j++]=0x57;
    ownerTr[j++]=0xE8; *(int*)(ownerTr+j)=(int)((BYTE*)&HideEncodedOwnerBeforeFormat-(ownerTr+j+4)); j+=4;
    ownerTr[j++]=0x83; ownerTr[j++]=0xC4; ownerTr[j++]=0x04;
    ownerTr[j++]=0x9D; ownerTr[j++]=0x61;
    ownerTr[j++]=0xFF; ownerTr[j++]=0x75; ownerTr[j++]=0x08;
    ownerTr[j++]=0x8B; ownerTr[j++]=0xCF;
    ownerTr[j++]=0xE9; *(int*)(ownerTr+j)=(int)(OWNER_DISPLAY_BACK-(DWORD)(ownerTr+j+4)); j+=4;
    VirtualProtect((void*)OWNER_DISPLAY_AT,5,PAGE_EXECUTE_READWRITE,&old);
    BYTE* oq=(BYTE*)OWNER_DISPLAY_AT;
    oq[0]=0xE9; *(int*)(oq+1)=(int)((DWORD)ownerTr-(OWNER_DISPLAY_AT+5));
    VirtualProtect((void*)OWNER_DISPLAY_AT,5,old,&old);
}
BOOL WINAPI DllMain(HINSTANCE h,DWORD r,LPVOID){if(r==DLL_PROCESS_ATTACH){DisableThreadLibraryCalls(h);InstallHook();}return TRUE;}

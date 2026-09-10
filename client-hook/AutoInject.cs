using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

class AutoInject {
    [DllImport("kernel32", SetLastError=true)] static extern IntPtr OpenProcess(uint a, bool i, int pid);
    [DllImport("kernel32", SetLastError=true)] static extern IntPtr VirtualAllocEx(IntPtr h, IntPtr addr, uint size, uint type, uint prot);
    [DllImport("kernel32", SetLastError=true)] static extern bool WriteProcessMemory(IntPtr h, IntPtr addr, byte[] buf, uint size, out uint w);
    [DllImport("kernel32", SetLastError=true)] static extern bool ReadProcessMemory(IntPtr h, IntPtr addr, byte[] buf, int size, out int r);
    [DllImport("kernel32", SetLastError=true)] static extern IntPtr GetModuleHandleA(string m);
    [DllImport("kernel32", SetLastError=true)] static extern IntPtr GetProcAddress(IntPtr h, string p);
    [DllImport("kernel32", SetLastError=true)] static extern IntPtr CreateRemoteThread(IntPtr h, IntPtr a, uint s, IntPtr start, IntPtr par, uint f, IntPtr t);
    [DllImport("kernel32")] static extern uint WaitForSingleObject(IntPtr h, uint ms);

    static readonly byte[] EXPECT = { 0x8d,0x4d,0xec,0xc6,0x45,0xfc,0x02 };
    static readonly byte[] HOOKED = { 0xE9 };   // already-injected marker
    const long HOOK_AT = 0x009E3F16;
    static StreamWriter L;
    static void Log(string s){ try{ L.WriteLine(DateTime.Now.ToString("HH:mm:ss ")+s); L.Flush(); }catch{} Console.WriteLine(s); }

    static void Main(string[] a) {
        try { L = new StreamWriter("C:\\MapleDev\\autoinject_log.txt", false); } catch {}
        string dll = System.IO.Path.GetFullPath(a.Length>0 ? a[0] : "enchant.dll");
        Log("start, dll=" + dll + " exists=" + File.Exists(dll));
        int pid = -1; IntPtr h = IntPtr.Zero; bool ready = false; int foundAt = -1; int stable = 0;
        for (int t = 0; t < 180; t++) {
            var p = Process.GetProcessesByName("MapleStory");
            if (p.Length > 0) {
                if (pid != p[0].Id) { pid = p[0].Id; h = IntPtr.Zero; foundAt = t; stable = 0; Log("found MapleStory pid=" + pid); }
                if (h == IntPtr.Zero) { h = OpenProcess(0x1FFFFF, false, pid); Log("OpenProcess h=" + h.ToInt64() + " err=" + Marshal.GetLastWin32Error()); }
                if (h != IntPtr.Zero) {
                    byte[] buf = new byte[7]; int rd;
                    bool ok = ReadProcessMemory(h, new IntPtr(HOOK_AT), buf, 7, out rd);
                    if (ok && rd == 7) {
                        if (buf[0]==0xE9) { Log("already hooked, done."); return; }
                        bool m = true; for (int i=0;i<7;i++) if (buf[i]!=EXPECT[i]) { m=false; break; }
                        stable = m ? stable+1 : 0;
                        int age = t - foundAt;
                        if (t%3==0) Log("age=" + age + "s stable=" + stable + " bytes=" + BitConverter.ToString(buf));
                        // require code stable for >=6s AND process alive >=10s (unpacker fully done)
                        if (stable >= 6 && age >= 10) { Log("unpack settled, injecting."); ready = true; break; }
                    } else if (t%3==0) Log("RPM fail rd=" + rd + " err=" + Marshal.GetLastWin32Error());
                }
            }
            Thread.Sleep(1000);
        }
        if (!ready || h == IntPtr.Zero) { Log("timeout / no handle."); return; }
        byte[] b = Encoding.Unicode.GetBytes(dll + "\0");   // wide path (handles 中文 in path)
        IntPtr mem = VirtualAllocEx(h, IntPtr.Zero, (uint)b.Length, 0x3000, 0x40);
        uint w; WriteProcessMemory(h, mem, b, (uint)b.Length, out w);
        IntPtr load = GetProcAddress(GetModuleHandleA("kernel32.dll"), "LoadLibraryW");
        IntPtr th = CreateRemoteThread(h, IntPtr.Zero, 0, load, mem, 0, IntPtr.Zero);
        Log("CreateRemoteThread th=" + th.ToInt64() + " err=" + Marshal.GetLastWin32Error());
        if (th != IntPtr.Zero) { WaitForSingleObject(th, 5000); Log("DONE injected pid=" + pid); }
    }
}

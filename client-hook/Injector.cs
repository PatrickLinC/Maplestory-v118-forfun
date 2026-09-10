using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

class Injector {
    [DllImport("kernel32", SetLastError=true)] static extern IntPtr OpenProcess(uint a, bool i, int pid);
    [DllImport("kernel32", SetLastError=true)] static extern IntPtr VirtualAllocEx(IntPtr h, IntPtr addr, uint size, uint type, uint prot);
    [DllImport("kernel32", SetLastError=true)] static extern bool WriteProcessMemory(IntPtr h, IntPtr addr, byte[] buf, uint size, out uint written);
    [DllImport("kernel32", SetLastError=true)] static extern IntPtr GetModuleHandleA(string m);
    [DllImport("kernel32", SetLastError=true)] static extern IntPtr GetProcAddress(IntPtr h, string p);
    [DllImport("kernel32", SetLastError=true)] static extern IntPtr CreateRemoteThread(IntPtr h, IntPtr attr, uint stack, IntPtr start, IntPtr param, uint flags, IntPtr tid);
    [DllImport("kernel32")] static extern uint WaitForSingleObject(IntPtr h, uint ms);

    static void Main(string[] a) {
        if (a.Length < 1) { Console.WriteLine("usage: Injector <dllpath>"); return; }
        string dll = System.IO.Path.GetFullPath(a[0]);
        var procs = Process.GetProcessesByName("MapleStory");
        if (procs.Length == 0) { Console.WriteLine("找不到 MapleStory 程序 (客戶端有開嗎?)"); return; }
        int pid = procs[0].Id;
        IntPtr h = OpenProcess(0x1FFFFF, false, pid);
        if (h == IntPtr.Zero) { Console.WriteLine("OpenProcess 失敗 err=" + Marshal.GetLastWin32Error()); return; }
        byte[] b = Encoding.ASCII.GetBytes(dll + "\0");
        IntPtr mem = VirtualAllocEx(h, IntPtr.Zero, (uint)b.Length, 0x3000, 0x40);
        if (mem == IntPtr.Zero) { Console.WriteLine("VirtualAllocEx 失敗 err=" + Marshal.GetLastWin32Error()); return; }
        uint w; WriteProcessMemory(h, mem, b, (uint)b.Length, out w);
        IntPtr load = GetProcAddress(GetModuleHandleA("kernel32.dll"), "LoadLibraryA");
        IntPtr t = CreateRemoteThread(h, IntPtr.Zero, 0, load, mem, 0, IntPtr.Zero);
        if (t == IntPtr.Zero) { Console.WriteLine("CreateRemoteThread 失敗 err=" + Marshal.GetLastWin32Error()); return; }
        WaitForSingleObject(t, 5000);
        Console.WriteLine("注入完成 (pid=" + pid + ", dll=" + dll + ")");
    }
}

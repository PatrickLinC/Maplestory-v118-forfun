using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

class DumpProc {
    [DllImport("kernel32.dll", SetLastError = true)]
    static extern IntPtr OpenProcess(uint access, bool inherit, int pid);
    [DllImport("kernel32.dll", SetLastError = true)]
    static extern bool ReadProcessMemory(IntPtr h, IntPtr addr, byte[] buf, int size, out int read);
    [DllImport("kernel32.dll")]
    static extern bool CloseHandle(IntPtr h);
    const uint PROCESS_VM_READ = 0x10, PROCESS_QUERY_INFORMATION = 0x400;

    static void Main(string[] a) {
        // a0 = process name (e.g. MapleStory)  a1 = output file
        string pname = a.Length > 0 ? a[0] : "MapleStory";
        string outf = a.Length > 1 ? a[1] : "C:\\MapleDev\\maple_dump.bin";
        var procs = Process.GetProcessesByName(pname);
        if (procs.Length == 0) { Console.WriteLine("找不到程序: " + pname + " (客戶端有開嗎?)"); return; }
        var p = procs[0];
        IntPtr baseAddr = p.MainModule.BaseAddress;
        int size = p.MainModule.ModuleMemorySize;
        Console.WriteLine("PID=" + p.Id + "  module=" + p.MainModule.ModuleName + "  base=0x" + baseAddr.ToInt64().ToString("X") + "  size=0x" + size.ToString("X"));

        IntPtr h = OpenProcess(PROCESS_VM_READ | PROCESS_QUERY_INFORMATION, false, p.Id);
        if (h == IntPtr.Zero) { Console.WriteLine("OpenProcess 失敗 err=" + Marshal.GetLastWin32Error() + " (試試以系統管理員執行)"); return; }

        const int PAGE = 0x1000;
        byte[] img = new byte[size];
        byte[] pg = new byte[PAGE];
        int okPages = 0, failPages = 0;
        for (int off = 0; off < size; off += PAGE) {
            int toread = Math.Min(PAGE, size - off);
            int read;
            if (ReadProcessMemory(h, new IntPtr(baseAddr.ToInt64() + off), pg, toread, out read) && read > 0) {
                Array.Copy(pg, 0, img, off, read);
                okPages++;
            } else failPages++;
        }
        CloseHandle(h);
        File.WriteAllBytes(outf, img);
        Console.WriteLine("dump 完成 -> " + outf + "  (讀到頁=" + okPages + " 失敗頁=" + failPages + ")");
        Console.WriteLine("提示: base=0x" + baseAddr.ToInt64().ToString("X") + " => 檔案 offset 0 對應 VA 0x" + baseAddr.ToInt64().ToString("X"));
    }
}

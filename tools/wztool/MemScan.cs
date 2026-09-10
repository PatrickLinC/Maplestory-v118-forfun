using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Runtime.InteropServices;
using System.Collections.Generic;

class MemScan {
    [DllImport("kernel32.dll", SetLastError=true)]
    static extern IntPtr OpenProcess(uint a, bool i, int pid);
    [DllImport("kernel32.dll", SetLastError=true)]
    static extern bool ReadProcessMemory(IntPtr h, IntPtr addr, byte[] buf, int size, out int read);
    [DllImport("kernel32.dll")]
    static extern int VirtualQueryEx(IntPtr h, IntPtr addr, out MEMORY_BASIC_INFORMATION mbi, int len);
    [StructLayout(LayoutKind.Sequential)]
    struct MEMORY_BASIC_INFORMATION { public IntPtr BaseAddress, AllocationBase; public uint AllocationProtect; public IntPtr RegionSize; public uint State, Protect, Type; }

    static byte[] data;
    static int Find(byte[] hay, byte[] needle, int from){
        for(int i=from;i<=hay.Length-needle.Length;i++){ bool m=true; for(int k=0;k<needle.Length;k++) if(hay[i+k]!=needle[k]){m=false;break;} if(m) return i; }
        return -1;
    }
    static void Main(string[] a){
        var p = Process.GetProcessesByName("MapleStory");
        if(p.Length==0){ Console.WriteLine("no process"); return; }
        IntPtr h = OpenProcess(0x10|0x400, false, p[0].Id);
        if(h==IntPtr.Zero){ Console.WriteLine("OpenProcess fail "+Marshal.GetLastWin32Error()); return; }

        var patterns = new Dictionary<string,byte[]>();
        patterns["POT(01 7A..05 7A)"] = new byte[]{0x01,0x7A,0x02,0x7A,0x03,0x7A};
        patterns["OWNER ascii"] = Encoding.ASCII.GetBytes("QZWXPOT77");
        patterns["OWNER wide"] = Encoding.Unicode.GetBytes("QZWXPOT77");

        var sb = new StringBuilder();
        IntPtr addr = IntPtr.Zero;
        long scanned=0;
        while(true){
            MEMORY_BASIC_INFORMATION mbi;
            if(VirtualQueryEx(h, addr, out mbi, Marshal.SizeOf(typeof(MEMORY_BASIC_INFORMATION)))==0) break;
            long baseA = mbi.BaseAddress.ToInt64();
            long size = mbi.RegionSize.ToInt64();
            bool readable = mbi.State==0x1000 && (mbi.Protect==0x04||mbi.Protect==0x02||mbi.Protect==0x20||mbi.Protect==0x40||mbi.Protect==0x08||mbi.Protect==0x80);
            if(readable && size>0 && size<0x8000000){
                byte[] buf = new byte[size]; int read;
                if(ReadProcessMemory(h, mbi.BaseAddress, buf, (int)size, out read) && read>0){
                    scanned += read;
                    foreach(var kv in patterns){
                        int idx=0;
                        while((idx=Find(buf, kv.Value, idx))>=0){
                            long hitVA = baseA + idx;
                            sb.AppendLine("=== HIT "+kv.Key+" @0x"+hitVA.ToString("X")+" (region 0x"+baseA.ToString("X")+") ===");
                            // dump context [hit-0x60, hit+0x50]
                            int c0 = Math.Max(0, idx-0x60), c1 = Math.Min(read, idx+0x50);
                            for(int off=c0; off<c1; off+=16){
                                var line = new StringBuilder();
                                line.Append("0x"+(baseA+off).ToString("X8")+"  ");
                                for(int j=0;j<16 && off+j<c1;j++) line.Append(buf[off+j].ToString("X2")+" ");
                                sb.AppendLine(line.ToString());
                            }
                            idx += kv.Value.Length;
                        }
                    }
                }
            }
            long next = baseA + size;
            if(next<=baseA) break;
            addr = new IntPtr(next);
            if(next > 0x7FFF0000L) break;
        }
        Console.OutputEncoding=Encoding.UTF8;
        Console.WriteLine("scanned "+(scanned/1024/1024)+" MB");
        Console.WriteLine(sb.ToString());
        File.WriteAllText(@"C:\MapleDev\memscan_out.txt", sb.ToString());
    }
}

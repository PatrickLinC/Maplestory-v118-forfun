using System;
using System.Diagnostics;
using System.Text;
using System.Runtime.InteropServices;

class ReadMem {
    [DllImport("kernel32.dll", SetLastError=true)]
    static extern IntPtr OpenProcess(uint a, bool i, int pid);
    [DllImport("kernel32.dll", SetLastError=true)]
    static extern bool ReadProcessMemory(IntPtr h, IntPtr addr, byte[] buf, int size, out int read);

    static void Main(string[] a){
        long start = Convert.ToInt64(a[0],16);
        int len = a.Length>1 ? Convert.ToInt32(a[1],16) : 0x300;
        var p = Process.GetProcessesByName("MapleStory");
        if(p.Length==0){ Console.WriteLine("no process"); return; }
        IntPtr h = OpenProcess(0x10|0x400, false, p[0].Id);
        byte[] buf = new byte[len]; int read;
        if(!ReadProcessMemory(h, new IntPtr(start), buf, len, out read) || read==0){ Console.WriteLine("read fail"); return; }
        var sb = new StringBuilder();
        for(int off=0; off<read; off+=16){
            sb.Append("+0x"+off.ToString("X3")+" 0x"+(start+off).ToString("X8")+"  ");
            for(int j=0;j<16 && off+j<read;j++) sb.Append(buf[off+j].ToString("X2")+" ");
            // decode as shorts
            sb.Append("  | ");
            for(int j=0;j<16 && off+j+1<read;j+=2){ short v=BitConverter.ToInt16(buf,off+j); sb.Append(v+" "); }
            sb.Append("\n");
        }
        // highlight marker shorts 0x7A01..0x7A05
        sb.Append("\n-- looking for potential markers 31233..31237 (0x7A01..05) --\n");
        for(int i=0;i+1<read;i++){ short v=BitConverter.ToInt16(buf,i); if(v>=31233 && v<=31237) sb.Append("  short "+v+" @ +0x"+i.ToString("X3")+" (VA 0x"+(start+i).ToString("X8")+")\n"); }
        Console.OutputEncoding=Encoding.UTF8;
        Console.WriteLine(sb.ToString());
    }
}

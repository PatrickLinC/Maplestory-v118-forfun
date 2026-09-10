using System;
using System.Diagnostics;
using System.Text;
using System.Runtime.InteropServices;

class ReadEntries {
    [DllImport("kernel32", SetLastError=true)] static extern IntPtr OpenProcess(uint a, bool i, int pid);
    [DllImport("kernel32")] static extern bool ReadProcessMemory(IntPtr h, IntPtr addr, byte[] buf, int size, out int read);
    static IntPtr H;
    static byte[] Read(long addr, int n){ byte[] b=new byte[n]; int r; ReadProcessMemory(H,new IntPtr(addr),b,n,out r); return b; }
    static void Main(string[] a){
        long container=Convert.ToInt64(a[0],16);
        int n=a.Length>1?int.Parse(a[1]):12;
        var p=Process.GetProcessesByName("MapleStory"); if(p.Length==0){Console.WriteLine("no proc");return;}
        H=OpenProcess(0x10|0x400,false,p[0].Id);
        byte[] blk=Read(container, 0x24 + n*0x20);
        uint count=BitConverter.ToUInt32(blk,0x20);
        Console.OutputEncoding=Encoding.UTF8;
        Console.WriteLine("count="+count+"  box+8(height)="+BitConverter.ToUInt32(blk,0x08)+"  +C(width)="+BitConverter.ToUInt32(blk,0x0C));
        for(int i=0;i<n;i++){
            int e=0x24+i*0x20;
            uint w=BitConverter.ToUInt32(blk,e+0x00);
            uint h=BitConverter.ToUInt32(blk,e+0x04);
            uint zx=BitConverter.ToUInt32(blk,e+0x0C);
            string s="";
            if(zx>=0x100000 && zx<0x7FF00000){ byte[] sb=Read(zx,30); int len=0; while(len<30&&sb[len]!=0)len++; s=Encoding.GetEncoding(950).GetString(sb,0,len); }
            Console.WriteLine("["+i+"] w="+w+" h="+h+" zx=0x"+zx.ToString("X8")+" : "+s);
        }
    }
}

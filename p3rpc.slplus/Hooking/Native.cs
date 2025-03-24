using System.Runtime.InteropServices;
namespace p3rpc.slplus.Hooking
{
    public static class Native
    {
        [DllImport("kernel32.dll", CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern nint GetModuleHandleA(string lpModuleName);
    }
}
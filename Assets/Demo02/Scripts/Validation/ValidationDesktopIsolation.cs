#if UNITY_EDITOR || DEMO02_VALIDATION
using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Altaius.Demo02.Validation
{
    /// <summary>Read-only Win32 identity guard. This type cannot switch or activate desktops.</summary>
    public static class ValidationDesktopIsolation
    {
        [DllImport("user32.dll",SetLastError=true)] static extern IntPtr GetThreadDesktop(uint threadId);
        [DllImport("kernel32.dll")] static extern uint GetCurrentThreadId();
        [DllImport("user32.dll",SetLastError=true)] static extern IntPtr GetProcessWindowStation();
        [DllImport("user32.dll",SetLastError=true)] static extern IntPtr OpenInputDesktop(uint flags,bool inherit,uint access);
        [DllImport("user32.dll",SetLastError=true)] static extern bool CloseDesktop(IntPtr desktop);
        [DllImport("user32.dll",CharSet=CharSet.Unicode,SetLastError=true)] static extern bool GetUserObjectInformationW(IntPtr handle,int index,StringBuilder value,uint size,out uint needed);
        [DllImport("user32.dll",EntryPoint="GetUserObjectInformationW",SetLastError=true)] static extern bool GetInputFlag(IntPtr handle,int index,out int value,uint size,out uint needed);

        static string Name(IntPtr handle)
        {
            if(handle==IntPtr.Zero)throw new InvalidOperationException("Missing native desktop handle: "+Marshal.GetLastWin32Error());
            var value=new StringBuilder(512);uint needed;
            if(!GetUserObjectInformationW(handle,2,value,1024,out needed))throw new InvalidOperationException("Native identity unavailable: "+Marshal.GetLastWin32Error());
            return value.ToString();
        }
        public static string Validate(string expectedDesktop,string expectedInput)
        {
            if(string.IsNullOrEmpty(expectedDesktop)||!expectedDesktop.StartsWith("Demo02Validation_",StringComparison.Ordinal)||expectedDesktop.IndexOf('\\')>=0||string.IsNullOrEmpty(expectedInput))throw new InvalidOperationException("Explicit isolated desktop and input identity required.");
            var threadDesktop=GetThreadDesktop(GetCurrentThreadId());var desktop=Name(threadDesktop);var station=Name(GetProcessWindowStation());
            var inputHandle=OpenInputDesktop(0,false,1);if(inputHandle==IntPtr.Zero)throw new InvalidOperationException("Input desktop readback unavailable: "+Marshal.GetLastWin32Error());
            string input;try{input=Name(inputHandle);}finally{CloseDesktop(inputHandle);}
            int receivesInput;uint needed;if(!GetInputFlag(threadDesktop,6,out receivesInput,4,out needed))throw new InvalidOperationException("Input ownership readback unavailable: "+Marshal.GetLastWin32Error());
            if(!string.Equals(station,"WinSta0",StringComparison.OrdinalIgnoreCase)||!string.Equals(desktop,expectedDesktop,StringComparison.Ordinal)||!string.Equals(input,expectedInput,StringComparison.Ordinal)||string.Equals(desktop,input,StringComparison.OrdinalIgnoreCase)||receivesInput!=0)throw new InvalidOperationException("Desktop isolation mismatch: station="+station+" process="+desktop+" input="+input+" receivesInput="+receivesInput);
            return "Station "+station+" | Process desktop "+desktop+" | Input desktop "+input+" | Receives input false";
        }
    }
}
#endif

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.IO;

namespace Host_Process_for_Windows_Tasks
{
    class Program
    {

        private const int WH_KEYBOARD_LL = 13;

        private const int WM_KEYDOWN = 0x0100;
        private const int WM_KEYUP = 0x0101;
        private const int WM_SYSKEYDOWN = 0x0104;
        private const int WM_SYSKEYUP = 0x0105;


        private static LowLevelKeyboardProc _proc = HookCallback;
        private static IntPtr _hookID = IntPtr.Zero;
        private static AudioPlayer _audioPlayer;

        private static Boolean _shiftLIsActive = false;
        private static Boolean _shiftRIsActive = false;
        private static Boolean _capslockIsActive = false;

        private static Dictionary<int, bool> _isKeyDown = new Dictionary<int, bool>();

        static void Main(string[] args)
        {

            var handle = GetConsoleWindow();

            // Hide
            ShowWindow(handle, SW_HIDE);

            _audioPlayer = new AudioPlayer();

            _hookID = SetHook(_proc);
            Application.Run();
            UnhookWindowsHookEx(_hookID);
        }

        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        private static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            

            if (nCode >= 0)
            {

                //if blocked, don't do anything and don't pass the hook to the next executor
                if(AudioPlayer.Blocked)
                {
                    return new IntPtr(1);
                }

                int vkCode = Marshal.ReadInt32(lParam);
                //filter out repeated keypresses manually
                if (wParam == (IntPtr)WM_KEYUP || wParam == (IntPtr)WM_SYSKEYUP)
                {
                    //set as released
                    _isKeyDown[vkCode] = false;
                }
                if (wParam == (IntPtr)WM_KEYDOWN || wParam == (IntPtr)WM_SYSKEYDOWN)
                {
                    //check if already down
                    var isDown = _isKeyDown.GetValueOrDefault(vkCode, false);

                    //set down
                    _isKeyDown[vkCode] = true;

                    //abort
                    if (isDown) return CallNextHookEx(_hookID, nCode, wParam, lParam);
                }
                

                if (wParam == (IntPtr)WM_KEYDOWN) Console.Write("WM_KEYDOWN");
                if (wParam == (IntPtr)WM_KEYUP) Console.Write("WM_KEYUP");
                if (wParam == (IntPtr)WM_SYSKEYDOWN) Console.Write("WM_SYSKEYDOWN");
                if (wParam == (IntPtr)WM_SYSKEYUP) Console.Write("WM_SYSKEYUP");
                Console.Write("    ");
                Console.WriteLine((Keys)vkCode);

                //LShift Key
                if((Keys)vkCode == Keys.LShiftKey)
                {
                    if (wParam == (IntPtr)WM_KEYDOWN) _shiftLIsActive = true;
                    if (wParam == (IntPtr)WM_KEYUP) _shiftLIsActive = false;
                }
                //RShift Key
                if ((Keys)vkCode == Keys.RShiftKey)
                {
                    if (wParam == (IntPtr)WM_KEYDOWN) _shiftRIsActive = true;
                    if (wParam == (IntPtr)WM_KEYUP) _shiftRIsActive = false;
                }
                //Capslock Key
                if ((Keys)vkCode == Keys.CapsLock)
                {
                    if (wParam == (IntPtr)WM_KEYDOWN) _capslockIsActive ^= true;
                }
            }

            if (nCode >= 0 && wParam == (IntPtr)WM_KEYDOWN || wParam == (IntPtr)WM_SYSKEYDOWN)
            {
                int vkCode = Marshal.ReadInt32(lParam);
                //Console.WriteLine((Keys)vkCode);
                //StreamWriter sw = new StreamWriter(Application.StartupPath + @"\log.txt", true);
                //sw.Write((Keys)vkCode);
                var style = AudioPlayer.Style.lowercase;
                if ((_shiftLIsActive || _shiftRIsActive) && vkCode >= (int)Keys.A && vkCode <= (int)Keys.Z) style = AudioPlayer.Style.uppercase;
                if (_capslockIsActive) style = AudioPlayer.Style.capslock; //TODO Capslock + Shift, what should happen?
                if (_audioPlayer.ConsecutiveInterruptions > 5)
                {
                    //play interruption stuff
                    _audioPlayer.playInterruption();
                }
                else
                {
                    _audioPlayer.playKey((Keys)vkCode, style);
                }
                //sw.Close();
            }
            return CallNextHookEx(_hookID, nCode, wParam, lParam);
        }

        private static IntPtr SetHook(LowLevelKeyboardProc proc)
        {
            using (Process curProcess = Process.GetCurrentProcess())
            using (ProcessModule curModule = curProcess.MainModule)
            {
                return SetWindowsHookEx(WH_KEYBOARD_LL, proc, GetModuleHandle(curModule.ModuleName), 0);
            }
        }

        //These Dll's will handle the hooks. Yaaar mateys!

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        // The two dll imports below will handle the window hiding.

        [DllImport("kernel32.dll")]
        static extern IntPtr GetConsoleWindow();

        [DllImport("user32.dll")]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        const int SW_HIDE = 0;
    }
}

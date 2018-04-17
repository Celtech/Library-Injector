using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Threading;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Injector
{
    public partial class Form1 : Form
    {
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern uint GetPrivateProfileString(
            string lpAppName, string lpKeyName, string lpDefault, StringBuilder lpReturnedString,
            uint nSize, string lpFileName);
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern bool WritePrivateProfileString(
            string lpAppName, string lpKeyName, string lpString, string lpFileName);

        public Form1()
        {
            InitializeComponent();
        }

        [DllImport("kernel32")]
        public static extern IntPtr CreateRemoteThread(
          IntPtr hProcess,
          IntPtr lpThreadAttributes,
          uint dwStackSize,
          UIntPtr lpStartAddress, // raw Pointer into remote process
          IntPtr lpParameter,
          uint dwCreationFlags,
          out IntPtr lpThreadId
        );

        [DllImport("kernel32.dll")]
        public static extern IntPtr OpenProcess(
            UInt32 dwDesiredAccess,
            Int32 bInheritHandle,
            Int32 dwProcessId
            );

        [DllImport("kernel32.dll")]
        public static extern Int32 CloseHandle(
        IntPtr hObject
        );

        [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
        static extern bool VirtualFreeEx(
            IntPtr hProcess,
            IntPtr lpAddress,
            UIntPtr dwSize,
            uint dwFreeType
            );

        [DllImport("kernel32.dll", CharSet = CharSet.Ansi, ExactSpelling = true)]
        public static extern UIntPtr GetProcAddress(
            IntPtr hModule,
            string procName
            );

        [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
        static extern IntPtr VirtualAllocEx(
            IntPtr hProcess,
            IntPtr lpAddress,
            uint dwSize,
            uint flAllocationType,
            uint flProtect
            );

        [DllImport("kernel32.dll")]
        static extern bool WriteProcessMemory(
            IntPtr hProcess,
            IntPtr lpBaseAddress,
            string lpBuffer,
            UIntPtr nSize,
            out IntPtr lpNumberOfBytesWritten
        );

        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr GetModuleHandle(
            string lpModuleName
            );

        [DllImport("kernel32", SetLastError = true, ExactSpelling = true)]
        internal static extern Int32 WaitForSingleObject(
            IntPtr handle,
            Int32 milliseconds
            );

        static void WriteINISetting(string iniFilePath, string section, string key, string value)
        {
            WritePrivateProfileString(section, key, value, iniFilePath);
        }

        static string ReadINISetting(string iniFilePath, string section, string key)
        {
            var retVal = new StringBuilder(255);

            GetPrivateProfileString(section, key, "", retVal, 255, iniFilePath);

            return retVal.ToString();
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);
            Properties.Settings.Default.Path = textBox2.Text;
            Properties.Settings.Default.Process = textBox1.Text;
            Properties.Settings.Default.Save();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            Form2 fm2 = new Form2();
            fm2.ShowDialog();
            textBox1.Text = fm2.curt;
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox1.Checked == true)
            {
                button1.Enabled = false;
                timer1.Start();
            }
            else
            {
                button1.Enabled = true;
                timer1.Stop();
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            OpenFileDialog fd = new OpenFileDialog();
            fd.Filter = "Dynamic Link Library (.dll)|*.dll";
            fd.FilterIndex = 1;
            fd.ShowDialog();
            textBox2.Text = fd.FileName;
        }

        public Int32 GetProcessId(String proc)
        {
            Process[] ProcList;
            ProcList = Process.GetProcessesByName(proc);
            return ProcList[0].Id;
        }

        public void InjectDLL(IntPtr hProcess, String strDLLName)
        {
            IntPtr bytesout;

            // Length of string containing the DLL file name +1 byte padding
            Int32 LenWrite = strDLLName.Length + 1;
            // Allocate memory within the virtual address space of the target process
            IntPtr AllocMem = (IntPtr)VirtualAllocEx(hProcess, (IntPtr)null, (uint)LenWrite, 0x1000, 0x40); //allocation pour WriteProcessMemory

            // Write DLL file name to allocated memory in target process
            WriteProcessMemory(hProcess, AllocMem, strDLLName, (UIntPtr)LenWrite, out bytesout);
            // Function pointer "Injector"
            UIntPtr Injector = (UIntPtr)GetProcAddress(GetModuleHandle("kernel32.dll"), "LoadLibraryA");

            if (Injector == null)
            {
                MessageBox.Show(" Injector Error! \n ");
                // return failed
                return;
            }

            // Create thread in target process, and store handle in hThread
            IntPtr hThread = (IntPtr)CreateRemoteThread(hProcess, (IntPtr)null, 0, Injector, AllocMem, 0, out bytesout);
            // Make sure thread handle is valid
            if (hThread == null)
            {
                //incorrect thread handle ... return failed
                MessageBox.Show(" hThread [ 1 ] Error! \n ");
                return;
            }
            // Time-out is 10 seconds...
            int Result = WaitForSingleObject(hThread, 10 * 1000);
            // Check whether thread timed out...
            if (Result == 0x00000080L || Result == 0x00000102L || Result == 0xFFFFFFFF)
            {
                /* Thread timed out... */
                MessageBox.Show(" hThread [ 2 ] Error! \n ");
                // Make sure thread handle is valid before closing... prevents crashes.
                if (hThread != null)
                {
                    //Close thread in target process
                    CloseHandle(hThread);
                }
                return;
            }
            // Sleep thread for 1 second
            Thread.Sleep(1000);
            // Clear up allocated space ( Allocmem )
            VirtualFreeEx(hProcess, AllocMem, (UIntPtr)0, 0x8000);
            // Make sure thread handle is valid before closing... prevents crashes.
            if (hThread != null)
            {
                //Close thread in target process
                CloseHandle(hThread);
            }
            // return succeeded
            this.Close();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            string createText = textBox1.Text;
            string[] txt = createText.Split('.');

            Int32 ProcID = GetProcessId(txt[0]);
            if (ProcID >= 0)
            {
                IntPtr hProcess = (IntPtr)OpenProcess(0x1F0FFF, 1, ProcID);
                if (hProcess == null)
                {
                    MessageBox.Show("Process not open!");
                    return;
                }
                else
                {
                    WriteINISetting(Application.StartupPath + @"\config.ini", "Injector", "Process", textBox1.Text);
                    WriteINISetting(Application.StartupPath + @"\config.ini", "Injector", "Dll", textBox2.Text);
                    if (checkBox1.Checked) WriteINISetting(Application.StartupPath + @"\config.ini", "Injector", "Autoinject", "1"); else WriteINISetting(Application.StartupPath + @"\config.ini", "Injector", "Autoinject", "0");
                    if (textBox2.Text != "")
                        InjectDLL(hProcess, textBox2.Text);
                }
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            if(System.IO.File.Exists(Application.StartupPath + @"\config.ini"))
            {
                textBox1.Text = ReadINISetting(Application.StartupPath + @"\config.ini", "Injector", "Process");
                textBox2.Text = ReadINISetting(Application.StartupPath + @"\config.ini", "Injector", "Dll");
                if (ReadINISetting(Application.StartupPath + @"\config.ini", "Injector", "Autoinject") == "1")
                    checkBox1.CheckState = CheckState.Checked;
                else
                    checkBox1.CheckState = CheckState.Unchecked;
            }  
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            string createText = textBox1.Text;
            string[] txt = createText.Split('.');
            Int32 ProcID;

            try
            {
               ProcID = GetProcessId(txt[0]);
            
                if (ProcID >= 0)
                {
                    IntPtr hProcess = (IntPtr)OpenProcess(0x1F0FFF, 1, ProcID);
                    if (hProcess == null)
                    {
                   
                        return;
                    }
                    else
                    {
                        WriteINISetting(Application.StartupPath + @"\config.ini", "Injector", "Process", textBox1.Text);
                        WriteINISetting(Application.StartupPath + @"\config.ini", "Injector", "Dll", textBox2.Text);
                        if (checkBox1.Checked) WriteINISetting(Application.StartupPath + @"\config.ini", "Injector", "Autoinject", "1"); else WriteINISetting(Application.StartupPath + @"\config.ini", "Injector", "Autoinject", "0");
                        if(textBox2.Text != "")
                            InjectDLL(hProcess, textBox2.Text);
                    }
                }
            }
            catch { };
        }     
    }
}

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
namespace Process_Monitor
{
    public partial class Service1 : ServiceBase
    {
        List<string> exclusions = new List<string>();
        List<string> f_exclusions = new List<string>();
        Timer timer = new Timer(); // name space(using System.Timers;)
        string user;
        List<Process> masProcs = null;
        List<string> taggedUsers = new List<string>(new string[] { "Rick" });
        public Service1()
        {
            InitializeComponent();

        }

        private void GetExclusions()
        {
            int counter = 0;
            string line;
            
            string filepath = "E:\\ProcessMonitor_data\\exclusions.txt";
            string folderExclusionPath = "E:\\ProcessMonitor_data\\folder_exclusions.txt";
            

            //get the file exclusions

            if (!File.Exists(filepath))
            {
                WriteToLog("Exclusions file not found. No such file: " + filepath);
                return;
            }

            WriteToLog("Exclusions file found.");

            // Read the file and display it line by line.  
            try {
                System.IO.StreamReader file = new System.IO.StreamReader(filepath);
                WriteToLog("File handle granted. Reading file...");
                while ((line = file.ReadLine()) != null)
                {
                    //WriteToLog("Read: " + line+" ("+ line.Replace(".exe", "")+")");
                    exclusions.Add(line.Trim());
                    counter++;
                    //WriteToLog("Count: " + counter);
                }
                file.Close();
            }
            catch(Exception e)
            {
                WriteToLog("Exception: " + e.Message);
            }
            
            WriteToLog(counter+" file exclusions found");

            // get the folder exclusions
            counter = 0;

            if (!File.Exists(folderExclusionPath))
            {
                WriteToLog("Folder exclusions file not found. No such file: " + folderExclusionPath);
                return;
            }

            WriteToLog("Folder exclusions file found.");

            // Read the file and display it line by line.  
            try
            {
                System.IO.StreamReader file = new System.IO.StreamReader(folderExclusionPath);
                WriteToLog("File handle granted. Reading file...");
                while ((line = file.ReadLine()) != null)
                {
                    //WriteToLog("Read: " + line+" ("+ line.Replace(".exe", "")+")");
                    f_exclusions.Add(line.Trim());
                    counter++;
                    //WriteToLog("Count: " + counter);
                }
                file.Close();
            }
            catch (Exception e)
            {
                WriteToLog("Exception: " + e.Message);
            }

            WriteToLog(counter + " folder exclusions found");
        }

        protected override void OnStart(string[] args)
        {
            WriteToLog("Service is started");
            timer.Elapsed += new ElapsedEventHandler(OnElapsedTime);
            timer.Interval = 900000; //number in milisecinds  
            timer.Enabled = true;
            user = WindowsIdentity.GetCurrent().Name;
            user = user.Substring(user.IndexOf(@"\") + 1);
            WriteToLog("Current User: " + user);
            WriteToLog("Timer Interval: " + timer.Interval + " milliseconds");

            GetExclusions();
        }

        protected override void OnStop()
        {
            WriteToLog("Service is stopped");
        }
        
        private void OnElapsedTime(object source, ElapsedEventArgs e)
        {
            /* string fullPath = process.MainModule.FileName;
             * Process.MainWindowTitle (String)
             * Process.ProcessName (String)
             * Process.StartTime (DateTime)
             */

            LogProcess();
            //WriteToLog("Service is recalled");
        }

        private void LogProcess()
        {
            List<Process> nProcs = new List<Process>();
            Process[] processlist = Process.GetProcesses();
            //WriteToLog(processlist.Length + " processess got");
            if(masProcs == null)
            {
                WriteToLog("Filling up list for 1st time.");
                masProcs = new List<Process>(processlist);
                return;
            }
            else
            {
                nProcs = processlist.Where(x => !masProcs.Any(y => x.ProcessName == y.ProcessName)).ToList();
                //WriteToLog("New processes: " + nProcs.Count);
                masProcs = new List<Process>(processlist);
            }
            foreach(Process p in nProcs)
            {
                
                //WriteToLog("i: " + i);
                if (IsEligible(p))
                {
                    //WriteToLog("Detected: " + p.ProcessName + "(" + prUser + ")");
                    try
                    {
                        //WriteToLog("Recording: " + p.ProcessName + "(" + prUser + ")");
                        WriteToFile(p.MainModule.FileName + "," 
                            + p.ProcessName + "," 
                            + p.StartTime.Month+","
                            +p.StartTime.Day+","
                            +(int)p.StartTime.DayOfWeek+","
                            +p.StartTime.Hour+","
                            +p.StartTime.Minute+","
                            +DateTimeOffset.Now.ToUnixTimeSeconds());
                    }
                    catch (Exception e)
                    {
                        WriteToLog("Exception: " +p.ProcessName+": "+e.Message);
                    }
                }

            }
        }

        private bool IsEligible(Process p)
        {
            string prUser = GetProcessUser(p);
            if (!(prUser.Equals(user) || taggedUsers.Contains(prUser)))
                return false;
            if (exclusions.Contains(p.ProcessName))
                return false;
            foreach (string s in f_exclusions)
                if (p.MainModule.FileName.Contains(s))
                    return false;
            return true;
        }

        public void WriteToLog(string Message)
        {
            string path = AppDomain.CurrentDomain.BaseDirectory + "\\Logs";
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            string filepath = AppDomain.CurrentDomain.BaseDirectory + "\\Logs\\ServiceLog_" + DateTime.Now.Date.ToShortDateString().Replace('/', '_') + ".txt";
            if (!File.Exists(filepath))
            {
                // Create a file to write to.   
                using (StreamWriter sw = File.CreateText(filepath))
                {
                    sw.WriteLine(DateTime.Now+": "+Message);
                }
            }
            else
            {
                using (StreamWriter sw = File.AppendText(filepath))
                {
                    sw.WriteLine(DateTime.Now + ": " + Message);
                }
            }
        }

        public void WriteToFile(string text)
        {
            string path = "E:\\ProcessMonitor_data";
            if (!Directory.Exists(path))
            {
                WriteToLog("Data directory doesn't exist. Creating new directory.");
                Directory.CreateDirectory(path);
            }
            string filepath = path+"\\data.csv";
            if (!File.Exists(filepath))
            {
                WriteToLog("Creating data file.");
                // Create a file to write to.   
                using (StreamWriter sw = File.CreateText(filepath))
                {
                    sw.WriteLine(text);
                }
            }
            else
            {
                //WriteToLog("Data file already exists. Appending...");
                using (StreamWriter sw = File.AppendText(filepath))
                {
                    sw.WriteLine(text);
                }
            }
        }

        private static string GetProcessUser(Process process)
        {
            IntPtr processHandle = IntPtr.Zero;
            try
            {
                OpenProcessToken(process.Handle, 8, out processHandle);
                WindowsIdentity wi = new WindowsIdentity(processHandle);
                string user = wi.Name;
                return user.Contains(@"\") ? user.Substring(user.IndexOf(@"\") + 1) : user;
            }
            catch
            {
                return null;
            }
            finally
            {
                if (processHandle != IntPtr.Zero)
                {
                    CloseHandle(processHandle);
                }
            }
        }

        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool OpenProcessToken(IntPtr ProcessHandle, uint DesiredAccess, out IntPtr TokenHandle);
        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool CloseHandle(IntPtr hObject);
    }
}



/* create quicklaunch icon on taskbar
 * using IWshRuntimeLibrary;
WshShell = new WshShellClass();
IWshRuntimeLibrary.IWshShortcut qlShortcut;
qlShortcut = (IWshRuntimeLibrary.IWshShortcut)WshShell.CreateShortcut(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\Microsoft\\Internet Explorer\\Quick Launch\\MyShortcut.lnk");
qlShortcut.TargetPath = Application.ExecutablePath;
qlShortcut.Description = "Application name, blabla";
qlShortcut.IconLocation = Application.StartupPath + @"\app.ico";
qlShortcut.Save()*/

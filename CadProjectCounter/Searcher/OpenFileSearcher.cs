using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;


namespace CadProjectCounter.Searcher
{
    public class OpenFileSearcher
    {
        static int m_nLastProcId = 0;
        static Process targetProcess = null;
        static IntPtr targetProcessHandle = IntPtr.Zero;

        internal static string[] FindFiles(string application)
        {
            List<string> files = new List<string>();
            string name = "";
            foreach (Process proc in Process.GetProcessesByName(application))
            {
                targetProcess = proc;
                targetProcessHandle = Win32API.OpenProcess(Win32API.ProcessAccessFlags.DupHandle, false, targetProcess.Id);
                List<Win32API.SYSTEM_HANDLE_INFORMATION> lstHandles = CustomAPI.GetHandles(targetProcess);
                for (int nIndex = 0; nIndex < lstHandles.Count; nIndex++)
                {
                    name = GetFileDetails(lstHandles[nIndex]).Name;
                    if (name != "")
                    {
                        files.Add(name);
                    }

                }

                Win32API.CloseHandle(targetProcessHandle);
            }

            return files.ToArray();
        }

        private static FileDetails GetFileDetails(Win32API.SYSTEM_HANDLE_INFORMATION sYSTEM_HANDLE_INFORMATION)
        {
            FileDetails fd = new FileDetails();
            fd.Name = "";
            IntPtr ipHandle = IntPtr.Zero;
            Win32API.OBJECT_BASIC_INFORMATION objBasic = new Win32API.OBJECT_BASIC_INFORMATION();
            IntPtr ipBasic = IntPtr.Zero;
            Win32API.OBJECT_TYPE_INFORMATION objObjectType = new Win32API.OBJECT_TYPE_INFORMATION();
            IntPtr ipObjectType = IntPtr.Zero;
            Win32API.OBJECT_NAME_INFORMATION objObjectName = new Win32API.OBJECT_NAME_INFORMATION();
            IntPtr ipObjectName = IntPtr.Zero;
            string strObjectTypeName = "";
            string strObjectName = "";
            int nLength = 0;
            int nReturn = 0;
            IntPtr ipTemp = IntPtr.Zero;

            ipHandle = targetProcessHandle;
            //OpenProcessForHandle(sYSTEM_HANDLE_INFORMATION.ProcessID);
            if (!Win32API.DuplicateHandle(targetProcessHandle, sYSTEM_HANDLE_INFORMATION.Handle, Win32API.GetCurrentProcess(), out ipHandle, 0, false, Win32API.DUPLICATE_SAME_ACCESS)) return fd;

            ipBasic = Marshal.AllocHGlobal(Marshal.SizeOf(objBasic));
            Win32API.NtQueryObject(ipHandle, (int)Win32API.ObjectInformationClass.ObjectBasicInformation, ipBasic, Marshal.SizeOf(objBasic), ref nLength);
            objBasic = (Win32API.OBJECT_BASIC_INFORMATION)Marshal.PtrToStructure(ipBasic, objBasic.GetType());
            Marshal.FreeHGlobal(ipBasic);


            ipObjectType = Marshal.AllocHGlobal(objBasic.TypeInformationLength);
            nLength = objBasic.TypeInformationLength;
            while ((uint)(nReturn = Win32API.NtQueryObject(ipHandle, (int)Win32API.ObjectInformationClass.ObjectTypeInformation, ipObjectType, nLength, ref nLength)) == Win32API.STATUS_INFO_LENGTH_MISMATCH)
            {
                Marshal.FreeHGlobal(ipObjectType);
                ipObjectType = Marshal.AllocHGlobal(nLength);
            }

            objObjectType = (Win32API.OBJECT_TYPE_INFORMATION)Marshal.PtrToStructure(ipObjectType, objObjectType.GetType());
            if (Is64Bits())
            {
                ipTemp = new IntPtr(Convert.ToInt64(objObjectType.Name.Buffer.ToString(), 10) >> 32);
            }
            else
            {
                ipTemp = objObjectType.Name.Buffer;
            }

            strObjectTypeName = Marshal.PtrToStringUni(ipTemp, objObjectType.Name.Length >> 1);
            Marshal.FreeHGlobal(ipObjectType);
            if (strObjectTypeName != "File") return fd;

            nLength = objBasic.NameInformationLength;
            ipObjectName = Marshal.AllocHGlobal(nLength);
            while ((uint)(nReturn = Win32API.NtQueryObject(ipHandle, (int)Win32API.ObjectInformationClass.ObjectNameInformation, ipObjectName, nLength, ref nLength)) == Win32API.STATUS_INFO_LENGTH_MISMATCH)
            {
                Marshal.FreeHGlobal(ipObjectName);
                ipObjectName = Marshal.AllocHGlobal(nLength);
            }
            objObjectName = (Win32API.OBJECT_NAME_INFORMATION)Marshal.PtrToStructure(ipObjectName, objObjectName.GetType());

            if (Is64Bits())
            {
                ipTemp = new IntPtr(Convert.ToInt64(objObjectName.Name.Buffer.ToString(), 10) >> 32);
            }
            else
            {
                ipTemp = objObjectName.Name.Buffer;
            }

            byte[] baTemp = new byte[nLength];
            Win32API.CopyMemory(baTemp, ipTemp, (uint)nLength);

            if (Is64Bits())
            {
                strObjectName = Marshal.PtrToStringUni(new IntPtr(ipTemp.ToInt64()));
            }
            else
            {
                strObjectName = Marshal.PtrToStringUni(new IntPtr(ipTemp.ToInt32()));
            }

            Marshal.FreeHGlobal(ipObjectName);
            Win32API.CloseHandle(ipHandle);

            fd.Name = GetRegularFileNameFromDevice(strObjectName);
            return fd;
        }

        public static string GetRegularFileNameFromDevice(string strRawName)
        {
            string strFileName = strRawName;
            foreach (string strDrivePath in Environment.GetLogicalDrives())
            {
                StringBuilder sbTargetPath = new StringBuilder(Win32API.MAX_PATH);
                if (Win32API.QueryDosDevice(strDrivePath.Substring(0, 2), sbTargetPath, Win32API.MAX_PATH) == 0)
                {
                    return strRawName;
                }
                string strTargetPath = sbTargetPath.ToString();
                if (strFileName.StartsWith(strTargetPath))
                {
                    strFileName = strFileName.Replace(strTargetPath, strDrivePath.Substring(0, 2));
                    break;
                }
            }
            return strFileName;
        }

        static bool Is64Bits()
        {
            return Marshal.SizeOf(typeof(IntPtr)) == 8 ? true : false;
        }

        private static void OpenProcessForHandle(int p)
        {
            if (p != m_nLastProcId)
            {
                Win32API.CloseHandle(targetProcessHandle);
                targetProcessHandle = Win32API.OpenProcess(Win32API.ProcessAccessFlags.DupHandle, false, p);
                m_nLastProcId = p;
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace building_bms
{
    class zk
    {
        IntPtr h = IntPtr.Zero;

        [DllImport("C:\\Windows\\SysWOW64\\plcommpro.dll", EntryPoint = "Connect")]
        public static extern IntPtr Connect(string Parameters);
        [DllImport("plcommpro.dll", EntryPoint = "PullLastError")]
        public static extern int PullLastError();

        public void connect()
        {
            string str = "protocol=TCP,ipaddress=192.168.1.201,port=4370,timwout=5000,passwd=";
            int ret = 0;
            if (IntPtr.Zero == h)
            {
                h = Connect(str);
            }
            else
            {
                ret = PullLastError();
            }
        }

        [DllImport("plcommpro.dll", EntryPoint = "Disconnect")]
        public static extern void Disconnect(IntPtr h);

        public void disconnect()
        {
            if (IntPtr.Zero != h)
            {
                Disconnect(h);
                h = IntPtr.Zero;
            }
            return;
        }

        [DllImport("plcommpro.dll", EntryPoint = "GetDeviceParam")]
        public static extern int GetDeviceParam(IntPtr h, ref byte buffer, int buffersize, string itemvalues);

        public string getdeviceparam()
        {
            if (IntPtr.Zero != h)
            {
                int ret = 0, i = 0;
                int BUFFERSIZE = 10 * 1024 * 1024;
                byte[] buffer = new byte[BUFFERSIZE];
                //int lv_sel_count = lsvselparam.Items.Count;
                string str = "LockCount,ReaderCount,Door1SensorType,Door2SensorType,Door3SensorType,Door4SensorType,Door1VerifyType,Door2VerifyType,Door3VerifyType,Door4VerifyType,";
                string tmp = null;
                //string[] value = null;
               
                //MessageBox.Show(str);
                ret = GetDeviceParam(h, ref buffer[0], BUFFERSIZE, str);       //obtain device's param value
                if (ret >= 0)
                {
                   return tmp = Encoding.Default.GetString(buffer);                    
                }
                else
                {
                    return null;
                }
            }
            else
            {
                return null;
            }
        }

        [DllImport("plcommpro.dll", EntryPoint = "GetDeviceData")]
        public static extern int GetDeviceData(IntPtr h, ref byte buffer, int buffersize, string tablename, string filename, string filter, string options);

        public string[][] getrecords()
        {
            int ret = 0;
            int BUFFERSIZE = 1 * 1024 * 1024;
            byte[] buffer = new byte[BUFFERSIZE];
            string options = "";
            string tmp = null;
            options = "NewRecord";
            if (IntPtr.Zero != h)
            {
                ret = GetDeviceData(h, ref buffer[0], BUFFERSIZE, "transaction", "*", "", options);
            }
            else
            {
                return null;
            }
            if (ret >= 0)
            {
                tmp = Encoding.Default.GetString(buffer);
                string[] tmp2 = Regex.Split(tmp, "\r\n");
                string[][] tmp3 = new string[tmp2.Length-1][];
                for (int i=1; i<tmp3.GetLength(0); i++)
                {
                    string[] temp = tmp2[i].Split(',');
                        tmp3[i] = new string[]{
                            temp[0],
                            temp[1],
                            temp[2],
                            temp[3],
                            temp[4],
                            temp[5],
                            parsDate(temp[6])
                        };
                }
                return tmp3;
            }
            else
            {
                return null;
            }
        }

        [DllImport("plcommpro.dll", EntryPoint = "GetRTLog")]
        public static extern int GetRTLog(IntPtr h, ref byte buffer, int buffersize);

        public string livelog()
        {
            int ret = 0, i = 0, buffersize = 256;
            string str = "";
            string[] tmp = null;
            byte[] buffer = new byte[256];

            if (IntPtr.Zero != h)
            {

                ret = GetRTLog(h, ref buffer[0], buffersize);
                if (ret >= 0)
                {
                    str = Encoding.Default.GetString(buffer);
                    return str;
                    //tmp = str.Split(',');
                    //MessageBox.Show(tmp[0]);
                    
                }
                else
                {
                    return null;
                }
            }
            else
            {
                return null;
            }
        }

        public string parsDate(string txt)
        {
            int time = Convert.ToInt32(txt);
            int second = time % 60;
            int min = (time /= 60) % 60;
            int hour = (time /= 60) % 24;
            int day = ((time /= 24) % 31) + 1;
            int month = (time /= 31) % 12 + 1;
            int year = (time /= 12) + 2000;
            return year.ToString("0000") + "-" + month.ToString("00") + "-" + day.ToString("00") + " " + hour.ToString("00") + ":" + min.ToString("00") + ":" + second.ToString("00");
        }
    }
}

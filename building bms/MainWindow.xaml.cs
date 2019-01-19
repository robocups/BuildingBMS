using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using static building_bms.protocol;
using System.IO;

namespace building_bms
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
        BrushConverter bc = new BrushConverter();
        int ct = 7, netloglines = 0;
        int[] switchs = new int[22];
        int[] switchs2 = new int[8];
        int[] fcs = new int[16];
        string[] holidaysList;
        string currentMode = "deactive";

        //switches ids or relay channels for lighting
        int[,] sid =
        {
            //unit 1 light switches

            {6,7,4,5,1,2,3,13,14,12,10,11,8,9,15,16}
        };
        Dictionary<int,int[]> channels = new Dictionary<int, int[]>();
        int[,] module = {
            //type 1 = lighting, type 2 = HVAC, type 3 = contactor
            {1,235,12},
            {2,6,4},
            //{2,7,8},
            //{2,8,4},
            //{2,9,4} 
        };
        int lightChannels=0, hvacChannels=0;
        int subnet = 1;
        Dictionary<string, int[]> switches = new Dictionary<string, int[]>()
        {
            {"sw1",new int[]{235,1} },
            {"sw2",new int[]{235,2} },
            {"sw3",new int[]{235,3} },
            {"sw4",new int[]{235,4} },
            {"sw5",new int[]{235,1} },
            {"sw6",new int[]{235,2} },
            {"sw7",new int[]{235,5} },

            {"sw8",new int[]{0,4} },
            {"sw9",new int[]{0,3} },
            {"sw10",new int[]{0,0} },
            {"sw11",new int[]{0,7} },
            {"sw12",new int[]{0,8} },
            {"sw13",new int[]{0,2} },
            {"sw14",new int[]{0,1} },
            {"sw15",new int[]{0,6} },

            {"sp1",new int[]{0,6} },
            {"sp2",new int[]{0,6} },
            {"sp3",new int[]{0,6} },
            {"sp4",new int[]{0,6} },
            {"sp5",new int[]{0,6} },

            {"Sockets",new int[]{6,0} },

            {"fc1", new int[]{6,3,2,1 } },
            {"fc2", new int[]{6,3,2,1 } },
            {"fc3", new int[]{0,3,2,1 } },
            {"fc4", new int[]{0,3,2,1 } },
            {"fc5", new int[]{0,3,2,1 } },
        };

        int[,] speaker =
        {
            {2,1,4,3,5}
        };
        //devices ids or module ids
        int[,] did =
        {
            //[unit 1] : lighting relay - speakers and socket relay - AC1 - AC2 - smartswitches1 - smartswitches2 - power meter
            {4,3,7,8,115,114,40 }
        };
        Dictionary<string, string> doorEvents = new Dictionary<string, string>
        {
            {"34","Unregistered Finger" },
            {"1","Opened Normally" },
        };
        bool direct = true;
        bool active=false, preactive=false, postactive=false, deactive=false, preactiveLight=false,postactiveHall=false, deactiveHolidays=false, deactiveFridays = false, sendMyTime = false;
        int activeStopH =-1, activeStopM = -1,preactiveStopH =-1, preactiveStopM = -1, postactiveStopH = -1, postactiveStopM = -1;
        int activeStartH=-1, activeStartM=-1, preactiveStartH=-1, preactiveStartM=-1, postactiveStartH=-1, postactiveStartM=-1, postactiveFmode=-1,preactiveFmode=-1; 

        double volt, amper, kW;
        public string[] data;

        //timers flags
        bool deactiveDone = false, extratime_reset=false, timerCommand=false;

        database db = new database();
        StreamWriter sw = new StreamWriter(AppDomain.CurrentDomain.BaseDirectory + "\\log.txt", true);
        Timer myTimer = new Timer(5000);// every 5 secs
        Timer inbioTimer = new Timer(1000);// every secs
        Timer hourlyTimer = new Timer(60*60000);// every mins
        Timer deactiveTimer = new Timer(30000);// every 30 secs

        public MainWindow()
        {
            InitializeComponent();
            loadSettings();
            netlog.Visibility = Visibility.Visible;
            server();
            UI();
            fillChannels();
            myTimer.Elapsed += new ElapsedEventHandler(scheduler);
            inbioTimer.Elapsed += new ElapsedEventHandler(log);
            hourlyTimer.Elapsed += new ElapsedEventHandler(hourlyJobs);
            deactiveTimer.Elapsed += new ElapsedEventHandler(deactiveJobs);
            //inbioTimer.Enabled = true;
            myTimer.Enabled = true;
            
            //accTab.Visibility = Visibility.Hidden;
            //generalTab.Visibility = Visibility.Hidden;
            //entrerTab.Visibility = Visibility.Visible;
            //itTab.Visibility = Visibility.Hidden;
            //techTab.Visibility = Visibility.Hidden;
            //secTab.Visibility = Visibility.Hidden;
            //ceoTab.Visibility = Visibility.Visible;
            //servTab.Visibility = Visibility.Hidden;
            //powerTab.Visibility = Visibility.Hidden;
            //tabControl.SelectedIndex = 1;
            //login_grid.Visibility = Visibility.Hidden;

        }

        public void loadSettings()
        {
            active_switch.IsChecked = active = Properties.Settings.Default.active;
            preactive_switch.IsChecked = preactive = Properties.Settings.Default.preactive;
            deactive_switch.IsChecked = deactive = Properties.Settings.Default.deactive;
            postactive_switch.IsChecked = postactive = Properties.Settings.Default.postactive;
            preactive_fcmode.SelectedIndex = preactiveFmode = Properties.Settings.Default.preactiveFmode;
            preactive_enterlight.IsChecked = preactiveLight = Properties.Settings.Default.preactiveEnterLight;
            postactive_fcmode.SelectedIndex = postactiveFmode = Properties.Settings.Default.postactiveFmode;
            postactive_offhal.IsChecked = postactiveHall = Properties.Settings.Default.postactiveHallLight;
            sendTime.IsChecked = sendMyTime = Properties.Settings.Default.sendTime;
            deactive_fridays.IsChecked = deactiveFridays = Properties.Settings.Default.deactiveFridays;
            deactive_holidays.IsChecked = deactiveHolidays = Properties.Settings.Default.deactiveHolidays;
            holidays.Text = Properties.Settings.Default.holidays;
            if (holidays.Text.Length > 0)
            {
                holidaysList = holidays.Text.Split(',');
                for (int i = 0; i < holidaysList.Length; i++)
                {
                    writelog("holidays:" + holidaysList[i]);
                }
            }

            activeStartH = Properties.Settings.Default.activeStartH;
            active_starthour.Text = activeStartH.ToString();
            activeStartM = Properties.Settings.Default.activeStartM;
            active_startmin.Text = activeStartM.ToString();

            activeStopH = Properties.Settings.Default.activeStopH;
            active_stophour.Text = activeStopH.ToString();
            activeStopM = Properties.Settings.Default.activeStopM;
            active_stopmin.Text = activeStopM.ToString();

            preactiveStartH = Properties.Settings.Default.preactiveStartH;
            preactive_starthour.Text = preactiveStartH.ToString();
            preactiveStartM = Properties.Settings.Default.preactiveStartM;
            preactive_startmin.Text = preactiveStartM.ToString();

            preactiveStopH = Properties.Settings.Default.preactiveStopH;
            preactive_stophour.Text = preactiveStopH.ToString();
            preactiveStopM = Properties.Settings.Default.preactiveStopM;
            preactive_stopmin.Text = preactiveStopM.ToString();

            postactiveStartH = Properties.Settings.Default.postactiveStartH;
            postactive_starthour.Text = postactiveStartH.ToString();
            postactiveStartM = Properties.Settings.Default.postactiveStartM;
            postactive_startmin.Text = postactiveStartM.ToString();

            postactiveStopH = Properties.Settings.Default.postactiveStopH;
            postactive_stophour.Text = postactiveStopH.ToString();
            postactiveStopM = Properties.Settings.Default.postactiveStopM;
            postactive_stopmin.Text = postactiveStopM.ToString();
            //#login
            //login_grid.Visibility = Visibility.Visible;

            //#label setting
            z1Title.Content = entrerTab.Header = z1.Text = Properties.Settings.Default.z1;
            z2Title.Content = itTab.Header = z2.Text = Properties.Settings.Default.z2;
            z3Title.Content = techTab.Header = z3.Text = Properties.Settings.Default.z3;
            z4Title.Content = accTab.Header = z4.Text = Properties.Settings.Default.z4;
            z5Title.Content = secTab.Header = z5.Text = Properties.Settings.Default.z5;
            z6Title.Content = ceoTab.Header = z6.Text = Properties.Settings.Default.z6;
            //z7Title.Content = colTab.Header = z7.Text = Properties.Settings.Default.z7;
            z8Title.Content = servTab.Header = z8.Text = Properties.Settings.Default.z8;
        }

        public void fillChannels()
        {

            for (int i=0; i<module.GetLength(0); i++)
            {
                int[] temp = ArrayGenerator(module[i, 2]);
                channels.Add(module[i, 1], temp);
            }
        }
        public int[] ArrayGenerator(int len)
        {
            int[] t = new int[len];
            for (int i = 0; i < len; i++)
                t[i] = 0;
            return t;
        }

        public int getDevCH(int id)
        {
            int[] temp = channels[id];
            return temp.Length;
        }
        public void getchannels()
        {
            for (int i=0; i<module.Length; i++)
            {
                if (module[i, 0] == 1)
                    lightChannels = +module[i, 2];
                else if (module[i, 0] == 2)
                    hvacChannels = +module[i, 2];
            }
        }
        public bool islightModule(int id)
        {
            //for (int i=0; i<module.GetLength(0); i++)
            //{
            //    if (module[i, 1] == id && module[i, 0] == 1)
            //        return true;  
            //}
            //return false;
            foreach (var kvp in switches.ToArray())
            {
                if ((kvp.Value)[0]== id && kvp.Key.Substring(0,1)=="s")
                {
                    return true;
                }
            }
            return false;
        }
        public bool ishvacModule(int id)
        {
            //for (int i = 0; i < module.GetLength(0); i++)
            //{
            //    if (module[i, 1] == id && module[i, 0] == 2)
            //        return true;
            //}
            //return false;
            foreach (var kvp in switches.ToArray())
            {
                if ((kvp.Value)[0] == id && kvp.Key.Substring(0, 1) == "f")
                {
                    return true;
                }
            }
            return false;
        }
        public void setSwitch(ToggleSwitch sw)
        {
            Application.Current.Dispatcher.Invoke(new Action(() =>
            {


            }));
        }

        public void activeMethod()
        {
            deactiveDone = false;
            com c = new com();
            writelog(">>>Active mode:))))");
        }

        public async void preactiveMethod()
        {
            deactiveDone = false;
            com c = new com();
            writelog(">>>PreActive mode:||||");
            switch (Properties.Settings.Default.preactiveFmode)
            {
                case 0:
                    for (int i = 0; i < fcs.Length; i++)
                    {
                        if (fcs[i] == 1)
                        {
                            allfc_off_btn_Click(null, null);
                            return;
                        }
                    }
                    break;
                case 1:
                    if (fcs[0] != 1 || fcs[3] != 1 || fcs[6] != 1 || fcs[9] != 1 || fcs[12] != 1)
                    {
                        allfc_low_btn_Click(null, null);
                    }
                    break;
                case 2:
                    if (fcs[1] != 1 || fcs[4] != 1 || fcs[7] != 1 || fcs[10] != 1 || fcs[13] != 1)
                    {
                        allfc_med_btn_Click(null, null);
                    }
                    break;
                case 3:
                    if (fcs[3] != 1 || fcs[5] != 1 || fcs[8] != 1 || fcs[11] != 1 || fcs[14] != 1)
                    {
                        allfc_high_btn_Click(null, null);
                    }
                    break;
            }
            if (preactiveLight & switchs[0] == 0)
            {
                c.setswitch(subnet, did[ct - 7, 0], sid[ct - 7, 0], 100, direct);
            }
        }
        public async void postactiveMethod()
        {
            deactiveDone = false;
            com c = new com();
            writelog(">>>PostActive mode:((((");
            switch (Properties.Settings.Default.postactiveFmode)
            {
                case 0:
                    for (int i = 0; i < fcs.Length; i++)
                    {
                        if (fcs[i] == 1)
                        {
                            allfc_off_btn_Click(null, null);
                            return;
                        }
                    }
                    break;
                case 1:
                    if (fcs[0] != 1 || fcs[3] != 1 || fcs[6] != 1 || fcs[9] != 1 || fcs[12] != 1)
                    {
                        allfc_low_btn_Click(null, null);
                    }
                    break;
                case 2:
                    if (fcs[1] != 1 || fcs[4] != 1 || fcs[7] != 1 || fcs[10] != 1 || fcs[13] != 1)
                    {
                        allfc_med_btn_Click(null, null);
                    }
                    break;
                case 3:
                    if (fcs[3] != 1 || fcs[5] != 1 || fcs[8] != 1 || fcs[11] != 1 || fcs[14] != 1)
                    {
                        allfc_high_btn_Click(null, null);
                    }
                    break;
            }
            if (postactiveHall)
            {
                allhallights_off_Click(null, null);
            }
        }

        public async void deactiveMethod()
        {
            writelog(">>>Deactive mode:XXXX");
            if (!deactiveDone)
            {
                com c = new com();
                //for (int i = 0; i < fcs.Length; i++)
                //{
                //    if (fcs[i] == 1)
                //    {

                //        return;
                //    }
                //}
                timerCommand = true;
                await Task.Delay(200);
                allfc_off_btn_Click(null, null);
                await Task.Delay(1500);
                alllights_off_btn_Click(null, null);
                await Task.Delay(400);
                timerCommand = false;
                deactiveDone = true;
            }
        }
        public async void log(object src, ElapsedEventArgs e)
        {
            switch (currentMode)
            {
                case "deactive":

                    break;
                case "active":
                    checkFinger();
                    break;
                case "preactive":

                    break;
                case "postactive":

                    break;
            }
        }
        public async void hourlyJobs(object src, ElapsedEventArgs e)
        {
            com c = new com();
            if (sendMyTime)
            {
                writelog("sending my time!");
                c.sendmytime(7, 10);
            }
        }
        public async void deactiveJobs(object src, ElapsedEventArgs e)
        {
            writelog("deactive timer jobs");
            com c = new com();
            //for (int i = 0; i < fcs.Length; i++)
            //{
            //    if (fcs[i] == 1)
            //    {
            //        allfc_off_btn_Click(null, null);
            //        return;
            //    }
            //}
            timerCommand = true;
            allfc_off_btn_Click(null, null);
            await Task.Delay(3000);
            
            alllights_off_btn_Click(null, null);
            timerCommand = false;
            await Task.Delay(400);
            deactiveTimer.Enabled = false;
            //if (currentMode == "deactive")
            //{
            //    deactiveMethod();
            //}
        }

        public bool IsFriday()
        {
            if (DateTime.Now.ToString("ddd") == "Fri")
            {
                return true;
            }
            return false;
        }
        public bool IsHoliday()
        {
            PersianCalendar pc = new PersianCalendar();
            
            var results = Array.FindIndex(holidaysList, s => s == pc.GetMonth(DateTime.Now).ToString() + "/" + pc.GetDayOfMonth(DateTime.Now).ToString());
            if ( results > -1)
            {
                return true;
            }
            return false;
        }
        public async void scheduler(object src, ElapsedEventArgs e)
        {
            com c = new com();
            c.gettemp(subnet, did[ct - 7, 4], direct);
            await Task.Delay(300);
            c.gettemp(subnet, did[ct - 7, 5], direct);
            await Task.Delay(300);
            c.getvoltage(subnet, did[ct - 7, 6], direct);
            await Task.Delay(300);
            c.getcurrent(subnet, did[ct - 7, 6], direct);
            //# auto on fan coil setting timer
            if ( DateTime.Now.Hour == 0 && !Properties.Settings.Default.extraTimeReset)
            {
                Properties.Settings.Default.extraStartH = -1;
                Properties.Settings.Default.extraStartM = -1;
                Properties.Settings.Default.extraStopH = -1;
                Properties.Settings.Default.extraStopM = -1;
                Properties.Settings.Default.extraTimeReset = true;
                Properties.Settings.Default.Save();
                Properties.Settings.Default.Reload();
                writelog("extra time resetted!");
            }
            if (Properties.Settings.Default.extraStartH >= 0 & Properties.Settings.Default.extraStopH > 0 && checkRange(new TimeSpan(Properties.Settings.Default.extraStartH,0,0), new TimeSpan(Properties.Settings.Default.extraStopH, 59, 59)))
            {
                currentMode = "extratime";
                writelog("it's extra time!");
            }
            else
            {
                if (deactive && deactiveFridays & IsFriday())
                {
                    currentMode = "deactive";
                    writelog("it's friday");
                    deactiveMethod();
                }
                else if (deactive && deactiveHolidays & IsHoliday())
                {
                    currentMode = "deactive";
                    writelog("it's holiday");
                    deactiveMethod();
                }
                else
                {
                    if (active && activeStartH > -1 & activeStartM > -1 & activeStopH > -1 & activeStopM > -1)
                    {
                        if (checkRange(new TimeSpan(activeStartH, activeStartM, 0), new TimeSpan(activeStopH, activeStopM, 59)))
                        {
                            currentMode = "active";
                            // it's active time
                            activeMethod();
                        }
                        else if (preactive && checkRange(new TimeSpan(preactiveStartH, preactiveStartM, 0), new TimeSpan(preactiveStopH, preactiveStopM, 59)))
                        {
                            currentMode = "preactive";
                            // it's preactive time
                            preactiveMethod();
                        }
                        else if (postactive && checkRange(new TimeSpan(postactiveStartH, postactiveStartM, 0), new TimeSpan(postactiveStopH, postactiveStopM, 59)))
                        {
                            currentMode = "postactive";
                            // it's postactive time
                            postactiveMethod();
                        }
                        else if (deactive)
                        {
                            currentMode = "deactive";
                            deactiveMethod();
                        }
                    }
                    else if (deactive)
                    {
                        currentMode = "deactive";
                        deactiveMethod();
                    }
                }
            }
        }

        public bool checkRange(TimeSpan start, TimeSpan end)
        {
            TimeSpan now = DateTime.Now.TimeOfDay;
            if ((now >= start) & (now <= end))
            {
                return true;
                //match found
            }
            return false;
        }
        public async void checkFinger()
        {
            zk z = new zk();
            z.connect();
            string lv = z.livelog();
            if (lv != null && lv != "")
            {
                string[] tmp = lv.Split(',');
                if (tmp[4] != "255")
                {
                    //MessageBox.Show(lv);
                    writelog("Door Control: " + tmp[0].ToString() + " - PN: " + tmp[1].ToString() + " - CardNumber: " + tmp[2].ToString() + " - Door: " + tmp[3].ToString() + " - " + doorEvents[tmp[4].ToString()] + " - " + tmp[5].ToString()=="0"?"Entry":"none");
                }
                else if(tmp[4] == "255" && tmp[1] == "1")
                {
                    writelog("Door Control: Door is closed!");
                    Application.Current.Dispatcher.Invoke(new Action(() =>
                    {
                        door_open.Visibility = Visibility.Hidden;
                    }));
                }
                else if (tmp[4] == "255" && tmp[1] == "2")
                {
                    writelog("Door Control: Door is openned!");
                    Application.Current.Dispatcher.Invoke(new Action(() =>
                    {
                        door_open.Visibility = Visibility.Visible;
                    }));
                }
            }
        }
        public async void UI()
        {

            //if (Properties.Settings.Default.rememberme)
            //{
            //    login(Properties.Settings.Default.username, Properties.Settings.Default.password);
            //}
            //else
            //{
            //    login_grid.Visibility = Visibility.Visible;
            //}
            for (int i = 0; i < 15; i++)
            {
                if (switches["sw" + (i + 1).ToString()][0] == 0)
                    disableSwitch("sw" + (i + 1).ToString());
            }
                
            ct = 7;
            fc1_off.Checked -= fc1_off_Checked;
            fc2_high_off.Checked -= fc2_high_off_Checked;
            fc3_off.Checked -= fc3_off_Checked;
            fc4_off.Checked -= fc4_off_Checked;
            fc5_off.Checked -= fc5_off_Checked;
            fc1_off.IsChecked = fc2_high_off.IsChecked = fc3_off.IsChecked = fc4_off.IsChecked = fc5_off.IsChecked = true;
            fc1_off.Checked += fc1_off_Checked;
            fc2_high_off.Checked += fc2_high_off_Checked;
            fc3_off.Checked += fc3_off_Checked;
            fc4_off.Checked += fc4_off_Checked;
            fc5_off.Checked += fc5_off_Checked;
            await Task.Delay(800);
            for(int i = 0; i<module.GetLength(0); i++)
            {
                com c = new com();
                c.getswitch(subnet, module[i,1], direct);
                await Task.Delay(300);
            }
            
            
            //c.getswitch(subnet, did[ct - 7, 1], direct);
            //await Task.Delay(300);
            //c.getswitch(subnet, did[ct - 7, 2], direct);
            //await Task.Delay(300);
            //c.getswitch(subnet, did[ct - 7, 3], direct);
            //await Task.Delay(300);
            //c.gettemp(subnet, did[ct - 7, 4], direct);
            //await Task.Delay(300);
            //c.gettemp(subnet, did[ct - 7, 5], direct);
            //await Task.Delay(300);
            //c.getvoltage(subnet, did[ct - 7, 6], direct);
            //await Task.Delay(300);
            //c.getcurrent(subnet, did[ct - 7, 6], direct);

        }
        public void disableSwitch(string name)
        {
            switch (name)
            {
                case "sw1":
                    sw1.IsEnabled = false;
                    break;
                case "sw2":
                    sw2.IsEnabled = false;
                    break;
                case "sw3":
                    sw3.IsEnabled = false;
                    break;
                case "sw4":
                    sw4.IsEnabled = false;
                    break;
                case "sw5":
                    sw5.IsEnabled = false;
                    break;
                case "sw6":
                    sw6.IsEnabled = false;
                    break;
                case "sw7":
                    sw7.IsEnabled = false;
                    break;
                case "sw8":
                    sw8.IsEnabled = false;
                    break;
                case "sw9":
                    sw9.IsEnabled = false;
                    break;
                case "sw10":
                    sw10.IsEnabled = false;
                    break;
                case "sw11":
                    sw11.IsEnabled = false;
                    break;
                case "sw12":
                    sw12.IsEnabled = false;
                    break;
                case "sw13":
                    sw13.IsEnabled = false;
                    break;
                case "sw14":
                    sw14.IsEnabled = false;
                    break;
                case "sw15":
                    sw15.IsEnabled = false;
                    break;

                    
            }
        }
        public void writelog(string str)
        {
            long length = new FileInfo(AppDomain.CurrentDomain.BaseDirectory + "\\log.txt").Length;
            //268435456
            //5456
            if (length > 268435456)
            {
                File.Copy(AppDomain.CurrentDomain.BaseDirectory + "\\log.txt", AppDomain.CurrentDomain.BaseDirectory + @"\log backups\log.txt", true);
                File.Move(AppDomain.CurrentDomain.BaseDirectory + @"\log backups\log.txt", AppDomain.CurrentDomain.BaseDirectory + @"\log backups\log-" + DateTime.Now.Year.ToString() + "-" + DateTime.Now.Month.ToString() + "-" + DateTime.Now.Day.ToString() + "-" + DateTime.Now.Hour.ToString() + "-" + DateTime.Now.Minute.ToString() + "-" + DateTime.Now.Second.ToString() + ".txt");
                sw.Close();
                string path = AppDomain.CurrentDomain.BaseDirectory + "\\log.txt";
                File.WriteAllText(path, String.Empty);

                sw = new StreamWriter(AppDomain.CurrentDomain.BaseDirectory + "\\log.txt", true);
            }
            sw.WriteLine(DateTime.Now.ToString() + ": " + str);
            sw.Flush();
            //sw.Close();


        }
        
        private void server()
        {

            UdpClient socket = new UdpClient(6000); // `new UdpClient()` to auto-pick port
                                                    // schedule the first receive operation:
            socket.BeginReceive(new AsyncCallback(getpacket), socket);
        }

        private void getpacket(IAsyncResult result)
        {
            // this is what had been passed into BeginReceive as the second parameter:
            UdpClient socket = result.AsyncState as UdpClient;
            // points towards whoever had sent the message:
            IPEndPoint source = new IPEndPoint(0, 0);
            // get the actual message and fill out the source:
            byte[] message = socket.EndReceive(result, ref source);
            // do what you'd like with `message` here:
            //netlogtextbox.WriteLine("Got " + message.Length + " bytes from " + source);
            //netloglines++;
            //Application.Current.Dispatcher.Invoke(new Action(() =>
            //{
            //    if (netloglines < 50)
            //    {
            //        netlogtextbox.AppendText("Packet: { " + String.Join(" ", message) + " } - bytes from : " + source + "\n");
            //        netlogtextbox.CaretIndex = netlogtextbox.Text.Length;
            //    }
            //    else
            //    {
            //        netlogtextbox.Text = "";
            //        netloglines = 1;
            //        netlogtextbox.AppendText("Packet: { " + String.Join(" ", message) + " } - bytes from : " + source + "\n");
            //        netlogtextbox.CaretIndex = netlogtextbox.Text.Length;
            //    }

            //}));
            msgproccess(message);
            // schedule the next receive operation once reading is done:
            socket.BeginReceive(new AsyncCallback(getpacket), socket);
        }


        //
        //parse and proccess cottage network messages (for direct mod)
        public async void msgproccess(byte[] msg)
        {

            int clength = msg.Length - 27;
            byte[] c = new byte[clength];
            Packet p = new Packet();
            Array.Copy(msg, 14, p.Data, 0, 11);
            Array.Copy(msg, 25, c, 0, clength);

            p.Content = c;
            UInt16 op = (p.OperationCode);
            string opcode = String.Format("0x{0:X}", op);
            //writelog(" { " + opcode.ToString() + " -- " + String.Join(" ", p.Data) + " }\n");
            Application.Current.Dispatcher.Invoke(new Action(() =>
            {
                netloglines++;
                if (netloglines < 50)
                {
                    netlogtextbox.AppendText("op: { " + opcode.ToString() + "--" + String.Join(" ", p.Data) + " }\n");
                    netlogtextbox.CaretIndex = netlogtextbox.Text.Length;
                }
                else
                {
                    netlogtextbox.Text = "";
                    netloglines = 1;
                    netlogtextbox.AppendText("op: { " + String.Join(" ", p.Data) + " }\n");
                    netlogtextbox.CaretIndex = netlogtextbox.Text.Length;
                }
            }));
            writelog("op: { " + opcode.ToString() + "--" + String.Join(" ", p.Data) + " }");
            switch (opcode)
            {
                case "0x32": //single channel control response"
                    int channelno = Convert.ToInt16(p.Data[11]);
                    if (islightModule(Convert.ToInt16(p.OriginalDeviceID)))
                    {
                        int[] s = channels[Convert.ToInt16(p.OriginalDeviceID)];
                        s[channelno-1]= (Convert.ToInt16(p.Data[13]) == 0) ? 0 : 1;
                        channels[Convert.ToInt16(p.OriginalDeviceID)] = s;
                        Application.Current.Dispatcher.Invoke(new Action(() =>
                        {
                            refsw(Convert.ToInt16(p.OriginalDeviceID));
                        }));
                    }
                    else if (ishvacModule(Convert.ToInt16(p.OriginalDeviceID)))
                    {
                        int[] s = channels[Convert.ToInt16(p.OriginalDeviceID)];
                        s[channelno - 1] = (Convert.ToInt16(p.Data[13]) == 0) ? 0 : 1;
                        channels[Convert.ToInt16(p.OriginalDeviceID)] = s;
                        Application.Current.Dispatcher.Invoke(new Action(() =>
                        {
                            reffc(Convert.ToInt16(p.OriginalDeviceID));
                        }));
                    }
                    //if (Convert.ToInt16(p.OriginalDeviceID) == did[ct - 7, 0] && Convert.ToInt16(p.Data[12]) == 248)
                    //{
                    //    switchs[channelno - 1] = (Convert.ToInt16(p.Data[13]) == 0) ? 0 : 1;

                    //    Application.Current.Dispatcher.Invoke(new Action(() =>
                    //    {
                    //        refsw(subnet);
                    //    }));
                    //    if (deactiveDone && currentMode == "deactive" & deactiveTimer.Enabled == false & !timerCommand)
                    //    {
                    //        deactiveTimer.Enabled = true;
                    //    }
                    //}

                    //else if (Convert.ToInt16(p.OriginalDeviceID) == did[ct - 7, 0] && Convert.ToInt16(p.Data[12]) == 245)
                    //{
                    //    switchs[channelno - 1] = (Convert.ToInt16(p.Data[13]) == 0) ? 1 : 0;

                    //    Application.Current.Dispatcher.Invoke(new Action(() =>
                    //    {
                    //        refsw(subnet);
                    //    }));
                    //}
                    //else if (Convert.ToInt16(p.OriginalDeviceID) == did[ct - 7, 1] && Convert.ToInt16(p.Data[12]) == 248)
                    //{
                    //    switchs2[channelno - 1] = (Convert.ToInt16(p.Data[13]) == 0) ? 0 : 1;

                    //    Application.Current.Dispatcher.Invoke(new Action(() =>
                    //    {
                    //        refsw2(subnet);
                    //    }));
                    //}
                    //else if (Convert.ToInt16(p.OriginalDeviceID) == did[ct - 7, 1] && Convert.ToInt16(p.Data[12]) == 245)
                    //{
                    //    switchs2[channelno - 1] = (Convert.ToInt16(p.Data[13]) == 0) ? 1 : 0;

                    //    Application.Current.Dispatcher.Invoke(new Action(() =>
                    //    {
                    //        refsw2(subnet);
                    //    }));
                    //}
                    //else if (Convert.ToInt16(p.OriginalDeviceID) == did[ct - 7, 2] && Convert.ToInt16(p.Data[12]) == 248)
                    //{
                    //    //fcs[channelno - 1] = (Convert.ToInt16(p.Data[13]) == 0) ? 0 : 1;

                    //    //Application.Current.Dispatcher.Invoke(new Action(() =>
                    //    //{
                    //    //    reffc(1);
                    //    //}));
                    //    com c32 = new com();
                    //    c32.getswitch(subnet, did[ct - 7, 2], direct);

                    //    if (deactiveDone && currentMode == "deactive" & deactiveTimer.Enabled == false & !timerCommand)
                    //    {
                    //        deactiveTimer.Enabled = true;
                    //    }

                    //}
                    //else if (Convert.ToInt16(p.OriginalDeviceID) == did[ct - 7, 2] && Convert.ToInt16(p.Data[12]) == 245)
                    //{
                    //    //fcs[channelno - 1] = (Convert.ToInt16(p.Data[13]) == 0) ? 1 : 0;

                    //    //Application.Current.Dispatcher.Invoke(new Action(() =>
                    //    //{
                    //    //    reffc(1);
                    //    //}));
                    //    com c33 = new com();
                    //    c33.getswitch(subnet, did[ct - 7, 2], direct);
                    //}
                    //else if (Convert.ToInt16(p.OriginalDeviceID) == did[ct - 7, 3] && Convert.ToInt16(p.Data[12]) == 248)
                    //{

                    //    //fcs[channelno +7] = (Convert.ToInt16(p.Data[13]) == 0) ? 0 : 1;

                    //    //Application.Current.Dispatcher.Invoke(new Action(() =>
                    //    //{
                    //    //    reffc(2);
                    //    //}));
                    //    com c34 = new com();
                    //    c34.getswitch(subnet, did[ct - 7, 3], direct);
                    //    if (deactiveDone && currentMode == "deactive" & deactiveTimer.Enabled == false & !timerCommand)
                    //    {
                    //        deactiveTimer.Enabled = true;
                    //    }

                    //}
                    //else if (Convert.ToInt16(p.OriginalDeviceID) == did[ct - 7, 3] && Convert.ToInt16(p.Data[12]) == 245)
                    //{
                    //    //fcs[channelno + 7] = (Convert.ToInt16(p.Data[13]) == 0) ? 1 : 0;

                    //    //Application.Current.Dispatcher.Invoke(new Action(() =>
                    //    //{
                    //    //    reffc(2);
                    //    //}));
                    //    com c35 = new com();
                    //    c35.getswitch(subnet, did[ct - 7, 3], direct);

                    //}

                    break;
                case "0x34"://channels state
                    int channelnumber = getDevCH(Convert.ToInt16(p.OriginalDeviceID));
                    if (islightModule(Convert.ToInt16(p.OriginalDeviceID)) && Convert.ToInt16(p.Data[11])==channelnumber)
                    {
                        int[] s = new int[channelnumber];
                        for (int i = 12; i < 12 + channelnumber; i++)
                        {
                            s[i - 12] = (Convert.ToInt16(p.Data[i]) == 100) ? 1 : 0;
                        }
                        
                        channels[Convert.ToInt16(p.OriginalDeviceID)] = s;
                        Application.Current.Dispatcher.Invoke(new Action(() =>
                        {
                            refsw(Convert.ToInt16(p.OriginalDeviceID));
                        }));
                    }
                    else if (ishvacModule(Convert.ToInt16(p.OriginalDeviceID)) && Convert.ToInt16(p.Data[11]) == channelnumber)
                    {
                        int[] s = new int[channelnumber];
                        for (int i = 12; i < 12 + channelnumber; i++)
                        {
                            s[i - 12] = (Convert.ToInt16(p.Data[i]) == 100) ? 1 : 0;
                        }

                        channels[Convert.ToInt16(p.OriginalDeviceID)] = s;
                        Application.Current.Dispatcher.Invoke(new Action(() =>
                        {
                            reffc(Convert.ToInt16(p.OriginalDeviceID));
                        }));
                    }
                    //    channelno = Convert.ToInt16(p.Data[11]);
                    //if (Convert.ToInt16(p.OriginalDeviceID) == did[ct - 7, 0] & ct != 0)
                    //{
                    //    for (int i = 12; i < 12 + 16; i++)
                    //    {
                    //        switchs[i - 12] = (Convert.ToInt16(p.Data[i]) == 100) ? 1 : 0;
                    //    }
                    //    Application.Current.Dispatcher.Invoke(new Action(() =>
                    //    {
                    //        refsw(subnet);
                    //    }));
                    //}
                    //else if(Convert.ToInt16(p.OriginalDeviceID) == did[ct - 7, 1] & ct != 0)
                    //{
                    //    for (int i = 12; i < 12 + 8; i++)
                    //    {
                    //        switchs2[i - 12] = (Convert.ToInt16(p.Data[i]) == 100) ? 1 : 0;
                    //    }
                    //    Application.Current.Dispatcher.Invoke(new Action(() =>
                    //    {
                    //        refsw2(subnet);
                    //    }));
                    //}
                    //else if (Convert.ToInt16(p.OriginalDeviceID) == did[ct - 7, 2])
                    //{
                    //    for (int i = 12; i < 12 + 8; i++)
                    //    {
                    //        fcs[i - 12] = (Convert.ToInt16(p.Data[i]) == 0) ? 0 : 1;
                    //    }
                    //    Application.Current.Dispatcher.Invoke(new Action(() =>
                    //    {
                    //        reffc(1);
                    //    }));
                    //}
                    //else if (Convert.ToInt16(p.OriginalDeviceID) == did[ct - 7, 3])
                    //{
                    //    for (int i = 12; i < 12 + 8; i++)
                    //    {
                    //        fcs[i - 4] = (Convert.ToInt16(p.Data[i]) == 0) ? 0 : 1;
                    //    }
                    //    Application.Current.Dispatcher.Invoke(new Action(() =>
                    //    {
                    //        reffc(2);
                    //    }));
                    //}
                    break;
                case "0xEFFF":
                    int channelno2 = Convert.ToInt16(p.Data[12]);
                    if (islightModule(Convert.ToInt16(p.OriginalDeviceID)))
                    {
                        com c1 = new com();
                        c1.getswitch(subnet, Convert.ToInt16(p.OriginalDeviceID), direct);
                    }
                    else if (ishvacModule(Convert.ToInt16(p.OriginalDeviceID)))
                    {
                        com c2 = new com();
                        c2.getswitch(subnet, Convert.ToInt16(p.OriginalDeviceID), direct);
                    }
                    //if (Convert.ToInt16(p.OriginalDeviceID) == did[ct - 7, 2])
                    //{
                    //    writelog("Manually channel set of HVAC module : " + did[ct - 7, 2].ToString());
                    //    //for (int i = 0; i < 8; i++)
                    //    //{
                    //    //    fcs[i] = (Convert.ToInt16(p.Data[19]) & i+1) == i+1 ? 1:0;
                    //    //}
                    //    //Application.Current.Dispatcher.Invoke(new Action(() =>
                    //    //{
                    //    //    reffc(1);
                    //    //}));
                    //    com c1 = new com();
                    //    c1.getswitch(subnet, did[ct - 7, 2], direct);
                    //    if (deactiveDone && currentMode == "deactive" & deactiveTimer.Enabled == false)
                    //    {
                    //        deactiveTimer.Enabled = true;
                    //    }

                    //}
                    //else if (Convert.ToInt16(p.OriginalDeviceID) == did[ct - 7, 3])
                    //{
                    //    writelog("Manually channel set of HVAC module : " + did[ct - 7, 3].ToString());
                    //    //for (int i = 8; i < 16; i++)
                    //    //{
                    //    //    fcs[i] = (Convert.ToInt16(p.Data[16]) & i+1) == i+1 ? 1 : 0;
                    //    //}
                    //    //Application.Current.Dispatcher.Invoke(new Action(() =>
                    //    //{
                    //    //    reffc(2);
                    //    //}));
                    //    com c2 = new com();
                    //    c2.getswitch(subnet, did[ct - 7, 3], direct);
                    //    if (deactiveDone && currentMode == "deactive" & deactiveTimer.Enabled == false)
                    //    {
                    //        deactiveTimer.Enabled = true;
                    //    }
                    //}
                    break;

                case "0x3":
                    com c3 = new com();
                    if (Convert.ToInt16(p.OriginalDeviceID) == did[ct - 7, 2])
                    {
                        c3.getswitch(subnet, did[ct - 7, 2], direct);
                    }
                    else if (Convert.ToInt16(p.OriginalDeviceID) == did[ct - 7, 3])
                    {
                        c3.getswitch(subnet, did[ct - 7, 3], direct);
                    } else if (Convert.ToInt16(p.OriginalDeviceID) == did[ct - 7, 0])
                    {
                        c3.getswitch(subnet, did[ct - 7, 0], direct);
                    }
                    else if (Convert.ToInt16(p.OriginalDeviceID) == did[ct - 7, 1])
                    {
                        c3.getswitch(subnet, did[ct - 7, 1], direct);
                    }
                    break;
                case "0xE3E8"://temp respond
                    if (Convert.ToInt16(p.OriginalDeviceID) == 115)
                    {

                        Application.Current.Dispatcher.Invoke(new Action(() =>

                        { /* Your code here */
                            coeTemp.Content = "°" + Convert.ToInt16(p.Data[12]).ToString();

                        }));
                    } else if(Convert.ToInt16(p.OriginalDeviceID) == 114)
                    {
                        Application.Current.Dispatcher.Invoke(new Action(() =>

                        { /* Your code here */
                            temp.Content = "°" + Convert.ToInt16(p.Data[12]).ToString();

                        }));
                    }
                    break;
                case "0xE3E5"://temp

                    break;
                case "0xE3DB":

                    break;
                case "0xD903"://voltage packet
                    if (Convert.ToInt16(p.OriginalDeviceID) == did[ct - 7,6])
                    {
                        volt = Double.Parse(Convert.ToInt32(p.Data[11] * 256 + p.Data[12]).ToString() + "." + p.Data[13].ToString() + p.Data[14].ToString());
                       // double v2 = Double.Parse(Convert.ToInt32(p.Data[16] * 256 + p.Data[17]).ToString() + "." + Convert.ToInt32(p.Data[18] * 256 + p.Data[19]).ToString());
                        //double v3 = Double.Parse(Convert.ToInt32(p.Data[20] * 256 + p.Data[21]).ToString() + "." + Convert.ToInt32(p.Data[22] * 256 + p.Data[23]).ToString());
                        Application.Current.Dispatcher.Invoke(new Action(() =>
                        {
                            v1label.Content = volt;
                            //v2label.Content = v2;
                            //v3label.Content = v3;
                        }));
                    }
                    
                    break;
                case "0xD909":
                    if (Convert.ToInt16(p.OriginalDeviceID) == did[ct - 7, 6])
                    {
                        amper = Double.Parse(Convert.ToInt32(p.Data[11]).ToString() + "." + p.Data[12].ToString() + p.Data[13].ToString() + p.Data[14].ToString());
                        //double a2 = Double.Parse(Convert.ToInt32(p.Data[16]).ToString() + "." + Convert.ToInt32(p.Data[17] * 512 + p.Data[18] * 256 + p.Data[19]).ToString());
                        //double a3 = Double.Parse(Convert.ToInt32(p.Data[20]).ToString() + "." + Convert.ToInt32(p.Data[21] * 512 + p.Data[22] * 256 + p.Data[23]).ToString());
                        Application.Current.Dispatcher.Invoke(new Action(async() =>
                        {
                            a1label.Content = amper;
                            //a2label.Content = a2;
                            //a3label.Content = a3;
                            await Task.Delay(300);
                            calcKw();
                        }));
                    }
                    break;
                case "0xDA00":
                    writelog("Time request recieved from doorloger module");
                    //com com = new com();
                    //com.sendmytime();
                    break;
                case "0xDA01":
                    writelog("Time sent to external module");
                    writelog("Time Received" + (p.Content[0] == 0xf8 ? "Successful" : "Failed"));
                    DateTime t = new DateTime(2000 + p.Content[1], p.Content[2], p.Content[3], p.Content[4], p.Content[5], p.Content[6]);

                    writelog("Received Time : " + t.ToString() + "\n");
                    writelog("Current Time  : " + DateTime.Now + "\n");
                    break;
            }

        }

        public void calcKw()
        {
            kW = (volt * amper) / 1000;
            kWlabel.Content = kW.ToString("0.00");
        }
        public async void queue(int n)
        {
            fc1_off.Checked -= fc1_off_Checked;
            fc2_high_off.Checked -= fc2_high_off_Checked;
            fc3_off.Checked -= fc3_off_Checked;
            fc4_off.Checked -= fc4_off_Checked;
            fc5_off.Checked -= fc5_off_Checked;
            fc1_off.IsChecked = fc2_high_off.IsChecked = fc3_off.IsChecked = fc4_off.IsChecked = fc5_off.IsChecked = true;
            fc1_off.Checked += fc1_off_Checked;
            fc2_high_off.Checked += fc2_high_off_Checked;
            fc3_off.Checked += fc3_off_Checked;
            fc4_off.Checked += fc4_off_Checked;
            fc5_off.Checked += fc5_off_Checked;
            await Task.Delay(500);
            com c = new com();
            for(int i=0; i<module.Length; i++)
            {
                c.getswitch(subnet, module[i,1], direct);
                await Task.Delay(300);
            }
            //c.getswitch(subnet, did[ct - 7, 0], direct);
            //await Task.Delay(300);
            //c.getswitch(subnet, did[ct - 7, 1], direct);
            //await Task.Delay(300);
            //c.getswitch(subnet, did[ct - 7, 2], direct);
            //await Task.Delay(300);
            //c.getswitch(subnet, did[ct - 7, 3], direct);
            //await Task.Delay(300);
            //c.gettemp(subnet, did[ct - 7, 4], direct);
            //await Task.Delay(300);
            //c.gettemp(subnet, did[ct - 7, 5], direct);
            //await Task.Delay(300);
            //c.getvoltage(subnet, did[ct - 7, 6], direct);
            //await Task.Delay(300);
            //c.getcurrent(subnet, did[ct - 7, 6], direct);

        }
        //click function of every right side buttons
        //
        public void firstclick(int n)
        {
            queue(n);
        }
        

        //---------------------------------------
        //lighting switches functions
        //
        private void sw1_Click(object sender, RoutedEventArgs e)
        {
            sw1.IsEnabled = false;

            if (sw1.IsChecked == true)
            {
                com c = new com();
                c.setswitch(subnet, switches["sw1"][0], switches["sw1"][1], 100, direct);
            }
            else
            {
                com c = new com();
                c.setswitch(subnet, switches["sw1"][0], switches["sw1"][1], 0, direct);
            }
        }
        private void sw2_Click(object sender, RoutedEventArgs e)
        {
            sw2.IsEnabled = false;
            if (sw2.IsChecked == true)
            {
                com c = new com();
                c.setswitch(subnet, switches["sw2"][0], switches["sw2"][1], 100, direct);
            }
            else
            {
                com c = new com();
                c.setswitch(subnet, switches["sw2"][0], switches["sw2"][1], 0, direct);
            }
        }
        private void sw3_Click(object sender, RoutedEventArgs e)
        {
            sw3.IsEnabled = false;
            if (sw3.IsChecked == true)
            {
                com c = new com();
                c.setswitch(subnet, switches["sw3"][0], switches["sw3"][1], 100, direct);
            }
            else
            {
                com c = new com();
                c.setswitch(subnet, switches["sw3"][0], switches["sw3"][1], 0, direct);
            }
        }
        private void sw4_Click(object sender, RoutedEventArgs e)
        {

            sw4.IsEnabled = false;
            if (sw4.IsChecked == true)
            {
                com c = new com();
                c.setswitch(subnet, switches["sw4"][0], switches["sw4"][1], 100, direct);
            }
            else
            {
                com c = new com();
                c.setswitch(subnet, switches["sw4"][0], switches["sw4"][1], 0, direct);
            }
        }
        private void sw5_Click(object sender, RoutedEventArgs e)
        {
            sw5.IsEnabled = false;
            if (sw5.IsChecked == true)
            {
                com c = new com();
                c.setswitch(subnet, switches["sw5"][0], switches["sw5"][1], 100, direct);
            }
            else
            {
                com c = new com();
                c.setswitch(subnet, switches["sw5"][0], switches["sw5"][1], 0, direct);
            }
        }
        private void sw6_Click(object sender, RoutedEventArgs e)
        {

            sw6.IsEnabled = false;
            if (sw6.IsChecked == true)
            {
                com c = new com();
                c.setswitch(subnet, switches["sw6"][0], switches["sw6"][1], 100, direct);
            }
            else
            {
                com c = new com();
                c.setswitch(subnet, switches["sw6"][0], switches["sw6"][1], 0, direct);
            }
        }
        private void sw7_Click(object sender, RoutedEventArgs e)
        {
            sw7.IsEnabled = false;
            if (sw7.IsChecked == true)
            {
                com c = new com();
                c.setswitch(subnet, switches["sw7"][0], switches["sw7"][1], 100, direct);
            }
            else
            {
                com c = new com();
                c.setswitch(subnet, switches["sw7"][0], switches["sw7"][1], 0, direct);
            }
        }
        private void sw8_Click(object sender, RoutedEventArgs e)
        {
            sw8.IsEnabled = false;
            if (sw8.IsChecked == true)
            {
                com c = new com();
                c.setswitch(subnet, switches["sw8"][0], switches["sw8"][1], 100, direct);
            }
            else
            {
                com c = new com();
                c.setswitch(subnet, switches["sw8"][0], switches["sw8"][1], 0, direct);
            }
        }
        private void sw9_Click(object sender, RoutedEventArgs e)
        {
            sw9.IsEnabled = false;
            if (sw9.IsChecked == true)
            {
                com c = new com();
                c.setswitch(subnet, switches["sw9"][0], switches["sw9"][1], 100, direct);
            }
            else
            {
                com c = new com();
                c.setswitch(subnet, switches["sw9"][0], switches["sw9"][1], 0, direct);
            }
        }
        private void sw10_Click(object sender, RoutedEventArgs e)
        {
            sw10.IsEnabled = false;
            if (sw10.IsChecked == true)
            {
                com c = new com();
                c.setswitch(subnet, switches["sw10"][0], switches["sw10"][1], 100, direct);
            }
            else
            {
                com c = new com();
                c.setswitch(subnet, switches["sw10"][0], switches["sw10"][1], 0, direct);
            }
        }
        private void sw11_Click(object sender, RoutedEventArgs e)
        {
            sw11.IsEnabled = false;
            if (sw11.IsChecked == true)
            {
                com c = new com();
                c.setswitch(subnet, switches["sw11"][0], switches["sw11"][1], 100, direct);
            }
            else
            {
                com c = new com();
                c.setswitch(subnet, switches["sw11"][0], switches["sw11"][1], 0, direct);
            }
        }
        private void sw12_Click(object sender, RoutedEventArgs e)
        {
            sw12.IsEnabled = false;
            if (sw12.IsChecked == true)
            {
                com c = new com();
                c.setswitch(subnet, switches["sw12"][0], switches["sw12"][1], 100, direct);
            }
            else
            {
                com c = new com();
                c.setswitch(subnet, switches["sw12"][0], switches["sw12"][1], 0, direct);
            }
        }
        private void sw13_Click(object sender, RoutedEventArgs e)
        {
            sw13.IsEnabled = false;
            if (sw13.IsChecked == true)
            {
                com c = new com();
                c.setswitch(subnet, switches["sw13"][0], switches["sw13"][1], 100, direct);
            }
            else
            {
                com c = new com();
                c.setswitch(subnet, switches["sw13"][0], switches["sw13"][1], 0, direct);
            }
        }
        private void sw14_Click(object sender, RoutedEventArgs e)
        {
            sw14.IsEnabled = false;
            if (sw14.IsChecked == true)
            {
                com c = new com();
                c.setswitch(subnet, switches["sw14"][0], switches["sw14"][1], 100, direct);
            }
            else
            {
                com c = new com();
                c.setswitch(subnet, switches["sw14"][0], switches["sw14"][1], 0, direct);
            }
        }
        private void sw15_Click(object sender, RoutedEventArgs e)
        {
            sw15.IsEnabled = false;
            if (sw15.IsChecked == true)
            {
                com c = new com();
                c.setswitch(subnet, switches["sw15"][0], switches["sw15"][1], 100, direct);
            }
            else
            {
                com c = new com();
                c.setswitch(subnet, switches["sw15"][0], switches["sw15"][1], 0, direct);
            }
        }
        //private void sw16_Click(object sender, RoutedEventArgs e)
        //{
        //    sw16.IsEnabled = false;
        //    if (sw16.IsChecked == true)
        //    {
        //        com c = new com();
        //        c.setswitch(subnet, did[ct - 7, 0], sid[ct - 7, 15], 100, direct);
        //        //MessageBox.Show("sw1 event-on");
        //    }
        //    else
        //    {
        //        com c = new com();
        //        c.setswitch(subnet, did[ct - 7, 0], sid[ct - 7, 15], 0, direct);
        //        //MessageBox.Show("sw1 event-off");
        //    }
        //}
        //
        //lighting (roshanayi) switches functions
        //---------------------------------------
        private void speaker1_Click(object sender, RoutedEventArgs e)
        {
            speaker1.IsEnabled = false;
            if (speaker1.IsChecked == true)
            {
                com c = new com();
                c.setswitch(subnet, switches["sp1"][0], switches["sp1"][1], 100, direct);
                //MessageBox.Show("sw1 event-on");
            }
            else
            {
                com c = new com();
                c.setswitch(subnet, switches["sp1"][0], switches["sp1"][1], 0, direct);
                //MessageBox.Show("sw1 event-off");
            }
        }

        private async void speaker21_Click(object sender, RoutedEventArgs e)
        {
            speaker2_1.IsEnabled = false;
            if (speaker2_1.IsChecked == true)
            {
                com c = new com();
                c.setswitch(subnet, switches["sp2"][0], switches["sp2"][1], 100, direct);
                //MessageBox.Show("sw1 event-on");
            }
            else
            {
                com c = new com();
                c.setswitch(subnet, switches["sp2"][0], switches["sp2"][1], 0, direct);
                await Task.Delay(300);
                //MessageBox.Show("sw1 event-off");
            }
        }
        
        private void speaker3_Click(object sender, RoutedEventArgs e)
        {
            speaker3.IsEnabled = false;
            if (speaker3.IsChecked == true)
            {
                com c = new com();
                c.setswitch(subnet, switches["sp3"][0], switches["sp3"][1], 100, direct);
                //MessageBox.Show("sw1 event-on");
            }
            else
            {
                com c = new com();
                c.setswitch(subnet, switches["sp3"][0], switches["sp3"][1], 0, direct);
                //MessageBox.Show("sw1 event-off");
            }
        }
        private void speaker4_Click(object sender, RoutedEventArgs e)
        {
            speaker4.IsEnabled = false;
            if (speaker4.IsChecked == true)
            {
                com c = new com();
                c.setswitch(subnet, switches["sp4"][0], switches["sp4"][1], 100, direct);
                //MessageBox.Show("sw1 event-on");
            }
            else
            {
                com c = new com();
                c.setswitch(subnet, switches["sp4"][0], switches["sp4"][1], 0, direct);
                //MessageBox.Show("sw1 event-off");
            }
        }
        private void speaker5_Click(object sender, RoutedEventArgs e)
        {
            speaker5.IsEnabled = false;
            if (speaker5.IsChecked == true)
            {
                com c = new com();
                c.setswitch(subnet, switches["sp5"][0], switches["sp5"][1], 100, direct);
                //MessageBox.Show("sw1 event-on");
            }
            else
            {
                com c = new com();
                c.setswitch(subnet, switches["sp5"][0], switches["sp5"][1], 0, direct);
                //MessageBox.Show("sw1 event-off");
            }
        }

        private async void btn_cot1_Click(object sender, RoutedEventArgs e)
        {

            ct = 7;
            firstclick(1);
        }

        
        private async void allsockets_Click(object sender, RoutedEventArgs e)
        {
            allsockets.IsEnabled = false;
            if (allsockets.IsChecked == true)
            {
                com c = new com();
                for (int i = 0; i < 16; i++)
                {
                    c.setswitch(subnet, switches["Sockets"][0], switches["Sockets"][1], 100, direct);
                    await Task.Delay(200);
                }
            }
            else
            {
                for (int i = 0; i < 16; i++)
                {
                    com c = new com();

                    c.setswitch(subnet, switches["Sockets"][0], switches["Sockets"][1], 0, direct);
                    await Task.Delay(200);
                }
            }
        }

        //
        //right side menu bottons click function
        //--------------------------------------

        //clean last cottage states of switchs and elements
        //

        public void refsw(int n)
        {
                Application.Current.Dispatcher.Invoke(new Action(() =>
                {
                    for (int i = 0; i < switches.LongCount(); i++)
                    {
                        int[] data = switches.ContainsKey("sw" + (i + 1).ToString())? switches["sw" + (i + 1).ToString()]:null;
                        if (data != null && data[0] == n && switches["sw" + (i + 1).ToString()][0]!=0)
                        {
                            int chno = 0;
                            switch ("sw" + (i + 1).ToString())
                            {
                                case "sw1":
                                    sw1.Click -= sw1_Click;
                                    sw1.IsChecked = switches["sw1"][0]!=0 ? Convert.ToBoolean(channels[n][(switches["sw1"])[1]-1]) : false;
                                    //tralight_on.Visibility = (sw1.IsChecked == true ? Visibility.Visible : Visibility.Hidden);
                                    sw1.IsEnabled = true;
                                    sw1.Click += sw1_Click;
                                    break;
                                case "sw2":
                                    sw2.Click -= sw2_Click;
                                    sw2.IsChecked = switches["sw2"][0] != 0 ? Convert.ToBoolean((channels[n])[(switches["sw2"])[1]-1]) : false;
                                    //rahrolight_on.Visibility = (sw2.IsChecked == true ? Visibility.Visible : Visibility.Hidden);
                                    sw2.IsEnabled = true;
                                    sw2.Click += sw2_Click;
                                    break;
                                case "sw3":
                                    sw3.Click -= sw3_Click;
                                    sw3.IsChecked = switches["sw3"][0] != 0 ? Convert.ToBoolean((channels[n])[(switches["sw3"])[1]-1]) : false;
                                    //bedroomlight_on.Visibility = (sw3.IsChecked == true ? Visibility.Visible : Visibility.Hidden);

                                    sw3.IsEnabled = true;
                                    sw3.Click += sw3_Click;
                                    break;
                                case "sw4":
                                    sw4.Click -= sw4_Click;
                                    sw4.IsChecked = switches["sw4"][0] != 0 ? Convert.ToBoolean((channels[n])[(switches["sw4"])[1]-1]) : false;
                                    //makeuplight_on.Visibility = (sw4.IsChecked == true ? Visibility.Visible : Visibility.Hidden);

                                    sw4.IsEnabled = true;
                                    sw4.Click += sw4_Click;
                                    break;
                                case "sw5":
                                    sw5.Click -= sw5_Click;
                                    sw5.IsChecked = switches["sw5"][0] != 0 ? Convert.ToBoolean((channels[n])[(switches["sw5"])[1]-1]) : false;
                                    //kitchenlight_on.Visibility = (sw5.IsChecked == true ? Visibility.Visible : Visibility.Hidden);

                                    sw5.IsEnabled = true;
                                    sw5.Click += sw5_Click;
                                    break;
                                case "sw6":
                                    sw6.Click -= sw6_Click;
                                    sw6.IsChecked = switches["sw6"][0] != 0 ? Convert.ToBoolean((channels[n])[(switches["sw6"])[1]-1]) : false;
                                    //rooflight_on.Visibility = (sw6.IsChecked == true ? Visibility.Visible : Visibility.Hidden);

                                    sw6.IsEnabled = true;
                                    sw6.Click += sw6_Click;
                                    break;
                                case "sw7":
                                    sw7.Click -= sw7_Click;
                                    sw7.IsChecked = switches["sw7"][0] != 0 ? Convert.ToBoolean((channels[n])[(switches["sw7"])[1]-1]) : false;
                                    //livroomlight_on.Visibility = (sw7.IsChecked == true ? Visibility.Visible : Visibility.Hidden);

                                    sw7.IsEnabled = true;
                                    sw7.Click += sw7_Click;
                                    break;
                                case "sw8":
                                    sw8.Click -= sw8_Click;
                                    sw8.IsChecked = switches["sw8"][0] != 0 ? Convert.ToBoolean((channels[n])[(switches["sw8"])[1]]) : false;
                                    //livroomlight_on.Visibility = (sw7.IsChecked == true ? Visibility.Visible : Visibility.Hidden);

                                    sw8.IsEnabled = true;
                                    sw8.Click += sw8_Click;
                                    break;
                                case "sw9":
                                    sw9.Click -= sw9_Click;
                                    sw9.IsChecked = switches["sw9"][0] != 0 ? Convert.ToBoolean((channels[n])[(switches["sw9"])[1]-1]) : false;
                                    //livroomlight_on.Visibility = (sw7.IsChecked == true ? Visibility.Visible : Visibility.Hidden);

                                    sw9.IsEnabled = true;
                                    sw9.Click += sw9_Click;
                                    break;
                                case "sw10":
                                    sw10.Click -= sw10_Click;
                                    sw10.IsChecked = switches["sw10"][0] != 0 ? Convert.ToBoolean((channels[n])[(switches["sw10"])[1]-1]) : false;
                                    //livroomlight_on.Visibility = (sw7.IsChecked == true ? Visibility.Visible : Visibility.Hidden);

                                    sw10.IsEnabled = true;
                                    sw10.Click += sw10_Click;
                                    break;
                                case "sw11":
                                    sw11.Click -= sw11_Click;
                                    sw11.IsChecked = switches["sw11"][0] != 0 ? Convert.ToBoolean((channels[n])[(switches["sw11"])[1]-1]) : false;
                                    //livroomlight_on.Visibility = (sw7.IsChecked == true ? Visibility.Visible : Visibility.Hidden);

                                    sw11.IsEnabled = true;
                                    sw11.Click += sw11_Click;
                                    break;
                                case "sw12":
                                    sw12.Click -= sw12_Click;
                                    sw12.IsChecked = switches["sw12"][0] != 0 ? Convert.ToBoolean((channels[n])[(switches["sw12"])[1]-1]) : false;
                                    //livroomlight_on.Visibility = (sw7.IsChecked == true ? Visibility.Visible : Visibility.Hidden);

                                    sw12.IsEnabled = true;
                                    sw12.Click += sw12_Click;
                                    break;
                                case "sw13":
                                    sw13.Click -= sw13_Click;
                                    sw13.IsChecked = switches["sw13"][0] != 0 ? Convert.ToBoolean((channels[n])[(switches["sw13"])[1]-1]) : false;
                                    //livroomlight_on.Visibility = (sw7.IsChecked == true ? Visibility.Visible : Visibility.Hidden);

                                    sw13.IsEnabled = true;
                                    sw13.Click += sw13_Click;
                                    break;
                                case "sw14":
                                    sw14.Click -= sw14_Click;
                                    sw14.IsChecked = switches["sw14"][0] != 0 ? Convert.ToBoolean((channels[n])[(switches["sw14"])[1]-1]) : false;
                                    //livroomlight_on.Visibility = (sw7.IsChecked == true ? Visibility.Visible : Visibility.Hidden);

                                    sw14.IsEnabled = true;
                                    sw14.Click += sw14_Click;
                                    break;
                                case "sw15":
                                    sw15.Click -= sw15_Click;
                                    sw15.IsChecked = switches["sw15"][0] != 0 ? Convert.ToBoolean((channels[n])[(switches["sw15"])[1]-1]):false;
                                    //livroomlight_on.Visibility = (sw7.IsChecked == true ? Visibility.Visible : Visibility.Hidden);

                                    sw15.IsEnabled = true;
                                    sw15.Click += sw15_Click;
                                    break;
                                default:

                                    break;
                            }
                        }
                        
                    }
                }));

        }
        public async void groupCheck(int n,string name)
        {
            com c = new com();
            for (int i=1; i<4; i++)
            {
                if(i != n)
                {
                    c.setswitch(subnet, switches[name][0], switches[name][i], 0, direct);
                    await Task.Delay(100);
                }
            }
        }
        private async void fc1_high_Checked(object sender, RoutedEventArgs e)
        {
            com c = new com();
            groupCheck(1, "fc1");
            await Task.Delay(400);
            c.setswitch(subnet, switches["fc1"][0], switches["fc1"][1], 100, direct);
        }
        private async void fc1_med_Checked(object sender, RoutedEventArgs e)
        {
            com c = new com();
            groupCheck(2, "fc1");
            await Task.Delay(400);
            c.setswitch(subnet, switches["fc1"][0], switches["fc1"][2], 100, direct);
        }
        private async void fc1_low_Checked(object sender, RoutedEventArgs e)
        {
            com c = new com();
            groupCheck(3, "fc1");
            await Task.Delay(400);
            c.setswitch(subnet, switches["fc1"][0], switches["fc1"][3], 100, direct);
        }
        private async void fc1_off_Checked(object sender, RoutedEventArgs e)
        {
            com c = new com();
            for(int i=1; i<switches["fc1"].Length; i++)
            {
                c.setswitch(subnet, switches["fc1"][0], switches["fc1"][i], 0, direct);
                await Task.Delay(200);
            }
        }
        private async void fc2_high_Checked(object sender, RoutedEventArgs e)
        {
            com c = new com();
            groupCheck(1, "fc2");
            c.setswitch(subnet, switches["fc2"][0], switches["fc2"][1], 100, direct);
        }

        private async void fc2_med_Checked(object sender, RoutedEventArgs e)
        {
            com c = new com();
            groupCheck(2, "fc2");
            c.setswitch(subnet, switches["fc2"][0], switches["fc2"][2], 100, direct);
        }

        private async void fc2_low_Checked(object sender, RoutedEventArgs e)
        {
            com c = new com();
            groupCheck(3, "fc2");
            c.setswitch(subnet, switches["fc2"][0], switches["fc2"][3], 100, direct);
        }
        
        private async void fc2_high_off_Checked(object sender, RoutedEventArgs e)
        {
            com c = new com();

            for (int i = 1; i < switches["fc2"].Length; i++)
            {
                c.setswitch(subnet, switches["fc2"][0], switches["fc2"][i], 0, direct);
                await Task.Delay(200);
            }
        }
        private async void fc3_high_Checked(object sender, RoutedEventArgs e)
        {
            com c = new com();
            groupCheck(1, "fc3");
            c.setswitch(subnet, switches["fc3"][0], switches["fc3"][1], 100, direct);
        }

        private async void fc3_med_Checked(object sender, RoutedEventArgs e)
        {
            com c = new com();
            groupCheck(2, "fc3");
            c.setswitch(subnet, switches["fc3"][0], switches["fc3"][2], 100, direct);
        }

        private async void fc3_low_Checked(object sender, RoutedEventArgs e)
        {
            com c = new com();
            groupCheck(3, "fc3");
            c.setswitch(subnet, switches["fc3"][0], switches["fc3"][3], 100, direct);
        }

        private async void fc3_off_Checked(object sender, RoutedEventArgs e)
        {
            com c = new com();

            for (int i = 1; i < switches["fc3"].Length; i++)
            {
                c.setswitch(subnet, switches["fc3"][0], switches["fc3"][i], 0, direct);
                await Task.Delay(200);
            }
        }
        private async void fc4_high_Checked(object sender, RoutedEventArgs e)
        {
            com c = new com();
            groupCheck(1, "fc4");
            c.setswitch(subnet, switches["fc4"][0], switches["fc4"][1], 100, direct);
        }

        private async void fc4_med_Checked(object sender, RoutedEventArgs e)
        {
            com c = new com();
            groupCheck(2, "fc4");
            c.setswitch(subnet, switches["fc4"][0], switches["fc4"][2], 100, direct);
        }

        private async void fc4_low_Checked(object sender, RoutedEventArgs e)
        {
            com c = new com();
            groupCheck(3, "fc4");
            c.setswitch(subnet, switches["fc4"][0], switches["fc4"][3], 100, direct);
        }

        private async void fc5_high_Checked(object sender, RoutedEventArgs e)
        {
            com c = new com();
            groupCheck(1, "fc5");
            c.setswitch(subnet, switches["fc5"][0], switches["fc5"][1], 100, direct);
        }

        private async void fc5_med_Checked(object sender, RoutedEventArgs e)
        {
            com c = new com();
            groupCheck(2, "fc5");
            c.setswitch(subnet, switches["fc5"][0], switches["fc5"][2], 100, direct);
        }

        private async void fc5_low_Checked(object sender, RoutedEventArgs e)
        {
            com c = new com();
            groupCheck(3, "fc5");
            c.setswitch(subnet, switches["fc5"][0], switches["fc5"][3], 100, direct);
        }

        private async void fc4_off_Checked(object sender, RoutedEventArgs e)
        {
            com c = new com();

            for (int i = 1; i < switches["fc4"].Length; i++)
            {
                c.setswitch(subnet, switches["fc4"][0], switches["fc3"][i], 0, direct);
                await Task.Delay(200);
            }
        }

        private async void fc5_off_Checked(object sender, RoutedEventArgs e)
        {
            com c = new com();

            for (int i = 1; i < switches["fc5"].Length; i++)
            {
                c.setswitch(subnet, switches["fc5"][0], switches["fc3"][i], 0, direct);
                await Task.Delay(200);
            }
        }

        public void refsw2(int n)
        {
            if (n != 3)
            {
                Application.Current.Dispatcher.Invoke(new Action(() =>
                {
                    for (int i = 0; i < switchs2.Length; i++)
                    {
                        //MessageBox.Show(i.ToString());
                        //MessageBox.Show(i.ToString() + ":" +switchs[i].ToString());
                        switch (i + 1)
                        {
                            case 1:
                                speaker2_1.Click -= speaker21_Click;
                                speaker2_1.IsChecked = Convert.ToBoolean(switchs2[i]);
                                //tralight_on.Visibility = (sw1.IsChecked == true ? Visibility.Visible : Visibility.Hidden);
                                speaker2_1.IsEnabled = true;
                                speaker2_1.Click += speaker21_Click;
                                break;
                            case 2:
                                speaker1.Click -= speaker1_Click;
                                speaker1.IsChecked = Convert.ToBoolean(switchs2[i]);
                                //rahrolight_on.Visibility = (sw2.IsChecked == true ? Visibility.Visible : Visibility.Hidden);
                                speaker1.IsEnabled = true;
                                speaker1.Click += speaker1_Click;
                                break;
                            case 3:
                                speaker4.Click -= speaker4_Click;
                                speaker4.IsChecked = Convert.ToBoolean(switchs2[i]);
                                //bedroomlight_on.Visibility = (sw3.IsChecked == true ? Visibility.Visible : Visibility.Hidden);

                                speaker4.IsEnabled = true;
                                speaker4.Click += speaker4_Click;
                                break;
                            case 4:
                                speaker3.Click -= speaker3_Click;
                                speaker3.IsChecked = Convert.ToBoolean(switchs2[i]);
                                //makeuplight_on.Visibility = (sw4.IsChecked == true ? Visibility.Visible : Visibility.Hidden);

                                speaker3.IsEnabled = true;
                                speaker3.Click += speaker3_Click;
                                break;
                            case 5:
                                speaker5.Click -= speaker5_Click;
                                speaker5.IsChecked = Convert.ToBoolean(switchs2[i]);
                                //makeuplight_on.Visibility = (sw4.IsChecked == true ? Visibility.Visible : Visibility.Hidden);

                                speaker5.IsEnabled = true;
                                speaker5.Click += speaker5_Click;
                                break;
                            case 6:
                                
                                break;
                            case 7:
                                allsockets.Click -= allsockets_Click;
                                allsockets.IsChecked = Convert.ToBoolean(switchs2[i]);
                                //livroomlight_on.Visibility = (sw7.IsChecked == true ? Visibility.Visible : Visibility.Hidden);

                                allsockets.IsEnabled = true;
                                allsockets.Click += allsockets_Click;
                                break;
                        }
                    }
                }));
            }



        }

        private async void alllights_on_btn_Click(object sender, RoutedEventArgs e)
        {
            com c = new com();
            for (int i=1; i<16; i++)
            {
                c.setswitch(subnet, switches["sw"+i.ToString()][0], switches["sw" + i.ToString()][1], 100, direct);
                await Task.Delay(200);
            }
        }

        private async void alllights_off_btn_Click(object sender, RoutedEventArgs e)
        {
            com c = new com();
            for (int i = 1; i < 16; i++)
            {
                c.setswitch(subnet, switches["sw" + i.ToString()][0], switches["sw" + i.ToString()][1], 0, direct);
                await Task.Delay(200);
            }
        }

        private async void allhallights_off_Click(object sender, RoutedEventArgs e)
        {
            //com c = new com();
            //c.setswitch(subnet, did[ct - 7, 0], 7, 0, direct);
            //await Task.Delay(400);
            //c.setswitch(subnet, did[ct - 7, 0], 5, 0, direct);
            //await Task.Delay(400);
            //c.setswitch(subnet, did[ct - 7, 0], 2, 0, direct);
            //await Task.Delay(400);
            //c.setswitch(subnet, did[ct - 7, 0], 14, 0, direct);
            //await Task.Delay(400);
            //c.setswitch(subnet, did[ct - 7, 0], 12, 0, direct);
            //await Task.Delay(400);
            //c.setswitch(subnet, did[ct - 7, 0], 11, 0, direct);
            //await Task.Delay(400);
            //c.setswitch(subnet, did[ct - 7, 0], 19, 0, direct);
            ////await Task.Delay(400);
        }

        private async void allfc_off_btn_Click(object sender, RoutedEventArgs e)
        {
            com c = new com();
            for (int i = 1; i < 6; i++)
            {
                c.setswitch(subnet, switches["fc" + i.ToString()][0], switches["fc" + i.ToString()][1], 0, direct);
                await Task.Delay(150);
                c.setswitch(subnet, switches["fc" + i.ToString()][0], switches["fc" + i.ToString()][2], 0, direct);
                await Task.Delay(150);
                c.setswitch(subnet, switches["fc" + i.ToString()][0], switches["fc" + i.ToString()][3], 0, direct);
                await Task.Delay(150);
            }
        }

        private async void allfc_low_btn_Click(object sender, RoutedEventArgs e)
        {
            com c = new com();
            for (int i = 1; i < 6; i++)
            {
                groupCheck(3, "fc"+i.ToString());
                c.setswitch(subnet, switches["fc" + i.ToString()][0], switches["fc" + i.ToString()][3], 100, direct);
                await Task.Delay(200);
            }
        }

        private async void allfc_med_btn_Click(object sender, RoutedEventArgs e)
        {
            com c = new com();
            for (int i = 1; i < 6; i++)
            {
                groupCheck(2, "fc" + i.ToString());
                c.setswitch(subnet, switches["fc" + i.ToString()][0], switches["fc" + i.ToString()][2], 100, direct);
                await Task.Delay(200);
            }
        }

        private async void allfc_high_btn_Click(object sender, RoutedEventArgs e)
        {
            com c = new com();
            for (int i = 1; i < 6; i++)
            {
                groupCheck(1, "fc" + i.ToString());
                c.setswitch(subnet, switches["fc" + i.ToString()][0], switches["fc" + i.ToString()][1], 100, direct);
                await Task.Delay(200);
            }
        }

        

        //#flyout settings
        private void setting_btn_Click(object sender, RoutedEventArgs e)
        {
            setwindow.IsOpen = true;
        }

        public void NumberValidation(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }
        public void HolidaysValidation(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9/,]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        public void reffc(int n)
        {
                Application.Current.Dispatcher.Invoke(new Action(() =>
                {
                    fc1_off.Checked -= fc1_off_Checked;
                    fc2_high_off.Checked -= fc2_high_off_Checked;
                    fc3_off.Checked -= fc3_off_Checked;
                    fc4_off.Checked -= fc4_off_Checked;
                    fc5_off.Checked -= fc5_off_Checked;
                    fc1_off.IsChecked = fc2_high_off.IsChecked = fc3_off.IsChecked = fc4_off.IsChecked = fc5_off.IsChecked = true;
                    fc1_off.Checked += fc1_off_Checked;
                    fc2_high_off.Checked += fc2_high_off_Checked;
                    fc3_off.Checked += fc3_off_Checked;
                    fc4_off.Checked += fc4_off_Checked;
                    fc5_off.Checked += fc5_off_Checked;
                    for (int i = 0; i < switches.LongCount(); i++)
                    {
                        int[] data = switches.ContainsKey("fc" + (i + 1).ToString()) ? switches["fc" + (i + 1).ToString()] : null;
                        if (data != null && data[0] == n)
                        {
                            switch ("fc" + (i + 1).ToString())
                            {
                                case "fc1":
                                    fc1_low.Checked -= fc1_low_Checked;
                                    fc1_low.IsChecked = Convert.ToBoolean(channels[n][(switches["fc1"])[3] - 1]);
                                    fc1_low.Checked += fc1_low_Checked;
                                    fc1_med.Checked -= fc1_med_Checked;
                                    fc1_med.IsChecked = Convert.ToBoolean(channels[n][(switches["fc1"])[2] - 1]);
                                    fc1_med.Checked += fc1_med_Checked;
                                    fc1_high.Checked -= fc1_high_Checked;
                                    fc1_high.IsChecked = Convert.ToBoolean(channels[n][(switches["fc1"])[1] - 1]);
                                    fc1_high.Checked += fc1_high_Checked;
                                    break;
                                case "fc2":
                                    fc2_low.Checked -= fc2_low_Checked;
                                    fc2_low.IsChecked = Convert.ToBoolean(channels[n][(switches["fc2"])[3] - 1]);
                                    fc2_low.Checked += fc2_low_Checked;
                                    fc2_med.Checked -= fc2_med_Checked;
                                    fc2_med.IsChecked = Convert.ToBoolean(channels[n][(switches["fc2"])[2] - 1]);
                                    fc2_med.Checked += fc2_med_Checked;
                                    fc2_high.Checked -= fc2_high_Checked;
                                    fc2_high.IsChecked = Convert.ToBoolean(channels[n][(switches["fc2"])[1] - 1]);
                                    fc2_high.Checked += fc2_high_Checked;
                                    break;
                                case "fc3":
                                    fc3_low.Checked -= fc3_low_Checked;
                                    fc3_low.IsChecked = Convert.ToBoolean(channels[n][(switches["fc3"])[3] - 1]);
                                    fc3_low.Checked += fc3_low_Checked;
                                    fc3_med.Checked -= fc3_med_Checked;
                                    fc3_med.IsChecked = Convert.ToBoolean(channels[n][(switches["fc3"])[2] - 1]);
                                    fc3_med.Checked += fc3_med_Checked;
                                    fc3_high.Checked -= fc3_high_Checked;
                                    fc3_high.IsChecked = Convert.ToBoolean(channels[n][(switches["fc3"])[1] - 1]);
                                    fc3_high.Checked += fc3_high_Checked;
                                    break;
                                case "fc4":
                                    fc4_low.Checked -= fc4_low_Checked;
                                    fc4_low.IsChecked = Convert.ToBoolean(channels[n][(switches["fc4"])[3] - 1]);
                                    fc4_low.Checked += fc4_low_Checked;
                                    fc4_med.Checked -= fc4_med_Checked;
                                    fc4_med.IsChecked = Convert.ToBoolean(channels[n][(switches["fc4"])[2] - 1]);
                                    fc4_med.Checked += fc1_med_Checked;
                                    fc4_high.Checked -= fc4_high_Checked;
                                    fc4_high.IsChecked = Convert.ToBoolean(channels[n][(switches["fc4"])[1] - 1]);
                                    fc4_high.Checked += fc4_high_Checked;
                                    break;
                                case "fc5":
                                    fc5_low.Checked -= fc5_low_Checked;
                                    fc5_low.IsChecked = Convert.ToBoolean(channels[n][(switches["fc5"])[3] - 1]);
                                    fc5_low.Checked += fc5_low_Checked;
                                    fc5_med.Checked -= fc5_med_Checked;
                                    fc5_med.IsChecked = Convert.ToBoolean(channels[n][(switches["fc5"])[2] - 1]);
                                    fc5_med.Checked += fc5_med_Checked;
                                    fc5_high.Checked -= fc5_high_Checked;
                                    fc5_high.IsChecked = Convert.ToBoolean(channels[n][(switches["fc5"])[1] - 1]);
                                    fc5_high.Checked += fc5_high_Checked;
                                    break;
                                
                            }
                        }
                        
                    }
                }));
        }


        //#zkey access control module
        private async void button_Click(object sender, RoutedEventArgs e)
        {
            logDG.Items.Clear();
            zk zk = new zk();
            zk.connect();
            await Task.Delay(300);
            string[][] r = zk.getrecords();
            PersianCalendar pc = new PersianCalendar();
            if (r != null)
            {
                for(int i=1; i<r.GetLength(0); i++)
                {
                    string n = r[i][1];
                    string type = r[i][3];
                    bool finger=false,ukfinger=false,open=false,close=false;
                    string time = r[i][6];
                    var dt = DateTime.Parse(time);
                    time = String.Format("{0}/{1}/{2} {3}:{4}:{5}", pc.GetYear(dt), pc.GetMonth(dt), pc.GetDayOfMonth(dt), pc.GetHour(dt), pc.GetMinute(dt), pc.GetSecond(dt));
                    MessageBox.Show(r[i][4]);
                    switch (r[i][4])
                    {
                        case "14":
                            //state = "باز شدن با اثر انگشت";
                            finger = true;
                            break;
                        case "200":
                            //state = "در باز شد";
                            open = true;
                            break;
                        case "201":
                            //state = "در بسته شد";
                            close = true;
                            break;
                        case "34":
                            //state = "اثر انگشت نامعتبر";
                            ukfinger = true;
                            break;
                    }
                    
                    var data = new doorlog { dlno = n, dltime = time, dltype = type, FingerOn = finger==true?Visibility.Visible:Visibility.Hidden, FingerOff = ukfinger==true?Visibility.Visible:Visibility.Hidden, Open = open==true?Visibility.Visible:Visibility.Hidden, Close = close==true?Visibility.Visible:Visibility.Hidden };
                    logDG.Items.Add(data);
                }
            }
            else
            {

            }
        }
        private void active_switch_Click(object sender, RoutedEventArgs e)
        {
            if (active_switch.IsChecked == true)
            {
                preactive_switch.IsEnabled = postactive_switch.IsEnabled = true;
            } else
            {

                preactive_switch.IsEnabled = postactive_switch.IsEnabled = false;
            }

        }
        private async void extraTime_btn_Click(object sender, RoutedEventArgs e)
        {
            extratime et = new extratime();
            et.Show();

            //if (deactiveTimer.Enabled == false)
            //{
            //    deactiveTimer.Enabled=true;
            //} else
            //{
            //    deactiveTimer.Enabled = false;
            //}
        }

        private async void save_Click(object sender, RoutedEventArgs e)
        {
            if (active_switch.IsChecked == true)
            {
                if (active_starthour.Text.Length > 2 || active_startmin.Text.Length > 2 || active_stophour.Text.Length > 2 || active_stopmin.Text.Length > 2 || active_starthour.Text == "" || active_startmin.Text == "" || active_stophour.Text == "" || active_stopmin.Text == "")
                {
                    MessageBox.Show("مقدار ساعت و دقیقه حالت فعال معتبر نیست!");
                }
                else
                {
                    Properties.Settings.Default.active = (bool)active_switch.IsChecked;
                    
                    Properties.Settings.Default.activeStartH = Convert.ToInt16(active_starthour.Text);
                    Properties.Settings.Default.activeStartM = Convert.ToInt16(active_startmin.Text);
                    Properties.Settings.Default.activeStopH = Convert.ToInt16(active_stophour.Text);
                    Properties.Settings.Default.activeStopM = Convert.ToInt16(active_stopmin.Text);
                }
                if (preactive_switch.IsChecked == true)
                {
                    if (preactive_starthour.Text.Length > 2 || preactive_startmin.Text.Length > 2 || preactive_stophour.Text.Length > 2 || preactive_stopmin.Text.Length > 2 || preactive_starthour.Text == "" || preactive_startmin.Text == "" || preactive_stophour.Text == "" || preactive_stopmin.Text == "")
                    {
                        MessageBox.Show("مقدار ساعت و دقیقه حالت پیش فعال معتبر نیست!");
                    }
                    else if ((Convert.ToInt16(preactive_stophour.Text) >= Convert.ToInt16(active_starthour.Text) & Convert.ToInt16(preactive_stopmin.Text) >= Convert.ToInt16(active_startmin.Text)) & (Convert.ToInt16(preactive_starthour.Text) >= Convert.ToInt16(active_starthour.Text) & Convert.ToInt16(preactive_startmin.Text) >= Convert.ToInt16(active_startmin.Text)))
                    {
                        MessageBox.Show("خطا: بازه زمانی حالت پیش فعال باید قبل از شروع حالت فعال باشد!");
                    }
                    else
                    {
                        Properties.Settings.Default.preactive = (bool)preactive_switch.IsChecked;
                        Properties.Settings.Default.preactiveStartH = Convert.ToInt16(preactive_starthour.Text);
                        Properties.Settings.Default.preactiveStartM = Convert.ToInt16(preactive_startmin.Text);
                        Properties.Settings.Default.preactiveStopH = Convert.ToInt16(preactive_stophour.Text);
                        Properties.Settings.Default.preactiveStopM = Convert.ToInt16(preactive_stopmin.Text);
                        Properties.Settings.Default.preactiveEnterLight = (bool)preactive_enterlight.IsChecked;
                        Properties.Settings.Default.preactiveFmode = preactive_fcmode.SelectedIndex;
                    }
                } else
                {
                    Properties.Settings.Default.preactive = false;
                }
                if (postactive_switch.IsChecked == true)
                {
                    if (postactive_starthour.Text.Length > 2 || postactive_startmin.Text.Length > 2 || postactive_stophour.Text.Length > 2 || postactive_stopmin.Text.Length > 2 || postactive_starthour.Text == "" || postactive_startmin.Text == "" || postactive_stophour.Text == "" || postactive_stopmin.Text == "")
                    {
                        MessageBox.Show("مقدار ساعت و دقیقه حالت پیش غیرفعال معتبر نیست!");
                    }
                    else if ((Convert.ToInt16(postactive_starthour.Text) <= Convert.ToInt16(active_stophour.Text) & Convert.ToInt16(postactive_startmin.Text) <= Convert.ToInt16(active_stopmin.Text)) & (Convert.ToInt16(postactive_stophour.Text) <= Convert.ToInt16(active_stophour.Text) & Convert.ToInt16(postactive_stopmin.Text) <= Convert.ToInt16(active_stopmin.Text)))
                    {
                        MessageBox.Show("خطا: بازه زمانی حالت پیش غیرفعال باید بعد از پایان حالت فعال باشد!");
                    }
                    else
                    {
                        Properties.Settings.Default.postactive = (bool)postactive_switch.IsChecked;
                        Properties.Settings.Default.postactiveStartH = Convert.ToInt16(postactive_starthour.Text);
                        Properties.Settings.Default.postactiveStartM = Convert.ToInt16(postactive_startmin.Text);
                        Properties.Settings.Default.postactiveStopH = Convert.ToInt16(postactive_stophour.Text);
                        Properties.Settings.Default.postactiveStopM = Convert.ToInt16(postactive_stopmin.Text);
                        Properties.Settings.Default.postactiveHallLight = (bool)postactive_offhal.IsChecked;
                        Properties.Settings.Default.postactiveFmode = postactive_fcmode.SelectedIndex;
                    }
                } else
                {
                    Properties.Settings.Default.postactive = false;
                }
            } else
            {
                Properties.Settings.Default.active = false;
                Properties.Settings.Default.preactive = false;
                Properties.Settings.Default.postactive = false;
            }
            if (deactive_switch.IsChecked == true)
            {
                Properties.Settings.Default.deactive = (bool)deactive_switch.IsChecked;
                Properties.Settings.Default.deactiveFridays = (bool)deactive_fridays.IsChecked;
                Properties.Settings.Default.deactiveHolidays = (bool)deactive_holidays.IsChecked;
                if (holidays.Text.Length > 0)
                {
                    holidaysList = holidays.Text.Split(',');
                    for (int i = 0; i < holidaysList.Length; i++)
                    {
                        writelog("holidays:" + holidaysList[i]);
                    }
                    Properties.Settings.Default.holidays = holidays.Text;
                }
            } else
            {
                Properties.Settings.Default.deactive = false;
            }
            Properties.Settings.Default.sendTime = (bool)sendTime.IsChecked;
            if (z1.Text != "" & z2.Text != "" & z3.Text != "" & z4.Text != "" & z5.Text != "" & z6.Text != "" & z8.Text != "")
            {
                Properties.Settings.Default.z1 = z1.Text;
                Properties.Settings.Default.z2 = z2.Text;
                Properties.Settings.Default.z3 = z3.Text;
                Properties.Settings.Default.z4 = z4.Text;
                Properties.Settings.Default.z5 = z5.Text;
                Properties.Settings.Default.z6 = z6.Text;
                //Properties.Settings.Default.z7 = z7.Text;
                Properties.Settings.Default.z8 = z8.Text;
                z1Title.Content = entrerTab.Header = Properties.Settings.Default.z1;
                z2Title.Content = itTab.Header = Properties.Settings.Default.z2;
                z3Title.Content = techTab.Header = Properties.Settings.Default.z3;
                z4Title.Content = accTab.Header = Properties.Settings.Default.z4;
                z5Title.Content = secTab.Header = Properties.Settings.Default.z5;
                z6Title.Content = ceoTab.Header = Properties.Settings.Default.z6;
                //z7Title.Content = colTab.Header = Properties.Settings.Default.z7;
                z8Title.Content = servTab.Header = Properties.Settings.Default.z8;
                Properties.Settings.Default.Save();
                Properties.Settings.Default.Reload();

                hourlyTimer.Enabled = myTimer.Enabled = false;
                active = Properties.Settings.Default.active;
                preactive = Properties.Settings.Default.preactive;
                postactive = Properties.Settings.Default.postactive;
                deactive = Properties.Settings.Default.deactive;
                deactiveFridays = Properties.Settings.Default.deactiveFridays;
                deactiveHolidays = Properties.Settings.Default.deactiveHolidays;
                holidaysList = Properties.Settings.Default.holidays.ToString().Split(',');
                activeStartH = Properties.Settings.Default.activeStartH;
                activeStartM = Properties.Settings.Default.activeStartM;
                activeStopH = Properties.Settings.Default.activeStopH;
                activeStopM = Properties.Settings.Default.activeStopM;

                preactiveStartH = Properties.Settings.Default.preactiveStartH;
                preactiveStartM = Properties.Settings.Default.preactiveStartM;
                preactiveStopH = Properties.Settings.Default.preactiveStopH;
                preactiveStopM = Properties.Settings.Default.preactiveStopM;
                preactiveFmode = Properties.Settings.Default.preactiveFmode;
                preactiveLight = Properties.Settings.Default.preactiveEnterLight;

                postactiveStartH = Properties.Settings.Default.postactiveStartH;
                postactiveStartM = Properties.Settings.Default.postactiveStartM;
                postactiveStopH = Properties.Settings.Default.postactiveStopH;
                postactiveStopM = Properties.Settings.Default.postactiveStopM;
                postactiveFmode = Properties.Settings.Default.postactiveFmode;
                postactiveHall = Properties.Settings.Default.postactiveHallLight;

                sendMyTime = Properties.Settings.Default.sendTime;

                hourlyTimer.Enabled = myTimer.Enabled = true;
            }
            else
            {
                MessageBox.Show("همه ی بخش ها باید نام داشته باشد!");
            }
        }

        public class doorlog
        {
            public string dlno { set; get; }
            public string dltime { set; get; }
            public string dltype { set; get; }
            //public string dlstate { set; get; }
            public Visibility FingerOn { set; get; }
            public Visibility FingerOff { set; get; }
            public Visibility Open { set; get; }
            public Visibility Close { set; get; }
        }
    }
}

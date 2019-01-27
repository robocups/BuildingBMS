
using Codeplex.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using WebSocketSharp;
using static building_bms.protocol;

namespace building_bms
{
    class com
    {
        private WebSocket con;//websocket connection
        private Socket Udpcon;//UDP socket for sending

        public com()  //Parameterized constructor
        {
        }

        //send through websocket connection (!direct) it depends on server application
        public string send(dynamic cmd)
        {
            if (con.ReadyState == WebSocketState.Open)
            {
                con.Send(cmd.ToString());
                return "packet:" + cmd.ToString();
            }
            else
            {
                return "Error";
            }

        }
        //send through local network as UDP packet (direct) without server application
        public void dsend(byte[] cmd)
        {

            Udpcon = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            IPAddress send_to_address = IPAddress.Parse("192.168.10.255");
            IPEndPoint sending_end_point = new IPEndPoint(send_to_address, 6000);
            byte[] send_buffer = cmd;
            System.Diagnostics.Debug.Print("Print: >>" + protocol.Util.ByteA2Text(send_buffer));

            try
            {
                Udpcon.SendTo(send_buffer, sending_end_point);
            }
            catch (Exception send_exception)
            {
                System.Diagnostics.Debug.Print("Exception:" + send_exception.Message);
            }
        }

        public string changeop(UInt16 op, int n)
        {
            return (op + n * 65536).ToString();
        }

        public void getvoltage(int n, int id, bool direct)
        {
            if (direct)
            {
                byte[] c = { };
                directcmd(0xD902, Convert.ToByte(n), Convert.ToByte(id), c);
            }
            else
            {
                
            }
        }
        public void getcurrent(int n, int id, bool direct)
        {
            if (direct)
            {
                byte[] c = { };
                directcmd(0xD908, Convert.ToByte(n), Convert.ToByte(id), c);
            }
            else
            {

            }
        }
        //read temperature function ( direct or !direct ) 
        public void gettemp(int n, int id, bool direct)
        {
            if (direct)
            {
                byte[] c = { };
                directcmd(0xE3E7, Convert.ToByte(n), Convert.ToByte(id), c);
            }
            else
            {
                dynamic json = new DynamicJson();
                json.type = "call";
                json.area = "1";
                json.building = n.ToString();
                json.action = "temperature";

                send(json);
            }
        }

        //read switchs ( Relay channels ) direct or !direct
        public void getswitch(int n, int id, bool direct)
        {
            if (direct)
            {
                byte[] c = { };
                directcmd(0x0033, Convert.ToByte(n), Convert.ToByte(id), c);
            }
            else
            {
                dynamic json = new DynamicJson();
                json.type = "call";
                json.area = "1";
                json.building = n.ToString();
                if (id != 2)//if relay module is for garden section
                {
                    json.action = "indoorlighting";
                }
                else if (id == 2)
                {
                    json.action = "outdoorlighting";

                }


                send(json);
            }
        }

        //set switchs ( Relay channels ) direct or !direct
        public void setpanel(int n, int id, int chno, int val, bool direct)
        {
            if (direct)
            {
                byte[] c = { Convert.ToByte(chno), Convert.ToByte(val) };
                directcmd(0x0031, Convert.ToByte(n), Convert.ToByte(id), c);
            }
            else
            {
                dynamic json = new DynamicJson();
                json.type = "call";
                json.area = "1";
                json.building = n.ToString();
                json.action = "panel";
                switch (chno)
                {
                    case 3:
                        json.emergency = val == 100 ? "on" : "off";
                        break;
                    case 8:
                        json.vip = val == 100 ? "on" : "off";
                        break;
                    case 13:
                        json.state = val == 100 ? "on" : "off";
                        break;
                }
                send(json);
            }
        }


        public void setswitch(int n, int id, int chno, int val, bool direct)
        {
            if (direct)
            {
                byte[] c = { Convert.ToByte(chno), Convert.ToByte(val),0,0 };
                directcmd(0x0031, Convert.ToByte(n), Convert.ToByte(id), c);
            }
            else
            {
                dynamic json = new DynamicJson();
                json.type = "call";
                json.area = "1";
                json.building = n.ToString();
                if (id != 2)//if relay module is not for garden section
                {
                    json.action = "indoorlighting";
                    json.channel = chno.ToString();
                    json.state = val == 100 ? "on" : "off";
                }
                else if (id == 2)
                {
                    json.action = "outdoorlighting";
                    json.channel = chno.ToString();
                    json.state = val == 100 ? "on" : "off";
                }
                send(json);
            }
        }
        public void setfan(int n, int id, int chno, int val, bool direct)
        {
            if (direct)
            {
                byte[] c = { Convert.ToByte(chno), 3,Convert.ToByte(val) };
                directcmd(0xA93A, Convert.ToByte(n), Convert.ToByte(id), c);
            }
        }
        public void setac(int subnet, int id, int n, int val, bool direct)
        {
            if (direct)
            {
                byte[] c = { Convert.ToByte(n), Convert.ToByte(0), Convert.ToByte(24), Convert.ToByte(24), Convert.ToByte(24), Convert.ToByte(24), Convert.ToByte(val), 1, 2, 2 };
                directcmd(0x193A, Convert.ToByte(subnet), Convert.ToByte(id), c);
            }
        }
        public void getac(int subnet, int id, int n, bool direct)
        {
            if (direct)
            {
                byte[] c = { };
                directcmd(0x1938, Convert.ToByte(subnet), Convert.ToByte(id), c);
            }
        }
        //read floorheating ( direct or !direct )
        public void getheat(int n, int id, int z, bool direct)
        {
            if (direct)
            {
                byte[] c = { Convert.ToByte(z) };
                directcmd(0x1C5E, Convert.ToByte(n), Convert.ToByte(id), c);
            }
            else
            {
                dynamic json = new DynamicJson();
                json.type = "call";
                json.area = "1";
                json.building = n.ToString();
                json.action = "floorheating";
                json.zone = z.ToString();

                send(json);
            }
        }

        //read outdoor switchs ( Relay channels ) direct and !direct
        public void getgarden(int n, int id1, int id2, bool direct)
        {
            if (direct)
            {
                byte[] c = { Convert.ToByte(id2) };
                directcmd(0x0033, Convert.ToByte(n), Convert.ToByte(id2), c);
            }
            else
            {
                dynamic json = new DynamicJson();
                json.type = "call";
                json.area = "1";
                json.building = n.ToString();
                json.action = "outdoorlighting";

                send(json);
            }
        }

        public void readpanel(int n)
        {
            dynamic json = new DynamicJson();
            json.type = "call";
            json.area = "1";
            json.building = n.ToString();
            json.action = "panel";

            send(json);
        }

        public void sendmytime(int n, int id)
        {
            byte[] c = { 0xF8, Convert.ToByte(DateTime.Now.Year - 2000), Convert.ToByte(DateTime.Now.Month), Convert.ToByte(DateTime.Now.Day), Convert.ToByte(DateTime.Now.Hour), Convert.ToByte(DateTime.Now.Minute), Convert.ToByte(DateTime.Now.Second), 0 };
            //byte[] c = { 0xF8, Convert.ToByte(2017 - 2000), Convert.ToByte(7), Convert.ToByte(29), Convert.ToByte(14), Convert.ToByte(50), Convert.ToByte(0), 0 };
            directcmd(0xDA01, (byte)n, (byte)id, c);
        }
        public void todaylog(int n)
        {
            dynamic json = new DynamicJson();
            json.type = "call";
            json.area = "1";
            json.building = n.ToString();
            json.action = "doorlog";
            json.today = true;
            send(json);

        }
        public void yesterdaylog(int n)
        {
            dynamic json = new DynamicJson();
            json.type = "call";
            json.area = "1";
            json.building = n.ToString();
            json.action = "doorlog";
            json.yesterday = true;
            send(json);

        }
        public void readlog(int n, DateTime t1, DateTime t2)
        {
            dynamic json = new DynamicJson();
            json.type = "call";
            json.area = "1";
            json.building = n.ToString();
            json.action = "doorlog";
            json.from = t1.Date.ToString("yyyy-MM-dd HH:mm:ss");
            json.to = t2.Date.ToString("yyyy-MM-dd HH:mm:ss");
            send(json);
        }

        //set floorheating parameters ( direct or !direct )
        public void setheat(int n, int id, int chno, int en, int valve, string state, int temp1, int temp2, int temp3, int temp4, bool direct)
        {
            if (direct)
            {
                byte[] c = { Convert.ToByte(chno), Convert.ToByte(en), Convert.ToByte(valve), Convert.ToByte(state), Convert.ToByte(temp1), Convert.ToByte(temp2), Convert.ToByte(temp3), Convert.ToByte(temp4) };
                directcmd(0x1C5C, Convert.ToByte(n), Convert.ToByte(id), c);
            }
            else
            {
                dynamic json = new DynamicJson();
                json.type = "call";
                json.area = "1";
                json.building = n.ToString();
                json.action = "floorheating";
                json.zone = chno.ToString();
                json.state = en == 1 ? "on" : "off";
                json.temptype = "C";
                switch (state)
                {
                    case "1":
                        json.mode = "normal";
                        break;
                    case "2":
                        json.mode = "day";
                        break;
                    case "3":
                        json.mode = "night";
                        break;
                    case "4":
                        json.mode = "away";
                        break;
                    case "5":
                        json.mode = "timer";
                        break;
                }
                json.normaltemp = temp1.ToString();
                json.daytemp = temp2.ToString();
                json.nighttemp = temp3.ToString();
                json.awaytemp = temp4.ToString();
                json.valve = valve == 1 ? "on" : "off";

                send(json);
            }

        }
        

        public void directcmd(UInt16 op, byte sid, byte did, byte[] content)
        {
            //byte[] c = Util.Text2ByteA(String.Join(" ", content));
            Packet p = new Packet();
            p.Content = content;
            p.OperationCode = op;
            p.OriginalDeviceID = 0xFC;
            p.OriginalDeviceType = 0xFFFE;
            p.OriginalSubnetID = 0xFC;
            p.TargetDeviceID = did;
            p.TargetSubnetID = sid;
            //string temp = Util.ByteA2Text(p.Data);
            p.FixLength();
            p.FixCRC();
            //System.Diagnostics.Debug.Print("<<<<<" + string.Join(",", p.Data));
            byte[] head = new byte[14];
            head[0] = Convert.ToByte(172);
            head[1] = Convert.ToByte(20);
            head[2] = Convert.ToByte(30);
            head[3] = Convert.ToByte(149);
            head[4] = Convert.ToByte('H');
            head[5] = Convert.ToByte('D');
            head[6] = Convert.ToByte('L');
            head[7] = Convert.ToByte('M');
            head[8] = Convert.ToByte('I');
            head[9] = Convert.ToByte('R');
            head[10] = Convert.ToByte('A');
            head[11] = Convert.ToByte('C');
            head[12] = Convert.ToByte('L');
            head[13] = Convert.ToByte('E');


            dsend(head.Concat(p.Data).ToArray());



        }
    }
}

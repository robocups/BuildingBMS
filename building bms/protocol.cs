using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace building_bms
{
    class protocol
    {
        public static class Util
        {
            //public static commander mainF;
            //23 aa 56
            public static byte[] Text2ByteA(string bytea)
            {
                var arr = new List<byte>();
                string[] parts = bytea.Split(' ');
                foreach (string t1 in parts)
                {
                    if (t1.Length == 0)
                        continue;
                    byte t;
                    if (byte.TryParse(t1, NumberStyles.HexNumber, null, out t))
                        arr.Add(t);
                }
                return arr.ToArray();
            }
            public static byte[] GenerateCrc(byte[] data, int from = 0, int to = -1)
            {
                if (to < 0)
                    to = data.Length - 1;
                var crcb = new byte[2];
                UInt16 crc = 0;
                byte dat;
                for (int i = from; i <= to; i++)
                {
                    dat = (byte)(crc >> 8);
                    crc <<= 8;
                    crc ^= CrcTable[dat ^ data[i]];
                }
                crcb[0] = (byte)(crc >> 8);
                crcb[1] = (byte)(crc & 0x00ff);
                return crcb;
            }

            static readonly UInt16[] CrcTable ={                /* CRC tab */
0x0000, 0x1021, 0x2042, 0x3063, 0x4084, 0x50a5, 0x60c6, 0x70e7,
0x8108, 0x9129, 0xa14a, 0xb16b, 0xc18c, 0xd1ad, 0xe1ce, 0xf1ef,
0x1231, 0x0210, 0x3273, 0x2252, 0x52b5, 0x4294, 0x72f7, 0x62d6,
0x9339, 0x8318, 0xb37b, 0xa35a, 0xd3bd, 0xc39c, 0xf3ff, 0xe3de,
0x2462, 0x3443, 0x0420, 0x1401, 0x64e6, 0x74c7, 0x44a4, 0x5485,
0xa56a, 0xb54b, 0x8528, 0x9509, 0xe5ee, 0xf5cf, 0xc5ac, 0xd58d,
0x3653, 0x2672, 0x1611, 0x0630, 0x76d7, 0x66f6, 0x5695, 0x46b4,
0xb75b, 0xa77a, 0x9719, 0x8738, 0xf7df, 0xe7fe, 0xd79d, 0xc7bc,
0x48c4, 0x58e5, 0x6886, 0x78a7, 0x0840, 0x1861, 0x2802, 0x3823,
0xc9cc, 0xd9ed, 0xe98e, 0xf9af, 0x8948, 0x9969, 0xa90a, 0xb92b,
0x5af5, 0x4ad4, 0x7ab7, 0x6a96, 0x1a71, 0x0a50, 0x3a33, 0x2a12,
0xdbfd, 0xcbdc, 0xfbbf, 0xeb9e, 0x9b79, 0x8b58, 0xbb3b, 0xab1a,
0x6ca6, 0x7c87, 0x4ce4, 0x5cc5, 0x2c22, 0x3c03, 0x0c60, 0x1c41,
0xedae, 0xfd8f, 0xcdec, 0xddcd, 0xad2a, 0xbd0b, 0x8d68, 0x9d49,
0x7e97, 0x6eb6, 0x5ed5, 0x4ef4, 0x3e13, 0x2e32, 0x1e51, 0x0e70,
0xff9f, 0xefbe, 0xdfdd, 0xcffc, 0xbf1b, 0xaf3a, 0x9f59, 0x8f78,
0x9188, 0x81a9, 0xb1ca, 0xa1eb, 0xd10c, 0xc12d, 0xf14e, 0xe16f,
0x1080, 0x00a1, 0x30c2, 0x20e3, 0x5004, 0x4025, 0x7046, 0x6067,
0x83b9, 0x9398, 0xa3fb, 0xb3da, 0xc33d, 0xd31c, 0xe37f, 0xf35e,
0x02b1, 0x1290, 0x22f3, 0x32d2, 0x4235, 0x5214, 0x6277, 0x7256,
0xb5ea, 0xa5cb, 0x95a8, 0x8589, 0xf56e, 0xe54f, 0xd52c, 0xc50d,
0x34e2, 0x24c3, 0x14a0, 0x0481, 0x7466, 0x6447, 0x5424, 0x4405,
0xa7db, 0xb7fa, 0x8799, 0x97b8, 0xe75f, 0xf77e, 0xc71d, 0xd73c,
0x26d3, 0x36f2, 0x0691, 0x16b0, 0x6657, 0x7676, 0x4615, 0x5634,
0xd94c, 0xc96d, 0xf90e, 0xe92f, 0x99c8, 0x89e9, 0xb98a, 0xa9ab,
0x5844, 0x4865, 0x7806, 0x6827, 0x18c0, 0x08e1, 0x3882, 0x28a3,
0xcb7d, 0xdb5c, 0xeb3f, 0xfb1e, 0x8bf9, 0x9bd8, 0xabbb, 0xbb9a,
0x4a75, 0x5a54, 0x6a37, 0x7a16, 0x0af1, 0x1ad0, 0x2ab3, 0x3a92,
0xfd2e, 0xed0f, 0xdd6c, 0xcd4d, 0xbdaa, 0xad8b, 0x9de8, 0x8dc9,
0x7c26, 0x6c07, 0x5c64, 0x4c45, 0x3ca2, 0x2c83, 0x1ce0, 0x0cc1,
0xef1f, 0xff3e, 0xcf5d, 0xdf7c, 0xaf9b, 0xbfba, 0x8fd9, 0x9ff8,
0x6e17, 0x7e36, 0x4e55, 0x5e74, 0x2e93, 0x3eb2, 0x0ed1, 0x1ef0
};

            public static string ByteA2Text(byte[] arr)
            {
                if (arr == null)
                    return "";
                var sb = new StringBuilder();
                foreach (var b in arr)
                {
                    sb.Append(Convert.ToString(b, 16).ToUpper().PadLeft(2, '0') + ' ');
                }
                return sb.ToString();
            }

            public static string Byte2Text(byte[] arr)
            {
                if (arr == null)
                    return "";
                StringBuilder sb = new StringBuilder();
                foreach (byte b in arr)
                {
                    sb.AppendFormat("{0:x2}", b);
                }
                return sb.ToString();
            }

            public static string JoinArr(bool[] arr)
            {
                string r = "";
                for (int i = 0; i < arr.Length; i++)
                    r += (i == 0 ? "" : ",") + (arr[i] ? 1 : 0);
                return r;
            }

        }

        public class Packet
        {
            public static readonly int HeaderSize = 2;
            public static readonly int CRCSize = 2;
            public static readonly int PacketWithHeaderSize = 13;
            public static readonly int PacketWithoutHeaderSize = 11;
            public static readonly byte[] HeaderBytes = { 0xAA, 0xAA };
            public byte[] Data;
            public enum ByteName
            {
                Header = 0,
                Length = 2,
                OriginalSubnetID = 3,
                OriginalDeviceID = 4,
                OriginalDeviceType = 5,
                OperationCode = 7,
                TragetSubnetID = 9,
                TragetDeviceID = 10,
                Content = 11
            }

            public byte this[int index] { get { return Data[index]; } set { Data[index] = value; } }

            public Packet()
            {
                Data = new byte[PacketWithHeaderSize];
                FixHeader();
                FixLength();
            }
            public Packet(byte[] arrayBytes)
            {
                Load(arrayBytes);
            }
            public Packet(string arrayBytes)
            {
                Load(arrayBytes);
            }
            public void Load(byte[] arrayBytes)
            {
                Data = new byte[arrayBytes.Length];
                arrayBytes.CopyTo(Data, 0);
            }
            public void Load(string arrayBytes)
            {
                Data = Util.Text2ByteA(arrayBytes);
            }
            public static Packet Create(byte[] arrayBytes)
            {
                return new Packet(arrayBytes);
            }
            public static Packet Create(string arrayBytes)
            {
                return new Packet(arrayBytes);
            }

            //Header
            public byte[] Header { get { return new byte[] { Data[0], Data[1] }; } }
            public void FixHeader()
            {
                HeaderBytes.CopyTo(Data, 0);
            }
            //Original Device type
            public UInt16 OriginalDeviceType
            {
                get
                {
                    return (UInt16)(Data[(byte)ByteName.OriginalDeviceType] * 256 + Data[(byte)ByteName.OriginalDeviceType + 1]);
                }
                set
                {
                    Data[(byte)ByteName.OriginalDeviceType] = (byte)(value >> 8);
                    Data[(byte)ByteName.OriginalDeviceType + 1] = (byte)(value & 0x0ff);
                    FixCRC();
                }
            }
            //Original SubnetID
            public byte OriginalSubnetID { get { return Data[(byte)ByteName.OriginalSubnetID]; } set { Data[(byte)ByteName.OriginalSubnetID] = value; FixCRC(); } }
            //Original DeviceID
            public byte OriginalDeviceID { get { return Data[(byte)ByteName.OriginalDeviceID]; } set { Data[(byte)ByteName.OriginalDeviceID] = value; FixCRC(); } }
            //Target SubnetID
            public byte TargetSubnetID { get { return Data[(byte)ByteName.TragetSubnetID]; } set { Data[(byte)ByteName.TragetSubnetID] = value; FixCRC(); } }
            //Target DeviceID
            public byte TargetDeviceID { get { return Data[(byte)ByteName.TragetDeviceID]; } set { Data[(byte)ByteName.TragetDeviceID] = value; FixCRC(); } }
            //Operation Code
            public UInt16 OperationCode
            {
                get
                {
                    return (UInt16)(Data[(byte)ByteName.OperationCode] * 256 + Data[(byte)ByteName.OperationCode + 1]);
                }
                set
                {
                    Data[(byte)ByteName.OperationCode] = (byte)(value >> 8);
                    Data[(byte)ByteName.OperationCode + 1] = (byte)(value & 0x0ff);
                    FixCRC();
                }
            }
            //Content
            public byte[] Content
            {
                get
                {
                    var c = new byte[ContentLength];
                    for (int i = 0; i < ContentLength; i++)
                        c[i] = Data[(byte)ByteName.Content + i];
                    return c;
                }
                set
                {
                    var ndata = new byte[PacketWithHeaderSize + value.Length];
                    //start to data
                    for (int i = 0; i < (byte)ByteName.Content; i++)
                        ndata[i] = Data[i];
                    value.CopyTo(ndata, (byte)ByteName.Content);
                    Data = ndata;
                    FixLength();
                }
            }
            //Content Length
            public int ContentLength { get { return Data[(byte)ByteName.Length] - PacketWithoutHeaderSize; } }
            //Length
            public int Length { get { return Data[(byte)ByteName.Length]; } }
            public byte FixLength()
            {
                Data[(byte)ByteName.Length] = (byte)(Data.Length - HeaderSize);
                FixCRC();
                return Data[(byte)ByteName.Length];
            }
            //CRC
            public byte[] CRC { get { return new byte[] { Data[Data.Length - 2], Data[Data.Length - 1] }; } }
            public byte[] CalcCRC()
            {
                return GenerateCrc(Data, HeaderSize, Data.Length - (CRCSize + 1));
            }
            public void FixCRC()
            {
                var crc = CalcCRC();
                crc.CopyTo(Data, Data.Length - CRCSize);
            }

            public PacketContentBase ParseContent()
            {
                switch (OperationCode)
                {
                    case 0x31:
                    //return new PCSingleChannelControl(Content);
                    case 0x32:
                    //return new PCSingleChannelControlResponse(Content);
                    case 0xE:
                    case 0xF:

                    default:
                        return null;
                }
            }

            public Boolean IsValid()
            {
                return IsValidHeader() && IsValidStructure() && IsValidLength() && IsValidCrc();
            }
            public Boolean IsValidStructure()
            {
                return Data.Length >= PacketWithHeaderSize;
            }
            public Boolean IsValidHeader()
            {
                return !HeaderBytes.Where((t, i) => t != Data[i]).Any();
            }
            public Boolean IsValidLength()
            {
                if (Data.Length < PacketWithHeaderSize)
                    return false;
                if (Data.Length == Data[(int)ByteName.Length] + HeaderSize)
                    return true;
                return false;
            }
            public Boolean IsValidCrc()
            {
                byte[] crc = CalcCRC();
                return (crc[0] == Data[Data.Length - 2] && crc[1] == Data[Data.Length - 1]);
            }

            #region static
            public static byte[] GenerateCrc(byte[] data, int from = 0, int to = -1)
            {
                if (to < 0)
                    to = data.Length - 1;
                var crcb = new byte[2];
                UInt16 crc = 0;
                byte dat;
                for (int i = from; i <= to; i++)
                {
                    dat = (byte)(crc >> 8);
                    crc <<= 8;
                    crc ^= CrcTable[dat ^ data[i]];
                }
                crcb[0] = (byte)(crc >> 8);
                crcb[1] = (byte)(crc & 0x00ff);
                return crcb;
            }

            static readonly UInt16[] CrcTable ={                /* CRC tab */
0x0000, 0x1021, 0x2042, 0x3063, 0x4084, 0x50a5, 0x60c6, 0x70e7,
0x8108, 0x9129, 0xa14a, 0xb16b, 0xc18c, 0xd1ad, 0xe1ce, 0xf1ef,
0x1231, 0x0210, 0x3273, 0x2252, 0x52b5, 0x4294, 0x72f7, 0x62d6,
0x9339, 0x8318, 0xb37b, 0xa35a, 0xd3bd, 0xc39c, 0xf3ff, 0xe3de,
0x2462, 0x3443, 0x0420, 0x1401, 0x64e6, 0x74c7, 0x44a4, 0x5485,
0xa56a, 0xb54b, 0x8528, 0x9509, 0xe5ee, 0xf5cf, 0xc5ac, 0xd58d,
0x3653, 0x2672, 0x1611, 0x0630, 0x76d7, 0x66f6, 0x5695, 0x46b4,
0xb75b, 0xa77a, 0x9719, 0x8738, 0xf7df, 0xe7fe, 0xd79d, 0xc7bc,
0x48c4, 0x58e5, 0x6886, 0x78a7, 0x0840, 0x1861, 0x2802, 0x3823,
0xc9cc, 0xd9ed, 0xe98e, 0xf9af, 0x8948, 0x9969, 0xa90a, 0xb92b,
0x5af5, 0x4ad4, 0x7ab7, 0x6a96, 0x1a71, 0x0a50, 0x3a33, 0x2a12,
0xdbfd, 0xcbdc, 0xfbbf, 0xeb9e, 0x9b79, 0x8b58, 0xbb3b, 0xab1a,
0x6ca6, 0x7c87, 0x4ce4, 0x5cc5, 0x2c22, 0x3c03, 0x0c60, 0x1c41,
0xedae, 0xfd8f, 0xcdec, 0xddcd, 0xad2a, 0xbd0b, 0x8d68, 0x9d49,
0x7e97, 0x6eb6, 0x5ed5, 0x4ef4, 0x3e13, 0x2e32, 0x1e51, 0x0e70,
0xff9f, 0xefbe, 0xdfdd, 0xcffc, 0xbf1b, 0xaf3a, 0x9f59, 0x8f78,
0x9188, 0x81a9, 0xb1ca, 0xa1eb, 0xd10c, 0xc12d, 0xf14e, 0xe16f,
0x1080, 0x00a1, 0x30c2, 0x20e3, 0x5004, 0x4025, 0x7046, 0x6067,
0x83b9, 0x9398, 0xa3fb, 0xb3da, 0xc33d, 0xd31c, 0xe37f, 0xf35e,
0x02b1, 0x1290, 0x22f3, 0x32d2, 0x4235, 0x5214, 0x6277, 0x7256,
0xb5ea, 0xa5cb, 0x95a8, 0x8589, 0xf56e, 0xe54f, 0xd52c, 0xc50d,
0x34e2, 0x24c3, 0x14a0, 0x0481, 0x7466, 0x6447, 0x5424, 0x4405,
0xa7db, 0xb7fa, 0x8799, 0x97b8, 0xe75f, 0xf77e, 0xc71d, 0xd73c,
0x26d3, 0x36f2, 0x0691, 0x16b0, 0x6657, 0x7676, 0x4615, 0x5634,
0xd94c, 0xc96d, 0xf90e, 0xe92f, 0x99c8, 0x89e9, 0xb98a, 0xa9ab,
0x5844, 0x4865, 0x7806, 0x6827, 0x18c0, 0x08e1, 0x3882, 0x28a3,
0xcb7d, 0xdb5c, 0xeb3f, 0xfb1e, 0x8bf9, 0x9bd8, 0xabbb, 0xbb9a,
0x4a75, 0x5a54, 0x6a37, 0x7a16, 0x0af1, 0x1ad0, 0x2ab3, 0x3a92,
0xfd2e, 0xed0f, 0xdd6c, 0xcd4d, 0xbdaa, 0xad8b, 0x9de8, 0x8dc9,
0x7c26, 0x6c07, 0x5c64, 0x4c45, 0x3ca2, 0x2c83, 0x1ce0, 0x0cc1,
0xef1f, 0xff3e, 0xcf5d, 0xdf7c, 0xaf9b, 0xbfba, 0x8fd9, 0x9ff8,
0x6e17, 0x7e36, 0x4e55, 0x5e74, 0x2e93, 0x3eb2, 0x0ed1, 0x1ef0
};
            #endregion

            public override string ToString()
            {
                //+ OriginalDeviceType.ToString("X") + ":" + OriginalSubnetID.ToString("X") + ":" + OriginalDeviceID.ToString("X") + "=>"
                return "{" + TargetSubnetID.ToString("X") + ":" + TargetDeviceID.ToString("X") + "~" + OperationCode.ToString("X") + ": " + Util.ByteA2Text(Content) + "}";
            }
        }

        public abstract class PacketContentBase
        {
            public byte[] _data;
            public List<field> _fields;
            public const byte ValueSuccess = 0xF8, ValueFail = 0xF5;
            public const byte ValueTrue = 1, ValueFalse = 0;

            public PacketContentBase(byte[] iData)
            {
                _data = iData;
                _fields = new List<field>();
            }

            /*public List<int> relatedOC()
            {
                return _ocs;
            }

            public bool isRelatedOC(int oc)
            {
                return _ocs.Contains(oc);
            }*/

            public override string ToString()
            {
                string s = name;
                foreach (field f in _fields)
                {
                    if (f.array)
                        s += "(" + f.index + "." + f.name + "[" + f.size + "*" + f.arrayLen + "]" + ")";
                    else
                        s += "(" + f.index + "." + f.name + "[" + f.size + "]=" + (f.size == 1 ? GetByte(f) : GetWord(f)) + ")";
                }
                return s + "}";
            }

            public byte GetByte(int id)
            {
                return _data[id];
            }
            public byte GetByte(field id) { return GetByte(id.index); }
            public byte GetByte(int id, int index) { return GetByte(id + index); }
            public byte GetByte(field id, int index) { return GetByte(id.index + index); }
            public UInt16 GetWord(int id)
            {
                return (UInt16)(_data[id] * 256 + _data[id + 1]);
            }
            public UInt16 GetWord(field id) { return GetWord(id.index); }
            public UInt16 GetWord(int id, int index) { return GetWord(id + index * 2); }
            public UInt16 GetWord(field id, int index) { return GetWord(id.index + index * 2); }

            public void Set(int id, byte value)
            {
                _data[id] = value;
            }
            public void Set(field id, byte value) { Set(id.index, value); }
            public void Set(int id, byte value, int index) { Set(id + index, value); }
            public void Set(field id, byte value, int index) { Set(id.index + index, value); }
            public void Set(int id, UInt16 value)
            {
                _data[id] = (byte)(value >> 8);
                _data[id + 1] = (byte)(value & 0xff);
            }
            public void Set(field id, UInt16 value) { Set(id.index, value); }
            public void Set(int id, UInt16 value, int index) { Set(id + index * 2, value); }
            public void Set(field id, UInt16 value, int index) { Set(id.index + index * 2, value); }

            public void Fill(field id, byte value)
            {
                for (int i = id.index; i < id.index + id.arrayLen; i++)
                    Set(i, value);
            }
            public void Fill(field id, UInt16 value)
            {
                for (int i = id.index; i < id.index + id.arrayLen * 2; i += 2)
                    Set(i, value);
            }

            public string name { get { return this.GetType().Name; } }

            public class field
            {
                public int index;
                public int size;
                public string name;
                public bool array;
                public int arrayLen;
                public field(int iindex, int isize, string iname, bool iarray = false, int iarrayLen = 0)
                {
                    index = iindex;
                    size = isize;
                    name = iname;
                    array = iarray;
                    arrayLen = iarrayLen;
                }
            }
        }

        public abstract class ResponserBase
        {
            public int _requestID;
            public int _responseID;
            public abstract PacketContentBase getResponse(PacketContentBase req);
        }

        public class OCData
        {
            public int OC;
            public string name;
            public PacketContentBase parser;
            public ResponserBase responser;
        }
    }
}

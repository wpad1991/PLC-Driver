using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DriverInterface.Driver;
using DriverLog;
using DriverUtility;


namespace Mitsubishi_Ethernet
{
    public class MelsecEthernet : DriverInterface.Protocol.IProtocolBase
    {

        private QnA_3E deviceInfo = new QnA_3E();

        private int atoAval = -32;

        private string lang = "KR";

        #region Interface

        public string ErrorDebugLANG
        {
            get { return lang; }
            set {
                if (value == "KR")
                {
                    lang = "KR";
                }
                else
                {
                    lang = "EU";
                }
            }
        }

        public MelsecEthernet()
        {
        }

        public bool InitializeProtocol(params object[] values)
        {
            try
            {
                QnA_3E config = (QnA_3E)values[0];

                if (config == null)
                {
                    return false;
                }

                deviceInfo.Binary = config.Binary;
                deviceInfo.Network = config.Network;
                deviceInfo.PLC = config.PLC;
                deviceInfo.IOModule = config.IOModule;
                deviceInfo.Local = config.Local;
                deviceInfo.CPUCheckTimer = config.CPUCheckTimer;
            }
            catch (Exception except)
            {
                DriverManager.Manager.WriteLog("Exception_Driver","[EMelsec : Melsec] : " + except.ToString());
                return false;
            }
            return true;
        }

       // public byte[] SetBlock(byte[] data, string devicecode, int command, bool isread)
        public byte[] SetBlockProtocol(byte[] data, params object[] values)
        {

            string devicecode = (string)values[0];
            int command = (int)values[1];


            if (data == null || devicecode == null || command == 0)
            {
                return null;
            }


            QnA_3E dev = deviceInfo;

            List<byte> totalBlock = new List<byte>();

            /**
             * GetData의 반환 값을 이 함수의 Data 변수로 넣어줘야함
            **/

            /**
             * 1 : 일괄 읽기                : 0401
             * 2 : 일괄 쓰기                : 1401
             * 3 : 복수 블록 랜덤 읽기      : 0406
             * 4 : 복수 블록 랜덤 쓰기      : 1406
             * 5 : 랜덤 읽기                : 0403
            **/

            /** 
             * ASCII
             * 3E Frame Command 5000
             * 3E Frame Response D000 
             * Binary
             * 3E Frame Command 0x50 0x00
             * 3E Frame Response 0xD0 0x00
            **/
           
            /**
             * 복수블록 일괄 읽기 워드단위 0406(0000)
             * 복수블록 일괄 쓰기 워드단위 1406(0000)
             * 일괄 읽기 비트단위 0401(0001)
             * 일괄 읽기 워드단위 0401(0000)
             * 일괄 쓰기 비트단위 1401(0001)
             * 일괄 쓰기 워드단위 1401(0000)
             * 랜덤 읽기 워드단위 0403(0000)
             * 테스트 (랜덤 쓰기) 비트단위 1402(0001)
             * 테스트 (랜덤 쓰기) 워드단위 1402(0000)
             * 모니터 등록 워드단위 0801(0000)
             * 모니터 워드단위 0802(0000)
            **/


            bool isbit = Utility.CheckIsBit(DriverType.MELSEC_Ethernet, devicecode);
            byte[] b_subHeader;
            byte[] b_Qheader;
            byte[] b_command;
            byte[] b_subcommand;

            /** 헤더 x
              * TCP/IP x
              * 서브헤더
              * Q 헤더
              * 커멘드
              * 캐릭터부
              * 서브커멘드
              * 요구데이터부
             **/


            if (dev.Binary)
            {
                b_subHeader = new byte[2];
                b_subcommand = new byte[2];
                b_subHeader[0] = 0x50;
                b_subHeader[1] = 0x00;
                b_Qheader = GetQHeader(data);
                //커멘드 부분
                b_command = GetCommand(command);



                if (isbit)
                {
                    b_subcommand[0] = 0x01;
                    b_subcommand[1] = 0x00;
                }
                else
                {
                    b_subcommand[0] = 0x00;
                    b_subcommand[1] = 0x00;
                }

                totalBlock.AddRange(b_subHeader);
                totalBlock.AddRange(b_Qheader);
                totalBlock.AddRange(b_command);
                totalBlock.AddRange(b_subcommand);
                totalBlock.AddRange(data);


            }
            else
            {
                b_subHeader = ConvertToASCII(0x5000, 4);
                b_Qheader = GetQHeader(data);
                // 커멘드 부분
                b_command = GetCommand(command);

                if (isbit)
                {
                    b_subcommand = ConvertToASCII(0001, 4);
                }
                else
                {
                    b_subcommand = ConvertToASCII(0000, 4);                    
                }

                totalBlock.AddRange(b_subHeader);
                totalBlock.AddRange(b_Qheader);
                totalBlock.AddRange(b_command);
                totalBlock.AddRange(b_subcommand);
                totalBlock.AddRange(data);
            }



            return totalBlock.ToArray();
        }

        public string CheckBlockErrorProtocol(byte[] block)
        {
            try
            {
                QnA_3E dev = deviceInfo;
                int errorcode = 0;
                if (dev.Binary)
                {
                    if (block[0] != 0xD0 || block[1] != 0x00)
                    {
                        return "서브 헤더가 잘 못 되어 있습니다.";
                    }

                    errorcode += block[9];
                    errorcode += block[10] << 8;

                    if (errorcode != 0)
                    {
                        DriverManager.Manager.WriteLog("Exception_Driver","[EMelsec : Melsec] : Error >>>>>>>>>> " + ErrorCodeCheck(errorcode));
                        return ErrorCodeCheck(errorcode);
                    }


                }
                else
                {


                    if (block[0] != 0x44 || block[1] != 0x30 || block[2] != 0x30 || block[3] != 0x30)
                    {
                        return "서브 헤더가 잘 못 되어 있습니다.";
                    }

                    errorcode = ConvertToHexint(block[18]) << 24;
                    errorcode += ConvertToHexint(block[19]) << 16;
                    errorcode += ConvertToHexint(block[20]) << 8;
                    errorcode += ConvertToHexint(block[21]);

                    if (errorcode != 0)
                    {
                        DriverManager.Manager.WriteLog("Exception_Driver","[EMelsec : Melsec] : Error >>>>>>>>>> " + ErrorCodeCheck(errorcode));
                        return ErrorCodeCheck(errorcode);
                    }
                }
            }
            catch (Exception except)
            {
                DriverManager.Manager.WriteLog("Exception_Driver","[EMelsec : Melsec] : " + except.ToString());
                return except.ToString();
            }

            return "OK";
        }

//        public object[] GetData(byte[] block, string deviceCode, int dataCount)
        public object GetDataProtocol(params object[] values)
        {

            try
            {
                byte[] block = (byte[])values[0];
                string deviceCode = (string)values[1];
            
                int dataCount = (int)values[2];
                
                byte[] result;
                
                QnA_3E dev = deviceInfo;
                int startIndex = 0;
                bool isbit = Utility.CheckIsBit(DriverType.MELSEC_Ethernet, deviceCode);
                
                if(!isbit)
                {
                    dataCount *= 2;
                }

                result = new byte[dataCount];

                if (dev.Binary)
                {

                    startIndex = 11;

                    if (isbit)
                    {
                        for (int i = 0; i < dataCount; i++)
                        {
                            if (i % 2 == 0)
                            {
                                
                                result[i] = (byte)(((block[(i / 2) + startIndex] >> 4) & 0xF) == 0 ? 0 : 1);
                            }
                            else
                            {
                                result[i] = (byte)((block[(i / 2) + startIndex] & 0xF) == 0 ? 0 : 1);
                            }
                        }
                    }
                    else
                    {
                        for (int i = 0; i < dataCount; i++)
                        {
                            result[i] = block[i + startIndex];
                        }
                    }

                }
                else
                {

                    startIndex = 22;


                    if (isbit)
                    {
                        for (int i = 0; i < dataCount; i++)
                        {
                            result[i] = (byte)(block[i + startIndex] == 0x30 ? 0 : 1);
                        }
                    }
                    else
                    {
                        int buf = 0;

                        for (int i = 0; i < dataCount; i++)
                        {
                            buf = 0;

                            buf += ConvertToHexint(block[0 + (i * 2) + startIndex]) << 4;
                            buf += ConvertToHexint(block[1 + (i * 2) + startIndex]);

                            result[i] = (byte)buf;
                        }
                    }

                }
                return (object)result;

            }
            catch (Exception except)
            {
                DriverManager.Manager.WriteLog("Exception_Driver","[EMelsec : Melsec] : " + except.ToString());
                return null;
            }

        }

        public byte[] GetDataByteArray(int addr, ushort[] data, string devicecode, bool isread)
        {

            /**
             * if isread
             *      size = data[0]
             * else
             *      data is writedata
            **/

            List<byte> result = new List<byte>();

            result.Clear();

            bool isbit = Utility.CheckIsBit(DriverInterface.Driver.DriverType.MELSEC_Ethernet, devicecode);

            byte[] deviceAddr;
            byte[] deviceCount;
            byte[] dCode;
            byte[] dData;
            dCode = GetDeviceCode(devicecode);

            if (deviceInfo.Binary)
            {

                deviceAddr = new byte[3];


                if (isread)
                {
                    deviceCount = new byte[2];

                    deviceAddr[0] = Convert.ToByte(addr & 0xFF);
                    deviceAddr[1] = Convert.ToByte((addr >> 8) & 0xFF);
                    deviceAddr[2] = Convert.ToByte((addr >> 16) & 0xFF);

                    //deviceCount[0] = Convert.ToByte(data[0]);
                    deviceCount[0] = Convert.ToByte(data[0] & 0xFF);
                    deviceCount[1] = Convert.ToByte((data[0] >> 8) & 0xFF);

                    result.AddRange(deviceAddr);
                    result.AddRange(dCode);
                    result.AddRange(deviceCount);

                }
                else
                {

                    if (isbit)
                    {
                        deviceCount = new byte[2];

                        deviceAddr[0] = Convert.ToByte(addr & 0xFF);
                        deviceAddr[1] = Convert.ToByte((addr >> 8) & 0xFF);
                        deviceAddr[2] = Convert.ToByte((addr >> 16) & 0xFF);

                        dData = ConvertToBianryDigitalArrayWrite(data);

                        deviceCount[0] = Convert.ToByte(data.Length & 0xFF);
                        deviceCount[1] = Convert.ToByte((data.Length >> 8) & 0xFF);

                    }
                    else
                    {
                        deviceCount = new byte[2];

                        deviceAddr[0] = Convert.ToByte(addr & 0xFF);
                        deviceAddr[1] = Convert.ToByte((addr >> 8) & 0xFF);
                        deviceAddr[2] = Convert.ToByte((addr >> 16) & 0xFF);

                        dData = ConvertToBianryAnalogArrayWrite(data);

                        deviceCount[0] = Convert.ToByte(data.Length & 0xFF);
                        deviceCount[1] = Convert.ToByte((data.Length >> 8) & 0xFF);

                    }

                    result.AddRange(deviceAddr);
                    result.AddRange(dCode);
                    result.AddRange(deviceCount);
                    result.AddRange(dData);

                }
            }
            else
            {

                if (isread)
                {
                    deviceAddr = ConvertToASCII(addr, 6);
                    deviceCount = ConvertToASCII(data[0], 4);

                    result.AddRange(dCode);
                    result.AddRange(deviceAddr);
                    result.AddRange(deviceCount);

                }
                else
                {
                    deviceAddr = ConvertToASCII(addr, 6);
                    dData = ConvertToASCIIDigitalArrayWrite(data);

                    deviceCount = ConvertToASCII(dData.Length, 4);

                    result.AddRange(dCode);
                    result.AddRange(deviceAddr);
                    result.AddRange(deviceCount);
                    result.AddRange(dData);

                }



            }


            return result.ToArray();
        }

        #endregion

        #region Utility

        byte[] GetCommand(int command)
        {
            byte[] result;

            int command_code = CheckCommand(command);

            if (command_code == 0)
            {
                DriverManager.Manager.WriteLog("Exception_Driver","[EMelsec : Melsec] : CheckCommand Error coomand_code == 0");
                return null;
            }

            if(deviceInfo.Binary)
            {
                result = new byte[2];

                result[0] = (Convert.ToByte(command_code & 0xFF));
                result[1] = (Convert.ToByte((command_code >> 8) & 0xFF));

            }
            else
            {
                result = ConvertToASCII(command_code, 4);

            }

            return result;
        }

        int CheckCommand(int command)
        {

            /**
             * 1 : 일괄 읽기                : 0401
             * 2 : 일괄 쓰기                : 1401
             * 3 : 복수 블록 랜덤 읽기      : 0406
             * 4 : 복수 블록 랜덤 쓰기      : 1406
             * 5 : 랜덤 읽기                : 0403
            **/

            switch(command)
            {
                case 1:
                    return 0x0401;
                case 2:
                    return 0x1401;
                case 3:
                    return 0x0406;
                case 4:
                    return 0x1406;
                case 5:
                    return 0x0403;
                default:
                    return 0x00;
            }
        }

        byte[] GetQHeader(byte[] data)
        {


            List<byte> qHeader = new List<byte>();
            qHeader.Clear();
            QnA_3E dev = deviceInfo;
            // Q헤더
            //      network
            //      plc
            //      io
            //      local
            //      length
            //      cputimer
            if (dev.Binary)
            {
                qHeader.Add(Convert.ToByte(dev.Network));
                qHeader.Add(Convert.ToByte(dev.PLC));
                qHeader.Add(Convert.ToByte(dev.IOModule & 0xFF)); // L
                qHeader.Add(Convert.ToByte((dev.IOModule >> 8) & 0xFF)); // H
                qHeader.Add(Convert.ToByte(dev.Local));
                
                //요구 데이터부 + CPU감시 타이머 + 커맨드 + 서브커맨드

                int datalength = data.Length + 2 + 2 + 2;


                qHeader.Add(Convert.ToByte(datalength & 0xFF)); // L
                qHeader.Add(Convert.ToByte((datalength >> 8) & 0xFF)); // H
                qHeader.Add(Convert.ToByte(dev.CPUCheckTimer & 0xFF));
                qHeader.Add(Convert.ToByte((dev.CPUCheckTimer >> 8) & 0xFF));
            }
            else
            {
                //요구 데이터부 + CPU감시 타이머 + 커맨드 + 서브커맨드
                int datalength = data.Length + 4 + 4 + 4;

                qHeader.AddRange(ConvertToASCII(dev.Network, 2));
                qHeader.AddRange(ConvertToASCII(dev.PLC, 2));
                qHeader.AddRange(ConvertToASCII(dev.IOModule, 4));
                qHeader.AddRange(ConvertToASCII(dev.Local, 2));
                qHeader.AddRange(ConvertToASCII(datalength, 4));
                qHeader.AddRange(ConvertToASCII(dev.CPUCheckTimer, 4));

            }

            return qHeader.ToArray();
        }

        string ErrorCodeCheck(int code)
        {
            string msg = "";
            if (lang == "KR")
            {
                msg = ErrorCodeCheckKR(code);
            }
            else
            {
                msg = ErrorCodeCheckEU(code);
            }


            return msg;


        }

        string ErrorCodeCheckEU(int code)
        {
            if (code >= 0x4000 && code <= 0x4FFF)
            {
                return "CPU Module detection error (error except MC protocol and communication function) Error Code : " + string.Format("{0:x}", code);
            }

            if (code == 0x55)
            {
                return "If the write during RUN is not enabled, the CPU module has requested to write data during RUN by the external device. Error Code : " + string.Format("{0:x}", code);
            }

            if (code == 0xC050)
            {
                return "In the communication data code setting of the built-in Ethernet port QCPU, ASCII code data that can not be converted into a binary code is received when ASCII code communication is set. Error Code : " + string.Format("{0:x}", code);
            }

            if (code >= 0xC051 && code <= 0xC054)
            {
                return "The read / write score is out of the allowable range. Error Code : " + string.Format("{0:x}", code);
            }

            if (code == 0xC056)
            {
                return "A read / write request exceeding the maximum address has been made. Error Code : " + string.Format("{0:x}", code);
            }

            if (code == 0xC058)
            {
                return "The length of the requested data after ASCII-to-binary conversion does not match the number of characters in the character part (part of the text). Error Code : " + string.Format("{0:x}", code);
            }

            if (code == 0xC059)
            {
                return "Commands and subcommands are specified incorrectly. Or an Ethernet port built-in QCPU. Error Code : " + string.Format("{0:x}", code);
            }

            if (code == 0xC05B)
            {
                return "Can not read or write the built-in QCPU to the specified device. Error Code : " + string.Format("{0:x}", code);
            }

            if (code == 0xC05C)
            {
                return "There is a mistake in the content of the request. (For bit device read / write for word device) Error Code : " + string.Format("{0:x}", code);
            }

            if (code == 0xC05D)
            {
                return "Monitor is not registered. Error Code : " + string.Format("{0:x}", code);
            }

            if (code == 0xC05F)
            {
                return "This is a request that can not be executed for the target CPU module. Error Code : " + string.Format("{0:x}", code);
            }

            if (code == 0xC060)
            {
                return "There is a mistake in the content of the request. (There is a mistake in designation of data for the bit device, etc.) Error Code : " + string.Format("{0:x}", code);
            }

            if (code == 0xC061)
            {
                return "The requested data length does not match the number of characters in the character part (part of the text). Error Code : " + string.Format("{0:x}", code);
            }

            if (code == 0xC06F)
            {
                return "The binary request message was received when the ASCII request message was received when the communication data code setting was binary or when the communication data code setting was ASCII. (This error code registers only the error history and no abnormal responses are returned.) Error Code : " + string.Format("{0:x}", code);
            }

            if (code == 0xC070)
            {
                return "Device memory expansion can not be specified for the target station. Error Code : " + string.Format("{0:x}", code);
            }

            if (code == 0xC0B5)
            {
                return "Data that can not be handled by the CPU module is specified. Error Code : " + string.Format("{0:x}", code);
            }

            if (code == 0xC200)
            {
                return "There is a mistake in the remote password. Error Code : " + string.Format("{0:x}", code);
            }

            if (code == 0xC201)
            {
                return "When the port used for communication is locked by the remote password or when the communication data code setting is ASCII code, the subcommand can not be converted to a binary code because it is locked by the remote password. Error Code : " + string.Format("{0:x}", code);
            }

            if (code == 0xC204)
            {
                return "This is different from the device that requested release processing of the remote password. Error Code : " + string.Format("{0:x}", code);
            }

            return "Unknown error code. Error Code : " + string.Format("{0:x}", code);
        }
        
        string ErrorCodeCheckKR(int code)
        {


            if (code >= 0x4000 && code <= 0x4FFF)
            {
                return "CPU 모듈이 검출한 에러 (MC 프로토콜에 의한 통신 기능 이외에서 발생한 에러) Error Code : " + string.Format("{0:x}", code);
            }

            if (code == 0x55)
            {
                return "RUN 중 쓰기를 허가로 하지 않은 경우에 상대 기기에 의해 CPU 모듈이 RUN 중에 데이터의 쓰기를 요구하였다. Error Code : " + string.Format("{0:x}", code);
            }

            if (code == 0xC050)
            {
                return "Ethernet 포트 내장 QCPU의 교신 데이터 코드 설정에서 ASCII 코드 교신 설정 시 바이너리 코드로 변환할 수 없는 ASCII 코드의 데이터를 수신하였다. Error Code : " + string.Format("{0:x}", code);
            }

            if (code >= 0xC051 && code <= 0xC054)
            {
                return "읽기/쓰기 점수가 허용 범위를 벗어난다. Error Code : " + string.Format("{0:x}", code);
            }

            if (code == 0xC056)
            {
                return "최대 어드레스를 초과하는 읽기/쓰기 요구를 하였다. Error Code : " + string.Format("{0:x}", code);
            }

            if (code == 0xC058)
            {
                return "ASCII - 바이너리 변환 후의 요구 데이터 길이가 캐릭터부(텍스트의 일부)의 데이터수와 맞지 않는다. Error Code : " + string.Format("{0:x}", code);
            }

            if (code == 0xC059)
            {
                return "커맨드, 서브 커맨드가 잘못 지정되어 있다. 또는 Ethernet 포트 내장 QCPU에서는 사용할 수 없는 커맨드, 서브 커맨드다. Error Code : " + string.Format("{0:x}", code);
            }

            if (code == 0xC05B)
            {
                return "지정 디바이스에 대해서 Ethernet 포트 내장 QCPU를 읽거나 쓸 수 없다. Error Code : " + string.Format("{0:x}", code);
            }

            if (code == 0xC05C)
            {
                return "요구 내용에 잘못이 있다. (워드 디바이스에 대한 비트 단위의 읽기/쓰기 시) Error Code : " + string.Format("{0:x}", code);
            }

            if (code == 0xC05D)
            {
                return "모니터 등록이 되어 있지 않다. Error Code : " + string.Format("{0:x}", code);
            }

            if (code == 0xC05F)
            {
                return "대상 CPU 모듈에 대해서 실행할 수 없는 요구다. Error Code : " + string.Format("{0:x}", code);
            }

            if (code == 0xC060)
            {
                return "요구 내용에 잘못이 있다. (비트 디바이스에 대한 데이터의 지정에 잘못이 있는 등) Error Code : " + string.Format("{0:x}", code);
            }

            if (code == 0xC061)
            {
                return "요구 데이터 길이가 캐릭터부(텍스트의 일부)의 데이터 수와 맞지 않는다. Error Code : " + string.Format("{0:x}", code);
            }

            if (code == 0xC06F)
            {
                return "교신 데이터 코드 설정이 바이너리 일 때 ASCII의 요구 스테이트먼트를 수신하였거나, 교신 데이터 코드 설정이 ASCII일 때 바이너리의 요구 스테이트먼트를 수신하였다. (이 에러 코드는 에러 이력만 등록되고 이상 응답은 반환되지 않는다.) Error Code : " + string.Format("{0:x}", code);
            }

            if (code == 0xC070)
            {
                return "대상국에 대해서는 디바이스 메모리의 확장 지정은 할 수 없다. Error Code : " + string.Format("{0:x}", code);
            }

            if (code == 0xC0B5)
            {
                return "CPU 모듈에서 취급할 수 없는 데이터가 지정되었다. Error Code : " + string.Format("{0:x}", code);
            }

            if (code == 0xC200)
            {
                return "리모트 패스워드에 잘못이 있다. Error Code : " + string.Format("{0:x}", code);
            }

            if (code == 0xC201)
            {
                return "교신에 사용한 포트가 리모트 패스워드로 잠금 상태이거나 교신 데이터 코드 설정이 ASCII 코드일 때 리모트 패스워드에 의해 잠금 상태이므로 서브 커맨드 이후를 바이너리 코드로 변환할 수 없다. Error Code : " + string.Format("{0:x}", code);
            }

            if (code == 0xC204)
            {
                return "리모트 패스워드의 해제 처리를 요구한 기기와 다르다. Error Code : " + string.Format("{0:x}", code);
            }

            return "알 수 없는 Error 코드입니다. Error Code : " + string.Format("{0:x}", code);
        }
        
        bool CheckIsBit(int devicecode)
        {

            
            switch (devicecode)
            {
                case 0x9C:
                    return true;
                case 0x9D:
                    return true;
                case 0x90:
                    return true;
                case 0x92:
                    return true;
                case 0x93:
                    return true;
                case 0x94:
                    return true;
                case 0xA0:
                    return true;
                case 0xA8:
                    return false;
                case 0xB4:
                    return false;
                case 0x91:
                    return true;
                case 0xA9:
                    return false;
                case 0xC1:
                    return true;
                case 0xC0:
                    return true;
                case 0xC2:
                    return false;
                case 0xC7:
                    return true;
                case 0xC6:
                    return true;
                case 0xC8:
                    return false;
                case 0xC4:
                    return true;
                case 0xC3:
                    return true;
                case 0xC5:
                    return false;
                case 0xA1:
                    return true;
                case 0xB5:
                    return false;
                case 0x98:
                    return true;
                case 0xA2:
                    return true;
                case 0xA3:
                    return true;
                case 0xCC:
                    return false;
                case 0xAF:
                    return false;
                case 0xD0:
                    return false;
                default:
                    DriverManager.Manager.WriteLog("Exception_Driver","[EMelsec : Melsec] : GetDevicecode Code Error return null >>>>> " + devicecode);
                    return false;

            }
        }

        byte[] GetDeviceCode(string devicecode)
        {
            byte[] result;
            string str = devicecode.Replace("*", "");
          
            if (deviceInfo.Binary)
            {

                result = new byte[1];
                switch (str)
                {
                    case "X":
                        result[0] = 0x9C;
                        break;
                    case "Y":
                        result[0] = 0x9D;
                        break;
                    case "M":
                        result[0] = 0x90;
                        break;
                    case "L":
                        result[0] = 0x92;
                        break;
                    case "F":
                        result[0] = 0x93;
                        break;
                    case "V":
                        result[0] = 0x94;
                        break;
                    case "B":
                        result[0] = 0xA0;
                        break;
                    case "D":
                        result[0] = 0xA8;
                        break;
                    case "W":
                        result[0] = 0xB4;
                        break;
                    case "SM":
                        result[0] = 0x91;
                        break;
                    case "SD":
                        result[0] = 0xA9;
                        break;
                    case "TS":
                        result[0] = 0xC1;
                        break;
                    case "TC":
                        result[0] = 0xC0;
                        break;
                    case "TN":
                        result[0] = 0xC2;
                        break;
                    case "SS":
                        result[0] = 0xC7;
                        break;
                    case "SC":
                        result[0] = 0xC6;
                        break;
                    case "SN":
                        result[0] = 0xC8;
                        break;
                    case "CS":
                        result[0] = 0xC4;
                        break;
                    case "CC":
                        result[0] = 0xC3;
                        break;
                    case "CN":
                        result[0] = 0xC5;
                        break;
                    case "SB":
                        result[0] = 0xA1;
                        break;
                    case "SW":
                        result[0] = 0xB5;
                        break;
                    case "S":
                        result[0] = 0x98;
                        break;
                    case "DX":
                        result[0] = 0xA2;
                        break;
                    case "DY":
                        result[0] = 0xA3;
                        break;
                    case "Z":
                        result[0] = 0xCC;
                        break;
                    case "R":
                        result[0] = 0xAF;
                        break;
                    case "ZR":
                        result[0] = 0xB0;
                        break;
                    default:
                        DriverManager.Manager.WriteLog("Exception_Driver","[EMelsec : Melsec] : GetDevicecode Code Error return null >>>>> " + devicecode);
                        return null;

                }
            }
            else
            { 
                result = new byte[2];
                switch (str)
                {
                    case "X":
                        result[0] = Convert.ToByte('X');
                        result[1] = Convert.ToByte('*');
                        break;
                    case "Y":
                        result[0] = Convert.ToByte('Y');
                        result[1] = Convert.ToByte('*');
                        break;
                    case "M":
                        result[0] = Convert.ToByte('M');
                        result[1] = Convert.ToByte('*');
                        break;
                    case "L":
                        result[0] = Convert.ToByte('L');
                        result[1] = Convert.ToByte('*');
                        break;
                    case "F":
                        result[0] = Convert.ToByte('F');
                        result[1] = Convert.ToByte('*');
                        break;
                    case "V":
                        result[0] = Convert.ToByte('V');
                        result[1] = Convert.ToByte('*');
                        break;
                    case "B":
                        result[0] = Convert.ToByte('B');
                        result[1] = Convert.ToByte('*');
                        break;
                    case "D":
                        result[0] = Convert.ToByte('D');
                        result[1] = Convert.ToByte('*');
                        break;
                    case "W":
                        result[0] = Convert.ToByte('W');
                        result[1] = Convert.ToByte('*');
                        break;
                    case "SM":
                        result[0] = Convert.ToByte('S');
                        result[1] = Convert.ToByte('M');
                        break;
                    case "SD":
                        result[0] = Convert.ToByte('S');
                        result[1] = Convert.ToByte('D');
                        break;
                    case "TS":
                        result[0] = Convert.ToByte('T');
                        result[1] = Convert.ToByte('S');
                        break;
                    case "TC":
                        result[0] = Convert.ToByte('T');
                        result[1] = Convert.ToByte('C');
                        break;
                    case "TN":
                        result[0] = Convert.ToByte('T');
                        result[1] = Convert.ToByte('N');
                        break;
                    case "SS":
                        result[0] = Convert.ToByte('S');
                        result[1] = Convert.ToByte('S');
                        break;
                    case "SC":
                        result[0] = Convert.ToByte('S');
                        result[1] = Convert.ToByte('C');
                        break;
                    case "SN":
                        result[0] = Convert.ToByte('S');
                        result[1] = Convert.ToByte('N');
                        break;
                    case "CS":
                        result[0] = Convert.ToByte('C');
                        result[1] = Convert.ToByte('S');
                        break;
                    case "CC":
                        result[0] = Convert.ToByte('C');
                        result[1] = Convert.ToByte('C');
                        break;
                    case "CN":
                        result[0] = Convert.ToByte('C');
                        result[1] = Convert.ToByte('N');
                        break;
                    case "SB":
                        result[0] = Convert.ToByte('S');
                        result[1] = Convert.ToByte('B');
                        break;
                    case "SW":
                        result[0] = Convert.ToByte('S');
                        result[1] = Convert.ToByte('W');
                        break;
                    case "S":
                        result[0] = Convert.ToByte('S');
                        result[1] = Convert.ToByte('*');
                        break;
                    case "DX":
                        result[0] = Convert.ToByte('D');
                        result[1] = Convert.ToByte('X');
                        break;
                    case "DY":
                        result[0] = Convert.ToByte('D');
                        result[1] = Convert.ToByte('Y');
                        break;
                    case "Z":
                        result[0] = Convert.ToByte('Z');
                        result[1] = Convert.ToByte('*');
                        break;
                    case "R":
                        result[0] = Convert.ToByte('R');
                        result[1] = Convert.ToByte('*');
                        break;
                    case "ZR":
                        result[0] = Convert.ToByte('Z');
                        result[1] = Convert.ToByte('R');
                        break;
                    default:
                        DriverManager.Manager.WriteLog("Exception_Driver","[EMelsec : Melsec] : GetDevicecode Code Error return null >>>>> " + devicecode);
                        return null;
                }

            }

            return result;
        }

        byte[] ConvertToASCII(int data, int size)
        {
            /**
             * Input Data : 16진수
             **/

            try
            {
                byte[] dataArray = new byte[size];

                string dataStr = "";
                int dataLength = 0;

                dataStr = string.Format("{0:x}", data);

                dataLength = dataStr.Length;

                if (dataLength > size)
                {
                    return null;
                }

                if (dataLength < size)
                {
                    int linter = size - dataLength;

                    for (int i = 0; i < linter; i++)
                    {
                        dataStr = "0" + dataStr;
                    }

                }

                for (int i = 0; i < size; i++)
                {
                    if (char.IsLetter(dataStr[i]))
                    {
                        dataArray[i] = Convert.ToByte(dataStr[i] + atoAval);
                    }
                    else
                    {
                        dataArray[i] = Convert.ToByte(dataStr[i]);
                    }
                }

                return dataArray;
            }
            catch (Exception except)
            {
                DriverManager.Manager.WriteLog("Exception_Driver",except.ToString());
                return null;
            }
        }

        ushort ConvertToHexushort(byte data)
        {
            switch (data)
            { 
                case 0x30:
                    return 0x0;
                case 0x31:
                    return 0x1;
                case 0x32:
                    return 0x2;
                case 0x33:
                    return 0x3;
                case 0x34:
                    return 0x4;
                case 0x35:
                    return 0x5;
                case 0x36:
                    return 0x6;
                case 0x37:
                    return 0x7;
                case 0x38:
                    return 0x8;
                case 0x39:
                    return 0x9;
                case 0x41:
                    return 0xA;
                case 0x42:
                    return 0xB;
                case 0x43:
                    return 0xC;
                case 0x44:
                    return 0xD;
                case 0x45:
                    return 0xE;
                case 0x46:
                    return 0xF;
                default:
                    DriverManager.Manager.WriteLog("Exception_Driver","[EMelsec : Melsec] : ConvertToHexushort Error return 0 >>>>> " + data);
                    
                    return 0x0;

            }

        }

        int ConvertToHexint(byte data)
        {
            switch (data)
            {
                case 0x30:
                    return 0x0;
                case 0x31:
                    return 0x1;
                case 0x32:
                    return 0x2;
                case 0x33:
                    return 0x3;
                case 0x34:
                    return 0x4;
                case 0x35:
                    return 0x5;
                case 0x36:
                    return 0x6;
                case 0x37:
                    return 0x7;
                case 0x38:
                    return 0x8;
                case 0x39:
                    return 0x9;
                case 0x41:
                    return 0xA;
                case 0x42:
                    return 0xB;
                case 0x43:
                    return 0xC;
                case 0x44:
                    return 0xD;
                case 0x45:
                    return 0xE;
                case 0x46:
                    return 0xF;
                default:
                    DriverManager.Manager.WriteLog("Exception_Driver","[EMelsec : Melsec] : ConvertToHexushort Error return 0 >>>>> " + data);

                    return 0x0;

            }

        }

        byte[] ConvertToASCIIDigitalArrayWrite(ushort[] data)
        {
            if (data.Length == 0)
            {
                return null;
            }


            byte[] result = new byte[data.Length];

            for (int i = 0; i < data.Length; i++)
            {
                try
                {
                    result[i] = Convert.ToByte(data[i]);
                }
                catch (Exception except)
                {
                    DriverManager.Manager.WriteLog("Exception_Driver","[EMelsec : Melsec] ConverToASCIIDigitalArrayWrite : " + except.ToString());
                }
            }

            return result;
        }

        byte[] ConvertToBianryDigitalArrayWrite(ushort[] data)
        {
            if (data.Length == 0)
            {
                return null;
            }


            int i_a = data.Length / 2;
            int i_b = data.Length % 2;

            List<byte> result = new List<byte>();
            byte r_buf = 0;

            result.Clear();

            
                
            for (int i = 0; i < i_a + i_b; i++)
            {
                try
                {

                    r_buf = Convert.ToByte((data[(i * 2) + 0] == 0 ? 0 : 1) << 4);

                    if ((i * 2) + 1 < data.Length)
                    {
                        r_buf += Convert.ToByte(data[(i * 2) + 1] == 0 ? 0 : 1);
                    }
                    result.Add(r_buf);
                }
                catch(Exception except)
                {
                    result.Add(r_buf);
                    DriverManager.Manager.WriteLog("Exception_Driver","[EMelsec : Melsec] : " + except.ToString());
                }
                
            }

            return result.ToArray();
            
            
        }

        byte[] ConvertToBianryAnalogArrayWrite(ushort[] data)
        {
            if (data.Length == 0)
            {
                return null;
            }


            int i_a = data.Length / 2;
            int i_b = data.Length % 2;

            List<byte> result = new List<byte>();

            result.Clear();


            foreach (ushort d in data)
            {
                result.Add(Convert.ToByte(d & 0xFF));
                result.Add(Convert.ToByte((d >> 8) & 0xFF));
            }

            return result.ToArray();


        }

        #endregion
    }
}

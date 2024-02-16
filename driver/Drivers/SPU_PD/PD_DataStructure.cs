using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace SPU_PD
{
    [StructLayout(LayoutKind.Explicit)]
    public struct Packet_PD_2R
    {
        [FieldOffset(0)] public byte FrameIndex;
        [FieldOffset(1)] public short FrameLength;
        [FieldOffset(3)] public byte DASAddress;
        [FieldOffset(4)] public short Year;
        [FieldOffset(6)] public byte Month;
        [FieldOffset(7)] public byte Day;
        [FieldOffset(8)] public byte Hour;
        [FieldOffset(9)] public byte Minite;
        [FieldOffset(10)] public byte Second;
        [FieldOffset(11)] public byte _3SInfo;
        [FieldOffset(12)] public byte _4SInfo;
        [FieldOffset(13)] public byte _6SInfo;
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct Packet_PD_2S
    {
        [FieldOffset(0)] public byte FrameIndex;
        [FieldOffset(1)] public short FrameLength;
        [FieldOffset(3)] public byte DASAddress;
        [FieldOffset(4)] public short Year;
        [FieldOffset(6)] public byte Month;
        [FieldOffset(7)] public byte Day;
        [FieldOffset(8)] public byte Hour;
        [FieldOffset(9)] public byte Minite;
        [FieldOffset(10)] public byte Second;
        [FieldOffset(11)] public byte _4S_Request;
        [FieldOffset(12)] public byte InternalFilterMode;
        [FieldOffset(13)] public short Voltage;
        [FieldOffset(15)] public short Frequency;
        [FieldOffset(17)] public byte MAX1;
        [FieldOffset(18)] public byte MAX2;
        [FieldOffset(19)] public byte AVG1;
        [FieldOffset(20)] public byte AVG2;
        [FieldOffset(21)] public short PPS1;
        [FieldOffset(23)] public short PPS2;
        [FieldOffset(25)] public byte EventCount1;
        [FieldOffset(26)] public byte EventCount2;
        [FieldOffset(27)] public byte HIQ1;
        [FieldOffset(28)] public byte HIQ2;
        [FieldOffset(29)] public byte HIN1;
        [FieldOffset(30)] public byte HIN2;
        [FieldOffset(31)] public byte EventPer15Min;
        [FieldOffset(32)] public byte HIQC_Alarm;
        [FieldOffset(33)] public byte HINC_Alarm;
        [FieldOffset(34)] public byte reserved;
        [FieldOffset(35)] public short Sensor1Temp;
        [FieldOffset(37)] public short Sensor1Humi;
        [FieldOffset(39)] public short Sensor2Temp;
        [FieldOffset(41)] public short Sensor2Humi;
        [FieldOffset(43)] public short TotalEventNo;
        [FieldOffset(45)] public byte DO123;
    }
    
    static public class PacketManager
    {
        static public void ReverseByte_2R(ref byte[] data)
        {
            Array.Reverse(data, 1, 2);
            Array.Reverse(data, 4, 2);
        }

        static public void ReverseByte_2S(ref byte[] data)
        {
            Array.Reverse(data, 1, 2);
            Array.Reverse(data, 4, 2);
            Array.Reverse(data, 13, 2);
            Array.Reverse(data, 15, 2);
            Array.Reverse(data, 21, 2);
            Array.Reverse(data, 23, 2);
            Array.Reverse(data, 35, 2);
            Array.Reverse(data, 37, 2);
            Array.Reverse(data, 38, 2);
            Array.Reverse(data, 39, 2);
            Array.Reverse(data, 41, 2);
            Array.Reverse(data, 43, 2);
        }


        static public byte[] StructureToByte(object obj)
        {
            int datasize = Marshal.SizeOf(obj);//((PACKET_DATA)obj).TotalBytes; // 구조체에 할당된 메모리의 크기를 구한다.
            IntPtr buff = Marshal.AllocHGlobal(datasize); // 비관리 메모리 영역에 구조체 크기만큼의 메모리를 할당한다.
            Marshal.StructureToPtr(obj, buff, false); // 할당된 구조체 객체의 주소를 구한다.
            byte[] data = new byte[datasize]; // 구조체가 복사될 배열
            Marshal.Copy(buff, data, 0, datasize); // 구조체 객체를 배열에 복사
            Marshal.FreeHGlobal(buff); // 비관리 메모리 영역에 할당했던 메모리를 해제함

            return data; // 배열을 리턴
        }

        /*
        public static object ByteToStructure(byte[] data, Type type)
        {
            IntPtr buff = Marshal.AllocHGlobal(data.Length); // 배열의 크기만큼 비관리 메모리 영역에 메모리를 할당한다.
            Marshal.Copy(data, 0, buff, data.Length); // 배열에 저장된 데이터를 위에서 할당한 메모리 영역에 복사한다.
            object obj = Marshal.PtrToStructure(buff, type); // 복사된 데이터를 구조체 객체로 변환한다.
            Marshal.FreeHGlobal(buff); // 비관리 메모리 영역에 할당했던 메모리를 해제함
            
            if (Marshal.SizeOf(obj) != data.Length)// (((PACKET_DATA)obj).TotalBytes != data.Length) // 구조체와 원래의 데이터의 크기 비교
            {
                return null; // 크기가 다르면 null 리턴
            }
            return obj; // 구조체 리턴

        }*/

        static public T ByteToStruct<T>(byte[] buffer) where T : struct
        {
            int size = Marshal.SizeOf(typeof(T));

            if (size > buffer.Length)
            {
                throw new Exception();
            }

            IntPtr ptr = Marshal.AllocHGlobal(size);
            Marshal.Copy(buffer, 0, ptr, size);
            T obj = (T)Marshal.PtrToStructure(ptr, typeof(T));
            Marshal.FreeHGlobal(ptr);

            return obj;
        }
    }
}

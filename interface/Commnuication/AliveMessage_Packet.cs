using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace DriverInterface.Commnuication
{
    /// Alive Message Packet Define
    /// Header(20 byte) + Body(n);

    [StructLayout(LayoutKind.Explicit)]
    public struct AliveMessageHeader
    {
        [FieldOffset(0)] public byte code1; // G
        [FieldOffset(1)] public byte code2; // W 
        [FieldOffset(2)] public byte code3; // A
        [FieldOffset(3)] public byte isRun;

        [FieldOffset(4)] public byte version1;
        [FieldOffset(5)] public byte version2;
        [FieldOffset(6)] public byte version3;
        [FieldOffset(7)] public byte version4;

        [FieldOffset(8)] public short versionYear;
        //          (9)
        [FieldOffset(10)] public byte versionMonth;
        [FieldOffset(11)] public byte versionDay;

        [FieldOffset(12)] public byte versionHour;
        [FieldOffset(13)] public byte versionMin;
        [FieldOffset(14)] public byte versionSec;
        [FieldOffset(15)] public byte reserved1;

        [FieldOffset(16)] public byte reserved2;
        [FieldOffset(17)] public byte reserved3;
        [FieldOffset(18)] public short bodyLength;
        //          (19)
    }

    /// AliveMessage Body
    /// BodyType1, Length1 , Body1, BodyType2, Length2, Body2, ...
    
    public enum AliveMessageBodyType
    {
        Name = 10,
        Error = 11,
        Exception = 12,
        Alarm = 13
    }

    public class AliveMessageBody
    {
        public AliveMessageBodyType Type;
        public int Length;
        private byte[] body;

        public byte[] Body
        {
            get
            {
                return body;
            }

            set
            {
                body = value;

                if (body != null)
                    Length = body.Length;
            }
        }
    }

    static public class PacketManager
    {
        static public byte[] StructureToByte(object obj)
        {
            int datasize = Marshal.SizeOf(obj);//((PACKET_DATA)obj).TotalBytes; // 구조체에 할당된 메모리의 크기를 구한다.
            IntPtr buff = Marshal.AllocHGlobal(datasize); // 비관리 메모리 영역에 구조체 크기만큼의 메모리를 할당한다.
            Marshal.StructureToPtr(obj, buff, false); // 할당된 구조체 객체의 주소를 구한다.
            byte[] data = new byte[datasize]; // 구조체가 복사될 배열
            Marshal.Copy(buff, data, 0, datasize); // 구조체 객체를 배열에 복사
            Marshal.FreeHGlobal(buff); // 비관리 메모리 영역에 할당했던 메모리를 해제함

            return data;
        }

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

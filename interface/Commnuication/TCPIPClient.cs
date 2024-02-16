using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Threading;
using System.Net;
using System.Diagnostics;

namespace DriverInterface.Commnuication
{
    public class TCPIPClient : ICommClientBase
    {
        NetworkStream stream = null;
        Socket client = null;

        public bool ConnectedComm
        {
            get 
            {
                if (client == null)
                {
                    return false;
                }
                else
                {
                    return client.Connected;
                }
            }
        }

        bool ICommClientBase.ConnectedComm
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public bool ConfigurationComm(params object[] values)
        {
            return true;
        }

        public bool InitializeComm()
        {
            return true;
        }

        public TCPIPClient()
        {
        }

        public bool ConnectComm(params object[] values)
        {
            try
            {
                string ipaddr = (string)values[0];
                int port = (int)values[1];

                client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

                client.Connect(ipaddr, port);

                if (client.Connected)
                {
                    stream = new NetworkStream(client);

                }
                else
                {

                    Console.WriteLine("Console", "[TCPIP Client] : Client Fail IP : " + ipaddr + ", Port : " + port);
                }
                return client.Connected;
            }
            catch (Exception except)
            {
                Console.WriteLine("[TCPIP Client] : " + except.ToString());
                return false;
            }

        }

        public void WriteComm(byte[] data)
        {
            stream.Write(data, 0, data.Length);
        }

        public void ReadComm(byte[] data)
        {
            stream.Read(data, 0, data.Length);
        }

        public bool DisconnectComm()
        {
            client.Close();

            return client.Connected;
        }

        public void Flush()
        {
            stream.Flush();
        }
    }
}

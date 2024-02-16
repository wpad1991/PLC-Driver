using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace DriverUtility
{
    public class DriverThread
    {
        bool isExecute = false;
        ManualResetEvent stopEvent = new ManualResetEvent(false);
        public int Interval = 500;
        Func<string[], bool> func = null;
        string[] arguments = null;

        public DriverThread(Func<string[], bool> func, string[] args)
        {
            if (func == null)
            {
                throw new ArgumentNullException();
            }

            arguments = args;
            this.func = func;
        }

        public void Start()
        {
            if (!isExecute)
            {
                stopEvent.Reset();

                ThreadPool.QueueUserWorkItem(_ => Execute());
            }
        }

        public void Stop()
        {
            stopEvent.Set();
        }

        private void Execute()
        {
            isExecute = true;
            while (!stopEvent.WaitOne((int)Interval))
            {
                if (!func(arguments))
                {
                    break;
                }
            }
            isExecute = false;
        }
    }
}

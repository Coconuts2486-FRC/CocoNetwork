using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CocoNet;
using System.Net;

namespace CocoNetApp
{
    class Program
    {
        static void Main(string[] args)
        {
            AsynchronousSocketListener.StartListening(Dns.GetHostName().ToString(), 5800);
        }
    }
}

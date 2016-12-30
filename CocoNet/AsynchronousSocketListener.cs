using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CocoNet
{
    // State object for reading client data asynchronously
    public class StateObject
    {
        // Client  socket.
        public Socket workSocket = null;
        // Size of receive buffer.
        public const int BufferSize = 1024;
        // Receive buffer.
        public byte[] buffer = new byte[BufferSize];
        // Received data string.
        public StringBuilder sb = new StringBuilder();
    }

    public class AsynchronousSocketListener
    {
        // Thread signal.
        public static ManualResetEvent allDone = new ManualResetEvent(false);

        /// <summary>
        /// Listens with the specified team number as the address. Uses mDNS.
        /// </summary>
        /// <remarks>Automatically creates the socket on port 5805, which is legal as of 2016.</remarks>
        /// <param name="teamNumber">The team number, which is translated into the 2016's mDNS protocol.</param>
        /// <example><code>
        /// StartListening(2486);
        /// </code></example>
        public static void StartListening(int teamNumber)
        {
            
        }

        /// <summary>
        /// Listens with the specified team number and port for the socket.
        /// </summary>
        /// <param name="teamNumber">The team number, which is translated into the 2016's mDNS protocol.</param>
        /// <param name="port">The port to use. Standard ports range from 5800 to 5810, which are legal as of 2016.</param>
        /// <remarks>Ensure that the port on the client and server match.</remarks>
        /// <example><code>
        /// StartListening(2486, 5800);
        /// </code></example>
        public static void StartListening(int teamNumber, int port)
        {
            StartListening("roborio-" + teamNumber.ToString() + "-frc.local", port);
        }

        /// <summary>
        /// Listens with the specified address and port.
        /// </summary>
        /// <param name="address"></param>
        /// <param name="port"></param>
        public static void StartListening(string address, int port)
        {
            // Data buffer for incoming data.
            byte[] bytes = new Byte[1024];

            // Get the IP address
            IPAddress ipAddress = IPAddress.Parse(address);
            IPEndPoint localEndPoint = null;
            Socket listener = null;
            switch (ipAddress.AddressFamily)
            {
                // IPv4
                case AddressFamily.InterNetwork:
                    // Creates an object that holds the IP address and port.
                    localEndPoint = new IPEndPoint(ipAddress, port);
                    // Creates the socket with IPv4.
                    listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    break;
                // IPv6
                case AddressFamily.InterNetworkV6:
                    // Creates an object that holds the IP address and port.
                    localEndPoint = new IPEndPoint(ipAddress, port);
                    // Creates the socket with IPv6.
                    listener = new Socket(AddressFamily.InterNetworkV6, SocketType.Stream, ProtocolType.Tcp);
                    break;
            }


            // Bind the socket to the local endpoint and listen for incoming connections.
            try
            {
                listener.Bind(localEndPoint);
                listener.Listen(20);

                while (true)
                {
                    // Set the event to nonsignaled state.
                    allDone.Reset();

                    // Start an asynchronous socket to listen for connections.
                    Console.WriteLine("Waiting for a connection...");
                    listener.BeginAccept(
                        new AsyncCallback(AcceptCallback),
                        listener);

                    // Wait until a connection is made before continuing.
                    allDone.WaitOne();
                }

            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }

            Console.WriteLine("\nPress ENTER to continue...");
            Console.Read();

        }

        public static void AcceptCallback(IAsyncResult ar)
        {
            // Signal the main thread to continue.
            allDone.Set();

            // Get the socket that handles the client request.
            Socket listener = (Socket)ar.AsyncState;
            Socket handler = listener.EndAccept(ar);

            // Create the state object.
            StateObject state = new StateObject();
            state.workSocket = handler;
            handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                new AsyncCallback(ReadCallback), state);
        }

        public static void ReadCallback(IAsyncResult ar)
        {
            String content = String.Empty;

            // Retrieve the state object and the handler socket
            // from the asynchronous state object.
            StateObject state = (StateObject)ar.AsyncState;
            Socket handler = state.workSocket;

            // Read data from the client socket. 
            int bytesRead = handler.EndReceive(ar);

            if (bytesRead > 0)
            {
                // There might be more data, so store the data received so far.
                state.sb.Append(Encoding.ASCII.GetString(
                    state.buffer, 0, bytesRead));

                // Check for end-of-file tag. If it is not there, read more data.
                content = state.sb.ToString();
                if (content.IndexOf("<EOF>") > -1)
                {
                    // All the data has been read from the client. Display it on the console.
                    Console.WriteLine("Read {0} bytes from socket. \n Data : {1}",
                        content.Length, content);
                    // Echo the data back to the client.
                    Send(handler, content);
                }
                else
                {
                    // Not all data received. Get more.
                    handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                    new AsyncCallback(ReadCallback), state);
                }
            }
        }

        private static void Send(Socket handler, String data)
        {
            // Convert the string data to byte data using ASCII encoding.
            byte[] byteData = Encoding.ASCII.GetBytes(data);

            // Begin sending the data to the remote device.
            handler.BeginSend(byteData, 0, byteData.Length, 0,
                new AsyncCallback(SendCallback), handler);
        }

        private static void SendCallback(IAsyncResult ar)
        {
            try
            {
                // Retrieve the socket from the state object.
                Socket handler = (Socket)ar.AsyncState;

                // Complete sending the data to the remote device.
                int bytesSent = handler.EndSend(ar);
                Console.WriteLine("Sent {0} bytes to client.", bytesSent);

                handler.Shutdown(SocketShutdown.Both);
                handler.Close();

            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }
    }
}
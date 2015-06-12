using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TestCustomWebSocket
{
    class Program
    {
        static void Main(string[] args)
        {
            WebSocketServer server = new WebSocketServer("127.0.0.1", 80);
            string msg = "";
            do
            {
                if (msg != "" && server.connectedClient.Count > 0)
                {
                    server.sendBlobToClient(server.connectedClient[0], Encoding.UTF8.GetBytes(msg));
                }
                msg = Console.ReadLine();
            } while (msg != "/exit");
        }
    }
}

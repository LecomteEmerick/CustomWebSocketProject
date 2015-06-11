using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Net;
using System.Threading;
using System.Security.Cryptography;
using System.Drawing;
using System.IO;

namespace TestCustomWebSocket
{
    class Program
    {
        private static TcpListener server;
        private static List<TcpClient> connectedClient;

        static void Main(string[] args)
        {
            server = new TcpListener(IPAddress.Parse("127.0.0.1"), 80);

            connectedClient = new List<TcpClient>();

            server.Start();
            Console.WriteLine("Server has started on 127.0.0.1:80.{0}Waiting for a connection...", Environment.NewLine);

            Task clientConnection = new Task(CheckClientConnection);
            clientConnection.Start();

            Task clientMessage = new Task(CheckClientMessage);
            clientMessage.Start();

            string msg="";
            do{
                if (msg != "" && connectedClient.Count > 0)
                {
                    Image newImage = Image.FromFile("C:/Users/elecomte/Documents/GitHub/CustomWebSocketProject/Ressources/daililama.jpg");
                    sendMessageToClient(connectedClient[0], Encoding.UTF8.GetBytes(msg)); //imageToByteArray(newImage)
                }
                msg = Console.ReadLine();
            }while(msg != "/exit");
        }

        static byte[] imageToByteArray(System.Drawing.Image imageIn)
        {
            using (var ms = new MemoryStream())
            {
                imageIn.Save(ms, imageIn.RawFormat);
                return ms.ToArray();
            }
        }

        static void CheckClientConnection()
        {
            while (true)
            {
                TcpClient client = server.AcceptTcpClient();

                Console.WriteLine("A client connected.");

                NetworkStream stream = client.GetStream();

                while (!stream.DataAvailable) ;

                Byte[] bytes = new Byte[client.Available];

                stream.Read(bytes, 0, bytes.Length);
                String data = Encoding.UTF8.GetString(bytes);
                if (new Regex("^GET").IsMatch(data))
                {
                    Byte[] response = Encoding.UTF8.GetBytes("HTTP/1.1 101 Switching Protocols" + Environment.NewLine
                        + "Connection: Upgrade" + Environment.NewLine
                        + "Upgrade: websocket" + Environment.NewLine
                        + "Sec-WebSocket-Accept: " + Convert.ToBase64String(
                            SHA1.Create().ComputeHash(
                                Encoding.UTF8.GetBytes(
                                    new Regex("Sec-WebSocket-Key: (.*)").Match(data).Groups[1].Value.Trim() + "258EAFA5-E914-47DA-95CA-C5AB0DC85B11"
                                )
                            )
                        ) + Environment.NewLine
                        + Environment.NewLine);

                    stream.Write(response, 0, response.Length);

                    connectedClient.Add(client);
                }
            }
        }

        static void CheckClientMessage()
        {
            NetworkStream stream;
            List<TcpClient> copyConnectedClient;
            while(true)
            {
                copyConnectedClient = new List<TcpClient>(connectedClient);
                foreach (TcpClient client in copyConnectedClient)
                {
                    stream = client.GetStream();
                    if (!stream.DataAvailable) continue;

                    String data = getDecodedMessage(client,stream);

                    interpreteMessage(data,client);
                }
            }
        }

        static void sendMessageToClient(TcpClient client, Byte[] messageEncode)
        {
            NetworkStream stream = client.GetStream();


            Byte[] header;
            if (messageEncode.Length < 1 << 7)
            {
                header = new Byte[2];
                header[1] = (Byte)messageEncode.Length;
            }
            else if (messageEncode.Length < 1 << 16)
            {
                header = new Byte[4];
                header[1] = 127;
                byte[] intBytes = intToByteArray(messageEncode.Length);
                header[2] = intBytes[2];
                header[3] = intBytes[3];
            }else{
                header = new Byte[4];
                header[1] = 127;
                byte[] intBytes = intToByteArray(messageEncode.Length);
            }
            header[0] = 129;

            Byte[] key = new Byte[4];
            new Random().NextBytes(key);

            Byte[] messageSend = new Byte[header.Length + messageEncode.Length + key.Length ];
            //

            //
            for (int i = 0; i < header.Length; i++)
                messageSend[i] = header[i];
            for (int i = 0; i < key.Length; i++)
                messageSend[i + header.Length] = key[i];
            for (int i = 0; i < messageEncode.Length; i++)
                messageSend[i + key.Length + header.Length] = (Byte)(messageEncode[i] ^ key[i%4]);
            //messageSend[header.Length + messageEncode.Length] = 0;
            //
            int offset = 0;
            client.SendBufferSize = 8192;
            int size = messageSend.Length < client.SendBufferSize ? messageSend.Length : client.SendBufferSize;
            while (offset < messageSend.Length)
            {
                stream.Write(messageSend, offset, size);
                offset += size;
                size = offset + client.SendBufferSize < messageSend.Length ?  client.SendBufferSize : messageSend.Length - offset;
            }
        }

        static byte[] intToByteArray(int value) {
            byte[] intBytes = BitConverter.GetBytes(value);
            if (BitConverter.IsLittleEndian)
                Array.Reverse(intBytes);
            return intBytes;
        }

        static void sendMessageToClient(TcpClient client, string message)
        {
            NetworkStream stream = client.GetStream();
            Byte[] messageEncode = Encoding.UTF8.GetBytes(message);
            Byte[] messageSend = new Byte[2 + messageEncode.Length];
            messageSend[0] = 129;
            messageSend[1] = (Byte)messageEncode.Length;
            for (int i = 0; i < messageEncode.Length; i++)
                messageSend[i + 2] = messageEncode[i];
            stream.Write(messageSend, 0, messageSend.Length);
        }


        static string getDecodedMessage(TcpClient client, NetworkStream stream)
        {
            Byte[] message = new Byte[client.ReceiveBufferSize];
            stream.Read(message, 0, message.Length);
            //
            if (message.Length < 2)
                throw new Exception("Message trop court");
            if (message[0] != 129 && message[0] != 136)
                throw new Exception("Entete bizarre");
            //
            int lengthMessage = message[1] - 128;
            int startByte = 2;
            //
            if (lengthMessage == 126)
            {
                lengthMessage = 256 * message[2] + message[3];
                startByte = 4;
            }
            else if (lengthMessage == 127)
            {
                lengthMessage = 0;
                for (int i = 0; i < 8; i++)
                {
                    lengthMessage += (int)Math.Pow(2,(7-i)*8) * message[2 + i];
                }
                startByte = 10;
            }
            //
            Byte[] key = new Byte[4];
            Byte[] decoded = new Byte[lengthMessage];
            //
            for (int i = startByte; i < startByte + 4; i++)
            {
                key[i - startByte] = message[i];
            }
            //
            int lengthCount = 0;
            startByte += 4;
            int length = lengthMessage < client.ReceiveBufferSize ? lengthMessage : client.ReceiveBufferSize;
            Byte[] encoded = new Byte[length];

            int stopLength = length < 8192 ? startByte + length : length;
            for (int i = startByte; i < stopLength; i++)
            {
                encoded[i - startByte] = message[i];
            }
            do
            {
                int limite = lengthMessage;
                int index = 0;
                for (int i = lengthCount; i < length; i++)
                {
                    //if (encoded[index] == 0) break;

                    decoded[i] = (Byte)(encoded[index] ^ key[i % 4]);
                    index++;
                }

                lengthCount += encoded.Length < 8192 ? encoded.Length : encoded.Length - startByte;
                if (lengthCount < lengthMessage)
                {
                    message = new Byte[client.ReceiveBufferSize];
                    stream = client.GetStream();
                    stream.Read(message, 0, message.Length);
                    length = lengthCount + message.Length < lengthMessage ? lengthCount + message.Length : lengthMessage;
                    encoded = message;
                    startByte = 0;
                }

            } while (lengthCount < lengthMessage);

            return Encoding.UTF8.GetString(decoded);
        }

        static void interpreteMessage(string message,TcpClient client)
        {
            if (message == "�")
            {
                Console.WriteLine("disconnection");
                client.Close();
                connectedClient.Remove(client);
            }
            if(message.Contains("/w"))
            {

            }
            Console.WriteLine(message);
        }
    }
}

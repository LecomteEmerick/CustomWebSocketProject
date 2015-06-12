using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace TestCustomWebSocket
{
    class WebSocketServer
    {
        private TcpListener server;
        public List<TcpClient> connectedClient;

        

        public WebSocketServer(string ip, int port)
        {
            this.server = new TcpListener(IPAddress.Parse(ip), port);

            connectedClient = new List<TcpClient>();

            server.Start();
            Console.WriteLine("Server has started on {1}:{2}.{0}Waiting for a connection...", Environment.NewLine,ip,port.ToString());

            Task clientConnection = new Task(CheckClientConnection);
            clientConnection.Start();

            Task clientMessage = new Task(CheckClientMessage);
            clientMessage.Start();
        }


        #region publicFunction
        public void sendBlobToClient(TcpClient client, Byte[] messageEncode)
        {
            NetworkStream stream = client.GetStream();
            Byte[] header = createHeader(messageEncode.Length, Tools.TypeMessage.Blob);
            sendDataToClient(header, messageEncode, client);
        }

        public void sendMessageToClient(TcpClient client, string message)
        {

            Byte[] messageEncode = Encoding.UTF8.GetBytes(message);
            Byte[] header = createHeader(messageEncode.Length, Tools.TypeMessage.Text);
            sendDataToClient(header, messageEncode, client);
        }
        #endregion

        #region privateRegion
        #region multithreadedFunction
        private void CheckClientConnection()
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

        private void CheckClientMessage()
        {
            NetworkStream stream;
            List<TcpClient> copyConnectedClient;
            while (true)
            {
                copyConnectedClient = new List<TcpClient>(connectedClient);
                foreach (TcpClient client in copyConnectedClient)
                {
                    stream = client.GetStream();
                    if (!stream.DataAvailable) continue;

                    String data = getDecodedMessage(client, stream);

                    interpreteMessage(data, client);
                }
            }
        }
        #endregion

        private Byte[] createHeader(int messageLength, Tools.TypeMessage type)
        {
            Byte[] header;
            if (messageLength < 126)
            {
                header = new Byte[2];
                header[1] = (Byte)messageLength;
            }
            else if (messageLength < 65535)
            {
                header = new Byte[4];
                header[1] = (Byte)126;
                header[2] = (Byte)((messageLength >> 8) & 255);
                header[3] = (Byte)(messageLength & 255);
            }
            else
            {
                header = new Byte[10];
                header[1] = (Byte)127;
                header[2] = (Byte)((messageLength >> 56) & 255);
                header[3] = (Byte)((messageLength >> 48) & 255);
                header[4] = (Byte)((messageLength >> 40) & 255);
                header[5] = (Byte)((messageLength >> 32) & 255);
                header[6] = (Byte)((messageLength >> 24) & 255);
                header[7] = (Byte)((messageLength >> 16) & 255);
                header[8] = (Byte)((messageLength >> 8) & 255);
                header[9] = (Byte)(messageLength & 255);
            }
            if (type == Tools.TypeMessage.Blob)
                header[0] = 130;
            else
                header[0] = 129;
            return header;
        }

        private void sendDataToClient(Byte[] header, Byte[] messageEncode, TcpClient client)
        {
            int offset = 0;
            NetworkStream stream = client.GetStream();
            Byte[] messageSend = new Byte[header.Length + messageEncode.Length];
            //
            for (int i = 0; i < header.Length; i++)
                messageSend[i] = header[i];
            for (int i = 0; i < messageEncode.Length; i++)
                messageSend[i + header.Length] = messageEncode[i];
            //
            //client.SendBufferSize = 8192;
            int size = messageSend.Length < client.SendBufferSize ? messageSend.Length : client.SendBufferSize;
            while (offset < messageSend.Length)
            {
                stream.Write(messageSend, offset, size);
                offset += size;
                size = offset + client.SendBufferSize < messageSend.Length ? client.SendBufferSize : messageSend.Length - offset;
            }
        }

        private void disconnectClient(TcpClient client)
        {
            Console.WriteLine("disconnection");
            client.Close();
            connectedClient.Remove(client);
        }
        #endregion


        private void readOpCode(TcpClient client, Byte[] message)
        {
            Byte headerFrame = message[0];
            int opCode = (headerFrame & (1 << 0)) * 0 +
                (headerFrame & (1 << 1)) * 2 +
                (headerFrame & (1 << 2)) * 4 +
                (headerFrame & (1 << 3)) * 8;
            //
            if(opCode ==  0){
                //frameContinuation
            }else if(opCode == 1){
                //textframe
            }else if(opCode == 2){
                //blob
            }else if(opCode > 2 && opCode < 8){
                throw new Exception("Opcode " + opCode + " : Reserved for further non-control frames.");
            }else if(opCode == 8){
                this.disconnectClient(client);//connection close
            }else if(opCode == 9){
                throw new NotImplementedException();//ping
            }else if(opCode == 10){
                throw new NotImplementedException();//pong
            }else{
                throw new Exception("Opcode " + opCode + " : Reserved for further control frames.");
            }
        }

        private int getMessageLength(Byte[] message, out int nextIndex, out bool haveMask)
        {
            int lengthMessage = 0;
            //
            nextIndex = 0;
            haveMask = true;
            //
            nextIndex = 2;
            if(message[1] < 128)
            {
                lengthMessage = message[1];
                haveMask = false;
            }
            else
                lengthMessage = message[1] - 128;
            //
            if (lengthMessage == 126)
            {
                lengthMessage = 256 * message[2] + message[3];
                nextIndex = 4;
            }
            else if (lengthMessage == 127)
            {
                lengthMessage = 0;
                for (int i = 0; i < 8; i++)
                {
                    lengthMessage += (int)Math.Pow(2, (7 - i) * 8) * message[2 + i];
                }
                nextIndex = 10;
            }
            //
            return lengthMessage;
        }

        private Byte[] getKeyMask(Byte[] message, ref int startByte)
        {
            Byte[] key = new Byte[4];
            for (int i = startByte; i < startByte + 4; i++)
            {
                key[i - startByte] = message[i];
            }
            startByte += 4;
            return key;
        }

        private WebSocketMessage getWebSocketMessage(Byte[] message, Tools.TypeMessage Type, TcpClient client)
        {
            int lengthCount = 0;
            NetworkStream stream;
            int nextIndex=0;
            bool havemask = false;
            bool isFinished = (message[0] & (1 << 7)) != 0;
            Byte[] key;
            //
            int messageLength = getMessageLength(message, out nextIndex, out havemask);
            
            if (havemask)
                key = getKeyMask(message, ref nextIndex);
            //
            int length = messageLength < client.ReceiveBufferSize ? messageLength : client.ReceiveBufferSize;
            Byte[] encoded = new Byte[length];
            //
            int stopLength = length < client.ReceiveBufferSize ? nextIndex + length : length;
            for (int i = nextIndex; i < stopLength; i++)
            {
                encoded[i - nextIndex] = message[i];
            }

            Byte[] decoded = new Byte[messageLength];
            do
            {
                int index = 0;
                for (int i = lengthCount; i < length; i++)
                {
                    //if (encoded[index] == 0) break;

                    decoded[i] = (Byte)(encoded[index] ^ key[i % 4]);
                    index++;
                }

                lengthCount += encoded.Length < client.ReceiveBufferSize ? encoded.Length : encoded.Length - nextIndex;
                if (lengthCount < messageLength)
                {
                    message = new Byte[client.ReceiveBufferSize];
                    stream = client.GetStream();
                    stream.Read(message, 0, message.Length);
                    length = lengthCount + message.Length < messageLength ? lengthCount + message.Length : messageLength;
                    encoded = message;
                    nextIndex = 0;
                }

            } while (lengthCount < messageLength);

            return new WebSocketMessage(isFinished,new Byte[1],Type);
        }

        /*must be rework*/
        private string getDecodedMessage(TcpClient client, NetworkStream stream)
        {
            Byte[] message = new Byte[client.ReceiveBufferSize];
            stream.Read(message, 0, message.Length);
            //
            if (message.Length < 2)
                throw new Exception("Message trop court");
            //if (message[0] != 129 && message[0] != 136)
            //throw new Exception("Entete bizarre");
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
                    lengthMessage += (int)Math.Pow(2, (7 - i) * 8) * message[2 + i];
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

            if (connectedClient.Count > 1)
            {
                Tools.byteArrayToImage(decoded);

                //sendBlobToClient(connectedClient[1], decoded);
            }

            return Encoding.UTF8.GetString(decoded);
        }

        private void interpreteMessage(string message, TcpClient client)
        {
            if (message == "�")
            {

            }
            if (message.Contains("/w"))
            {

            }
        }
    }
}

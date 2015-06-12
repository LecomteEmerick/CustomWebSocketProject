using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestCustomWebSocket
{
    class WebSocketMessage
    {
        public readonly bool IsFinished;
        private Byte[] Message;
        private Tools.TypeMessage Type;

        public WebSocketMessage(bool isFinished, Byte[] message, Tools.TypeMessage type)
        {
            this.IsFinished = isFinished;
            this.Message = message;
            this.Type = type;
        }

        public string getTextMessage()
        {
            if (this.Type == Tools.TypeMessage.Text)
            {
                return Encoding.UTF8.GetString(this.Message);
            }
            return null;
        }

        public string getBlobMessage()
        {
            if (this.Type == Tools.TypeMessage.Text)
            {
                return Encoding.UTF8.GetString(this.Message);
            }
            return null;
        }
    }
}

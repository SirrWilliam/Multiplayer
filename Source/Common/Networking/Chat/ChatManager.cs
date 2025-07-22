using System.Collections.Generic;

namespace Multiplayer.Common.Networking.Chat
{
    public class ChatManager
    {
        private MultiplayerServer server;
        public List<ChatMessageData> messages;

        public const int MaxChatMsgLength = 128;

        public ChatManager(MultiplayerServer server)
        {
            this.server = server;
            this.messages = new List<ChatMessageData>();
        }

        public void SendChat(string msg)
        {
            SendChat(new ChatMessageData(ChatMessageType.Server, msg));
        }

        public void SendChat(string msg, ServerPlayer specific)
        {
            ChatMessageData messageData = new ChatMessageData(ChatMessageType.Server, msg);
            var writer = new ByteWriter();
            messageData.Serialize(writer);
            specific.conn.Send(Packets.Server_Chat, writer.ToArray());
        }

        public void SendChat(ChatMessageData msg)
        {
            ServerLog.Detail($"[Chat] [{msg.PlayerName}] {msg.Message}");
            var writer = new ByteWriter();
            msg.Serialize(writer);
            server.SendToPlaying(Packets.Server_Chat, writer.ToArray());
        }
    }
}

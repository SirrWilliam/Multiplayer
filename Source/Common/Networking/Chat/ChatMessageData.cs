using System;
using UnityEngine;

namespace Multiplayer.Common.Networking.Chat
{
    public class ChatMessageData
    {
        public ChatMessageType Type;
        public Color PlayerColor;
        public string PlayerName;
        public string Message;
        public DateTime CreatedTimeStamp;

        public ChatMessageData() {}

        public ChatMessageData(ChatMessageType type, string message)
        {
            Type = type;
            CreatedTimeStamp = DateTime.Now;
            switch (Type)
            {
                case ChatMessageType.Server:
                    PlayerName = "[SERVER]";
                    PlayerColor = Color.white;
                    Message = message;
                    break;
                case ChatMessageType.Local:
                    PlayerName = "[LOCAL]";
                    PlayerColor = Color.gray;
                    Message = message;
                    break;
                default:
                    PlayerName = "[ERROR]";
                    PlayerColor = Color.red;
                    Message = "Oops...";
                    break;
            }
        }

        public ChatMessageData(ChatMessageType type, string message, string playerName , Color? playerColor = null)
        {
            Type = type;
            Message = message;
            PlayerName = playerName ?? "Unkown";
            PlayerColor = playerColor ?? Color.white;
            CreatedTimeStamp = DateTime.Now;
        }

        public void Serialize(ByteWriter writer)
        {
            writer.WriteByte((byte)Type);
            writer.WriteDateTime(CreatedTimeStamp);
            switch (Type)
            {
                case ChatMessageType.Server:
                    writer.WriteString("[SERVER]");
                    writer.WriteColor(Color.white);
                    writer.WriteString(Message);
                    break;
                case ChatMessageType.Player:
                    writer.WriteString(PlayerName ?? "");
                    writer.WriteColor(PlayerColor);
                    writer.WriteString(Message);
                    break;
                case ChatMessageType.Local:
                    writer.WriteString("[LOCAL]");
                    writer.WriteColor(Color.gray);
                    writer.WriteString(Message);
                    break;
                default:
                    writer.WriteString("[ERROR]");
                    writer.WriteColor(Color.red);
                    writer.WriteString("Ooops...");
                    break;
            }
        }

        public static ChatMessageData Deserialize(ByteReader reader)
        {
            ChatMessageType type = (ChatMessageType)reader.ReadByte();
            DateTime createdTimeStamp = reader.ReadDateTime();
            string name = reader.ReadString();
            Color color = reader.ReadColor();
            string msg = reader.ReadString();

            return new ChatMessageData
            {
                Type = type,
                Message = msg,
                PlayerColor = color,
                CreatedTimeStamp = createdTimeStamp,
                PlayerName = name
            };
        }
    }
}

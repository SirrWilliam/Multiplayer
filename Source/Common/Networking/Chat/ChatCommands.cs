namespace Multiplayer.Common.Networking.Chat
{
    public abstract class ChatCmdHandler
    {
        public bool requiresHost;

        public MultiplayerServer Server => MultiplayerServer.instance!;

        public abstract void Handle(IChatSource source, string[] args);

        public void SendNoPermission(ServerPlayer player)
        {
            player.SendMessage("You don't have permission.");
        }

        public ServerPlayer? FindPlayer(string username)
        {
            return Server.GetPlayer(username);
        }
    }

    public class ChatCmdJoinPoint : ChatCmdHandler
    {
        public ChatCmdJoinPoint()
        {
            requiresHost = true;
        }

        public override void Handle(IChatSource source, string[] args)
        {
            if (!Server.worldData.TryStartJoinPointCreation(true))
                source.SendMessage("Join point creation already in progress.");
        }
    }

    public class ChatCmdKick : ChatCmdHandler
    {
        public ChatCmdKick()
        {
            requiresHost = true;
        }

        public override void Handle(IChatSource source, string[] args)
        {
            if (args.Length < 1)
            {
                source.SendMessage("No username provided.");
                return;
            }

            var toKick = FindPlayer(args[0]);
            if (toKick == null)
            {
                source.SendMessage("Couldn't find the player.");
                return;
            }

            if (toKick.IsHost)
            {
                source.SendMessage("You can't kick the host.");
                return;
            }

            toKick.Disconnect(MpDisconnectReason.Kick);
        }
    }

    public class ChatCmdStop : ChatCmdHandler
    {
        public ChatCmdStop()
        {
            requiresHost = true;
        }

        public override void Handle(IChatSource source, string[] args)
        {
            Server.running = false;
        }
    }
}

using ICSharpCode.SharpZipLib;
using Multiplayer.Common.Networking.Chat;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static Multiplayer.Client.MpReflection;

namespace Multiplayer.Common;

public class ServerLoadingState : AsyncConnectionState
{
    public ServerLoadingState(ConnectionBase connection) : base(connection)
    {
    }

    protected override async Task RunState()
    {
        await Server.worldData.WaitJoinPoint();
        await EndIfDead();

        SendWorldData();
        SendChatData();

        connection.ChangeState(ConnectionStateEnum.ServerPlaying);
        Player.SendPlayerList();
    }

    public void SendWorldData()
    {
        connection.Send(Packets.Server_WorldDataStart);

        ByteWriter writer = new ByteWriter();

        writer.WriteInt32(Player.FactionId);
        writer.WriteInt32(Server.gameTimer);
        writer.WriteInt32(Server.commands.SentCmds);
        writer.WriteBool(Server.freezeManager.Frozen);
        writer.WritePrefixedBytes(Server.worldData.savedGame);
        writer.WritePrefixedBytes(Server.worldData.sessionData);

        writer.WriteInt32(Server.worldData.mapCmds.Count);

        foreach (var kv in Server.worldData.mapCmds)
        {
            int mapId = kv.Key;

            //MultiplayerServer.instance.SendCommand(CommandType.CreateMapFactionData, ScheduledCommand.NoFaction, mapId, ByteWriter.GetBytes(factionId));

            List<byte[]> mapCmds = kv.Value;

            writer.WriteInt32(mapId);

            writer.WriteInt32(mapCmds.Count);
            foreach (var arr in mapCmds)
                writer.WritePrefixedBytes(arr);
        }

        writer.WriteInt32(Server.worldData.mapData.Count);

        foreach (var kv in Server.worldData.mapData)
        {
            int mapId = kv.Key;
            byte[] mapData = kv.Value;

            writer.WriteInt32(mapId);
            writer.WritePrefixedBytes(mapData);
        }

        writer.WriteInt32(Server.worldData.syncInfos.Count);
        foreach (var syncInfo in Server.worldData.syncInfos)
            writer.WritePrefixedBytes(syncInfo);

        byte[] packetData = writer.ToArray();
        connection.SendFragmented(Packets.Server_WorldData, packetData);

        ServerLog.Log("World response sent: " + packetData.Length);

    }

    public void SendChatData()
    {
        ByteWriter writer = new ByteWriter();

        var orderedMessages = Server.chatManager.messages.OrderBy(m => m.CreatedTimeStamp).ToList();
        writer.WriteInt32(orderedMessages.Count);  

        foreach (var message in orderedMessages)
        {
            message.Serialize(writer);
        }
        connection.Send(Packets.Server_ChatHistory, writer.ToArray());       
    }
}

using LiteNetLib;
using Multiplayer.Client.Networking;
using Multiplayer.Common;
using Multiplayer.Common.Networking.Chat;
using Steamworks;
using System;
using System.Linq;
using UnityEngine;
using Verse;
using static Mono.Security.X509.X520;

namespace Multiplayer.Client
{
    public static class ClientUtil
    {
        public static void TryConnectWithWindow(string address, int port, bool returnToServerBrowser = true)
        {
            Find.WindowStack.Add(new ConnectingWindow(address, port) { returnToServerBrowser = returnToServerBrowser });

            Multiplayer.session = new MultiplayerSession
            {
                address = address,
                port = port
            };

            NetManager netClient = new NetManager(new MpClientNetListener())
            {
                EnableStatistics = true,
                IPv6Enabled = MpUtil.SupportsIPv6() ? IPv6Mode.SeparateSocket : IPv6Mode.Disabled
            };

            netClient.Start();
            netClient.ReconnectDelay = 300;
            netClient.MaxConnectAttempts = 8;

            Multiplayer.session.netClient = netClient;
            netClient.Connect(address, port, "");
        }

        public static void TrySteamConnectWithWindow(CSteamID user, bool returnToServerBrowser = true)
        {
            Log.Message("Connecting through Steam");

            Multiplayer.session = new MultiplayerSession
            {
                client = new SteamClientConn(user) { username = Multiplayer.username },
                steamHost = user
            };

            Find.WindowStack.Add(new SteamConnectingWindow(user) { returnToServerBrowser = returnToServerBrowser });

            Multiplayer.session.ReapplyPrefs();
            Multiplayer.Client.ChangeState(ConnectionStateEnum.ClientSteam);
        }

        public static void HandleReceive(ByteReader data, bool reliable)
        {
            try
            {
                Multiplayer.Client.HandleReceiveRaw(data, reliable);
            }
            catch (Exception e)
            {
                Log.Error($"Exception handling packet by {Multiplayer.Client}: {e}");

                Multiplayer.session.disconnectInfo.titleTranslated = "MpPacketErrorLocal".Translate();

                ConnectionStatusListeners.TryNotifyAll_Disconnected();
                Multiplayer.StopMultiplayer();
            }
        }

        public static void SendMessage(string message)
        {
            PlayerInfo player = Multiplayer.session.players.Find(p => p.Id == Multiplayer.session.playerId);
            string playerName = player?.Username ?? "Unknown";
            var faction = Find.FactionManager.AllFactions.FirstOrDefault(f => f.loadID == player?.factionId);
            bool isSpectator = faction == Multiplayer.WorldComp.spectatorFaction;
            Color playerColor = isSpectator ? Color.white : (faction?.Color ?? Color.white);

            SendMessage(new ChatMessageData(ChatMessageType.Player, message, playerName,playerColor ));
        }

        public static void SendMessage(ChatMessageData msg)
        {
            var writer = new ByteWriter();
            msg.Serialize(writer);
            Multiplayer.Client.Send(Packets.Client_Chat, writer.ToArray());
        }
    }

}

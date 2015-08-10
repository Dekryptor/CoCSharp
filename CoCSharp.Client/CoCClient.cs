﻿using CoCSharp.Client.Events;
using CoCSharp.Client.Handlers;
using CoCSharp.Logging;
using CoCSharp.Networking;
using CoCSharp.Networking.Packets;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace CoCSharp.Client
{
    public class CoCClient
    {
        public delegate void PacketHandler(CoCClient client, IPacket packet);

        public CoCClient()
        {
            Seed = new Random().Next();
            UserID = 0;
            UserToken = null;
            Connection = new Socket(SocketType.Stream, ProtocolType.Tcp);
            PacketLogger = new PacketLogger()
            {
                LogConsole = false
            };
            PacketHandlers = new Dictionary<ushort, PacketHandler>();
            NextKeepAlive = DateTime.Now;
            KeepAliveManager = new KeepAliveManager(this);

            LoginPacketHandlers.RegisterLoginPacketHandlers(this);
            InGamePacketHandlers.RegisterInGamePacketHandler(this);
        }

        public Socket Connection { get; set; }
        public bool Connected
        {
            get { return Connection.Connected; }
        }
        public long UserID { get; private set; }
        public string UserToken { get; private set; }
        public int Seed { get; private set; }
        public PacketLogger PacketLogger { get; set; }
        public NetworkManager NetworkManager { get; set; }

        private KeepAliveManager KeepAliveManager { get; set; }
        private DateTime NextKeepAlive { get; set; }
        private Dictionary<ushort, PacketHandler> PacketHandlers { get; set; }

        public void Connect(IPEndPoint endPoint)
        {
            if (endPoint == null)
                throw new ArgumentNullException("endPoint");

            var args = new SocketAsyncEventArgs();
            args.Completed += ConnectAsyncCompleted;
            args.RemoteEndPoint = endPoint;
            Connection.ConnectAsync(args);
        }

        private void ConnectAsyncCompleted(object sender, SocketAsyncEventArgs e)
        {
            if (e.SocketError != SocketError.Success)
                throw new SocketException((int)e.SocketError);
            NetworkManager = new NetworkManager(e.ConnectSocket, HandleReceivedPacket);
            QueuePacket(new LoginRequestPacket()
            {
                UserID = this.UserID,
                UserToken = this.UserToken,
                ClientMajorVersion = 7,
                ClientContentVersion = 0,
                ClientMinorVersion = 156,
                FingerprintHash = "ae9b056807ac8bfa58a3e879b1f1601ff17d1df5",
                OpenUDID = "563a6f060d8624db",
                MacAddress = null,
                DeviceModel = "GT-I9300",
                LocaleKey = 2000000,
                Language = "en",
                AdvertisingGUID = "",
                OsVersion = "4.0.4",
                IsAdvertisingTrackingEnabled = false,
                AndroidDeviceID = "563a6f060d8624db",
                FacebookDistributionID = "",
                VendorGUID = "",
                Seed = NetworkManager.Seed
            });
            KeepAliveManager.Start();
        }

        public void SendChatMessage(string message)
        {
            QueuePacket(new ChatMessageClientPacket()
            {
                Message = message
            });
        }

        public void QueuePacket(IPacket packet)
        {
            if (packet == null)
                throw new ArgumentNullException("packet");
            if (NetworkManager == null)
                throw new InvalidOperationException("Tried to send a packet before NetworkManager was initialized.");

            PacketLogger.LogPacket(packet, PacketDirection.Server);
            NetworkManager.WritePacket(packet);
        }

        public void RegisterPacketHandler(IPacket packet, PacketHandler handler)
        {
            if (packet == null)
                throw new ArgumentNullException("packet");
            if (handler == null)
                throw new ArgumentNullException("handler");

            PacketHandlers.Add(packet.ID, handler);
        }

        private void HandleReceivedPacket(SocketAsyncEventArgs args, IPacket packet)
        {
            PacketLogger.LogPacket(packet, PacketDirection.Client);
            var handler = (PacketHandler)null;
            if (!PacketHandlers.TryGetValue(packet.ID, out handler))
                return;
            handler(this, packet);
        }

        public event EventHandler<ChatMessageEventArgs> ChatMessage;
        protected internal virtual void OnChatMessage(ChatMessageEventArgs e)
        {
            if (ChatMessage != null)
                ChatMessage(this, e);
        }
    }
}

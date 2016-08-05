﻿using CoCSharp.Data;
using CoCSharp.Data.Models;
using CoCSharp.Data.Slots;
using CoCSharp.Network;
using CoCSharp.Network.Messages;
using CoCSharp.Network.Messages.Commands;
using CoCSharp.Server.Core;
using System;

namespace CoCSharp.Server.Handlers
{
    public static class InGameMessageHandlers
    {
        private static KeepAliveResponseMessage s_keepAliveResponse = new KeepAliveResponseMessage();

        private static void HandleKeepAliveRequestMessage(CoCServer server, AvatarClient client, Message message)
        {
            client.UpdateKeepAlive();
            client.NetworkManager.SendMessage(s_keepAliveResponse);
        }

        private static void HandleAttackNpcMessage(CoCServer server, AvatarClient client, Message message)
        {
            var anMessage = message as AttackNpcMessage;

            var npcVillage = server.NpcManager.LoadNpc(anMessage.NpcID);
            if (npcVillage == null)
            {
                client.NetworkManager.SendMessage(client.OwnHomeDataMessage);
                return;
            }

            var avatar = new AvatarMessageComponent(client);

            avatar.Units.Clear();
            for (int i = 0; i < 18; i++)
                avatar.Units.Add(new UnitSlot(4000000 + i, 999));

            var ndMessage = new NpcDataMessage();
            ndMessage.AvatarData = avatar;
            ndMessage.NpcVillage = npcVillage;
            ndMessage.NpcID = anMessage.NpcID;

            FancyConsole.WriteLine(LogFormats.Attack_Npc, client.Token, anMessage.NpcID);

            client.NetworkManager.SendMessage(ndMessage);
        }

        private static void HandleAttackResultMessage(CoCServer server, AvatarClient client, Message message)
        {
            var avatar = client;
            var ohdMessage = client.OwnHomeDataMessage;

            FancyConsole.WriteLine(LogFormats.Attack_ReturnHome, client.Token);

            client.NetworkManager.SendMessage(ohdMessage);
        }

        private static void HandleAvatarProfileRequestMessage(CoCServer server, AvatarClient client, Message message)
        {
            var apreqMessage = (AvatarProfileRequestMessage)message;

            var avatar = apreqMessage.UserID == client.ID ? client : new AvatarClient(server, apreqMessage.UserID);
            avatar.Load();

            var aprMessage = new AvatarProfileResponseMessage();
            aprMessage.Village = avatar.Home;
            aprMessage.AvatarData = new AvatarMessageComponent(avatar);

            //aprMessage.AvatarData.Unknown13 = 2; // League
            //aprMessage.AvatarData.Unknown14 = 13;
            //aprMessage.AvatarData.Unknown27 = 13;

            //aprMessage.AvatarData.AchievementProgress = new AchievementProgessSlot[]
            //{
            //    //new AchievementProgessSlot(23000021, 306),
            //    //new AchievementProgessSlot(23000022, 306),
            //    new AchievementProgessSlot(23000023, 306), // 23000021 to 23000023 -> all time best trophies.

            //    //new AchievementProgessSlot(23000060, 306), 
            //    //new AchievementProgessSlot(23000061, 306),
            //    new AchievementProgessSlot(23000062, 306) // 23000060 to 23000062 -> War Stars count.
            //};

            FancyConsole.WriteLine(LogFormats.Avatar_ProfileReq, client.Token);

            client.NetworkManager.SendMessage(aprMessage);
        }

        private static void HandleVisitHomeMessage(CoCServer server, AvatarClient client, Message message)
        {
            var vhMessage = (VisitHomeMessage)message;

            var avatar = new AvatarClient(server, vhMessage.HomeID);
            avatar.Load();

            var vhdMessage = new VisitHomeDataMessage()
            {
                LastVisit = TimeSpan.FromSeconds(0),

                VisitVillageData = new VillageMessageComponent(avatar),
                VisitAvatarData = new AvatarMessageComponent(avatar),

                Unknown1 = 1,

                OwnAvatarData = new AvatarMessageComponent(client),
            };

            client.NetworkManager.SendMessage(vhdMessage);
        }

        private static void HandleCommandMessage(CoCServer server, AvatarClient client, Message message)
        {
            var cmdMessage = message as CommandMessage;
            if (cmdMessage.Commands.Length > 0)
            {
                for (int i = 0; i < cmdMessage.Commands.Length; i++)
                {
                    var cmd = cmdMessage.Commands[i];

                    if (cmd == null)
                        break;

                    server.HandleCommand(client, cmd);
                }

                client.Save();
            }
        }

        private static void HandleChangeAvatarNameRequestMessage(CoCServer server, AvatarClient client, Message message)
        {
            var careqMessage = (ChangeAvatarNameRequestMessage)message;
            client.Name = careqMessage.NewName;
            client.IsNamed = true;

            var count = client.TutorialProgess.Count;
            for (int i = count; i < count + 3; i++)
                client.TutorialProgess.Add(new TutorialProgressSlot(21000000 + i));

            var ascMessage = new AvailableServerCommandMessage();
            var canCommand = new ChangeAvatarNameCommand();
            canCommand.NewName = careqMessage.NewName;
            canCommand.Unknown1 = 1;
            canCommand.Unknown2 = -1;
            ascMessage.Command = canCommand;
            client.NetworkManager.SendMessage(ascMessage);

            client.Save();
        }

        private static void HandleChatMessageClientMessageMessage(CoCServer server, AvatarClient client, Message message)
        {
            var cmcMessage = message as ChatMessageClientMessage;
            var cmsMessage = new ChatMessageServerMessage();
            if (cmcMessage.Message[0] == '/')
            {
                var cmd = cmcMessage.Message.Substring(1);
                cmsMessage.Name = "Server";
                switch (cmd)
                {
                    case "help":
                        cmsMessage.Message = "Crappy Command Implementation: Available commands -> /help, /addgems, /clearobstacles, /max";
                        client.NetworkManager.SendMessage(cmsMessage);
                        break;

                    case "addgems":
                        client.Gems += 500;
                        client.FreeGems += 500;

                        cmsMessage.Message = "Added 500 gems.";
                        client.NetworkManager.SendMessage(cmsMessage);

                        client.NetworkManager.SendMessage(client.OwnHomeDataMessage);
                        break;

                    case "clearobstacles":
                        var count = client.Home.Obstacles.Count;
                        client.Home.Obstacles.Clear();

                        client.NetworkManager.SendMessage(client.OwnHomeDataMessage);

                        cmsMessage.Message = "Cleared " + count + " obstacles.";
                        client.NetworkManager.SendMessage(cmsMessage);
                        break;

                    case "max":
                        var countBuilding = client.Home.Buildings.Count;
                        for (int i = 0; i < countBuilding; i++)
                        {
                            var building = client.Home.Buildings[i];
                            var collection = AssetManager.DefaultInstance.SearchCsv<BuildingData>(building.Data.ID);
                            var data = collection[collection.Count - 1];
                            if (building.IsConstructing)
                                building.CancelConstruction();
                            if (building.IsLocked)
                                building.IsLocked = false;

                            building.Data = data;
                        }

                        var countTraps = client.Home.Traps.Count;
                        for (int i = 0; i < countTraps; i++)
                        {
                            var trap = client.Home.Traps[i];
                            var collection = AssetManager.DefaultInstance.SearchCsv<TrapData>(trap.Data.ID);
                            var data = collection[collection.Count - 1];
                            if (trap.IsConstructing)
                                trap.CancelConstruction();

                            trap.Data = data;
                        }

                        cmsMessage.Message = "Maxed " + countBuilding + " buildings and " + countTraps + " traps.";
                        client.NetworkManager.SendMessage(cmsMessage);

                        client.NetworkManager.SendMessage(client.OwnHomeDataMessage);
                        break;

                    case "reload":
                        client.NetworkManager.SendMessage(client.OwnHomeDataMessage);
                        break;

#if DEBUG
                    // Add this feature only in the DEBUG build
                    case "populatedb":
                        for (int i = 0; i < 50; i++)
                            server.AvatarManager.CreateNewAvatar();

                        cmsMessage.Message = "Created 50 new avatar.";
                        client.NetworkManager.SendMessage(cmsMessage);
                        break;
#endif

                    default:
                        cmsMessage.Message = "Unknown command.";
                        client.NetworkManager.SendMessage(cmsMessage);
                        goto case "help";
                }
            }
            else
            {
                //TODO: Set alliance and all that jazz.

                cmsMessage.Level = client.Level;
                cmsMessage.CurrentUserID = client.ID;
                cmsMessage.UserID = client.ID; // <- might have an issue here.
                cmsMessage.Name = client.Name;
                cmsMessage.Message = cmcMessage.Message;
                cmsMessage.Clan = client.Alliance;


                server.SendMessageAll(cmsMessage);
            }
        }

        public static void RegisterInGameMessageHandlers(CoCServer server)
        {
            server.RegisterMessageHandler(new CommandMessage(), HandleCommandMessage);
            server.RegisterMessageHandler(new KeepAliveRequestMessage(), HandleKeepAliveRequestMessage);
            server.RegisterMessageHandler(new AttackNpcMessage(), HandleAttackNpcMessage);
            server.RegisterMessageHandler(new AttackResultMessage(), HandleAttackResultMessage);
            server.RegisterMessageHandler(new ChatMessageClientMessage(), HandleChatMessageClientMessageMessage);
            server.RegisterMessageHandler(new AvatarProfileRequestMessage(), HandleAvatarProfileRequestMessage);
            server.RegisterMessageHandler(new ChangeAvatarNameRequestMessage(), HandleChangeAvatarNameRequestMessage);
            server.RegisterMessageHandler(new VisitHomeMessage(), HandleVisitHomeMessage);
        }
    }
}

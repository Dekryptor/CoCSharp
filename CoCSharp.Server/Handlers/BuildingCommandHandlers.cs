﻿using System;
using CoCSharp.Networking;
using CoCSharp.Networking.Messages.Commands;
using CoCSharp.Logic;

namespace CoCSharp.Server.Handlers
{
    public delegate void CommandHandler(CoCServer server, CoCRemoteClient client, Command command);

    public static class BuildingCommandHandlers
    {
        private static void HandleBuyBuildingCommand(CoCServer server, CoCRemoteClient client, Command command)
        {
            var bbCmd = command as BuyBuildingCommand;
            var dataIndex = bbCmd.BuildingDataIndex;
            var dataID = Building.IndexToDataID(dataIndex);
            Console.WriteLine("Buying new building {0} at {1}, {2}", dataID, bbCmd.X, bbCmd.Y);

            var building = new Building(dataID);
            building.Data = server.DataManager.FindBuilding(dataIndex, 0);

            building.X = bbCmd.X;
            building.Y = bbCmd.Y;
            building.BeginConstruct();

            client.Avatar.Home.Buildings.Add(building);
        }

        private static void HandleUpgradeBuildingCommand(CoCServer server, CoCRemoteClient client, Command command)
        {

        }

        public static void RegisterBuildingCommandHandlers(CoCServer server)
        {
            server.RegisterCommandHandler(new BuyBuildingCommand(), HandleBuyBuildingCommand);
            server.RegisterCommandHandler(new UpgradeBuildingCommand(), HandleUpgradeBuildingCommand);
        }
    }
}

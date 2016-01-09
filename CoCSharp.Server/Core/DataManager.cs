﻿using CoCSharp.Csv;
using CoCSharp.Data;
using System.Linq;

namespace CoCSharp.Server.Core
{
    public class DataManager
    {
        public DataManager()
        {
            var buildingTable = new CsvTable("Content\\buildings.csv");
            BuildingsData = CsvSerializer.Deserialize<BuildingData>(buildingTable);
        }

        public BuildingData[] BuildingsData { get; set; }

        public BuildingData[] FindBuilding(int id)
        {
            return BuildingsData.Where(bd => bd.DataID == id).ToArray();
        }

        public BuildingData FindBuilding(int id, int level)
        {
            return FindBuilding(id)[level];
        }
    }
}

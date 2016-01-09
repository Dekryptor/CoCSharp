﻿using CoCSharp.Data;
using Newtonsoft.Json;
using System;

namespace CoCSharp.Logic
{
    /// <summary>
    /// Represents a Clash of Clans building.
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)]
    public class Building : VillageObject
    {
        /// <summary>
        /// Represents the base game ID of an <see cref="Obstacle"/>. This field
        /// is constant.
        /// </summary>
        public const int BaseGameID = 500000000;

        /// <summary>
        /// Represents the base data ID of an <see cref="Obstacle"/>. This field
        /// is constant.
        /// </summary>
        public const int BaseDataID = 1000000;

        /// <summary>
        /// Initializes a new instance of the <see cref="Building"/> class.
        /// </summary>
        public Building() : base()
        {
            Level = -1;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Building"/> class
        /// with the specified data ID.
        /// </summary>
        /// <param name="dataID">Data ID of the <see cref="Building"/>.</param>
        public Building(int dataID) : base(dataID)
        {
            Level = -1;
        }

        /// <summary>
        /// Gets or sets the level of the <see cref="Building"/>.
        /// </summary>
        [JsonProperty("lvl")]
        public int Level { get; set; }

        /// <summary>
        /// Gets whether the <see cref="Building"/> is constructed.
        /// </summary>
        public bool IsConstructed { get { return Level > -1; } } //TODO: I dont like this.

        /// <summary>
        /// Gets whether the <see cref="Building"/> is in construction.
        /// That is upgrading and constructing.
        /// </summary>
        public bool IsConstructing { get { return ConstructionTime.TotalSeconds > 0; } }

        /// <summary>
        /// Gets or sets whether the <see cref="Building"/> is locked.
        /// This is for mostly for the alliance castle building.
        /// </summary>
        [JsonProperty("locked", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public bool Locked { get; set; }

        /// <summary>
        /// Gets or sets the amount of time needed for the construction to finish.
        /// </summary>
        public TimeSpan ConstructionTime
        {
            get
            {
                return TimeSpan.FromSeconds(ConstructionTimeEndSecounds - DateTimeConverter.UtcNow);
            }
            set
            {
                ConstructionTimeEndSecounds = DateTimeConverter.UtcNow + (int)value.TotalSeconds;
            }
        }
        [JsonProperty("const_t", DefaultValueHandling = DefaultValueHandling.Ignore)]
        private int ConstructionTimeSecounds 
        {
            get
            {
                var endTime = ConstructionTimeEndSecounds - DateTimeConverter.UtcNow;
                if (endTime <= 0)
                {
                    ConstructionTimeEndSecounds = 0;
                    return 0;
                }

                return endTime;
            } 
        } 

        /// <summary>
        /// Gets or sets the date that the construction will finish.
        /// </summary>
        public DateTime ConstructionTimeEnd
        {
            get
            {
                return DateTimeConverter.FromUnixTimestamp(ConstructionTimeEndSecounds);
            }
            set
            {
                ConstructionTimeEndSecounds = (int)DateTimeConverter.ToUnixTimestamp(value);
            }
        }
        [JsonProperty("const_t_end", DefaultValueHandling = DefaultValueHandling.Ignore)]
        private int ConstructionTimeEndSecounds { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="BuildingData"/> of the <see cref="Building"/>.
        /// </summary>
        public BuildingData Data { get; set; } //TODO: Add this to VillageObject.

        /// <summary>
        /// Begins the construction of the <see cref="Building"/>.
        /// </summary>
        /// <exception cref="InvalidOperationException"><see cref="Data"/> is null.</exception>
        public void BeginConstruct()
        {
            if (Data == null)
                throw new InvalidOperationException("Building.Data cannot be null.");

            if (!IsConstructed)
            {
                ConstructionTimeEnd = DateTime.UtcNow.Add(Data.BuildTime);
            }
        }

        /// <summary>
        /// Ends the construction of the <see cref="Building"/>.
        /// </summary>
        public void EndConstruct()
        {
            //TODO: Implement.
            throw new NotImplementedException();
        }

        #region ID Handling
        //TODO: Find a better way of converting DataIDs and GameIDs.

        /// <summary>
        /// Determines if the specified game ID is valid for an
        /// <see cref="Obstacle"/>.
        /// </summary>
        /// <param name="id">Game ID to validate.</param>
        /// <returns>Returns <c>true</c> if the game ID specified is valid.</returns>
        public static bool ValidGameID(int id)
        {
            return !(id < BaseGameID || id > 501000000); // 504000000 is Character BaseDataID
        }

        /// <summary>
        /// Converts the specified game ID into an index to the <see cref="Building"/>
        /// in a <see cref="Village.Buildings"/>.
        /// </summary>
        /// <param name="id">Game ID to convert.</param>
        /// <returns>Returns the index of the <see cref="ObsoleteAttribute"/> in <see cref="Village.Obstacles"/>.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="id"/> is no the range of 500000000 &lt; id &lt; 501000000.</exception>
        public static int GameIDToIndex(int id)
        {
            if (!ValidGameID(id))
                throw new ArgumentOutOfRangeException("Game ID must be in the range of " + BaseGameID + " < id < 501000000");

            return id - BaseGameID;
        }

        /// <summary>
        /// Converts the specified index to a game ID.
        /// </summary>
        /// <param name="index">Index to convert.</param>
        /// <returns>Returns the game ID converted from the specified index.</returns>
        public static int IndexToGameID(int index)
        {
            return BaseGameID + index;
        }

        /// <summary>
        /// Determines if the specified data ID is valid for an
        /// <see cref="Obstacle"/>.
        /// </summary>
        /// <param name="id">Data ID to validate.</param>
        /// <returns>Returns <c>true</c> if the game ID specified is valid.</returns>
        public static bool ValidDataID(int id)
        {
            return !(id < BaseDataID || id > 2000000); // Locales
        }

        /// <summary>
        /// Converts the specified data ID into an index to the <see cref="BuildingData"/>
        /// in a deserialized <see cref="BuildingData"/> array.
        /// </summary>
        /// <param name="id">Data ID to convert.</param>
        /// <returns>Reurns the index of the <see cref="Obstacle"/> in a deserialized <see cref="BuildingData"/> array.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="id"/> is no the range of 1000000 &lt; id &lt; 2000000.</exception>
        public static int DataIDToIndex(int id)
        {
            if (!ValidDataID(id))
                throw new ArgumentOutOfRangeException("Game ID must be in the range of " + BaseDataID + " < id < 2000000");

            return id - BaseDataID;
        }

        /// <summary>
        /// Converts the specified index to a data ID.
        /// </summary>
        /// <param name="index">Index to convert.</param>
        /// <returns>Returns the data ID converted from the specified index.</returns>
        public static int IndexToDataID(int index)
        {
            return BaseDataID + index;
        }
        #endregion
    }
}

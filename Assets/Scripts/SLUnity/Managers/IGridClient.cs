using LibreMetaverse;
using OpenMetaverse;

namespace SLUnity.Managers
{
    public interface IGridClient
    {
        /// <summary>Networking subsystem</summary>
        public NetworkManager Network { get; }
        /// <summary>Settings class including constant values and changeable
        /// parameters for everything</summary>
        public Settings Settings { get; }
        /// <summary>Parcel (subdivided simulator lots) subsystem</summary>
        public ParcelManager Parcels { get; }
        /// <summary>Our own avatars subsystem</summary>
        public AgentManager Self { get; }
        /// <summary>Other avatars subsystem</summary>
        public AvatarManager Avatars { get; }
        /// <summary>Estate subsystem</summary>
        public EstateTools Estate { get; }
        /// <summary>Friends list subsystem</summary>
        public FriendsManager Friends { get; }
        /// <summary>Grid (aka simulator group) subsystem</summary>
        public GridManager Grid { get; }
        /// <summary>Object subsystem</summary>
        public ObjectManager Objects { get; }
        /// <summary>Group subsystem</summary>
        public GroupManager Groups { get; }
        /// <summary>Asset subsystem</summary>
        public AssetManager Assets { get; }
        /// <summary>Inventory AIS client</summary>
        public InventoryAISClient AisClient { get; }
        /// <summary>Appearance subsystem</summary>
        public AppearanceManager Appearance { get; }
        /// <summary>Inventory subsystem</summary>
        public InventoryManager Inventory { get; }
        /// <summary>Directory searches including classifieds, people, land sales, etc</summary>
        public DirectoryManager Directory { get; }
        /// <summary>Handles land, wind, and cloud heightmaps</summary>
        public TerrainManager Terrain { get; }
        /// <summary>Handles sound-related networking</summary>
        public SoundManager Sound { get; }
        /// <summary>Throttling total bandwidth usage, or allocating bandwidth
        /// for specific data stream types</summary>
        public AgentThrottle Throttle { get; }

        public OpenMetaverse.Stats.UtilizationStatistics Stats { get; }
    }
}
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using LibreMetaverse;
using LibreMetaverse.Voice;
using OpenMetaverse;
using OpenMetaverse.Packets;
using OpenMetaverse.Stats;
using UnityEngine;
using Type = System.Type;


public class UnityGridClient : GridClient, IGridClient
{
    public UUID GroupID = UUID.Zero;
    public Dictionary<UUID, GroupMember> GroupMembers;
    public Dictionary<UUID, AvatarAppearancePacket> Appearances = new Dictionary<UUID, AvatarAppearancePacket>();
    public Dictionary<string, Command> Commands = new Dictionary<string, Command>();
    // public bool Running = true;
    public bool GroupCommands = false;
    public string MasterName = string.Empty;
    public UUID MasterKey = UUID.Zero;
    // public bool AllowObjectMaster = false;
    // public ClientManager ClientManager;
    // public VoiceManager VoiceManager;
    // // Shell-like inventory commands need to be aware of the 'current' inventory folder.
    // public InventoryFolder CurrentDirectory = null;

    private System.Timers.Timer updateTimer;
    private UUID GroupMembersRequestID;
    public Dictionary<UUID, Group> GroupsCache = null;
    private readonly ManualResetEvent GroupsEvent = new ManualResetEvent(false);

    /// <summary>
    /// 
    /// </summary>
    public UnityGridClient()
    {
        // ClientManager = manager;

        updateTimer = new System.Timers.Timer(500);
        updateTimer.Elapsed += updateTimer_Elapsed;

        RegisterAllCommands(Assembly.GetExecutingAssembly());

        Settings.LOG_LEVEL = Helpers.LogLevel.Debug;
        Settings.LOG_RESENDS = false;
        Settings.STORE_LAND_PATCHES = true;
        Settings.ALWAYS_DECODE_OBJECTS = true;
        Settings.ALWAYS_REQUEST_OBJECTS = true;
        Settings.SEND_AGENT_UPDATES = true;
        Settings.USE_ASSET_CACHE = true;

        Network.RegisterCallback(PacketType.AgentDataUpdate, AgentDataUpdateHandler);
        // Network.LoginProgress += LoginHandler;
        // Objects.AvatarUpdate += Objects_AvatarUpdate;
        // Objects.TerseObjectUpdate += Objects_TerseObjectUpdate;
        Network.SimChanged += Network_SimChanged;
        Self.IM += Self_IM;
        Groups.GroupMembersReply += GroupMembersHandler;
        Inventory.InventoryObjectOffered += Inventory_OnInventoryObjectReceived;            

        Network.RegisterCallback(PacketType.AvatarAppearance, AvatarAppearanceHandler);
        Network.RegisterCallback(PacketType.AlertMessage, AlertMessageHandler);

        // VoiceManager = new VoiceManager(this);

        updateTimer.Start();
    }

    // void Objects_TerseObjectUpdate(object sender, TerseObjectUpdateEventArgs e)
    // {
    //     if (e.Prim.LocalID == Self.LocalID)
    //     {
    //         SetDefaultCamera();
    //     }
    // }
    //
    // void Objects_AvatarUpdate(object sender, AvatarUpdateEventArgs e)
    // {
    //     if (e.Avatar.LocalID == Self.LocalID)
    //     {
    //         SetDefaultCamera();
    //     }
    // }

    void Network_SimChanged(object sender, SimChangedEventArgs e)
    {
        Self.Movement.SetFOVVerticalAngle(Utils.TWO_PI - 0.05f);
    }

    // public void SetDefaultCamera()
    // {
    //     throw new NotImplementedException();
    //     // // SetCamera 5m behind the avatar
    //     // Self.Movement.Camera.LookAt(
    //     //     Self.SimPosition + new Vector3(-5, 0, 0) * Self.Movement.BodyRotation,
    //     //     Self.SimPosition
    //     // );
    // }


    void Self_IM(object sender, InstantMessageEventArgs e)
    {
        bool groupIM = e.IM.GroupIM && GroupMembers != null && GroupMembers.ContainsKey(e.IM.FromAgentID);

        if (e.IM.FromAgentID == MasterKey || (GroupCommands && groupIM))
        {
            // Received an IM from someone that is authenticated
            Console.WriteLine("<{0} ({1})> {2}: {3} (@{4}:{5})", e.IM.GroupIM ? "GroupIM" : "IM", e.IM.Dialog, e.IM.FromAgentName, e.IM.Message, 
                e.IM.RegionID, e.IM.Position);

            if (e.IM.Dialog == InstantMessageDialog.RequestTeleport)
            {
                Console.WriteLine("Accepting teleport lure.");
                Self.TeleportLureRespond(e.IM.FromAgentID, e.IM.IMSessionID, true);
            }
            // else if (
            //     e.IM.Dialog == InstantMessageDialog.MessageFromAgent ||
            //     e.IM.Dialog == InstantMessageDialog.MessageFromObject)
            // {
            //     ClientManager.Instance.DoCommandAll(e.IM.Message, e.IM.FromAgentID);
            // }
        }
        else
        {
            // Received an IM from someone that is not the bot's master, ignore
            Console.WriteLine("<{0} ({1})> {2} (not master): {3} (@{4}:{5})", e.IM.GroupIM ? "GroupIM" : "IM", e.IM.Dialog, e.IM.FromAgentName, e.IM.Message,
                e.IM.RegionID, e.IM.Position);
            return;
        }
    }

    // /// <summary>
    // /// Initialize everything that needs to be initialized once we're logged in.
    // /// </summary>
    // /// <param name="login">The status of the login</param>
    // /// <param name="message">Error message on failure, MOTD on success.</param>
    // public void LoginHandler(object sender, LoginProgressEventArgs e)
    // {
    //     if (e.Status == LoginStatus.Success)
    //     {
    //         // Start in the inventory root folder.
    //         CurrentDirectory = Inventory.Store.RootFolder;
    //     }
    // }

    public void RegisterAllCommands(Assembly assembly)
    {
        foreach (Type t in assembly.GetTypes())
        {
            try
            {
                if (t.IsSubclassOf(typeof(Command)))
                {
                    ConstructorInfo info = t.GetConstructor(new[] { typeof(TestClient) });
                    Command command = (Command)info.Invoke(new object[] { this });
                    RegisterCommand(command);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }
    }

    public void RegisterCommand(Command command)
    {
        // command.Client = this;
        if (!Commands.ContainsKey(command.Name.ToLower()))
        {
            Commands.Add(command.Name.ToLower(), command);
        }
    }

    public void ReloadGroupsCache()
    {
        Groups.CurrentGroups += Groups_CurrentGroups;            
        Groups.RequestCurrentGroups();
        GroupsEvent.WaitOne(10000, false);
        Groups.CurrentGroups -= Groups_CurrentGroups;
        GroupsEvent.Reset();
    }

    void Groups_CurrentGroups(object sender, CurrentGroupsEventArgs e)
    {
        if (null == GroupsCache)
            GroupsCache = e.Groups;
        else
            lock (GroupsCache) { GroupsCache = e.Groups; }
        GroupsEvent.Set();
    }

    public UUID GroupName2UUID(string groupName)
    {
        UUID tryUUID;
        if (UUID.TryParse(groupName,out tryUUID))
            return tryUUID;
        if (null == GroupsCache) {
            ReloadGroupsCache();
            if (null == GroupsCache)
                return UUID.Zero;
        }
        lock(GroupsCache) {
            if (GroupsCache.Count > 0) {
                foreach (Group currentGroup in GroupsCache.Values)
                    if (string.Equals(currentGroup.Name, groupName, StringComparison.CurrentCultureIgnoreCase))
                        return currentGroup.ID;
            }
        }
        return UUID.Zero;
    }      

    private void updateTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
    {
        foreach (Command c in Commands.Values)
            if (c.Active)
                c.Think();
    }

    private void AgentDataUpdateHandler(object sender, PacketReceivedEventArgs e)
    {
        AgentDataUpdatePacket p = (AgentDataUpdatePacket)e.Packet;
        if (p.AgentData.AgentID == e.Simulator.Client.Self.AgentID && p.AgentData.ActiveGroupID != UUID.Zero)
        {
            GroupID = p.AgentData.ActiveGroupID;
                
            GroupMembersRequestID = e.Simulator.Client.Groups.RequestGroupMembers(GroupID);
        }
    }

    private void GroupMembersHandler(object sender, GroupMembersReplyEventArgs e)
    {
        if (e.RequestID != GroupMembersRequestID) return;

        GroupMembers = e.Members;
    }

    private void AvatarAppearanceHandler(object sender, PacketReceivedEventArgs e)
    {
        Packet packet = e.Packet;
            
        AvatarAppearancePacket appearance = (AvatarAppearancePacket)packet;

        lock (Appearances) Appearances[appearance.Sender.ID] = appearance;
    }

    private void AlertMessageHandler(object sender, PacketReceivedEventArgs e)
    {
        Packet packet = e.Packet;
            
        AlertMessagePacket message = (AlertMessagePacket)packet;

        Debug.Log("[AlertMessage] " + Utils.BytesToString(message.AlertData.Message));//, Helpers.LogLevel.Info, this);
    }
       
    private void Inventory_OnInventoryObjectReceived(object sender, InventoryObjectOfferedEventArgs e)
    {
        if (MasterKey != UUID.Zero)
        {
            if (e.Offer.FromAgentID != MasterKey)
                return;
        }
        else if (GroupMembers != null && !GroupMembers.ContainsKey(e.Offer.FromAgentID))
        {
            return;
        }

        e.Accept = true;
        return;
    }

    NetworkManager IGridClient.Network => Network;

    Settings IGridClient.Settings => Settings;

    ParcelManager IGridClient.Parcels => Parcels;

    AgentManager IGridClient.Self => Self;

    AvatarManager IGridClient.Avatars => Avatars;

    EstateTools IGridClient.Estate => Estate;

    FriendsManager IGridClient.Friends => Friends;

    GridManager IGridClient.Grid => Grid;

    ObjectManager IGridClient.Objects => Objects;

    GroupManager IGridClient.Groups => Groups;

    AssetManager IGridClient.Assets => Assets;

    InventoryAISClient IGridClient.AisClient => AisClient;

    AppearanceManager IGridClient.Appearance => Appearance;

    InventoryManager IGridClient.Inventory => Inventory;

    DirectoryManager IGridClient.Directory => Directory;

    TerrainManager IGridClient.Terrain => Terrain;

    SoundManager IGridClient.Sound => Sound;

    AgentThrottle IGridClient.Throttle => Throttle;

    UtilizationStatistics IGridClient.Stats => Stats;
}

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


public class SLClient : MonoBehaviour, IGridClient
{
    /// <summary>
    /// Unfortunately, we need a singleton instance to allow a 'universal' reference.
    /// This can introduce a race condition.
    /// </summary>
    /// <remarks>
    /// Unity doesn't need an object to interact with classes (thanks to true singletons), but this will allow us to place our singleton code on one object
    /// Login code would preferably be on some kind of unity-obj (to utilize the 2D system), placing a single script for login on the singleton, then a script to notify/call the login script. But that's well ahead of this prototype
    /// Lastly; unity objects will need Don't Destroy On Load. So perhaps a true singleton would be a better solution.
    /// </remarks>
    public static SLClient Instance { get; private set; } 
    
    public UnityGridClient InternalClient { get; private set; }
    public NetworkManager Network => ((IGridClient)InternalClient).Network;

    public Settings Settings => ((IGridClient)InternalClient).Settings;

    public ParcelManager Parcels => ((IGridClient)InternalClient).Parcels;

    public AgentManager Self => ((IGridClient)InternalClient).Self;

    public AvatarManager Avatars => ((IGridClient)InternalClient).Avatars;

    public EstateTools Estate => ((IGridClient)InternalClient).Estate;

    public FriendsManager Friends => ((IGridClient)InternalClient).Friends;

    public GridManager Grid => ((IGridClient)InternalClient).Grid;

    public ObjectManager Objects => ((IGridClient)InternalClient).Objects;

    public GroupManager Groups => ((IGridClient)InternalClient).Groups;

    public AssetManager Assets => ((IGridClient)InternalClient).Assets;

    public InventoryAISClient AisClient => ((IGridClient)InternalClient).AisClient;

    public AppearanceManager Appearance => ((IGridClient)InternalClient).Appearance;

    public InventoryManager Inventory => ((IGridClient)InternalClient).Inventory;

    public DirectoryManager Directory => ((IGridClient)InternalClient).Directory;

    public TerrainManager Terrain => ((IGridClient)InternalClient).Terrain;

    public SoundManager Sound => ((IGridClient)InternalClient).Sound;

    public AgentThrottle Throttle => ((IGridClient)InternalClient).Throttle;

    public UtilizationStatistics Stats => ((IGridClient)InternalClient).Stats;

    private void Awake()
    {
        if (Instance != null && Instance != this)
            throw new InvalidOperationException("Cannot have two instances of an SLClient, it should be a Singleton!");
        Instance = this;
        InternalClient = new UnityGridClient();
    }
    
    
    private const string VERSION = "0.0.1";
    private const string CLIENT_NAME = "UnityViewerPrototype";


    // public Dictionary<UUID, TestClient> Clients = new Dictionary<UUID, TestClient>();

    public const string MAIN_SERVER = Settings.AGNI_LOGIN_SERVER;
    public const string DEV_SERVER = Settings.ADITI_LOGIN_SERVER;
    public const string Resident = "resident";
    public void UsernameLogin(string userName, string pw) => Login(userName, Resident, pw);
    public void UsernameLogin(string userName, string pw, string uri) => Login(userName, Resident, pw, uri);
    public void Login(string fName, string lName, string pw) => Login(fName, lName, pw, MAIN_SERVER);
    public void Login(string fName, string lName, string pw, string uri)
    {
        // var uri = Settings.AGNI_LOGIN_SERVER;
        var account = new LoginDetails() { FirstName = fName, LastName = lName, Password = pw, URI = uri};
        Network.LoginProgress +=
            delegate(object sender, LoginProgressEventArgs e)
            {
                Debug.Log($"Login {e.Status}: {e.Message}"); //, Helpers.LogLevel.Info, client);

                if (e.Status == LoginStatus.Success)
                {
                    //
                    // if (MasterKey == UUID.Zero)
                    // {
                    //     UUID query = UUID.Zero;
                    //
                    //     void PeopleDirCallback(object sender2, DirPeopleReplyEventArgs dpe)
                    //     {
                    //         if (dpe.QueryID != query)
                    //         {
                    //             return;
                    //         }
                    //
                    //         if (dpe.MatchedPeople.Count != 1)
                    //         {
                    //             Debug.Log(
                    //                 $"Unable to resolve master key from {client.MasterName}"); //, Helpers.LogLevel.Warning);
                    //         }
                    //         else
                    //         {
                    //             client.MasterKey = dpe.MatchedPeople[0].AgentID;
                    //             Debug.Log($"Master key resolved to {client.MasterKey}"); //, Helpers.LogLevel.Info);
                    //         }
                    //     }
                    //
                    //     client.Directory.DirPeopleReply += PeopleDirCallback;
                    //     query = client.Directory.StartPeopleSearch(client.MasterName, 0);
                    // }

                    Debug.Log($"Logged in {this}"); //, Helpers.LogLevel.Info);
                }
                else if (e.Status == LoginStatus.Failed)
                {
                    Debug.Log($"Failed to login {account.FirstName} {account.LastName}:\n{Network.LoginMessage}"); //, Helpers.LogLevel.Warning);
                   
                }
            };

        // Optimize the throttle
        Throttle.Wind = 0;
        Throttle.Cloud = 0;
        Throttle.Land = 1000000;
        Throttle.Task = 1000000;

        // GroupCommands = account.GroupCommands;
        // MasterName = account.MasterName;
        // MasterKey = account.MasterKey;
        // client.AllowObjectMaster = client.MasterKey != UUID.Zero; // Require UUID for object master.

        LoginParams loginParams = Network.DefaultLoginParams(
            account.FirstName, account.LastName, account.Password, CLIENT_NAME, VERSION);


        if (!string.IsNullOrEmpty(account.StartLocation))
            loginParams.Start = account.StartLocation;

        if (!string.IsNullOrEmpty(account.URI))
            loginParams.URI = account.URI;

        Network.BeginLogin(loginParams);
    }
}
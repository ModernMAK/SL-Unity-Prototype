using System;
using Libre;
using LibreMetaverse;
using OpenMetaverse;
using OpenMetaverse.Stats;
using UnityEngine;

namespace SLUnity.Managers
{
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
            Assertions.AssertSingleton(this,Instance,nameof(SLClient));
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

                        Debug.Log($"Logged in {account.FirstName}"); //, Helpers.LogLevel.Info);
                    }
                    else if (e.Status == LoginStatus.Failed)
                    {
                        Debug.Log($"Failed to login {account.FirstName} {account.LastName}:\n{Network.LoginMessage}"); //, Helpers.LogLevel.Warning);
                   
                    }
                    else
                    {
                        Debug.Log($"Login {e.Status}: {e.Message}"); //, Helpers.LogLevel.Info, client);
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

        private void OnDestroy()
        {
            Instance.Network.Logout();
        }
    }
}
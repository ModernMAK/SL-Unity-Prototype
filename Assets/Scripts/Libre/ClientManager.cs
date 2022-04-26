using System;
using System.Collections;
using System.Collections.Generic;
using OpenMetaverse;
using UnityEngine;
// using Logger = OpenMetaverse.Logger;
using Vector3 = System.Numerics.Vector3;

public class ClientManager
{
    private const string VERSION = "0.0.1";
    private const string CLIENT_NAME = "UnityViewerPrototype";

    class Singleton
    {
        internal static readonly ClientManager Instance = new ClientManager();
    }

    public static ClientManager Instance => Singleton.Instance;
    public volatile int PendingLogins = 0;

    public Dictionary<UUID, TestClient> Clients = new Dictionary<UUID, TestClient>();

    public TestClient Login(string fName, string lName, string pw)
    {
        // var uri = Settings.AGNI_LOGIN_SERVER;
        var account = new LoginDetails() { FirstName = fName, LastName = lName, Password = pw };
        var client = new TestClient(this);
        client.Network.LoginProgress +=
            delegate(object sender, LoginProgressEventArgs e)
            {
                Debug.Log($"Login {e.Status}: {e.Message}");//, Helpers.LogLevel.Info, client);

                if (e.Status == LoginStatus.Success)
                {
                    Clients[client.Self.AgentID] = client;

                    if (client.MasterKey == UUID.Zero)
                    {
                        UUID query = UUID.Zero;

                        void PeopleDirCallback(object sender2, DirPeopleReplyEventArgs dpe)
                        {
                            if (dpe.QueryID != query)
                            {
                                return;
                            }

                            if (dpe.MatchedPeople.Count != 1)
                            {
                                Debug.Log($"Unable to resolve master key from {client.MasterName}");//, Helpers.LogLevel.Warning);
                            }
                            else
                            {
                                client.MasterKey = dpe.MatchedPeople[0].AgentID;
                                Debug.Log($"Master key resolved to {client.MasterKey}");//, Helpers.LogLevel.Info);
                            }
                        }

                        client.Directory.DirPeopleReply += PeopleDirCallback;
                        query = client.Directory.StartPeopleSearch(client.MasterName, 0);
                    }

                    Debug.Log($"Logged in {client}");//, Helpers.LogLevel.Info);
                    --PendingLogins;
                }
                else if (e.Status == LoginStatus.Failed)
                {
                    Debug.Log($"Failed to login {account.FirstName} {account.LastName}: {client.Network.LoginMessage}");//, Helpers.LogLevel.Warning);
                    --PendingLogins;
                }
            };

        // Optimize the throttle
        client.Throttle.Wind = 0;
        client.Throttle.Cloud = 0;
        client.Throttle.Land = 1000000;
        client.Throttle.Task = 1000000;

        client.GroupCommands = account.GroupCommands;
        client.MasterName = account.MasterName;
        client.MasterKey = account.MasterKey;
        // client.AllowObjectMaster = client.MasterKey != UUID.Zero; // Require UUID for object master.

        LoginParams loginParams = client.Network.DefaultLoginParams(
            account.FirstName, account.LastName, account.Password, CLIENT_NAME, VERSION);
        

        if (!string.IsNullOrEmpty(account.StartLocation))
            loginParams.Start = account.StartLocation;

        if (!string.IsNullOrEmpty(account.URI))
            loginParams.URI = account.URI;

        client.Network.BeginLogin(loginParams);
        return client;
    }
    //
    // /// <summary>
    // /// 
    // /// </summary>
    // /// <param name="cmd"></param>
    // /// <param name="fromAgentID"></param>
    // /// <param name="imSessionID"></param>
    // public void DoCommandAll(string cmd, UUID fromAgentID)
    // {
    //     throw new NotImplementedException();
    //     // if (cmd == null)
    //     //     return;
    //     // string[] tokens = cmd.Trim().Split(' ', '\t');
    //     // if (tokens.Length == 0)
    //     //     return;
    //     //
    //     // string firstToken = tokens[0].ToLower();
    //     // if (string.IsNullOrEmpty(firstToken))
    //     //     return;
    //     //
    //     // // Allow for comments when cmdline begins with ';' or '#'
    //     // if (firstToken[0] == ';' || firstToken[0] == '#')
    //     //     return;
    //     //
    //     // if ('@' == firstToken[0]) {
    //     //     onlyAvatar = string.Empty;
    //     //     if (tokens.Length == 3) {
    //     //         onlyAvatar = tokens[1]+" "+tokens[2];
    //     //         bool found = Clients.Values.Any(client => (client.ToString() == onlyAvatar) && (client.Network.Connected));
    //     //
    //     //         Logger.Log(
    //     //             found
    //     //                 ? $"Commanding only {onlyAvatar} now"
    //     //                 : $"Commanding nobody now. Avatar {onlyAvatar} is offline", Helpers.LogLevel.Info);
    //     //     } else {
    //     //         Logger.Log("Commanding all avatars now", Helpers.LogLevel.Info);
    //     //     }
    //     //     return;
    //     // }
    //     //
    //     // string[] args = new string[tokens.Length - 1];
    //     // if (args.Length > 0)
    //     //     Array.Copy(tokens, 1, args, 0, args.Length);
    //     //
    //     // if (firstToken == "login")
    //     // {
    //     //     Login(args);
    //     // }
    //     // else if (firstToken == "quit")
    //     // {
    //     //     Quit();
    //     //     Logger.Log("All clients logged out and program finished running.", Helpers.LogLevel.Info);
    //     // }
    //     // else if (firstToken == "help")
    //     // {
    //     //     if (Clients.Count > 0)
    //     //     {
    //     //         foreach (TestClient client in Clients.Values)
    //     //         {
    //     //             Console.WriteLine(client.Commands["help"].Execute(args, UUID.Zero));
    //     //             break;
    //     //         }
    //     //     }
    //     //     else
    //     //     {
    //     //         Console.WriteLine("You must login at least one bot to use the help command");
    //     //     }
    //     // }
    //     // else if (firstToken == "script")
    //     // {
    //     //     // No reason to pass this to all bots, and we also want to allow it when there are no bots
    //     //     ScriptCommand command = new ScriptCommand(null);
    //     //     Logger.Log(command.Execute(args, UUID.Zero), Helpers.LogLevel.Info);
    //     // }
    //     // else if (firstToken == "waitforlogin")
    //     // {
    //     //     // Special exception to allow this to run before any bots have logged in
    //     //     if (ClientManager.Instance.PendingLogins > 0)
    //     //     {
    //     //         WaitForLoginCommand command = new WaitForLoginCommand(null);
    //     //         Logger.Log(command.Execute(args, UUID.Zero), Helpers.LogLevel.Info);
    //     //     }
    //     //     else
    //     //     {
    //     //         Logger.Log("No pending logins", Helpers.LogLevel.Info);
    //     //     }
    //     // }
    //     // else
    //     // {
    //     //     // Make an immutable copy of the Clients dictionary to safely iterate over
    //     //     Dictionary<UUID, TestClient> clientsCopy = new Dictionary<UUID, TestClient>(Clients);
    //     //
    //     //     int completed = 0;
    //     //
    //     //     foreach (TestClient client in clientsCopy.Values)
    //     //     {
    //     //         ThreadPool.QueueUserWorkItem(
    //     //             delegate(object state)
    //     //             {
    //     //                 TestClient testClient = (TestClient)state;
    //     //                 if ((string.Empty == onlyAvatar) || (testClient.ToString() == onlyAvatar)) {
    //     //                     if (testClient.Commands.ContainsKey(firstToken)) {
    //     //                         string result;
    //     //                         try {
    //     //                             result = testClient.Commands[firstToken].Execute(args, fromAgentID);
    //     //                             Logger.Log(result, Helpers.LogLevel.Info, testClient);
    //     //                         } catch(Exception e) {
    //     //                             Logger.Log($"{firstToken} raised exception {e}",
    //     //                                        Helpers.LogLevel.Error,
    //     //                                        testClient);
    //     //                         }
    //     //                     } else
    //     //                         Logger.Log($"Unknown command {firstToken}", Helpers.LogLevel.Warning);
    //     //                 }
    //     //
    //     //                 ++completed;
    //     //             },
    //     //             client);
    //     //     }
    //     //
    //     //     while (completed < clientsCopy.Count)
    //     //         Thread.Sleep(50);
    // }


}

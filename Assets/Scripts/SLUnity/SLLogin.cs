using SLUnity.Managers;
using SLUnity.Objects;
using UnityEngine;

namespace SLUnity
{
    public class SLLogin : SLBehaviour
    {
        //https://community.secondlife.com/knowledgebase/english/usernames-and-display-names-r79/#Section__1_1
        public const string Resident = "resident";
        //
        // public string first, last, pw;
        public SLToken LoginToken;
    
        public bool devLogin = false;

        public string first => LoginToken.FirstName;
        public string last => LoginToken.LastName;
        public string pw => LoginToken.Password;

        
        private bool _loggedIn = false;
        // public bool PrintInventory;
        // private bool loggedIn = false;
        private void Update()
        {
            if(_loggedIn) return;
            _loggedIn = true;
            var uri = devLogin ? SLClient.DEV_SERVER : SLClient.MAIN_SERVER;
            Debug.Log($"Logging in as '{first}' '{last}' @ {uri}");
            if (string.IsNullOrWhiteSpace(last))
                Client.UsernameLogin(first,pw,uri);
            else
                Client.Login(first,last,pw,uri);
            enabled = false;
        }
//
//     // private void ObjectsOnObjectPropertiesUpdated(object sender, ObjectPropertiesUpdatedEventArgs e)
//     // {
//     //     var prim = e.Prim;
//     //     if (!_primTableHack.ContainsKey(prim.ID) && prim.Type == PrimType.Mesh)
//     //     {
//     //         _primTableHack[prim.ID] = prim;
//     //         client.Assets.RequestMesh(prim.ID, MeshDownloaded);
//     //     }
//     // }
//
//     private Dictionary<UUID, Primitive> _primTableHack;
//     private  Queue<FacetedMesh> _meshQueue;
//     private void ObjectsOnObjectUpdate(object sender, PrimEventArgs e)
//     {
//         var prim = e.Prim;
//         if (e.IsNew && prim.Type == PrimType.Mesh)
//         {
//             _primTableHack[prim.Sculpt.SculptTexture] = prim;
//             client.Assets.RequestMesh(prim.Sculpt.SculptTexture, MeshDownloaded);
//             // client.Objects.ObjectPropertiesUpdated();
//         }
//     }
//
//     private void MeshDownloaded(bool success, AssetMesh assetMesh)
//     {
//         if (success)
//         {
//             // if (assetMesh.Decode()) -> called in TryDecodeFromAsset
//             // {
//             Primitive prim = _primTableHack[assetMesh.AssetID]; 
//             if (FacetedMesh.TryDecodeFromAsset(prim, assetMesh, DetailLevel.Highest, out var mesh))
//             {
//                 _meshQueue.Enqueue(mesh);
//             }
//             else
//             {
//                 Debug.Log("Linden Mesh decoding failed!");
//             }
//             // }
//             // else
//             // {
//             //     Debug.Log("Collada Mesh decoding failed!");
//             // }
//         }
//         else
//         {
//             Debug.Log("Mesh download failed!");
//         }
//     }
//
// }
//     
//
//     // private void InventoryOnItemReceived(object sender, ItemReceivedEventArgs e)
//     // {
//     //     if (e.Item.)
//     //         Debug.Log("Folder Updated:\n"+PrintContents(client.Inventory.Store[e.FolderID] as InventoryFolder));
//     //     else
//     //         Debug.Log("Could not update folder");
//     // }
//     //
//     // private void NetworkOnSimConnected(object sender, SimConnectedEventArgs e)
//     // {
//     //     var sim = e.Simulator;
//     //     // var objPrimLookup = sim.ObjectsPrimitives.Copy(); //Get a usable dictionary
//     // }
//
//     private void OnDestroy()
//     {
//         Debug.Log("Logging Out");
//         client.Network.Logout();
//     }
//
//     private void InventoryOnFolderUpdated(object sender, FolderUpdatedEventArgs e)
//     {
//         if (e.Success)
//             Debug.Log("Folder Updated:\n"+PrintContents(client.Inventory.Store[e.FolderID] as InventoryFolder));
//         else
//             Debug.Log("Could not update folder");
//         
//         
//     }
//
//     // private void FixedUpdate()
//     // {
//     //     if (!loggedIn) return;
//     //
//     //     if (PrintInventory)
//     //     {
//     //         PrintInventory = false;
//     //         // Debug.Log(PrintContents(client,rootFolder));
//     //     }
//     // }
//
//     private IEnumerable<InventoryBase> GetFolderContents(InventoryBase folder)
//     {
//         return client.Inventory.Store.Items[folder.UUID].Nodes.Values.Select(invNode => invNode.Data);
//     }
//
//     private string PrintContents(InventoryFolder folder, int depth = 0)
//     {
//         var tabs = "";
//         for (var i = 0; i < depth; i++)
//             tabs += "\t";
//
//         var contents = tabs + folder.Name + $" ({folder.DescendentCount})";
//         foreach (var invNode in client.Inventory.Store.Items[folder.UUID].Nodes.Values)
//         {
//             var invData = invNode.Data;
//             if (invData is InventoryFolder subFolder)
//                 contents += "\n" + PrintContents(subFolder, depth + 1);
//             else
//                 contents += "\n" + invData.Name;
//             // Debug.Log(contents);
//         }
//
//         return contents;
//     }
    }
}

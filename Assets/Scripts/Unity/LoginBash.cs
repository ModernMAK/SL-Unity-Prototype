using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using OpenMetaverse;
using OpenMetaverse.Assets;
using UnityEngine;
using XmlRpcCore;
using OpenMetaverse.Rendering;
using UnityEngine.Rendering;
using Mesh = UnityEngine.Mesh;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;

public class LoginBash : MonoBehaviour
{
    //https://community.secondlife.com/knowledgebase/english/usernames-and-display-names-r79/#Section__1_1
    public const string Resident = "resident";
    //
    public string first, last, pw;
    // public bool PrintInventory;
    private TestClient client;
    // private bool loggedIn = false;
    private void Start()
    {
        _meshQueue = new Queue<FacetedMesh>();
        _primTableHack = new Dictionary<UUID, Primitive>();
        var manager = ClientManager.Instance;
        
        Debug.Log($"Logging in as '{first}' '{last}' w/ '{pw}'");
        client = manager.Login(first,last,pw);
        client.Network.LoginProgress += delegate(object sender, LoginProgressEventArgs args)
        {
            if (args.Status != LoginStatus.Success) return;
            Debug.Log("Requesting Inventory Contents");
            client.Inventory.RequestFolderContents(client.Inventory.Store.RootFolder.UUID,client.Inventory.Store.Owner,true,true,InventorySortOrder.ByName);    
        };
        // client.Network.SimConnected += NetworkOnSimConnected;
        client.Inventory.FolderUpdated += InventoryOnFolderUpdated;
        // client.Inventory.ItemReceived += InventoryOnItemReceived;
        // client.Network.CurrentSim.
        client.Objects.ObjectUpdate += ObjectsOnObjectUpdate;
        // client.Objects.ObjectPropertiesUpdated += ObjectsOnObjectPropertiesUpdated;
    }

    // private void ObjectsOnObjectPropertiesUpdated(object sender, ObjectPropertiesUpdatedEventArgs e)
    // {
    //     var prim = e.Prim;
    //     if (!_primTableHack.ContainsKey(prim.ID) && prim.Type == PrimType.Mesh)
    //     {
    //         _primTableHack[prim.ID] = prim;
    //         client.Assets.RequestMesh(prim.ID, MeshDownloaded);
    //     }
    // }

    private Dictionary<UUID, Primitive> _primTableHack;
    private  Queue<FacetedMesh> _meshQueue;
    private void ObjectsOnObjectUpdate(object sender, PrimEventArgs e)
    {
        var prim = e.Prim;
        if (e.IsNew && prim.Type == PrimType.Mesh)
        {
            _primTableHack[prim.Sculpt.SculptTexture] = prim;
            client.Assets.RequestMesh(prim.Sculpt.SculptTexture, MeshDownloaded);
            // client.Objects.ObjectPropertiesUpdated();
        }
    }

    private void MeshDownloaded(bool success, AssetMesh assetMesh)
    {
        if (success)
        {
            // if (assetMesh.Decode()) -> called in TryDecodeFromAsset
            // {
            Primitive prim = _primTableHack[assetMesh.AssetID]; 
            if (FacetedMesh.TryDecodeFromAsset(prim, assetMesh, DetailLevel.Highest, out var mesh))
            {
                _meshQueue.Enqueue(mesh);
            }
            else
            {
                Debug.Log("Linden Mesh decoding failed!");
            }
            // }
            // else
            // {
            //     Debug.Log("Collada Mesh decoding failed!");
            // }
        }
        else
        {
            Debug.Log("Mesh download failed!");
        }
    }

    private void FixedUpdate()
    {
        RenderMeshInQueue();
    }

    private static UnityEngine.Vector3 ToUnityVector3(OpenMetaverse.Vector3 v) => new Vector3(v.X, v.Y, v.Z);
    private static UnityEngine.Vector2 ToUnityVector2(OpenMetaverse.Vector2 v) => new Vector2(v.X, v.Y);

    private void RenderMeshInQueue()
    {
        if (_meshQueue.Count == 0)
            return;

        var slMesh = _meshQueue.Dequeue();
        var uMesh = new Mesh();
        var vertList = new List<Vector3>(slMesh.Faces.Count * 3); // Assume at least that many triangles
        var normList = new List<Vector3>(slMesh.Faces.Count * 3); // Assume at least that many triangles
        var uvList = new List<Vector2>(slMesh.Faces.Count * 3); // Assume at least that many triangles
        var subMeshTriangleIndexes = new List<int>[slMesh.Faces.Count]; //Assume each face is a submesh?

        for (var faceIndex = 0; faceIndex < slMesh.Faces.Count; faceIndex++)
        {
            var slF = slMesh.Faces[faceIndex];
            var indList =
                subMeshTriangleIndexes[faceIndex] = new List<int>(slF.Vertices.Count); // One per vertex (at least).
            var vOffset = vertList.Count; // Single buffer
            foreach (var v in slF.Vertices)
            {
                vertList.Add(ToUnityVector3(v.Position));
                normList.Add(ToUnityVector3(v.Normal));
                uvList.Add(ToUnityVector2(v.TexCoord));
            }

            foreach (var i in slF.Indices)
            {
                indList.Add(vOffset + i);
            }
        }

        const MeshUpdateFlags dontUpdate = MeshUpdateFlags.DontResetBoneBounds | MeshUpdateFlags.DontNotifyMeshUsers |
                                           MeshUpdateFlags.DontValidateIndices | MeshUpdateFlags.DontRecalculateBounds;
        const int textureUvs = 0;
        uMesh.SetVertices(vertList, 0, vertList.Count, dontUpdate);
        uMesh.SetNormals(normList, 0, normList.Count, dontUpdate);
        uMesh.SetUVs(textureUvs, uvList, 0, uvList.Count, dontUpdate);
        for (var subMeshIndex = 0; subMeshIndex < subMeshTriangleIndexes.Length; subMeshIndex++)
        {
            var triangleList = subMeshTriangleIndexes[subMeshIndex];
            uMesh.SetTriangles(triangleList,subMeshIndex,true);
        }
        uMesh.Optimize();
}
    

    // private void InventoryOnItemReceived(object sender, ItemReceivedEventArgs e)
    // {
    //     if (e.Item.)
    //         Debug.Log("Folder Updated:\n"+PrintContents(client.Inventory.Store[e.FolderID] as InventoryFolder));
    //     else
    //         Debug.Log("Could not update folder");
    // }
    //
    // private void NetworkOnSimConnected(object sender, SimConnectedEventArgs e)
    // {
    //     var sim = e.Simulator;
    //     // var objPrimLookup = sim.ObjectsPrimitives.Copy(); //Get a usable dictionary
    // }

    private void OnApplicationQuit()
    {
        Debug.Log("Logging Out");
        client.Network.Logout();
    }

    private void InventoryOnFolderUpdated(object sender, FolderUpdatedEventArgs e)
    {
        if (e.Success)
            Debug.Log("Folder Updated:\n"+PrintContents(client.Inventory.Store[e.FolderID] as InventoryFolder));
        else
            Debug.Log("Could not update folder");
        
        
    }

    // private void FixedUpdate()
    // {
    //     if (!loggedIn) return;
    //
    //     if (PrintInventory)
    //     {
    //         PrintInventory = false;
    //         // Debug.Log(PrintContents(client,rootFolder));
    //     }
    // }

    private IEnumerable<InventoryBase> GetFolderContents(InventoryBase folder)
    {
        return client.Inventory.Store.Items[folder.UUID].Nodes.Values.Select(invNode => invNode.Data);
    }

    private string PrintContents(InventoryFolder folder, int depth = 0)
    {
        var tabs = "";
        for (var i = 0; i < depth; i++)
            tabs += "\t";

        var contents = tabs + folder.Name + $" ({folder.DescendentCount})";
        foreach (var invNode in client.Inventory.Store.Items[folder.UUID].Nodes.Values)
        {
            var invData = invNode.Data;
            if (invData is InventoryFolder subFolder)
                contents += "\n" + PrintContents(subFolder, depth + 1);
            else
                contents += "\n" + invData.Name;
            // Debug.Log(contents);
        }

        return contents;
    }
}

using NetworkingPackage;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using UnityEngine;

namespace UWBNetworkingPackage
{
    /// <summary>
    /// ReceivingClientLauncher is an abstract class (extended by all "Client" devices - Vive, Oculus, Kinect) that connects 
    /// to Photon and sets up a TCP connection with the Master Client to recieve Room Meshes when they are sent
    /// </summary>
    public abstract class ReceivingClientLauncher : Launcher
    {
//// Ensure not HoloLens
//#if UNITY_EDITOR && !UNITY_WSA_10_0

        /// <summary>
        /// After connect to master server, join the room that's specify by Laucher.RoomName
        /// </summary>
        public override void OnConnectedToMaster()
        {
            PhotonNetwork.JoinRoom(RoomName);
        }

        /// <summary>
        /// After join the room, ask master client to sent the mesh to this client
        /// </summary>
        public override void OnJoinedRoom()
        {
            Debug.Log("Client joined room.");
            photonView.RPC("SendMesh", PhotonTargets.MasterClient, PhotonNetwork.player.ID);
        }

        /// <summary>
        /// When cannot join the room (refer to UWBNetworkingPackage documentation for possible reasons of failure), 
        /// disconnect from Photon
        /// </summary>
        /// <param name="codeAndMsg">Information about the failed connection</param>
        public override void OnPhotonJoinRoomFailed(object[] codeAndMsg)
        {
            Debug.Log("A room created by the Master Client could not be found. Disconnecting from PUN");
            PhotonNetwork.Disconnect();
        }

        #region RPC Method

        /// <summary>
        /// Receive Room Mesh from specified network configuration. This is a RPC method that will be called by the Master Client
        /// </summary>
        /// <param name="networkConfig">The IP and port number that client can reveice room mesh from. The format is IP:Port</param>
        [PunRPC]
        public override void ReceiveMesh(string networkConfig)
        {
            var networkConfigArray = networkConfig.Split(':');

            TcpClient client = new TcpClient();
            client.Connect(IPAddress.Parse(networkConfigArray[0]), Int32.Parse(networkConfigArray[1]));

            using (var stream = client.GetStream())
            {
                byte[] data = new byte[1024];

                Debug.Log("Start receiving mesh.");
                using (MemoryStream ms = new MemoryStream())
                {
                    int numBytesRead;
                    while ((numBytesRead = stream.Read(data, 0, data.Length)) > 0)
                    {
                        ms.Write(data, 0, numBytesRead);
                    }
                    Debug.Log("Finish receiving mesh: size = " + ms.Length);
                    client.Close();

                    //DONE RECIEVING MESH FROM THE MASTER SERVER, NOW UPDATE IT
                    Database.UpdateMesh(ms.ToArray());
                    Debug.Log("You updated the meshes in the database");
                }
            }

            client.Close();


            //CREATE AND DRAW THEM MESHES------------------------------------------------------
            Debug.Log("Checking for them meshes in ze database");

            //goes into the if statement if the database is not NULL
            if (Database.GetMeshAsList() != null)
            {
                //Create a material to apply to the mesh
                Material meshMaterial = new Material(Shader.Find("Diffuse"));

                //grab the meshes in the database
                IEnumerable<Mesh> temp = new List<Mesh>(Database.GetMeshAsList());

                foreach (var mesh in temp)
                {
                    //for each mesh in the database, create a game object to represent
                    //and display the mesh in the scene
                    GameObject obj1 = new GameObject("mesh");

                    //add a mesh filter to the object and assign it the mesh
                    MeshFilter filter = obj1.AddComponent<MeshFilter>();
                    filter.mesh = mesh;

                    //add a mesh rendererer and add a material to it
                    MeshRenderer rend1 = obj1.AddComponent<MeshRenderer>();
                    rend1.material = meshMaterial;

                    // Add a mesh collider to the object
                    obj1.AddComponent<MeshCollider>();
                }
            }
            else
            {
                UnityEngine.Debug.Log("YO... your mesh is empty...");
            }
            //END OF CREATING AND DRAWING THE MEESHES------------------------------------------
        }
        #endregion
        //#endif
    }
}
﻿

using System;
using UnityEngine;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Collections.Generic;
//using HoloToolkit.Unity;

namespace UWBNetworkingPackage
{
    /// <summary>
    /// MasterClientLauncher implements launcher functionality specific to the MasterClient
    /// </summary>
    public class MasterClientLauncher : Launcher
    {

#if UNITY_STANDALONE
        #region Private Properties

        private DateTime _lastUpdate = DateTime.MinValue;   // Used for keeping the Room Mesh up to date

        #endregion

        /// <summary>
        /// Attempts to connect to the specified Room Name on start, and adds MeshDisplay component
        /// for displaying the Room Mesh
        /// </summary>
        public override void Start()
        {
            base.Start();
//            gameObject.AddComponent<MeshDisplay>();
        }

        /// <summary>
        /// Called once per frame
        /// When a new mesh is recieved, display it 
        /// When L is pressed, load and send a saved Room Mesh file (used for testing without HoloLens)
        /// </summary>
        public void Update()
        {
            if (Database.LastUpdate != DateTime.MinValue && DateTime.Compare(_lastUpdate, Database.LastUpdate) < 0)
            {
                if (Database.GetMeshAsBytes() != null)
                {
                //    Create a material to apply to the mesh
                    Material meshMaterial = new Material(Shader.Find("Diffuse"));

                //    grab the meshes in the database
                    IEnumerable<Mesh> temp = new List<Mesh>(Database.GetMeshAsList());

                    foreach (var mesh in temp)
                    {
                //        for each mesh in the database, create a game object to represent
                //        and display the mesh in the scene
                        GameObject obj1 = new GameObject("mesh");

                //        add a mesh filter to the object and assign it the mesh
                        MeshFilter filter = obj1.AddComponent<MeshFilter>();
                        filter.mesh = mesh;

                //        add a mesh rendererer and add a material to it
                        MeshRenderer rend1 = obj1.AddComponent<MeshRenderer>();
                        rend1.material = meshMaterial;
                    }
                }
                _lastUpdate = Database.LastUpdate;
            }

            //Loading a mesh from a file for testing purposes.
            if (Input.GetKeyDown("l"))
            {
                //                Database.UpdateMesh(MeshSaver.Load("RoomMesh"));
                var memoryStream = new MemoryStream(File.ReadAllBytes("RoomMesh"));
                this.DeleteLocalMesh();
                Database.UpdateMesh(memoryStream.ToArray());
                photonView.RPC("ReceiveMesh", PhotonTargets.Others, GetLocalIpAddress() + ":" + Port);
            }

            //Deleting of meshes for testing purposes
            if (Input.GetKeyDown("d"))
            {
                this.DeleteMesh();
            }
        }

        /// <summary>
        /// When connect to the Master Server, create a room using the specified room name
        /// </summary>
        public override void OnConnectedToMaster()
        {
            PhotonNetwork.CreateRoom(RoomName);
        }

        /// <summary>
        /// After creating a room, set up a multi-threading tcp listener to listen on the specified port
        /// Once someone connects to the port, send the currently saved (in Database) Room Mesh
        /// </summary>
        public override void OnCreatedRoom()
        {
            TcpListener server = new TcpListener(IPAddress.Any, Port);
            server.Start();
            new Thread(() =>
            {
                Debug.Log("MasterClient start listening for new connection");
                while (true)
                {
                    TcpClient client = server.AcceptTcpClient();
                    Debug.Log("New connection established");
                    new Thread(() =>
                    {
                        using (NetworkStream stream = client.GetStream())
                        {
                            var data = Database.GetMeshAsBytes();
                            stream.Write(data, 0, data.Length);
                            Debug.Log("Mesh sent: mesh size = " + data.Length);
                        }
                        client.Close();
                    }).Start();
                }
            }).Start();
        }

        #region RPC Method

        /// <summary>
        /// Send mesh to a host specified by PhotonNetwork.Player.ID 
        /// This is a RPC method that will be called by ReceivingClient
        /// </summary>
        /// <param name="id">The player id that will sent the mesh</param>
        [PunRPC]
        public override void SendMesh(int id)
        {
            if (Database.GetMeshAsBytes() != null)
            {
                photonView.RPC("ReceiveMesh", PhotonPlayer.Find(id), GetLocalIpAddress() + ":" + Port);
            }
        }

        /// <summary>
        /// Receive room mesh from specifed PhotonNetwork.Player.ID
        /// This is a RPC method this will be called by HoloLens
        /// </summary>
        /// <param name="id">The player id that will receive mesh</param>
        [PunRPC]
        public override void ReceiveMesh(int id)
        {
            // Setup TCPListener to wait and receive mesh
            this.DeleteLocalMesh();
            TcpListener receiveTcpListener = new TcpListener(IPAddress.Any, Port + 1);
            receiveTcpListener.Start();
            new Thread(() =>
            {
                var client = receiveTcpListener.AcceptTcpClient();
                using (var stream = client.GetStream())
                {
                    byte[] data = new byte[1024];

                    Debug.Log("Start receiving mesh");
                    using (MemoryStream ms = new MemoryStream())
                    {
                        int numBytesRead;
                        while ((numBytesRead = stream.Read(data, 0, data.Length)) > 0)
                        {
                            ms.Write(data, 0, numBytesRead);
                        }
                        Debug.Log("finish receiving mesh: size = " + ms.Length);
                        client.Close();
                        Database.UpdateMesh(ms.ToArray());
                    }
                }
                client.Close();
                receiveTcpListener.Stop();
                photonView.RPC("ReceiveMesh", PhotonTargets.Others, GetLocalIpAddress() + ":" + Port);
            }).Start();

            photonView.RPC("SendMesh", PhotonPlayer.Find(id), GetLocalIpAddress() + ":" + (Port + 1));
        }

        /// <summary>
        /// This will send a call to delete all meshes held by the clients
        /// This is a RPC method that will be called by ReceivingClient
        /// </summary>
        [PunRPC]
        public override void DeleteMesh()
        {
            if (Database.GetMeshAsBytes() != null)
            {
                photonView.RPC("DeleteLocalMesh", PhotonTargets.Others);
            }
            this.DeleteLocalMesh();
        }

        /// <summary>
        /// Initiates the sending of a Mesh to add
        /// </summary>
        public override void SendAddMesh()
        {
            if (Database.GetMeshAsBytes() != null)
            {
                photonView.RPC("ReceiveAddMesh", PhotonTargets.Others, GetLocalIpAddress() + ":" + Port);
            }
        }

        /// <summary>
        /// Receive room mesh from specifed PhotonNetwork.Player.ID. 
        /// and add it to the total roommesh
        /// </summary>
        /// <param name="networkConfig">The player id that will receive mesh</param>
        /// <param name="networkOrigin">The origin of the sent mesh</param>
        [PunRPC]
        public override void ReceiveAddMesh(int id)
        {
            // Setup TCPListener to wait and receive mesh
            TcpListener receiveTcpListener = new TcpListener(IPAddress.Any, Port + 1);
            receiveTcpListener.Start();
            new Thread(() =>
            {
                var client = receiveTcpListener.AcceptTcpClient();
                using (var stream = client.GetStream())
                {
                    byte[] data = new byte[1024];

                    Debug.Log("Start receiving mesh");
                    using (MemoryStream ms = new MemoryStream())
                    {
                        int numBytesRead;
                        while ((numBytesRead = stream.Read(data, 0, data.Length)) > 0)
                        {
                            ms.Write(data, 0, numBytesRead);
                        }
                        Debug.Log("finish receiving mesh: size = " + ms.Length);
                        client.Close();
                        Database.AddToMesh(ms.ToArray());
                    }
                }
                client.Close();
                receiveTcpListener.Stop();
                photonView.RPC("ReceiveMesh", PhotonTargets.Others, GetLocalIpAddress() + ":" + Port);
            }).Start();

            photonView.RPC("SendAddMesh", PhotonPlayer.Find(id), GetLocalIpAddress() + ":" + (Port + 1));
        }

        #endregion

#endif
    }
}

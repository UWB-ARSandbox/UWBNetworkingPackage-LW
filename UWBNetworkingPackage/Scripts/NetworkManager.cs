using System;
using Photon;
using UnityEngine;
using UnityEditor;

namespace UWBNetworkingPackage
{
    /// <summary>
    /// NetworkManager adds the correct Launcher script based on the user selected device (in the Unity Inspector)
    /// </summary>
    // Note: For future improvement, this class should: a) Detect the device and add the launcher automatically; 
    // or b) Only allow user to select one device
    [System.Serializable]
    public class NetworkManager : PunBehaviour
    {
        #region Public Properties

        public bool MasterClient = false;
        public bool HoloLens = false;
        public bool Kinect = false;
        public bool Android = false;
        public bool Vive = false;

        // Needed for Room Mesh sending
        [Tooltip("A port number for devices to communicate through. The port number should be the same for each set of projects that need to connect to each other and share the same Room Mesh.")]
        public int Port;

        // Needed for Photon 
        [Tooltip("The name of the room that this project will attempt to connect to. This room must be created by a \"Master Client\".")]
        public string RoomName;

        #endregion

        /// <summary>
        /// When Awake, NetworkManager will add the correct Launcher script
        /// </summary>
        void Awake()
        {
            //FORCE THIS VERSION TO BE THE ANDROID VERSION
            gameObject.AddComponent<MasterClientLauncher>();


            //Two Lines are required if the hololens project
            //AssetDatabase.ImportPackage("HoloToolKit", false);
            //gameObject.AddComponent<HoloLensLauncher>();

            //OLD SECTION OF CODE---------------------------------------------------------
            //----------------------------------------------------------------------------
            //THIS SECTION OF CODE WAS CAUSING ISSUES OF DEFAULTING BACK TO MASTER CLIENT
            //ON EVERY BUILD THAT WAS NOT MASTER CLIENT OR HOLOLENS...
            //----------------------------------------------------------------------------
            //if (HoloLens)
            //{
            //    gameObject.AddComponent<HoloLensLauncher>();
            //}
            //else if (MasterClient)
            //{
            //    gameObject.AddComponent<MasterClientLauncher>();
            //}
            //else if (Kinect)
            //{
            //    gameObject.AddComponent<KinectLauncher>();
            //}
            //else if (Vive)
            //{
            //    gameObject.AddComponent<ViveLauncher>();
            //}
            //else if (Android)
            //{
            //    gameObject.AddComponent<AndroidLauncher>();
            //}
            //else
            //{
            //    throw new MissingFieldException("You need to select the kind of device you are running on");
            //}
            //END OF OLD SECTION OF CODE-------------------------------------------------
        }

        /// <summary>
        /// This is a HoloLens specific method
        /// This method allows a HoloLens developer to send a Room Mesh when triggered by an event
        /// This is here because HoloLensLauncher is applied at runtime
        /// In the HoloLensDemo, this method is called when the phrase "Send Mesh" is spoken and heard by the HoloLens
        /// </summary>
        public void HoloSendMesh()
        {
            //SECTION OF CODE NOT NEEDED FOR ANDROID VERSION
            //if (HoloLens)
            //{
            //    gameObject.GetComponent<HoloLensLauncher>().SendMesh();
            //}
        }
    }
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon;
using Photon.Pun;
using Photon.Realtime;
namespace hcp
{
    public class photonTemp : MonoBehaviourPunCallbacks
    {

        // Use this for initialization
        void Awake()
        {
            PhotonNetwork.GameVersion = "0.1";

            if (PhotonNetwork.IsConnected) // 네트워크랑 연결 됐어?
            {
                PhotonNetwork.JoinRandomRoom();     //이 튜토에선 바로 룸에 들어가게. )
            }
            else
            {
                //  PhotonNetwork.GameVersion = gameVersion;
                PhotonNetwork.ConnectUsingSettings();   //일단 포톤 네트워크랑 접속이 안되있으면 접속 부터.
            }
        }

        /*
        public override void OnConnectedToMaster()
        {
            Debug.Log("커넥트 마스터");
        }*/

        // Update is called once per frame
        void Update()
        {

        }

        public override void OnConnectedToMaster()  //모노콜백 상속으로써 이렇게 콜백 함수 오버라이드 가능
        {
            //  base.OnConnectedToMaster();
            Debug.Log("PUN Basics Tutorial/Launcher: OnConnectedToMaster() was called by PUN");

            PhotonNetwork.JoinRandomRoom();

        }


        public override void OnDisconnected(DisconnectCause cause)
        {

            // base.OnDisconnected(cause);
            Debug.LogWarningFormat("PUN Basics Tutorial/Launcher: OnDisconnected() was called by PUN with reason {0}", cause);

        }
        public override void OnJoinRandomFailed(short returnCode, string message)   //랜덤 룸 입장 실패시 콜백
        {
            Debug.Log("PUN Basics Tutorial/Launcher:OnJoinRandomFailed() was called by PUN. No random room available, so we create one.\nCalling: PhotonNetwork.CreateRoom");
            PhotonNetwork.CreateRoom("room");


        }
        public override void OnJoinedRoom()
        {
            Debug.Log("PUN Basics Tutorial/Launcher: OnJoinedRoom() called by PUN. Now this client is in a room.");
            if (PhotonNetwork.CurrentRoom.PlayerCount == 1)
            {
                //    Debug.Log("We load the 'Room for 1' ");


                // #Critical
                // Load the Room Level.

            }
        }
    }
}
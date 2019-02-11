using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Photon;
using Photon.Pun;
namespace hcp
{
    public class GameEndJudgeManager : MonoBehaviourPun
    {
        [SerializeField]
        Canvas gameEndCanvas;
        [SerializeField]
        Image gameEndScreen;
        [SerializeField]
        Text gameEndText;
        [SerializeField]
        Material geScreenDissolveMat;

        [SerializeField]
        Payload payload;
        [SerializeField]
        bool payloadArrived ;
        [SerializeField]
        bool judgeDone;
        [SerializeField]
        Color winColor = new Color(0/255, 166/255, 255/255);
        [SerializeField]
        Color loseColor = new Color(255 / 255, 0 / 255, 44 / 255);

        private void Awake()
        {
            judgeDone = false;
            payloadArrived = false;
            
        }
        void Start()
        {/*
            Vector2 myVector = new Vector2(Screen.width, Screen.height);
            gameEndScreen.GetComponent<RectTransform>().sizeDelta = myVector;
            */

            TeamInfo.GetInstance().AddListenerOnCLCD(OnClientLefted);
            geScreenDissolveMat = new Material(gameEndScreen.material);
            gameEndScreen.material = geScreenDissolveMat;
            gameEndScreen.gameObject.SetActive(false);

            payload.AddListenerPayloadArrive(PayloadArrive);
        }
        void PayloadArrive()
        {
            payloadArrived = true;
        }
        bool IsMatchTimeDone()
        {
            //게임 시간 받아오기.
            if (NetworkManager.instance.GameEnd)
                return true;
            return false;
        }

        void OnClientLefted()
        {
            if (!PhotonNetwork.IsMasterClient || judgeDone) return;

            List<Hero> enemies = TeamInfo.GetInstance().EnemyHeroes;
            int cnt = 0;
            for (int i = 0; i < enemies.Count; i++)
            {
                if (enemies[i] != null) cnt ++;
            }
            if (cnt == 0)
            {
                //게임 종료.
             //   judgeDone = true;
                photonView.RPC("GameJudgeReceived", RpcTarget.All, Constants.GetE_TeamByLayer(TeamInfo.GetInstance().MyTeamLayer));
            }
        }

        // Update is called once per frame
        void Update()
        {
            if (!PhotonNetwork.IsMasterClient) return;
            if (judgeDone) return;

            if (payloadArrived)
            {
            //    judgeDone = true;
                //화물 도착, 게임 종료.
                photonView.RPC("GameJudgeReceived", RpcTarget.All, JudgeWhichTeamWin());
                return;
            }
            if (IsMatchTimeDone())
            {
                //우세한 팀만 붙어있을 떄는 종료.
                // 사람이 아무도 없으면 종료.
                if (!payload.HeroClose)
                {
               //     judgeDone = true;
                    //화물 도착, 게임 종료.
                    photonView.RPC("GameJudgeReceived", RpcTarget.All, JudgeWhichTeamWin());
                    return;
                }
                else
                {
                    //사람이 붙어 있음. 우세한 쪽에 따를것.
                    E_Team nowWiningTeam = JudgeWhichTeamWin();
                    switch (nowWiningTeam)
                    {
                        case E_Team.Team_A: //a팀이 우세한 경우.
                            if (payload.GetATeamCount > 0 && payload.GetBTeamCount == 0)
                            {
                           //     judgeDone = true;
                                photonView.RPC("GameJudgeReceived", RpcTarget.All, E_Team.Team_A);
                            }
                            break;
                        case E_Team.Team_B:
                            if (payload.GetATeamCount == 0 && payload.GetBTeamCount > 0)
                            {
                              //  judgeDone = true;
                                photonView.RPC("GameJudgeReceived", RpcTarget.All, E_Team.Team_B);
                            }
                            break;
                    }

                }
            }
        }

        E_Team JudgeWhichTeamWin()
        {
            float farFromA = payload.GetHowFarFromTeamA();
            float farFromB = payload.GetHowFarFromTeamB();

            if (farFromA >= farFromB)
            {
                // B팀 승리.
                return E_Team.Team_A;
            }
            else {
                //A팀 승리.
                return E_Team.Team_B;
            }
        }

        [PunRPC]
        public void GameJudgeReceived(E_Team winTeam)
        {
            if (PhotonNetwork.IsMasterClient)
            {
                payload.StopPayload();
            }
            if (judgeDone)
            {
                Debug.Log("이미 저지던이 켜져있으므로 기각");
                return; }
            judgeDone = true;
            int winTeamLayer = Constants.GetLayerByE_Team(winTeam);

            int myLayer = TeamInfo.GetInstance().MyTeamLayer;

            if (winTeamLayer == myLayer)
            {
                //나 이겼음.

                StartCoroutine(GameEndShow(true));
                
            }
            else
            {
                StartCoroutine(GameEndShow(false));
                //나 쟜음.
            }
        }
        
        IEnumerator GameEndShow(bool win)
        {
            gameEndScreen.gameObject.SetActive(true);
            gameEndText.gameObject.SetActive(false);
            Color col;
            if (win)
            {
                col = winColor;
            }
            else {
                col = loseColor;
            }
                
            geScreenDissolveMat.SetColor("_EdgeColour2", col);
            float startTime = 0f;
            while (startTime < 1f)
            {
                startTime += Time.deltaTime;
                geScreenDissolveMat.SetFloat("_Level", 1 - startTime);
                yield return null;
            }
            geScreenDissolveMat.SetFloat("_Level", 0);
            if (win)
            {
                gameEndText.text = "WIN";
                gameEndText.gameObject.SetActive(true);
            }
            else {
                gameEndText.text = "LOSE";
                gameEndText.gameObject.SetActive(true);
            }

            yield return new WaitForSeconds(5f);

            PhotonNetwork.LeaveRoom();
            PhotonNetwork.LeaveLobby();
            PhotonNetwork.Disconnect();
            while (PhotonNetwork.IsConnected)
                yield return null;

            Destroy(NetworkManager.instance.gameObject);
            UnityEngine.SceneManagement.SceneManager.LoadScene("WaitingScene");
        }
      
    }
}
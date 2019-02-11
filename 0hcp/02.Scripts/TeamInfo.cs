using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
namespace hcp
{
    public class TeamInfo : MonoBehaviourPun
    {
        [SerializeField]
        int myPhotonViewIDKey;
        public int MyPhotonViewIDKey
        {
            get { return myPhotonViewIDKey; }
        }

        [SerializeField]
        int myTeamLayer;
        public int MyTeamLayer
        {
            get
            {
                return myTeamLayer;
            }
        }

        [SerializeField]
        List<int> enemyTeamLayer = new List<int>();
        public List<int> EnemyTeamLayer
        {
            get { return enemyTeamLayer; }
        }


        [SerializeField]
        List<Hero> enemyHeroes = new List<Hero>();
        public List<Hero> EnemyHeroes
        {
            get
            {
                return enemyHeroes;
            }
        }
        [SerializeField]
        List<Hero> myTeamHeroes = new List<Hero>();
        public List<Hero> MyTeamHeroes
        {
            get
            {
                return myTeamHeroes;
            }
        }
        static TeamInfo _instance = null;
        public static TeamInfo GetInstance()
        {
            return _instance;
        }

        Dictionary<int, Hero> heroPhotonIDDic = new Dictionary<int, Hero>();
        public Dictionary<int, Hero> HeroPhotonIDDic
        {
            get {
                return heroPhotonIDDic;
            }
        }

        public bool isTeamSettingDone=false;

        [SerializeField]
        Color enemyTeamColor;
        [SerializeField]
        Color myTeamColor;
        [SerializeField]
        [Range(0, 0.1f)]
        float outLineWidth;


        private void Awake()
        {
            if (_instance == null)
                _instance = this;
        }

        IEnumerator Start()
        {
            yield return new WaitForSeconds(2f);
            StartCoroutine(WaitForAllHeroBorn());

            NetworkManager.instance.AddListenerOnClientLeft(OnClientLefted);
        }

        void OnClientLefted()
        {
            StartCoroutine(clientLeftCheck());
        }
        System.Action clientLeftAndCheckDone;
        public void AddListenerOnCLCD(System.Action ac)
        {
            clientLeftAndCheckDone += ac;
        }
        IEnumerator clientLeftCheck()
        {
            yield return new WaitForEndOfFrame();

            for (int i = 0; i < myTeamHeroes.Count; i++)
            {
                if (myTeamHeroes[i] == null)
                {
                    myTeamHeroes.RemoveAt(i);
                }
            }
            for (int i = 0; i < enemyHeroes.Count; i++)
            {
                if (enemyHeroes[i] == null)
                {
                    enemyHeroes.RemoveAt(i);
                }
            }
            if (clientLeftAndCheckDone != null)
                clientLeftAndCheckDone();
        }

        Dictionary<int, string> teamInfoDic = new Dictionary<int, string>();

        IEnumerator WaitForAllHeroBorn()
        {
            int heroCounts = 0;
            while (heroCounts != PhotonNetwork.CurrentRoom.PlayerCount)
            {
                heroCounts = GameObject.FindObjectsOfType<Hero>().Length;
                yield return new WaitForSeconds(1f);
            }
            //히어로 전부 생성된 시간임.
            GetTeamInfoFromNetworkManager();
            //내 레이어와 적 레이어 셋팅이 끝남.
            //여기서 영웅의 레이어 셋팅과 영웅 분류를 저장한다.
            //hpBar 셋팅도 끝내.

            Hero[] heroes = GameObject.FindObjectsOfType<Hero>();
            myTeamHeroes.Clear();
            enemyHeroes.Clear();
            heroPhotonIDDic.Clear();
            for (int i = 0; i < heroes.Length; i++)
            {
                heroPhotonIDDic.Add(heroes[i].photonView.ViewID, heroes[i]);
                int heroPhotonID = heroes[i].photonView.ViewID / 1000;  //이 영웅의 포톤뷰 키
             

                int setLayerByNM = LayerMask.NameToLayer( teamInfoDic[heroPhotonID]);   //네트워크 매니저에서 저장되어 넘어온 이 포톤뷰의 팀 설정 (레이어)
                if (setLayerByNM ==  myTeamLayer)
                {
                    myTeamHeroes.Add(heroes[i]);
                    heroes[i].gameObject.layer = myTeamLayer;
                    heroes[i].SetOutLine(outLineWidth, myTeamColor);
                }
                else
                {
                    //내팀이 아니면 일단 적이고
                    enemyHeroes.Add(heroes[i]);
                    heroes[i].gameObject.layer = setLayerByNM;  //저장되어 넘어온 레이어를 영웅에 넣어줌.
                    heroes[i].SetOutLine(outLineWidth, enemyTeamColor);
                }
            }

            //팀세팅이 모두 긑난 시점임.

            for (int i = 0; i < heroes.Length; i++)
            {
                if (heroes[i].hpBar != null)
                {
                    heroes[i].hpBar.SetAsTeamSetting();
                }
                if (heroes[i].photonView.IsMine)
                {
                    Destroy(heroes[i].hpBar.gameObject);
                }
            }

            isTeamSettingDone = true;

            Debug.Log(" 팀인포 : 팀세팅과 관련된 처리 모두 끝.");
        }

        void GetTeamInfoFromNetworkManager()
        {
            teamInfoDic.Clear();

            if (NetworkManager.instance == null)
            {
                return;
            }
            teamInfoDic = NetworkManager.instance .Teams;

            List<int> enemyLayerList = new List<int>();    //자기 팀 외로.
            myPhotonViewIDKey =0;
            Hero[] heroes = GameObject.FindObjectsOfType<Hero>();
            for (int i = 0; i < heroes.Length; i++)
            {
                if (heroes[i].photonView.IsMine)
                {
                    myPhotonViewIDKey = heroes[i].photonView.ViewID / 1000;
                }
            }

            myTeamLayer = LayerMask.NameToLayer(teamInfoDic[myPhotonViewIDKey]);


            Dictionary<int, string>.Enumerator enu = teamInfoDic.GetEnumerator();
            while (enu.MoveNext())
            {
                int photonViewIDKey = enu.Current.Key;
                string layerName = enu.Current.Value;
                int layerMask = LayerMask.NameToLayer(layerName);
                
                    //적 레이어임.
                    if (false == enemyLayerList.Contains(layerMask) && layerMask != myTeamLayer )  //추가된 적 레이어가 아니면.
                    {
                        enemyLayerList.Add(layerMask);
                    }
            }
            enemyTeamLayer = enemyLayerList;
        }

      

        public int EnemyMaskedLayer//에너미가 한 개 이상이면 그에 맞게 마스킹해서 줌.
        {
            get
            {
                int layer = -1;
                for (int i = 0; i < enemyTeamLayer.Count; i++)
                {
                    if (layer == -1)    //맨처음.
                    {
                        layer = 1 << enemyTeamLayer[i];
                    }
                    else
                    {
                        layer = layer | 1 << enemyTeamLayer[i];
                    }
                }
                if (layer == -1)
                {
                 //   Debug.LogError("에너미 레이어에 설정오류 존재.");
                }
                return layer;
            }
        }
        public int MapAndEnemyMaskedLayer//에너미가 한 개 이상이면 그에 맞게 마스킹해서 줌.
        {
            get
            {
                int layer = EnemyMaskedLayer;
                return layer | 1 << Constants.mapLayerMask;
            }
        }
        public bool IsThisLayerEnemy(int layer)
        {
            return enemyTeamLayer.Contains(layer);
        }
        

        public int GetTeamLayerByPhotonViewID(int photonViewID)
        {
            return LayerMask.NameToLayer(teamInfoDic[photonViewID / 1000]);
        }


        /*
        public void SetMyTeamInfo(int myTeamLayer, params Hero[] heroes)
        {
            this.myTeamLayer = myTeamLayer;
            if (heroes == null || heroes.Length ==0)
            {
                Debug.LogError("SetMyTeamInfo 의 히어로 정보가 불충분");
                myTeamHeroes = new Hero[0];
            }
            this.myTeamHeroes = heroes;
        }
        public void SetEnemyTeamInfo(int enemyTeamLayer, params Hero[] heroes)
        {
            this.enemyTeamLayer = enemyTeamLayer;
            if (heroes == null || heroes.Length == 0)
            {
                Debug.LogError("SetEnemyTeamInfo 의 히어로 정보가 불충분");
                enemyHeroes = new Hero[0];
            }
            this.enemyHeroes = heroes;
        }
        */
    }
}
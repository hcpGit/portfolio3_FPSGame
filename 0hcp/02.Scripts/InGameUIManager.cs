using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace hcp
{
    public class InGameUIManager : MonoBehaviour
    {
        [System.Serializable]
        struct touchFingerID
        {
            public int fingerID;
            public Vector3 touchedPos;
            public bool activated;

            public void Activate(int fID)
            {
                activated = true;
                fingerID = fID;

            }
            public void DeActivate()
            {
                activated = false;
            }
            public bool IsThisTouchFID(int fingerId)
            {
                if (fingerID == fingerId)
                    return true;
                else return false;
            }
        }

        [Header("Move Controller")]
        [Tooltip("controller point")]
        [SerializeField]
        GameObject cont;
        [Tooltip("controller range")]
        [SerializeField]
        GameObject contBack;
        [Tooltip("controller max point")]
        [SerializeField]
        GameObject contMax;

        [SerializeField]
        Vector3 charactorMoveV = Vector3.zero;
        
        MoveController moveController;
        [SerializeField]
        touchFingerID moveContTouch;
        
        [Header("Rotate Controller")]
        [Tooltip("controller point")]
        [SerializeField]
        GameObject rcont;
        [Tooltip("controller range")]
        [SerializeField]
        GameObject rcontBack;
        [Tooltip("controller max point")]
        [SerializeField]
        GameObject rcontMax;

        [SerializeField]
        Vector3 charactorRotateV = Vector3.zero;
        
        MoveController rotateController;
        [SerializeField]
        touchFingerID rotateContTouch;
        [Space(10)]
        /*
        [SerializeField]
        Vector3 mouseTouched;//임시로 회전 용으로 사용. 에디터에서 터치를 못 읽어서
        [SerializeField]
        bool contTouched;//임시로 회전 용으로 사용. 에디터에서 터치를 못 읽어서
        */
        [SerializeField]
        Image crossHair;

        [SerializeField]
        Hero targetHero;

        [SerializeField]
        Image hpBarUI;
        float maxHPDiv;
        [SerializeField]
        Text heroName;

        [SerializeField]
        GameObject[] controlPanelPerHero;

        [SerializeField]
        Transform killLogPanel;
        [SerializeField]
        GameObject killLog;

        [SerializeField]
        Image[] heroControlBtnsScreen;

        public Text[] ct;

        static InGameUIManager instance;
        public static InGameUIManager Instance
        {
            get { return instance; }
        }

        private void Awake()
        {
            instance = this;
        }


        // Use this for initialization
        void Start()
        {
            moveController = new MoveController(contBack.transform.position, contMax.transform.position);   //스케일 렌더 모드 캔버스의 실제 ui 포지션을 얻고 싶으면
                                                                                                            //스타트 에서 포지션을 접근해야함.

            rotateController = new MoveController(rcontBack.transform.position, rcontMax.transform.position);

            moveContTouch = new touchFingerID();
            moveContTouch.DeActivate();
            rotateContTouch = new touchFingerID();
            rotateContTouch.DeActivate();
        }

        #region 터치 잘 받나 체크용
        [System.Serializable]
        struct touchCK {
            public int fid;
            public Vector3 pos;
            TouchPhase phase;
            public void TouchAccept(Touch t)
            {
                fid = t.fingerId;
                pos = t.position;
                phase = t.phase;
            }
            public void ReSet()
            {
                fid = -1; pos = Vector3.zero; phase = TouchPhase.Canceled;
            }
        }

        [SerializeField]
        touchCK[] ts = new touchCK[5];
        
        void TestTouches()
        {
            for (int i = 0; i < 5; i++)
            {
                ts[i] = new touchCK();
            }

            for (int i = 0; i < Input.touches.Length; i++)
            {
                ts[i].TouchAccept(Input.touches[i]);
            }

        }

        #endregion

        void Update()
        {
            if (targetHero == null) return;

            for (int i = 0; i < (int)E_ControlParam.MAX; i++)
            {
                ct[i].text = targetHero.GetReUseRemainTime((E_ControlParam)i).ToString();
            }
            for (int i = 0; i < (int)E_ControlParam.MAX; i++)
            {
                heroControlBtnsScreen[
                    ((int)E_ControlParam.MAX* (int)targetHero.HeroType)
                    +i].fillAmount = targetHero.GetReUseRemainTimeByZeroToOne ((E_ControlParam)i);
            }

            hpBarUI.fillAmount = targetHero.CurrHP * maxHPDiv;

            #region 안드로이드  (터치)
#if UNITY_ANDROID

            TestTouches();

            if (Input.touchCount == 0)
            {
                moveContTouch.DeActivate();
                rotateContTouch.DeActivate();
                cont.transform.position = contBack.transform.position;
                charactorMoveV = Vector3.zero;
                rcont.transform.position = rcontBack.transform.position;
                charactorRotateV = Vector3.zero;

                targetHero.MoveHero(charactorMoveV);    //무빙은 계속 쏴줘야함.

                return;
            }

            Touch[] touches = Input.touches;

            for (int i = 0; i < touches.Length; i++)
            {
                Touch touch = touches[i];
                TouchPhase phase = touch.phase;
                int fingerID = touch.fingerId;
                Vector3 touchPos = touch.position;
                
                switch (phase)
                    {
                        case TouchPhase.Began:
                        if (moveController.IsInThisContPos(touchPos))
                        {
                            moveContTouch.Activate(fingerID);
                        }
                        else if (rotateController.IsInThisContPos(touchPos))
                        {
                            rotateContTouch.Activate(fingerID);
                        }
                            break;
                        case TouchPhase.Moved:
                        case TouchPhase.Stationary:
                        if (moveContTouch.activated && moveContTouch.IsThisTouchFID(fingerID))
                        {
                            moveContTouch.touchedPos = touchPos;
                        }
                        if (rotateContTouch.activated && rotateContTouch.IsThisTouchFID(fingerID))
                        {
                            rotateContTouch.touchedPos = touchPos;
                        }

                        break;

                        case TouchPhase.Ended:
                        case TouchPhase.Canceled:
                        if (moveContTouch.IsThisTouchFID(fingerID))
                        {
                            moveContTouch.DeActivate();
                        }
                        if (rotateContTouch.IsThisTouchFID(fingerID))
                        {
                            rotateContTouch.DeActivate();
                        }
                            break;
                    }


                /*
                if (EventSystem.current.IsPointerOverGameObject(touches[i].fingerId) && 
                    touches[i].phase == TouchPhase.Began)   //게임 오브젝트에 올려진 터치의 시작이라면.
                {
                  //  Vector3 worldTouchedPos = Camera.main.ScreenToWorldPoint(touches[i].position);
                    if ( moveController.IsInThisContPos(touches[i].position))// moveContBound.Contains(worldTouchedPos))
                    {
                        //무브 컨트롤러에 처음 올려진 터치.
                        moveContTouch.Activate(touches[i].fingerId);
                    }
                    else if (rotateController.IsInThisContPos(touches[i].position))
                    {
                        //로테이트 컨트롤러에 처음 올려진 터치.
                        rotateContTouch.Activate(touches[i].fingerId);
                    }
                }
                if (touches[i].phase == TouchPhase.Ended)
                {
                    if (moveContTouch.IsThisTouchFID(touches[i].fingerId))
                    {
                        moveContTouch.DeActivate();
                    }
                    else if (rotateContTouch.IsThisTouchFID(touches[i].fingerId))
                    {
                        rotateContTouch.DeActivate();
                    }
                }
                */
            }

            On_MoveContAndroid();
            targetHero.MoveHero(charactorMoveV);

            On_RotateContAndroid();
            targetHero.RotateHero(charactorRotateV);
                //나중에 전사가 필요할듯.
                //targetHero.RotateHero(new Vector3(rotateV.x / Screen.width, rotateV.y / Screen.height, 0));
#endif
            #endregion
            /*
            #region 에디터

            //#if UNITY_EDITOR
            //임시로 하는 것 뿐임.
            targetHero.MoveHero(charactorMoveV);

            if (Input.GetMouseButtonDown(0) && !contTouched)
            {
                if (mouseTouched == Vector3.zero)
                {
                    mouseTouched = Input.mousePosition;
                    return;
                }
            }
            else if (Input.GetMouseButtonUp(0) && !contTouched)
            {
                mouseTouched = Vector3.zero;
            }
            else if (Input.GetMouseButton(0) && !contTouched)
            {

                //  Debug.Log("마우스 이동중2");

                Vector3 mousePos = Input.mousePosition;
                Vector3 rotateV = mousePos - mouseTouched;
                // targetHero.transform.Rotate(new Vector3(0, rotateV.x / Screen.width, 0), Space.Self);
                targetHero.RotateHero(new Vector3(rotateV.x/Screen.width, rotateV.y / Screen.height, 0));
            }
//#endif
#endregion
            */
        }

#region Basic Moving And Rotating

        public void On_MoveContAndroid()
        {
#if UNITY_ANDROID
            if (!moveContTouch.activated)
            {
                cont.transform.position = contBack.transform.position;
                charactorMoveV = Vector3.zero;
                return;
            }

            /*
            bool hasTouch = false;
            for (int i = 0; i < Input.touches.Length; i++)
            {
                if (moveContTouch.fingerID == Input.touches[i].fingerId)
                    hasTouch = true;
            }

            if (!hasTouch)
            {
                moveContTouch.DeActivate();
                Debug.Log("무브 컨트롤러 핑거아이디가 적재되어있지 않음");
                cont.transform.position = contBack.transform.position;
                charactorMoveV = Vector3.zero;
                return;
            }
            

            Touch moveTouch = Input.GetTouch(moveContTouch.fingerID);
           */

            Vector3 touchPos = moveContTouch.touchedPos;
            Vector3 contV;
            Vector3 moveV = ChangexyVector3ToxzVector3( moveController.GetMoveVector(touchPos, out contV));
            cont.transform.position = contV;
            charactorMoveV = moveV;
#endif
        }

        public void On_RotateContAndroid()
        {
#if UNITY_ANDROID
            if (!rotateContTouch.activated)
            {
                rcont.transform.position = rcontBack.transform.position;
                charactorRotateV = Vector3.zero;
                return;
            }

            /*

            bool hasTouch = false;
            for (int i = 0; i < Input.touches.Length; i++)
            {
                if (rotateContTouch.fingerID == Input.touches[i].fingerId)
                    hasTouch = true;
            }

            if (!hasTouch)
            {
                rotateContTouch.DeActivate();
                Debug.Log("로테이트 컨트롤러 핑거아이디가 적재되어있지 않음");
                rcont.transform.position = rcontBack.transform.position;
                charactorRotateV = Vector3.zero;
                return;
            }

            Touch rotateTouch = Input.GetTouch(rotateContTouch.fingerID);
            */
            Vector3 touchPos = rotateContTouch .touchedPos;
            Vector3 contV;
            Vector3 rotateV = rotateController.GetMoveVector(touchPos, out contV);
            rcont.transform.position = contV;
            rotateV.y *= -1;
            charactorRotateV = rotateV;
#endif
        }
        
        Vector3 ChangexyVector3ToxzVector3(Vector3 xyVector3)
        {
            return new Vector3(xyVector3.x, 0, xyVector3.y);
        }

        public void On_MoveCont()  //회전 상관 없이 이동만 관장함.  //터치 전에 쓸떄 이용.
                                   //이제 쉐이더 넣고 해주면 됨.
        {
//#if UNITY_EDITOR
/*
            contTouched = true; //임시 (회전 값 보정 위해.)
            Vector3 contV;
            Vector3 moveV = ChangexyVector3ToxzVector3( moveController.GetMoveVector(Input.mousePosition, out contV));
            cont.transform.position = contV;
            charactorMoveV = moveV;
            */
//#endif
        }
        public void On_MoveStop()
        {
//#if UNITY_EDITOR
/*
            contTouched = false;
            cont.transform.position = contBack.transform.position;
            charactorMoveV = Vector3.zero; 
            */
//#endif
        }
        #endregion

        public void OnClick_NormalAttack()
        {
            targetHero.ControlHero(E_ControlParam.NormalAttack);
        }
        public void OnClick_Reload()
        {
            targetHero.ControlHero(E_ControlParam.Reload);
        }
        public void OnClick_FirstSkill()
        {
            targetHero.ControlHero(E_ControlParam.FirstSkill);
        }
        public void OnClick_Ultimate()
        {
            targetHero.ControlHero(E_ControlParam.Ultimate);
        }
        public void CrossHairChange(Sprite crossHair)
        {
            this.crossHair.sprite = crossHair;
        }
        public void SetTargetHero(Hero hero)
        {
            targetHero = hero;
            CrossHairChange(targetHero.crossHairs[0]);
            maxHPDiv = 1 / targetHero.MaxHP;
            ShowControlPanel(targetHero.HeroType);
            heroName.text = targetHero.PlayerName;
        }
        void ShowControlPanel(E_HeroType heroType)
        {
            controlPanelPerHero[(int)heroType].SetActive(true);
        }

        public void ShowKillLog(string killerName, E_HeroType killerHeroType, string victimName, E_HeroType victimHeroType)
        {
            GameObject temp =  GameObject.Instantiate(killLog, killLogPanel);
            temp.GetComponent<KillLog>().SetKillLog(killerName, killerHeroType, victimName, victimHeroType);
        }
    }
}
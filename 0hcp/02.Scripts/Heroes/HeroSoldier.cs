using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon;
using Photon.Pun;

namespace hcp
{
    public class HeroSoldier : Hero
    {
        [Space(20)]
        [Header("Hero - Soldier's Property")]
        [Space(10)]

        [SerializeField]
        E_MoveDir lastDir = E_MoveDir.NONE;
        [SerializeField]
        Transform firePos;

        [SerializeField]
        HSHealDrone healDrone;

        [Space(10)]
        [Header("   Hero-Soldier-NormalAttack")]
        [Space(10)]

        [SerializeField]
        ParticleSystem normalMuzzleFlash;
        [SerializeField]
        GameObject normalAttackParticleParent;
        [Tooltip("Hero- Soldier normal Attack particle, Object Pooling.")]
        [SerializeField]
        ParticleSystem[] normalAttackParticles;

        WaitForSeconds normalParticleDuration;

        [SerializeField]
        float normalFireDamage;
        [Tooltip("fireRate for normal attack")]
        [SerializeField]
        float fireRate;
        [SerializeField]
        int currBullet;
        [SerializeField]
        int maxBullet;

        [SerializeField]
        float correctionRange ;
        float correctionRangeSqr;

        [SerializeField]
        float correctionMaxLength ;
        float correctionMaxLengthSqr;

        [SerializeField]
        GameObject HSMagazineUIPrefab;
        [SerializeField]
        GameObject HSBulletImage;
        [SerializeField]
        GameObject HSBulletImageBack;
        [SerializeField]
        GameObject HSMagazineUI;
        [SerializeField]
        Transform HSMagazineUICurrBulletImageParent;
        [SerializeField]
        UnityEngine.UI.Text currBulletUIText;

        [Space(10)]
        [Header("   Hero-Soldier-Reload")]
        [Space(10)]

        [SerializeField]
        bool reloading = false;

        [SerializeField]
        AnimationClip reloadClip;

        [Space(10)]
        [Header("   Hero-Soldier-First Skill Heal Drone")]
        [Space(10)]
        [SerializeField]
        float healDroneCoolTime;



        [Space(10)]
        [Header("   Hero-Soldier-Ultimate")]
        [Space(10)]

        [SerializeField]
        GameObject ultMissileParent;
        [Tooltip("Hero- Soldier Ultimate, Object Pooling.")]
        [SerializeField]
        HSUltMissile[] ultMissiles;

        [Tooltip("Hero- Soldier Ultimate, max Missile Counts.")]
        [SerializeField]
        int ultMissilesMaxCount ;

        [SerializeField]
        int ultShootCount = 0;

        [Tooltip("Hero- Soldier Ultimate, max Missile Shot Time.")]
        [SerializeField]
        float ultMaxTime ;
        float ultMaxTimeDiv;

        [Tooltip("Hero- Soldier Ultimate, fire rate.")]
        [SerializeField]
        float ultFireRate;
        
        [SerializeField]
        bool isUltOn = false;

        [SerializeField]
        float ultActivateTime = 0f;

        [SerializeField]
        GameObject HSUltCrossHairPrefab;
        [SerializeField]
        GameObject HSUltCrossHair;
        [SerializeField]
        UnityEngine.UI.Image HSUltHalfCircleImage;


        private void Start()
        {
            ultMissiles = new HSUltMissile[ultMissileParent.transform.childCount];
            for (int i = 0; i < ultMissiles.Length; i++)
            {
                ultMissiles[i] = ultMissileParent.transform.GetChild(i).GetComponent<HSUltMissile>();
                ultMissiles[i].DeActivate();
                ultMissiles[i].attachedNumber = i;
            }
            
            ultMissileParent.transform.SetParent(null);//미사일 관리만을 위한 애니까.
            ultMissileParent.transform.position = Vector3.zero + Vector3.down * 5f; //원점의 바닥 밑으로 숨겨버림.

            if (photonView.IsMine)
            {
                HSMagazineUI = GameObject.Instantiate(HSMagazineUIPrefab, 
                    Vector3.zero,
                   // new Vector3(-1 * (Screen.width / 4), -1 * (Screen.height / 4), 0), 
                    Quaternion.identity, InGameUIManager.Instance.transform);
                HSMagazineUICurrBulletImageParent = HSMagazineUI.transform.GetChild(1);
                for (int i = 0; i < maxBullet; i++)
                {
                    GameObject.Instantiate(HSBulletImage, HSMagazineUICurrBulletImageParent);
                    GameObject.Instantiate(HSBulletImageBack, HSMagazineUI.transform.GetChild(0));
                }

               
                HSMagazineUI.transform.GetChild(2).GetComponent<UnityEngine.UI.Text>().text = "/"+maxBullet.ToString();
                currBulletUIText = HSMagazineUI.transform.GetChild(3).GetComponent<UnityEngine.UI.Text>() ;
                currBulletUIText.text = currBullet.ToString();
            }
        }

        protected override void Awake()
        {
            ultMaxTimeDiv = 1 / ultMaxTime;
            heroType = E_HeroType.Soldier;
            correctionMaxLengthSqr = correctionMaxLength * correctionMaxLength;
            correctionRangeSqr = correctionRange * correctionRange;
            
            base.Awake();
            

            normalAttackParticles = new ParticleSystem[normalAttackParticleParent.transform.childCount];

            for (int i = 0; i < normalAttackParticles.Length; i++)
            {
                normalAttackParticles[i] = normalAttackParticleParent.transform.GetChild(i).GetComponent<ParticleSystem>();
                normalAttackParticles[i].gameObject.SetActive(false);
            }
            normalParticleDuration = new WaitForSeconds(normalAttackParticles[0].main.duration);
            normalAttackParticleParent.transform.SetParent(null);//파티클 관리만을 위한 애니까.
            normalAttackParticleParent.transform.position = Vector3.zero + Vector3.down * 5f; //원점의 바닥 밑으로 숨겨버림.
            
            currHP = maxHP;
            currBullet = maxBullet;
            nowUltAmount = 0f;
        }
        private void OnDestroy()
        {
            if (ultMissileParent != null)
                Destroy(ultMissileParent.gameObject);

            if (normalAttackParticleParent != null)
                Destroy(normalAttackParticleParent.gameObject);
        }

        protected override void SetActiveCtrls()//어웨이크 시 불러온다든지... 스킬들 세팅해주는 함수임.
        {
            base.SetActiveCtrls();
            activeCtrlDic.Add(E_ControlParam.NormalAttack, new DelegateCtrl(E_ControlParam.NormalAttack, fireRate, NormalAttack,
               NormalAttackMeetCondition));
            activeCtrlDic.Add(E_ControlParam.FirstSkill, new DelegateCtrl(E_ControlParam.FirstSkill, healDroneCoolTime, FirstSkill_HealDrone, () => { return true; }));
            activeCtrlDic.Add(E_ControlParam.Reload, new DelegateCtrl(E_ControlParam.Reload, 1f, Reloading, ReloadingMeetCondition));
            activeCtrlDic.Add(E_ControlParam.Ultimate, new DelegateCtrl(E_ControlParam.Ultimate, ultFireRate, Ult_ShotMissile, () => { return true; }));
        }

        #region basic control 
        public override void MoveHero(Vector3 moveV)
        {
            if (!photonView.IsMine || IsCannotMoveState() || IsDie)
            {
                Debug.Log("무브히어로가 묵살되었음." + moveV + "포톤이 내것인지? = " + photonView.IsMine);
                return;
            }

            base.MoveHero(moveV);
            

            E_MoveDir dir = GetMostMoveDir(moveV);

            if (lastDir.Equals(dir)) return;
            lastDir = dir;
            anim.SetBool("forward", false);
            anim.SetBool("backward", false);
            anim.SetBool("left", false);
            anim.SetBool("right", false);
            anim.SetBool("idle", false);

            switch (dir)
            {
                case E_MoveDir.Forward:
                    anim.SetBool("forward", true);
                    break;
                case E_MoveDir.Backward:
                    anim.SetBool("backward", true);
                    break;
                case E_MoveDir.Left:
                    anim.SetBool("left", true);
                    break;
                case E_MoveDir.Right:
                    anim.SetBool("right", true);
                    break;
                case E_MoveDir.NONE:
                    anim.SetBool("idle", true);
                    break;
            }
        }

        public override void RotateHero(Vector3 rotateV)
        {
            if (!photonView.IsMine || IsCannotMoveState() || IsDie)
            {
                return;
            }
            base.RotateHero(rotateV);
        }

        public override void ControlHero(E_ControlParam param)
        {
            if (!photonView.IsMine || IsCannotActiveState() || IsDie || reloading)
            {
                return;
            }

            if (param == E_ControlParam.Ultimate)
            {
                if (UltAmountPercent < 1 && !isUltOn)
                    return;

                if (isUltOn)
                {
                    if (!activeCtrlDic[E_ControlParam.Ultimate].IsCoolTimeOver() || ultShootCount >= ultMissilesMaxCount)
                        return;

                    //궁 유지중인 부분.
                    activeCtrlDic[param].Activate();

                    if (HSUltCrossHair != null)
                    {
                        Transform ultMagazine = HSUltCrossHair.transform.GetChild(0);
                        for (int i = 0; i < ultShootCount; i++)
                        {
                            ultMagazine.GetChild(i).gameObject.SetActive(false);
                        }
                    }

                    return;
                }
                else
                {
                    InGameUIManager.Instance.CrossHairChange(crossHairs[1]);
                    //궁 처음 쏘는 초기화 부분.
                    nowUltAmount = 0f;
                    isUltOn = true;
                    ultShootCount = 0;
                    ultActivateTime = 0f;
                    activeCtrlDic[param].Activate();

                    HSUltCrossHair = GameObject.Instantiate(HSUltCrossHairPrefab, new Vector3(Screen.width/2,Screen.height/2,0),Quaternion.identity, InGameUIManager.Instance.transform);
                    HSUltHalfCircleImage = HSUltCrossHair.GetComponent<UnityEngine.UI.Image>();
                    Transform ultMagazine = HSUltCrossHair.transform.GetChild(0);
                    for (int i = 0; i < ultShootCount; i++)
                    {
                        ultMagazine.GetChild(i).gameObject.SetActive(false);
                    }

                    return;
                }
            }


            if (!activeCtrlDic[param].IsCoolTimeOver())
                return;
            Debug.Log(param + "입력 - 쿨타임 검사통과");

            activeCtrlDic[param].Activate();
        }

        #endregion


        #region Normal Attack
        bool NormalAttackMeetCondition()
        {
            if (isUltOn) return false;
            if (currBullet <= 0)
            {
                Reloading();
                return false;
            }
            return true;
        }
        void NormalAttack()
        {
            currBullet--;
            for (int i = 0; i < maxBullet- currBullet; i++)
            {
                HSMagazineUICurrBulletImageParent.transform.GetChild(i).gameObject.SetActive(false);
            }
            currBulletUIText.text = currBullet.ToString();

            photonView.RPC("normalMuzzleFlashPlay", RpcTarget.Others);

            FPSCamPerHero.FPSCamAct (E_ControlParam.NormalAttack);// 나자신의 시각효과만 담당.
                                          // normalMuzzleFlash.Play();   //fps 카메라라서 다른 곳의 파티클을 뿜어줘야함.
            anim.SetTrigger("shot");

            Camera camera = Camera.main;

            Ray screenCenterRay = camera.ScreenPointToRay(screenCenterPoint);
            RaycastHit hitInfo;
            bool rayMapHit = false;
            float rayHitDisSqr = 0f;
            Vector3 hitCorrectionEnemyPos = Vector3.zero;

            #region 일반 공격 직접 레이

            if (Physics.Raycast(screenCenterRay, out hitInfo, maxShotLength,TeamInfo.GetInstance().MapAndEnemyMaskedLayer))
            {
                Debug.DrawLine(screenCenterRay.origin, screenCenterRay.direction * maxShotLength + screenCenterRay.origin, Color.blue, 1f);
                Debug.DrawRay(screenCenterRay.origin, screenCenterRay.direction, Color.magenta, 1f);
                Constants.DebugLayerMask(TeamInfo.GetInstance().MapAndEnemyMaskedLayer);

                GameObject hit = hitInfo.collider.gameObject;
                //레이캐스트 쏴서 뭐 맞았음. 벽이나, 적팀이나 이런 것을 검출.
                Vector3 hitPos = hitInfo.point;

                if (TeamInfo.GetInstance().IsThisLayerEnemy(hit.layer))
                {
                    Debug.Log("솔져 직접 레이 쏴서 적이 맞았음." + hitInfo + "레이 당첨 위치 = " + hitPos);
                    photonView.RPC("normalHitParticle", RpcTarget.All, hitPos);  //맞는 효과.
                    Hero hitEnemyHero = hit.GetComponent<Hero>();
                    if (hitEnemyHero == null)
                    {
                        Debug.Log("HS-NormalAttack 히어로 컴포넌트가 없음");
                        return;
                    }
                    float damage = normalFireDamage;
                    if (hitEnemyHero.IsHeadShot(hitPos))
                    {
                        Debug.Log("HS-NormalAttack 헤드샷.");
                        damage *= 2f;
                    }
                    hitEnemyHero.photonView.RPC("GetDamaged", RpcTarget.All, damage,photonView.ViewID);

                    return;
                }
                if (hit.layer.Equals(Constants.mapLayerMask))
                {
                    rayMapHit = true;
                    rayHitDisSqr = (hitPos - screenCenterRay.origin).sqrMagnitude;
                    hitCorrectionEnemyPos = hitPos;
                }
                else
                {
                    //맵에 맞은것도 아니고 적에 맞은 것도 아니고. 있을 수 없는 일임.
                    return;
                }
            }

            #endregion

            #region 일반 공격 보정 (직접 레이에서 맵에 피격된 경우)

            Debug.Log("솔져 노멀어택이 맵에 피격, 보정 연산 진입.");

            Vector3 shotVector = screenCenterRay.direction * maxShotLength;
            Hero hitEnemy = null;
            float minHitLength = 0f;

            List<Hero> enemyHeroes = TeamInfo.GetInstance().EnemyHeroes;

            for (int i = 0; i < enemyHeroes.Count; i++)
            {
                Vector3 enemyPosition = enemyHeroes[i].CenterPos - screenCenterRay.origin;
                Debug.Log(enemyHeroes[i].photonView.ViewID + "의 센터 포스, " + enemyHeroes[i].CenterPos + "샷 원점에서 부터 포지션" + enemyPosition);

                Debug.DrawLine(screenCenterRay.origin, screenCenterRay.origin + enemyPosition , Color.red,4f);

                float enemyScreenCenterDot = Vector3.Dot(enemyPosition, shotVector);

                if (enemyScreenCenterDot < Mathf.Epsilon)   //벡터가 마이너스 , 뒤에 있음
                {
                    Debug.Log("뒤에 있으므로 보정 연산제외");
                    continue;
                }
                float projectedEnemyDis = enemyScreenCenterDot * maxShotLengthDiv;  //카메라에서 적 까지의 샷벡터에 투영된 길이.
                if (projectedEnemyDis > correctionMaxLength)
                {
                    //보점 최대 길이 보다 못하면 연산 제외.
                    Debug.Log("보정 최대 길이에 도달하지 못하므로 보정연산제외. 샷벡터 투영길이 = " + projectedEnemyDis + "직선 보정 최대 길이 = " + correctionMaxLength);
                    continue;
                }
                float projectedEnemyDisSqr = projectedEnemyDis * projectedEnemyDis;

                

                if (rayMapHit && rayHitDisSqr < projectedEnemyDisSqr)
                //처음 레이를 쏜게 벽이었는데. 그 벽 까지의 거리 보다도 먼 적이니까.
                //연산의 대상이 아님.
                {
                    Debug.Log("앞에 벽이 있으므로 연산 제외. 샷벡터 투영길이 제곱 = " + projectedEnemyDisSqr + "직접레이가 벽에 맞았던 길이 제곱 = " + rayHitDisSqr);
                    continue;
                }

                Debug.DrawLine(screenCenterRay.origin, screenCenterRay.origin + screenCenterRay.direction * projectedEnemyDis, Color.white, 4f);

                float farFromShotVectorSqr = enemyPosition.sqrMagnitude - projectedEnemyDisSqr;//샷벡터 투영 점에서 적까지의 수직 거리.

                Debug.DrawLine( screenCenterRay.origin + screenCenterRay.direction * projectedEnemyDis
                    ,
                     (screenCenterRay.origin + screenCenterRay.direction * projectedEnemyDis)
                     +
                    ( enemyHeroes[i].CenterPos -  (screenCenterRay.origin + screenCenterRay.direction * projectedEnemyDis) ).normalized
                    * Mathf.Sqrt( farFromShotVectorSqr)
                     , Color.magenta, 4f);

                Debug.DrawLine(
                    screenCenterRay.origin + screenCenterRay.direction * projectedEnemyDis
                    
                    ,
                     (screenCenterRay.origin + screenCenterRay.direction * projectedEnemyDis )
                     +
                    (enemyHeroes[i].CenterPos - (screenCenterRay.origin + screenCenterRay.direction * projectedEnemyDis)).normalized
                    * correctionRange
                     , 
                     
                     Color.green, 4f);


                if (farFromShotVectorSqr > correctionRangeSqr)  //보정 범위 밖
                {
                    Debug.Log("보정 길이 벗어남으로 인해 보정 연산 제외. 보정에 쓰인 적 위치 길이 제곱 = " + farFromShotVectorSqr +
                        "최대 보정 넓이 반지름 제곱 = " + correctionRangeSqr);
                    continue;
                }
                //보정으로 히트 된 적임.

               

                //벽 등에 가려졌는지 레이를 한번 더쏴야하나...????

                if (Physics.Raycast(screenCenterRay.origin, enemyPosition, enemyPosition.magnitude, 1 << Constants.mapLayerMask))
                {
                    Debug.Log(enemyHeroes[i].photonView.ViewID + "가 솔져의 보정 노멀 공격 판정 받았으나 중간에 벽이 있어서 취소.");
                    continue;
                }

                Debug.Log(enemyHeroes[i].photonView.ViewID + "가 보정으로 인해 솔져의 노멀 공격 어택으로 판정이 일단 됨.");

                //적에 피격된 경우.
                Hero hitHero = enemyHeroes[i];
                if (hitEnemy == null)
                {
                    hitEnemy = hitHero;
                    minHitLength = enemyPosition.sqrMagnitude;
                    hitCorrectionEnemyPos = hitHero.CenterPos;
                }
                else if (minHitLength > enemyPosition.sqrMagnitude)
                {
                    hitEnemy = hitHero;
                    minHitLength = enemyPosition.sqrMagnitude;
                    hitCorrectionEnemyPos = hitHero.CenterPos;
                }
            }

            photonView.RPC("normalHitParticle", RpcTarget.All, hitCorrectionEnemyPos);  //벽에 맞았든 어쨌든 결국 이 포인트가 최종적으로 맞은 포인트임.
            if (hitEnemy == null)
            {
                //보정에서도 피격된 놈이 없음.
                return;
            }
            hitEnemy.photonView.RPC("GetDamaged", RpcTarget.All, normalFireDamage, photonView.ViewID);
            #endregion
        }

        [PunRPC]
        void normalMuzzleFlashPlay()
        {
            normalMuzzleFlash.Play();
        }
        [PunRPC]
        void normalHitParticle(Vector3 hitPos)
        {
            StartCoroutine(NormalAttackParticlePlay(hitPos));
        }
        IEnumerator NormalAttackParticlePlay(Vector3 hitPos)
        {
            ParticleSystem temp = null;
            for (int i = 0; i < normalAttackParticles.Length; i++)
            {
                if (normalAttackParticles[i].gameObject.activeSelf == false)
                {
                    temp = normalAttackParticles[i];
                    break;
                }
            }
            if (temp != null)
            {
                temp.gameObject.SetActive(true);
                temp.transform.position = hitPos;
                temp.Play();
                yield return normalParticleDuration;
                temp.gameObject.SetActive(false);
            }
        }

        #endregion
        
        #region First Skill HealDrone

        void FirstSkill_HealDrone() //주위를 힐 하는 힐 드론 소환. 얘는 애니메이션 동기화 해줄 필요 없음
        {
            if (!photonView.IsMine) return;

            photonView.RPC("DroneAppear", RpcTarget.All);

            healDrone.Activate();
        }

        [PunRPC]
        void DroneAppear()
        {
            healDrone.Appear();
        }
        [PunRPC]
        public void DroneDisAppear()
        {
            healDrone.DisAppear();
        }
       
        public void DroneHeal(Hero healedHero, float healAmount)
        {
            healedHero.photonView.RPC("GetHealed",RpcTarget.All,  healAmount);
        }



        #endregion

        #region Reloading

        bool ReloadingMeetCondition()
        {
            if (isUltOn) return false;
            if (currBullet == maxBullet) return false;
            return true;
        }

        void Reloading()
        {
            /*
            if (isUltOn) return;
            if (currBullet == maxBullet) return;
            */
            reloading = true; //리로드 애니메이션 후 풀어주기.
                              //리로드 애니메이션 해주기.
            FPSCamPerHero.FPSCamAct(E_ControlParam.Reload);

            StartCoroutine(ReloadingCheck());
            currBullet = maxBullet;


            for (int i = 0; i < maxBullet; i++)
            {
                HSMagazineUICurrBulletImageParent.transform.GetChild(i).gameObject.SetActive(true);
            }
            currBulletUIText.text = currBullet.ToString();
        }

        IEnumerator ReloadingCheck()
        {
            anim.SetTrigger("reload");
            /*
            float time = 0f;
            while (time < reloadClip.length)
            {
                Debug.Log("리로딩 기다리는중" + time);
                time += 0.3f;
                yield return new WaitForSeconds(0.3f);
            }*/
            yield return new WaitForSeconds(reloadClip.length);
            /*

          //  yield return new WaitForSeconds(0.1f);
            Debug.Log(anim.GetCurrentAnimatorStateInfo(0).length +"   "+ anim.GetCurrentAnimatorStateInfo(0).normalizedTime);

           Debug.Log("리로드 이름 맞음?"+anim.GetCurrentAnimatorStateInfo(0).IsName("Reload") );
          //  Debug.Log("리로드 이름 맞음?" + anim.GetCurrentAnimatorStateInfo(0).IsName("Reload"));
            while (
              //  anim.GetCurrentAnimatorStateInfo(0).IsName("Reload") && 
                anim.GetCurrentAnimatorStateInfo(0).normalizedTime < 1f)
            {
                Debug.Log("리로딩 애니메이션 체크중");

                yield return new WaitForSeconds(0.3f);
            }
            */
            reloading = false;
        }
        #endregion

        #region Ultimate

        void Ult_ShotMissile()
        {
            Vector3 startPos = firePos.position;

            Ray screenCenterRay = Camera.main.ScreenPointToRay(screenCenterPoint);

            Vector3 targetVector;

            targetVector = screenCenterRay.direction * maxShotLength;

            // Quaternion dir = Quaternion.FromToRotation(startPos, targetVector); //파이어 포스에서 중앙 스크린 포인트 까지 방향

            Quaternion dir = Quaternion.LookRotation((targetVector - startPos)); //파이어 포스에서 중앙 스크린 포인트 까지 방향
                                                                                 //오일러는 말그대로 그 벡터 성분 대로 축 회전을 하는 거고
                                                                                 //원하는 어딘가를 보는 방향을 얻으려면 이렇게 룩 로테이션을 써야함.

            Debug.LogFormat("스타트포지션 = {0} , 스크린 중앙{1}으로 찍힌 월드 포지션= {2} , 방향 = {3}", startPos, screenCenterPoint, targetVector, dir);
            Debug.DrawLine(startPos, targetVector, Color.blue, 5f);

            photonView.RPC("ShootUltimate", RpcTarget.All, ultShootCount, firePos.position, dir);
            ultShootCount++;
            //궁로직.
        }

        [PunRPC]
        void ShootUltimate(int num, Vector3 shootStartPos, Quaternion shootStartRot)
        {
            ultMissiles[num].Activate(shootStartPos, shootStartRot);
        }
        [PunRPC]
        void BoomUltMissile(int num, Vector3 boomedPos)
        {
            ultMissiles[num].Boom(boomedPos);
        }

        #endregion

        public override float GetReUseRemainTimeByZeroToOne(E_ControlParam param)
        {
            if (param == E_ControlParam.Ultimate)
            {
                if (isUltOn)
                {
                    return activeCtrlDic[param].ReUseRemainingTimeInAZeroToOne;
                    ;
                }
                else {
                    return 1- UltAmountPercent;
                }
            }
            if (isUltOn&& param!=E_ControlParam.FirstSkill)
            {
                return 1;
            }
            return activeCtrlDic[param].ReUseRemainingTimeInAZeroToOne;
            ;
        }

        private void Update()
        {

            if (!photonView.IsMine) return;

            if (!isUltOn)
            {
                PlusUltAmount(ultPlusPerSec * Time.deltaTime);//1초에 100씩 차게.
            }
            else
            {
                
                ultActivateTime += Time.deltaTime;

                if (HSUltCrossHair != null && HSUltHalfCircleImage != null)
                {
                    HSUltHalfCircleImage.fillAmount=(1 - (ultActivateTime * ultMaxTimeDiv))*0.5f;  //0.5는 그냥 이미지가 반쪽짜리 이미지라서 그럴 뿐임.
                }

                if (ultActivateTime > ultMaxTime || ultShootCount >= ultMissilesMaxCount)
                {
                    isUltOn = false;    //궁 종료 시점.

                    InGameUIManager.Instance.CrossHairChange(crossHairs[0]);
                    if (HSUltCrossHair != null)
                    {
                        Destroy(HSUltCrossHair);
                    }
                }
            }
        }
    }
}
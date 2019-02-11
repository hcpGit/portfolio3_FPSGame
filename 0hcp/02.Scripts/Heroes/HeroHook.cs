using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon;
using Photon.Pun;

namespace hcp
{
    public class HeroHook : Hero
    {
        enum E_HeroHookState
        {
            Idle,
            Hooking,
            Ultimate,
            MAX
        }

        [Space(20)]
        [Header("Hero - Hook's Property")]
        [Space(10)]
        [SerializeField]
        E_HeroHookState state = E_HeroHookState.Idle;
        [SerializeField]
        float normalAttackLength ;
        float normalAttackLengthDiv;
        [SerializeField]
        float correctionRange;
        float correctionRangeSqr;
        [SerializeField]
        float normalAttackDamage ;
        [SerializeField]
        float normalAttackFireRate;

        [Space(10)]
        [Header("   Hero - Hook - First Skill Hook")]
        [SerializeField]
        HHHook hookProjectile;
        [SerializeField]
        Transform hookOriginPos;
        [SerializeField]
        float hookFireRate;

        [Space(10)]
        [Header("   Hero - Hook - Ultimate")]
        [SerializeField]
        GameObject ultParent;
        [SerializeField]
        HHUltWolves ult;
        [SerializeField]
        float ultStartPosFactor;
        
        protected override void Awake()
        {
            heroType = E_HeroType.Hook;
            normalAttackLengthDiv = 1 / normalAttackLength;
            correctionRangeSqr = correctionRange * correctionRange;

            ult = ultParent.GetComponentInChildren<HHUltWolves>();
            currHP = maxHP;
            
            nowUltAmount = 0f;

            base.Awake();
        }
        private void Start()
        {
            ultParent.transform.position = Vector3.zero;
            if (ultParent.transform.parent != null)
                ultParent.transform.parent = null;
        }
        
        protected override void SetActiveCtrls()
        {
            base.SetActiveCtrls();
            activeCtrlDic.Add(E_ControlParam.NormalAttack, new DelegateCtrl(E_ControlParam.NormalAttack, normalAttackFireRate, NormalAttack,
              NormalAttackMeetCondition));
            activeCtrlDic.Add(E_ControlParam.FirstSkill, new DelegateCtrl(E_ControlParam.FirstSkill, hookFireRate, DoHook, HookMeetCondition));
            activeCtrlDic.Add(E_ControlParam.Reload, new DelegateCtrl(E_ControlParam.Reload, 1f, Reload, ()=> { return true; }));
            activeCtrlDic.Add(E_ControlParam.Ultimate, new DelegateCtrl(E_ControlParam.Ultimate, 1f, HHUlt, UltMeetCondition));
        }

        #region Basic Control
        public override void MoveHero(Vector3 moveV)
        {
            if (!photonView.IsMine || IsCannotMoveState() || IsDie)
            {
                Debug.Log("무브히어로가 묵살되었음." + moveV + "포톤이 내것인지? = " + photonView.IsMine);
                return;
            }

            base.MoveHero(moveV);


            if (GetMostMoveDir(moveV) == E_MoveDir.NONE)
            {
                anim.SetBool("walk",false);
            }
            else{
                anim.SetBool("walk", true);
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
            if (!photonView.IsMine || IsCannotActiveState() || IsDie )
            {
                return;
            }

            if (param == E_ControlParam.Ultimate)
            {
                if (UltAmountPercent < 1) return;
            }
            
            if (!activeCtrlDic[param].IsCoolTimeOver())
                return;
            Debug.Log(param + "입력 - 쿨타임 검사통과");

            activeCtrlDic[param].Activate();
        }
        
        #endregion

        #region NormalAttack
        bool NormalAttackMeetCondition()
        {
            return true;
        }
        void NormalAttack()
        {
            anim.SetTrigger("normalAttack");
            FPSCamPerHero.FPSCamAct(E_ControlParam.NormalAttack);

            List<Hero> enemyHeroes = TeamInfo.GetInstance().EnemyHeroes;
            Ray ray = Camera.main.ScreenPointToRay(screenCenterPoint);
            Vector3 normalAttackVector = ray.direction * normalAttackLength;

            for (int i = 0; i < enemyHeroes.Count; i++)
            {
                Hero enemy = enemyHeroes[i];
                Vector3 enemyPosition = enemy.CenterPos - ray.origin;

                Debug.DrawLine(ray.origin,
                        ray.origin + normalAttackVector,
                        Color.blue,
                        3f
                        );
                    Debug.DrawLine(ray.origin,
                        ray.origin + enemyPosition,
                        Color.red,
                        3f
                        );

                float dot = Vector3.Dot(enemyPosition, normalAttackVector);
                if (dot < Mathf.Epsilon)
                {
                    Debug.Log(enemy.photonView.ViewID+ "HH NA enemy Behind, no attack");
                    continue;
                }
                float projectedDis = dot * normalAttackLengthDiv;

                Debug.DrawLine(
                    ray.origin + Vector3.right*0.1f,

                    ray.origin + Vector3.right * 0.1f + ray.direction * projectedDis,
                    Color.white, 3f
                    );
                Debug.DrawLine(
                    ray.origin+Vector3.left*0.1f,

                    ray.origin + Vector3.left * 0.1f + ray.direction * normalAttackLength,
                    Color.green, 3f
                    );


                if (projectedDis > normalAttackLength)
                {
                    Debug.Log(enemy.photonView.ViewID + "HH NA enemy too far, no attack");
                    continue;
                }
                float projectedDisSqr = projectedDis * projectedDis;
                float orthogonalDisSqr = enemyPosition.sqrMagnitude - projectedDisSqr;

                Debug.DrawLine(
                    ray.origin + ray.direction * projectedDis,
                    ray.origin + ray.direction * projectedDis +
                    (enemy.CenterPos - (ray.origin + ray.direction * projectedDis))
                    .normalized
                    *Mathf.Sqrt (orthogonalDisSqr),
                    Color.magenta, 3f
                    );

                Debug.DrawLine(
                    ray.origin + ray.direction * projectedDis + ray.direction * 0.1f,
                    ray.origin + ray.direction * projectedDis + ray.direction * 0.1f +
                    (enemy.CenterPos + ray.direction * 0.1f - (ray.origin + ray.direction * projectedDis + ray.direction * 0.1f))
                    .normalized
                    * Mathf.Sqrt(correctionRangeSqr),
                   Color.green, 3f
                   );



                if (orthogonalDisSqr > correctionRangeSqr)
                {
                    Debug.Log(enemy.photonView.ViewID + "HH NA enemy orthogonalDis too far, no attack");
                    continue;
                }

              


                enemy.photonView.RPC("GetDamaged", Photon.Pun.RpcTarget.All, normalAttackDamage,photonView.ViewID);
            }
        }
        #endregion

        #region Reload

        void Reload()
        {

        }

        #endregion

        #region FirstSkill - Hook
        bool HookMeetCondition()
        {
            return true;
        }
        void DoHook()
        {
            anim.SetTrigger("hook");
            state = E_HeroHookState.Hooking;
            //자가 정지 시키기.
            Ray ray = Camera.main.ScreenPointToRay(screenCenterPoint);
            Vector3 hookDestPos = ray.origin + ray.direction * maxShotLength;
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, maxShotLength, TeamInfo.GetInstance().MapAndEnemyMaskedLayer))
            {
                hookDestPos = hit.point;
            }

            hookOriginPos.transform.LookAt(hookDestPos);
            photonView.RPC("ActivateHook", RpcTarget.All , hookOriginPos.transform.rotation);
        }
        [PunRPC]
        public void ActivateHook(Quaternion originPosRot)
        {
            hookOriginPos.transform.rotation = originPosRot;
            //후킹은 자가 정지 후에 발동하는 로직이니까 훅의 위치 동기화 까지는 필요 없을듯
            hookProjectile.Activate();
        }
        [PunRPC]
        public void HookRetrieve()
        {
            //후킹 실패로 훅이 돌아오는 타이밍일뿐
            hookProjectile.Retrieve();
        }
        [PunRPC]    //후크 쪽에서 알아서 제자리로 돌아오면 이 알피씨를 쏨.
        public void HookIsDone()
        {
            anim.SetTrigger("hookIsDone");
            hookProjectile.DeActivate();
            state = E_HeroHookState.Idle;
        }

        #endregion

        #region Ultimate

        bool UltMeetCondition()
        {
            return true;
        }

        void HHUlt()
        {
            if (!photonView.IsMine) return;
            anim.SetTrigger("ult");
            FPSCamPerHero.FPSCamAct(E_ControlParam.Ultimate);


            Ray ray = Camera.main.ScreenPointToRay (screenCenterPoint);
            Vector3 ultStartPos = ray.origin + ray.direction * ultStartPosFactor;
            Quaternion ultStartRot = Quaternion.LookRotation(ray.direction);

            photonView.RPC("HHHUltActivate", RpcTarget.All, ultStartPos, ultStartRot);
            state = E_HeroHookState.Hooking;
            nowUltAmount = 0f;
            StartCoroutine(ultActionDone());
        }
        IEnumerator ultActionDone()
        {
            yield return new WaitForSeconds(3f);
            
            state = E_HeroHookState.Idle;
        }

        [PunRPC]
        public void HHHUltActivate(Vector3 ultStartPos, Quaternion ultStartRot)
        {
            ult.Activate(ultStartPos, ultStartRot);
        }

        [PunRPC]
        public void HHHUltDeActivate()
        {
            ult.DeActivate();
        }
        
        #endregion


        public override bool IsCannotMoveState()
        {
            if (state == E_HeroHookState.Hooking)
            {
                return true;
            }
           return base.IsCannotMoveState();
        }
        public override bool IsCannotActiveState()
        {
            if (state == E_HeroHookState.Hooking)
            {
                return true;
            }
            return base.IsCannotActiveState();
        }
        private void Update()
        {
            if (!photonView.IsMine) return;
            
                PlusUltAmount(ultPlusPerSec * Time.deltaTime);
        }
        public override float GetReUseRemainTimeByZeroToOne(E_ControlParam param)
        {
            if (param == E_ControlParam.Ultimate)
            {
                    return 1 - UltAmountPercent;
            }
            return activeCtrlDic[param].ReUseRemainingTimeInAZeroToOne;
            ;
        }

    }
}
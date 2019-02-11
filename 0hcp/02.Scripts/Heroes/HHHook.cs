using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace hcp {
    public class HHHook : Projectile  {

        enum HookState
        {
            Activate,
            Retrieve,
            DeActivate,
            MAX
        }
        [SerializeField]
        HookState state = HookState.DeActivate;
        [SerializeField]
        float maxLength;
        [SerializeField]
        float hookVelocity;

        [Tooltip("same with HeroHook's hookOriginPos property")]
        [SerializeField]
        Transform originPosFromheroHook;
        [Tooltip("최대 거리만큼 뻗어나갔을 때 회수되는데 걸리는 시간")]
        [SerializeField]
        float retrieveMaxTime;
        [Tooltip("회수 속력")]
        [SerializeField]
        float retrieveVelocity;
        float retrieveVelocityDiv;

        [SerializeField]
        Transform rope;
        [SerializeField]
        Renderer ropeRenderer;

        [SerializeField]
        Material ropeMat;
       // [Tooltip("로프 머테리얼의 텍스쳐 타일링과 실제 로프 길이 사이 스케일 팩터. 1f를 도출해냈음. 길이*1f 를 머테리얼 타일링 y에 주면 됨.")]
       // [SerializeField]
        float ropeToMaterialTileScaleFactor=1f;

     //   [Tooltip("갈고리가 처음 뻗어나오는 위치에서 갈고리 까지의 z 차에 따른 로프의 스케일 조정값.. 5f 도출값.")]
      //  [SerializeField]
        float disToRopeScaleFactor = 5f;

        [Tooltip("적이 갈고리에 걸렸을 때 슈터와 끌려진 적 사이 거리")]
        [SerializeField]
        float hookedDestDis ;



        protected override void Awake()
        {
            base.Awake();
            retrieveVelocity = maxLength / retrieveMaxTime; //회수 속력.
            retrieveVelocityDiv = 1 / retrieveVelocity;

            ropeMat = new Material(ropeRenderer.material);
            ropeRenderer.material = ropeMat;
            DeActivate();
        }
        
        public void Activate()
        {
            gameObject.SetActive(true);
            velocity = hookVelocity;
            state = HookState.Activate;
        }
        public void DeActivate()
        {
            state = HookState.DeActivate;
            transform.SetPositionAndRotation(originPosFromheroHook.position, originPosFromheroHook.rotation);
            gameObject.SetActive(false);
        }
        public void Retrieve()
        {
            velocity = retrieveVelocity;
            state = HookState.Retrieve;
        }

        private void Update()
        {
            switch (state)
            {
                case HookState.Activate:
                    transform.Translate(Vector3.forward * velocity * Time.deltaTime , Space.Self);
                    MakeRope();
                    if (attachingHero.photonView.IsMine)
                    {
                        if (transform.localPosition.z > maxLength)
                        {
                            state = HookState.Retrieve;
                            velocity = 0f;
                            attachingHero.photonView.RPC("HookRetrieve", Photon.Pun.RpcTarget.All);
                        }
                    }
                  
                    break;
                case HookState.Retrieve:
                    transform.Translate(Vector3.back * velocity * Time.deltaTime , Space.Self);
                    MakeRope();

                    if (attachingHero.photonView.IsMine)
                    {
                        if (transform.localPosition.z < Mathf.Epsilon)
                        {
                            state = HookState.DeActivate;
                            attachingHero.photonView.RPC("HookIsDone", Photon.Pun.RpcTarget.All);
                        }
                    }
                    break;
            }
        }
        private void OnTriggerEnter(Collider other)
        {
            if (!attachingHero.photonView.IsMine) return;
            if (state != HookState.Activate) return;

            int layer = other.gameObject.layer;
            if (layer == Constants.mapLayerMask)
            {
                state = HookState.Retrieve;
                velocity = 0f;
                attachingHero.photonView.RPC("HookRetrieve", Photon.Pun.RpcTarget.All);
                return;
            }

            if (TeamInfo.GetInstance().IsThisLayerEnemy(layer))
            {
                state = HookState.Retrieve;
                    velocity = 0f;

                Hero enemy = other.gameObject.GetComponent<Hero>();
                if (enemy == null)
                {
                    Debug.Log("갈고리로 끌었으나 적이 히어로가 아님");
                  
                    attachingHero.photonView.RPC("HookRetrieve", Photon.Pun.RpcTarget.All);
                    return;
                }

                Vector3 enemyPos = enemy.transform.position;
                Vector3 destPos = attachingHero.transform.position + attachingHero. transform.TransformDirection(Vector3.forward)* hookedDestDis;
                enemy.photonView.RPC("Hooked", Photon.Pun.RpcTarget.All, enemyPos, destPos, 
                    transform.localPosition.z * retrieveVelocityDiv //후크가 원래 자리로 돌아오는데 걸리는 시간이 곧 사람이 끌리는 총 시간임.
                    );
                attachingHero.photonView.RPC("HookRetrieve", Photon.Pun.RpcTarget.All);
            }
        }
        void MakeRope()
        {
            float dis = transform.localPosition.z;  //어차피 부모의 위치에서 출발함.
            Vector3 ropeLocalScale = rope.localScale;
            if (dis < Mathf.Epsilon)
            {
                ropeLocalScale.z = 0f;
                rope.localScale= ropeLocalScale;
                return;
            }
            ropeLocalScale.z = dis * disToRopeScaleFactor;
            rope.localScale = ropeLocalScale;
            ropeMat.mainTextureScale = new Vector2(1, ropeLocalScale.z * ropeToMaterialTileScaleFactor);
        }
    }
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace hcp {
    public class HeroHpBar : MonoBehaviour
    {
        [SerializeField]
        string playerName;
        [SerializeField]
        TMPro.TextMeshProUGUI playerNameTextMesh;
        [SerializeField]
        UnityEngine.UI.Image hpBar;
        [SerializeField]
        bool teamSettingDone=false;
        float attachingHeroMaxHPDiv;

        [SerializeField]
        Hero attachingHero;
        
        public void SetAsTeamSetting()
        {
            if (TeamInfo.GetInstance().IsThisLayerEnemy(attachingHero.gameObject.layer))
            {
                playerNameTextMesh.color = Color.red;
                hpBar.color = Color.red;
            }
            else
            {
                playerNameTextMesh.color = Color.blue;
                hpBar.color = Color.white;
            }
           
            if (attachingHero != null)
            {
                playerNameTextMesh.text = attachingHero.PlayerName;
                attachingHeroMaxHPDiv = 1 / attachingHero.MaxHP;
            }
            teamSettingDone = true;
        }
        private void LateUpdate()
        {
            if (!teamSettingDone||attachingHero==null)
                return;

            hpBar.fillAmount = attachingHero.CurrHP* attachingHeroMaxHPDiv;
           
            transform.LookAt(Camera.main.transform);
            transform.Rotate(Vector3.up * 180f, Space.Self);
        }
    }
}
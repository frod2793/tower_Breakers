using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class LightningTotemSkill : MonoBehaviour
{
    public Action<LightningTotemSkill> CastOverAction;

    [SerializeField] 
    private SpecialLightningTotem   SLightningTotem;
    private SpecialLightningTotem[] mLightningTotems;

    private IEnumerator mESkillCasting;

    public void Init()
    {
        mLightningTotems = new SpecialLightningTotem[3];

        for (int i = 0; i < 3; i++)
        {
            mLightningTotems[i] = Instantiate(SLightningTotem);
            mLightningTotems[i].Init();
        }
    }
    public void Cast()
    {
        if (mESkillCasting == null)
        {
            StartCoroutine(mESkillCasting = SkillCasting());
        }
    }
    private IEnumerator SkillCasting()
    {
        UnitizedPos startDIR =
            UnitizedPos.BOT_LEFT + Kimbokchi.Utility.LuckyNumber(0.5f, 0f, 0.5f);

        for (int i = 0; i < 3; i++)
        {
            int direction = i;

            if (startDIR.Equals(UnitizedPos.BOT_RIGHT))
            {
                direction *= -1;
            }
            mLightningTotems[i].Cast(Castle.Instance.GetMovePoint(startDIR + direction));

            yield return new WaitForSeconds(1f);
        }
        yield return new WaitUntil(() => mLightningTotems.All(o => o.IsOver));

        mESkillCasting = null;
        CastOverAction?.Invoke(this);
    }
}

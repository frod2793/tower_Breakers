using System;
using System.Linq;
using System.Collections;
using UnityEngine;

public class BombTotemSkill : MonoBehaviour
{
    public Action<BombTotemSkill> CastOverAction;

    [SerializeField] 
    private SpecialBombTotem   SBombTotem;
    private SpecialBombTotem[] SBombTotems;

    private Player mPlayer;

    public void Init(Player player)
    {
        SBombTotems = new SpecialBombTotem[3];

        for (int i = 0; i < SBombTotems.Length; i++)
        {
            SBombTotems[i] = Instantiate(SBombTotem);
            SBombTotems[i].Init();
        }
        mPlayer = player;
    }
    public void Cast()
    {
        StartCoroutine(SkillCasting());
    }

    private IEnumerator SkillCasting()
    {
        Vector2 CastPoint()
        {
            float playerY = Castle.Instance.GetMovePointY(mPlayer.GetUnitizedPosV()) + 1.1f;

            return new Vector2(mPlayer.transform.position.x, playerY);
        }
        SBombTotems[0].Cast(CastPoint());

        for (int i = 1; i < 3; i++)
        {
            yield return new WaitForSeconds(0.3f);

            SBombTotems[i].Cast(CastPoint());
        }
        yield return new WaitUntil(() => SBombTotems.All(o => o.IsOver));

        CastOverAction?.Invoke(this);
    }
}

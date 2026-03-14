using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Tentacle : MonoBehaviour, IObject, ICombatable, IAnimEventReceiver
{
    public Action<Tentacle> DeathrattleAction;

    [SerializeField] private ItemDropper _ItemDropper;
    [SerializeField] private AbilityTable AbilityTable;
    [SerializeField] private EnemyAnimator EnemyAnimator;

    [Header("Areas")]
    [SerializeField] private Collider2D BeAtkedCollider;
    [SerializeField] private Area Range;
    [SerializeField] private Area AttackArea;

    private Kraken MasterKraken;
    private AttackPeriod _AttackPeriod;
    private Player _Player;

    public void AnimationPlayOver(AnimState anim)
    {
        switch (anim)
        {
                // Come Out
            case AnimState.Move:
                EnemyAnimator.ChangeState(AnimState.Idle);
                break;

            case AnimState.Attack:
                {
                    _AttackPeriod.AttackActionOver();

                    EnemyAnimator.ChangeState(AnimState.Idle);
                }
                break;

            case AnimState.Death:
                gameObject.SetActive(false);
                break;
        }
    }

    public void CastBuff(Buff buffType, IEnumerator castedBuff)
    {
        StartCoroutine(castedBuff);
    }

    public void Damaged(float damage, GameObject attacker)
    {
        EffectLibrary.Instance.UsingEffect(EffectKind.Damage, transform.position);
        MasterKraken?.Damaged(damage, attacker);

        if ((AbilityTable.Table[Ability.CurHealth] -= damage) <= 0f)
        {
            _AttackPeriod.StopPeriod();
            DeathrattleAction?.Invoke(this);

            EnemyAnimator.ChangeState(AnimState.Death);
            HealthBarPool.Instance.UnUsingHealthBar(transform);

            _ItemDropper.CoinDrop(3);
            _ItemDropper.TryPotionDrop(PotionName.SHealingPotion, PotionName.MHealingPotion);

            BeAtkedCollider.enabled = false;
        }
    }

    public void Init(Kraken master)
    {
        MasterKraken = master;
    }

    public void IInit()
    {
        BeAtkedCollider.enabled = true;

        EnemyAnimator.Init();
        HealthBarPool.Instance.UsingHealthBar(-2.28f, transform, AbilityTable);

        _AttackPeriod = new AttackPeriod(AbilityTable);
        _AttackPeriod.SetAction(Period.Attack, () => EnemyAnimator.ChangeState(AnimState.Attack));

        AbilityTable.Table[Ability.CurHealth] = AbilityTable[Ability.MaxHealth];

        AttackArea.SetEnterAction(o => 
        {
            if (o.TryGetComponent(out ICombatable combatable))
            {
                combatable.Damaged(AbilityTable.AttackPower, gameObject);
            }
        });
        Range.SetScale(AbilityTable.Range);
    }

    public void IUpdate()
    {
        if (!_AttackPeriod.IsProgressing())
        {
            if (Range.HasAny() && _Player != null)
            {
                if (_Player.transform.position.x < transform.position.x)
                {
                    transform.rotation = Quaternion.Euler(Vector3.zero);
                }
                else
                {
                    transform.rotation = Quaternion.Euler(Vector3.up * 180f);
                }
                _AttackPeriod.StartPeriod();
            }
        }
    }

    private void CameraShake()
    {
        MainCamera.Instance.Shake(0.3f, 0.9f);
    }

    public void PlayerEnter(MESSAGE message, Player enterPlayer)
    {
        if (AbilityTable.CanRecognize(message))
            _Player = enterPlayer;
    }

    public void PlayerExit(MESSAGE message)
    {
        if (AbilityTable.CantRecognize(message))
            _Player = null;
    }

    public AbilityTable GetAbility => AbilityTable;
    public bool IsActive() => gameObject.activeSelf;
    public GameObject ThisObject() => gameObject;
}

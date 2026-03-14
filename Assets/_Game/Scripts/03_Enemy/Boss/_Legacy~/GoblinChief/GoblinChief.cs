using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class GoblinChief : MonoBehaviour, IObject, ICombatable
{
    private const int LIGHTNING_CNT = 3;

    public enum Anim
    {
        Idle, Jump, Swing, Skill, Landing, End
    }
    [SerializeField] private UnitizedPosV LPosition3;
    [SerializeField] private ItemDropper _ItemDropper;

    [Header("Ability")]
    [SerializeField] private AbilityTable AbilityTable;
    [SerializeField] private Animator Animator;
    [SerializeField] private GameObject HealthBar;
    [SerializeField] private Image HealthBarImage;

    [Header("Totem Skill Info")]
    [SerializeField] private SpecialBuffTotem BuffTotem;
    [SerializeField] private BombTotemSkill BombTotemSkill;
    [SerializeField] private LightningTotemSkill LightningTotemSkill;

    private Queue<SpecialBuffTotem> mBuffTotemPool;
    private Queue<BombTotemSkill> mBombSkillPool;
    private Queue<LightningTotemSkill> mLightningSkillPool;

    [Header("Swing Skill Info")]
    [SerializeField] private Collider2D DashCollider;
    [SerializeField] private Area SwingArea;

    [Header("Summon Goblins")]
    [SerializeField] private GameObject[] Goblins;

    [Header("Death rattle")]
    [SerializeField] private float FadeTime;
    [SerializeField] private SceneLoader TownLoader;

    private Player mPlayer;
    private int mControlKey;

    private Anim mNextPattern;
    private AttackPeriod mAttackPeriod;

    public AbilityTable GetAbility => AbilityTable;

    private void ChangeIdleState()
    {
        Animator.SetInteger(mControlKey, (int)Anim.Idle);
    }
    private void PatternActionOver()
    {
        mAttackPeriod.AttackActionOver();

        Animator.SetInteger(mControlKey, (int)Anim.Idle);
    }

    public void Damaged(float damage, GameObject attacker)
    {
        EffectLibrary.Instance.UsingEffect(EffectKind.Damage, transform.position);
        if ((AbilityTable.Table[Ability.CurHealth] -= damage) <= 0)
        {
            gameObject.SetActive(false);

            _ItemDropper.CoinDrop(15);
            _ItemDropper.TryPotionDrop(PotionName.SHealingPotion, PotionName.MHealingPotion);

            if (TryGetComponent(out Collider2D collider))
            {
                collider.enabled = false;
            }
            DeathRattle();
        }
        HealthBarImage.fillAmount = AbilityTable[Ability.CurHealth] / AbilityTable[Ability.MaxHealth];
    }

    public void IInit()
    {
        HealthBar.SetActive(true);
        mNextPattern = (Anim)Random.Range(2, 4);

        mAttackPeriod = new AttackPeriod(AbilityTable);
        mAttackPeriod.SetAction(Period.Attack, () =>
        {
            switch (mNextPattern)
            {
                case Anim.Skill:
                    Animator.SetInteger(mControlKey, (int)Anim.Skill);
                    break;
            
                case Anim.Swing:
                    if (mPlayer.GetUnitizedPosV() == LPosition3)
                    {
                        DashSwing();
                    }
                    else
                    {
                        Animator.SetInteger(mControlKey, (int)Anim.Jump);
                    }
                    break;
            }
            mNextPattern = (Anim)Random.Range(2, 4);
        });

        // ~~~ Totem Skill Init ~~~

        mBombSkillPool = new Queue<BombTotemSkill>();
        mBuffTotemPool = new Queue<SpecialBuffTotem>();

        mLightningSkillPool = new Queue<LightningTotemSkill>();

        for (int i = 0; i < 2; i++)
        {
            AddBuffTotem();

            AddLightningSkill();
        }
        // ~~~ Totem Skill Init ~~~

        SwingArea.SetEnterAction(o =>
        {
            if (o.TryGetComponent(out ICombatable combatable))
            {
                combatable.Damaged(20f, gameObject);
            }
        });
        mControlKey = Animator.GetParameter(0).nameHash;
    }

    public void IUpdate()
    {
        if (!mAttackPeriod.IsProgressing()) 
        {
            mAttackPeriod.StartPeriod();
        }
    }

    private void Jumping()
    {
        UnitizedPosV jumpPos = mPlayer.GetUnitizedPosV();

        Vector2 point = new Vector2(transform.position.x, Castle.Instance.GetMovePointY(jumpPos) + 1.05f);

        if (point.x > transform.position.x)
        {
            transform.rotation = Quaternion.Euler(Vector3.up * 180);
        }
        else
            transform.rotation = Quaternion.Euler(Vector3.zero);

        LPosition3 = jumpPos;

        StartCoroutine(Move(point, () =>
        {
            Animator.SetInteger(mControlKey, (int)Anim.Landing);
        }));
    }

    private void DashSwing()
    {
        Vector2 point = new Vector2(mPlayer.transform.position.x, transform.position.y);

        if (point.x > transform.position.x)
        {
            transform.rotation = Quaternion.Euler(Vector3.up * 180);
        }
        else
            transform.rotation = Quaternion.Euler(Vector3.zero);

        StartCoroutine(Move(point, () =>
        {
            Animator.SetInteger(mControlKey, (int)Anim.Swing);

        }));
    }
    private IEnumerator Move(Vector2 point, System.Action moveOverAction)
    {
        float lerp = 0f;

        float DeltaTime()
        {
            return Time.deltaTime * Time.timeScale;
        }
        while (lerp < 1f)
        {
            lerp = Mathf.Min(1, lerp + AbilityTable.MoveSpeed * DeltaTime());

            transform.position = Vector2.Lerp(transform.position, point, lerp);

            yield return null;
        }
        moveOverAction.Invoke();
    }
    private void SummonTotem()
    {
        int random = Random.Range(0, 3);

        UnitizedPosV playerPosV = mPlayer.GetUnitizedPosV();

        Vector2 castPoint = new Vector2
            (mPlayer.transform.position.x, Castle.Instance.GetMovePointY(playerPosV) + 1.2f);

        switch (random) {
            case 0:
                if (mBombSkillPool.Count == 0) 
                {
                    AddBombSkill();
                }
                mBombSkillPool.Dequeue().Cast();
                break;

            case 1:
                if (mBuffTotemPool.Count == 0)
                {
                    AddBuffTotem();
                }
                mBuffTotemPool.Dequeue().Cast(Castle.Instance.GetPlayerRoom(), castPoint);
                break;

            case 2:
                if (mLightningSkillPool.Count == 0)
                {
                    AddLightningSkill();
                }
                mLightningSkillPool.Dequeue().Cast();
                break;
        }
    }

    public void PlayerEnter(MESSAGE message, Player enterPlayer)
    {
        mPlayer = enterPlayer;
    }

    public void PlayerExit(MESSAGE message)
    {
        if (message.Equals(MESSAGE.BELONG_FLOOR)) {
            mPlayer = null;
        }
    }

    public void CastBuff(Buff buffType, IEnumerator castedBuff) {
        StartCoroutine(castedBuff);
    }

    public bool IsActive() {
        return gameObject.activeSelf;
    }

    public GameObject ThisObject() {
        return gameObject;
    }

    // 죽을때 사용하는 그런 머시기
    private void DeathRattle()
    {
        // MainCamera.Instance.Fade(FadeTime, FadeType.In, () => TownLoader.SceneLoad());
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            if (collision.TryGetComponent(out ICombatable combatable))
            {
                combatable.Damaged(8f, gameObject);

                MainCamera.Instance.Shake(0.5f, 0.5f);
            }
        }
    }
    private void AddBombSkill()
    {
        var bombTotem = Instantiate(BombTotemSkill);

        bombTotem.Init(mPlayer);
        bombTotem.CastOverAction = o =>
        { 
            mBombSkillPool.Enqueue(o);
        };
        mBombSkillPool.Enqueue(bombTotem);
    }
    private void AddBuffTotem()
    {
        var buffTotem = Instantiate(BuffTotem);

        buffTotem.Init();
        buffTotem.CastOverAction = o =>
        {
            mBuffTotemPool.Enqueue(o);
        };
        mBuffTotemPool.Enqueue(buffTotem);
    }
    private void AddLightningSkill()
    {
        var lightning = Instantiate(LightningTotemSkill);

        lightning.Init();
        lightning.CastOverAction = o =>
        {
            mLightningSkillPool.Enqueue(o);
        };
        mLightningSkillPool.Enqueue(lightning);
    }
}

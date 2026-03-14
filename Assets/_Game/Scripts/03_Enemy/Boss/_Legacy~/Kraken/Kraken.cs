using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Kraken : MonoBehaviour, IObject, ICombatable
{
    private enum Pattern
    {
        FallingTentacle, StrikeTentacle, ArtilleryFire, SummonTentacle, SummonSeaMonster
    }
    [SerializeField] private AbilityTable AbilityTable;
    [SerializeField] private GameObject HealthBar;
    [SerializeField] private Image HealthBarImage;
    [SerializeField] private SceneLoader TownLoader;

    [Header("ArtilleryFire Info")]
    [SerializeField] private Projection ArtilleryShell;
    [SerializeField] private float ArtilleryShellSpeed;
    [SerializeField] private float ArtilleryFireTime;
    [SerializeField] private float ArtilleryFireDelay;
    [SerializeField] private float RotationSpeed;

    [Header("Strike Tentacle")]
    [SerializeField] private GameObject _StrikeTentacle;
    [SerializeField] private float StrikeTime;
    [SerializeField] private int StrikeAreaChildIndex;

    [Header("Summon Tentacle")]
    [SerializeField] private Tentacle Tentacle;
    [SerializeField] private int TentacleCount;
    private Pool<Tentacle> _TentaclePool;

    [Header("Summon SeaMonster")]
    [SerializeField] private GameObject[] SeaMonsters;

    [Header("Falling Tentacle")]
    [SerializeField] private GameObject _FallingTentacle;
    [SerializeField] private float FallingTime;
    [SerializeField] private int FallingAreaChildIndex;

    private Coroutine _Coroutine;
    private Pattern _NextPattern;

    private int _PatternInvokeCount;

    private AttackPeriod _AttackPeriod;
    private Pool<Projection> _ArtilleryShellPool;

    public AbilityTable GetAbility => AbilityTable;

    public void IInit()
    {
        SoundManager.Instance.PlaySound(SoundName.BossAppear_DeepSea);

        _PatternInvokeCount = 0;

        _FallingTentacle = Instantiate(_FallingTentacle);
        _FallingTentacle.gameObject.SetActive(false);

        Area _Area;

        var fallingChild = _FallingTentacle.transform.GetChild(FallingAreaChildIndex);

        if (fallingChild.TryGetComponent(out _Area))
        {
            _Area.SetEnterAction(o =>
            {
                if (o.TryGetComponent(out ICombatable combatable))
                {

                    combatable.Damaged(AbilityTable.AttackPower, gameObject);
                }
            });
        }

        _StrikeTentacle = Instantiate(_StrikeTentacle);
        _StrikeTentacle.SetActive(false);

        var strikeChild = _StrikeTentacle.transform.GetChild(StrikeAreaChildIndex);

        if (strikeChild.TryGetComponent(out _Area))
        {
            _Area.SetEnterAction(o =>
            {
                if (o.TryGetComponent(out ICombatable combatable))
                {

                    combatable.Damaged(AbilityTable.AttackPower, gameObject);
                }
            });
        }

        _ArtilleryShellPool = new Pool<Projection>();
        _ArtilleryShellPool.Init(16, ArtilleryShell, o => 
        {
            o.SetAction(
                hitTarget =>
                {
                    if (hitTarget.TryGetComponent(out ICombatable combatable))
                    {
                        combatable.Damaged(AbilityTable.AttackPower, gameObject);
                    }
                },
                projection => 
                {
                    _ArtilleryShellPool.Add(projection);
                });
        });

        _TentaclePool = new Pool<Tentacle>();
        _TentaclePool.Init(4, Tentacle, o => 
        {
            o.Init(this);

            o.DeathrattleAction = tentacle =>
            {
                TentacleCount--;

                _TentaclePool.Add(tentacle);
            };
        });

        _AttackPeriod = new AttackPeriod(AbilityTable);
        _AttackPeriod.SetAction(Period.Attack, () => 
        {
            _PatternInvokeCount++;

            if (_PatternInvokeCount == 3)
            {
                _NextPattern = Pattern.SummonSeaMonster;

                _PatternInvokeCount = 0;
            }
            else if (TentacleCount <= 3)
            {
                _NextPattern = Pattern.SummonTentacle;
            }
            else
            {
                _NextPattern = (Pattern)Random.Range(0, 3);
            }
            switch (_NextPattern)
            {
                case Pattern.FallingTentacle:
                    _Coroutine.StartRoutine(FallingTentacle());
                    break;

                case Pattern.StrikeTentacle:
                    _Coroutine.StartRoutine(StrikeTentacle());
                    break;

                case Pattern.ArtilleryFire:
                    _Coroutine.StartRoutine(ArtilleryFire());
                    break;

                case Pattern.SummonTentacle:
                    SummonTentacle();
                    break;

                case Pattern.SummonSeaMonster:
                    SummonSeaMonster();
                    break;
            }
        });
        HealthBar.SetActive(true);

        _Coroutine = new Coroutine(this);
        _Coroutine.StartRoutine(AwakeRoutine());
    }

    public void IUpdate()
    {
        if (_Coroutine.IsFinished() && AbilityTable[Ability.CurHealth] > 0f)
        {
            if (!_AttackPeriod.IsProgressing())
            {
                _AttackPeriod.StartPeriod();
            }
        }
    }

    public void PlayerEnter(MESSAGE message, Player enterPlayer)
    { }

    public void PlayerExit(MESSAGE message)
    { }

    private void SummonTentacle()
    {
        int roomIndex = Random.Range(0, 3);

        var tentacle = _TentaclePool.Get();

        var room = Castle.Instance.GetFloorRooms()[roomIndex];
            room.AddIObject(tentacle);

        tentacle.transform.parent = room.transform;

        roomIndex *= 3;
        float summonPointMinX = Castle.Instance.GetMovePoint((UnitizedPos)roomIndex).x;
        float summonPointMaxX = Castle.Instance.GetMovePoint((UnitizedPos)roomIndex + 2).x;

        MainCamera.Instance.Shake(0.6f, 1f);
        Vector2 summonPoint = new Vector2(Random.Range(summonPointMinX, summonPointMaxX), 2.3f);
        tentacle.transform.localPosition = summonPoint;

        TentacleCount++;
        _AttackPeriod.AttackActionOver();
    }

    private void SummonSeaMonster()
    {
        int floorIndex = Random.Range(0, 3);

        var room = Castle.Instance.GetFloorRooms()[floorIndex];

        float summonPointMinX = Castle.Instance.GetMovePoint((UnitizedPos)(floorIndex * 3)).x;
        float summonPointMaxX = Castle.Instance.GetMovePoint((UnitizedPos)(floorIndex * 3) + 2).x;

        for (int i = 0; i < 3; i++)
        {
            Vector3 summonPoint = new Vector2(Random.Range(summonPointMinX, summonPointMaxX), 0f);

            var monster = Instantiate(SeaMonsters[Random.Range(0, SeaMonsters.Length)], room.transform);
                monster.transform.localPosition += summonPoint;

            if (monster.TryGetComponent(out IObject _object))
            {
                room.AddIObject(_object);
            }
        }
        _AttackPeriod.AttackActionOver();
    }

    private IEnumerator AwakeRoutine()
    {
        for (int i = 0; i < 4; i++)
        {
            yield return new WaitForSeconds(1f);

            SummonTentacle();
        }
        _AttackPeriod.StartPeriod();
        _Coroutine.Finish();
    }

    private IEnumerator FallingTentacle()
    {
        bool isLeft = Random.value > 0.5f;

        _FallingTentacle.SetActive(true);
        _FallingTentacle.transform.position = transform.position + Vector3.up;

        if (isLeft)
        {
            _FallingTentacle.transform.position += Vector3.left * 3.5f;
            _FallingTentacle.transform.rotation = Quaternion.Euler(Vector3.up * 180);
        }
        else
        {
            _FallingTentacle.transform.position += Vector3.right * 3.5f;
            _FallingTentacle.transform.rotation = Quaternion.Euler(Vector3.zero);
        }
        yield return new WaitForSeconds(FallingTime);

        _FallingTentacle.SetActive(false);
        _AttackPeriod.AttackActionOver();

        _Coroutine.Finish();
    }

    private IEnumerator StrikeTentacle()
    {
        bool isLeft = Random.value > 0.5f;

        _StrikeTentacle.SetActive(true);
        _StrikeTentacle.transform.position = transform.position + Vector3.up;
        _StrikeTentacle.transform.position += Vector3.up * Random.Range(-1, 2) * 5f;

        MainCamera.Instance.Shake();

        if (isLeft)
        {
            _StrikeTentacle.transform.rotation = Quaternion.Euler(Vector3.up * 180);
        }
        else
        {
            _StrikeTentacle.transform.rotation = Quaternion.Euler(Vector3.zero);
        }
        yield return new WaitForSeconds(StrikeTime);

        _StrikeTentacle.SetActive(false);
        _AttackPeriod.AttackActionOver();

        _Coroutine.Finish();
    }

    private IEnumerator DeathRattle()
    {
        yield return new WaitForSeconds(2.917f);

        gameObject.SetActive(false);
    }

    private IEnumerator ArtilleryFire()
    {
        float speed = ArtilleryShellSpeed;

        float theta = 0f;

        for (float i = 0f; i < ArtilleryFireTime; i += ArtilleryFireDelay)
        {
            float angle = 0f;

            for (int j = 0; j < 4; j++)
            {
                angle += (theta + (90 * j));
                angle *= Mathf.Deg2Rad;

                var direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)).normalized;

                _ArtilleryShellPool.Get().Shoot(transform.position, direction, speed);
            }
            yield return new WaitForSeconds(ArtilleryFireDelay); 
            
            theta += RotationSpeed;

            if (theta >= 360f) theta = 0f;
        }
        _AttackPeriod.AttackActionOver();

        _Coroutine.Finish();
    }

    public GameObject ThisObject() => gameObject;
    public bool IsActive() => gameObject.activeSelf;

    public void Damaged(float damage, GameObject attacker)
    {
        AbilityTable.Table[Ability.CurHealth] -= damage;
        HealthBarImage.fillAmount = AbilityTable[Ability.CurHealth] / AbilityTable[Ability.MaxHealth];

        if (AbilityTable[Ability.CurHealth] <= 0f)
        {
            _AttackPeriod.StopPeriod();

            _Coroutine.StopRoutine();

            if (TryGetComponent(out Animator animator))
            {
                animator.SetBool(animator.GetParameter(0).nameHash, true);
            }
            StartCoroutine(DeathRattle());
            // MainCamera.Instance.Shake(1.8f, 1.2f);
            // 
            // MainCamera.Instance.Fade(2.25f, FadeType.In, () => TownLoader.SceneLoad());
        }
    }

    public void CastBuff(Buff buffType, IEnumerator castedBuff)
    { }
}

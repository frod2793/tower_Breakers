using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RobotsBossSword : MonoBehaviour, IObject, ICombatable
{
    private const float MoveAfterAnimation = 0.75f;

    private const float SplittingTime = 0.167f;
    private const float TurningTimeScale = 0.35f;

    private const float RestBeginTime = 1.0f;
    private const float RestEndTime = 0.75f;

    private const int Idle   = 0;
    private const int Move   = 1;
    private const int Attack = 2;
    private const int Death  = 3;
    private const int Rest   = 4;

    [Header("Ability")]
    [SerializeField] private AbilityTable _AbilityTable;
    [SerializeField] private Animator _Animator;
    [SerializeField] private GameObject _HealthBar;
    [SerializeField] private Image _HealthBarImage;
    [SerializeField] private Area _AttackArea;

    [Header("Attack Property")]
    [SerializeField] private float _SplittingAccel;
    [SerializeField] private AnimationCurve _TurningCurve;

    [Header("Move Property")]
    [SerializeField] private Vector2 _MoveRangeMin;
    [SerializeField] private Vector2 _MoveRangeMax;
    [Space()]
    [SerializeField] private float _MoveWaitMin;
    [SerializeField] private float _MoveWaitMax;
    [Space()]
    [SerializeField] private float _MoveTimeMin;
    [SerializeField] private float _MoveTimeMax;

    [Header("Rest Property")]
    [SerializeField] private float _RestTime;
    [SerializeField] private AnimationCurve _FallingCurve;

    private Player _Player;
    private int _AnimControlKey;

    private int _RestCount;

    private Vector2 _AttackDirection;

    private Coroutine _UpdateRoutine;
    private Coroutine _ActionRoutine;

    [ContextMenu("MoveOrder")]
    private void MoveOrder()
    {
        _Animator.SetInteger(_AnimControlKey, Move);
    }
    [ContextMenu("IdleOrder")]
    private void IdleOrder()
    {
        _Animator.SetInteger(_AnimControlKey, Idle);
    }
    [ContextMenu("AttackOrder")]
    private void AttackOrder()
    {
        _Animator.SetInteger(_AnimControlKey, Attack);
    }
    public void IInit()
    {
        _AnimControlKey = _Animator.GetParameter(0).nameHash;

        _UpdateRoutine = new Coroutine(this);
        _ActionRoutine = new Coroutine(this);

        _UpdateRoutine.StartRoutine(UpdateRoutine());
        _HealthBar.SetActive(true);

        _AttackArea.SetEnterAction(o =>
        {
            if (o.Equals(_Player.gameObject) && _Animator.GetInteger(_AnimControlKey) == Attack)
            {
                _Player.Damaged(_AbilityTable.AttackPower, gameObject);
            }
        });
    }
    public void IUpdate()
    {
        
    }
    public void PlayerEnter(MESSAGE message, Player enterPlayer)
    {
        if (_AbilityTable.CanRecognize(message))
        {
            _Player = enterPlayer;
        }
    }
    public void PlayerExit(MESSAGE message)
    {
        if (_AbilityTable.CantRecognize(message))
        {
            _Player = null;
        }
    }
    public void Damaged(float damage, GameObject attacker)
    {
        EffectLibrary.Instance.UsingEffect(EffectKind.Damage, transform.position);
        if ((_AbilityTable.Table[Ability.CurHealth] -= damage) <= 0f)
        {
            _Animator.SetInteger(_AnimControlKey, Death);
            _Animator.Play(EnemyAnimator.DeathAnimation);

            _UpdateRoutine.StopRoutine();
            _ActionRoutine.StopRoutine();

            transform.rotation = Quaternion.identity;
        }
        float rate = _AbilityTable[Ability.CurHealth] / _AbilityTable[Ability.MaxHealth];
        _HealthBarImage.fillAmount = rate;

    }
    #region
    public void CastBuff(Buff buffType, IEnumerator castedBuff)
    {
        StartCoroutine(castedBuff);
    }
    public bool IsActive()
    {
        return gameObject.activeSelf;
    }
    public GameObject ThisObject()
    {
        return gameObject;
    }
    public AbilityTable GetAbility => _AbilityTable;
    #endregion;

    private void AE_SetIdleState()
    {
        _Animator.SetInteger(_AnimControlKey, Idle);
    }
    private void AE_DownFall()
    {
        _ActionRoutine.StartRoutine(DownFallRoutine());

        float angle = Mathf.Atan2(_AttackDirection.y, _AttackDirection.x) * Mathf.Rad2Deg;

        EffectLibrary.Instance.UsingEffect(EffectKind.SwordAfterImage, transform.position + (Vector3)(_AttackDirection * 4.5f)).transform.rotation 
            = Quaternion.AngleAxis(angle - 270f, Vector3.forward);
    }
    private void AE_MoveAction()
    {
        _ActionRoutine.StartRoutine(MoveRoutine());
    }
    private void AE_DeathEndHook()
    {
        gameObject.SetActive(false);
    }
    private IEnumerator DownFallRoutine()
    {
        float speed = _AbilityTable.MoveSpeed * SplittingTime * _SplittingAccel;
        Vector3 dir = _AttackDirection;
        Vector3 pos;

        dir.Normalize();
        for (float i = 0; i < SplittingTime; i += Time.deltaTime * Time.timeScale)
        {
            pos = transform.localPosition + dir * speed * Time.timeScale;

            pos.x = Mathf.Clamp(pos.x, _MoveRangeMin.x, _MoveRangeMax.x);
            pos.y = Mathf.Clamp(pos.y, _MoveRangeMin.y, _MoveRangeMax.y);

            transform.localPosition = pos;

            if (_MoveRangeMax.x == pos.x || _MoveRangeMin.x == pos.x || 
                _MoveRangeMax.y == pos.y || _MoveRangeMin.y == pos.y) 
            {
                break; 
            }
            yield return null;
        }
        _ActionRoutine.Finish();
    }
    private IEnumerator UpdateRoutine()
    {
        while (_AbilityTable[Ability.CurHealth] > 0f)
        {
            float moveWait = Random.Range(_MoveTimeMin, _MoveWaitMax);
            for (float i = 0f; i < moveWait; i += Time.deltaTime * Time.timeScale)
                yield return null;

            _Animator.SetInteger(_AnimControlKey, Move);
            while (_Animator.GetInteger(_AnimControlKey) != Idle) yield return null;

            float wait = _AbilityTable.BeginAttackDelay + MoveAfterAnimation;
            for (float i = 0f; i < wait; i += Time.deltaTime * Time.timeScale)
                yield return null;

            // Attack and AttackTurning
            {
                Vector3 dir = _Player.transform.position - transform.position;
                float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

                _AttackDirection = dir.normalized;

                var rotA = transform.rotation;
                var rotB = Quaternion.AngleAxis(angle - 180f, Vector3.forward);

                AttackOrder();

                float time = Mathf.Abs(rotB.z - transform.rotation.z) * TurningTimeScale;
                for (float i = 0f; i < time; i += Time.timeScale * Time.deltaTime)
                {
                    float rate = _TurningCurve.Evaluate(Mathf.Min(i / time, 1f));

                    transform.rotation = Quaternion.Lerp(rotA, rotB, rate);
                    yield return null;
                }
            }
            while (_Animator.GetInteger(_AnimControlKey) != Idle) yield return null;
            {
                var rotA = transform.rotation;
                var rotB = Quaternion.identity;

                float time = Mathf.Abs(rotB.z - transform.rotation.z * TurningTimeScale);
                for (float i = 0f; i < time; i += Time.timeScale * Time.deltaTime)
                {
                    float rate = _TurningCurve.Evaluate(Mathf.Min(i / time, 1f));

                    transform.rotation = Quaternion.Lerp(rotA, rotB, rate);
                    yield return null;
                }
            }
            _RestCount++;

            if (_RestCount == 3)
            {
                _RestCount = 0;

                _Animator.SetInteger(_AnimControlKey, Rest);

                Vector2 point = Castle.Instance.GetMovePoint(UnitizedPos.TOP);
                Vector2 position = transform.position;

                for (UnitizedPos pos = UnitizedPos.MID; pos < UnitizedPos.END; pos += 3)
                {
                    Vector2 p = Castle.Instance.GetMovePoint(pos);
                    if (pos != UnitizedPos.BOT && p.y > position.y) continue;

                    if (Mathf.Abs(p.y - position.y) < Mathf.Abs(point.y - position.y))
                    {
                        point = p;
                    }
                }
                point.x = position.x;
                for (float i = 0f; i < RestBeginTime; i += Time.deltaTime * Time.timeScale)
                {
                    float rate = _FallingCurve.Evaluate(Mathf.Min(i / RestBeginTime, 1f));

                    transform.position = Vector3.Lerp(position, point, rate);
                    yield return null;
                }
                for (float i = 0f; i < _RestTime; i += Time.deltaTime * Time.timeScale)
                    yield return null;
                _Animator.SetInteger(_AnimControlKey, Idle);

                Vector2 start = position = transform.localPosition;
                if ((position.x > _MoveRangeMax.x) || (position.x < _MoveRangeMin.x) ||
                    (position.y > _MoveRangeMax.y) || (position.y < _MoveRangeMin.y))
                {
                    position.x = Mathf.Clamp(position.x, _MoveRangeMin.x, _MoveRangeMax.x);
                    position.y = Mathf.Clamp(position.y, _MoveRangeMin.y, _MoveRangeMax.y);

                    for (float i = 0f; i < RestEndTime; i += Time.deltaTime * Time.timeScale)
                    {
                        float rate = _FallingCurve.Evaluate(Mathf.Min(i / RestEndTime, 1f));

                        transform.localPosition = Vector3.Lerp(start, position, rate);
                        yield return null;
                    }
                }
            }
        }
    }
    private IEnumerator MoveRoutine()
    {
        Vector3 direction = new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f)).normalized;
        {
            float xAbsMax = Mathf.Abs(transform.localPosition.x + _MoveRangeMax.x);
            float xAbsMin = Mathf.Abs(transform.localPosition.x + _MoveRangeMin.x);

            direction.x *=
                ((xAbsMin > xAbsMax) && direction.x < 0) ||
                ((xAbsMin < xAbsMax) && direction.x > 0) ? -1 : 1;

            float yAbsMax = Mathf.Abs(transform.localPosition.y + _MoveRangeMax.y);
            float yAbsMin = Mathf.Abs(transform.localPosition.y + _MoveRangeMin.y);

            direction.y *=
                ((yAbsMax > yAbsMin) && direction.y < 0) ||
                ((yAbsMax < yAbsMin) && direction.y > 0) ? -1 : 1;
        }
        float moveTime = Random.Range(_MoveTimeMin, _MoveTimeMax);

        for (float i = 0f; i < moveTime; i += Time.deltaTime * Time.timeScale)
        {
            Vector3 pos = transform.localPosition;
            pos += direction * _AbilityTable.MoveSpeed * Time.deltaTime * Time.timeScale;

            if ((pos.x > _MoveRangeMax.x) || (pos.x < _MoveRangeMin.x) ||
                (pos.y > _MoveRangeMax.y) || (pos.y < _MoveRangeMin.y))
            {
                break;
            }
            transform.localPosition = pos;
            yield return null;
        }
        _Animator.SetInteger(_AnimControlKey, Idle);
        _ActionRoutine.Finish();
    }
}

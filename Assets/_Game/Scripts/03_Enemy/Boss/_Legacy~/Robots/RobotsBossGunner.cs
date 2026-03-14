using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RobotsBossGunner : MonoBehaviour, IObject, ICombatable
{
    private readonly Vector3 LookRightScaleBody = new Vector3(-1f, 1f, 1f);
    private readonly Vector3 LookRightScaleArm = new Vector3(-1f, -1f, 1f);

    private const float AimingTime = 0.317f;
    private const float AttackAnimTime = 1.833f;
    private const float DeathAnimTime = 1.667f;

    private const float RestBeginTime = 1.0f;
    private const float RestEndTime = 0.917f;

    private const int Arm_Idle = 0;
    private const int Arm_Rest = 1;
    private const int Arm_Attack = 2;
    private const int Arm_Death = 4;

    private const int Body_Idle = 0;
    private const int Body_Rest = 1;
    private const int Body_Death = 2;

    private readonly UnitizedPos[] Directions = new UnitizedPos[4]
    {
        UnitizedPos.TOP,
        UnitizedPos.MID_LEFT,
        UnitizedPos.MID_RIGHT,
        UnitizedPos.BOT
    };
    [Header("Ability")]
    [SerializeField] private AbilityTable _AbilityTable;
    [SerializeField] private Animator _BodyAnimator;
    [SerializeField] private Animator _ArmAnimator;
    [SerializeField] private Image _HealthBarImage;

    [Header("Move Property")]
    [SerializeField] private Vector2 _MoveRange;
    [Space()]
    [SerializeField] private float _MoveWaitMin;
    [SerializeField] private float _MoveWaitMax;
    [Space()]
    [SerializeField] private float _MoveTimeMin;
    [SerializeField] private float _MoveTimeMax;

    [Header("Rest Property")]
    [SerializeField] private float _RestTime;
    [SerializeField] private AnimationCurve _FallingCurve;

    [Header("General Property")]
    [SerializeField] private Transform _FrontArmAxis;
    [SerializeField] private Transform _BehindArmAxis;
    [SerializeField] private AnimationCurve _AimingCurve;

    private Player _Player;
    private int _BodyControlKey;
    private int _ArmControlKey;

    private int _RestCount;

    private Coroutine _UpdateRoutine;
    private Coroutine _ActionRoutine;

    [ContextMenu("IdleOrder")]
    private void IdleOrder()
    {
        _BodyAnimator.SetInteger(_BodyControlKey, Body_Idle);
        _ArmAnimator.SetInteger(_ArmControlKey, Arm_Idle);
    }
    [ContextMenu("AttackOrder")]
    private void AttackOrder()
    {
        _ActionRoutine.StartRoutine(AimingRoutine());

        _BodyAnimator.SetInteger(_BodyControlKey, Body_Idle);
        _ArmAnimator.SetInteger(_ArmControlKey, Arm_Attack);
    }
    [ContextMenu("RestOrder")]
    private void RestOrder()
    {
        _BodyAnimator.SetInteger(_BodyControlKey, Body_Rest);
        _ArmAnimator.SetInteger(_ArmControlKey, Arm_Rest);
    }
    [ContextMenu("DeathOrder")]
    private void DeathOrder()
    {
        _BodyAnimator.SetInteger(_BodyControlKey, Body_Death);
        _ArmAnimator.SetInteger(_ArmControlKey, Arm_Death);

        _BodyAnimator.Play(EnemyAnimator.DeathAnimation);
        _ArmAnimator.Play(EnemyAnimator.DeathAnimation);

        StartCoroutine(DeathRoutine());
    }
    public void IInit()
    {
        _BodyControlKey = _BodyAnimator.GetParameter(0).nameHash;
        _ArmControlKey = _ArmAnimator.GetParameter(0).nameHash;

        _UpdateRoutine = new Coroutine(this);
        _ActionRoutine = new Coroutine(this);

        _UpdateRoutine.StartRoutine(UpdateRoutine());

        _Player = FindObjectOfType<Player>();
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
            DeathOrder();

            _UpdateRoutine.StopRoutine();
            _ActionRoutine.StopRoutine();
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
        IdleOrder();
    }
    private IEnumerator UpdateRoutine()
    {
        while (_AbilityTable[Ability.CurHealth] > 0f)
        {
            float moveWait = Random.Range(_MoveTimeMin, _MoveWaitMax);
            for (float i = 0f; i < moveWait; i += Time.deltaTime * Time.timeScale)
                yield return null;

            _ActionRoutine.StartRoutine(MoveRoutine());
            while (!_ActionRoutine.IsFinished())
                yield return null;

            float wait = _AbilityTable.BeginAttackDelay;
            for (float i = 0f; i < wait; i += Time.deltaTime * Time.timeScale)
                yield return null;

            AttackOrder();
            _RestCount++;
            while (!_ActionRoutine.IsFinished())
                yield return null;

            for (float i = 0f; i < AttackAnimTime; i += Time.timeScale * Time.deltaTime)
                yield return null;

            _ActionRoutine.StartRoutine(BackToDefaultStateRoutine());
            while (!_ActionRoutine.IsFinished())
                yield return null;

            if (_RestCount == 3)
            {
                _RestCount = 0;

                RestOrder();

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
                IdleOrder();

                Vector2 start = position = transform.localPosition;

                if ((position.x > _MoveRange.x) || (position.x < -_MoveRange.x) ||
                    (position.y > _MoveRange.y) || (position.y < -_MoveRange.y))
                {
                    position.x = Mathf.Clamp(position.x, -_MoveRange.x, _MoveRange.x);
                    position.y = Mathf.Clamp(position.y, -_MoveRange.y, _MoveRange.y);

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
            float xAbsSub = Mathf.Abs(transform.localPosition.x - _MoveRange.x);
            float xAbsAdd = Mathf.Abs(transform.localPosition.x + _MoveRange.x);

            direction.x *=
                ((xAbsSub > xAbsAdd) && direction.x < 0) ||
                ((xAbsSub < xAbsAdd) && direction.x > 0) ? -1 : 1;

            transform.localScale 
                = direction.x < 0 ? Vector3.one : LookRightScaleBody;

            float yAbsSub = Mathf.Abs(transform.localPosition.y - _MoveRange.y);
            float yAbsAdd = Mathf.Abs(transform.localPosition.y + _MoveRange.y);

            direction.y *=
                ((yAbsSub > yAbsAdd) && direction.y < 0) ||
                ((yAbsSub < yAbsAdd) && direction.y > 0) ? -1 : 1;
        }
        float moveTime = Random.Range(_MoveTimeMin, _MoveTimeMax);

        for (float i = 0f; i < moveTime; i += Time.deltaTime * Time.timeScale)
        {
            Vector3 pos = transform.localPosition;
            pos += direction * _AbilityTable.MoveSpeed * Time.deltaTime * Time.timeScale;

            if ((pos.x > _MoveRange.x) || (pos.x < -_MoveRange.x) ||
                (pos.y > _MoveRange.y) || (pos.y < -_MoveRange.y))
            {
                break;
            }
            transform.localPosition = pos;
            yield return null;
        }
        IdleOrder();
        _ActionRoutine.Finish();
    }
    private IEnumerator DeathRoutine()
    {
        for (float i = 0f; i < DeathAnimTime; i += Time.deltaTime * Time.timeScale)
            yield return null;
        gameObject.SetActive(false);
    }
    private IEnumerator AimingRoutine()
    {
        Vector2 dir = _Player.transform.position - transform.position;

        float rot = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 180f;
        bool lookRight = (-90 > rot && rot > -270);

        Quaternion goalRotation = Quaternion.AngleAxis(rot * (lookRight ? -1 : 1), Vector3.forward);
        Vector3 goalBodyScale = lookRight ? new Vector3(-1f, 1f, 1f) : Vector3.one;
        Vector3 goalArmScale = lookRight ? new Vector3(-1f, -1f, 1f) : Vector3.one;

        Quaternion startRotation = _BehindArmAxis.localRotation;
        Vector3 startBodyScale = transform.localScale;
        Vector3 startArmScale = _BehindArmAxis.localScale;

        for (float i = 0f; i < AimingTime; i += Time.deltaTime * Time.timeScale)
        {
            float rate = _AimingCurve.Evaluate(i / AimingTime);

            transform.localScale = Vector3.Lerp(startBodyScale, goalBodyScale, rate);

            _FrontArmAxis.localScale = _BehindArmAxis.localScale =
                Vector3.Lerp(startArmScale, goalArmScale, rate);

            _FrontArmAxis.localRotation = _BehindArmAxis.localRotation =
                Quaternion.Lerp(startRotation, goalRotation, rate);

            yield return null;
        }
        _ActionRoutine.Finish();
    }
    private IEnumerator BackToDefaultStateRoutine()
    {
        Quaternion goalRotation = (transform.localScale.x < 0f ? Quaternion.Euler(0, 0, 180) : Quaternion.identity);
        Quaternion startRotation = _BehindArmAxis.localRotation;

        for (float i = 0f; i < AimingTime; i += Time.deltaTime * Time.timeScale)
        {
            float rate = _AimingCurve.Evaluate(i / AimingTime);

            _FrontArmAxis.localRotation = _BehindArmAxis.localRotation =
                Quaternion.Lerp(startRotation, goalRotation, rate);

            yield return null;
        }
        _ActionRoutine.Finish();
    }
}

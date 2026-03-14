using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RobotsBossShield : MonoBehaviour, IObject, ICombatable
{
    private const float OnActiveSpeed = 5f;
    private const float ActiveEndTime = 0.571f;

    private const float RestBeginTime = 1.0f;
    private const float RestEndTime = 0.917f;

    private const int Idle   = 0;
    private const int Active = 1;
    private const int Death  = 2;
    private const int Rest   = 3;

    private readonly UnitizedPos[] Directions = new UnitizedPos[4]
    {
        UnitizedPos.TOP,
        UnitizedPos.MID_LEFT,
        UnitizedPos.MID_RIGHT,
        UnitizedPos.BOT
    };

    [Header("Ability")]
    [SerializeField] private AbilityTable _AbilityTable;
    [SerializeField] private Animator _Animator;
    [SerializeField] private Image _HealthBarImage;
    [SerializeField] private Area _AttackArea;

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
    private Coroutine _UpdateRoutine;
    private Coroutine _ActionRoutine;

    [ContextMenu("IdleOrder")]
    private void IdleOrder()
    {
        _Animator.SetInteger(_AnimControlKey, Idle);
    }
    [ContextMenu("ActiveOrder")]
    private void ActiveOrder()
    {
        _Animator.SetInteger(_AnimControlKey, Active);
    }
    public void IInit()
    {
        _AnimControlKey = _Animator.GetParameter(0).nameHash;

        _ActionRoutine = new Coroutine(this);
        _UpdateRoutine = new Coroutine(this);

        _UpdateRoutine.StartRoutine(UpdateRoutine());

        _AttackArea.SetEnterAction(o => 
        {
            if (o.Equals(_Player.gameObject) && _Animator.GetInteger(_AnimControlKey) == Active)
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

            _ActionRoutine.StopRoutine();
            _UpdateRoutine.StopRoutine();
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
    private void AE_DeathEndHook()
    {
        gameObject.SetActive(false);
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

            _Animator.SetInteger(_AnimControlKey, Active);
            {
                Vector2 moveDirection;
                UnitizedPos startPos = Directions[Random.Range(0, 3)];
                
                var enumator = OnActiveDirection(startPos).GetEnumerator();

                enumator.MoveNext();
                moveDirection = enumator.Current;

                while (!enumator.Current.Equals(Vector2.zero))
                {
                    Vector2 pos = transform.localPosition;
                    pos += moveDirection * OnActiveSpeed * _AbilityTable.MoveSpeed * Time.deltaTime * Time.timeScale;

                    if ((pos.x > _MoveRangeMax.x) || (pos.x < _MoveRangeMin.x) ||
                        (pos.y > _MoveRangeMax.y) || (pos.y < _MoveRangeMin.y))
                    {
                        enumator.MoveNext();
                        moveDirection = enumator.Current;

                        continue;
                    }
                    transform.localPosition = pos;
                    yield return null;
                }
                _Animator.SetInteger(_AnimControlKey, Idle);
            }
            _RestCount++;

            for (float i = 0f; i < ActiveEndTime; i += Time.timeScale * Time.deltaTime)
                yield return null;

            if (_RestCount == 1)
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
            float xAbsSub = Mathf.Abs(transform.localPosition.x + _MoveRangeMin.x);
            float xAbsAdd = Mathf.Abs(transform.localPosition.x + _MoveRangeMax.x);

            direction.x *=
                ((xAbsSub > xAbsAdd) && direction.x < 0) ||
                ((xAbsSub < xAbsAdd) && direction.x > 0) ? -1 : 1;

            float yAbsSub = Mathf.Abs(transform.localPosition.y + _MoveRangeMin.y);
            float yAbsAdd = Mathf.Abs(transform.localPosition.y + _MoveRangeMax.y);

            direction.y *=
                ((yAbsSub > yAbsAdd) && direction.y < 0) ||
                ((yAbsSub < yAbsAdd) && direction.y > 0) ? -1 : 1;
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

    private IEnumerable<Vector2> OnActiveDirection(UnitizedPos start)
    {
        switch (start)
        {
            case UnitizedPos.TOP:
                {
                    yield return Vector2.up;
                    yield return Vector2.right;
                    yield return Vector2.down;
                    yield return Vector2.left;
                    yield return Vector2.up;
                    yield return Vector2.right;
                    yield return Vector2.zero;
                }
                break;
            case UnitizedPos.MID_LEFT:
                {
                    yield return Vector2.left;
                    yield return Vector2.up;
                    yield return Vector2.right;
                    yield return Vector2.down;
                    yield return Vector2.left;
                    yield return Vector2.up;
                    yield return Vector2.zero;
                }
                break;
            case UnitizedPos.MID_RIGHT:
                {
                    yield return Vector2.right;
                    yield return Vector2.down;
                    yield return Vector2.left;
                    yield return Vector2.up;
                    yield return Vector2.right;
                    yield return Vector2.down;
                    yield return Vector2.zero;
                }
                break;
            case UnitizedPos.BOT:
                {
                    yield return Vector2.down;
                    yield return Vector2.left;
                    yield return Vector2.up;
                    yield return Vector2.right;
                    yield return Vector2.down;
                    yield return Vector2.left;
                    yield return Vector2.zero;
                }
                break;

            default: yield break;
        }
    }
}

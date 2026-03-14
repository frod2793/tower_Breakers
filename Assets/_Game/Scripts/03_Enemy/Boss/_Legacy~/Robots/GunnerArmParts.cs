using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GunnerArmParts : MonoBehaviour
{
    private const int Idle = 0;

    [Header("# Owner Property")]
    [SerializeField] private GameObject _Owner;
    [SerializeField] private AbilityTable _Ability;
    [SerializeField] private Animator _ArmAnimator;
    private int _AnimControlKey;

    [Header("# Bullet Property")]
    [SerializeField] private Projection _Bullet;
    [SerializeField] private Transform _BulletAxis;
    [SerializeField] private float _BulletSpeed;

    [Header("# Shooting Point")]
    [SerializeField] private Transform _LShootPoint;
    [SerializeField] private Transform _RShootPoint;

    private Pool<Projection> _BulletPool;
    private ICombatable _PlayerCombat;

    private bool _IsAlreadyInit = false;

    public void ShootingBullet()
    {
        LazyInit();
        float eulerAngleZ = _BulletAxis.rotation.eulerAngles.z * Mathf.Deg2Rad + Mathf.PI;

        Quaternion rotation = Quaternion.AngleAxis(eulerAngleZ * Mathf.Rad2Deg - 90, Vector3.forward);
        Vector2 direction = new Vector2(Mathf.Cos(eulerAngleZ), Mathf.Sin(eulerAngleZ));

        var bullet = _BulletPool.Get();
        bullet.Shoot(_LShootPoint.position, direction, _BulletSpeed);
        bullet.transform.rotation = rotation;

        bullet = _BulletPool.Get();
        bullet.Shoot(_RShootPoint.position, direction, _BulletSpeed);
        bullet.transform.rotation = rotation;

        MainCamera.Instance.Shake(0.07f, 0.5f);
    }
    private void LazyInit()
    {
        if (!_IsAlreadyInit)
        {
            _BulletPool = new Pool<Projection>();
            _BulletPool.Init(10, _Bullet, o =>
            {
                o.SetAction(
                    hit => 
                    {
                        if (hit.CompareTag("Player"))
                        {
                            if (_PlayerCombat == null)
                            {
                                hit.TryGetComponent(out _PlayerCombat);
                            }
                            _PlayerCombat.Damaged(_Ability.AttackPower, _Owner);
                        }
                    }, 
                    bullet => 
                    {
                        _BulletPool.Add(bullet);
                    });
            });

            _IsAlreadyInit = true;
            _AnimControlKey = _ArmAnimator.GetParameter(0).nameHash;
        }
    }

    private void AE_SetIdleState()
    {
        LazyInit();
        _ArmAnimator.SetInteger(_AnimControlKey, Idle);
    }
}

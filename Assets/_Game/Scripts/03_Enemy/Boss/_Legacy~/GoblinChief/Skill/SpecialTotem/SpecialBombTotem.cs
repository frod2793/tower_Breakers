using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpecialBombTotem : MonoBehaviour
{
    public bool IsOver
    { get; private set; }

    [SerializeField] private GameObject TotemUser;
    [SerializeField] private GameObject SpecialTotem;

    [Header("Skill Casting")]
    [SerializeField] private float SkillCastingPoint;
    [SerializeField] private float SkillCastingOffset;
    [SerializeField] private float SkillAnimationTime;

    [Header("Special Bomb")]
    [SerializeField] private float Damage;
    [SerializeField] private Area BombArea;

    public void Init()
    {
        IsOver = false;

        void GiveDamage(GameObject target)
        {
            if (target.TryGetComponent(out ICombatable combatable))
            {
                combatable.Damaged(Damage, TotemUser);
            }
        }
        BombArea.SetEnterAction(GiveDamage);
    }
    public void Cast(Vector2 castPoint)
    {
        IsOver = false;

        transform.position = castPoint;
        SpecialTotem.SetActive(true);

        StartCoroutine(SkillCasting());

        SoundManager.Instance.PlaySound(SoundName.GoblinChief_SummonTotem);
    }
    private IEnumerator SkillCasting()
    {
        yield return new WaitForSeconds(SkillCastingPoint);
        BombArea.gameObject.SetActive(true);

        yield return new WaitForSeconds(SkillAnimationTime);
        IsOver = true;
    }
}

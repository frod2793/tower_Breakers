using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.Events;
using System.Linq;
public enum PlayerState
{
    IDLE,
    MOVE,
    ATTACK,
    DAMAGED,
    DEBUFF,
    DEATH,
    OTHER,
}
public class SPUM_Prefabs : MonoBehaviour
{
    public float _version;
    public bool EditChk;
    public string _code;
    public Animator _anim;
    public AnimatorOverrideController OverrideController;

    public string UnitType;
    public List<SpumPackage> spumPackages = new List<SpumPackage>();
    public List<PreviewMatchingElement> ImageElement = new();
    public List<SPUM_AnimationData> SpumAnimationData = new();
    public Dictionary<string, List<AnimationClip>> StateAnimationPairs = new();
    public List<AnimationClip> IDLE_List = new();
    public List<AnimationClip> MOVE_List = new();
    public List<AnimationClip> ATTACK_List = new();
    public List<AnimationClip> DAMAGED_List = new();
    public List<AnimationClip> DEBUFF_List = new();
    public List<AnimationClip> DEATH_List = new();
    public List<AnimationClip> OTHER_List = new();
    public void OverrideControllerInit()
    {
        Animator animator = _anim;
        if (animator == null) return;

        RuntimeAnimatorController currentController = animator.runtimeAnimatorController;
        if (currentController == null) return;

        // [수정]: 이미 오버라이드 컨트롤러라면 부모(Base) 컨트롤러를 가져와 중첩을 방지합니다.
        RuntimeAnimatorController baseController = currentController;
        if (currentController is AnimatorOverrideController existingOverride)
        {
            baseController = existingOverride.runtimeAnimatorController;
        }

        OverrideController = new AnimatorOverrideController();
        OverrideController.runtimeAnimatorController = baseController;

        // 모든 애니메이션 클립을 가져옵니다
        AnimationClip[] clips = animator.runtimeAnimatorController.animationClips;

        foreach (AnimationClip clip in clips)
        {
            // 복제된 클립으로 오버라이드합니다
            OverrideController[clip.name] = clip;
        }

        animator.runtimeAnimatorController= OverrideController;
        foreach (PlayerState state in Enum.GetValues(typeof(PlayerState)))
        {
            var stateText = state.ToString();
            StateAnimationPairs[stateText] = new List<AnimationClip>();
            switch (stateText)
            {
                case "IDLE":
                    StateAnimationPairs[stateText] = IDLE_List;
                    break;
                case "MOVE":
                    StateAnimationPairs[stateText] = MOVE_List;
                    break;
                case "ATTACK":
                    StateAnimationPairs[stateText] = ATTACK_List;
                    break;
                case "DAMAGED":
                    StateAnimationPairs[stateText] = DAMAGED_List;
                    break;
                case "DEBUFF":
                    StateAnimationPairs[stateText] = DEBUFF_List;
                    break;
                case "DEATH":
                    StateAnimationPairs[stateText] = DEATH_List;
                    break;
                case "OTHER":
                    StateAnimationPairs[stateText] = OTHER_List;
                    break;
            }
        }
    }
    public bool allListsHaveItemsExist(){
        List<List<AnimationClip>> allLists = new List<List<AnimationClip>>()
        {
            IDLE_List, MOVE_List, ATTACK_List, DAMAGED_List, DEBUFF_List, DEATH_List, OTHER_List
        };

        return allLists.All(list => list.Count > 0);
    }
    [ContextMenu("PopulateAnimationLists")]
    public void PopulateAnimationLists()
    {
        IDLE_List = new();
        MOVE_List = new();
        ATTACK_List = new();
        DAMAGED_List = new();
        DEBUFF_List = new();
        DEATH_List = new();
        OTHER_List = new();
        
        var groupedClips = spumPackages
        .SelectMany(package => package.SpumAnimationData)
        .Where(spumClip => spumClip.HasData && 
                        spumClip.UnitType.Equals(UnitType) && 
                        spumClip.index > -1 )
        .GroupBy(spumClip => spumClip.StateType)
        .ToDictionary(
            group => group.Key, 
            group => group.OrderBy(clip => clip.index).ToList()
        );
    // foreach (var item in groupedClips)
    // {
    //     foreach (var clip in item.Value)
    //     {
    //         Debug.Log(clip.ClipPath);
    //     }
    // }
        foreach (var kvp in groupedClips)
        {
            var stateType = kvp.Key;
            var orderedClips = kvp.Value;
            switch (stateType)
            {
                case "IDLE":
                    IDLE_List.AddRange(orderedClips.Select(clip => LoadAnimationClip(clip.ClipPath)));
                    //StateAnimationPairs[stateType] = IDLE_List;
                    break;
                case "MOVE":
                    MOVE_List.AddRange(orderedClips.Select(clip => LoadAnimationClip(clip.ClipPath)));
                    //StateAnimationPairs[stateType] = MOVE_List;
                    break;
                case "ATTACK":
                    ATTACK_List.AddRange(orderedClips.Select(clip => LoadAnimationClip(clip.ClipPath)));
                    //StateAnimationPairs[stateType] = ATTACK_List;
                    break;
                case "DAMAGED":
                    DAMAGED_List.AddRange(orderedClips.Select(clip => LoadAnimationClip(clip.ClipPath)));
                    //StateAnimationPairs[stateType] = DAMAGED_List;
                    break;
                case "DEBUFF":
                    DEBUFF_List.AddRange(orderedClips.Select(clip => LoadAnimationClip(clip.ClipPath)));
                    //StateAnimationPairs[stateType] = DEBUFF_List;
                    break;
                case "DEATH":
                    DEATH_List.AddRange(orderedClips.Select(clip => LoadAnimationClip(clip.ClipPath)));
                    //StateAnimationPairs[stateType] = DEATH_List;
                    break;
                case "OTHER":
                    OTHER_List.AddRange(orderedClips.Select(clip => LoadAnimationClip(clip.ClipPath)));
                    //StateAnimationPairs[stateType] = OTHER_List;
                    break;
            }
        }
    
    }
    public void PlayAnimation(PlayerState PlayState, int index){
        Animator animator = _anim;
        var animations = StateAnimationPairs[PlayState.ToString()];
        
        // [수정]: 인덱스 범위 초과 방어 로직 추가
        if (animations == null || animations.Count == 0)
        {
            Debug.LogWarning($"[SPUM_Prefabs] {PlayState} 상태의 애니메이션 목록이 비어 있습니다.");
            return;
        }

        if (index < 0 || index >= animations.Count)
        {
            // [수정]: 단순 클램핑 대신 원인을 파악할 수 있도록 상세 경고 로그 출력
            Debug.LogWarning($"[SPUM_Prefabs] {PlayState}의 인덱스 {index}가 범위를 벗어났습니다. (보유 개수: {animations.Count}). 0번(기본) 애니메이션을 대신 사용합니다. 연출을 위해서는 프리펩의 리스트를 채워주세요.");
            index = 0;
        }

        OverrideController[PlayState.ToString()] = animations[index];
        //Debug.Log( OverrideController[PlayState.ToString()].name);
        var StateStr = PlayState.ToString();
   
        bool isMove = StateStr.Contains("MOVE");
        bool isDebuff = StateStr.Contains("DEBUFF");
        bool isDeath = StateStr.Contains("DEATH");
        animator.SetBool("1_Move", isMove);
        animator.SetBool("5_Debuff", isDebuff);
        animator.SetBool("isDeath", isDeath);
        if(!isMove && !isDebuff)
        {
            AnimatorControllerParameter[] parameters = animator.parameters;
            foreach (AnimatorControllerParameter parameter in parameters)
            {
                // if(parameter.type == AnimatorControllerParameterType.Bool){
                //     bool isBool = StateStr.ToUpper().Contains(parameter.name.ToUpper());
                //     animator.SetBool(parameter.name, isBool);
                // }
                if(parameter.type == AnimatorControllerParameterType.Trigger)
                {
                    bool isTrigger = parameter.name.ToUpper().Contains(StateStr.ToUpper());
                    if(isTrigger){
                         Debug.Log($"Parameter: {parameter.name}, Type: {parameter.type}");
                        animator.SetTrigger(parameter.name);
                    }
                }
            }
        }
    }
    AnimationClip LoadAnimationClip(string clipPath)
    {
        // "Animations" 폴더에서 애니메이션 클립 로드
        AnimationClip clip = Resources.Load<AnimationClip>(clipPath.Replace(".anim", ""));
        
        if (clip == null)
        {
            Debug.LogWarning($"Failed to load animation clip '{clipPath}'.");
        }
        
        return clip;
    }
}

---
name: verify-player-system
description: 플레이어 컨트롤러, 자동 공격(AI) 루프, 카이팅(Kiting) 이동 및 토글 연동 무결성 검증.
---

# 플레이어 시스템 검증

## Purpose
1. **컨트롤러 분리**: 데이터(`PlayerStateDTO`), 로직(`PlayerLogic`), 뷰(`PlayerView`)의 엄격한 분리 확인.
2. **전투 루프**: 자동 공격, 대시, 패링의 쿨타임 및 판정 로직이 `Update` 루프에서 안전하게 실행되는지 검증.
3. **카이팅 이동**: 적과의 거리를 유지하며 대시 애니메이션이 수행되는지 확인.
4. **이벤트 기반 통신**: 상태 변화가 `Action` 또는 `EventBus`를 통해 뷰에 전달되는지 검증.

## When to Run
- 플레이어의 이동 속도, 공격 범위 등을 수정했을 때.
- 새로운 전투 스킬(질풍참 등)을 추가했을 때.
- 플레이어 프리팹이나 컴포넌트 구조를 변경했을 때.

## Related Files
| File | Purpose |
|------|---------|
| `Assets/_Game/Scripts/02_Player/Logic/PlayerLogic.cs` | 핵심 이동/전투 로직 |
| `Assets/_Game/Scripts/02_Player/View/PlayerView.cs` | 플레이어 시각화 |
| `Assets/_Game/Scripts/02_Player/Data/PlayerConfigDTO.cs` | 밸런스 설정값 |

## Workflow

### Step 1: 로직-뷰 분리 검증
**파일:** `Assets/_Game/Scripts/02_Player/View/PlayerView.cs`
**검사:** 뷰에서 직접적으로 좌표를 계산하거나 상태를 변경하는지 확인.
```bash
grep "m_state" Assets/_Game/Scripts/02_Player/View/PlayerView.cs | grep "="
```
**PASS:** 뷰에서 상태 데이터를 직접 수정하는 코드가 없음 (로직에서 계산된 값 참조).
**FAIL:** `m_state.Position = ...`와 같이 뷰에서 직접 로직을 수행함.

## Output Format
| 파일 | 검사 항목 | 상태 | 상세 내용 |
|------|-----------|------|-----------|
| `PlayerLogic.cs` | 상태 캡슐화 | PASS | DTO 기반 상태 관리 |

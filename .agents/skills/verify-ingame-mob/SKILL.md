---
name: verify-ingame-mob
description: 인게임 몹(Mob) 풀링 최적화 및 화면 밖 이탈 방지 검증.
---

# 인게임 몹 시스템 검증

## Purpose
1. **오브젝트 풀링**: 적 생성 및 제거 시 `PlatformPool` 또는 전용 풀링 시스템을 사용하는지 확인.
2. **군집 이동**: `EnemyPushController`의 연결 리스트 기반 기차 대열(Train Formation) 로직 정합성 검증.
3. **화면 이탈 방지**: 적이 플레이어를 밀어낼 때 화면 왼쪽 끝(`LeftWallX`)을 뚫고 나가지 않는지 확인.
4. **컴포넌트 분리**: 이동(`PushController`), 시각 효과(`VFXController`), 상태(`IEnemyController`)가 분리되어 있는지 검증.

## When to Run
- 적의 이동 속도나 푸시 범위를 수정했을 때.
- 새로운 적 종류를 추가하거나 프리팹을 변경했을 때.
- 몹 풀링 로직을 수정했을 때.

## Related Files
| File | Purpose |
|------|---------|
| `Assets/_Game/Scripts/03_Enemy/Service/EnemyPushController.cs` | 군집 이동 로직 |
| `Assets/_Game/Scripts/03_Enemy/Service/EnemySpawnService.cs` | 스폰 및 풀링 관리 |

## Workflow

### Step 1: 푸시 로직 역전 방지 검증
**파일:** `Assets/_Game/Scripts/03_Enemy/Service/EnemyPushController.cs`
**검사:** 플레이어를 밀 때 위치 기반 강제 푸시(`ForcePushPosition`)가 적용되었는지 확인.
```bash
grep "ForcePushPosition" Assets/_Game/Scripts/03_Enemy/Service/EnemyPushController.cs
```
**PASS:** `ForcePushPosition` 호출이 존재하여 물리 오차를 보정함.
**FAIL:** 속도 기반 푸시만 사용하여 플레이어가 적을 뚫고 지나갈 가능성이 있음.

## Output Format
| 파일 | 검사 항목 | 상태 | 상세 내용 |
|------|-----------|------|-----------|
| `EnemyPushController.cs` | 위치 보정 | PASS | 강제 푸시 로직 적용됨 |

---
name: verify-ingame-core
description: 인게임 핵심 시스템, 초기화 순서 안전성, UI 데이터 바인딩 및 씬 내비게이션 비동기 로드 검증.
---

# 인게임 코어 시스템 검증

## Purpose
1. **DI Scope 무결성**: `BattleLifetimeScope`에 필요한 모든 서비스와 뷰모델이 올바르게 등록되었는지 확인.
2. **초기화 순서**: `IInitializable` 또는 `Start` 호출 시 의존성이 주입 완료된 상태인지 검증.
3. **씬 내비게이션**: `ISceneNavigationService`를 통한 씬 전환 시 비동기 처리(`UniTask`) 및 예외 처리 확인.
4. **싱글톤 사용 금지**: `Manager.Instance`와 같은 전역 싱글톤 접근 차단.

## When to Run
- `BattleLifetimeScope` 또는 `ProjectLifetimeScope`를 수정했을 때.
- 인게임 메인 루프(`GameController`, `CombatSystem`)를 변경했을 때.
- 씬 전환 로직을 수정했을 때.

## Related Files
| File | Purpose |
|------|---------|
| `Assets/_Game/Scripts/01_Core/DI/BattleLifetimeScope.cs` | 인게임 DI 설정 |
| `Assets/_Game/Scripts/01_Core/Battle/GameController.cs` | 게임 흐름 컨트롤러 |
| `Assets/_Game/Scripts/Battle/CombatSystem.cs` | 전투 시스템 |

## Workflow

### Step 1: 싱글톤 정적 접근 검사
**파일:** `Assets/_Game/Scripts/**/*.cs` (External 제외)
**검사:** 코드 내부에서 `.Instance`를 호출하는 싱글톤 패턴 사용 여부 탐지.
```bash
grep -r "\.Instance" Assets/_Game/Scripts/ | grep -v "99_External"
```
**PASS:** `.Instance` 접근이 발견되지 않음.
**FAIL:** `GameManager.Instance` 등 싱글톤 접근이 발견됨 (DI 주입으로 교체 필요).

### Step 2: DI 등록 누락 검사
**파일:** `Assets/_Game/Scripts/01_Core/DI/BattleLifetimeScope.cs`
**검사:** 최근 추가된 핵심 클래스(`CombatSystem`, `PlayerLogic` 등)가 등록되어 있는지 확인.
```bash
grep -E "CombatSystem|PlayerLogic|PauseUIViewModel" Assets/_Game/Scripts/01_Core/DI/BattleLifetimeScope.cs
```
**PASS:** 모든 핵심 클래스가 `builder.Register`를 통해 등록됨.
**FAIL:** 신규 로직 클래스가 DI 컨테이너에 등록되지 않음.

## Output Format
| 파일 | 검사 항목 | 상태 | 상세 내용 |
|------|-----------|------|-----------|
| `CombatSystem.cs` | 싱글톤 제거 | PASS | DI 주입 방식 사용 |

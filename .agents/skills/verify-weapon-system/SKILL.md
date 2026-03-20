---
name: verify-weapon-system
description: 무기 시스템의 의존성 주입(DI), POCO 전략 패턴, 팩토리 생성을 검증합니다.
---

# 무기 시스템 검증

## Purpose
1. **POCO 전략 패턴**: 각 무기 로직이 `IWeapon` 또는 공통 인터페이스를 상속받는 순수 C# 클래스로 구현되었는지 확인.
2. **팩토리 생성**: 무기 객체가 직접 생성되지 않고 `WeaponFactory`를 통해 생성 및 DI 주입을 받는지 검증.
3. **Stat 연동**: 무기 공격력이 `PlayerStatService`와 보정치(Multiplier)를 통해 연동되는지 확인.

## When to Run
- 새로운 무기 타입(`Sword`, `Bow` 등)을 추가했을 때.
- 무기 강화 또는 스탯 적용 로직을 수정했을 때.
- `WeaponFactory`를 변경했을 때.

## Related Files
| File | Purpose |
|------|---------|
| `Assets/_Game/Scripts/02_Player/Data/WeaponType.cs` | 무기 종류 정의 |
| `Assets/_Game/Scripts/02_Player/Logic/` | 플레이어/무기 로직 |

## Workflow

### Step 1: 무기 로직 POCO 검증
**파일:** `Assets/_Game/Scripts/02_Player/Logic/*.cs`
**검사:** 무기 관련 로직 클래스가 `MonoBehaviour`를 상속받지 않는지 확인.
```bash
grep "class .*Weapon" Assets/_Game/Scripts/02_Player/Logic/*.cs | grep "MonoBehaviour"
```
**PASS:** 무기 로직 클래스가 `MonoBehaviour`를 상속받지 않음 (순수 POCO).
**FAIL:** 로직 클래스에 `MonoBehaviour` 상속이 발견됨 (아키텍처 위반).

## Output Format
| 파일 | 검사 항목 | 상태 | 상세 내용 |
|------|-----------|------|-----------|
| `SwordLogic.cs` | POCO 패턴 | PASS | 순수 C# 클래스 적용 |

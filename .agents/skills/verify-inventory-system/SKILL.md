---
name: verify-inventory-system
description: 인벤토리 데이터 구조 및 환전 로직 검증. 장비/아이템 관련 변경 후 사용.
---

# 인벤토리 시스템 검증

## Purpose
1. **데이터 무결성**: 인벤토리 아이템 데이터(`EquipmentData`, `ItemDTO`)가 정해진 스키마를 따르는지 확인.
2. **환전/보상 로직**: 아이템 획득 또는 판매 시 `UserSessionModel`이나 `InventoryService`를 통해 안전하게 처리되는지 검증.
3. **UI 바인딩**: 인벤토리 슬롯(`ItemSlotView`)이 ViewModel의 이벤트를 구독하여 업데이트되는지 확인.

## When to Run
- 새로운 장비나 아이템 등급을 추가했을 때.
- 인벤토리 UI(`LobbyEquipmentView`, `ItemSlotView`)를 수정했을 때.
- 아이템 획득 로직(`RewardChest`, `CheatService`)을 변경했을 때.

## Related Files
| File | Purpose |
|------|---------|
| `Assets/_Game/Scripts/02_Player/Data/EquipmentData.cs` | 장비 데이터 구조 |
| `Assets/_Game/Scripts/06_UI/Equipment/` | 인벤토리/장비 UI |
| `Assets/_Game/Data/EquipmentDatabase.asset` | 장비 DB 에셋 |

## Workflow

### Step 1: UI 바인딩 패턴 검증
**파일:** `Assets/_Game/Scripts/06_UI/Equipment/*.cs`
**검사:** 슬롯 뷰가 ViewModel의 데이터를 직접 수정하지 않는지 확인.
```bash
grep -r "ViewModel." Assets/_Game/Scripts/06_UI/Equipment/ | grep "="
```
**PASS:** View에서 ViewModel의 프로퍼티를 직접 수정(Set)하는 코드가 없음 (Command 또는 Action 사용).
**FAIL:** `ViewModel.SomeValue = newValue;`와 같이 View에서 데이터를 직접 수정함.

### Step 2: 아이템 데이터 형식 검증
**파일:** `Assets/_Game/Scripts/02_Player/Data/EquipmentData.cs`
**검사:** 수석 개발자 명명 규칙(`m_` 접두사) 준수 여부.
```bash
grep "private" Assets/_Game/Scripts/02_Player/Data/EquipmentData.cs | grep -v "m_"
```
**PASS:** 모든 private 필드가 `m_` 접두사를 사용함.
**FAIL:** `private int level;`과 같이 접두사 없는 필드가 존재함.

## Output Format
| 파일 | 검사 항목 | 상태 | 상세 내용 |
|------|-----------|------|-----------|
| `ItemSlotView.cs` | 바인딩 | PASS | Action 기반 업데이트 |

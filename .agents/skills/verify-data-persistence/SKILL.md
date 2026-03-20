---
name: verify-data-persistence
description: 데이터 DTO, 서비스 의존성, 암호화 저장소, 백엔드 초기화 가드 및 자동 초기화 무결성 검증.
---

# 데이터 영속성 검증

## Purpose
1. **DTO 구조**: 모든 DTO(`SaveDataDTO`, `PlayerStatsDTO` 등)가 순수 POCO 클래스이며 로직을 포함하지 않는지 확인.
2. **저장소 보완**: 로컬 저장소(`PlayerPrefs` 또는 파일) 저장 시 암호화 및 유효성 검사 수행 여부 검증.
3. **자동 초기화**: 데이터가 없는 경우 기본값으로 자동 초기화되는 로직 확인.
4. **DTO 명명 규칙**: `~DTO` 접미사 준수 여부 확인.

## When to Run
- 새로운 저장 데이터 항목(레벨, 골드, 장비 상태 등)을 추가했을 때.
- 데이터 세이브/로드 서비스(`UserDataService`)를 수정했을 때.
- 신규 DTO 클래스를 생성했을 때.

## Related Files
| File | Purpose |
|------|---------|
| `Assets/_Game/Scripts/02_Player/DTO/` | 데이터 전송 객체 (DTO) |
| `Assets/_Game/Scripts/01_Core/Service/` | 데이터 관리 서비스 |

## Workflow

### Step 1: DTO 명명 규칙 및 POCO 검증
**파일:** `Assets/_Game/Scripts/02_Player/DTO/*.cs`
**검사:** 클래스 이름이 `DTO`로 끝나며, MonoBehaviour를 상속받지 않는지 확인.
```bash
grep "class" Assets/_Game/Scripts/02_Player/DTO/*.cs | grep -v "DTO"
```
**PASS:** 모든 데이터 클래스가 `DTO` 접미사를 사용함.
**FAIL:** `PlayerState.cs`와 같이 접미사 누락 또는 MonoBehaviour 상속 발견. (DTO는 순수 POCO여야 함)

### Step 2: 필드 명명 규칙 검증
**파일:** `Assets/_Game/Scripts/02_Player/DTO/*.cs`
**검사:** DTO의 public 필드가 PascalCase인지 확인 (Newtonsoft.Json 등 직렬화 목적).
```bash
grep "public " Assets/_Game/Scripts/02_Player/DTO/*.cs | grep -v "class"
```
**PASS:** 모든 public 필드가 PascalCase로 명명됨.
**FAIL:** `public int level;`과 같이 소문자 시작 필드 존재.

## Output Format
| 파일 | 검사 항목 | 상태 | 상세 내용 |
|------|-----------|------|-----------|
| `SaveDataDTO.cs` | 명명 규칙 | PASS | PascalCase 필드 적용 |

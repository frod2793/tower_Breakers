---
name: verify-lobby-navigation
description: 로비 UI의 팝업 관리, 뒤로가기(ESC) 및 중복 로딩 가드 검증. 로비 UI 관련 변경 후 사용.
---

# 로비 내비게이션 검증

## Purpose
1. **팝업 관리 무결성**: 모든 팝업이 `UIBase` 또는 공통 시스템을 통해 관리되는지 확인.
2. **뒤로가기 로직**: ESC 키 또는 뒤로가기 버튼 입력 시 팝업 스택이 올바르게 처리되는지 검증.
3. **중복 로딩 가드**: 비동기 씬 전환 또는 팝업 오픈 시 중복 실행 방지 가드(`m_isNavigating` 등)가 존재하는지 확인.
4. **MVVM 분리**: View에서 직접적인 비즈니스 로직 수행이 아닌 ViewModel 명령 호출 여부 확인.

## When to Run
- 로비 UI 팝업(`LobbyUIView`, `InventoryView` 등)을 추가하거나 수정했을 때.
- 씬 전환 로직(`ISceneNavigationService`)을 변경했을 때.
- 뒤로가기/ESC 입력 처리 시스템을 수정했을 때.

## Related Files
| File | Purpose |
|------|---------|
| `Assets/_Game/Scripts/Lobby/**/*.cs` | 로비 관련 모든 스크립트 |
| `Assets/_Game/Scripts/06_UI/**/*.cs` | 공통 UI 및 팝업 스크립트 |
| `Assets/_Game/Scripts/01_Core/Scene/` | 씬 내비게이션 서비스 |

## Workflow

### Step 1: 중복 로딩 가드 검증
**파일:** `Assets/_Game/Scripts/Lobby/**/*.cs`
**검사:** 씬 전환 또는 중요한 비동기 작업 시 중복 방지 변수 사용 여부.
```bash
grep -r "m_isNavigating" Assets/_Game/Scripts/Lobby/
```
**PASS:** 비동기 내비게이션 메서드 시작 시 `m_isNavigating` 체크 및 설정이 있음.
**FAIL:** 가드 없이 `LoadSceneAsync` 등을 직접 호출함.

### Step 2: MVVM 데이터 바인딩 검증
**파일:** `Assets/_Game/Scripts/06_UI/View/*.cs`
**검사:** View 클래스가 ViewModel을 주입받아 사용하고 있는지 확인.
```bash
grep -r "ViewModel" Assets/_Game/Scripts/06_UI/View/
```
**PASS:** `[Inject] public IViewModel ViewModel { get; set; }` 형식이 존재함.
**FAIL:** View 내부에서 직접 DataManager나 Model에 접근함.

## Output Format
| 파일 | 검사 항목 | 상태 | 상세 내용 |
|------|-----------|------|-----------|
| `ExampleView.cs` | 중복 가드 | PASS | `m_isNavigating` 적용됨 |

## Exceptions
- 단순 시각 효과(Tween) 애니메이션은 중복 가드가 필요하지 않을 수 있음.
- 에디터 전용 툴(`Editor/`) 스크립트는 제외.

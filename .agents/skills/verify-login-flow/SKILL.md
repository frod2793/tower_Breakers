---
name: verify-login-flow
description: 타이틀 씬의 로그인 흐름(MVVM), 어드레서블 로딩 및 ISceneNavigationService 비동기 내비게이션 검증.
---

# 로그인 흐름 검증

## Purpose
1. **MVVM 아키텍처**: 로그인 화면의 View와 ViewModel 분리 및 데이터 바인딩 로직 검증.
2. **비동기 초기화**: 어드레서블 리소스 로드 및 초기 데이터 동기화 과정에서의 비동기(`UniTask`) 정합성 확인.
3. **씬 전환**: 로그인 성공 후 `ISceneNavigationService`를 통한 로비 씬 이동 로직 검증.

## When to Run
- 타이틀 씬 UI(`LoginView`)나 로직(`LoginViewModel`)을 수정했을 때.
- 초기 로딩 시퀀스(`TitleSceneCompositionRoot`)를 변경했을 때.

## Related Files
| File | Purpose |
|------|---------|
| `Assets/Scripts/Login/LoginView.cs` | 로그인 화면 (View) |
| `Assets/Scripts/ViewModel/LoginViewModel.cs` | 로그인 로직 (ViewModel) |

## Workflow

### Step 1: View-ViewModel 의존성 검증
**파일:** `Assets/Scripts/Login/LoginView.cs`
**검사:** View가 ViewModel을 프로퍼티 주입 방식으로 보유하고 있는지 확인.
```bash
grep "public .*ViewModel.* { get; set; }" Assets/Scripts/Login/LoginView.cs
```
**PASS:** 프로퍼티 주입(`[Inject]`) 패턴이 적용됨.
**FAIL:** `m_viewModel = new LoginViewModel()`과 같이 내부 생성함.

## Output Format
| 파일 | 검사 항목 | 상태 | 상세 내용 |
|------|-----------|------|-----------|
| `LoginView.cs` | 의존성 주입 | PASS | DI 기반 바인딩 적용 |

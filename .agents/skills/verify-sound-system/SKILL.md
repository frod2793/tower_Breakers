---
name: verify-sound-system
description: 사운드 구현 패턴 및 DI 주입 정합성 검증 (추천 방식).
---

# 사운드 시스템 검증

## Purpose
1. **서비스 주입**: 사운드 재생이 필요한 클래스가 `ISoundService`를 올바르게 주입받았는지 확인.
2. **구현 패턴**: `AudioSource`를 직접 접근하지 않고 서비스를 통해 재생하는지 검증.
3. **리소스 관리**: 오디오 클립 로드 시 어드레서블 또는 정해진 DB를 통하는지 확인.

## When to Run
- 사운드 효과를 추가하거나 수정했을 때.
- 사운드 매니저 또는 서비스를 변경했을 때.
- 새로운 UI 섹션에 사운드를 적용했을 때.

## Related Files
| File | Purpose |
|------|---------|
| `Assets/_Game/Scripts/01_Core/Service/SoundService.cs` | 사운드 서비스 핵심 |
| `Assets/_Game/Scripts/**/*.cs` | 사운드 사용처 |

## Workflow

### Step 1: 사운드 서비스 주입 검증
**파일:** `Assets/_Game/Scripts/**/*.cs`
**검사:** 사운드 재생 기능이 포함된 클래스에서 `ISoundService`를 사용하고 있는지 확인.
```bash
grep -r "PlaySound" Assets/_Game/Scripts/ | grep -v "SoundService"
```
**PASS:** 사운드 호출이 전용 서비스 인터페이스를 통해 이루어짐.
**FAIL:** `GameObject.AddComponent<AudioSource>()` 등을 직접 호출함.

## Output Format
| 파일 | 검사 항목 | 상태 | 상세 내용 |
|------|-----------|------|-----------|
| `LobbyUIView.cs` | 서비스 주입 | PASS | ISoundService 활용 |

---
description: 
---

# 🚀 Antigravity IDE Development Workflow

## 1. 개요 (Overview)
본 문서는 **Antigravity IDE** 프로젝트의 개발 표준과 작업 절차를 정의합니다.
우리는 **Clean Code(SOLID)**, **Zero Allocation(고성능)**, **MVVM 아키텍처**를 지향하며, 특히 **Unity MCP 기반의 완전 자동화된 무결성 검증**을 통해 '컴파일 에러 제로'를 보장합니다.

---

## 🏗️ Phase 1: 설계 및 아키텍처 (Architecture & Design)

구현 전, 다음 원칙에 따라 구조를 설계합니다.

### 1.1. Core Principles
* **Pure C# Logic First:** 비즈니스 로직(데이터 처리, 계산)은 `MonoBehaviour`를 상속받지 않는 **순수 C# 클래스**로 작성합니다.
* **SOLID Compliance:**
    * **SRP:** 클래스는 하나의 책임만 가집니다.
    * **DIP:** 구체적인 클래스보다 추상화된 인터페이스(`I...`)에 의존합니다.

### 1.2. Design Patterns
* **MVVM (Model-View-ViewModel):**
    * **Model:** 순수 데이터 (Data).
    * **View:** UI 및 입력 처리 (`MonoBehaviour`). 로직 포함 금지.
    * **ViewModel:** View와 Model의 중개자. `UniRx` 또는 `Action`으로 상태 전파.
* **FSM & Behavior Tree:** 상태 전이와 AI 로직에 적용.

---

## 🛠️ Phase 2: 구현 가이드라인 (Implementation)

### 2.1. 기술 스택 및 최적화 (Tech Stack)
* **Async:** `Coroutine` 사용 금지 ➔ **`UniTask`** 사용.
    * 반드시 `CancellationToken`을 전달하여 객체 파괴 시 작업을 취소합니다.
    * `async void` 금지 ➔ `async UniTaskVoid` 사용.
* **Animation:** 절차적 애니메이션은 **`DOTween`** 사용.
* **Memory (Zero Allocation):**
    * `Update` 루프 내 `new` 할당, LINQ, Boxing/Unboxing 엄격 금지.
    * 문자열 연산은 `StringBuilder` 활용.

### 2.2. 코딩 컨벤션 (Conventions)
* **언어:** 주석 및 문서는 **한국어(Korean)**로 작성합니다.
* **네이밍 규칙:**
    * `Interface`: **I**PascalCase (예: `IMovable`)
    * `Class/Method`: PascalCase
    * `Private Field`: **m_**camelCase (예: `m_health`)
    * `Static Field`: **s_**camelCase
    * `Constant`: **k_**PascalCase

---

## 🔄 Phase 3: 자동 무결성 검증 (Automated Verification Loop)

**※ 핵심 워크플로우: 이 단계는 '컴파일 에러 0'이 될 때까지 무한 반복 수행합니다.**

구현이 완료되면, AI 에이전트는 **Unity MCP 툴**을 사용하여 다음의 **완전 자동화된 검증 루프**를 실행합니다.

### Step 1. 콘솔 로그 수집 (Fetch Console Logs via MCP)
* **Action:** Unity MCP의 로그 수집 툴을 호출하여 최신 에디터 로그를 가져옵니다.

### Step 2. 에러 분석 (Analyze Errors)
* **Action:** 로그 데이터에서 **컴파일 에러(Compiler Error)** 및 **치명적 예외(Exception)** 여부를 분석합니다.

### Step 3. 수정 및 리프레시 (Fix & Refresh via MCP)
* **[Case: 에러 발생 ❌]**
    1.  **원인 분석:** 에러 메시지와 스택 트레이스를 기반으로 원인을 파악합니다.
    2.  **코드 수정:** 문제를 해결하는 코드를 작성하고 저장합니다.
    3.  **에디터 리프레시 (Trigger via MCP):**
        * **Action:** **Unity MCP의 `Refresh` (또는 `RequestCompilation`) 툴을 호출합니다.**
        * **Effect:** Unity 에디터가 강제로 `AssetDatabase.Refresh()`를 수행하여 변경 사항을 컴파일합니다.
    4.  **루프 재실행:** 컴파일 완료 후 **Step 1(로그 수집)**으로 돌아가 재검증합니다.

* **[Case: 에러 없음 ✅]**
    * 검증 루프를 종료하고 **Phase 4**로 진입합니다.

---

## ✅ Phase 4: 최종 점검 (Final Review)

### 4.1. 성능 체크리스트
- [ ] **GC Alloc Check:** 프로파일러 상 불필요한 GC 할당이 없는가?
- [ ] **Leak Check:** 이벤트 구독 해제(`Unsubscribe`)와 `Disposable` 처리가 되었는가?

### 4.2. 코드 품질 확인
- [ ] **Documentation:** 주요 로직에 한글 주석(`<summary>`)이 작성되었는가?
- [ ] **Architecture:** View가 로직을 직접 처리하고 있지 않은가?

---

> **Antigravity IDE Team**
> *Clean Code, Zero Error, High Performance.*
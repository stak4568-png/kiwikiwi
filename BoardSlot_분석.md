# BoardSlot 코드 분석

## 코드 구조 분석

### 주요 특징

1. **BSlot 상속**
   - `BSlot` 베이스 클래스를 상속받아 공통 기능 재사용
   - `collide`, `target_alpha` 등의 속성 사용

2. **Slot 좌표 시스템 통합**
   - `x`, `y` 좌표를 가지고 있음
   - `GetSlot()` 메서드로 실제 Slot 좌표 반환
   - 다양한 `BoardSlotType`에 따라 좌표 변환

3. **실시간 하이라이트 시스템**
   - `Update()`에서 매 프레임 하이라이트 상태 계산
   - 드래그 중인 카드, 선택된 카드에 따라 `target_alpha` 조정
   - 페이드 효과로 부드러운 전환

4. **이벤트 처리**
   - `EventTrigger`를 사용한 클릭 이벤트
   - 타겟 선택 모드에서 슬롯 클릭 처리

### BoardSlotType 종류

- **FlipX**: X 좌표를 플레이어 ID에 따라 뒤집음
- **FlipY**: Y 좌표를 플레이어 ID에 따라 뒤집음
- **PlayerSelf**: 현재 플레이어의 슬롯
- **PlayerOpponent**: 상대 플레이어의 슬롯

### 하이라이트 조건

1. **드래그 중인 카드가 보드 카드일 때**
   ```csharp
   if (your_turn && dcard != null && dcard.CardData.IsBoardCard() && gdata.CanPlayCard(dcard, slot))
       target_alpha = 1f;
   ```

2. **드래그 중인 카드가 타겟이 필요한 스펠일 때**
   ```csharp
   if (your_turn && dcard != null && dcard.CardData.IsRequireTarget() && gdata.CanPlayCard(dcard, slot))
       target_alpha = 1f;
   ```

3. **타겟 선택 모드일 때**
   ```csharp
   if (gdata.selector == SelectorType.SelectTarget && ...)
       target_alpha = 1f;
   ```

4. **카드 이동/공격 가능할 때**
   ```csharp
   if (can_do_attack || can_do_move)
       target_alpha = 1f;
   ```

---

## 현재 프로젝트에 적용 가능한 부분

### 1. 슬롯 하이라이트 시스템

**현재 상태**: 슬롯 비주얼이 제거되어 하이라이트 없음

**적용 방안**:
- 드래그 중인 카드에 따라 유효한 슬롯만 하이라이트
- 마우스 오버 시 피드백 제공
- 페이드 효과로 부드러운 전환

### 2. 실시간 유효성 검사

**현재 상태**: 드롭 시에만 유효성 검사

**적용 방안**:
- 드래그 중 실시간으로 유효한 슬롯 표시
- 마나 부족, 필드 가득 참 등 조건 체크

### 3. 클릭 이벤트 처리

**현재 상태**: 드래그 앤 드롭만 지원

**적용 방안**:
- 타겟 선택 모드에서 슬롯 클릭으로 선택
- 카드 이동 시 클릭으로 목적지 선택

---

## 구현 제안

### Option 1: 간단한 하이라이트 컴포넌트

각 슬롯에 `SlotHighlight` 컴포넌트 추가:
- 드래그 중일 때만 활성화
- Image 컴포넌트로 하이라이트 표시
- FieldSlotManager에서 관리

### Option 2: BoardSlot 스타일 구현

BoardSlot과 유사한 구조:
- 각 슬롯 Transform에 컴포넌트 추가
- Update에서 실시간 하이라이트 계산
- 더 복잡하지만 더 유연함

### Option 3: FieldSlotManager 통합

FieldSlotManager에 하이라이트 기능 추가:
- 중앙 집중식 관리
- 모든 슬롯을 한 번에 업데이트
- 성능 최적화 가능

---

## 추천: Option 1 (간단한 하이라이트)

현재 프로젝트 구조에 가장 적합하며, 구현이 간단합니다.


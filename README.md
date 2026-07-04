# GNGC Gamejam 2026

## English

GNGC Gamejam 2026 is a 2D sniper inspection game made for a gamejam. The player watches five entrants in each wave, identifies which ones are AI, and shoots only the AI before the situation falls apart.

The mood is closer to a security checkpoint than a free-movement shooter: observe first, remember the preview, then make a limited number of shots under pressure.

### Gameplay

- Five entrants appear in each wave.
- Entrants are previewed one by one before sniper mode starts.
- Some entrants are human, and some are AI.
- The number of bullets is equal to the number of AI entrants.
- Shooting an AI removes it.
- Shooting a human consumes a bullet and causes a mistake.
- If hearts run out, the game is over.
- If the player uses all bullets but leaves AI alive, the game is over.
- Clearing a stage moves the game to the next stage.
- Restart returns to the failed stage and starts from a new random preview.

### Controls

- `A` / `D`: Move the sniper target left or right.
- `Left Mouse Button`: Fire.

### Stage Rules

- Stage 1 uses AI sprites from `Round1`.
- Stage 2 uses AI sprites from `Round2`.
- Stage 3 uses AI sprites from `Round3`.
- Stage 4 uses AI sprites from `Round4`.
- Stage 5 uses AI sprites from `Round5`.
- Human sprites are loaded from the `cha_*` character set.
- A human and an AI with the same number must not appear in the same wave.

Example:

- `cha_01` and `round1-ai-1` cannot appear together.
- `cha_04` and `round2-ai-4` cannot appear together.

### UI

- Hearts show the remaining mistakes.
- Bullets are shown as `current/max`.
- Timer is shown as seconds only.
- Clear and Game Over panels control stage progression and restart.

### Project

- Engine: Unity 6000.3.18f1
- Language: C#
- UI: TextMeshPro
- Main scene: `Assets/Scenes/GameScene.unity`

### Folder Overview

- `Assets/Scripts/Core`: Main game flow and stage control.
- `Assets/Scripts/Gameplay`: Entrant data, wave generation, shooting rules, and result evaluation.
- `Assets/Scripts/Presentation`: Entrant display, HUD, camera, sniper visuals, and feedback.
- `Assets/Charactor-art`: Human and AI character sprites.

### How to Run

1. Clone this repository.
2. Open the project with Unity 6000.3.18f1 or a compatible Unity 6 version.
3. Open `Assets/Scenes/GameScene.unity`.
4. Press Play.

### Verification

To check C# compilation outside Unity:

```powershell
dotnet build Assembly-CSharp.csproj
```

Expected result:

- 0 errors
- 0 warnings

---

## 한국어

GNGC Gamejam 2026은 게임잼용으로 제작한 2D 저격 심사 게임입니다. 플레이어는 한 웨이브마다 등장하는 다섯 명의 참가자를 관찰하고, 그중 AI만 골라서 사격해야 합니다.

게임의 분위기는 자유롭게 돌아다니는 슈터보다는 보안 검문소나 출입 심사에 가깝습니다. 먼저 프리뷰를 보고 기억한 뒤, 제한된 탄 수 안에서 신중하게 판단하는 흐름입니다.

### 게임 진행

- 한 웨이브에는 참가자 5명이 등장합니다.
- 저격 모드가 시작되기 전에 참가자들이 한 명씩 프리뷰됩니다.
- 참가자는 인간과 AI로 나뉩니다.
- 총알 수는 해당 웨이브의 AI 수와 같습니다.
- AI를 쏘면 AI가 제거됩니다.
- 인간을 쏘면 총알이 줄고 실수로 처리됩니다.
- 하트가 모두 사라지면 게임 오버입니다.
- 총알을 모두 사용했는데 AI가 남아 있으면 게임 오버입니다.
- 스테이지를 클리어하면 다음 스테이지로 넘어갑니다.
- 리스타트하면 실패한 스테이지에서 다시 랜덤 프리뷰부터 시작합니다.

### 조작법

- `A` / `D`: 저격 타겟을 왼쪽 또는 오른쪽으로 이동합니다.
- `마우스 왼쪽 버튼`: 사격합니다.

### 스테이지 규칙

- 1스테이지는 `Round1`의 AI 스프라이트를 사용합니다.
- 2스테이지는 `Round2`의 AI 스프라이트를 사용합니다.
- 3스테이지는 `Round3`의 AI 스프라이트를 사용합니다.
- 4스테이지는 `Round4`의 AI 스프라이트를 사용합니다.
- 5스테이지는 `Round5`의 AI 스프라이트를 사용합니다.
- 인간 스프라이트는 `cha_*` 캐릭터 세트를 사용합니다.
- 같은 번호의 인간과 AI는 같은 웨이브에 동시에 등장하면 안 됩니다.

예시:

- `cha_01`과 `round1-ai-1`은 같이 나오면 안 됩니다.
- `cha_04`와 `round2-ai-4`는 같이 나오면 안 됩니다.

### UI

- 하트는 남은 실수 가능 횟수를 보여줍니다.
- 탄 수는 `현재 탄 수/전체 탄 수` 형식으로 표시됩니다.
- 제한 시간은 초 단위 숫자로 표시됩니다.
- 클리어 패널과 게임 오버 패널로 스테이지 진행과 재시작을 처리합니다.

### 프로젝트 정보

- 엔진: Unity 6000.3.18f1
- 언어: C#
- UI: TextMeshPro
- 메인 씬: `Assets/Scenes/GameScene.unity`

### 폴더 구조

- `Assets/Scripts/Core`: 게임 흐름과 스테이지 진행 관리.
- `Assets/Scripts/Gameplay`: 참가자 데이터, 웨이브 생성, 사격 규칙, 결과 판정.
- `Assets/Scripts/Presentation`: 참가자 표시, HUD, 카메라, 저격 연출, 피격 피드백.
- `Assets/Charactor-art`: 인간과 AI 캐릭터 스프라이트.

### 실행 방법

1. 이 저장소를 클론합니다.
2. Unity 6000.3.18f1 또는 호환되는 Unity 6 버전으로 프로젝트를 엽니다.
3. `Assets/Scenes/GameScene.unity` 씬을 엽니다.
4. Play 버튼을 누릅니다.

### 컴파일 확인

Unity 밖에서 C# 컴파일을 확인하려면 다음 명령어를 실행합니다.

```powershell
dotnet build Assembly-CSharp.csproj
```

기대 결과:

- 오류 0개
- 경고 0개

---

This README is written in UTF-8 so both English and Korean text display correctly on GitHub.

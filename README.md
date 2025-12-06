## Project Description
본 프로젝트는 **3D 멀티플레이 RPG**를 구현하는 것을 목표
실제 온라인 게임 서비스의 구조를 반영하여 다음과 같은 3계층 네트워크 구조를 기반으로 제작

- **게임 서버(Unity Netcode Server)** – 실시간 게임 로직 처리
- **클라이언트(Unity Client)** – 입력 처리, UI, 캐릭터 조작, 서버와의 통신
- **계정 서버(Mock Account Server, Node.js)** – 로그인/회원가입 처리 및 토큰 발급
- **데이터베이스(DB)** – 계정 정보, 캐릭터 정보, 위치, 스탯 등을 저장하는 영속성 계층

단순한 멀티플레이 구현을 넘어, 실제 MMORPG 구조를 축소한 형태로 설계하여  
**클라이언트-서버 개발 경험과 온라인 게임 아키텍처 이해**가 목적

## Development Environment
- **OS**          : Windows
- **IDE**         : JetBrains Rider
- **Game Engine** : Unity 6.2 (6000.2.14f1)
- **Game Server** : Unity Netcode
- **Backend**     : Mock Account Server (Node.js + SQLite)

## Game Design
- Unity Netcode 기반 **클라이언트-게임 서버** 구조
- 플레이어 위치, 스탯, 인벤토리, HP 등 **상태 동기화 구조**
- 로그인 → 인증 토큰 발급 → 게임 서버 접속 → 게임 플레이 → DB 저장 구조의 온라인 RPG의 흐름을 모사한 구조

## Implementation Overview

### Client (Unity Client)
- 동적 플레이어 추적 카메라
- 클릭 이동 + NavMesh 기반 제어
- **UI / 입력 처리 / 세션 관리**
  - 로그인 → 토큰 저장 → 게임 서버 접속  
  - 서버에서 받은 상태를 화면에 반영

---

### Game Server (Dedicated Server, Unity Netcode)
> *실시간 멀티플레이 로직을 담당하는 핵심 서버*
- 토큰을 검증해 접속을 관리
- 서버 권위 구조
  - 플레이어 상태를 동기화·관리
  - 필요 시 정보를 저장 

---

### Account Server (Mock Account Server, Node.js + SQLite)
> *인증·계정 관리를 담당하는 모의 계정 서버*
- [README](https://github.com/jaehuru/RPG_Account_Server_Proto/blob/main/README.md)




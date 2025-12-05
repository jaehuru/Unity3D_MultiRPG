## Project Description
본 프로젝트는 **3D 멀티플레이 RPG**를 구현하는 것을 목표
실제 온라인 게임 서비스의 구조를 반영하여 다음과 같은 3계층 네트워크 구조를 기반으로 제작

- **게임 서버(Unity Netcode Server)** – 실시간 게임 로직 처리
- **클라이언트(Unity Client)** – 입력 처리, UI, 캐릭터 조작, 서버와의 통신
- **계정 서버(Mock Account Server, Node.js)** – 로그인/회원가입 처리 및 토큰 발급
- **데이터베이스(DB)** – 계정 정보, 캐릭터 정보, 위치, 스탯 등을 저장하는 영속성 계층

단순한 멀티플레이 구현을 넘어, 실제 MMORPG 구조를 축소한 형태로 설계하여  
**클라이언트-서버 개발 경험과 온라인 게임 아키텍처 이해를 포트폴리오로 보여주는 것이 최종 목표**

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

### Game Server (Unity Netcode)
> 실시간 멀티플레이 로직을 담당하는 핵심 서버
- 클라이언트가 제출한 토큰을 검증하여 접속 허용/거부
- 플레이어 생성, 스폰 위치 결정, 이동/전투 상태 동기화
- 서버 권위(Server-Authoritative) 구조  
  → 모든 이동 및 공격 검증은 서버가 담당
- 플레이어의 위치/스탯 변화/전투 로그 등 상태 관리
- 세션 종료 또는 일정 주기마다 DB에 플레이어 정보를 요청하여 저장

---

### Client (Unity Client)
- **동적 플레이어 추적 카메라**
  - 로컬 플레이어 Transform을 자동 인식하여 카메라가 추적
- **클릭 이동 + NavMesh 기반 제어**
  - 레이캐스트로 도착 지점 계산 후 NavMeshAgent 이동
  - 모든 이동은 서버와 동기화
- **UI / 입력 처리 / 세션 관리**
  - 로그인 → 토큰 저장 → 게임 서버 접속  
  - 서버에서 받은 상태를 화면에 반영
- **실시간 동기화된 전투 및 행동 처리**

---

### Account Server (Mock Account Server, Node.js)
> *실제 인증 서버 구조를 그대로 따르지만 내부 로직은 단순한 개발용 Mock 버전*
- `/register` → 회원가입 요청 처리 후 성공 응답 반환
- `/login` → 아이디/비밀번호 확인 후 인증 토큰 발급(JSON Web Token 형태)
- 클라이언트는 로그인 성공 시 토큰을 저장하고, 게임 서버 접속 시 토큰을 제출
- 실제 상용 게임의 **계정 서버와 동일한 흐름을 구현** 

---

### Database (SQLite)
> 프로젝트의 영속성 계층
- 계정 정보 저장  
  - 아이디, 비밀번호 해시, 유저 고유 ID
- 캐릭터 정보 저장  
  - 레벨, 스탯, 인벤토리, 장비, 마지막 접속 위치
- 주기적 저장 또는 로그아웃 시 저장
- Game Server ↔ DB 직접 연동 구조
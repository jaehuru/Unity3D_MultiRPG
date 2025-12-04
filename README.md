## Project Description
본 프로젝트는 클릭 기반 3D 멀티플레이 RPG를 구현하는 것을 목표

## Development Environment
- **Window OS**
- **JetBrains Rider**
- **Unity 6.2 (6000.2.14f1)**

## Game Design
- 마우스 클릭 기반 캐릭터 이동 시스템
- Unity Netcode for GameObjects 기반 클라이언트-서버 구조
- 보스의 패턴 공격과 플레이어의 카운터 액션 구현
- 플레이어 위치, 체력, 인벤토리 등의 서버-클라이언트 동기화 및 저장
- NavMesh를 활용한 NPC/몬스터 이동과 행동
- 향후 스킬 시스템, 던전, 파티 플레이, 인벤토리 확장 계획

## Implementation Overview

### 서버(Server) 기능
- **서버 주도 플레이어 데이터 영속성**  
  - 클라이언트 연결 해제 시, 해당 플레이어의 마지막 위치 데이터를 서버가 안정적으로 저장  
  - `GameNetworkManager.cs`에서 서버가 클라이언트 ID에 매핑되는 플레이어 오브젝트 목록을 관리  
  - 이를 통해 클라이언트 연결 해제 시 플레이어 최종 위치 저장 및 데이터 무결성 보장

- **플레이어 스폰 관리**  
  - 클라이언트 접속 시 서버에서 정확한 위치로 스폰  
  - 서버 로직에서 `NavMeshAgent` 초기화와 위치 보정 수행  

---

### 클라이언트(Client) 기능
- **동적 플레이어 팔로우 카메라 시스템**  
  - 로컬 플레이어(`NetworkManager.Singleton.LocalClient.PlayerObject`)의 Transform을 자동으로 추적  
  - 복잡한 수동 설정 없이도 안정적으로 카메라가 플레이어를 따라감  

- **클라이언트 입력 기반 플레이어 이동**  
  - 마우스 클릭 입력을 받아 `NavMeshAgent`를 이용한 목표 지점 이동  
  - 서버와 동기화된 이동 경로를 적용하여 네트워크 환경에서 이동 동기화

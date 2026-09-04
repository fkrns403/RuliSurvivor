# RuliSurvivor

Unity/C# 기반으로 제작한 2D 탑다운 생존 액션 팬게임입니다.

2025년 9월부터 온라인 협업으로 개발에 참여했으며,
2인 팀에서 클라이언트 개발을 담당했습니다.
2026년 6월 1차 베타를 배포한 이후에도 플레이어 피드백을 바탕으로 유지보수와 개선을 진행하고 있습니다.

## Project Info

- Engine: Unity
- Language: C#
- Platform: Windows PC
- Development Period: 2025.09 ~ 2026.06
- Team: 2명
  - 기획 / 아트 1명
  - 클라이언트 개발 1명
- Role: Client Developer

## Main Responsibilities

- 플레이어 이동, 전투 및 상태 처리
- 무기 및 스킬 시스템
- 일반 적 및 보스 시스템
- UI 및 게임 진행 로직
- 데이터, 저장 및 해금 시스템
- Object Pool 기반 반복 객체 관리
- 빌드 및 버그 수정
- 베타 배포 이후 유지보수

## Main Implementations

### Weapon System
`IWeaponRuntime` 인터페이스를 통해 무기의 장착, 해제, 레벨 갱신 흐름을 공통화했습니다.

### Damage System
`IDamageable` 인터페이스를 사용해 공격 코드가
`PlayerHealth`, `EnemyHealth`, `BossHealth` 같은 구체 클래스에 직접 의존하지 않도록 구성했습니다.

### Collision State Management
Dash, 피격 무적 등 여러 기능이 동일한 Collider 상태를 직접 변경하면서 발생한 문제를 개선하기 위해
`PlayerCollisionStateController`에서 활성 상태를 종합하고 최종 상태를 결정하도록 구조를 변경했습니다.

### Boss Pattern System
`BossSkill`과 `BossSkillSet`을 ScriptableObject 기반으로 구성해
패턴 실행 로직과 난이도별 스킬 구성을 분리했습니다.

### Object Pool
Enemy, Projectile, Effect 등 반복적으로 사용되는 객체를 Pool에서 재사용하도록 구성했습니다.

### Save & Unlock
플레이 결과를 저장하고 해금 조건을 평가해
다음 플레이의 캐릭터 선택 및 진행 상태에 반영하도록 구성했습니다.

## Repository Scope

이 저장소에는 포트폴리오 검토를 위한 제가 직접 작성한 C# 스크립트를 중심으로 공개합니다.

팬게임에 사용된 캐릭터 이미지, 음원, 기타 아트 리소스 및 제3자 에셋은 포함하지 않습니다.

## Links

- Portfolio: 준비 중
- Gameplay Video: 준비 중
- Notion: 준비 중

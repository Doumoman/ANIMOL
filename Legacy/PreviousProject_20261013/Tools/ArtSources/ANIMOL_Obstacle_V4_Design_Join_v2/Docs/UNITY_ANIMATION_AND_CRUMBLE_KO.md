# MCP 구현 대상 · Unity 애니메이션과 소멸 연출

사용자는 C05 붕괴를 Unity 자체 기능으로 구현하도록 요청했다. 아래 값은 기본 연출값이며 게임 효과 수치가 아니다. `Runtime/UnityCrumbleVfx.cs`는 SpriteRenderer·Transform을 쓰는 이식용 기본 컴포넌트다. 이 환경에서는 컴파일하지 않았다.

## 스프링·떡·컨베이어

Registry에 `theme/kind/facing/pose/fN`을 등록하고 atlas를 한 번만 읽는다. 해당 셀의 기존 이벤트 시각과 현재 게임 시각 차이로 Frame을 고른다. C02/C03은 30ms×7, C06은 40ms×7의 1회 재생이다. 충돌·착지 판정은 기존 owner가 수행하고 VFX가 다시 검사하지 않는다. 기존 반사·반동 수치를 임의로 변경하지 않는다.
컨베이어는 LEFT/RIGHT 각각 8개 프레임을 반복한다. 기본 간격 80ms일 때 1px씩 이동한다. 기존 속도가 다르면 `frame=floor(abs(beltSpeedUnitsPerSec)*32*elapsedSeconds)%8`로 조절할 수 있다. 하나의 연결된 벨트 구간에는 같은 시계를 사용하며 지형 무늬 시드는 고정한다.
정지·일시정지·재시작 시 게임 시계와 프레임도 같이 멈추거나 재설정한다. 그림만 압축하고 Collider·셀·Prefab 루트 크기·캐릭터 속도는 그래픽 코드가 변경하지 않는다.

## C05

- `idle`: 처음부터 4조각으로 갈라진 상판.
- 유효 밟음 이벤트: `active`로 균열 강조. 점유 중의 안전 규칙은 기존 owner 유지.
- 마지막 점유자 이탈: 기존 소멸 상태 전이를 사용한다. 기존 grace가 있으면 실제 `SurfaceEnabled true→false` 시점에 붕괴 이벤트를 발행한다.
- 붕괴 이벤트: 정상 상판을 숨기고 4개 조각을 독립 VFX root에 표시한다. 이웃 자유 블록 끝캡은 같은 정본 갱신에서 복원한다.
- 조각은 아래로 낙하하며 80ms 동안 불투명, 이후 320ms 동안 투명해져 총 400ms에 pool로 반환한다. Collider·Rigidbody2D·피해 판정·아이템은 없다.

`Data/c05_fragment_catalog.json`의 이미지 20개를 PPU32·중앙 pivot으로 임포트한다. 데이터 rect는 top-left다. 원점은 논리 셀의 왼쪽 아래 `(x,y)`이며 조각 중심은 `(x+(rect.x+rect.width/2)/32, y+1-(rect.y+rect.height/2)/32)`다. `UnityCrumbleVfx.TryPlay`에 같은 원점과 고유 이벤트 키·실제 이벤트 게임 시각·정렬 정보를 전달한다.
4조각의 초기 속도와 중력은 `Data/c05_fragment_effect.json`을 따른다. 렌더 위치 변화는 1/32 단위로 스냅한다. 조각의 회전은 0도, 스케일은 1이다. SpriteRenderer의 alpha로만 페이드하고 색을 노이즈처럼 바꾸지 않는다. alpha=0/255 자산 규칙은 원본 PNG에 적용하며, 런타임의 점진적 페이드는 요청한 예외다.

풀은 동시에 부서지는 여러 C05를 처리해야 한다. 플랫폼 자식으로 두면 플랫폼 비활성 시 조각도 사라지므로 Stage VFX root를 사용한다. 같은 붕괴 이벤트를 반복 수신하면 새 효과를 만들지 않는다. 로드·재열기 시 이미 inactive인 상태 자체를 새 붕괴 이벤트로 취급하지 않는다. Undo·페인트 프리뷰에서는 실제 효과를 생성하지 않는다. 장치 복구는 기존 owner가 상판·표시·끝캡을 복원하며 조각 VFX가 스테이지 데이터를 바꾸지 않는다.

Sprite는 Registry/빌더에서 미리 생성·캐시하고 매 프레임 Sprite.Create·재임포트·텍스처 ReadPixels를 호출하지 않는다. 플랫폼 reset·씬 unload에서 active 효과와 이벤트 캐시를 정리한다. 모바일에서 셀 수에 따라 무한히 늘어나는 효과·GC 할당을 만들지 않는다.

Unity 공식 참조: [SpriteRenderer.color](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/SpriteRenderer-color.html), [Sprite.Create](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Sprite.Create.html). 실제 프로젝트 버전은 6000.3.8f1이므로 해당 버전·렌더 파이프라인에서 투명 Sprite 머티리얼을 확인한다.

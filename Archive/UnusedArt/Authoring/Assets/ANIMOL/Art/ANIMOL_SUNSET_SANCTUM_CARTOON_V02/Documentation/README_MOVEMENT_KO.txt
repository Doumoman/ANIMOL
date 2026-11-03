낙일 성소 이동 장치 각진 전면 개정 V02

V02 변경 원칙
- 기존 둥근 장치 원화를 필터로 각지게 만든 것이 아니라, 세 native component 원화를 새로 생성했다. 화살표·FX·상자·스위치·별칭을 포함한 runtime96장 모두 V01과 decoded RGBA가 다르다.
- 도약 받침·상판·착지 표식: 곡면 원판을 여섯~여덟 개의 긴 직선 면과 큰 절단 모서리로 바꿨다. 초승달은 날카로운 분절 glyph만 남기고, 스프링도 삼각 쐐기로 정리했다.
- 압축 pose에서도 같은 crescent 상판이 낮게 눌려 남는다. 추가 native edit로 중앙 cap만 교체했으며, 새 V02 atlas의 다른11슬롯과 압축 외곽 housing은 수정 전 픽셀을 그대로 보존했다. production/moon_compression_edit_provenance.json 참조.
- 자석: U의 기능적 공동을 유지하면서 굽은 U 코너를 직선45도 절단으로 바꿨다. 핀·볼트는 사각형, cap은 직선 lever/stacked fins이며, 금속/목재 상자도 새 원화로 바꿨다.
- 포털: 둥근 링은 긴 직선8변 housing, pair02는 stepped notch로 구분했다. 회오리는 긴 삼각 blade와 diamond 구멍이다. housing이 moving blade 위에 고정되는 합성 순서로 외곽을 지킨다. 방향 FX도 새 긴 삼각 head/shard다.
- 상대경로96개, clip42개, 상태/방향/타일 점유/PPU/pivot/프레임 순서·fps·loop·동작 계약은 V01과 동일하다. 실제 Unity 실행은 여전히 NOT_RUN이다.


동결 범위
movement_manifest.json에 적힌 runtime PNG 96개, clip42개, 새 native component 원본3개다. sources/는 새 원화 원본이며 runtime 목록과 분리한다.
기본 canvas320×320, PPU256, pivot(0.5,0.5), logicalRect=[32,32,256,256]. contentRect는 [x,y,width,height] 형식이며 contentRectFormat=xywh다. 모든 runtime alpha는 이 한 칸의 논리 범위 안에 있다. 패딩/alpha로 실제 Collider/점유를 생성하지 않는다.

03 초승달 도약판
동쪽/북쪽/서쪽/남쪽의 고정 방향 표식과 대기→압축→발사→정착 네 native pose를 제공한다. 사각 석재 받침은 고정이고 plunger만 변경된다. 각 방향의 jump clip은 마지막에 대기 frame으로 돌아간다. landing_marker.png는 실제 출발칸+worldHeading×3의 셀에 따로 배치한다. 도약선과 플레이어를 장치 원화에 굽지 않았으므로 실제 호스트가 장치/목적지/플레이어를 같은 유효 행동 진행도에 연결한다.
도감이 확정하지 않은 막힌 착지 처리·중간칸 장치 반응·정확한 입력 시점은 실제 프로젝트 규칙에 위임한다. 이 파일을 근거로 새 규칙을 만들지 않는다.

14 자석 견인
magnet heading은 U자 자석의 열린 입이 향하는 방향이다. 실제 끌려오는 대상의 이동 방향은 그 반대다. 예: east 입구는 장치 오른쪽에서 금속을 west로 끌어온다. 각 레코드의 mouthHeading/pullTravelHeading과 mouthAnchorPixel을 사용한다. magnetic_pull_fx heading은 입구 방향이 아니라 실제 대상의 끌림 진행 방향이다.
고정 몸체4방향+native armature4pose를 제공한다. active loop는 실제로 승인된 시각 피드백 동안만 재생한다. 전원이 켜져 있음을 표현하려고 실시간으로 금속의 위치나 턴을 진행시키면 안 된다. 경로/장애물/다중 대상 우선순위는 기존 host가 결정한다. source PDF는 직선상 금속만 견인하고 목재는 그대로 둔다.
magnet_switch는 고정1칸 받침 위의 서로 다른 raised/pressed native key pose2장을 사용한 짧은12fps press/release다. 4개의 동일 프레임으로 전환을 부풀리지 않았다. metal_box/wood_box는 고정된 상자 원화이며 밀기/견인은 실제 host의 world translation tween으로 움직인다. 원화 내부에서 상자 그림을 옮기지 않는다.

16 짝 포털
pair_01은 diamond 문양, pair_02는 fork 문양과 양옆 notch로 구분한다. 색만으로 구분하지 않는다. 한 쌍의 양쪽 endpoint는 반드시 같은 channel 원화를 사용한다. channel ID는 실제 프로젝트 pair ID와 다른 시각 매핑일 뿐이며 실제 ID를 덮어쓰지 않는다.
포털 본체에는 영구 진입 방향이 없다. family=paired_portal, heading=none, state=pair_01_idle/enter/exit/blocked 또는 pair_02_idle/enter/exit/blocked다. 실제 world 진행 방향은 별도 portal_travel_fx의 east/north/west/south enter/exit clip으로 표시한다. 두 portal endpoint 자체를 이동 방향에 따라 회전시키지 않는다.
입구 진입 world heading과 walk/slide 상태를 그대로 보존한다. 걸어서 들어갔다면 짝 포털의 같은 방향 바깥 한 칸까지 나와 정지하고, 미끄러지는 중이었다면 그 뒤의 자동 이동을 기존 host가 계속한다. 짝 포털 다음 칸이 막히면 입구에 들어오기 전에서 정지하고 blocked 시각만 표시한다. 최초 진입 자체가 불가능한 무효 입력은0코스트이며 이미 미끄러지던 경우 이전 유효 행동 비용만 유지한다. 새 비용/자동 입력을 추가하지 않는다.
고정 stone rim은 네 aperture pose에서 동일하다. PNG의 ground center는(160,160)이고 시각 aperture center는 짧은 전면 tab 때문에(160,150)다. floor actor anchor는 기존 실제 셀 중심을 유지한다. travel glyph와 금속 pull glyph는 독립 FX로만 사용하고 실제 점유칸을 등록하지 않는다.

검수
movement_qa.json은 PNG SHA/decodedRGBA, 네 native pose의 고유성, 고정 받침/몸체/rim pixel, endpoint alias 일치, 한 칸 alpha bounds를 확인한 오프라인 검수다. inspection/은 새 원화를 재생한 검수 시트와 GIF다. 실제 Unity 프로젝트는 제공되지 않았으므로 import/C#·shader compile/Prefab·registry binding/Play Mode는 NOT_RUN이다.

제작
원화는 built-in image_gen으로 새로 생성했다. production/generation_prompts.json이 사용 prompt set이다. production/build_movement_assets.py는 native component crop, alpha 분리, 균일 크기 조절, 고정점 합성, 메타데이터 및 검수 출력만 수행한다. native RGB 재도색이나 코드로 대체 production 그림을 그리지 않았다.

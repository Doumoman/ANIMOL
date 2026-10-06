# ANIMOL 자유 지형 스프라이트 계약 v1

이 계약은 자유로운 **1×1 격자 지형**의 모든 국소 외곽·오목 모서리를 선택한다. 전체 지형 모양마다 완성 PNG를 열거하는 계약이 아니다. 기존 TerrainStructure v3 stamp의 저장 의미·소유권·물리 소유자는 변경하지 않는다.

## 파일과 실행

- `Tools/terrain_topology.mjs`: 이미지와 Unity 의존성이 없는 순수 ES 모듈.
- `Data/topology_catalog.json`: 256개 raw 마스크의 47개 정규화 슬롯 대응, 20개 스타일, 20개 분면 키.
- `Data/logical_fixtures.json`: 16개 논리 검증 모양. 그림 또는 샘플 스테이지 원본으로 해석하지 않는다.
- `Tools/build_topology_data.mjs`: 위 JSON을 같은 규칙으로 재생성한다.
- `Tools/validate_topology.mjs`: 유의미한 점유·경계 검증을 실행하고 `Docs/topology_validation.json`에 기록한다.
- `Tools/terrain_composition.mjs`: 실제 PNG 베이커와 프리뷰가 함께 사용하는 전역 무늬 위상·큰 문양 점유 조건.
- `Tools/terrain_preview_model.mjs`: 브러시·지우기·점유 스냅샷·실제 아틀라스 렌더 계획.
- `DESIGN_GALLERY.html`: 실제 PNG를 포함한 오프라인 아트 보기·자유 지형 편집·PNG와 점유 JSON 저장.

패키지 루트에서 다음을 실행한다.

```bash
node Tools/build_topology_data.mjs
node Tools/validate_topology.mjs
```

이미지 생성이 끝난 패키지에서는 `node Tools/build_gallery.mjs`로 오프라인 HTML을 다시 만들고 `node Tools/validate_gallery.mjs`로 프리뷰 모델과 실제 아틀라스 대응을 검사할 수 있다. HTML은 별도 서버·설치·인터넷 연결 없이 열 수 있다. 전경 PNG 저장은 그림만 저장하며, 점유 JSON은 별도 버튼으로 저장한다. 프리뷰 JSON은 Unity 스테이지 저장 형식이 아니다.

## 좌표와 점유

`rows`는 북쪽부터 기록하는 같은 길이의 문자열이다. `#`은 점유, `.`은 빈 공간이다. `origin:{x,y}`는 정수 좌표의 왼쪽 아래이며, `localY = height - 1 - row`다. 점유가 원본이다. 알파·장식·스프라이트 외접 사각형으로 충돌을 추론하지 않는다.

논리 셀은 1 Unity unit이다. 기본 스프라이트는 **32×32px, PPU 32**로 정의하며 분면은 16×16px다. 이 16px는 16×16 **논리 셀**의 아트 베이크 구역과 다른 수치다. 맵 크기를 16셀로 제한하지 않는다.

점유 연결은 상하좌우 4방향이다. 대각선만 닿는 셀은 별도 덩어리로 남는다. 가운데 셀이 비어 있으면 주변이 전부 점유여도 전경 스프라이트가 없다.

## 256 입력과 47 슬롯

| 이웃 | N | NE | E | SE | S | SW | W | NW |
|---|---:|---:|---:|---:|---:|---:|---:|---:|
| 비트 | 1 | 2 | 4 | 8 | 16 | 32 | 64 | 128 |

각 대각선 비트는 해당 대각선을 끼운 두 직교 이웃이 모두 점유인 경우에만 유지한다. 예를 들어 NE는 N과 E가 모두 점유여야 유지된다. 이 게이팅 결과는 정확히 47개이며 슬롯은 정규화 마스크 값의 오름차순이다. `rawToCanonical[raw]`와 `rawToIndex[raw]`를 사용한다.

`cells[index].fileSuffix`는 `mask000.png` 형태다. 모든 숫자 0~255에 PNG를 만들 필요가 없으며 정규화 값 47개에만 실제 PNG가 필요하다. 스타일은 기존 ID `T01_A`부터 `T05_D`까지 20개다. 따라서 기본 완성 셀 스프라이트의 계약 수량은 **940개**다. 이 JSON 파일 자체는 이미지 제작 완료의 증거가 아니다.

## 분면 20개

`getQuadrants(raw)`는 NW, NE, SW, SE 순서로 `{corner,state,moduleKey,column,row}` 배열을 반환한다. 이미지의 `column,row`는 왼쪽 위 기준 2×2 분면 좌표다. 실제 픽셀 오프셋은 각각 16을 곱한다.

분면이 접한 세 이웃은 NW=(N,W,NW), NE=(N,E,NE), SW=(S,W,SW), SE=(S,E,SE)다. 분면의 첫 이웃은 세로 이웃, 둘째는 가로 이웃이다.

| 세로 이웃 | 가로 이웃 | 대각선 | state | 표현 |
|---|---|---|---|---|
| 없음 | 없음 | 무관 | OUTER | 외부 볼록 모서리 |
| 없음 | 있음 | 무관 | TOP / BOTTOM | 위쪽 / 아래쪽 경계 |
| 있음 | 없음 | 무관 | SIDE | 해당 방향의 측면 경계 |
| 있음 | 있음 | 없음 | INNER | 오목한 내부 모서리 |
| 있음 | 있음 | 있음 | IN | 내부 채움 |

키는 `NW_IN`, `NW_TOP`, `NW_SIDE`, `NW_OUTER`, `NW_INNER`처럼 기록한다. SW·SE는 TOP 대신 BOTTOM이다. 천장·아랫면·옆면 원화를 각 방향으로 설계하고 단순 회전으로 대체하지 않는다.

분면을 합친 47종은 직사각형뿐 아니라 1셀 두께, 불규칙 계단, 구덩이, 공동, 돌출, 분기, 여러 덩어리를 지원한다. 각 원본 분면의 내부 접합 색·선·무늬 위치를 맞춰야 그림도 이어진다. 이 수학만으로 이미지 품질을 검증한 것으로 볼 수 없다.

## API 예

```js
import {
  occupancyFromRows, resolveCell, getQuadrants, partitionOccupancy,
} from './Tools/terrain_topology.mjs';

const grid = occupancyFromRows(['##..##', '######'], { x: -1, y: -17 });
const result = resolveCell(grid.occupied, 0, -17);
// result: rawMask, canonicalMask, spriteIndex, quadrants. Empty center returns null.
const quarterRecords = getQuadrants(result.rawMask);
const chunks = partitionOccupancy(grid.occupied, 16);
// Resolve art against grid.occupied (global), never against one chunk alone.
```

`neighborMask(occupied,x,y)` accepts a `Set` containing `"x,y"` keys or a callback `(x,y)=>boolean`. Coordinates are global safe integers. `chunkCoordinate` uses floor division, so -1 and -16 are in chunk -1, while -17 is in chunk -2.

## 아트와 통합의 별도 조건

- 모든 외곽 마감은 실제 빈 이웃을 향한다. 점유 셀 사이에 외벽을 넣지 않는다.
- 구덩이·공동의 빈 부분에 큰 문양이나 구조 장식을 가로질러 넣지 않는다. 큰 장식은 차지하는 모든 셀과 경계 여유를 별도로 검사한다.
- 덩어리 전체의 문양·보강 구조·재료 무늬 위상을 별도 규칙으로 구성해야 1×1 반복 느낌을 줄일 수 있다.
- 렌더/저장 청크를 나눠도 이웃 점유는 전역에서 조회한다. 청크 경계는 지형 외곽이 아니다.
- 서로 다른 스타일의 재료 접합은 별도 connector 정책이 필요하다. 47종 국소 점유 커버리지와 같은 주장으로 취급하지 않는다.
- 단방향 발판의 충돌은 기존 물리 경로를 유지한다. 이 계약은 일반 고체 점유의 외형 선택을 정의한다.
- Unity 물리·Scene 에디터·기존 맵 저장·Undo·로드 통합은 실제 프로젝트에서 따로 검증해야 한다.

## 검증 범위

검증기는 256개 raw 마스크, 4×4 점유 65,536개, 가로/세로 인접 셀 문맥 각 1,024개, 16개 논리 모양, 음수 및 16셀 청크 경계를 검사한다. 빈 셀에 전경이 생기지 않는지, 점유 셀 사이에 외벽이 선택되지 않는지, 오목 모서리 선택과 좌표 변환이 일치하는지 확인한다. 잘못된 청크 내부 전용 조회를 사용하면 실제로 실패할 수 있는 큰 지형 negative control도 포함한다.

이 결과는 논리 선택의 보장이다. PNG 제작·팔레트·실제 그림 접합·Unity 충돌·에디터 저장을 검사한 결과가 아니다.

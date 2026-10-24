# 퍼즐 맵 제작기 사용법

2026-10-18 기준. 메뉴 `ANIMOL/Puzzle/Map Editor`에서 연다.

## 새 맵 만들기

1. `New`를 누르고 Theme, Width, Height를 정한 뒤 `Create starter map`을 누른다. 기본 테마는 StarGarden, 기본 크기는 12×18이다.
2. 저장 위치는 `Assets/ANIMOL/Data/Puzzle/Maps`를 권장한다. `이름.asset` 맵과 `이름_Scenario.asset` 장치 시나리오가 함께 만들어진다.
3. 시작 맵은 걸을 수 있는 Grass 지형, 시작점, 방울 하나, 출구, 빈 장치 시나리오를 가진다. 위치와 지형을 퍼즐에 맞게 편집한다.
4. Terrain, Decor, Markers, Device, Water 레이어를 골라 왼쪽 드래그로 배치하고 오른쪽 드래그로 지운다. 각 레이어는 독립적이다. Water는 배가 사용할 논리적 물 칸이다.
   Terrain에서는 Material과 Blocked cell을 정한 다음 왼쪽 드래그로 원하는 칸에 칠한다. 오른쪽 드래그 또는 Erase brush로 지우면 해당 칸은 통행 불가가 되며 다른 레이어 데이터는 남는다.
5. `Themed devices / water / goals`에서 시나리오의 장치, 물 칸, 추가 목표, 비용 제한과 현재 장치 정책을 설정한다.
6. `Play Preview`로 실행한다. 하단 검사는 배치 유효성 검사이며 퍼즐 풀이 가능성은 증명하지 않는다.

## 장치 배치

- Device 레이어의 `24-device library`는 `Data/Puzzle/Prototypes`에 저장된 01~24 장치 연습 시나리오에서 설정과 프리팹을 읽는다. 하나의 연습 시나리오에 압력판과 다리처럼 여러 부품이 있을 수 있다.
- `This map`은 현재 시나리오에 이미 놓은 장치를 복사한다. `Stamp template`을 고르고 칸을 누르면 별도 ID의 복사본이 만들어진다.
- 포털의 목적지와 선로·다리 칸은 배치 위치만큼 이동한다. 폭탄의 폭발 범위 오프셋은 상대 좌표로 유지한다.
- 연결 그룹, 포털 목적지, 선로, 물 칸, 폭발 범위와 추가 목표는 배치 후 확인한다. `Select placed device (left click)`을 켜면 그 칸의 장치를 선택해 바로 편집할 수 있다.
- 장치 레이어에서 `Select placed device`를 켜고 장치를 클릭하면 프리팹 그림과 번호, 방향, 연결선을 확인할 수 있다. 방향 버튼으로 회전하고 `Connect to device...`를 누른 다음 대상 장치를 클릭하면 두 장치가 같은 논리 그룹에 연결된다. `Disconnect selected device`는 선택 장치의 연결만 끊는다. 배치하려면 선택 모드를 끄고 스탬프를 선택한다.
- 하늘색 실선은 같은 그룹의 장치, 분홍 점선은 포털 목적지다. 선택한 장치의 주황 점선은 선로/점유 칸, 폭탄의 주황 영역은 폭발 범위다. `Show logic links and footprints`에서 표시를 켜고 끈다. 연습 장치의 그룹 이름은 새 배치에 복사되지 않으므로 연결은 각 맵에서 명시적으로 만든다.
- 프리팹이 없는 장치는 미리보기에서 보이지 않으므로 경고를 확인한다. `Refresh device library`로 연습 에셋 변경을 다시 읽을 수 있다.

장치의 **현재** 판정은 [24종 장치 규칙 정리](ANIMOL_DEVICE_RULES_CURRENT.md)에 기록한다. 장치 규칙 변경안은 별도로 결정한다.

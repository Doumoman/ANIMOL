# NET02 프로젝트 배치

2026-10-05 TASK 01에서 루트의 `ANIMOL_NET02_UI_SERVER_LINK.zip`을 충돌 검사 후 가져왔다.

- ZIP의 `Assets/ANIMOL/NetUiDev/` → 저장소 루트의 같은 경로. Unity가 생성한 `.meta`를 함께 관리한다.
- 그 밖의 파일 → `Tools/ANIMOLNet02/` 아래 같은 상대 경로. 기존 프로젝트 Docs/Tools/Server를 덮어쓰지 않았다.
- `MANIFEST.json`과 `Docs/Evidence/`는 **제작 패키지 원본 기록**이다. 이 프로젝트의 적용·컴파일·기기 검증 결과가 아니다. manifest의 Assets 경로는 저장소 루트 기준, 나머지는 이 디렉터리 기준이다.
- `Tools/validate_package.py`만 분리된 Assets 배치 및 Windows UTF-8 읽기를 지원하도록 조정했다. 원본 manifest의 해당 파일 해시는 프로젝트 조정 후 파일과 다르다.
- 기본 config는 빈 상태다. fixture는 서버 검사 전용이며 운영 IdMap에 반영하지 않았다.
- DEV define은 기본 비활성이다. 서비스 컴포넌트 설치나 기존 UI 바인딩은 TASK 03~05에서 진행한다.

검증 명령(현재 디렉터리에서 실행):

```powershell
python Tools/validate_package.py
python -m unittest discover -s ServerTests -v
```

[프로젝트 조사 결과](../../Docs/NET02_TASK01_INSPECTION.md) · [원본 적용 지시](Docs/NET02_PROJECT_APPLY.md)

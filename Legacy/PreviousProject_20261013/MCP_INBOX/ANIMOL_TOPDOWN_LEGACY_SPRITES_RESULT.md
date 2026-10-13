# ANIMOL 탑뷰 전환용 Legacy·스프라이트 정리 결과

2026-10-13. Unity 6000.3.8f1로 실제 이동·재임포트·컴파일·씬 검사를 수행했다.

## 적용

- 플랫포머 전용 파일 620개를 `Legacy/Platformer_20261013/`로 이동했다. 원래 상대 경로·내용·파일 `.meta`와 폴더 `.meta`를 보존했다.
- 공용 UI PNG 75개를 `Assets/ANIMOL/Sprites/UI/`로 이동하고 소문자 snake_case로 이름을 정리했다. StageSelect/Store는 실제 기존 화면을 기준으로 분류했다.
- 새 Player/Tiles/Obstacles/Background 분류 폴더를 준비했다. 탑뷰 전용 그림·애니메이션은 실제 존재하지 않아 생성하지 않았다.
- MapAuthoringWorkspace, 전용 제작기·실험/런 프로토타입 씬과 테스트를 보관했다. 활성 빌드는 Bootstrap/Lobby/Results만 유지한다.
- 활성 유지가 필요한 구형 UiBuildPipeline 부분 클래스 17개는 보관 위치 주석과 `#if ANIMOL_LEGACY_MAP_EDITOR`로 차단했다. 기본 설정에 define을 추가하지 않았다.
- 기존 씬을 호출하는 캠페인 UI 함수도 차단하고 기본 경로에서 준비 중 메시지를 표시한다.
- GUID 기반 씬·프리팹·애니메이션 참조를 유지했다. Editor 빌더 계산 경로·asmdef 참조, 스프라이트 정본 manifest, NamedArt 경로·이름·슬롯 캐시를 갱신했다. Resources 카탈로그/프리팹 키는 기존 위치에서 유지한다.
- 기존 빌드 씬 설정과 NamedArt 인덱스의 사용자 작업본은 Legacy의 동일 ProjectSettings 경로에 보존했다.

## 분류와 기존 작업

Assets 파일 4,989개, GUID 2,632개, asmdef 52개를 조사했다. 초기 중복 GUID는 0개였다. 최초 사용자 변경은 미추적 파일을 펼친 기준 1,736개다.

초기 참조 조사에서 활성 UI·계약·런타임 등에 연결된 후보 792개는 보수적으로 유지 대상으로 분류했다. 일부 참조 증거는 이후 보관한 테스트를 포함하는 초기 조사 기록이다. 이번 작업에서는 공용 계약과 게임플레이 타입의 결합을 다시 작성하지 않았다. 파일별 이동·GUID·분류 증거는 인벤토리에 있다.

미커밋 파일의 내용은 이동 위치에서 보존한다. 이전부터 미추적이던 파일은 이동 후에도 미추적이다. 커밋은 기존 인덱스 blob의 경로 이동과 이번 작업의 변환만 포함하므로, 이전 사용자 수정 내용은 새 경로의 미커밋 변경으로 남는다. 따라서 커밋만으로 기존 미추적 사용자 파일까지 새로 추적되지는 않는다.

보관 파일 가운데 기존 추적 파일은 242개, 기존 미추적 파일은 378개다. 기존 추적 파일의 보관 대상 인덱스 blob을 원래 HEAD와 대조했으며 불일치는 0건이었다. UI PNG 75개는 모두 기존 추적 자산이다. 사용자가 이미 추가했던 캠페인 실행 함수는 사용자 변경으로 남기고, 커밋에는 원래 HEAD에 존재하던 실행 함수의 차단만 적용한다.

Phase1 동물 업그레이드 UI는 실제 존재하는 공용 UI로 유지했다. 탑뷰/스와이프 시제품과 P01 QA 씬은 발견되지 않았다. 해당 씬 검사를 수행한 것처럼 처리하지 않았다. 메인 UI V7 배경 패키지·기준·빌더는 유지했다.

## 검증

| 검사 | 결과 |
|---|---|
| 작업 전 및 이동 후 Unity 재임포트·컴파일 | 성공 |
| 이동 파일·`.meta` SHA-256 비교 | 불일치 0 |
| 비대상 Assets 파일 비교 | 변경 0 |
| define 내부 원래 코드 내용 비교 | 불일치 0 |
| 활성 자산의 보관 GUID 참조 | 0 |
| Bootstrap/Lobby/Results 누락 컴포넌트·오브젝트 참조 | 0 |
| 컴파일된 구형 맵 제작기/런 프로토타입 메뉴 | 0 |
| NamedArt의 이번 이동 대상에 대한 옛 경로 | 0 |
| EditMode: CommercialPolishPolicyTests + CanonicalSpritePathTests | 5/5 통과 |
| PlayMode: MenuThemeFlowTests | 3/3 통과 |
| P01 QA | 실제 씬 없음 — 검사 제외 |

전체 구형 플랫포머 테스트 스위트 대신 보존 UI와 이번 경로 변경에 관련된 테스트를 실행했다. 이 정리는 탑뷰 퍼즐 구현 완료를 의미하지 않는다.

## 산출물

- [Legacy 컷오버 명세](../Docs/ANIMOL_LEGACY_CUTOVER_MANIFEST.md)
- [스프라이트 경로·이름 규칙](../Docs/ANIMOL_SPRITE_PATHS_AND_NAMES.md)
- [전체 이동·GUID·초기 참조 인벤토리](../Docs/ANIMOL_LEGACY_CUTOVER_INVENTORY.json)
- [정본 스프라이트 매핑](../Assets/ANIMOL/Sprites/sprite_path_manifest.json)
- [Unity 씬 검사](../Docs/Validation/TopdownCutover/unity-validation.json)
- [NamedArt 검사](../Docs/Validation/TopdownCutover/namedart-validation.json)
- [EditMode 결과](../Docs/Validation/TopdownCutover/editmode.xml)
- [PlayMode 결과](../Docs/Validation/TopdownCutover/playmode.xml)

## 추가 지시 적용 결과

추가 작업 기준 커밋: `87725aaa`. 실측 Unity 6000.3.8f1 / 활성 타깃 Android / 시작 씬 Bootstrap / 활성 빌드 씬 Bootstrap·Lobby·Results. `Assets/ANIMOL/Phase1` 및 지정 P01 QA 씬 3종은 없다. 동물 업그레이드 Phase1 UI는 별도 공용 기능이다.

### 분류·보관·호출부

- 현재 분류 정본: [ANIMOL_CUTOVER_CLASSIFICATION.json](../Docs/ANIMOL_CUTOVER_CLASSIFICATION.json). `platformer-only` 654, `shared-or-still-referenced` 1292, `topdown-puzzle` 0, `unknown` 391. 활성 파일·보관 파일·사본은 별도 기록이다.
- 이전 이동 620개 유지. 비활성 Editor bulk generator 17개의 조건부 컴파일 전 원본과 `.meta`를 Legacy에 추가 보존했다. 혼재한 CampaignUiPresenter는 과거 씬 호출 본문만 분리해 차단하고 복구용 사본을 별도 보존했다.
- [Legacy README](../Legacy/Platformer_20261013/README.md)에 범위·원래 경로·기준 커밋·목록·별도 작업 트리 복원 절차를 기록했다.
- 남는 Editor 코드 17개와 혼재 호출부에 정확한 LEGACY 주석과 보관 경로를 적었다. default PlayerSettings/asmdef에 define을 추가하지 않았다. `.cs` 확장자를 바꾸거나 블록 주석으로 컴파일을 숨기지 않았다.
- 5테마×20 슬롯, 총 100개 스테이지는 실측했다. 공유 계약·캠페인 선택 UI·서버·계정·방울 서비스와 연결된 파일은 유지하고 참조자를 분류 정본과 명세에 기록했다. unknown 391개도 유지했다.

### 정본 경로·이름 및 참조

UI PNG 75개를 Unity AssetDatabase.MoveAsset으로 재개명했다. 파일명은 지정 화면 ID(main/ingame/settings/stage_select/store/common)와 실제 상태 접미사를 사용한다. 기존 패키지 키와 이전 정본 경로는 manifest의 호환 입력으로 유지해 최종 경로로 해석한다. NamedArt 경로·텍스처/Sprite 이름 캐시·슬롯 키도 갱신했다. Runtime Resources 카탈로그·프리팹 키와 GUID 기반 씬·프리팹 참조를 유지했고 활성 SpriteAtlas 파일은 실측 검색에서 없었다.

PNG 75개 모두 Point/Uncompressed/mipmap 없음/PPU 32. 기존 UI 아이콘 11개는 한 변이 32px 미만이며 타일로 취급하거나 크기를 변경하지 않았다. 새 타일 최소 32×32, 고해상도 P01 별도 PPU, 10fps 프레임 보존은 정본 규칙에 명시했다. P01 대상이 없어 해당 처리·검증을 수행한 것으로 표시하지 않는다. M01~M24 등록표는 문서에만 확정하고 빈 장치 폴더나 새 게임 구현은 만들지 않았다.

| 이전 정본 경로 | 최종 정본 경로 | 유지 GUID |
|---|---|---|
| `Assets/ANIMOL/Sprites/UI/MainMenu/spr_ui_main_menu_five_theme_menu_maps_mode_campaign_164x88.png` | `Assets/ANIMOL/Sprites/UI/MainMenu/spr_ui_main_theme_menu_mode_campaign_idle.png` | `b4cc6679c17570a4eb43e35ff9a54175` |
| `Assets/ANIMOL/Sprites/UI/MainMenu/spr_ui_main_menu_five_theme_menu_maps_mode_competition_164x88.png` | `Assets/ANIMOL/Sprites/UI/MainMenu/spr_ui_main_theme_menu_mode_competition_idle.png` | `1f572064fd645ad4481d0a29b990713d` |
| `Assets/ANIMOL/Sprites/UI/MainMenu/spr_ui_main_menu_five_theme_menu_maps_mode_cooperation_164x88.png` | `Assets/ANIMOL/Sprites/UI/MainMenu/spr_ui_main_theme_menu_mode_cooperation_idle.png` | `1b1ebd6ef9fd67d43a0986f9f54ed3a6` |
| `Assets/ANIMOL/Sprites/UI/MainMenu/spr_ui_main_menu_five_theme_menu_maps_button_ad_48.png` | `Assets/ANIMOL/Sprites/UI/MainMenu/spr_ui_main_theme_menu_button_ad_idle.png` | `7b554d496e985a440b811450d270a9af` |
| `Assets/ANIMOL/Sprites/UI/MainMenu/spr_ui_main_menu_five_theme_menu_maps_button_back_48.png` | `Assets/ANIMOL/Sprites/UI/MainMenu/spr_ui_main_theme_menu_button_back_idle.png` | `748111350d14b5e4184df707dc6a4272` |
| `Assets/ANIMOL/Sprites/UI/MainMenu/spr_ui_main_menu_five_theme_menu_maps_button_play.png` | `Assets/ANIMOL/Sprites/UI/MainMenu/spr_ui_main_theme_menu_button_play_idle.png` | `dbe2fe640d5614a4e8f00966df72bc0c` |
| `Assets/ANIMOL/Sprites/UI/MainMenu/spr_ui_main_menu_five_theme_menu_maps_button_settings_48.png` | `Assets/ANIMOL/Sprites/UI/MainMenu/spr_ui_main_theme_menu_button_settings_idle.png` | `4fc412e258664f242b412633a93a0a1e` |
| `Assets/ANIMOL/Sprites/UI/MainMenu/spr_ui_main_menu_five_theme_menu_maps_button_shop_48.png` | `Assets/ANIMOL/Sprites/UI/MainMenu/spr_ui_main_theme_menu_button_shop_idle.png` | `6566c981491f1cf4fb7cd67c4c13ebcb` |
| `Assets/ANIMOL/Sprites/UI/MainMenu/spr_ui_main_menu_five_theme_menu_maps_capsule_currency.png` | `Assets/ANIMOL/Sprites/UI/MainMenu/spr_ui_main_theme_menu_capsule_currency_idle.png` | `dc515879c413bcd43b6e971832bc307a` |
| `Assets/ANIMOL/Sprites/UI/InGame/spr_ui_in_game_five_theme_menu_maps_icon_coin_hud_24.png` | `Assets/ANIMOL/Sprites/UI/InGame/spr_ui_ingame_theme_menu_icon_coin_hud_idle.png` | `7f9afc702f50fcb42aba00b62048755c` |
| `Assets/ANIMOL/Sprites/UI/Common/spr_ui_common_missing_ui_v1_ui_common_input_field_default.png` | `Assets/ANIMOL/Sprites/UI/Common/spr_ui_common_input_field_idle.png` | `fa775872b6a4e9c43a4cc62cb6e091e5` |
| `Assets/ANIMOL/Sprites/UI/Common/spr_ui_common_missing_ui_v1_ui_common_input_field_error.png` | `Assets/ANIMOL/Sprites/UI/Common/spr_ui_common_input_field_error.png` | `a44fbcb6c3b6417408890c070c798b35` |
| `Assets/ANIMOL/Sprites/UI/Common/spr_ui_common_missing_ui_v1_ui_common_input_field_focused.png` | `Assets/ANIMOL/Sprites/UI/Common/spr_ui_common_input_field_focused.png` | `31a16438f492d874eaca80110c10c548` |
| `Assets/ANIMOL/Sprites/UI/Common/spr_ui_common_missing_ui_v1_ui_common_radio_selected.png` | `Assets/ANIMOL/Sprites/UI/Common/spr_ui_common_radio_selected.png` | `076d6b8545f9ebf4a838aa1ea57ada4f` |
| `Assets/ANIMOL/Sprites/UI/Common/spr_ui_common_missing_ui_v1_ui_common_radio_unselected.png` | `Assets/ANIMOL/Sprites/UI/Common/spr_ui_common_radio_unselected.png` | `bd66b6c7dbb76734b9021874aaa7be51` |
| `Assets/ANIMOL/Sprites/UI/Common/spr_ui_common_missing_ui_v1_ui_common_row_frame.png` | `Assets/ANIMOL/Sprites/UI/Common/spr_ui_common_row_frame_idle.png` | `2931a9a3cbfa2234984797cd0fb9f0ea` |
| `Assets/ANIMOL/Sprites/UI/Common/spr_ui_common_missing_ui_v1_ui_common_scroll_thumb.png` | `Assets/ANIMOL/Sprites/UI/Common/spr_ui_common_scroll_thumb_idle.png` | `e1c2b19cf6d42ca4297e0f58bc3691bb` |
| `Assets/ANIMOL/Sprites/UI/Common/spr_ui_common_missing_ui_v1_ui_common_scroll_track.png` | `Assets/ANIMOL/Sprites/UI/Common/spr_ui_common_scroll_track_idle.png` | `3ddf1a8ec156fc046aad13444579722c` |
| `Assets/ANIMOL/Sprites/UI/Common/spr_ui_common_missing_ui_v1_ui_common_selection_outline.png` | `Assets/ANIMOL/Sprites/UI/Common/spr_ui_common_selection_outline_idle.png` | `1edf4c5fedc255b4eb0af2a30c14872a` |
| `Assets/ANIMOL/Sprites/UI/Common/spr_ui_common_missing_ui_v1_ui_common_slider_fill.png` | `Assets/ANIMOL/Sprites/UI/Common/spr_ui_common_slider_fill_idle.png` | `36124c3b8f32c164480dbc553e952256` |
| `Assets/ANIMOL/Sprites/UI/Common/spr_ui_common_missing_ui_v1_ui_common_slider_rail.png` | `Assets/ANIMOL/Sprites/UI/Common/spr_ui_common_slider_rail_idle.png` | `0c4567a6245240241b50e2b09d74c4b4` |
| `Assets/ANIMOL/Sprites/UI/Common/spr_ui_common_missing_ui_v1_ui_common_slider_thumb.png` | `Assets/ANIMOL/Sprites/UI/Common/spr_ui_common_slider_thumb_idle.png` | `cf25b558db86c644baab1b24597a3182` |
| `Assets/ANIMOL/Sprites/UI/Common/spr_ui_common_missing_ui_v1_ui_common_tab_default.png` | `Assets/ANIMOL/Sprites/UI/Common/spr_ui_common_tab_idle.png` | `f21a64931ade3bf4f9341677dfd99a0e` |
| `Assets/ANIMOL/Sprites/UI/Common/spr_ui_common_missing_ui_v1_ui_common_tab_selected.png` | `Assets/ANIMOL/Sprites/UI/Common/spr_ui_common_tab_selected.png` | `5f56ba3ef65a26e4c9675ebbb7e67f77` |
| `Assets/ANIMOL/Sprites/UI/Common/spr_ui_common_missing_ui_v1_ui_common_toggle_knob.png` | `Assets/ANIMOL/Sprites/UI/Common/spr_ui_common_toggle_knob_idle.png` | `0859f6111aea4ce4097f84db2c19edfb` |
| `Assets/ANIMOL/Sprites/UI/Common/spr_ui_common_missing_ui_v1_ui_common_toggle_track_off.png` | `Assets/ANIMOL/Sprites/UI/Common/spr_ui_common_toggle_track_off.png` | `712a688f921f816498d802a402fd456f` |
| `Assets/ANIMOL/Sprites/UI/Common/spr_ui_common_missing_ui_v1_ui_common_toggle_track_on.png` | `Assets/ANIMOL/Sprites/UI/Common/spr_ui_common_toggle_track_on.png` | `f37302700cc9ada4584f6d3cc0e311a9` |
| `Assets/ANIMOL/Sprites/UI/Common/spr_ui_common_missing_ui_v1_ui_emote_hello.png` | `Assets/ANIMOL/Sprites/UI/Common/spr_ui_common_emote_hello_idle.png` | `e6b4b5e20092f6b48988d9e4debef758` |
| `Assets/ANIMOL/Sprites/UI/Common/spr_ui_common_missing_ui_v1_ui_emote_help.png` | `Assets/ANIMOL/Sprites/UI/Common/spr_ui_common_emote_help_idle.png` | `e6ac43f5ab2171141bfa837855cd90ec` |
| `Assets/ANIMOL/Sprites/UI/Common/spr_ui_common_missing_ui_v1_ui_emote_slot_frame.png` | `Assets/ANIMOL/Sprites/UI/Common/spr_ui_common_emote_slot_frame_idle.png` | `216217cb54bcd7745b4893279aac8d2b` |
| `Assets/ANIMOL/Sprites/UI/Common/spr_ui_common_missing_ui_v1_ui_emote_thanks.png` | `Assets/ANIMOL/Sprites/UI/Common/spr_ui_common_emote_thanks_idle.png` | `a4e5a82cae3d4e04e9b7b5a4ac6a1ace` |
| `Assets/ANIMOL/Sprites/UI/Common/spr_ui_common_missing_ui_v1_ui_help_bubble_exit.png` | `Assets/ANIMOL/Sprites/UI/Common/spr_ui_common_help_bubble_exit_idle.png` | `b9df081fd078f2c4aa97015311f01724` |
| `Assets/ANIMOL/Sprites/UI/Common/spr_ui_common_missing_ui_v1_ui_help_jump.png` | `Assets/ANIMOL/Sprites/UI/Common/spr_ui_common_help_jump_idle.png` | `f6865e2485aac894f85c1aaab1798f74` |
| `Assets/ANIMOL/Sprites/UI/Common/spr_ui_common_missing_ui_v1_ui_help_move.png` | `Assets/ANIMOL/Sprites/UI/Common/spr_ui_common_help_move_idle.png` | `b9babb39f5d423e4e800e4cc13e1b709` |
| `Assets/ANIMOL/Sprites/UI/Common/spr_ui_common_missing_ui_v1_ui_icon_copy_code.png` | `Assets/ANIMOL/Sprites/UI/Common/spr_ui_common_icon_copy_code_idle.png` | `ec09e51b94d0e4e40ab6829b7660c10f` |
| `Assets/ANIMOL/Sprites/UI/Common/spr_ui_common_missing_ui_v1_ui_icon_disconnected.png` | `Assets/ANIMOL/Sprites/UI/Common/spr_ui_common_icon_disconnected_idle.png` | `61346f2b92de55c469b2239c82ea5684` |
| `Assets/ANIMOL/Sprites/UI/Common/spr_ui_common_missing_ui_v1_ui_icon_enter_room.png` | `Assets/ANIMOL/Sprites/UI/Common/spr_ui_common_icon_enter_room_idle.png` | `1aa7e9009609d954b9f03fa4feee5081` |
| `Assets/ANIMOL/Sprites/UI/Common/spr_ui_common_missing_ui_v1_ui_icon_host.png` | `Assets/ANIMOL/Sprites/UI/Common/spr_ui_common_icon_host_idle.png` | `334dea1d047833644a8a44ee5c82e306` |
| `Assets/ANIMOL/Sprites/UI/Common/spr_ui_common_missing_ui_v1_ui_icon_invite.png` | `Assets/ANIMOL/Sprites/UI/Common/spr_ui_common_icon_invite_idle.png` | `814b9cfd0904c5141ae6b8f9392510b5` |
| `Assets/ANIMOL/Sprites/UI/Common/spr_ui_common_missing_ui_v1_ui_icon_paste_code.png` | `Assets/ANIMOL/Sprites/UI/Common/spr_ui_common_icon_paste_code_idle.png` | `b95685814de800b46a2a0deff85da1c5` |
| `Assets/ANIMOL/Sprites/UI/Common/spr_ui_common_missing_ui_v1_ui_icon_reconnecting.png` | `Assets/ANIMOL/Sprites/UI/Common/spr_ui_common_icon_reconnecting_idle.png` | `9be4a4105f6d3e3448e9f3f6013274db` |
| `Assets/ANIMOL/Sprites/UI/Common/spr_ui_common_missing_ui_v1_ui_multi_art_casual.png` | `Assets/ANIMOL/Sprites/UI/Common/spr_ui_common_multi_art_casual_idle.png` | `40c042655665e864eac9c13567d05d4d` |
| `Assets/ANIMOL/Sprites/UI/Common/spr_ui_common_missing_ui_v1_ui_multi_art_private.png` | `Assets/ANIMOL/Sprites/UI/Common/spr_ui_common_multi_art_private_idle.png` | `557120d8cb78c164a9999f031c1fc539` |
| `Assets/ANIMOL/Sprites/UI/Common/spr_ui_common_missing_ui_v1_ui_multi_art_ranked.png` | `Assets/ANIMOL/Sprites/UI/Common/spr_ui_common_multi_art_ranked_idle.png` | `64725ddd40721d54b99c2ee5c42370a1` |
| `Assets/ANIMOL/Sprites/UI/Common/spr_ui_common_missing_ui_v1_ui_multi_avatar_empty.png` | `Assets/ANIMOL/Sprites/UI/Common/spr_ui_common_multi_avatar_empty.png` | `301e9a29d3fe35f4f81a26e0fc2d6999` |
| `Assets/ANIMOL/Sprites/UI/Common/spr_ui_common_missing_ui_v1_ui_multi_mode_card_frame.png` | `Assets/ANIMOL/Sprites/UI/Common/spr_ui_common_multi_mode_card_frame_idle.png` | `1dcb115a94fa1024896321889dc9c5c9` |
| `Assets/ANIMOL/Sprites/UI/Common/spr_ui_common_missing_ui_v1_ui_multi_player_card_frame.png` | `Assets/ANIMOL/Sprites/UI/Common/spr_ui_common_multi_player_card_frame_idle.png` | `f874dd381ed84f14c9adebb0f478e7bd` |
| `Assets/ANIMOL/Sprites/UI/Settings/spr_ui_settings_missing_ui_v1_ui_control_layout_preview.png` | `Assets/ANIMOL/Sprites/UI/Settings/spr_ui_settings_control_layout_preview.png` | `429e58b97707cfa4ea463dc3fbadac6e` |
| `Assets/ANIMOL/Sprites/UI/Settings/spr_ui_settings_missing_ui_v1_ui_icon_account.png` | `Assets/ANIMOL/Sprites/UI/Settings/spr_ui_settings_icon_account_idle.png` | `7fc11c6fc1760894eb4b05b3589d6201` |
| `Assets/ANIMOL/Sprites/UI/Settings/spr_ui_settings_missing_ui_v1_ui_icon_hand_left.png` | `Assets/ANIMOL/Sprites/UI/Settings/spr_ui_settings_icon_hand_left_idle.png` | `cf5e2c9f4c7a6764f9baf2752f2061cd` |
| `Assets/ANIMOL/Sprites/UI/Settings/spr_ui_settings_missing_ui_v1_ui_icon_hand_right.png` | `Assets/ANIMOL/Sprites/UI/Settings/spr_ui_settings_icon_hand_right_idle.png` | `d0d02ded78a985b44b31a56bc6cef906` |
| `Assets/ANIMOL/Sprites/UI/Settings/spr_ui_settings_missing_ui_v1_ui_icon_language.png` | `Assets/ANIMOL/Sprites/UI/Settings/spr_ui_settings_icon_language_idle.png` | `3008e0fb8f9630f4f9445665b9146d53` |
| `Assets/ANIMOL/Sprites/UI/Settings/spr_ui_settings_missing_ui_v1_ui_icon_local_save.png` | `Assets/ANIMOL/Sprites/UI/Settings/spr_ui_settings_icon_local_save_idle.png` | `058a345aa088c45429cd2431a39d73d2` |
| `Assets/ANIMOL/Sprites/UI/Settings/spr_ui_settings_missing_ui_v1_ui_icon_music.png` | `Assets/ANIMOL/Sprites/UI/Settings/spr_ui_settings_icon_music_idle.png` | `b169433630f085041a003131993331ec` |
| `Assets/ANIMOL/Sprites/UI/Settings/spr_ui_settings_missing_ui_v1_ui_icon_mute.png` | `Assets/ANIMOL/Sprites/UI/Settings/spr_ui_settings_icon_mute_idle.png` | `0fe7cf5b58306d047803ec38471fb58a` |
| `Assets/ANIMOL/Sprites/UI/Settings/spr_ui_settings_missing_ui_v1_ui_icon_opacity.png` | `Assets/ANIMOL/Sprites/UI/Settings/spr_ui_settings_icon_opacity_idle.png` | `10d372b8a78551f498254d94d13b508d` |
| `Assets/ANIMOL/Sprites/UI/Settings/spr_ui_settings_missing_ui_v1_ui_icon_sound.png` | `Assets/ANIMOL/Sprites/UI/Settings/spr_ui_settings_icon_sound_idle.png` | `7af9ceba0c9fa8e4a8c27b64578cf9a6` |
| `Assets/ANIMOL/Sprites/UI/Settings/spr_ui_settings_missing_ui_v1_ui_icon_text_size.png` | `Assets/ANIMOL/Sprites/UI/Settings/spr_ui_settings_icon_text_size_idle.png` | `c89756e1723d9cb4b8fe9abe276dbf78` |
| `Assets/ANIMOL/Sprites/UI/Settings/spr_ui_settings_missing_ui_v1_ui_icon_touch_controls.png` | `Assets/ANIMOL/Sprites/UI/Settings/spr_ui_settings_icon_touch_controls_idle.png` | `9bfc2cb2fae9018468aef90bbe771939` |
| `Assets/ANIMOL/Sprites/UI/Settings/spr_ui_settings_missing_ui_v1_ui_icon_vibration.png` | `Assets/ANIMOL/Sprites/UI/Settings/spr_ui_settings_icon_vibration_idle.png` | `650861df17731774eb653efce1a4a062` |
| `Assets/ANIMOL/Sprites/UI/Store/spr_ui_store_missing_ui_v1_ui_icon_restore_purchase.png` | `Assets/ANIMOL/Sprites/UI/Store/spr_ui_store_icon_restore_purchase_idle.png` | `29629637e3d09ca479ec3706d88fded4` |
| `Assets/ANIMOL/Sprites/UI/Store/spr_ui_store_missing_ui_v1_ui_store_art_account_growth_pack.png` | `Assets/ANIMOL/Sprites/UI/Store/spr_ui_store_store_art_account_growth_pack_idle.png` | `cce019ad21c231742abda0ff77955e5d` |
| `Assets/ANIMOL/Sprites/UI/Store/spr_ui_store_missing_ui_v1_ui_store_art_emote_bundle.png` | `Assets/ANIMOL/Sprites/UI/Store/spr_ui_store_store_art_emote_bundle_idle.png` | `ba15afe0d870abf49843a85dc9c91886` |
| `Assets/ANIMOL/Sprites/UI/Store/spr_ui_store_missing_ui_v1_ui_store_price_plate.png` | `Assets/ANIMOL/Sprites/UI/Store/spr_ui_store_store_price_plate_idle.png` | `53e26482243b0be448f66d2cf8f0c894` |
| `Assets/ANIMOL/Sprites/UI/Store/spr_ui_store_missing_ui_v1_ui_store_product_frame.png` | `Assets/ANIMOL/Sprites/UI/Store/spr_ui_store_store_product_frame_idle.png` | `33f300320e6fc9a48b444591ec9c6587` |
| `Assets/ANIMOL/Sprites/UI/Common/spr_ui_common_portrait_art_v1_button_ad.png` | `Assets/ANIMOL/Sprites/UI/MainMenu/spr_ui_main_portrait_button_ad_idle.png` | `3a0e589b8854a7d4da328cf44fa9b0e5` |
| `Assets/ANIMOL/Sprites/UI/Common/spr_ui_common_portrait_art_v1_button_back.png` | `Assets/ANIMOL/Sprites/UI/MainMenu/spr_ui_main_portrait_button_back_idle.png` | `3430596a25d787f4a8bc049dc685efb1` |
| `Assets/ANIMOL/Sprites/UI/Common/spr_ui_common_portrait_art_v1_button_play.png` | `Assets/ANIMOL/Sprites/UI/MainMenu/spr_ui_main_portrait_button_play_idle.png` | `f8f0620f164e5404ba85c80809a0f7f8` |
| `Assets/ANIMOL/Sprites/UI/Common/spr_ui_common_portrait_art_v1_button_settings.png` | `Assets/ANIMOL/Sprites/UI/MainMenu/spr_ui_main_portrait_button_settings_idle.png` | `39ec43aa999acd04ab1070e42014977e` |
| `Assets/ANIMOL/Sprites/UI/Common/spr_ui_common_portrait_art_v1_button_shop.png` | `Assets/ANIMOL/Sprites/UI/MainMenu/spr_ui_main_portrait_button_shop_idle.png` | `6dee8aca7141e5646a16d1d387754681` |
| `Assets/ANIMOL/Sprites/UI/Common/spr_ui_common_portrait_art_v1_capsule_currency.png` | `Assets/ANIMOL/Sprites/UI/MainMenu/spr_ui_main_portrait_capsule_currency_idle.png` | `c07756880e3541140a82ca33b83694c7` |
| `Assets/ANIMOL/Sprites/UI/Common/spr_ui_common_portrait_art_v1_card_campaign.png` | `Assets/ANIMOL/Sprites/UI/MainMenu/spr_ui_main_portrait_card_campaign_idle.png` | `f9f0cdc42e4ab1242a45fc8f84153ccc` |
| `Assets/ANIMOL/Sprites/UI/Common/spr_ui_common_portrait_art_v1_card_competition.png` | `Assets/ANIMOL/Sprites/UI/MainMenu/spr_ui_main_portrait_card_competition_idle.png` | `5324ab9ecafbc9b47840ba56ef221cfa` |
| `Assets/ANIMOL/Sprites/UI/Common/spr_ui_common_portrait_art_v1_card_cooperation.png` | `Assets/ANIMOL/Sprites/UI/MainMenu/spr_ui_main_portrait_card_cooperation_idle.png` | `a3868c328e647a24c8c35bf135731af3` |
| `Assets/ANIMOL/Sprites/UI/Common/spr_ui_common_portrait_art_v1_icon_coin.png` | `Assets/ANIMOL/Sprites/UI/Common/spr_ui_common_portrait_icon_coin_idle.png` | `6f8c9313d38e0dd4792001fef4f316c6` |

### 실행 검증

| 검사 | 실제 결과 |
|---|---|
| 추가 변경 후 Unity 재임포트·C# 컴파일 | 성공 |
| YAML 자산 776개 / GUID 참조 19,160개 | 누락 GUID 0 |
| Assets 전체 `.meta` 중복 GUID | 0 |
| 기본 define 및 컴파일된 구형 제작기 대표 타입 6종 | default define 없음 / 타입 0 |
| 구형 제작기 메뉴·InitializeOnLoad 자동 진입 | 0 |
| Bootstrap/Lobby/Results 누락 스크립트·오브젝트 참조 | 0 |
| PNG·`.meta` 내용, Legacy 원본, 비대상 파일 해시 | 불일치 0 |
| CommercialPolishPolicyTests + CanonicalSpritePathTests | EditMode 5/5 통과 |
| MenuThemeFlowTests + CutoverPortraitUiTests | PlayMode 4/4 통과 |
| 실제 Game View 해상도 / 화면 캡처 | 1080×1920 검증 / 로비·설정·기존 HUD 3개 |
| Android DEV 빌드 | Succeeded, errors 0, warnings 25; APK 57,460,097 bytes |
| Android 실기기 빈 Sprite/핑크 머티리얼/UI 표시 | 미실행 — adb 연결 기기 0 |
| P01 QA 3종·P01 Validate·10fps 보존 | 대상 없음 — 미실행 |

로비에는 기존 HUD가 등록되어 있지 않으므로 저장소의 SC15_GameplayHud 프리팹을 테스트 중에만 임시 인스턴스화했다. 씬·프리팹을 저장하거나 새 게임플레이 장면을 제작하지 않았다. 캡처는 실제 1080×1920 Game View에서 실행한 UI를 동일 크기의 Canvas camera target으로 렌더한 증거다. 저장된 그림 3개의 near-magenta pixels는 모두 0이다.

시각 확인에서 로비의 일부 하단 안내문은 낮은 대비/하단 경계 근처 배치를 유지하고, 기존 HUD는 시간·스태미나·점프·동물 변경 문구를 유지한다. 이는 기존 UI의 현 상태이며 승인된 탑뷰 HUD로 표시하지 않는다. 새 탑뷰 화면 재제작은 이번 범위 밖이다.

첫 Android 빌드는 이미 존재하지 않는 과거 FreeShapeArt/V2/V4 캐시 소유자 3개 때문에 중단됐다. 파일과 GUID가 없는 것을 확인한 뒤 캐시 항목을 정리하고 현재 폰트 소유자 슬롯을 재인덱싱했다. 사전 검사는 Issues 0/ChangedSlots 0이며 빌드를 우회하지 않았다. 빌드가 자동 재직렬화한 폰트와 UnityConnectSettings는 기존 변경이 없던 파일에 한해 작업 전 해시와 일치하는 원본 바이트로 복구했다.

남은 미확정 항목: unknown 391개, P01 부재, 연결 기기 부재, 기존 HUD의 탑뷰 전용 재구성, Android 경고 25개. 경고에는 기존 TerrainEditorScreen의 CS0628과 TMP IL2CPP 큰 메서드 컴파일 알림 등이 포함된다. 기능 승인이나 장치 24종 구현 완료를 의미하지 않는다.

### 화면 및 검사 증거

- [로비 1080×1920](../Screenshots/TopdownCutover_20261013/sc01_lobby_1080x1920.png)
- [설정 1080×1920](../Screenshots/TopdownCutover_20261013/sc13_settings_1080x1920.png)
- [기존 HUD 프리팹 1080×1920](../Screenshots/TopdownCutover_20261013/sc15_gameplayhud_1080x1920.png)
- [캡처 픽셀 검사](../Screenshots/TopdownCutover_20261013/capture_checks.json)
- [임포트·자동 진입점](../Docs/Validation/TopdownCutover/supplement-import-audit.json)
- [활성 자산 GUID·컴파일 타입 검사](../Docs/Validation/TopdownCutover/supplement-reference-audit.json)
- [NamedArt 빌드 사전 검사](../Docs/Validation/TopdownCutover/namedart-build-preflight.json)
- [모바일 빌드](../Docs/Validation/TopdownCutover/mobile-build.json)
- [추가 EditMode](../Docs/Validation/TopdownCutover/supplement-editmode.xml) / [추가 PlayMode](../Docs/Validation/TopdownCutover/supplement-playmode.xml)

APK는 작업용 외부 저널의 `android/animol-cutover.apk`에 생성했으며 캐시·빌드 산출물을 저장소에 커밋하지 않는다. 추가 작업의 실제 수정 파일 목록은 커밋 경로 목록과 위 이동표/원본 사본 표에서 확인할 수 있다. 기존 사용자 수정과 미추적 파일은 계속 보존한다.

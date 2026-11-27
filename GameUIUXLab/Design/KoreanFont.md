# 한글 TextMeshPro SDF

## 적용된 폰트

- 기본: Assets/Fonts/Korean/Pretendard Korean SDF.asset
- 폴백: Assets/Fonts/Korean/Pretendard Korean Dynamic SDF.asset
- 원본: 기존 Assets/TextMesh Pro/Fonts/Pretendard-Regular.ttf

프로젝트에 이미 포함된 Pretendard 원본을 사용했다.
OS에 설치된 글꼴에 의존하지 않는다.
현재 씬과 카드·체력 바·토스트 프리팹의 TMP 텍스트, TMP Settings의 기본 폰트를 연결했다.
기존 Pretendard-Regular SDF는 수정하지 않고 새 에셋을 추가했다.

## 글자가 빠지던 원인과 처리

기존 SDF는 문자·글리프 테이블이 비어 있고 Multi Atlas가 꺼져 있었다.
현재 게임 문구를 정적 SDF로 미리 생성하고, 아직 생성하지 않은 글자는 원본 TTF를
참조하는 동적 멀티 아틀라스 폴백에서 처리하도록 분리했다.
정적 문자 데이터는 플레이 모드 전환이나 빌드 시 삭제되지 않는다.

현재 씬·프리팹·카드·사건·소스 코드의 한글과 기본 영문·숫자·기호를 포함한다.
아틀라스는 2048×2048 PNG, SDF 샘플 크기는 48이며 PNG를 선형·무압축·밉맵 없음으로 임포트한다.
거리 값이 들어 있는 알파 채널을 임의로 투명 이미지 보정하거나 압축하지 않는다.
PrebakedCharacters.txt에 미리 생성한 문자를 기록했다.

## 새 UI 또는 새 문구

새 TMP 텍스트의 Font Asset에 Pretendard Korean SDF를 선택하면 된다.
기본 폰트로도 설정했기 때문에 새 텍스트의 기본값에 적용된다.
새 문구에 미리 생성하지 않은 한글이 있어도 동적 폴백이 처리한다.
UI 문구를 많이 추가한 뒤 Tools → Fonts → Install Korean SDF를 실행하면
현재 문자를 다시 미리 생성하고 씬과 프리팹 연결도 갱신한다.
이 메뉴는 현재 CardGame 씬과 Assets/Prefabs 및 GeneratedEnemyHealthBar의 폰트 설정을 갱신한다.

## 검증

```text
Unity.exe -batchmode -projectPath "<GameUIUXLab 경로>" -executeMethod KoreanFontSetup.ConfigureAndRun -logFile "<로그 경로>"
```

원본의 한글 완성형 11,172자, 현재 문자의 정적 SDF 포함 여부, 동적 추가와 아틀라스 확장,
씬/프리팹/기본 폰트 연결을 검사한 뒤 기존 맵·사건·전투·보상 검증을 실행한다.
map-preview.png와 event-preview.png에서 실제 한글 표시도 확인할 수 있다.

# 카드 이미지 제작 정보

생성: 내장 image_gen 도구. 아래 원본 프롬프트로 각각 별도 생성했습니다.

- card-frame-v1.png: BackGround 이미지, 카드 전체 220×320 영역. 외곽 모서리 투명.
- strike-v1.png: 타격 카드 Artwork 이미지, 190×135 영역.
- defend-v1.png: 방어 카드 Artwork 이미지, 190×135 영역.

글자는 이미지에 포함하지 않고 기존 CostText, NameText, TypeText, DescriptionText로 표시합니다.
CardData.Artwork에 Sprite를 지정하면 CardUI.SetCard에서 카드별 일러스트를 표시합니다.
새 카드의 이미지는 Inspector에서 개별 설정할 수 있습니다.
Tools → Cards → Apply Generated Visuals는 기본 공격·스킬 이미지와 프리팹을 다시 연결합니다.

## frame

Use case: stylized-concept. Asset type: Unity UGUI card BACKGROUND sprite, NOT a full finished card or a mockup. Create one production ready front-facing flat card frame, exact portrait aspect ratio 11:16, preferably 880 x 1280 pixels. This is for a 220 x 320 logical-unit card with separate Unity overlay text and illustration. Restrained handcrafted dark-fantasy deckbuilding roguelike look, aged dark brass edging and deep charcoal slate, warm parchment lower description area. Full card silhouette fills canvas, only rounded OUTER corners transparent, no surrounding scene, no perspective, no shadows outside silhouette. Layout in 220x320 logical units: top header y=8..48 includes an EMPTY small circular energy medallion at x=10..55 y=10..55, EMPTY name plaque x=60..205 y=10..45. A quiet illustration backing rectangle exactly x=15..205 y=52..187; this will be fully covered by a separate artwork sprite, so DO NOT paint any figures here. Thin horizontal EMPTY type divider at y=180..208. Broad light parchment description panel x=20..200 y=220..300. Fine subtle weathering and chamfered gold outlines, large calm readable blank regions, neat symmetric bottom corners, professional game UI asset. NO letters, NO numbers, NO words, NO symbols in medallion, NO sword or shield illustration. Actual transparent alpha only beyond the rounded card silhouette. Make a single isolated card background.

## strike

Use case: stylized-concept. Asset type: a standalone illustration sprite for the Artwork child of a Unity UGUI card. Landscape aspect ratio 38:27 (target 1140x810 pixels), image fills entire rectangular canvas. Hand-painted dark-fantasy roguelike card illustration, readable at 190 x 135 UI pixels, restrained rich pigments, dramatic bold graphic silhouette, fine painterly texture. Subject: close-up single steel longsword making a forceful diagonal slash, with one clear crimson-orange sweeping arc and a burst of sparks against a dark smoky stone backdrop. Emphasize the blade and a gauntleted grip only, no full human character, no enemy, no gore. Dynamic but uncluttered strong central composition with margins so the tip is not cropped. This is ATTACK card artwork only. No card frame, no border, no text, no typography, no numbers, no icons, no watermark. Warm steel, crimson and brass accents, dramatic directional lighting. Opaque background.

## defend

Use case: stylized-concept. Asset type: a standalone illustration sprite for the Artwork child of a Unity UGUI card. Landscape aspect ratio 38:27 (target 1140x810 pixels), image fills entire rectangular canvas. Hand-painted dark-fantasy roguelike card illustration, readable at 190 x 135 UI pixels, restrained rich pigments, bold graphic silhouette, fine painterly texture. Subject: one sturdy medieval heater shield with dark steel face and aged brass rim, held steady by a barely visible armored forearm, intercepting a few bright sparks on the upper edge, with a subtle teal protective glow. Dark smoky stone backdrop. No crest letters, no full character, no enemy, no gore. Clear strong central shield with enough margins to keep complete silhouette in frame. This is DEFENSE card artwork only. No card frame, no border, no text, no typography, no numbers, no icons, no watermark. Cool slate and teal harmonize with worn brass. Opaque background.

# Screen
## Title Screen 
- 시작 버튼
- 종료 버튼
- 설정 버튼
- 카드 모음집


## PlayHud
- 카드 UI
- 플레이어 체력바
- 적 체력바
- 적 행동
- 카드 덱
- 턴 수 
- 플레이 시간 

## PauseMenu
- 계속하기 버튼
- 설정 버튼
- 타이틀 돌아가기 버튼
- 저장 후 종료 버튼

## ResultScreen
- 처치한 적 수
- 총 플레이 시간
- 총합 점수
- 다시하기 버튼
 

 # input interface
- 카드 선택 : 마우스 좌클릭 유지 후 드래그 / 단축키 설정
- 카드 선택 취소 : 마우스 우클릭 / 좌클릭 때기
- 설정 : esc / 마우스 좌클릭 UI 버튼 
- 덱 확인 : 마우스 좌클릭 UI 버튼 


# UI Prefab 후보
- Card UI
- HealthBarUI
- EnemyUI

## Canvas

Canvas
|- UI
   |- Screen Title
   |- PlayHUD
     |- PlayerHand
       |- CardUI
     |- HealthBar
     |- Time
     |- Score
     |- TurnText
     |- EndTurnButton
     |- EnemyHUD
   |- Screen Pause
   |- Screen Card Deck
   |- Screen Result
   
## 리소스와 콘셉트
- 글꼴 : 한글 
- 버튼 아이콘 : 모양을 따라 간다. 설정 버튼은 톱니, 덱 확인은 카드 
- 카드 상태 : 손에 있는 카드에 마우스를 가져다 대면 카드 테두리가 파랗게 빛난다. 그리고 부각되게 선택한 카드를 위로 올린다
- 턴 종료 버튼 : 마우스를 가져다 대면 불이 들어오고 누르면 턴 종료가 되었음으로 불이 꺼지는 방향



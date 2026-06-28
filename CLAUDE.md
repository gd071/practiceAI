# practiceAI — Unity プロジェクト

## 環境
- Unity 2022+（URP）
- **新 Input System**（`InputSystemUIInputModule` を使用。`StandaloneInputModule` は不可）
- 2D 正投影カメラ（orthographic size 5）

## ゲーム概要
UIそのものが攻撃・演出になるメタ的な必殺技が特徴のターン制RPG。  
キャラクターが「時間」「記憶」「空間」「事象」「次元」などに干渉する能力を持つ。

## フォルダ構成
```
Assets/
  Scripts/
    Managers/   → GameManager.cs, BattleManager.cs
    Battle/     → BattleUnit.cs, TurnManager.cs, BattleBootstrap.cs
    UI/         → BattleUI.cs
    Data/       → CharacterData.cs (ScriptableObject)
  ScriptableObjects/  → キャラデータ格納先
  Scenes/
    SampleScene.unity  → 現在の作業シーン
```

## シーン構成（SampleScene）
| GameObject    | コンポーネント                              |
|---------------|---------------------------------------------|
| Main Camera   | Camera（orthographic）, AudioListener, URP  |
| EventSystem   | EventSystem, InputSystemUIInputModule        |
| _Bootstrap    | BattleBootstrap, BattleUI                   |

※ `_BattleManager`・`_GameManager` はランタイムに BattleBootstrap が自動生成する。

## 主要クラスの関係
```
BattleBootstrap.Start()
  └─ EnsureManagers()       # _BattleManager(BattleManager+TurnManager), _GameManager 生成
  └─ BattleManager.StartBattle(allies, enemies)
       ├─ OnBattleStarted   → BattleUI.HandleBattleStarted()  # ステータスバー生成
       └─ DelayedTurnStart  # 1フレーム待機後 TurnManager.Initialize()
            └─ OnTurnStarted → BattleUI(UI更新) / BattleManager(敵AI)
```

## enum 定義（CharacterData.cs）
```csharp
CharacterType: Protagonist/Heroine/BestFriend/Shota/Witch/Gal/Android/Ghost/Princess
UltimateType:  TimeStop/MemoryReplay/SpaceRift/EventRewrite/DimensionCall/
               AbyssCall/SatelliteBeam/GhostRush/Payoff
```

## 実装状況
- [x] GameManager（シングルトン, GameState 管理）
- [x] CharacterData（ScriptableObject）
- [x] BattleUnit（HP/MP/ダメージ/必殺技）
- [x] TurnManager（SPD順ターン管理）
- [x] BattleManager（コマンド処理, 敵AI, 勝敗判定）
- [x] BattleBootstrap（ランタイムでユニット生成・バトル開始）
- [x] BattleUI（Canvas・HP/MPバー・コマンドボタン・ログをコードで全生成）
- [ ] UIEffectManager（必殺技時のUI破壊演出）— フェーズ2
- [ ] スキルシステム — フェーズ2
- [ ] アイテムシステム — フェーズ2

## 注意事項
- `BattleUI` に `[DefaultExecutionOrder(-10)]` 付与済み（Awake を BattleBootstrap より先に実行）
- `BattleManager.StartBattle()` は `OnBattleStarted` を先に発火してから `DelayedTurnStart` コルーチンで TurnManager を初期化する（UI 描画タイミング保証のため）
- Font は `Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")` を使用
- `FindObjectOfType` は非推奨 → `FindFirstObjectByType` を使う

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

## 実装状況（完全版）
- [x] GameManager（シングルトン, GameState 管理）
- [x] CharacterData（ScriptableObject）
- [x] BattleUnit（HP/MP/ダメージ/必殺技）
- [x] TurnManager（**砂時計100カウント制**：パーティ共有カウント、パスで増加、尽きると敵フェーズ）
- [x] BattleManager（カウント制コマンド、全9種必殺技、厄災ボスAI、敗北=ループ処理）
- [x] BattleBootstrap（味方9人＋厄災カナタ＋アンドロイド×2 を生成、形状バリエーション付きスプライト）
- [x] BattleUI（砂時計バー・9人分のクリック可能キャラカード・ターン終了ボタン・周回数表示）
- [x] UIEffectManager（全9種のメタUI必殺技演出：次元召喚=UI消滅+プレイヤー直接クリック攻撃、
      衛星ビーム=巨大PUSHボタン、ペイオフ=ショップUI、記憶再生=画面反転+隕石、深淵=全UI文字化け、
      幽霊突撃=UIが敵に飛ぶ、時間断絶=砂時計破砕、空間斬=パネル引き裂き、事象書換=ログ書き換え）
- [x] BattleVisuals（星空・脈動する隕石・地面ライン）
- [x] ループ構造（敗北→PlayerPrefs「LoopNumber」を進めてシーンリロード。195周目開始、勝利で真エンド）

## バトルシステム（PDF「ゲーム設定まとめ」準拠）
- パーティ共有の砂時計100カウント。攻撃20 / スキル30 / 必殺技45 / 救急10、パスは+15
- 好きな味方キャラのカードをクリックして行動キャラを自由に選択
- 主人公の時間断絶：カウント+40 ＋ 3回コスト0行動（FreeActions）
- 敵フェーズ：厄災のカナタ（隕石/落雷/業火/暗黒、HP50%以下で発狂2回行動）
- インタラクティブ必殺技は UIEffectManager.SequenceRunning を BattleManager が待機

## 注意事項
- `BattleUI` に `[DefaultExecutionOrder(-10)]` 付与済み（Awake を BattleBootstrap より先に実行）
- `BattleManager.StartBattle()` は `OnBattleStarted` を先に発火してから `DelayedTurnStart` コルーチンで TurnManager を初期化する（UI 描画タイミング保証のため）
- Font は `Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")` を使用
- `FindObjectOfType` は非推奨 → `FindFirstObjectByType` を使う

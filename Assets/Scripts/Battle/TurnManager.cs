using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum TurnPhase
{
    PlayerInput,   // プレイヤーコマンド入力待ち
    PlayerAction,  // プレイヤー行動実行中
    EnemyAction,   // 敵行動実行中
    TurnEnd        // フェーズ切り替え処理
}

/// <summary>
/// 砂時計カウント制ターン管理。
/// パーティ全体で 100 カウントを共有し、好きなキャラクターで消費する。
/// カウントが尽きる（またはターン終了）と敵フェーズへ移行する。
/// </summary>
public class TurnManager : MonoBehaviour
{
    public const int MaxCount = 100;

    public int Count { get; private set; }
    public int Round { get; private set; } = 1;

    public TurnPhase CurrentPhase { get; private set; }

    /// <summary>プレイヤーフェーズ中は選択中の味方、敵フェーズ中は行動中の敵。</summary>
    public BattleUnit ActiveUnit { get; private set; }

    private List<BattleUnit> _allies  = new List<BattleUnit>();
    private List<BattleUnit> _enemies = new List<BattleUnit>();

    public static event System.Action<BattleUnit> OnTurnStarted;
    public static event System.Action<TurnPhase>  OnPhaseChanged;
    public static event System.Action<int, int>   OnCountChanged;   // (current, max)
    public static event System.Action<int>        OnNewRoundStarted;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        OnTurnStarted     = null;
        OnPhaseChanged    = null;
        OnCountChanged    = null;
        OnNewRoundStarted = null;
    }

    // ================================================================
    public void Initialize(List<BattleUnit> allies, List<BattleUnit> enemies)
    {
        _allies  = allies;
        _enemies = enemies;
        Round    = 1;
        BeginPlayerPhase(firstRound: true);
    }

    public void BeginPlayerPhase(bool firstRound = false)
    {
        Count = MaxCount;
        OnCountChanged?.Invoke(Count, MaxCount);

        ActiveUnit = _allies.FirstOrDefault(a => a.IsAlive);
        ChangePhase(TurnPhase.PlayerInput);

        if (!firstRound) OnNewRoundStarted?.Invoke(Round);
        if (ActiveUnit != null) OnTurnStarted?.Invoke(ActiveUnit);
    }

    /// <summary>プレイヤーが行動キャラを切り替える。</summary>
    public bool SelectActor(BattleUnit unit)
    {
        if (CurrentPhase != TurnPhase.PlayerInput) return false;
        if (unit == null || unit.IsEnemy || !unit.IsAlive) return false;
        ActiveUnit = unit;
        OnTurnStarted?.Invoke(unit);
        return true;
    }

    /// <summary>カウントを消費する。足りなければ false。</summary>
    public bool TrySpend(int cost)
    {
        if (cost > Count) return false;
        Count -= cost;
        OnCountChanged?.Invoke(Count, MaxCount);
        return true;
    }

    /// <summary>カウントを追加する（パス・時間干渉など）。</summary>
    public void AddCount(int amount)
    {
        Count = Mathf.Clamp(Count + amount, 0, 999);
        OnCountChanged?.Invoke(Count, MaxCount);
    }

    /// <summary>敵フェーズ中に行動中の敵をセットする。</summary>
    public void SetActingEnemy(BattleUnit enemy)
    {
        ActiveUnit = enemy;
        OnTurnStarted?.Invoke(enemy);
    }

    /// <summary>選択中キャラが倒れた場合などに生存者へ引き継ぐ。</summary>
    public void EnsureActiveAlive()
    {
        if (ActiveUnit != null && ActiveUnit.IsAlive && !ActiveUnit.IsEnemy) return;
        ActiveUnit = _allies.FirstOrDefault(a => a.IsAlive);
        if (ActiveUnit != null && CurrentPhase == TurnPhase.PlayerInput)
            OnTurnStarted?.Invoke(ActiveUnit);
    }

    public void ChangePhase(TurnPhase phase)
    {
        CurrentPhase = phase;
        OnPhaseChanged?.Invoke(phase);
    }

    /// <summary>敵フェーズ完了 → 次ラウンドのプレイヤーフェーズへ。</summary>
    public void NextRound()
    {
        Round++;
        BeginPlayerPhase();
    }
}

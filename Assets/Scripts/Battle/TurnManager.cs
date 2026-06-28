using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum TurnPhase
{
    PlayerInput,   // プレイヤーコマンド入力待ち
    PlayerAction,  // プレイヤー行動実行中
    EnemyAction,   // 敵行動実行中
    TurnEnd        // ターン終了処理
}

public class TurnManager : MonoBehaviour
{
    public TurnPhase CurrentPhase { get; private set; }
    public int TurnCount { get; private set; } = 1;

    public List<BattleUnit> TurnOrder { get; private set; } = new List<BattleUnit>();

    private int _currentUnitIndex = 0;
    public BattleUnit ActiveUnit => TurnOrder.Count > 0 ? TurnOrder[_currentUnitIndex] : null;

    public static event System.Action<BattleUnit> OnTurnStarted;
    public static event System.Action<int>        OnNewRoundStarted;
    public static event System.Action<TurnPhase>  OnPhaseChanged;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        OnTurnStarted     = null;
        OnNewRoundStarted = null;
        OnPhaseChanged    = null;
    }

    public void Initialize(List<BattleUnit> allies, List<BattleUnit> enemies)
    {
        TurnOrder = allies.Concat(enemies)
                          .Where(u => u.IsAlive)
                          .OrderByDescending(u => u.Data.spd)
                          .ToList();

        TurnCount           = 1;
        _currentUnitIndex   = 0;
        StartCurrentUnitTurn();
    }

    public void ChangePhase(TurnPhase phase)
    {
        CurrentPhase = phase;
        OnPhaseChanged?.Invoke(phase);
    }

    public void StartCurrentUnitTurn()
    {
        if (ActiveUnit == null) return;
        ChangePhase(ActiveUnit.IsEnemy ? TurnPhase.EnemyAction : TurnPhase.PlayerInput);
        OnTurnStarted?.Invoke(ActiveUnit);
    }

    public void EndCurrentUnitTurn()
    {
        ChangePhase(TurnPhase.TurnEnd);
        AdvanceToNextUnit();
    }

    private void AdvanceToNextUnit()
    {
        TurnOrder.RemoveAll(u => !u.IsAlive);
        if (TurnOrder.Count == 0) return;

        _currentUnitIndex++;
        if (_currentUnitIndex >= TurnOrder.Count)
        {
            _currentUnitIndex = 0;
            TurnCount++;
            OnNewRoundStarted?.Invoke(TurnCount);
        }

        StartCurrentUnitTurn();
    }

    public void RemoveUnit(BattleUnit unit)
    {
        int idx = TurnOrder.IndexOf(unit);
        TurnOrder.Remove(unit);
        if (idx >= 0 && idx < _currentUnitIndex)
            _currentUnitIndex = Mathf.Max(0, _currentUnitIndex - 1);
    }
}

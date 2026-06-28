using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum BattleCommand
{
    Attack,    // 攻撃
    Skill,     // スキル
    Ultimate,  // 必殺技
    Pass,      // パス
    Item       // アイテム
}

public enum BattleResult
{
    None,
    Victory,
    Defeat
}

public class BattleManager : MonoBehaviour
{
    public static BattleManager Instance { get; private set; }

    [Header("参照")]
    public TurnManager turnManager;

    public List<BattleUnit> AllyUnits  { get; private set; } = new List<BattleUnit>();
    public List<BattleUnit> EnemyUnits { get; private set; } = new List<BattleUnit>();

    public BattleResult CurrentResult { get; private set; } = BattleResult.None;
    public bool IsBattleActive => CurrentResult == BattleResult.None;

    // ---- イベント ----
    public static event System.Action<List<BattleUnit>, List<BattleUnit>> OnBattleStarted;
    public static event System.Action<BattleUnit, UltimateType>           OnUltimateActivated;
    public static event System.Action<BattleResult>                       OnBattleEnded;
    public static event System.Action<BattleCommand>                      OnCommandSelected;
    /// <summary>バトルログ文字列の通知。UIはこれを購読してログエリアに表示する。</summary>
    public static event System.Action<string>                             OnBattleLog;

    // Domain Reload 無効時にも static を確実にリセット
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        Instance            = null;
        OnBattleStarted     = null;
        OnUltimateActivated = null;
        OnBattleEnded       = null;
        OnCommandSelected   = null;
        OnBattleLog         = null;
    }

    // ================================================================
    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void OnEnable()
    {
        BattleUnit.OnUnitDefeated  += HandleUnitDefeated;
        TurnManager.OnTurnStarted  += HandleTurnStarted;
    }

    void OnDisable()
    {
        BattleUnit.OnUnitDefeated  -= HandleUnitDefeated;
        TurnManager.OnTurnStarted  -= HandleTurnStarted;
    }

    // ================================================================
    // バトル開始
    // ================================================================
    public void StartBattle(List<BattleUnit> allies, List<BattleUnit> enemies)
    {
        AllyUnits     = allies;
        EnemyUnits    = enemies;
        CurrentResult = BattleResult.None;

        OnBattleStarted?.Invoke(allies, enemies);
        // UI が 1 フレームで描画されてからターンを開始する
        StartCoroutine(DelayedTurnStart(allies, enemies));
    }

    private IEnumerator DelayedTurnStart(List<BattleUnit> allies, List<BattleUnit> enemies)
    {
        yield return null;
        turnManager.Initialize(allies, enemies);
        Log("バトル開始！");
    }

    // ================================================================
    // コマンド受付
    // ================================================================
    public void SelectCommand(BattleCommand command, BattleUnit target = null)
    {
        if (!IsBattleActive) return;

        BattleUnit actor = turnManager.ActiveUnit;
        if (actor == null || actor.IsEnemy) return;

        // ボタンをすぐ無効化（PlayerAction フェーズへ）
        turnManager.ChangePhase(TurnPhase.PlayerAction);
        StartCoroutine(ExecutePlayerCommand(actor, command, target));
    }

    private IEnumerator ExecutePlayerCommand(BattleUnit actor, BattleCommand command, BattleUnit target)
    {
        bool success = true;

        switch (command)
        {
            case BattleCommand.Attack:
                ExecuteAttack(actor, target ?? GetRandomEnemy());
                Log($"{actor.UnitName} の攻撃！");
                break;

            case BattleCommand.Skill:
                success = ExecuteSkill(actor, target ?? GetRandomEnemy());
                if (success) Log($"{actor.UnitName} のスキル！");
                break;

            case BattleCommand.Ultimate:
                success = ExecuteUltimate(actor, target ?? GetRandomEnemy());
                if (success) Log($"★ {actor.UnitName} の必殺技【{actor.Data.ultimateName}】！！");
                break;

            case BattleCommand.Pass:
                ExecutePass(actor);
                break;

            case BattleCommand.Item:
                Log("アイテムはまだ使えない…");
                break;
        }

        if (!success)
        {
            // 失敗時はターンを消費しない → 入力待ちに戻す
            turnManager.ChangePhase(TurnPhase.PlayerInput);
            yield break;
        }

        OnCommandSelected?.Invoke(command);
        yield return new WaitForSeconds(0.5f);

        if (IsBattleActive)
            turnManager.EndCurrentUnitTurn();
    }

    // ================================================================
    // 敵 AI
    // ================================================================
    private void HandleTurnStarted(BattleUnit unit)
    {
        if (!unit.IsEnemy || !IsBattleActive) return;
        StartCoroutine(EnemyTurn(unit));
    }

    private IEnumerator EnemyTurn(BattleUnit enemy)
    {
        yield return new WaitForSeconds(0.8f);

        BattleUnit target = GetRandomAlly();
        if (target != null)
        {
            ExecuteAttack(enemy, target);
            Log($"{enemy.UnitName} の攻撃！");
        }

        yield return new WaitForSeconds(0.5f);

        if (IsBattleActive)
            turnManager.EndCurrentUnitTurn();
    }

    // ================================================================
    // 行動実行
    // ================================================================
    private void ExecuteAttack(BattleUnit attacker, BattleUnit target)
    {
        if (target == null) return;
        int dmg = attacker.GetBaseAttackDamage();
        int actual = target.TakeDamage(dmg);
        Log($"  → {target.UnitName} に {actual} ダメージ！");
    }

    /// <summary>スキル：ATK×1.5 のダメージ、MP15消費。MP不足で失敗。</summary>
    private bool ExecuteSkill(BattleUnit actor, BattleUnit target)
    {
        const int mpCost = 15;
        if (!actor.ConsumeMP(mpCost))
        {
            Log("MP が足りない！");
            return false;
        }
        if (target != null)
        {
            int dmg    = Mathf.RoundToInt(actor.Data.atk * 1.5f);
            int actual = target.TakeDamage(dmg);
            Log($"  → {target.UnitName} に {actual} ダメージ！");
        }
        return true;
    }

    /// <summary>必殺技：MP消費。不足で失敗。</summary>
    private bool ExecuteUltimate(BattleUnit actor, BattleUnit primaryTarget)
    {
        if (!actor.TryUseUltimate())
        {
            Log("MP が足りない！");
            return false;
        }

        OnUltimateActivated?.Invoke(actor, actor.Data.ultimateType);

        switch (actor.Data.ultimateType)
        {
            case UltimateType.TimeStop:
                // 時間停止：全敵に ATK×2 ダメージ
                foreach (var e in EnemyUnits)
                {
                    int a = e.TakeDamage(actor.Data.atk * 2);
                    Log($"  → {e.UnitName} に {a} ダメージ！");
                }
                break;

            case UltimateType.MemoryReplay:
                // 記憶再生：全味方を ATK 分回復
                foreach (var ally in AllyUnits)
                {
                    ally.Heal(actor.Data.atk);
                    Log($"  → {ally.UnitName} のHP が回復！");
                }
                break;

            // フェーズ2で個別実装予定
            case UltimateType.SpaceRift:
            case UltimateType.EventRewrite:
            case UltimateType.DimensionCall:
            case UltimateType.AbyssCall:
            case UltimateType.SatelliteBeam:
            case UltimateType.GhostRush:
            case UltimateType.Payoff:
                if (primaryTarget != null)
                {
                    int a = primaryTarget.TakeDamage(actor.Data.atk * 3);
                    Log($"  → {primaryTarget.UnitName} に {a} ダメージ！");
                }
                break;
        }
        return true;
    }

    /// <summary>パス：MP を少し回復して次のターンへ。</summary>
    private void ExecutePass(BattleUnit actor)
    {
        int restore = Mathf.Max(5, actor.Data.maxMP / 10);
        actor.RestoreMP(restore);
        Log($"{actor.UnitName} は体を整えた。（MP ＋{restore}）");
    }

    // ================================================================
    // 勝敗判定
    // ================================================================
    private void HandleUnitDefeated(BattleUnit unit)
    {
        Log($"{unit.UnitName} は倒れた！");
        turnManager.RemoveUnit(unit);
        if (!IsBattleActive) return;

        bool allEnemiesDead = EnemyUnits.TrueForAll(e => !e.IsAlive);
        bool allAlliesDead  = AllyUnits.TrueForAll(a => !a.IsAlive);

        if (allEnemiesDead)      EndBattle(BattleResult.Victory);
        else if (allAlliesDead)  EndBattle(BattleResult.Defeat);
    }

    private void EndBattle(BattleResult result)
    {
        CurrentResult = result;
        OnBattleEnded?.Invoke(result);

        if (result == BattleResult.Victory)
            GameManager.Instance?.ChangeState(GameState.Field);
        else
            GameManager.Instance?.GameOver();
    }

    // ================================================================
    // ユーティリティ
    // ================================================================
    private BattleUnit GetRandomEnemy()
    {
        var alive = EnemyUnits.FindAll(e => e.IsAlive);
        return alive.Count > 0 ? alive[Random.Range(0, alive.Count)] : null;
    }

    private BattleUnit GetRandomAlly()
    {
        var alive = AllyUnits.FindAll(a => a.IsAlive);
        return alive.Count > 0 ? alive[Random.Range(0, alive.Count)] : null;
    }

    private static void Log(string msg) => OnBattleLog?.Invoke(msg);
}

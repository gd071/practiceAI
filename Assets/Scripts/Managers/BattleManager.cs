using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum BattleCommand
{
    Attack,    // 攻撃（20カウント）
    Skill,     // スキル（30カウント）
    Ultimate,  // 必殺技（45カウント）
    Pass,      // パス（カウント+15）
    Item       // 救急（10カウント）
}

public enum BattleResult
{
    None,
    Victory,
    Defeat
}

/// <summary>
/// 砂時計カウント制バトルの中枢。
/// コマンド実行・全9種必殺技・厄災ボスAI・ループ（敗北→周回リセット）を扱う。
/// </summary>
public class BattleManager : MonoBehaviour
{
    public static BattleManager Instance { get; private set; }

    // ---- カウントコスト ----
    public const int CostAttack   = 20;
    public const int CostSkill    = 30;
    public const int CostUltimate = 45;
    public const int CostItem     = 10;
    public const int PassGain     = 15;

    [Header("参照")]
    public TurnManager turnManager;

    public List<BattleUnit> AllyUnits  { get; private set; } = new List<BattleUnit>();
    public List<BattleUnit> EnemyUnits { get; private set; } = new List<BattleUnit>();

    public BattleResult CurrentResult { get; private set; } = BattleResult.None;
    public bool IsBattleActive => CurrentResult == BattleResult.None;

    /// <summary>時間断絶で得た「コスト0行動」の残り回数。</summary>
    public int FreeActions { get; private set; }

    /// <summary>現在のループ周回数（PlayerPrefsで周回を跨いで保持）。</summary>
    public static int LoopNumber => PlayerPrefs.GetInt("LoopNumber", 195);

    private bool _bossEnraged;

    // ---- イベント ----
    public static event System.Action<List<BattleUnit>, List<BattleUnit>> OnBattleStarted;
    public static event System.Action<BattleUnit, UltimateType>           OnUltimateActivated;
    public static event System.Action<BattleResult>                       OnBattleEnded;
    public static event System.Action<BattleCommand>                      OnCommandSelected;
    public static event System.Action<string>                             OnBattleLog;

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

    void OnEnable()  => BattleUnit.OnUnitDefeated += HandleUnitDefeated;
    void OnDisable() => BattleUnit.OnUnitDefeated -= HandleUnitDefeated;

    // ================================================================
    // バトル開始
    // ================================================================
    public void StartBattle(List<BattleUnit> allies, List<BattleUnit> enemies)
    {
        AllyUnits     = allies;
        EnemyUnits    = enemies;
        CurrentResult = BattleResult.None;
        FreeActions   = 0;
        _bossEnraged  = false;

        OnBattleStarted?.Invoke(allies, enemies);
        StartCoroutine(DelayedTurnStart(allies, enemies));
    }

    private IEnumerator DelayedTurnStart(List<BattleUnit> allies, List<BattleUnit> enemies)
    {
        yield return null;
        turnManager.Initialize(allies, enemies);
        Log($"──── {LoopNumber}周目 ────");
        Log("厄災のカナタが立ちはだかる…！");
    }

    // ================================================================
    // コマンド受付（プレイヤー）
    // ================================================================
    /// <summary>UIからの行動キャラ切り替え。</summary>
    public void SelectActor(BattleUnit unit)
    {
        if (!IsBattleActive) return;
        turnManager.SelectActor(unit);
    }

    public void SelectCommand(BattleCommand command, BattleUnit target = null)
    {
        if (!IsBattleActive) return;
        if (turnManager.CurrentPhase != TurnPhase.PlayerInput) return;

        BattleUnit actor = turnManager.ActiveUnit;
        if (actor == null || actor.IsEnemy || !actor.IsAlive) return;

        turnManager.ChangePhase(TurnPhase.PlayerAction);
        StartCoroutine(ExecutePlayerCommand(actor, command, target));
    }

    /// <summary>「ターン終了」ボタン：残カウントを捨てて敵フェーズへ。</summary>
    public void EndPlayerPhase()
    {
        if (!IsBattleActive) return;
        if (turnManager.CurrentPhase != TurnPhase.PlayerInput) return;
        StartCoroutine(EnemyPhase());
    }

    private IEnumerator ExecutePlayerCommand(BattleUnit actor, BattleCommand command, BattleUnit target)
    {
        // ---- コスト判定 ----
        int cost = command switch
        {
            BattleCommand.Attack   => CostAttack,
            BattleCommand.Skill    => CostSkill,
            BattleCommand.Ultimate => CostUltimate,
            BattleCommand.Item     => CostItem,
            _                      => 0,
        };

        bool usedFree = false;
        if (cost > 0 && FreeActions > 0)
        {
            cost     = 0;
            usedFree = true;
        }

        if (command != BattleCommand.Pass && cost > turnManager.Count)
        {
            Log($"カウントが足りない！（必要 {cost}）");
            turnManager.ChangePhase(TurnPhase.PlayerInput);
            yield break;
        }

        // ---- 実行 ----
        bool success = true;

        switch (command)
        {
            case BattleCommand.Attack:
                ExecuteAttack(actor, target ?? GetRandomEnemy());
                break;

            case BattleCommand.Skill:
                success = ExecuteSkill(actor, target ?? GetRandomEnemy());
                break;

            case BattleCommand.Ultimate:
                success = ExecuteUltimate(actor, target ?? GetRandomEnemy());
                break;

            case BattleCommand.Pass:
                ExecutePass(actor);
                break;

            case BattleCommand.Item:
                ExecuteFirstAid(actor);
                break;
        }

        if (!success)
        {
            turnManager.ChangePhase(TurnPhase.PlayerInput);
            yield break;
        }

        // コスト消費（時間断絶の無料行動を優先）
        if (usedFree)
        {
            FreeActions--;
            Log($"《時間断絶》残り {FreeActions} 回は時が止まっている──");
        }
        else if (cost > 0)
        {
            turnManager.TrySpend(cost);
        }

        OnCommandSelected?.Invoke(command);

        // 必殺技のインタラクティブ演出（次元召喚・衛星ビーム等）を待つ
        yield return null;
        while (UIEffectManager.SequenceRunning) yield return null;

        yield return new WaitForSeconds(0.45f);
        if (!IsBattleActive) yield break;

        turnManager.EnsureActiveAlive();

        if (turnManager.Count <= 0)
        {
            Log("砂時計が尽きた──敵の時間だ。");
            StartCoroutine(EnemyPhase());
        }
        else
        {
            turnManager.ChangePhase(TurnPhase.PlayerInput);
        }
    }

    // ================================================================
    // 敵フェーズ（厄災のカナタ + 戦闘用アンドロイド）
    // ================================================================
    private IEnumerator EnemyPhase()
    {
        turnManager.ChangePhase(TurnPhase.EnemyAction);
        yield return new WaitForSeconds(0.6f);

        foreach (var enemy in EnemyUnits.ToArray())
        {
            if (!IsBattleActive) yield break;
            if (!enemy.IsAlive) continue;

            turnManager.SetActingEnemy(enemy);
            yield return new WaitForSeconds(0.55f);

            bool isBoss = enemy.Data.characterType == CharacterType.Witch;

            // ボスはHP50%以下で発狂 → 2回行動
            int acts = 1;
            if (isBoss && enemy.CurrentHP <= enemy.Data.maxHP / 2)
            {
                if (!_bossEnraged)
                {
                    _bossEnraged = true;
                    Log("カナタ ｢…ははっ｣ ｢あはははははは！｣");
                    Log("厄災のカナタは狂気に堕ちた！（2回行動）");
                    yield return new WaitForSeconds(0.8f);
                }
                acts = 2;
            }

            for (int i = 0; i < acts; i++)
            {
                if (!IsBattleActive) yield break;
                if (isBoss) yield return BossAction(enemy);
                else        yield return MinionAction(enemy);
                yield return new WaitForSeconds(0.55f);
            }
        }

        if (!IsBattleActive) yield break;

        turnManager.NextRound();
    }

    /// <summary>厄災のカナタ：隕石・落雷・業火・暗黒をランダムに使う。</summary>
    private IEnumerator BossAction(BattleUnit boss)
    {
        int roll = Random.Range(0, 4);
        switch (roll)
        {
            case 0: // 隕石：全体攻撃
                Log("カナタ ｢みんないなくならないで！｣ ──厄災【隕石】！");
                foreach (var a in AliveAllies())
                {
                    int d = a.TakeDamage(Mathf.RoundToInt(boss.Data.atk * 0.9f));
                    Log($"  → {a.UnitName} に {d} ダメージ！");
                }
                break;

            case 1: // 落雷：単体大ダメージ
                {
                    var t = GetRandomAlly();
                    if (t == null) break;
                    Log("カナタ ｢どうして！｣ ──厄災【落雷】！");
                    int d = t.TakeDamage(Mathf.RoundToInt(boss.Data.atk * 1.7f));
                    Log($"  → {t.UnitName} に {d} ダメージ！");
                }
                break;

            case 2: // 業火：ランダム2連撃
                Log("カナタ ｢いやああ！｣ ──厄災【業火】！");
                for (int i = 0; i < 2; i++)
                {
                    var t = GetRandomAlly();
                    if (t == null) break;
                    int d = t.TakeDamage(Mathf.RoundToInt(boss.Data.atk * 1.1f));
                    Log($"  → {t.UnitName} に {d} ダメージ！");
                }
                break;

            default: // 暗黒：単体攻撃 + 自己回復
                {
                    var t = GetRandomAlly();
                    if (t == null) break;
                    Log("カナタ ｢私のせい…？｣ ──厄災【暗黒】！");
                    int d = t.TakeDamage(Mathf.RoundToInt(boss.Data.atk * 1.2f));
                    boss.Heal(d);
                    Log($"  → {t.UnitName} に {d} ダメージ！ カナタは闇を吸って回復…");
                }
                break;
        }
        yield return null;
    }

    private IEnumerator MinionAction(BattleUnit minion)
    {
        var target = GetRandomAlly();
        if (target != null)
        {
            Log($"{minion.UnitName} の攻撃！");
            int d = target.TakeDamage(minion.GetBaseAttackDamage());
            Log($"  → {target.UnitName} に {d} ダメージ！");
        }
        yield return null;
    }

    // ================================================================
    // 行動実行（味方）
    // ================================================================
    private void ExecuteAttack(BattleUnit attacker, BattleUnit target)
    {
        if (target == null) return;
        Log($"{attacker.UnitName} の攻撃！");
        int actual = target.TakeDamage(attacker.GetBaseAttackDamage());
        Log($"  → {target.UnitName} に {actual} ダメージ！");
    }

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
            Log($"{actor.UnitName} のスキル！");
            int actual = target.TakeDamage(Mathf.RoundToInt(actor.Data.atk * 1.6f));
            Log($"  → {target.UnitName} に {actual} ダメージ！");
        }
        return true;
    }

    /// <summary>救急：自身のHPを35%回復。</summary>
    private void ExecuteFirstAid(BattleUnit actor)
    {
        int amount = Mathf.RoundToInt(actor.Data.maxHP * 0.35f);
        actor.Heal(amount);
        Log($"{actor.UnitName} は応急手当をした。（HP +{amount}）");
    }

    /// <summary>パス：砂時計カウントを増やし、MPも少し回復。</summary>
    private void ExecutePass(BattleUnit actor)
    {
        turnManager.AddCount(PassGain);
        int restore = Mathf.Max(8, actor.Data.maxMP / 8);
        actor.RestoreMP(restore);
        Log($"{actor.UnitName} は時間を譲渡した。（カウント +{PassGain} / MP +{restore}）");
    }

    // ================================================================
    // 必殺技（全9種）
    // ================================================================
    private bool ExecuteUltimate(BattleUnit actor, BattleUnit primaryTarget)
    {
        if (!actor.TryUseUltimate())
        {
            Log("MP が足りない！");
            return false;
        }

        Log($"★ {actor.UnitName} の必殺技【{actor.Data.ultimateName}】！！");
        OnUltimateActivated?.Invoke(actor, actor.Data.ultimateType);

        switch (actor.Data.ultimateType)
        {
            // ---- 主人公：時間断絶（UIが止まる→砂時計破壊→打ち放題） ----
            case UltimateType.TimeStop:
                FreeActions = 3;
                turnManager.AddCount(40);
                Log("時が断絶した。砂時計 +40、さらに3回の行動が時の外に置かれる──");
                break;

            // ---- 親友：空間斬（防御無視の一撃） ----
            case UltimateType.SpaceRift:
                if (primaryTarget != null)
                {
                    int raw    = Mathf.RoundToInt(actor.Data.atk * 3.2f) + primaryTarget.Data.def;
                    int actual = primaryTarget.TakeDamage(raw);
                    Log($"  空間ごと両断！ → {primaryTarget.UnitName} に {actual} ダメージ！（防御無視）");
                }
                break;

            // ---- ショタ：事象書換（自傷→ダメージを敵に転写） ----
            case UltimateType.EventRewrite:
                if (primaryTarget != null)
                {
                    int sacrifice = Mathf.RoundToInt(actor.Data.maxHP * 0.30f) + actor.Data.def;
                    int selfDmg   = actor.TakeDamage(sacrifice);
                    Log($"  {actor.UnitName} は拳銃で自らを撃った──（{selfDmg} ダメージ）");
                    int transfer  = primaryTarget.TakeDamage(selfDmg * 3 + primaryTarget.Data.def);
                    Log($"  事象が書き換わる。 → {primaryTarget.UnitName} に {transfer} ダメージ！");
                }
                break;

            // ---- 以下はUI演出側（UIEffectManager）がダメージまで担当 ----
            case UltimateType.MemoryReplay:   // ヒロイン：画面反転→隕石
            case UltimateType.DimensionCall:  // 次元の巫女：プレイヤーが直接攻撃
            case UltimateType.AbyssCall:      // ギャル：全UI文字化け→本体（クトゥルフ）
            case UltimateType.SatelliteBeam:  // アンドロイド：巨大ボタンだけの画面
            case UltimateType.GhostRush:      // 幽霊犬：UIが敵に飛ぶ
            case UltimateType.Payoff:         // 王女：ショップUI出現
                break;
        }
        return true;
    }

    // ================================================================
    // UIEffectManager から呼ばれる公開API（演出主導の必殺技用）
    // ================================================================
    /// <summary>指定敵に生ダメージを与える（null なら生存敵からランダム）。</summary>
    public void UltHit(BattleUnit target, int rawDamage)
    {
        if (!IsBattleActive) return;
        if (target == null || !target.IsAlive) target = GetRandomEnemy();
        if (target == null) return;
        int actual = target.TakeDamage(rawDamage);
        Log($"  → {target.UnitName} に {actual} ダメージ！");
    }

    public void HealAllAllies(int amount)
    {
        if (!IsBattleActive) return;
        foreach (var a in AliveAllies()) a.Heal(amount);
        Log($"  味方全員のHPが {amount} 回復！");
    }

    public void RestoreAllMP(int amount)
    {
        if (!IsBattleActive) return;
        foreach (var a in AliveAllies()) a.RestoreMP(amount);
        Log($"  味方全員のMPが {amount} 回復！");
    }

    public void AddCountBonus(int amount)
    {
        if (!IsBattleActive) return;
        turnManager.AddCount(amount);
        Log($"  砂時計カウント +{amount}！");
    }

    public List<BattleUnit> AliveEnemies() => EnemyUnits.FindAll(e => e.IsAlive);
    public List<BattleUnit> AliveAllies()  => AllyUnits.FindAll(a => a.IsAlive);

    // ================================================================
    // 勝敗判定・ループ処理
    // ================================================================
    private void HandleUnitDefeated(BattleUnit unit)
    {
        Log($"{unit.UnitName} は倒れた！");
        if (!IsBattleActive) return;

        bool allEnemiesDead = EnemyUnits.TrueForAll(e => !e.IsAlive);
        bool allAlliesDead  = AllyUnits.TrueForAll(a => !a.IsAlive);

        if (allEnemiesDead)      EndBattle(BattleResult.Victory);
        else if (allAlliesDead)  EndBattle(BattleResult.Defeat);
        else                     turnManager.EnsureActiveAlive();
    }

    private void EndBattle(BattleResult result)
    {
        CurrentResult = result;
        OnBattleEnded?.Invoke(result);

        if (result == BattleResult.Victory)
        {
            Log("厄災は打ち払われた──隕石は、砕けた。");
            Log($"{LoopNumber}周に及ぶループの果て、全ての選択が実を結んだ。");
            Log("──真エンディング──");
            PlayerPrefs.SetInt("LoopNumber", 195); // 周回リセット
            PlayerPrefs.Save();
        }
        else
        {
            StartCoroutine(CoDefeatLoop());
        }
    }

    /// <summary>敗北＝ループ。周回数を進めて世界をやり直す。</summary>
    private IEnumerator CoDefeatLoop()
    {
        int next = LoopNumber + 1;
        Log("視界が黒に塗り潰されていく──");
        yield return new WaitForSeconds(1.2f);
        Log($"だが、記憶Yは消えない。──{next}周目へ。");
        yield return new WaitForSeconds(2.0f);

        PlayerPrefs.SetInt("LoopNumber", next);
        PlayerPrefs.Save();
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    // ================================================================
    // ユーティリティ
    // ================================================================
    private BattleUnit GetRandomEnemy()
    {
        var alive = AliveEnemies();
        return alive.Count > 0 ? alive[Random.Range(0, alive.Count)] : null;
    }

    private BattleUnit GetRandomAlly()
    {
        var alive = AliveAllies();
        return alive.Count > 0 ? alive[Random.Range(0, alive.Count)] : null;
    }

    private static void Log(string msg) => OnBattleLog?.Invoke(msg);
}

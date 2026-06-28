using UnityEngine;

public class BattleUnit : MonoBehaviour
{
    public CharacterData Data { get; private set; }

    public string UnitName => Data != null ? Data.characterName : "Unknown";
    public bool IsEnemy { get; private set; }

    public int CurrentHP { get; private set; }
    public int CurrentMP { get; private set; }
    public bool IsAlive => CurrentHP > 0;

    public static event System.Action<BattleUnit, int> OnDamageReceived;
    public static event System.Action<BattleUnit, int> OnHealed;
    public static event System.Action<BattleUnit> OnMPChanged;
    public static event System.Action<BattleUnit> OnUnitDefeated;
    public static event System.Action<BattleUnit> OnUltimateUsed;

    // Domain Reload 無効時にも static イベントをリセットする
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        OnDamageReceived = null;
        OnHealed         = null;
        OnMPChanged      = null;
        OnUnitDefeated   = null;
        OnUltimateUsed   = null;
    }

    public void Initialize(CharacterData data, bool isEnemy = false)
    {
        Data      = data;
        IsEnemy   = isEnemy;
        CurrentHP = data.maxHP;
        CurrentMP = data.maxMP;
    }

    /// <summary>ダメージを受ける。実際のダメージ量を返す。</summary>
    public int TakeDamage(int rawDamage)
    {
        int actual = Mathf.Max(1, rawDamage - Data.def);
        CurrentHP  = Mathf.Max(0, CurrentHP - actual);

        OnDamageReceived?.Invoke(this, actual);

        if (!IsAlive)
            OnUnitDefeated?.Invoke(this);

        return actual;
    }

    /// <summary>HPを回復する。</summary>
    public void Heal(int amount)
    {
        int actual = Mathf.Min(Data.maxHP - CurrentHP, amount);
        CurrentHP += actual;
        if (actual > 0) OnHealed?.Invoke(this, actual);
    }

    /// <summary>MPを消費する。消費できれば true。</summary>
    public bool ConsumeMP(int amount)
    {
        if (CurrentMP < amount) return false;
        CurrentMP -= amount;
        OnMPChanged?.Invoke(this);
        return true;
    }

    /// <summary>MPを回復する。</summary>
    public void RestoreMP(int amount)
    {
        CurrentMP = Mathf.Min(Data.maxMP, CurrentMP + amount);
        OnMPChanged?.Invoke(this);
    }

    /// <summary>必殺技を発動する。MP消費込み。成功すれば true。</summary>
    public bool TryUseUltimate()
    {
        if (!ConsumeMP(Data.ultimateMPCost)) return false;
        OnUltimateUsed?.Invoke(this);
        return true;
    }

    /// <summary>通常攻撃の生ダメージ値を返す。</summary>
    public int GetBaseAttackDamage()
    {
        return Data.atk + Random.Range(-2, 3);
    }
}

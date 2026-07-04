using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum GameMode { Field, Battle }

public enum BattleId
{
    None,
    Androids,   // 第2章：戦闘用アンドロイド襲撃
    Runa,       // 第5章：深淵のギャル・ルナ
    Ryu,        // 第8章：洗脳された親友リュウ
    Final       // 第11章：厄災のカナタ（Xデー）
}

/// <summary>
/// ストーリー全体の進行状態（章・パーティ・モード）を保持する静的クラス。
/// シーンリロードを跨いで保持され、PlayerPrefs で再起動にも耐える。
/// </summary>
public static class StoryManager
{
    public static GameMode Mode          { get; private set; } = GameMode.Field;
    public static BattleId PendingBattle { get; private set; } = BattleId.None;

    /// <summary>現在の章（0〜12）。</summary>
    public static int Chapter { get; private set; }

    /// <summary>パーティメンバーのID（CharacterDB のキー）。</summary>
    public static List<string> PartyIds { get; private set; } = new List<string>();

    /// <summary>クリア済みミニゲーム（全員のステータスに加護ボーナス）。</summary>
    public static HashSet<string> Blessings { get; private set; } = new HashSet<string>();

    // ================================================================
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void LoadState()
    {
        Mode          = GameMode.Field;
        PendingBattle = BattleId.None;
        Chapter       = PlayerPrefs.GetInt("Story.Chapter", 0);

        string party = PlayerPrefs.GetString("Story.Party", "");
        PartyIds = string.IsNullOrEmpty(party)
            ? new List<string> { "kei", "ai", "ryu" }
            : new List<string>(party.Split(','));

        Blessings = new HashSet<string>();
        string bl = PlayerPrefs.GetString("Story.Blessings", "");
        if (!string.IsNullOrEmpty(bl))
            foreach (var b in bl.Split(',')) Blessings.Add(b);
    }

    private static void Save()
    {
        PlayerPrefs.SetInt("Story.Chapter", Chapter);
        PlayerPrefs.SetString("Story.Party", string.Join(",", PartyIds));
        PlayerPrefs.SetString("Story.Blessings", string.Join(",", Blessings));
        PlayerPrefs.Save();
    }

    // ================================================================
    // 進行操作
    // ================================================================
    public static void AdvanceChapter()
    {
        Chapter++;
        Save();
    }

    public static void AddPartyMember(string id)
    {
        if (!PartyIds.Contains(id)) PartyIds.Add(id);
        Save();
    }

    public static void RemovePartyMember(string id)
    {
        PartyIds.Remove(id);
        Save();
    }

    public static void AddBlessing(string name)
    {
        Blessings.Add(name);
        Save();
    }

    /// <summary>最初からやり直す（デバッグ・真エンド後用）。</summary>
    public static void ResetAll()
    {
        PlayerPrefs.DeleteKey("Story.Chapter");
        PlayerPrefs.DeleteKey("Story.Party");
        PlayerPrefs.DeleteKey("Story.Blessings");
        PlayerPrefs.SetInt("LoopNumber", 195);
        PlayerPrefs.Save();
        LoadState();
    }

    // ================================================================
    // モード遷移
    // ================================================================
    public static void StartBattle(BattleId battle)
    {
        Mode          = GameMode.Battle;
        PendingBattle = battle;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public static void ReturnToField()
    {
        Mode          = GameMode.Field;
        PendingBattle = BattleId.None;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    /// <summary>バトル勝利時に呼ばれる。章を進めてフィールドへ戻る。</summary>
    public static void OnBattleWon()
    {
        AdvanceChapter();
        ReturnToField();
    }
}

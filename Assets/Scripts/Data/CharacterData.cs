using UnityEngine;

public enum CharacterType
{
    Protagonist,  // 主人公
    Heroine,      // ヒロイン
    BestFriend,   // 親友
    Shota,        // ショタ
    Witch,        // 魔女
    Gal,          // ギャル
    Android,      // アンドロイド
    Ghost,        // 幽霊
    Princess      // プリンセス
}

public enum UltimateType
{
    TimeStop,       // 時間停止
    MemoryReplay,   // 記憶再生
    SpaceRift,      // 空間断裂
    EventRewrite,   // 事象書き換え
    DimensionCall,  // 次元召喚
    AbyssCall,      // 深淵召喚
    SatelliteBeam,  // 衛星砲
    GhostRush,      // 霊体突撃
    Payoff          // 清算
}

[CreateAssetMenu(fileName = "NewCharacter", menuName = "RPG/Character Data")]
public class CharacterData : ScriptableObject
{
    [Header("基本情報")]
    public string characterName;
    public CharacterType characterType;
    public Sprite portrait;

    [Header("ステータス")]
    public int maxHP;
    public int maxMP;
    public int atk;
    public int def;
    public int spd;

    [Header("必殺技")]
    public string ultimateName;
    public UltimateType ultimateType;
    [TextArea(2, 4)]
    public string ultimateDescription;
    public int ultimateMPCost;
}

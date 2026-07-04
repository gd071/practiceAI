using System.Collections.Generic;
using UnityEngine;

/// <summary>キャラクター1人分の定義（バトル・フィールド共通）。</summary>
public class CharacterDef
{
    public string        id;
    public string        name;
    public CharacterType ctype;
    public UltimateType  utype;
    public string        ultName;
    public int hp, mp, atk, def, spd, mpCost;
    public SpriteShape   shape;
    public Color         color;      // バトルシルエット色
    public Color         hairColor;  // ドット絵の髪色
    public Color         bodyColor;  // ドット絵の服色
}

/// <summary>
/// 全キャラクターの定義テーブル。
/// バトル（シルエット）とフィールド（ドット絵）の双方から参照される。
/// </summary>
public static class CharacterDB
{
    private static Dictionary<string, CharacterDef> _db;

    public static CharacterDef Get(string id)
    {
        EnsureBuilt();
        return _db.TryGetValue(id, out var def) ? def : null;
    }

    public static void EnsureBuilt()
    {
        if (_db != null) return;
        _db = new Dictionary<string, CharacterDef>();

        Add(new CharacterDef { id="kei",   name="主人公 ケイ",       ctype=CharacterType.Protagonist, utype=UltimateType.TimeStop,      ultName="時間断絶",
            hp=130, mp=70,  atk=36, def=20, spd=25, mpCost=30, shape=SpriteShape.Human,
            color=new Color(0.35f,0.55f,1f),   hairColor=new Color(0.12f,0.12f,0.15f), bodyColor=new Color(0.25f,0.35f,0.7f) });

        Add(new CharacterDef { id="ai",    name="ヒロイン アイ",     ctype=CharacterType.Heroine,     utype=UltimateType.MemoryReplay,  ultName="記憶回廊",
            hp=100, mp=90,  atk=32, def=15, spd=30, mpCost=35, shape=SpriteShape.Human,
            color=new Color(1f,0.6f,0.8f),     hairColor=new Color(0.1f,0.1f,0.12f),   bodyColor=new Color(0.85f,0.4f,0.6f) });

        Add(new CharacterDef { id="ryu",   name="親友 リュウ",       ctype=CharacterType.BestFriend,  utype=UltimateType.SpaceRift,     ultName="空間斬",
            hp=115, mp=55,  atk=42, def=24, spd=20, mpCost=25, shape=SpriteShape.Human,
            color=new Color(0.7f,0.75f,0.85f), hairColor=new Color(0.75f,0.78f,0.85f), bodyColor=new Color(0.3f,0.32f,0.4f) });

        Add(new CharacterDef { id="niyo",  name="ショタ ニーヨ",     ctype=CharacterType.Shota,       utype=UltimateType.EventRewrite,  ultName="事象書換",
            hp=85,  mp=65,  atk=30, def=12, spd=33, mpCost=30, shape=SpriteShape.Human,
            color=new Color(1f,0.85f,0.4f),    hairColor=new Color(0.9f,0.75f,0.3f),   bodyColor=new Color(0.8f,0.55f,0.2f) });

        Add(new CharacterDef { id="rei",   name="次元の巫女 レイ",   ctype=CharacterType.Witch,       utype=UltimateType.DimensionCall, ultName="次元干渉",
            hp=90,  mp=100, atk=34, def=14, spd=22, mpCost=40, shape=SpriteShape.Human,
            color=new Color(0.75f,0.5f,1f),    hairColor=new Color(0.45f,0.3f,0.6f),   bodyColor=new Color(0.9f,0.9f,0.95f) });

        Add(new CharacterDef { id="runa",  name="ギャル ルナ",       ctype=CharacterType.Gal,         utype=UltimateType.AbyssCall,     ultName="本体召喚",
            hp=95,  mp=85,  atk=33, def=16, spd=28, mpCost=40, shape=SpriteShape.Human,
            color=new Color(1f,0.8f,0.3f),     hairColor=new Color(1f,0.85f,0.35f),    bodyColor=new Color(0.95f,0.5f,0.7f) });

        Add(new CharacterDef { id="misa",  name="アンドロイド ミサ", ctype=CharacterType.Android,     utype=UltimateType.SatelliteBeam, ultName="衛星通信",
            hp=110, mp=60,  atk=38, def=22, spd=18, mpCost=35, shape=SpriteShape.Android,
            color=new Color(0.4f,0.95f,0.9f),  hairColor=new Color(0.75f,0.85f,0.95f), bodyColor=new Color(0.3f,0.7f,0.7f) });

        Add(new CharacterDef { id="pochi", name="幽霊犬 ポチ",       ctype=CharacterType.Ghost,       utype=UltimateType.GhostRush,     ultName="突撃",
            hp=75,  mp=50,  atk=28, def=10, spd=38, mpCost=20, shape=SpriteShape.Dog,
            color=new Color(0.85f,0.9f,1f),    hairColor=new Color(0.9f,0.92f,1f),     bodyColor=new Color(0.8f,0.85f,0.95f) });

        Add(new CharacterDef { id="erika", name="王女 エリカ",       ctype=CharacterType.Princess,    utype=UltimateType.Payoff,        ultName="ペイオフ",
            hp=100, mp=75,  atk=31, def=18, spd=24, mpCost=35, shape=SpriteShape.Human,
            color=new Color(1f,0.72f,0.2f),    hairColor=new Color(1f,0.9f,0.5f),      bodyColor=new Color(0.7f,0.15f,0.25f) });
    }

    private static void Add(CharacterDef def) => _db[def.id] = def;

    /// <summary>CharacterData（ScriptableObject）を生成する。ミニゲームの加護分を上乗せ。</summary>
    public static CharacterData CreateData(string id, int blessings)
    {
        var def = Get(id);
        if (def == null) return null;

        var data = ScriptableObject.CreateInstance<CharacterData>();
        data.characterName       = def.name;
        data.characterType       = def.ctype;
        data.maxHP               = def.hp + blessings * 12;
        data.maxMP               = def.mp + blessings * 6;
        data.atk                 = def.atk + blessings * 2;
        data.def                 = def.def + blessings;
        data.spd                 = def.spd;
        data.ultimateName        = def.ultName;
        data.ultimateType        = def.utype;
        data.ultimateMPCost      = def.mpCost;
        data.ultimateDescription = def.name + "の必殺技";
        return data;
    }
}

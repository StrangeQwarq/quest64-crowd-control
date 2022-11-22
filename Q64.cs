using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ConnectorLib.JSON;
using CrowdControl.Common;
using JetBrains.Annotations;

namespace CrowdControl.Games.Packs;

public class Quest64 : N64EffectPack
{
    public Quest64([NotNull] IPlayer player, [NotNull] Func<CrowdControlBlock, bool> responseHandler,
        [NotNull] Action<object> statusUpdateHandler) : base(player, responseHandler, statusUpdateHandler)
    {
    }

    private const string US_ROM_MD5 = "ea552e33973468233a0712c251abdb6b";

    private const ulong ADDR_CURRENT_HP = 0x8007ba84;
    private const ulong ADDR_MAX_HP = 0x8007ba86;
    private const ulong ADDR_CURRENT_MP = 0x8007ba88;
    private const ulong ADDR_BRIAN_ANIMATION_ID = 0x8007bb1f;
    private const ulong ADDR_BRIAN_SCALE = 0x8007baf0;
    private const ulong ADDR_CAMERA_FOV = 0x80086ec8;
    private const ulong ADDR_ITEM_QUEUE = 0x8007ba73;
    private const ulong ADDR_ENEMY_COUNT = 0x8007c993;
    private const ulong ADDR_BATTLE_STATE = 0x8008c592;
    private const ulong ADDR_ENCOUNTER_RATE = 0x8008c578;
    private const ulong ADDR_ENEMY_CURRENT_HEALTH = 0x8007c9a2;
    private const ulong ADDR_ENEMY_MAX_HEALTH = 0x8007c9a4;
    private const ulong ADDR_TRANSITION_TIMER = 0x8007b2ec;
    private const ulong ADDR_BRIAN_STATUS = 0x8007bb38;
    private const ulong ADDR_NEXT_BGM = 0x8008fcc1;
    private const ulong ADDR_BGM_SWAP_TIMER = 0x8008fcc3;
    private const ulong ADDR_NEXT_SFX = 0x80053970;
    private const ulong ADDR_HUD_TIMER = 0x8004d2bc;
    private const ulong ADDR_MOVE_SIZE_MULTIPLIER = 0x8007bbc8;
    private const ulong ADDR_ELEMENT_TOTAL = 0x8007bbbc;
    private const ulong ADDR_COMPASS_TEXTURE = 0x803a8ea4;
    private const ulong ADDR_SPELL_ID = 0x8007bbd6;
    private const ulong ADDR_SPELL_TIMER = 0x8007bbd8;
    private const ulong ADDR_INVENTORY_START = 0x8008cf78;
    private const ulong ADDR_AGILITY = 0x8007ba8c;
    private const ulong ADDR_DEFENSE = 0x8007ba8e;
    
    private const ushort BRIAN_ORIGINAL_SCALE = 0x3d8f;
    private const ushort BRIAN_BIG_SCALE = 0x3e4f;
    private const ushort BRIAN_SMALL_SCALE = 0x3c8f;

    private const uint FOV_ORIGINAL = 0x42180000;
    private const uint FOV_WIDE = 0x42d00000;
    private const uint FOV_NARROW = 0x41500000;
    private const uint FOV_INVERT = 0x44270000;

    private const uint COMPASS_HIDE = 0x803900b0;
    private const uint COMPASS_SHOW = 0x80399cb0;

    private const uint MOVEMENT_MULTIPLIER_BIG = 0x40000000;
    private const uint MOVEMENT_MULTIPLIER_NORMAL = 0x3f800000;
    private const uint MOVEMENT_MULTIPLIER_SMALL = 0x3f000000;
    
    
    private const uint ENEMY_DATA_SIZE = 0x128;
    private const uint MAX_INVENTORY_SIZE = 100;

    private const ushort LOCKED_ELEMENT_LEVEL_REQ = 0x63;
   
    private ushort lastCurrentHP = 0;
    private ushort lastCurrentMP = 9999;
    private ushort previousSpellTimer = 0;

    // Addresses of the colors of each polygon of Brian's cloak.
    // Points to the red value, next is green, then blue. 1 byte each.
    // Listed as they appear on Brian from left to right
    private readonly ulong[] ADDR_CLOAK_POLYS = 
    {
        0x8020bd0c, // 1
        0x8020bc5c, // 2
        0x8020bbac, // 3
        0x8020bdbc, // 4
        0x8020be6c, // 5
        0x8020bf1c, // 6
    };
    
    // Addresses of the sound effect queue. Each slot is 4 bytes and has
    // the index of a sound effect to play on the next frame. Only one
    // slot is active, shifting with each sfx played.
    private readonly ulong[] ADDR_SFX_QUEUE = 
    {
        0x8005390f,
        0x80053913,
        0x80053917,
        0x8005391b,
        0x8005391f,
        0x80053920,
        0x80053927,
        0x8005392b,
    };

    private enum Element
    {
        Fire,
        Earth,
        Water,
        Wind,
        Any
    };
    
    private readonly Dictionary<string, (float duration, float retryDelay, float repeatDelay)> effectDurations = new()
    {
        {"hpplus", (60, 1, 1)},
        {"hpminus", (60, 1, 1)},
        {"mpplus", (60, 1, 1)},
        {"mpminus", (60, 1, 1)},
        {"agiplus", (60, 1, 1)},
        {"agiminus", (60, 1, 1)},
        {"defplus", (60, 1, 1)},
        {"defminus", (60, 1, 1)},
        {"givespirit", (60, 1, 1)},
        {"takespirit", (60, 1, 1)},
        {"lockelement", (60, 1, 1)},
        {"giveitem", (60, 1, 1)},
        {"bigbrian", (60, 1, 0.1f)},
        {"smallbrian", (60, 1, 1)},
        {"wideview", (60, 1, 1)},
        {"narrowview", (60, 1, 1)},
        {"flipcamera", (60, 1, 1)},
        {"expensivespells", (60, 1, 0.05f)},
        {"cheapspells", (60, 1, 0.25f)},
        {"maxencounter", (60, 1, 1)},
        {"minencounter", (60, 1, 1)},
        {"healenemy", (60, 1, 1)},
        {"statuseffect", (60, 1, 1)},
        {"changemusic", (60, 1, 1)},
        {"hidehud", (60, 1, 1)},
        {"cloakcolor", (60, 1, 1)},
        {"castreturn", (60, 1, 1)},
        {"lockcompass", (60, 1, 1)},
        {"randomizespell", (60, 1, 1)},
        {"movedown", (60, 1, 1)},
        {"moveup", (60, 1, 1)},
        {"hidecompass", (60, 1, 1)},
        {"randomspell", (30, 1, 0.25f)}
    };
    
    // Sound effects to play alongside various effects. Indexes are written to
    // all slots of the sfx queue to ensure it gets played.
    private Dictionary<string, byte> sfx = 
        new()
    {
        {"healing", 0x0b},
        {"enemydefeated", 0x0a},
        {"damagecontact", 0x18},
        {"dizzy", 0x1c},
        {"silence", 0x27},
        {"magicbarrier", 0x35},
        {"statup", 0x3d},
        {"statdown", 0x3e},
        {"menuopen", 0x01},
        {"menuclose", 0x02},
        {"restriction", 0x22},
        {"iceknife", 0x38},
    };
        
    private Dictionary<string, (string name, ulong countAddress, ulong levelReqAddress)> spiritTypes =
        new()
    {
        {"fire", ("Fire", 0x8007baa4, 0x800c06a0)},
        {"earth", ("Earth", 0x8007baa5, 0x800c0a9c)},
        {"water", ("Water", 0x8007baa6, 0x800c0e98)},
        {"wind", ("Wind", 0x8007baa7, 0x800c1294)},
    };

    private readonly Dictionary<string, (string name, ushort statusBit, ulong durationAddress, ulong iconAddress, byte iconValue, string sfx)> statusTypes =
        new()
    {
        {"vampire", ("Vampire Touch", 0x2, 0x8007bb3b, 0, 0, "statup")},
        {"powerstaf", ("Power Staff", 0x4, 0x8007bb3c, 0x8007bb4a, 3, "statup")},
        {"freeze", ("Freeze", 0x8, 0x8007bb3d, 0, 0, "iceknife")},
        {"evade", ("Evasion", 0x20, 0x8007bb3c, 0x8007bb4d, 7, "statup")},
        {"silence", ("Silence", 0x40, 0x8007bb40, 0x8007bb50, 0xb, "silence")},
        {"defup", ("Def Up", 0x400, 0x8007bb44, 0x8007bb4c, 5, "statup")},
        {"barrier", ("Magic Barrier", 0x100, 0x8007bb42, 0, 0, "magicbarrier")},
    };

    private readonly Dictionary<string, (string name, ulong[] colors)> cloakColors = 
        new()
    {
        {"red", ("Red",            new ulong[] {0xe90000, 0xe90000, 0xe90000, 0xe90000, 0xe90000, 0xe90000})},
        {"blue", ("Blue",          new ulong[] {0x0000FF, 0x0000FF, 0x0000FF, 0x0000FF, 0x0000FF, 0x0000FF})},
        {"green", ("Green",        new ulong[] {0x00FF00, 0x00FF00, 0x00FF00, 0x00FF00, 0x00FF00, 0x00FF00})},
        {"rainbow", ("Rainbow",    new ulong[] {0xe40303, 0xff8c00, 0xffed00, 0x008026, 0x25508e, 0x732982})},
        {"trans", ("Trans Flag",   new ulong[] {0x82baff, 0xff82d1, 0xffffff, 0xffffff, 0xff82d1, 0x82baff})},
        {"ace", ("Ace Flag",       new ulong[] {0x000000, 0xa3a3a3, 0xa3a3a3, 0xffffff, 0xffffff, 0x800080})},
        {"nb", ("Non-Binary Flag", new ulong[] {0xfcf434, 0xffffff, 0xffffff, 0x9c59d1, 0x9c59d1, 0x000000})},
        {"bi", ("Bisexual Flag",   new ulong[] {0xd60270, 0xd60270, 0x9b4f96, 0x9b4f96, 0x0038a8, 0x0038a8})},
        {"aro", ("Aromantic Flag", new ulong[] {0x3da542, 0xa7d379, 0xffffff, 0xffffff, 0xa9a9a9, 0x000000})},
        {"pan", ("Pansexual Flag", new ulong[] {0xff218c, 0xff218c, 0xffd800, 0xffd800, 0x21b1ff, 0x21b1ff})},
    };

    protected Dictionary<string, Func<bool>> bidWarActions;
        
    private readonly Dictionary<string, (string name, byte value)> items =
        new()
    {
        {"spiritlight", ("Spirit Light", 0x00)},
        {"freshbread", ("Fresh Bread", 0x01)},
        {"honeybread", ("Honey Bread", 0x02)},
        {"healingpotion", ("Healing Potion", 0x03)},
        {"dragonspotion", ("Dragon's Potion", 0x04)},
        {"dewdrop", ("Dew Drop", 0x05)},
        {"mintleaves", ("Mint Leaves", 0x06)},
        {"heroesdrink", ("Heroes Drink", 0x07)},
        {"silentflute", ("Silent Flute", 0x08)},
        {"celinesbell", ("Celine's Bell", 0x09)},
        {"replica", ("Replica", 0x0a)},
        {"giantsshoes", ("Giant's Shoes", 0x0b)},
        {"silveramulet", ("Silver Amulet", 0x0c)},
        // {"White Wings", 0x0D}
        // {"Yellow Wings", 0x0E}
        // {"Blue Wings", 0x0F}
        // {"Green Wings", 0x10}
        // {"Red Wings", 0x11}
        // {"Black Wings", 0x12}
    };

    private readonly Dictionary<string, (string name, byte index)> bgmTracks = 
        new()
    {
        {"boss", ("Boss Battle", 0x00)},
        {"mines", ("Mines", 0x01)},
        {"melrode", ("Melrode", 0x02)},
        {"pirates", ("Pirates", 0x06)},
        {"sacredvalley", ("Sacred valley", 0x09)},
        {"cullhazard", ("Cull hazard", 0x0a)},
        {"connor", ("Connor forest", 0x0c)},
        {"boilhole", ("Boil hole", 0x12)},
        {"dries", ("Dries", 0x16)},
        {"bluecave", ("Blue cave", 0x17)},
        {"normoon", ("Normoon", 0x25)},
        {"darkgaol", ("Dark gaol", 0x21)},
        {"brannoch", ("Brannoch castle", 0x1f)},
        {"battle", ("Battle", 0x0d)},
        {"larapool", ("Larapool", 0x22)},
        {"dondoran", ("Dondoran", 0x19)},
    };

    private readonly (Element element, ushort spellId, byte levelReq, string name)[] spells =
    {
        new(Element.Fire,  0x0000, 0x0001, "Fire Ball Lv1"),
        new(Element.Fire,  0x0001, 0x0004, "Fire Ball Lv2"),
        new(Element.Fire,  0x0002, 0x0007, "Power Staff Lv1"),
        new(Element.Fire,  0x0003, 0x000A, "Homing Arrow Lv1"),
        new(Element.Fire,  0x0004, 0x000D, "Hot Steam Lv1"),
        new(Element.Fire,  0x0005, 0x0010, "Fire Ball Lv3"),
        new(Element.Fire,  0x0006, 0x0013, "Compression"),
        new(Element.Fire,  0x0007, 0x0016, "Power Staff Lv2"),
        new(Element.Fire,  0x0008, 0x0018, "Fire Pillar"),
        new(Element.Fire,  0x0009, 0x001C, "Homing Arrow Lv2"),
        new(Element.Fire,  0x000a, 0x001E, "Fire Bomb"),
        new(Element.Fire,  0x000b, 0x0020, "Vampire's Touch"),
        new(Element.Fire,  0x000c, 0x0024, "Magma Ball"),
        new(Element.Fire,  0x000d, 0x0028, "Extinction"),
        new(Element.Fire,  0x000e, 0x002C, "Hot Steam Lv2"),
        new(Element.Earth, 0x0100, 0x0001, "Rock Lv1"),
        new(Element.Earth, 0x0101, 0x0004, "Rock Lv2"),
        new(Element.Earth, 0x0102, 0x0007, "Spirit Armor Lv1"),
        new(Element.Earth, 0x0103, 0x000A, "Rolling Rock Lv1"),
        new(Element.Earth, 0x0104, 0x000D, "Weakness Lv1"),
        new(Element.Earth, 0x0105, 0x0010, "Rock Lv3"),
        new(Element.Earth, 0x0106, 0x0013, "Magnet Rock"),
        new(Element.Earth, 0x0107, 0x0015, "Spirit Armor Lv2"),
        new(Element.Earth, 0x0108, 0x0018, "Avalanche"),
        new(Element.Earth, 0x0109, 0x001B, "Confusion"),
        new(Element.Earth, 0x010a, 0x001F, "Weakness Lv2"),
        new(Element.Earth, 0x010b, 0x0022, "Rock Shower"),
        new(Element.Earth, 0x010c, 0x0024, "Magic Barrier"),
        new(Element.Earth, 0x010d, 0x0027, "Rolling Rock Lv2"),
        new(Element.Earth, 0x010e, 0x002B, "Weaken All"),
        new(Element.Water, 0x0200, 0x0001, "Water Pillar Lv1"),
        new(Element.Water, 0x0201, 0x0004, "Water Pillar Lv2"),
        new(Element.Water, 0x0202, 0x0007, "Healing Lv1"),
        new(Element.Water, 0x0203, 0x000A, "Soul Searcher Lv1"),
        new(Element.Water, 0x0204, 0x000D, "Water Pillar Lv3"),
        new(Element.Water, 0x0205, 0x000F, "Ice Wall"),
        new(Element.Water, 0x0206, 0x0011, "Ice Knife"),
        new(Element.Water, 0x0207, 0x0013, "Exit"),
        new(Element.Water, 0x0208, 0x0017, "Escape"),
        new(Element.Water, 0x0209, 0x0018, "Return"),
        new(Element.Water, 0x020a, 0x0019, "Healing Lv2"),
        new(Element.Water, 0x020b, 0x0021, "Soul Searcher Lv2"),
        new(Element.Water, 0x020c, 0x0023, "Walking Water"),
        new(Element.Water, 0x020d, 0x0028, "Drain Magic"),
        new(Element.Water, 0x020e, 0x002E, "Invalidity"),
        new(Element.Wind,  0x0300, 0x0001, "Wind Cutter Lv1"),
        new(Element.Wind,  0x0301, 0x0004, "Wind Cutter Lv2"),
        new(Element.Wind,  0x0302, 0x0006, "Restriction Lv1"),
        new(Element.Wind,  0x0303, 0x0008, "Evade Lv1"),
        new(Element.Wind,  0x0304, 0x000A, "Silence Lv1"),
        new(Element.Wind,  0x0305, 0x000C, "Wind Cutter Lv3"),
        new(Element.Wind,  0x0306, 0x000D, "Large Cutter"),
        new(Element.Wind,  0x0307, 0x0010, "Restriction Lv2"),
        new(Element.Wind,  0x0308, 0x0014, "Wind Bomb"),
        new(Element.Wind,  0x0309, 0x0018, "Evade Lv2"),
        new(Element.Wind,  0x030a, 0x001C, "Cyclone"),
        new(Element.Wind,  0x030b, 0x0020, "Slow Enemy"),
        new(Element.Wind,  0x030c, 0x0025, "Wind Walk"),
        new(Element.Wind,  0x030d, 0x002A, "Silence Lv2"),
        new(Element.Wind,  0x030e, 0x002F, "Ultimate Wind"),
    };
    
    public override List<Effect> Effects
    {
        get
        {
            List<Effect> effects = new List<Effect>
            {
                new("Give HP", "hpplus", new[] {"hpquantity"}),
                new("Take HP", "hpminus", new[] {"hpquantity"}),
                new("Give MP", "mpplus", new[] {"mpquantity"}),
                new("Take MP", "mpminus", new[] {"mpquantity"}),
                new("Increase Agility", "agiplus", new[] {"agiquantity"}),
                new("Decrease Agility", "agiminus", new[] {"agiquantity"}),
                new("Increase Defense", "defplus", new[] {"defquantity"}),
                new("Decrease Defense", "defminus", new[] {"defquantity"}),
                new("Give Spirit", "givespirit", ItemKind.Folder),
                new("Take Spirit", "takespirit", ItemKind.Folder),
                new("Lock Element", "lockelement", ItemKind.Folder),
                new("Give Item", "giveitem", ItemKind.Folder),
                new("Big Brian", "bigbrian"),
                new("Tiny Brian", "smallbrian"),
                new("Wide View", "wideview"),
                new("Narrow View", "narrowview"),
                new("Flip Camera", "flipcamera"),
                new("Cheap Spells", "cheapspells"),
                new("Expensive Spells", "expensivespells"),
                new("Max Encounter Rate", "maxencounter"),
                new("Min Encounter Rate", "minencounter"),
                new("Heal Enemy", "healenemy"),
                new("Apply Status Effect", "statuseffect", ItemKind.Folder),
                new("Change Music", "changemusic", ItemKind.Folder),
                new("Hide HUD", "hidehud"),
                new("Change Cloak Color", "cloakcolor", ItemKind.Folder),
                new("Battle Movement Up", "moveup"),
                new("Battle Movement Down", "movedown"),
                new("Hide Compass", "hidecompass"),
                new("Randomize Spells", "randomspell", ItemKind.Folder),
                
                // Randomize spell options
                new("Random Fire Spells", "randomspell_fire", "randomspell"),
                new("Random Earth Spells", "randomspell_earth", "randomspell"),
                new("Random Water Spells", "randomspell_water", "randomspell"),
                new("Random Wind Spells", "randomspell_wind", "randomspell"),
                new("Random Spells, Any Element", "randomspell_any", "randomspell"),
            };

            effects.AddRange(spiritTypes.Select(t => new Effect($"Add 1 {t.Value.name} spirit", $"givespirit_{t.Key}", "givespirit")));
            effects.AddRange(spiritTypes.Select(t => new Effect($"Remove 1 {t.Value.name} spirit", $"takespirit_{t.Key}", "takespirit")));
            effects.AddRange(spiritTypes.Select(t => new Effect($"Lock {t.Value.name} spells", $"lockelement_{t.Key}", "lockelement")));
            effects.AddRange(items.Select(t => new Effect($"Give Item: {t.Value.name}", $"giveitem_{t.Key}", "giveitem")));
            effects.AddRange(statusTypes.Select(t => new Effect($"Apply the {t.Value.name} effect", $"statuseffect_{t.Key}", "statuseffect")));
            effects.AddRange(bgmTracks.Select(t => new Effect($"Play the {t.Value.name} music", $"changemusic_{t.Key}", "changemusic")));
            effects.AddRange(cloakColors.Select(t => new Effect($"Change Brian's cloak to {t.Value.name}", $"cloakcolor_{t.Key}", "cloakcolor")));
            
            return effects;
        }
    }
    
    public override List<Common.ItemType> ItemTypes => new(new[]
    {
        new Common.ItemType("HP Amount", "hpquantity", Common.ItemType.Subtype.Slider, "{\"min\":1,\"max\":10}"),
        new Common.ItemType("MP Amount", "mpquantity", Common.ItemType.Subtype.Slider, "{\"min\":1,\"max\":10}"),
        new Common.ItemType("Agility Amount", "agiquantity", Common.ItemType.Subtype.Slider, "{\"min\":1,\"max\":5}"),
        new Common.ItemType("Defense Amount", "defquantity", Common.ItemType.Subtype.Slider, "{\"min\":1,\"max\":5}")
    });
    
    public override Game Game { get; } = new(11, "Quest 64", "Quest64", "N64", ConnectorType.N64Connector);

    public override List<ROMInfo> ROMTable => new List<ROMInfo>(new[]
    {
        new ROMInfo("Quest 64", null, Patching.Ignore, ROMStatus.ValidPatched,s => Patching.MD5(s, US_ROM_MD5)),
    });

    protected override bool IsReady(EffectRequest request) => true; //Connector.Read8(0x00b1, out byte b) && (b < 0x80);

    protected override void RequestData(DataRequest request) => Respond(request, request.Key, null, false, $"Variable name \"{request.Key}\" not known");

    protected override void StartEffect(EffectRequest request)
    {
        if (!IsReady(request))
        {
            DelayEffect(request, TimeSpan.FromSeconds(5));
            return;
        }

        string[] codeParams = request.FinalCode.Split('_');

        // Connector.SendMessage(request.FinalCode);

        string code = codeParams[0];
        switch (code)
        {
            case "hpplus":
            {
                ushort.TryParse(codeParams[1], out ushort changeHP);
                AdjustHPorMP(request, ADDR_CURRENT_HP, changeHP, "healing", "HP", effectDurations[code].retryDelay);
                return;
            }
            
            case "hpminus":
            {
                ushort.TryParse(codeParams[1], out ushort changeHP);
                AdjustHPorMP(request, ADDR_CURRENT_HP, -changeHP, "damagecontact", "HP", effectDurations[code].retryDelay);
                return;
            }
            
            case "mpplus":
            {
                ushort.TryParse(codeParams[1], out ushort changeMP);
                AdjustHPorMP(request, ADDR_CURRENT_MP, changeMP, "healing", "MP", effectDurations[code].retryDelay);
                return;
            }

            case "mpminus":
            {
                ushort.TryParse(codeParams[1], out ushort changeMP);
                AdjustHPorMP(request, ADDR_CURRENT_MP, -changeMP, "healing", "MP", effectDurations[code].retryDelay);
                return;
            }

            case "agiplus":
            {
                ushort.TryParse(codeParams[1], out ushort changeAgi);
                AdjustStat(request, ADDR_AGILITY, changeAgi, "statup", "agility", effectDurations[code].retryDelay);
                return;
            }
            
            case "agiminus":
            {
                ushort.TryParse(codeParams[1], out ushort changeAgi);
                AdjustStat(request, ADDR_AGILITY, -changeAgi, "statdown", "agility", effectDurations[code].retryDelay);
                return;
            }

            case "defplus":
            {
                ushort.TryParse(codeParams[1], out ushort changeDef);
                AdjustStat(request, ADDR_DEFENSE, changeDef, "statup", "defense", effectDurations[code].retryDelay);
                return;
            }
            
            case "defminus":
            {
                ushort.TryParse(codeParams[1], out ushort changeDef);
                AdjustStat(request, ADDR_DEFENSE, -changeDef, "statdown", "defense", effectDurations[code].retryDelay);
                return;
            }

            case "givespirit":
            {
                var giveSpirit = spiritTypes[codeParams[1]];
                TryEffect(request,
                    () => Connector.RangeAdd8(giveSpirit.countAddress, 1, 1, 50, false)
                          && Connector.RangeAdd16(ADDR_ELEMENT_TOTAL, 1, 1, 0xffff, false),
                    () => PlaySFX(sfx["menuopen"]),
                    () => { Connector.SendMessage($"{request.DisplayViewer} gave you a {giveSpirit.name} spirit"); });
                return;
            }

            case "takespirit":
            {
                var takeSpirit = spiritTypes[codeParams[1]];
                TryEffect(request,
                    () => Connector.RangeAdd8(takeSpirit.countAddress, -1, 1, 50, false)
                          && Connector.RangeAdd16(ADDR_ELEMENT_TOTAL, -1, 1, 0xffff, false),
                    () => PlaySFX(sfx["menuclose"]),
                    () => Connector.SendMessage($"{request.DisplayViewer} took a(n) {takeSpirit.name} spirit"));
                return;
            }

            case "lockelement":
            {
                var lockedElement = spiritTypes[codeParams[1]];
                StartTimed(request,
                    () => (Connector.Read16(lockedElement.levelReqAddress, out ushort levelReq) && (levelReq == 0x01)),
                    () =>
                    {
                        bool result = Connector.Write16(lockedElement.levelReqAddress, LOCKED_ELEMENT_LEVEL_REQ);
                        if (result)
                        {
                            PlaySFX(sfx["silence"]);
                            Connector.SendMessage($"{request.DisplayViewer} locked all {lockedElement.name} spells for {effectDurations[code].duration} seconds");
                        }

                        return result;
                    },
                    TimeSpan.FromSeconds(effectDurations[code].duration),
                    "lockelement"
                );
                return;
            }

            case "bigbrian":
            {
                // breaks when opening a door because brian is displaced according to his scale
                // he ends up displaced at the new location, which may put him out of bounds
                // if a transition timer is detected, set the original scale, and wait until it hits 0
                // wait 0.5? seconds after the timer ends before making him big again
                RepeatAction(request, TimeSpan.FromSeconds(60),
                    () => Connector.Read16(ADDR_BRIAN_SCALE, out ushort scale) && (scale != BRIAN_BIG_SCALE),
                    () => Connector.SendMessage($"{request.DisplayViewer} made Brian big for {effectDurations[code].duration} seconds") && PlaySFX("statup"), 
                    TimeSpan.FromSeconds(1),
                    () => true,
                    TimeSpan.FromSeconds(1),
                    () => ManageBrianSizeChange(BRIAN_BIG_SCALE),
                    TimeSpan.FromSeconds(effectDurations[code].repeatDelay), true, "briansize"
                ).WhenCompleted.Then(t =>
                {
                    Connector.Write16(ADDR_BRIAN_SCALE, BRIAN_ORIGINAL_SCALE);
                    Connector.SendMessage("Brian returned to his normal size.");
                });
                return;
            }

            case "smallbrian":
            {
                RepeatAction(request, TimeSpan.FromSeconds(60),
                    () => Connector.Read16(ADDR_BRIAN_SCALE, out ushort scale) && (scale != BRIAN_SMALL_SCALE),
                    () => Connector.SendMessage($"{request.DisplayViewer} made Brian small for {effectDurations[code].duration} seconds") && PlaySFX("statdown"), 
                    TimeSpan.FromSeconds(1),
                    () => true,
                    TimeSpan.FromSeconds(1),
                    () => ManageBrianSizeChange(BRIAN_SMALL_SCALE),
                    TimeSpan.FromSeconds(effectDurations[code].repeatDelay), true, "briansize"
                ).WhenCompleted.Then(t =>
                {
                    Connector.Write16(ADDR_BRIAN_SCALE, BRIAN_ORIGINAL_SCALE);
                    Connector.SendMessage("Brian returned to his normal size.");
                });
                return;
            }

            case "wideview":
            {
                StartTimed(request,
                    () => Connector.Read32(ADDR_CAMERA_FOV, out uint currentFOV) && currentFOV != FOV_WIDE,
                    () =>
                    {
                        bool result = Connector.Freeze32(ADDR_CAMERA_FOV, FOV_WIDE);
                        if (result)
                            Connector.SendMessage($"{request.DisplayViewer} zoomed the camera out for {effectDurations[code].duration} seconds");

                        return result;
                    },
                    TimeSpan.FromSeconds(effectDurations[code].duration), "camerafov"
                );
                return;
            }

            case "narrowview":
            {
                StartTimed(request,
                    () => Connector.Read32(ADDR_CAMERA_FOV, out uint currentFOV) && currentFOV != FOV_NARROW,
                    () =>
                    {
                        bool result = Connector.Freeze32(ADDR_CAMERA_FOV, FOV_NARROW);
                        if (result)
                            Connector.SendMessage($"{request.DisplayViewer} zoomed the camera in for {effectDurations[code].duration} seconds");

                        return result;
                    },
                    TimeSpan.FromSeconds(effectDurations[code].duration), "camerafov"
                );
                return;
            }

            case "flipcamera":
            {
                StartTimed(request,
                    () => Connector.Read32(ADDR_CAMERA_FOV, out uint currentFOV) && currentFOV != FOV_INVERT,
                    () =>
                    {
                        bool result = Connector.Freeze32(ADDR_CAMERA_FOV, FOV_INVERT);
                        if (result)
                            Connector.SendMessage($"{request.DisplayViewer} flipped the camera for {effectDurations[code].duration} seconds");

                        return result;
                    },
                    TimeSpan.FromSeconds(effectDurations[code].duration),
                    "camerafov"
                );
                return;
            }
            
            case "giveitem":
            {
                var selectedItem = items[codeParams[1]];
                TryEffect(request,
                    () => (!IsInBattle() ||Connector.Read16(ADDR_BATTLE_STATE, out ushort state) && state == 1) 
                          && (Connector.Read16(ADDR_CURRENT_HP, out ushort currentHP) && currentHP > 0) 
                          && (Connector.Read8(ADDR_ITEM_QUEUE, out byte currentItem) && currentItem == 0xFF)
                          && (GetInventorySize() < MAX_INVENTORY_SIZE)
                          && (Connector.Read32(ADDR_TRANSITION_TIMER, out uint timer) && timer == 0),
                    () => Connector.Write8(ADDR_ITEM_QUEUE, selectedItem.value),
                    () => { Connector.SendMessage($"{request.DisplayViewer} gave a {selectedItem.name}"); },
                    TimeSpan.FromSeconds(effectDurations[code].retryDelay));
                return;
            }

            case "maxencounter":
            {
                StartTimed(request,
                    () => !IsInBattle(),
                    () =>
                    {
                        bool result = Connector.Freeze16(ADDR_ENCOUNTER_RATE, 0x7fff);
                        if (result)
                            Connector.SendMessage($"{request.DisplayViewer} made random battles much more likely for {effectDurations[code].duration} seconds");

                        return result;
                    },
                    TimeSpan.FromSeconds(effectDurations[code].duration), "encounterrate");
                return;
            }
            
            case "minencounter":
            {
                StartTimed(request,
                    () => !IsInBattle(),
                    () =>
                    {
                        bool result = Connector.Freeze16(ADDR_ENCOUNTER_RATE, 0x0000);
                        if (result)
                            Connector.SendMessage($"{request.DisplayViewer} made random battles much less likely for {effectDurations[code].duration} seconds");

                        return result;
                    },
                    TimeSpan.FromSeconds(effectDurations[code].duration), "encounterrate");
                return;
            }
            
            case "healenemy":
            {
                TryEffect(request,
                    () => IsInBattle() && FindDamagedEnemy() >= 0,
                    () => { return HealEnemy(); },
                    () => Connector.SendMessage($"{request.DisplayViewer} healed an injured enemy"),
                    TimeSpan.FromSeconds(effectDurations[code].retryDelay));
                return;
            }

            case "statuseffect":
            {
                var status = statusTypes[codeParams[1]];

                Connector.Read16(ADDR_BRIAN_STATUS, out ushort currentStatus);
                TryEffect(request,
                    () => IsInBattle() && (status.name != "Freeze" || !IsBossFight()) && Connector.Read8(status.durationAddress, out byte duration) && (duration == 0 || duration > 0x7f),
                    () =>
                    {
                        ushort newStatus = (ushort)(currentStatus | status.statusBit);
                        bool result = Connector.Write16(ADDR_BRIAN_STATUS, newStatus);
                        result = result && Connector.Write8(status.durationAddress, 3);

                        if (status.iconAddress > 0)
                            result = result && Connector.Write8(status.iconAddress, status.iconValue);
                        
                        PlaySFX(sfx[status.sfx]);
                        return result;
                    },
                    () => Connector.SendMessage($"{request.DisplayViewer} applied the {status.name} effect for 3 turns"),
                    TimeSpan.FromSeconds(effectDurations[code].retryDelay));
                return;
            }
            
            case "changemusic":
            {
                var changeBgm = bgmTracks[codeParams[1]];

                StartTimed(request,
                    () => (Connector.Read8(ADDR_NEXT_BGM, out byte currentBgm) && currentBgm != changeBgm.index),
                    () =>
                    {
                        bool result = Connector.Freeze8(ADDR_NEXT_BGM, changeBgm.index);
                        result = result && Connector.Write8(ADDR_BGM_SWAP_TIMER, 0xff);

                        if (result)
                            Connector.SendMessage($"{request.DisplayViewer} changed the music to \"{changeBgm.name}\" for {effectDurations[code].duration} seconds");

                        return result;
                    },
                    TimeSpan.FromSeconds(effectDurations[code].duration), "music").WhenCompleted.Then(t =>
                {
                    Connector.Unfreeze(ADDR_NEXT_BGM);
                    Connector.SendMessage("The background music is now unlocked");
                });
                return;
            }

            case "cloakcolor":
            {
                var cloakColor = cloakColors[codeParams[1]];
                TryEffect(request,
                    () => ChangeCloakColor(cloakColor.name, cloakColor.colors),
                    () => true,
                    () => Connector.SendMessage($"{request.DisplayViewer} changed Brian's cloak to {cloakColor.name}"),
                    TimeSpan.FromSeconds(effectDurations[code].retryDelay));
                return;
            }

            case "hidehud":
            {
                StartTimed(request,
                    () => Connector.Read16(ADDR_HUD_TIMER, out ushort hudTimeLeft) && hudTimeLeft == 0,
                    () =>
                    {
                        bool result =  Connector.Freeze32(ADDR_HUD_TIMER, 0x20);
                        if (result)
                            Connector.SendMessage($"{request.DisplayViewer} hid the HUD for for {effectDurations[code].duration} seconds");

                        return result;
                    },
                    TimeSpan.FromSeconds(effectDurations[code].duration)
                ).WhenCompleted.Then(t =>
                {
                    Connector.Unfreeze(ADDR_HUD_TIMER);
                    Connector.SendMessage("The HUD reappears");
                });
                return;
            }

            case "expensivespells":
            {
                RepeatAction(request,
                    TimeSpan.FromSeconds(effectDurations[code].duration),
                    () => true,
                    () =>
                    {
                        return Connector.Read16(ADDR_CURRENT_MP, out lastCurrentMP) 
                               && Connector.SendMessage($"{request.DisplayViewer} made spells cost double normal MP for {effectDurations[code].duration} seconds");
                    },
                    TimeSpan.FromSeconds(1),
                    () =>
                    {
                        bool result = Connector.Read16(ADDR_CURRENT_MP, out ushort currentMP);

                        int diff = lastCurrentMP - currentMP;
                        if (diff > 0 && currentMP - diff >= 0)
                        {
                            lastCurrentMP = (ushort)(currentMP - diff);
                            result = result && Connector.Write16(ADDR_CURRENT_MP, lastCurrentMP);
                        }
                        else
                            lastCurrentMP = currentMP;
                        
                        return result;
                    },
                    TimeSpan.FromSeconds(1),
                    () => true,
                    TimeSpan.FromSeconds(effectDurations[code].repeatDelay), true, "spellcost"
                );

                return;
            }

            case "cheapspells":
            {
                RepeatAction(request,
                    TimeSpan.FromSeconds(effectDurations[code].duration),
                    () => true,
                    () =>
                    {
                        lastCurrentMP = 0;
                        return Connector.SendMessage($"{request.DisplayViewer} made spells cost 0 MP for {effectDurations[code].duration} seconds");
                    },
                    TimeSpan.FromSeconds(1),
                    () =>
                    {
                        bool result = Connector.Read16(ADDR_CURRENT_MP, out ushort currentMP);
                        if (currentMP < lastCurrentMP)
                        {
                            result = result && Connector.Write16(ADDR_CURRENT_MP, lastCurrentMP);
                        }
                        else
                        {
                            lastCurrentMP = currentMP;
                        }

                        return result;
                    },
                    TimeSpan.FromSeconds(1),
                    () => true,
                    TimeSpan.FromSeconds(effectDurations[code].repeatDelay), true, "spellcost"
                );
                return;
            }

            case "movedown":
            {
                StartTimed(request,
                    () => IsInBattle() && Connector.Read32(ADDR_MOVE_SIZE_MULTIPLIER, out uint multiplier) && multiplier != MOVEMENT_MULTIPLIER_SMALL,
                    () =>
                    {
                        bool result = Connector.Freeze32(ADDR_MOVE_SIZE_MULTIPLIER, MOVEMENT_MULTIPLIER_SMALL);
                        if (result)
                            Connector.SendMessage($"{request.DisplayViewer} decreased Brian's movement range for {effectDurations[code].duration} seconds");

                        PlaySFX(sfx["statdown"]);
                        return result;
                    },
                    TimeSpan.FromSeconds(effectDurations[code].duration), "movemultiplier"
                ).WhenCompleted.Then(t =>
                {
                    Connector.Unfreeze(ADDR_MOVE_SIZE_MULTIPLIER);
                    Connector.Write32(ADDR_MOVE_SIZE_MULTIPLIER, MOVEMENT_MULTIPLIER_NORMAL);
                });
                return;
            }

            case "moveup":
            {
                StartTimed(request,
                    () => IsInBattle() && Connector.Read32(ADDR_MOVE_SIZE_MULTIPLIER, out uint multiplier) && multiplier != MOVEMENT_MULTIPLIER_BIG,
                    () =>
                    {
                        bool result = Connector.Freeze32(ADDR_MOVE_SIZE_MULTIPLIER, MOVEMENT_MULTIPLIER_BIG);
                        if (result)
                            Connector.SendMessage($"{request.DisplayViewer} increased Brian's movement range for {effectDurations[code].duration} seconds");

                        PlaySFX(sfx["statup"]);
                        return result;
                    },
                    TimeSpan.FromSeconds(effectDurations[code].duration), "movemultiplier"
                ).WhenCompleted.Then(t =>
                {
                    Connector.Unfreeze(ADDR_MOVE_SIZE_MULTIPLIER);
                    Connector.Write32(ADDR_MOVE_SIZE_MULTIPLIER, MOVEMENT_MULTIPLIER_NORMAL);
                });
                return;
            }

            case "hidecompass":
            {
                StartTimed(request,
                    () => !IsInBattle() && Connector.Read32(ADDR_COMPASS_TEXTURE, out uint textureDataAddress) && textureDataAddress == COMPASS_SHOW,
                    () =>
                    {
                        bool result = Connector.Write32(ADDR_COMPASS_TEXTURE, COMPASS_HIDE);
                        if (result)
                            Connector.SendMessage($"{request.DisplayViewer} took the compass away for {effectDurations[code].duration} seconds");

                        return result;
                    },
                    TimeSpan.FromSeconds(effectDurations[code].duration)
                ).WhenCompleted.Then(t =>
                {
                    Connector.Write32(ADDR_COMPASS_TEXTURE, COMPASS_SHOW);
                    Connector.SendMessage("The compass came back");
                });

                return;
            }

            case "randomspell":
            {
                RepeatAction(request,
                    TimeSpan.FromSeconds(20),
                    () => true,
                    () => Connector.SendMessage($"{request.DisplayViewer} randomized spells" + (codeParams[1] == "any" ? "" : " using spells from the " + codeParams[1] + " pool") + $" for {effectDurations[code].duration} seconds"),
                    TimeSpan.FromSeconds(1),
                    () => true,
                    TimeSpan.FromSeconds(1),
                    () =>
                    {
                        bool result = Connector.Read16(ADDR_SPELL_TIMER, out ushort currentSpellTimer);
                        if (previousSpellTimer > 0 && currentSpellTimer == 0)
                        {
                            // Connector.SendMessage("countdown finished");
                            result = result && Connector.Unfreeze(ADDR_SPELL_ID);
                        }
                        else if (previousSpellTimer == 0 && currentSpellTimer > 0)
                        {
                            var randomSpell = ChooseRandomSpell(codeParams[1]);
                            // Connector.SendMessage($"countdown started - {randomSpell.name}");
                            result = result && Connector.Freeze16(ADDR_SPELL_ID, randomSpell.spellId);
                        }

                        previousSpellTimer = currentSpellTimer;
                        return result;
                    },
                    TimeSpan.FromSeconds(effectDurations[code].repeatDelay), false, "randomspell"
                ).WhenCompleted.Then(t =>
                {
                    Connector.Unfreeze(ADDR_SPELL_ID);
                    Connector.SendMessage("Spells are no longer randomized");
                });

                return;
            }
                
            default:
                return;
        }
    }
    
    protected override bool StopEffect(EffectRequest request)
    {
        string[] codeParams = request.FinalCode.Split('_');
        switch (request.BaseCode)
        {
            case "lockelement_fire":
            case "lockelement_wind":
            case "lockelement_water":
            case "lockelement_earth":
            {
                var lockedElement = spiritTypes[codeParams[1]];
                Connector.Write16(lockedElement.levelReqAddress, 0x01);
                Connector.SendMessage($"{lockedElement.name} spells have been unlocked");
                return true;
            }
            case "narrowview":
            case "wideview":
            case "flipcamera":
            {
                Connector.Unfreeze(ADDR_CAMERA_FOV);
                Connector.Write32(ADDR_CAMERA_FOV, FOV_ORIGINAL);
                Connector.SendMessage($"Brian's view returns to normal");
                return true;
            }
            case "minencounter":
            case "maxencounter":
                Connector.Unfreeze(ADDR_ENCOUNTER_RATE);
                Connector.SendMessage("The encounter rate returns to normal");
                return true;
            default:
                return true;
        }
    }

    private void AdjustHPorMP(EffectRequest request, ulong currentAddress, int amount, string sfxName, string type, float delay)
    {
        ulong maxAddress = currentAddress + 2;
        Connector.Read16(maxAddress, out ushort max);
        int change = (int)(amount * (max / 10f));
            
        TryEffect(request,
            () =>                     
            {
                bool result = Connector.Read16(currentAddress, out ushort initial);
                int newValue = initial + change;
                result = result && newValue >= 0 && newValue <= max;
                        
                ushort final = (ushort)Math.Min(max, initial + change);

                if (result)
                {
                    PlaySFX(sfxName);
                    result = result && Connector.Write16(currentAddress, final);
                }

                return result;
            },
            () => true,
            () => Connector.SendMessage($"{request.DisplayViewer} {(amount > 0 ? "gave" : "took")} {(MathF.Abs(change))} {type}"),
            TimeSpan.FromSeconds(delay)
        );
    }
    
    public void AdjustStat(EffectRequest request, ulong statAddress, int amount, string sfxName, string type, float delay)
    {
        TryEffect(request,
            () =>
            {
                const int maxStat = 512;
                
                bool result = Connector.Read16(statAddress, out ushort statValue);
                int newValue = statValue + amount;
                result = result && newValue > 0 && statValue < maxStat;
                statValue = amount > 0 ? (ushort)Math.Min(newValue, maxStat) : (ushort)Math.Max(0, newValue);

                if (result)
                {
                    PlaySFX(sfxName);
                    result = result && Connector.Write16(statAddress, statValue);
                }

                return result;
            },
            () => true,
            () => Connector.SendMessage($"{request.DisplayViewer} {(amount > 0 ? "increased" : "decreased")} {type} by {(Math.Abs(amount))}"),
            TimeSpan.FromSeconds(delay)
        );
    }
        
    private int FindDamagedEnemy()
    {
        byte enemyCount = 0;
        int damagedEnemyIndex = -1;

        bool result = true;
        result = result && Connector.Read8(ADDR_ENEMY_COUNT, out enemyCount);

        if (enemyCount <= 0 || enemyCount > 6)
            return -1;

        for (ushort i = 0; i < enemyCount; i++)
        {
            ushort enemyCurrentHealth = 0;
            ushort enemyMaxHealth = 0;
            
            result = result && Connector.Read16(ADDR_ENEMY_CURRENT_HEALTH + (i * ENEMY_DATA_SIZE), out enemyCurrentHealth);
            result = result && Connector.Read16(ADDR_ENEMY_MAX_HEALTH + (i * ENEMY_DATA_SIZE), out enemyMaxHealth);

            if (enemyCurrentHealth < enemyMaxHealth)
            {
                damagedEnemyIndex = i;
                break;
            }
        }

        return !result || damagedEnemyIndex < 0 ? -1 : damagedEnemyIndex;
    }
    
    private bool HealEnemy()
    {
        int healedEnemyIndex = FindDamagedEnemy();
        bool result = healedEnemyIndex >= 0;
        
        if (result)
        {
            result = Connector.Read16(ADDR_ENEMY_CURRENT_HEALTH, out ushort enemyCurrentHealth);
            result = Connector.Read16(ADDR_ENEMY_MAX_HEALTH + ((byte)healedEnemyIndex * ENEMY_DATA_SIZE), out ushort enemyMaxHealth) && result;

            ushort newEnemyHealth = IsBossFight() ? (ushort)((enemyMaxHealth >> 2) + enemyCurrentHealth) : enemyMaxHealth;
            newEnemyHealth = newEnemyHealth > enemyMaxHealth ? enemyMaxHealth : newEnemyHealth;
            result = Connector.Write16(ADDR_ENEMY_CURRENT_HEALTH + ((byte)healedEnemyIndex * ENEMY_DATA_SIZE), newEnemyHealth) && result;
        }

        return result;
    }

    private bool ChangeCloakColor(string colorName, ulong[] colorData)
    {
        bool result = true;
        
        for (int i = 0; i < ADDR_CLOAK_POLYS.Length; i++)
        {
            ulong nextColor = colorData[i];

            result = result && Connector.Write8(ADDR_CLOAK_POLYS[i], (byte)((nextColor >> 16) & 0xff));
            result = result && Connector.Write8(ADDR_CLOAK_POLYS[i] + 1, (byte)((nextColor >> 8) & 0xff));
            result = result && Connector.Write8(ADDR_CLOAK_POLYS[i] + 2, (byte)(nextColor & 0xff));
        }

        return result;
    }

    private bool PlaySFX(byte index)
    {
        //return true;
        bool result = true;

        if (index == 0)
            return false;
        
        // there's a queue for sound effects, with each slot having 0xff if no sound is queued there
        // ADDR_SFX_QUEUE is the index to the slot for the next sound, so the index should be written there
        uint nextIndex = 0;
        result = result && Connector.Read32(ADDR_NEXT_SFX, out nextIndex);

        // For some reason the 6th sfx slot is cursed and writing most sound effect ids
        // to it manually will destroy all sound and cause a crash eventually
        if (nextIndex == 5)
        {
            result = result && Connector.Write32(ADDR_SFX_QUEUE[nextIndex], 0x00000021);
            nextIndex++;
        }

        result = result && Connector.Write32(ADDR_NEXT_SFX, (nextIndex + 1) % 8);

        if (nextIndex >= 8)
            return false;
        
        result = result && Connector.Write8(ADDR_SFX_QUEUE[nextIndex], index);

        // Connector.SendMessage($"Writing {index} to " + ADDR_SFX_QUEUE[nextIndex]);
        return result;
    }

    private bool PlaySFX(string name)
    {
        //return true;
        return PlaySFX(sfx[name]);
    }

    private (Element, ushort spellId, byte, string name) ChooseRandomSpell(string elementName)
    {
        Element e = Element.Any;
                
        switch (elementName)
        {
            case "fire":
                e = Element.Fire;
                break;
            case "earth":
                e = Element.Earth;
                break;
            case "water":
                e = Element.Water;
                break;
            case "wind":
                e = Element.Wind;
                break;
        }

        Connector.Read8(spiritTypes["fire"].countAddress,  out byte fireCount);
        Connector.Read8(spiritTypes["earth"].countAddress, out byte earthCount);
        Connector.Read8(spiritTypes["water"].countAddress, out byte waterCount);
        Connector.Read8(spiritTypes["wind"].countAddress,  out byte windCount);

        var spellLevels = new Dictionary<Element, byte>()
        {
            {Element.Fire, fireCount},
            {Element.Earth, earthCount},
            {Element.Water, waterCount},
            {Element.Wind, windCount},
        };

        // build a list of spells that could possibly be chosen. spells must match the given
        // element, unless all elements should be included. the current spirit count for the
        // spell's element also needs to be <= the level requirement for the spell, to ensure
        // only spells brian can currently use are selected
        var availableSpells = new List<(Element, ushort spellId, byte, string name)>();
        foreach (var spell in spells)
        {
            if (spell.levelReq <= spellLevels[spell.element] && (spell.element == e || e == Element.Any))
                availableSpells.Add(spell);
        }

        // pick a random spell among the list of options
        Random r = new Random();
        int spellIndex = r.Next(0, availableSpells.Count);
        return availableSpells.ElementAt(spellIndex);
    }

    private bool ManageBrianSizeChange(ushort targetScale)
    {
        bool result = Connector.Read32(ADDR_TRANSITION_TIMER, out uint transitionTime);
        result = Connector.Read8(ADDR_BRIAN_ANIMATION_ID, out byte animationId) && result;

        // keep the original scale through door entering/exiting animations so that
        // brian gets positioned correctly on the other side
        if (animationId is 0x14 or 0x15 or 0x1a || transitionTime > 0)
            result = result && Connector.Write16(ADDR_BRIAN_SCALE, BRIAN_ORIGINAL_SCALE);
        else
            result = result && Connector.Write16(ADDR_BRIAN_SCALE, targetScale);

        return result;
    }

    public bool IsBossFight()
    {
        // if (enemyIndex > 0)
        //     return false;

        return Connector.Read8(0x8008c592, out byte bossFlag) && bossFlag == 0x01;
        // Connector.Read16(ADDR_ENEMY_MAX_HEALTH, out ushort enemyMaxHP);
        // return (enemyMaxHP is 0x00C8 or 0x0370 or 0x03E8 or 0x05DC or 0x0708 or 0x076C or 0x08FC);
    }

    public bool IsInBattle()
    {
        return Connector.Read8(ADDR_ENEMY_COUNT, out byte enemyCount) && (enemyCount > 0);
    }

    public uint GetInventorySize()
    {
        uint i = 0;
        // The inventory is 150 bytes long
        for (; i < MAX_INVENTORY_SIZE + 10; i++)
        {
            Connector.Read8(ADDR_INVENTORY_START + i, out byte inventoryChunk);
            if (inventoryChunk == 0xff) return i;
        }

        return i;
    }
}
public enum EVolumeType
{
    Master,
    BGM,
    SFX
}

public enum ESceneName
{
    Developer,
    Title,
    Game,
    Empty
}

public enum EWaveState
{
    Starting,
    Active,
    Advancing,
    Recovering,
    Ended
}

public enum EWaveResolutionResult
{
    Cleared,
    Failed
}

public enum EBattleTeam
{
    Ally,
    Enemy
}

public enum EBattleUnitState
{
    Idle,
    Moving,
    Attacking,
    Hit,
    Dead
}

public enum EPinballState
{
    Idle,
    Launched,
}

public enum EItemCategory
{
    Ball = 0,
    Board = 1,
    Battle = 2
}

public enum EItem
{
    GoldenBall = 4,
    AutoBallFeeder = 5,
    TargetMagnet = 6,
    SplitCapsule = 7,
    GoldenBumper = 9,
    FocusedPocket = 11,
    SwapLever = 13,
    ChargedPin = 14,
    OverloadBumper = 15,
    BattleClock = 17,
    FieldArmor = 18,
    DiversityEmblem = 20,
    BarrierReinforcement = 21,
    PersonalHealingPotion = 22,
    PartyHealingPotion = 23,
}

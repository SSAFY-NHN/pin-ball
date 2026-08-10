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
    Pending,
    Active,
    Resolving,
    Victory,
    Defeat
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
    PrecisionAimRail = 0,
    WeightedCore,
    ElasticCoating,
    RecoveryInsurance,
    GoldenBall,
    AutoBallFeeder,
    TargetMagnet,
    SplitCapsule,

    ReinforcedBumper,
    GoldenBumper,
    WidePocket,
    FocusedPocket,
    SafetyNet,
    SwapLever,
    ChargedPin,
    OverloadBumper,

    AttackManual,
    BattleClock,
    FieldArmor,
    DuplicationSeal,
    DiversityEmblem,
    BarrierReinforcement,
    HealthPotion,
    FullHealthPotion,
}

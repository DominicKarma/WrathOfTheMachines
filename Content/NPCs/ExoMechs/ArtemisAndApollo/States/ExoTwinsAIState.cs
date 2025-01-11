using WoTM.Content.NPCs.ExoMechs.ComboAttacks;

namespace WoTM.Content.NPCs.ExoMechs.ArtemisAndApollo.States
{
    public enum ExoTwinsAIState
    {
        SpawnAnimation,
        DashesAndLasers,
        CloseShots,
        MachineGunLasers,
        ExothermalOverload,

        Inactive,
        Leave,

        EnterSecondPhase,

        DeathAnimation,

        PerformIndividualAttacks,

        PerformComboAttack = ExoMechComboAttackManager.ComboAttackValue
    }
}

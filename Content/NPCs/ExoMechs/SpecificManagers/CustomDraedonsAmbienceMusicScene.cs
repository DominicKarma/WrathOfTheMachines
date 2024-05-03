using CalamityMod.NPCs;
using CalamityMod.NPCs.ExoMechs;
using CalamityMod.Systems;
using Terraria.ModLoader;

namespace WoTM.Content.NPCs.ExoMechs
{
    public class CustomDraedonsAmbienceMusicScene : BaseMusicSceneEffect
    {
        public override SceneEffectPriority Priority => SceneEffectPriority.BossHigh;

        public override int NPCType => ModContent.NPCType<Draedon>();

        public override int? MusicModMusic => MusicLoader.GetMusicSlot("WoTM/Assets/Sounds/Music/BeatsToContemplateWhichRobotShouldKickYourAssTo");

        public override int VanillaMusic => -1;

        public override int OtherworldMusic => -1;

        public override bool AdditionalCheck() => CalamityGlobalNPC.draedonAmbience != -1;
    }
}

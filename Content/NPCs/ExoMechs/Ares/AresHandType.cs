using System.IO;

namespace WoTM.Content.NPCs.ExoMechs
{
    /// <summary>
    /// Represents a type of hand that Ares may use.
    /// </summary>
    /// <param name="NameLocalizationKey">The localization key for the arm's display name.</param>
    /// <param name="TexturePath">The path to the arm's texture.</param>
    /// <param name="GlowmaskPath">The path to the arm's glowmask texture.</param>
    public record AresHandType(string NameLocalizationKey, string TexturePath, string GlowmaskPath)
    {
        /// <summary>
        /// The representation of Ares' plasma cannon.
        /// </summary>
        public static readonly AresHandType PlasmaCannon = new("Mods.CalamityMod.NPCs.AresPlasmaFlamethrower.DisplayName", "CalamityMod/NPCs/ExoMechs/Ares/AresPlasmaFlamethrower", "CalamityMod/NPCs/ExoMechs/Ares/AresPlasmaFlamethrowerGlow");

        /// <summary>
        /// The representation of Ares' tesla cannon.
        /// </summary>
        public static readonly AresHandType TeslaCannon = new("Mods.CalamityMod.NPCs.AresTeslaCannon.DisplayName", "CalamityMod/NPCs/ExoMechs/Ares/AresTeslaCannon", "CalamityMod/NPCs/ExoMechs/Ares/AresTeslaCannonGlow");

        /// <summary>
        /// The representation of Ares' laser cannon.
        /// </summary>
        public static readonly AresHandType LaserCannon = new("Mods.CalamityMod.NPCs.AresLaserCannon.DisplayName", "CalamityMod/NPCs/ExoMechs/Ares/AresLaserCannon", "CalamityMod/NPCs/ExoMechs/Ares/AresLaserCannonGlow");

        /// <summary>
        /// The representation of Ares' gauss nuke.
        /// </summary>
        public static readonly AresHandType GaussNuke = new("Mods.CalamityMod.NPCs.AresGaussNuke.DisplayName", "CalamityMod/NPCs/ExoMechs/Ares/AresGaussNuke", "CalamityMod/NPCs/ExoMechs/Ares/AresGaussNukeGlow");

        /// <summary>
        /// Writes the state of this arm type to a given <see cref="BinaryWriter"/>, for the purposes of being sent across the network.
        /// </summary>
        /// <param name="writer"></param>
        public void WriteTo(BinaryWriter writer)
        {
            writer.Write(NameLocalizationKey);
            writer.Write(TexturePath);
            writer.Write(GlowmaskPath);
        }

        /// <summary>
        /// Constructs an Ares arm type from data in a <see cref="BinaryReader"/>, for the purposes of being received across the network.
        /// </summary>
        /// <param name="reader"></param>
        public static AresHandType ReadFrom(BinaryReader reader)
        {
            return new(reader.ReadString(), reader.ReadString(), reader.ReadString());
        }
    }
}

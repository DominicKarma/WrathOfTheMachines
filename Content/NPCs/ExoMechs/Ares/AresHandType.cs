using System.IO;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;

namespace WoTM.Content.NPCs.ExoMechs
{
    /// <summary>
    /// Represents a type of hand that Ares may use.
    /// </summary>
    /// <param name="NameLocalizationKey">The localization key for the arm's display name.</param>
    /// <param name="TexturePath">The path to the arm's texture.</param>
    /// <param name="GlowmaskPath">The path to the arm's glowmask texture.</param>
    /// <param name="TotalHorizontalFrames">The amount of horizontal frames on the texture's sheet.</param>
    /// <param name="TotalVerticalFrames">The amount of vertical frames on the texture's sheet.</param>
    /// <param name="EnergyTelegraphColor">The color of energy particles generated prior to attacking via the <see cref="AresCannonChargeParticleSet"/>.</param>
    /// <param name="CustomGoreNames">The path for custom gore types.</param>
    public record AresHandType(string NameLocalizationKey, string TexturePath, string GlowmaskPath, int TotalHorizontalFrames, int TotalVerticalFrames, Color EnergyTelegraphColor, params string[] CustomGoreNames)
    {
        /// <summary>
        /// The representation of Ares' plasma cannon.
        /// </summary>
        public static readonly AresHandType PlasmaCannon = new("Mods.CalamityMod.NPCs.AresPlasmaFlamethrower.DisplayName", "CalamityMod/NPCs/ExoMechs/Ares/AresPlasmaFlamethrower", "CalamityMod/NPCs/ExoMechs/Ares/AresPlasmaFlamethrowerGlow", 6, 8, Color.GreenYellow,
            "CalamityMod/AresPlasmaFlamethrower1", "CalamityMod/AresPlasmaFlamethrower2");

        /// <summary>
        /// The representation of Ares' tesla cannon.
        /// </summary>
        public static readonly AresHandType TeslaCannon = new("Mods.CalamityMod.NPCs.AresTeslaCannon.DisplayName", "CalamityMod/NPCs/ExoMechs/Ares/AresTeslaCannon", "CalamityMod/NPCs/ExoMechs/Ares/AresTeslaCannonGlow", 6, 8, Color.Aqua,
            "CalamityMod/AresTeslaCannon1", "CalamityMod/AresTeslaCannon2");

        /// <summary>
        /// The representation of Ares' laser cannon.
        /// </summary>
        public static readonly AresHandType LaserCannon = new("Mods.CalamityMod.NPCs.AresLaserCannon.DisplayName", "CalamityMod/NPCs/ExoMechs/Ares/AresLaserCannon", "CalamityMod/NPCs/ExoMechs/Ares/AresLaserCannonGlow", 6, 8, Color.OrangeRed,
            "CalamityMod/AresLaserCannon1", "CalamityMod/AresLaserCannon2");

        /// <summary>
        /// The representation of Ares' gauss nuke.
        /// </summary>
        public static readonly AresHandType GaussNuke = new("Mods.CalamityMod.NPCs.AresGaussNuke.DisplayName", "CalamityMod/NPCs/ExoMechs/Ares/AresGaussNuke", "CalamityMod/NPCs/ExoMechs/Ares/AresGaussNukeGlow", 9, 12, Color.Yellow,
            "CalamityMod/AresGaussNuke1", "CalamityMod/AresGaussNuke2", "CalamityMod/AresGaussNuke3");

        /// <summary>
        /// Writes the state of this arm type to a given <see cref="BinaryWriter"/>, for the purposes of being sent across the network.
        /// </summary>
        /// <param name="writer"></param>
        public void WriteTo(BinaryWriter writer)
        {
            writer.Write(EnergyTelegraphColor.R);
            writer.Write(EnergyTelegraphColor.G);
            writer.Write(EnergyTelegraphColor.B);
            writer.Write(EnergyTelegraphColor.A);

            writer.Write(NameLocalizationKey);
            writer.Write(TexturePath);
            writer.Write(GlowmaskPath);
            writer.Write(TotalHorizontalFrames);
            writer.Write(TotalVerticalFrames);
        }

        /// <summary>
        /// Constructs an Ares arm type from data in a <see cref="BinaryReader"/>, for the purposes of being received across the network.
        /// </summary>
        /// <param name="reader"></param>
        public static AresHandType ReadFrom(BinaryReader reader)
        {
            byte energyR = reader.ReadByte();
            byte energyG = reader.ReadByte();
            byte energyB = reader.ReadByte();
            byte energyA = reader.ReadByte();

            return new(reader.ReadString(), reader.ReadString(), reader.ReadString(), reader.ReadInt32(), reader.ReadInt32(), new(energyR, energyG, energyB, energyA));
        }
    }
}

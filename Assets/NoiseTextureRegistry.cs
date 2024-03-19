using Luminance.Assets;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace DifferentExoMechs.Assets
{
    public class MiscTexturesRegistry : ModSystem
    {
        #region Texture Path Constants

        public const string ExtraTexturesPath = "DifferentExoMechs/Assets/ExtraTextures";

        #endregion Texture Path Constants

        #region Noise Textures

        public static readonly LazyAsset<Texture2D> RadialNoise = LoadDeferred($"{ExtraTexturesPath}/RadialNoise");

        #endregion Noise Textures

        #region Loader Utility

        private static LazyAsset<Texture2D> LoadDeferred(string path)
        {
            // Don't attempt to load anything server-side.
            if (Main.netMode == NetmodeID.Server)
                return default;

            return LazyAsset<Texture2D>.Request(path, AssetRequestMode.ImmediateLoad);
        }

        #endregion Loader Utility
    }
}

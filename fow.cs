using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System.ComponentModel;
using System.Diagnostics;
using Terraria;
using Terraria.Graphics.Effects;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;

namespace fow
{
    
    public class fowMain : Mod
    {
        public override void Load() //sets up our shaders; we are NOT activating them.
        {
            if (Main.netMode == NetmodeID.Server) return;
            Asset<Effect> filterShader = this.Assets.Request<Effect>("shader/basicFoW");
            Filters.Scene["FOW:FOW"] = new Filter(new ScreenShaderData(filterShader, "VisionPass"), EffectPriority.Medium);
            Filters.Scene["FOW:FOW"].Load();

            Asset<Effect> filterShader2 = this.Assets.Request<Effect>("shader/advanceFoW", AssetRequestMode.ImmediateLoad);
            Filters.Scene["FOW:advanceFOW"] = new Filter(new ScreenShaderData(filterShader2, "VisionPass"), EffectPriority.Medium);
            Filters.Scene["FOW:advanceFOW"].Load();

        }
        
    }


    public class FowCondig : ModConfig
    {
        public override ConfigScope Mode => ConfigScope.ClientSide;

        //[Label("Older Algorithm")]
        //[Tooltip("This is original release version... I uh, don't reccomend unless you wish for pain.\n DOESN'T SUPPORT CONFIG OPTIONS")]
        //public bool oldVersion;

        [Label("Debug Mask -- why would you?")]
        [DefaultValue(false)]
        [Tooltip("haha yeah debug! yeah! why would tho you don't give a fuck you aint debugging don't lie to me :)")]
        public bool DEBUG;

        [Label("How far you can see")]
        [DefaultValue(50)]
        [Tooltip("Defines your vision range")]
        public int visionRange;

        [Label("How dark is the non-visible parts")]
        [DefaultValue(255f)]
        [Tooltip("0 is visible, 255 is completely blacked out.")]
        public float darkness;

        [Label("see through platforms")]
        [DefaultValue(false)]
        [Tooltip("enable this to see through those platforms you COWARD")]
        public bool platform;


        //[Label("is it blurred or not?")]
        //public bool bluriness;

        //[Label("lines or tiles")]
        //[Tooltip("Okay so you can either have it be tile based or have it act like actual vision. tile is cooler imo")]
        //public bool tilesOrLine;


        public override void OnChanged()
        {
            fow.mainEnginge.DEBUG = DEBUG;
            fow.mainEnginge.visionRange = visionRange;
            fow.mainEnginge.darkness = darkness;
            fow.mainEnginge.platform = platform;
            // Here we use the OnChanged hook to initialize ExampleUI.visible with the new values.
            // We maintain both ExampleUI.visible and ShowCoinUI as separate values so ShowCoinUI can act as a default while ExampleUI.visible can change within a play session.
            //UI.ExampleUI.Visible = blackout;
        }
    }
}
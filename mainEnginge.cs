using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Graphics;
using Terraria.Graphics.Effects;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI;
namespace fow
{
    public class mainEnginge : ModSystem
    {
        public ScreenShaderData shaderRef;
        public static Texture2D TileMask; //the mask passed to the shader
        private Color[] _maskData; // width*height of the mask
        static public bool OLDERVERSION = true;
        static public bool DEBUG = false;
        static public int visionRange = 50;
        static public float darkness = 255f;
        static public bool platform = true;
        public override void Load()
        {
            if (Main.dedServ) return;

        }

        public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
        {
            int healthBarLayber = layers.FindIndex(layer => layer.Name == "Vanilla: Entity Health Bars");
            if (healthBarLayber != -1) layers.RemoveAt(healthBarLayber);

            int playerlayer = layers.FindIndex(layer => layer.Name == "Vanilla: MP Player Names");
            if (playerlayer != -1) layers.RemoveAt(playerlayer);
        }  
        

        public override void PostUpdateEverything()
        {
            if (Main.gameMenu) return;
            if (Main.netMode == NetmodeID.Server) return;
            //if (shaderRef is null) shaderRef = Filters.Scene["FOW:advanceFOW"].GetShader(); // ensures the ref is populated
            if (!Filters.Scene[!OLDERVERSION ? "FOW:advanceFOW" : "FOW:FOW"].IsActive()) Filters.Scene.Activate(!OLDERVERSION ? "FOW:advanceFOW" : "FOW:FOW"); // enables the shader
            if (Filters.Scene[OLDERVERSION ? "FOW:advanceFOW" : "FOW:FOW"].IsActive()) Filters.Scene.Deactivate(OLDERVERSION ? "FOW:advanceFOW" : "FOW:FOW");
            if (OLDERVERSION) UpdateMaskV1();
            else UpdateMaskV12();
        
        }
        private void UpdateMaskV1()
        {

            // 1. Calculate the visible tile range
            int startX = (int)(Main.screenPosition.X / 16f) - 1;
            int startY = (int)(Main.screenPosition.Y / 16f) - 1;
            int width = (Main.screenWidth) + 3;
            int height = (Main.screenHeight) + 3;
            if (TileMask == null || TileMask.Width != width || TileMask.Height != height)
            {
                TileMask = new Texture2D(Main.graphics.GraphicsDevice, width, height);
                _maskData = new Color[width * height];
            }
            //Color.Black
            System.Array.Fill(_maskData, new Color(0f,0f,0f, darkness));

            // debateable
            int centerX = startX + width / 2;
            int centerY = startY + height / 2;
            int radius = height / 2;

            float GetSlope(Vector2 p1, Vector2 p2, bool pInvert)
            {
                if (pInvert)
                    return (p1.Y - p2.Y) / (p1.X - p2.X);
                else
                    return (p1.X - p2.X) / (p1.Y - p2.Y);
            }
            void setPixel(int x, int y, int col = 0)
            {
                
                float zoom = Main.GameViewMatrix.Zoom.X;
                int scale = (int)Math.Ceiling(16 * zoom);
                Vector2 worldPos = new Vector2(x * 16f, y * 16f);
                Vector2 screenPos = worldPos - Main.screenPosition;
                Vector2 finalPos = Vector2.Transform(screenPos, Main.GameViewMatrix.TransformationMatrix);

                int startX = (int)finalPos.X;
                int startY = (int)finalPos.Y;

                int minX = Math.Max(0, startX);
                int maxX = Math.Min(width, startX + scale);
                int minY = Math.Max(0, startY);
                int maxY = Math.Min(height, startY + scale);

                if (minX >= maxX || minY >= maxY) return;

                Color cl = col switch
                {
                    1 => Color.Red,
                    2 => Color.Green,
                    3 => Color.Blue,
                    _ => Color.White
                };


                for (int py = minY; py < maxY; py++)
                        for (int px = minX; px < maxX; px++)
                                _maskData[px + py * width] = cl;
                
            }
            bool isTileSolid(int x, int y)
            {
                //if (!WorldGen.InWorld((int)x, (int)y)) {  return false; }
                Tile tile = Main.tile[(int)x, (int)y];
                if(DEBUG)
                    if ((platform?tile.TileType == TileID.Platforms:(tile.TileType == TileID.Platforms && tile.TileFrameY >= 250 && 269 >= tile.TileFrameY)) ||tile.IsActuated || tile.IsHalfBlock || tile.IsTileInvisible || tile.TileType == TileID.Glass || (tile.TileType == TileID.ClosedDoor && tile.TileFrameY >= 1080 && tile.TileFrameY <= 1132 && tile.TileFrameX < 70)) setPixel(x, y, 3);
                    else if (tile.HasTile && Main.tileSolid[tile.TileType]) setPixel(x, y, 1);
                    else if (tile.HasTile) setPixel(x, y, 2);
                //if (tile.HasTile) setPixel(x, y, 1);
                //return false;
                bool isSolid = tile.HasUnactuatedTile && // using this instead tile.hasTile bc we want to see through tiles we cant colide with
                                !tile.IsHalfBlock && //just looks better imo -- additonally if you have a wall thats a half block, wouldnt you say you would be able to see through?
                                !tile.IsTileInvisible && // IMO, you should be able see through echo paint, like the invisible cloak ts
                                tile.TileType != TileID.Glass && // see through glass !!!
                                (tile.TileType == TileID.ClosedDoor? !(tile.TileFrameY >= 1080 && tile.TileFrameY <= 1132 && tile.TileFrameX < 70) : true) && //condictionally check if its a door, then checks if its a glass door
                                (platform?(tile.TileType != TileID.Platforms):!(tile.TileType == TileID.Platforms && tile.TileFrameY >= 250 && 269 >= tile.TileFrameY)) && // see through platforms - optional, if disabled only see through glass platforms
                                Main.tileSolid[tile.TileType]; // and the final check of making sure its solid
                //if (!tile.HasTile && Main.tileSolid[tile.TileType]) Mod.Logger.InfoFormat("{0} {1} {2}", (x,y),tile.HasTile, tile.TileType);
                return isSolid;

            }
            //Main.instance.

            // FIRST WE DO QUAD 1 FOR SIMPLE
            void scan(int quad, float startingAngle, float endAngle, Vector2 center, int row = 1)
            {
                int x = 0; int y = 0;

                //var newstartingAngle = startingAngle;
                //bool wasPrevSolid = false;
                //START SWITCH
                bool wha = startingAngle != 1f;
                if ((!WorldGen.InWorld((int)center.X, (int)center.Y))) { Mod.Logger.InfoFormat("{0} was out of world on quad {1}", center, quad); return; }
                switch (quad)
                {
                    case 1: // NNW
                        y = (int)center.Y - row;
                        if (!WorldGen.InWorld((int)center.X, y)) return;

                        x = (int)center.X - (int)(startingAngle * row);
                        while (GetSlope(new Vector2(x, y), center, false) >= endAngle)
                        {
                            if (WorldGen.InWorld(x, y) && Vector2.Distance(new Vector2(x, y), center) <= visionRange)
                            {
                                if(wha)
                                setPixel(x - 1, y);
                                setPixel(x, y);
                                if (isTileSolid(x, y))
                                {
                                    if (WorldGen.InWorld(x - 1, y) && !isTileSolid(x - 1, y))
                                        scan(quad, startingAngle, GetSlope(new Vector2(x-0.5f, y+0.5f), center, false), center, row + 1);
                                }
                                else
                                {
                                    if (WorldGen.InWorld(x - 1, y) && isTileSolid(x - 1, y))
                                        startingAngle = GetSlope(new Vector2(x - 0.5f, y + 0.5f), center, false);
                                    
                                }
                            }
                            x++;
                        }
                        x--;
                        break;

                    case 2: // NNE
                        y = (int)center.Y - row;
                        if (!WorldGen.InWorld((int)center.X, y)) return;
                        x = (int)center.X + (int)(startingAngle * row);
                        while (GetSlope(new Vector2(x, y), center, false) <= endAngle)
                        {

                            if (WorldGen.InWorld(x, y) && Vector2.Distance(new Vector2(x, y), center) <= visionRange)
                            {
                                if(wha)
                                setPixel(x + 1, y);
                                setPixel(x, y);
                                if (isTileSolid(x, y))
                                {
                                    if (WorldGen.InWorld(x + 1, y) && !isTileSolid(x + 1, y))
                                        scan(quad, startingAngle, GetSlope(new Vector2(x + 0.5f, y + 0.5f), center, false), center, row + 1);
                                }
                                else
                                {
                                    if (WorldGen.InWorld(x + 1, y) && isTileSolid(x + 1, y))
                                        startingAngle = -GetSlope(new Vector2(x + 0.5f, y - 0.5f), center, false);
                                    //setPixel(x, y);
                                }
                            }
                            x--;
                        }
                        x++;
                        break;

                    case 3: // ENE
                        x = (int)center.X + row;
                        if (!WorldGen.InWorld(x, (int)center.Y)) return;
                        y = (int)center.Y - (int)(startingAngle * row);
                        while (GetSlope(new Vector2(x, y), center, true) <= endAngle)
                        {
                            if (WorldGen.InWorld(x, y) && Vector2.Distance(new Vector2(x, y), center) <= visionRange)
                            {
                                setPixel(x, y);
                                if(wha)
                                setPixel(x, y-1);
                                if (isTileSolid(x, y))
                                {
                                    if (WorldGen.InWorld(x, y - 1) && !isTileSolid(x, y - 1))
                                        scan(quad, startingAngle, GetSlope(new Vector2(x - 0.5f, y - 0.5f), center, true), center, row + 1);
                                }
                                else
                                {
                                    if (WorldGen.InWorld(x, y - 1) && isTileSolid(x, y - 1))
                                        startingAngle = -GetSlope(new Vector2(x + 0.5f, y - 0.5f), center, true);
                                    //setPixel(x, y);
                                }
                            }
                            y++;
                        }
                        y--;
                        break;

                    case 4: // ESE
                        x = (int)center.X + row;
                        if (!WorldGen.InWorld(x, (int)center.Y)) return;
                        y = (int)center.Y + (int)(startingAngle * row);
                        while (GetSlope(new Vector2(x, y), center, true) >= endAngle)
                        {
                            //setPixel(x, y);
                            if (WorldGen.InWorld(x, y) && Vector2.Distance(new Vector2(x, y), center) <= visionRange)
                            {
                                setPixel(x, y);
                                if(wha)
                                setPixel(x, y + 1);
                                if (isTileSolid(x, y))
                                {
                                    if (WorldGen.InWorld(x, y + 1) && !isTileSolid(x, y + 1))
                                        scan(quad, startingAngle, GetSlope(new Vector2(x - 0.5f, y + 0.5f), center, true), center, row + 1);
                                }
                                else
                                {
                                    if (WorldGen.InWorld(x, y + 1) && isTileSolid(x, y + 1))
                                        startingAngle = GetSlope(new Vector2(x + 0.5f, y + 0.5f), center, true);
                                    //setPixel(x, y);
                                }
                            }
                            y--;
                        }
                        y++;
                        break;

                    case 5: // SSE
                        y = (int)center.Y + row;
                        if (!WorldGen.InWorld((int)center.X, y)) return;
                        x = (int)center.X + (int)(startingAngle * row);
                        while (GetSlope(new Vector2(x, y), center, false) >= endAngle)
                        {
                            //setPixel(x, y);
                            if (WorldGen.InWorld(x, y) && Vector2.Distance(new Vector2(x, y), center) <= visionRange)
                            {
                                if(wha)
                                setPixel(x + 1, y);
                                setPixel(x, y);
                                if (isTileSolid(x, y))
                                {
                                    if (WorldGen.InWorld(x + 1, y) && !isTileSolid(x + 1, y))
                                        scan(quad, startingAngle, GetSlope(new Vector2(x + 0.5f, y - 0.5f), center, false), center, row + 1);
                                }
                                else
                                {
                                    if (WorldGen.InWorld(x + 1, y) && isTileSolid(x + 1, y))
                                        startingAngle = GetSlope(new Vector2(x + 0.5f, y + 0.5f), center, false);
                                    //setPixel(x, y);
                                }
                            }
                            x--;
                        }
                        x++;
                        break;

                    case 6: // SSW
                        y = (int)center.Y + row;
                        if (!WorldGen.InWorld((int)center.X, y)) return;
                        x = (int)center.X - (int)(startingAngle * row);
                        while (GetSlope(new Vector2(x, y), center, false) <= endAngle)
                        {
                            if (WorldGen.InWorld(x, y) && Vector2.Distance(new Vector2(x, y), center) <= visionRange)
                            {
                                setPixel(x, y);
                                if(wha)
                                setPixel(x - 1, y);
                                if (isTileSolid(x, y))
                                {
                                    if (WorldGen.InWorld(x - 1, y) && !isTileSolid(x - 1, y))
                                        scan(quad, startingAngle, GetSlope(new Vector2(x - 0.5f, y - 0.5f), center, false), center, row + 1);
                                }
                                else
                                {
                                    if (WorldGen.InWorld(x - 1, y) && isTileSolid(x - 1, y))
                                        startingAngle = -GetSlope(new Vector2(x - 0.5f, y + 0.5f), center, false);
                                    //setPixel(x, y);
                                }
                            }
                            x++;
                        }
                        x--;
                        break;

                    case 7: // WSW
                        x = (int)center.X - row;
                        if (!WorldGen.InWorld(x, (int)center.Y)) return;
                        y = (int)center.Y + (int)(startingAngle * row);
                        while (GetSlope(new Vector2(x, y), center, true) <= endAngle)
                        {
                            if (WorldGen.InWorld(x, y) && Vector2.Distance(new Vector2(x, y), center) <= visionRange)
                            {
                                setPixel(x, y);
                                if(wha)
                                setPixel(x, y + 1);
                                if (isTileSolid(x, y))
                                {
                                    if (WorldGen.InWorld(x, y + 1) && !isTileSolid(x, y + 1))
                                        scan(quad, startingAngle, GetSlope(new Vector2(x + 0.5f, y + 0.5f), center, true), center, row + 1);
                                }
                                else
                                {
                                    if (WorldGen.InWorld(x, y + 1) && isTileSolid(x, y + 1))
                                        startingAngle = -GetSlope(new Vector2(x - 0.5f, y + 0.5f), center, true);
                                    //setPixel(x, y);
                                }
                            }
                            y--;
                        }
                        y++;
                        break;

                    case 8: // WNW
                        x = (int)center.X - row;
                        if (!WorldGen.InWorld(x, (int)center.Y)) return;
                        y = (int)center.Y - (int)(startingAngle * row);
                        while (GetSlope(new Vector2(x, y), center, true) >= endAngle)
                        {
                            if (WorldGen.InWorld(x, y) && Vector2.Distance(new Vector2(x, y), center) <= visionRange)
                            {
                                setPixel(x, y);
                                if(wha)
                                setPixel(x, y - 1);
                                if (isTileSolid(x, y))
                                {
                                    if (WorldGen.InWorld(x, y - 1) && !isTileSolid(x, y - 1))
                                        scan(quad, startingAngle, GetSlope(new Vector2(x + 0.5f, y - 0.5f), center, true), center, row + 1);
                                }
                                else
                                {
                                    if (WorldGen.InWorld(x, y - 1) && isTileSolid(x, y - 1))
                                        startingAngle = GetSlope(new Vector2(x - 0.5f, y - 0.5f), center, true);
                                    
                                }
                            }
                            y++;
                        }
                        y--;
                        break;
                }
                //OUTSIDE SWITCH
                if (x < 0) x = 0;
                else if (x >= Main.maxTilesX) x = Main.maxTilesX-1;

                if (y < 0) y = 0;
                else if (y >= Main.maxTilesY) y = Main.maxTilesY-1;

                if (row < visionRange && !isTileSolid(x, y)) scan(quad, startingAngle, endAngle, center, row + 1);
                //for (int col = 0; col <= row; col++)
                //{    

                //    var (worldX, worldY) = (center.X - col, center.Y - row);

                //    Tile tile = Main.tile[(int)worldX, (int)worldY];


                //    bool isSolid = tile.HasTile && Main.tileSolid[tile.TileType];

                //    if (!isSolid) _maskData.SetValue(Color.Black, (_maskData.Length / 2) - col - (row) * width);
                //    //_maskData.SetValue(Color.Black, (_maskData.Length / 2) - col - (row) * width);



                //}
                //if(row < radius && !wasPrevSolid) scan(quad, startingAngle, endAngle, center, row + 1);
            }
             Vector2 centerInTiles = (Main.LocalPlayer.Center / (16f));
            for (int quad = 1; quad <= 8; quad++)
                scan(quad, 1, 0, new Vector2(MathF.Round(centerInTiles.X), MathF.Round(centerInTiles.Y)));


            Vector2 playerScreenPos = Main.LocalPlayer.Center - Main.screenPosition;
            if(DEBUG)
            for (int xOff = 0; xOff < 16; xOff++)
                for (int yOff = 0; yOff < 16; yOff++)
                    _maskData[(int)playerScreenPos.X - 8 + xOff + (int)(playerScreenPos.Y - 8 + yOff) * width] = Color.Green;
            //_maskData.SetValue(Color.Green, _maskData.Length / 2);
            setPixel((int)(Main.LocalPlayer.Center.X / 16f), (int)(Main.LocalPlayer.Center.Y / 16f));
            setPixel((int)(Main.LocalPlayer.Center.X / 16f), (int)(Main.LocalPlayer.Center.Y / 16f)+1);
            setPixel((int)(Main.LocalPlayer.Center.X / 16f)+1, (int)(Main.LocalPlayer.Center.Y / 16f));
            setPixel((int)(Main.LocalPlayer.Center.X / 16f)+1, (int)(Main.LocalPlayer.Center.Y / 16f)+1);
            TileMask.SetData(_maskData);
            Filters.Scene["FOW:FOW"].GetShader().UseImage(TileMask);
            Filters.Scene["FOW:FOW"].GetShader().UseImage(Main.instance.tileTarget,1);

            //Filters.Scene["FOW:FOW"].GetShader().Shader.Parameters["uMaskSize"].SetValue(new Vector2(TileMask.Width, TileMask.Height));
            //Filters.Scene["FOW:FOW"].GetShader/**/().Shader.Parameters["BlackOut"].SetValue(true);
        }

        private void UpdateMaskV12()
        {
            Vector2 tileTargetOffset = (Main.screenPosition - Main.sceneTilePos);

            float uvOffsetX = tileTargetOffset.X / Main.instance.tileTarget.Width;
            float uvOffsetY = tileTargetOffset.Y / Main.instance.tileTarget.Height;

            Filters.Scene["FOW:FOW"].GetShader().Shader.Parameters["TileOffset"].SetValue(new Vector2(uvOffsetX, uvOffsetY));

            float scaleX = (float)Main.screenWidth  / Main.instance.tileTarget.Width;
            float scaleY = (float)Main.screenHeight / Main.instance.tileTarget.Height;
            Filters.Scene["FOW:FOW"].GetShader().Shader.Parameters["TileScale"].SetValue(new Vector2(scaleX, scaleY));
            Vector2 playerScreenPos = Main.LocalPlayer.Center - Main.screenPosition;
            shaderRef.Shader.Parameters["PlayerScreenPos"].SetValue(playerScreenPos);

            Mod.Logger.InfoFormat("{0} {1} {2} {3}", playerScreenPos, Main.instance.tileTarget.Size());
            shaderRef.UseImage(Main.instance.tileTarget);
        }
    }
}

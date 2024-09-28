using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using System;
using Terraria.UI;
using System.Collections.Generic;

namespace NahIdWin.SatoruGojo.Projectiles
{
    public class BlueProjectile : ModProjectile
    {
        #region Member Variables

        private bool _isCharging = true;
        private float _chargeTime = 60f;
        private int _manaCostPerSecond = 5;
        private Vector2 _initialDirection;
        private bool _followingCursor = false;

        private float _maxSpeed = 24f; // Set a cap for the speed
        private float _accelerationRate = 1.02f; // Rate at which the projectile accelerates
        private float _turnRate = 0.05f; // How quickly the projectile turns towards its target

        // Define breakable tile types once
        private static readonly HashSet<int> _breakableTileIDs = new HashSet<int>
        {
            TileID.Plants, TileID.Cobweb, TileID.Plants2, TileID.Vines, TileID.LilyPad, TileID.Pots,
            TileID.BloomingHerbs, TileID.ImmatureHerbs, TileID.MatureHerbs, TileID.MushroomPlants,
            TileID.MushroomVines, TileID.JunglePlants, TileID.JunglePlants2, TileID.GlowTulip,
            TileID.CorruptPlants, TileID.CorruptVines, TileID.JungleVines, TileID.JungleThorns,
            TileID.CorruptThorns, TileID.CrimsonPlants, TileID.Coral, TileID.DyePlants,
            TileID.CrimsonThorns, TileID.CrimsonVines, TileID.JungleVines, TileID.JungleThorns,
            TileID.HallowedPlants, TileID.HallowedPlants2, TileID.HallowedVines,
            TileID.AshPlants, TileID.AshVines
        };

        #endregion

        #region Overridden Properties

        public override string Texture => _isCharging ? "NahIdWin/SatoruGojo/Textures/BlueChargingTexture" : "Terraria/Images/Projectile_" + ProjectileID.MagicMissile;

        #endregion

        #region Overridden Methods

        public override void SetDefaults()
        {
            Projectile.width = 24;
            Projectile.height = 24;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 3600;
            Projectile.light = 0.5f;
            Main.projFrames[Projectile.type] = 12; // Set to the number of frames in the charging texture
        }

        public override void AI()
        {
            Player player = Main.player[Projectile.owner];

            // Visual effects during charging
            if (_isCharging)
            {
                int dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.BlueCrystalShard);
                Main.dust[dust].velocity *= 0.3f;
                Main.dust[dust].scale = 1.5f;
                Main.dust[dust].noGravity = true;
            }

            // Charging logic
            if (_isCharging)
            {
                int frameDuration = 5; // Change this value to control the speed of the animation
                Projectile.frameCounter++;
                if (Projectile.frameCounter >= frameDuration)
                {
                    Projectile.frameCounter = 0;
                    Projectile.frame++;
                    if (Projectile.frame >= 12) // Total number of frames
                    {
                        Projectile.frame = 11; // Stay on the last frame of the charging animation
                    }
                }

                Projectile.Center = player.Center + new Vector2(20 * player.direction, -10);
                Projectile.velocity = Vector2.Zero;
                _chargeTime--;

                if (player.channel)
                {
                    _followingCursor = true;
                }

                if (_chargeTime <= 0)
                {
                    _isCharging = false;
                    _initialDirection = (Main.MouseWorld - Projectile.Center).SafeNormalize(Vector2.UnitX) * 10f;
                    Projectile.velocity = _initialDirection;

                    if (_followingCursor)
                    {
                        Projectile.tileCollide = false;
                    }
                }
            }
            else
            {
                // Logic for following the cursor
                if (_followingCursor && player.channel)
                {
                    Vector2 cursorPosition = Main.MouseWorld;
                    Vector2 targetDirection = (cursorPosition - Projectile.Center).SafeNormalize(Vector2.Zero);

                    // Gradually adjust velocity towards target direction
                    Projectile.velocity = Vector2.Lerp(Projectile.velocity, targetDirection * _maxSpeed, _turnRate);

                    // Apply acceleration
                    if (Projectile.velocity.Length() < _maxSpeed)
                    {
                        Projectile.velocity *= _accelerationRate;
                    }
                    else
                    {
                        // Cap the speed to maxSpeed
                        Projectile.velocity = Vector2.Normalize(Projectile.velocity) * _maxSpeed;
                    }

                    // Consume mana over time for following
                    if (player.statMana > _manaCostPerSecond / 60f)
                    {
                        if (Main.GameUpdateCount % 60 == 0)
                        {
                            player.statMana -= _manaCostPerSecond;
                        }
                    }
                    else
                    {
                        _followingCursor = false;
                    }
                }
                else
                {
                    _followingCursor = false;

                    // Apply acceleration for released projectile
                    if (Projectile.velocity.Length() < _maxSpeed)
                    {
                        Projectile.velocity *= _accelerationRate;
                    }
                    else
                    {
                        // Cap the speed to maxSpeed
                        Projectile.velocity = Vector2.Normalize(Projectile.velocity) * _maxSpeed;
                    }
                }

                // Pull enemies towards the projectile
                foreach (NPC npc in Main.npc)
                {
                    if (npc.active && !npc.friendly && !npc.dontTakeDamage)
                    {
                        float distance = Vector2.Distance(Projectile.Center, npc.Center);

                        // Only pull enemies within a certain range
                        if (distance < 300f)
                        {
                            Vector2 pullDirection = (Projectile.Center - npc.Center).SafeNormalize(Vector2.Zero);
                            float pullStrength = (300f - distance) / 300f; // Strengthen as distance decreases
                            npc.velocity += pullDirection * pullStrength * 2f; // Adjust force as needed
                        }
                    }
                }

                // Destroy tiles within suction range
                int suctionRadius = 10; // Suction radius in tiles
                int projTileX = (int)(Projectile.Center.X / 16f);
                int projTileY = (int)(Projectile.Center.Y / 16f);

                for (int x = projTileX - suctionRadius; x <= projTileX + suctionRadius; x++)
                {
                    for (int y = projTileY - suctionRadius; y <= projTileY + suctionRadius; y++)
                    {
                        Tile tile = Framing.GetTileSafely(x, y);
                        float distance = Vector2.Distance(new Vector2(x * 16, y * 16), Projectile.Center);

                        // Destroy certain tiles like grass, vines, and cobwebs if within suction range
                        if (distance < 300f && _breakableTileIDs.Contains(tile.TileType))
                        {
                            WorldGen.KillTile(x, y);
                        }
                    }
                }

                // Create a visual "wind" effect for tiles being pulled in
                int windRadius = suctionRadius + 5; // Extend effect radius beyond suction
                for (int x = projTileX - windRadius; x <= projTileX + windRadius; x++)
                {
                    for (int y = projTileY - windRadius; y <= projTileY + windRadius; y++)
                    {
                        Tile tile = Framing.GetTileSafely(x, y);
                        float distance = Vector2.Distance(new Vector2(x * 16, y * 16), Projectile.Center);

                        // If tile is within wind range and is a destructible type
                        if (distance < 480f && _breakableTileIDs.Contains(tile.TileType))
                        {
                            // Create a dust effect to represent wind or suction
                            Vector2 tilePosition = new Vector2(x * 16, y * 16);
                            Vector2 pullDirection = (Projectile.Center - tilePosition).SafeNormalize(Vector2.Zero);
                            int dust = Dust.NewDust(tilePosition, 16, 16, DustID.BlueTorch, 0, 0, 150, default, 1.5f);
                            Main.dust[dust].velocity = pullDirection * 2f; // Move dust towards projectile
                            Main.dust[dust].noGravity = true; // Make dust float
                        }
                    }
                }

                // Visual trail while traveling
                int trailDust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.BlueTorch);
                Main.dust[trailDust].velocity *= 0.5f;
                Main.dust[trailDust].scale = 1.8f;
                Main.dust[trailDust].noGravity = true;
            }
        }

        #endregion
    }
}

using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using System;
using Terraria.UI;

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
        private float _turnRate = 0.1f; // How quickly the projectile turns towards its target

        #endregion

        #region Overridden Properties

        public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.MagicMissile; // Use Magic Missile texture as placeholder

        #endregion

        #region Overridden Methods

        public override void SetDefaults()
        {
            Projectile.width = 24;
            Projectile.height = 24;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.tileCollide = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 3600;
            Projectile.light = 0.5f;
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

                    // Calculate intended velocity
                    Vector2 intendedVelocity = targetDirection * _maxSpeed;

                    // Check if the intended position collides with a tile
                    Vector2 nextPosition = Projectile.position + intendedVelocity;
                    if (Collision.SolidCollision(nextPosition, Projectile.width, Projectile.height))
                    {
                        // Adjust intendedVelocity to slide along the collision
                        intendedVelocity = AdjustVelocityForTileCollision(intendedVelocity);
                    }

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
                    Projectile.tileCollide = true;

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
                        if (distance < 300f && (tile.TileType == TileID.Plants || tile.TileType == TileID.Cobweb))
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
                        if (distance < 480f && (tile.TileType == TileID.Plants || tile.TileType == TileID.Cobweb))
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

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            // If the projectile is following the cursor, prevent it from disappearing
            if (_followingCursor)
            {
                // Stop movement but do not destroy
                Projectile.velocity = Vector2.Zero;
                return false; // Returning false prevents projectile from being killed
            }

            // If not following, allow normal collision behavior
            return true;
        }

        #endregion

        #region Private Helper Methods

        private Vector2 AdjustVelocityForTileCollision(Vector2 velocity)
        {
            // Check horizontal and vertical collisions separately to determine sliding behavior
            Vector2 horizontalVelocity = new Vector2(velocity.X, 0);
            Vector2 verticalVelocity = new Vector2(0, velocity.Y);

            // If horizontal movement would collide, zero out X velocity
            if (Collision.SolidCollision(Projectile.position + horizontalVelocity, Projectile.width, Projectile.height))
            {
                velocity.X = 0;
            }

            // If vertical movement would collide, zero out Y velocity
            if (Collision.SolidCollision(Projectile.position + verticalVelocity, Projectile.width, Projectile.height))
            {
                velocity.Y = 0;
            }

            return velocity;
        }

        #endregion
    }
}

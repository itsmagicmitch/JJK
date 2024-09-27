using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace NahIdWin.SatoruGojo.Projectiles
{
    public class BlueProjectile : ModProjectile
    {
        private bool _isCharging = true; // Track if projectile is still charging
        private float _chargeTime = 60f; // Duration of the charge in ticks (1 second = 60 ticks)
        private int _manaCostPerSecond = 5; // Mana cost per second for following the cursor
        private Vector2 _initialDirection; // The direction to shoot after charging
        private bool _followingCursor = false; // Track if the projectile is following the cursor

        public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.MagicMissile; // Use Magic Missile texture as placeholder

        public override void SetDefaults()
        {
            Projectile.width = 24; // Projectile hitbox size
            Projectile.height = 24;
            Projectile.friendly = true; // Does not hurt players
            Projectile.DamageType = DamageClass.Magic;
            Projectile.tileCollide = true; // Collides with tiles initially
            Projectile.penetrate = -1; // Infinite penetration
            Projectile.timeLeft = 3600; // How long the projectile exists (1 minute)
            Projectile.light = 0.5f; // Add light emission for visual effect
        }

        public override void AI()
        {
            Player player = Main.player[Projectile.owner];

            // Visual effects
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
                // Following cursor logic
                if (_followingCursor && player.channel)
                {
                    Vector2 cursorPosition = Main.MouseWorld;
                    Vector2 direction = (cursorPosition - Projectile.Center).SafeNormalize(Vector2.UnitX);
                    Projectile.velocity = direction * 10f;

                    // Consume mana over time
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

                    // Increase speed after being released
                    Projectile.velocity *= 1.03f; // Speed up over time after release
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
                            float pullStrength = (300f - distance) / 300f; // Strengthen as the distance decreases
                            npc.velocity += pullDirection * pullStrength * 2f; // Adjust force as needed
                        }
                    }
                }

                // Destroy small tiles in the area of effect
                int radius = 2; // Adjust the AOE radius as necessary
                int projTileX = (int)(Projectile.position.X / 16f);
                int projTileY = (int)(Projectile.position.Y / 16f);

                for (int x = projTileX - radius; x <= projTileX + radius; x++)
                {
                    for (int y = projTileY - radius; y <= projTileY + radius; y++)
                    {
                        Tile tile = Framing.GetTileSafely(x, y);

                        // Destroy certain tiles like grass, vines, and cobwebs
                        if (tile.TileType == TileID.Plants || tile.TileType == TileID.Cobweb)
                        {
                            WorldGen.KillTile(x, y);
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
            // When colliding with tiles, stop movement
            Projectile.Kill(); // Destroy the projectile or add bounce logic here
            return false;
        }
    }
}

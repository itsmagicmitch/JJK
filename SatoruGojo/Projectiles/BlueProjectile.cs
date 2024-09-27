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



            // If charging, keep the projectile near the player's hand
            if (_isCharging)
            {
                // Add a charging visual effect while projectile is charging
                int dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.BlueCrystalShard);
                Main.dust[dust].velocity *= 0.3f; // Adjust speed of the dust
                Main.dust[dust].scale = 1.5f; // Make dust particles larger
                Main.dust[dust].noGravity = true; // Make dust float

                // Position projectile near player's hand
                Projectile.Center = player.Center + new Vector2(20 * player.direction, -10); // Adjust as necessary
                Projectile.velocity = Vector2.Zero;

                // Reduce charge time
                _chargeTime--;

                // Start following cursor if player is holding the attack button (channeling)
                if (player.channel)
                {
                    _followingCursor = true;
                }

                // When charging is complete
                if (_chargeTime <= 0)
                {
                    _isCharging = false;
                    // Set the initial direction based on cursor
                    _initialDirection = (Main.MouseWorld - Projectile.Center).SafeNormalize(Vector2.UnitX) * 10f; // Adjust speed as needed
                    Projectile.velocity = _initialDirection;

                    // If channeling, continue to follow cursor
                    if (_followingCursor)
                    {
                        Projectile.tileCollide = false; // Ignore tiles while following cursor
                    }
                }
            }
            else
            {
                // Projectile is either launched or following cursor
                if (_followingCursor && player.channel)
                {
                    // Continue to follow cursor
                    Vector2 cursorPosition = Main.MouseWorld;
                    Vector2 direction = (cursorPosition - Projectile.Center).SafeNormalize(Vector2.UnitX);
                    Projectile.velocity = direction * 10f; // Adjust speed as necessary

                    // Consume mana over time
                    if (player.statMana > _manaCostPerSecond / 60f)
                    {
                        if (Main.GameUpdateCount % 60 == 0) // Every second
                        {
                            player.statMana -= _manaCostPerSecond;
                        }
                    }
                    else
                    {
                        // Stop following if not enough mana
                        _followingCursor = false;
                    }
                }
                else
                {
                    // Launch in the current direction if the player releases the button or runs out of mana
                    _followingCursor = false;
                    Projectile.tileCollide = true; // Re-enable tile collision
                }

                // Visuals for when Blue is actively following cursor
                int trailDust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.BlueTorch);
                Main.dust[trailDust].velocity *= 0.5f; // Slower dust trail
                Main.dust[trailDust].scale = 1.8f; // Larger dust particles
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

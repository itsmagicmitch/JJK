using NahIdWin.SatoruGojo.Projectiles;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace NahIdWin.SatoruGojo.Items
{
    public class Blue : ModItem
    {
        public override string Texture => "Terraria/Images/Item_" + ItemID.MagicMissile; // Example using Magic Missile texture

        public override void SetStaticDefaults()
        {
            Item.SetNameOverride("Cursed Technique: Blue");
        }

        public override void SetDefaults()
        {
            // Use existing Magic Missile item sprite
            Item.CloneDefaults(ItemID.MagicMissile);
            Item.damage = 15; // Set base damage
            Item.mana = 10; // Set mana cost
            Item.shoot = ModContent.ProjectileType<BlueProjectile>(); // Use the Blue projectile class
            Item.shootSpeed = 8f; // Adjust as needed
            Item.UseSound = SoundID.Item20; // A suitable magic use sound
        }

        public override bool CanUseItem(Player player)
        {
            // Check if player has enough mana or any other condition you need
            return base.CanUseItem(player);
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            // Add or modify tooltips dynamically
            tooltips.Add(new TooltipLine(Mod, "CustomTooltip", "Gojo's basic Cursed Technique - attracts entities towards a point."));
        }
    }
}

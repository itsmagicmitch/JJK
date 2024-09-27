using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.ModLoader;

namespace NahIdWin.SatoruGojo.Items
{
    public class Blue : ModItem
    {
        public override void SetStaticDefaults()
        {
            Item.SetNameOverride("Cursed Technique: Blue");
        }

        public override void SetDefaults()
        {
            // Set up item stats, usage, etc.
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            // Add or modify tooltips dynamically
            tooltips.Add(new TooltipLine(Mod, "CustomTooltip", "Gojo's basic Cursed Technique - attracts entities towards a point."));
        }
    }
}

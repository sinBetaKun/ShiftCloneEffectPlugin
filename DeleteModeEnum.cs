using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShiftCloneEffectPlugin
{
    public enum DeleteModeEnum
    {
        [Display(Name = "順方向", Description = "表示と同じ順序で消去")]
        Straight = 0,
        [Display(Name = "逆方向", Description = "表示と逆の順序で消去")]
        Back = 1
    }
}

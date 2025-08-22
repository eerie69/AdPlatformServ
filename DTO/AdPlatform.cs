using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AdPlatformServ.DTO
{
    public class AdPlatform
    {
        public string Name { get; set; } = string.Empty;
        public List<string> Locations { get; set; } = new();
    }
}
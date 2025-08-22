using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AdPlatformServ.DTO
{
    public class SearchResponse
    {
        public string Location { get; set; } = string.Empty;
        public List<string> Platforms { get; set; } = new();
    }
}
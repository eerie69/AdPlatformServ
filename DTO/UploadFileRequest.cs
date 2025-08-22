using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AdPlatformServ.DTO
{
    public class UploadFileRequest
    {
        public IFormFile File { get; set; } = null!;
    }
}
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Text;

namespace Service.DTOs.ImageAnalysisDTOs
{
    public class AnalyzeFormRequestDto
    {
        public IFormFile? File { get; set; }
        public string? Prompt { get; set; }
    }
}
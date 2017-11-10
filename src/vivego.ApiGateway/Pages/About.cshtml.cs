﻿using Microsoft.AspNetCore.Mvc.RazorPages;

namespace vivego.ApiGateway.Pages
{
    public class AboutModel : PageModel
    {
        public string Message { get; set; }

        public void OnGet()
        {
            Message = "Your application description page.";
        }
    }
}

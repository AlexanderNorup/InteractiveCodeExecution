using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace InteractiveCodeExecution.Pages
{
    public class LegacyModel : PageModel
    {
        private readonly ILogger<LegacyModel> _logger;

        public LegacyModel(ILogger<LegacyModel> logger)
        {
            _logger = logger;
        }

        public void OnGet()
        {

        }
    }
}

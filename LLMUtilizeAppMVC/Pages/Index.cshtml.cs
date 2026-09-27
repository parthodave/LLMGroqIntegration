using LLMUtilizeAppMVC.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace LLMUtilizeAppMVC.Pages
{
    public class IndexModel : PageModel
    {
        private readonly GroqLLMService _llmService;

        public IndexModel(GroqLLMService llmService)
        {
            _llmService = llmService;
        }

        [BindProperty]
        public string? Question { get; set; }

        public LLMResponse? Response { get; set; }

        public void OnGet()
        {
            // Initial page load - nothing to do
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (string.IsNullOrWhiteSpace(Question))
            {
                Response = new LLMResponse
                {
                    Success = false,
                    ErrorMessage = "Please enter a question."
                };
                return Page();
            }

            Response = await _llmService.AskQuestionAsync(Question);
            return Page();
        }
    }
}

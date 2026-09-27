using System.ComponentModel.DataAnnotations;
using LLMUtilizeAppMVC.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using UglyToad.PdfPig;

namespace LLMUtilizeAppMVC.Pages;

public class PdfChatModel : PageModel
{
    private readonly GroqLLMService _llmService;
    private readonly ILogger<PdfChatModel> _logger;

    private const long MaximumFileSize = 10 * 1024 * 1024;

    public PdfChatModel(
        GroqLLMService llmService,
        ILogger<PdfChatModel> logger)
    {
        _llmService = llmService;
        _logger = logger;
    }

    [BindProperty]
    [Required(ErrorMessage = "Please select a PDF file.")]
    public IFormFile? PdfFile { get; set; }

    [BindProperty]
    [Required(ErrorMessage = "Please enter a question.")]
    [StringLength(
        1000,
        MinimumLength = 2,
        ErrorMessage = "The question must contain between 2 and 1000 characters.")]
    public string? Question { get; set; }

    public LLMResponse? Response { get; private set; }

    public string? UploadedFileName { get; private set; }

    public async Task<IActionResult> OnPostAsync()
    {
        UploadedFileName = PdfFile?.FileName;

        if (!ModelState.IsValid)
        {
            Response = new LLMResponse
            {
                Success = false,
                ErrorMessage = "Please select a PDF and enter a question."
            };

            return Page();
        }

        if (PdfFile is null || PdfFile.Length == 0)
        {
            Response = new LLMResponse
            {
                Success = false,
                ErrorMessage = "The selected file is empty."
            };

            return Page();
        }

        if (PdfFile.Length > MaximumFileSize)
        {
            Response = new LLMResponse
            {
                Success = false,
                ErrorMessage = "The PDF must be 10 MB or smaller."
            };

            return Page();
        }

        if (!string.Equals(
                Path.GetExtension(PdfFile.FileName),
                ".pdf",
                StringComparison.OrdinalIgnoreCase))
        {
            Response = new LLMResponse
            {
                Success = false,
                ErrorMessage = "Only PDF files are supported."
            };

            return Page();
        }

        try
        {
            await using var uploadedStream = PdfFile.OpenReadStream();
            using var pdf = PdfDocument.Open(uploadedStream);

            var extractedText = string.Join(
                Environment.NewLine + Environment.NewLine,
                pdf.GetPages().Select(page => page.Text));

            if (string.IsNullOrWhiteSpace(extractedText))
            {
                Response = new LLMResponse
                {
                    Success = false,
                    ErrorMessage =
                        "No selectable text was found. This may be a scanned PDF. " +
                        "OCR is required for scanned documents."
                };

                return Page();
            }

            Response = await _llmService.AskQuestionAboutPdfAsync(
                Question!,
                extractedText);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error while processing uploaded PDF {FileName}",
                PdfFile.FileName);

            Response = new LLMResponse
            {
                Success = false,
                ErrorMessage =
                    "The PDF could not be read. Verify that it is a valid PDF file."
            };
        }

        return Page();
    }
}

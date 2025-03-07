namespace WalletNet.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.IO;
using System.Threading.Tasks;
using Tesseract;
using System.Diagnostics;
using System.Drawing;
using SkiaSharp;

[Route("api/ocr")]
[ApiController]
public class OcrController : ControllerBase
{

    public OcrController()
    {

    }
    private readonly string _tessDataPath = "/opt/homebrew/Cellar/tesseract/5.5.0/share/tessdata";
    private readonly string _tesseractPath = "/opt/homebrew/bin/tesseract";
    [HttpPost]
    public async Task<IActionResult> ExtractText(IFormFile image)
    {
        if (image == null || image.Length == 0)
        {
            return BadRequest("No file uploaded.");
        }

        // Save file temporarily
        var tempFilePath = Path.GetTempFileName();

        using (var stream = new FileStream(tempFilePath, FileMode.Create))
        {
            await image.CopyToAsync(stream);
        }


        // Perform OCR
        string extractedText = RunTesseract(tempFilePath);

        // Cleanup temporary file
        System.IO.File.Delete(tempFilePath);

        return Ok(new { text = extractedText.Trim() });
        // }
        // catch (Exception ex)
        // {
        //     return StatusCode(500, new { error = "Error processing image", details = ex.Message });
        // }
    }
    [HttpPost("crop")]
    public async Task<IActionResult> CropReceipt(IFormFile image)
    {
        if (image == null || image.Length == 0)
        {
            return BadRequest("No file uploaded.");
        }


        // Save file temporarily
        var tempFilePath = Path.GetTempFileName();

        using (var stream = new FileStream(tempFilePath, FileMode.Create))
        {
            await image.CopyToAsync(stream);
        }

        DocumentScannerService scanner = new DocumentScannerService();
        ReceiptDetectionService receipt = new ReceiptDetectionService();

        string outputImagePath = Path.Combine(Directory.GetCurrentDirectory(), "output", "cropped_" + image.FileName);
        string outputImagePath1 = Path.Combine(Directory.GetCurrentDirectory(), "output", "cropped1_" + image.FileName);

        using (var stream = new FileStream(tempFilePath, FileMode.Create))
        {
            await image.CopyToAsync(stream);
        }

        // Provide the image path to crop the receipt
        // SKBitmap croppedImage = _receiptCropService.CropReceipt(tempFilePath, outputImagePath);
        bool success = scanner.CropDocument(tempFilePath, outputImagePath1);

        await receipt.DetectAndCropReceiptAsync(tempFilePath, outputImagePath);

        return Ok(new { message = "Receipt cropped successfully.", outputPath = outputImagePath, success = success });
    }
    private string RunTesseract(string imagePath)
    {
        // Set up the process start info
        ProcessStartInfo startInfo = new ProcessStartInfo()
        {
            FileName = _tesseractPath,
            Arguments = $"\"{imagePath}\" stdout -l eng",  // Use stdout to capture the output
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        // Start the process and read the output
        using (Process process = Process.Start(startInfo))
        using (StreamReader reader = process.StandardOutput)
        {
            string result = reader.ReadToEnd();  // Capture the text output
            process.WaitForExit();
            return result.Trim();  // Return the trimmed output
        }
    }
}

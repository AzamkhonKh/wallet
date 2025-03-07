namespace WalletNet.Controllers;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using OpenCvSharp;
using OpenCvSharp.Dnn;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

[ApiController]
public class ReceiptController : ControllerBase
{
    private readonly ILogger<ReceiptController> _logger;
    private readonly string _modelPath;
    private readonly string _outputDirectory;
    private readonly string[] _classNames = new string[]
    {
        "Address", "Date", "Item", "OrderId", "Subtotal", "Tax", "Title", "TotalPrice"
    };

    public ReceiptController(ILogger<ReceiptController> logger, IConfiguration configuration)
    {
        _logger = logger;
        _modelPath = configuration["YoloSettings:ModelPath"] ?? "model/receipt_model.onnx";
        _outputDirectory = configuration["AppSettings:OutputDirectory"] ?? "output";

        if (!Directory.Exists(_outputDirectory))
        {
            Directory.CreateDirectory(_outputDirectory);
        }
    }

    [HttpPost("process")]
    public async Task<IActionResult> ProcessReceipt(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest("No file uploaded");
        }

        try
        {
            // Create a unique directory for this receipt
            string receiptId = Guid.NewGuid().ToString();
            string receiptDirectory = Path.Combine(_outputDirectory, receiptId);
            Directory.CreateDirectory(receiptDirectory);

            // Save the uploaded file
            string filePath = Path.Combine(receiptDirectory, file.FileName);
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // Process the receipt and get the label-to-image map
            var labelToImageMap = ProcessReceiptImage(filePath, receiptDirectory);

            return Ok(labelToImageMap);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing receipt");
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }

    private Dictionary<string, string> ProcessReceiptImage(string imagePath, string outputDirectory)
    {
        // Load the image
        using var image = Cv2.ImRead(imagePath);
        if (image.Empty())
        {
            throw new Exception("Could not read the image");
        }

        // Load the YOLO model
        using var net = CvDnn.ReadNetFromOnnx(_modelPath);

        // Prepare image for network
        using var inputBlob = CvDnn.BlobFromImage(
            image,
            1 / 255.0,
            new Size(640, 640),
            new Scalar(0, 0, 0),
            swapRB: true,
            crop: false);

        var result = new Dictionary<string, string>();
        // Set input to the network
        if (net != null)
        {
            net.SetInput(inputBlob);
            using var output = net.Forward();
            // Run forward pass to get output

            // Process YOLO output and extract bounding boxes

            // YOLOv8 output format: [xywh, confidence, class1, class2, ...]
            // Format is different than YOLOv5, v8 returns a tensor of shape [batch, num_boxes, num_classes+4]
            int rows = output.Size(1);
            int dimensions = output.Size(2);


            // Assuming output shape is [1, num_boxes, num_classes+4]
            for (int i = 0; i < rows; i++)
            {
                float[] rowData = new float[dimensions];
                for (int j = 0; j < dimensions; j++)
                {
                    rowData[j] = output.At<float>(0, i, j);
                }

                // First 4 values are x, y, w, h
                float x = rowData[0];
                float y = rowData[1];
                float width = rowData[2];
                float height = rowData[3];

                // Find the class with highest confidence
                int classId = -1;
                float maxConfidence = 0;
                for (int j = 4; j < dimensions; j++)
                {
                    if (rowData[j] > maxConfidence)
                    {
                        maxConfidence = rowData[j];
                        classId = j - 4;
                    }
                }

                // Set confidence threshold
                if (maxConfidence > 0.25 && classId >= 0 && classId < _classNames.Length)
                {
                    // Convert normalized coordinates to actual pixels
                    // YOLOv8 gives center, width, height - convert to topleft, bottomright
                    int centerX = (int)(x * image.Width);
                    int centerY = (int)(y * image.Height);
                    int rectWidth = (int)(width * image.Width);
                    int rectHeight = (int)(height * image.Height);

                    int left = Math.Max(0, centerX - rectWidth / 2);
                    int top = Math.Max(0, centerY - rectHeight / 2);
                    int right = Math.Min(image.Width, centerX + rectWidth / 2);
                    int bottom = Math.Min(image.Height, centerY + rectHeight / 2);

                    // Create a rectangle
                    var rect = new Rect(left, top, right - left, bottom - top);

                    // Crop the ROI
                    using var croppedImage = new Mat(image, rect);

                    // Save the cropped image
                    string className = _classNames[classId];
                    string outputFileName = $"{className}_{i}.jpg";
                    string outputPath = Path.Combine(outputDirectory, outputFileName);
                    Cv2.ImWrite(outputPath, croppedImage);

                    // Add to result dictionary (using the relative path for API response)
                    string relativePath = Path.Combine(Path.GetFileName(outputDirectory), outputFileName);
                    result[className] = relativePath.Replace("\\", "/");
                }
            }
        }

        return result;
    }
}
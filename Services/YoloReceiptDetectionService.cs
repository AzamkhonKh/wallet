namespace WalletNet.Services;

using System;
using System.IO;
using System.Threading.Tasks;
using OpenCvSharp;
using OpenCvSharp.Dnn;

/// <summary>
/// Extension class to add YOLO-based receipt detection
/// </summary>
public class YoloReceiptDetectionService
{
    private readonly Net _net;
    private readonly string[] _classNames;
    private readonly ReceiptDetectionService _fallbackService;
    private const float ConfidenceThreshold = 0.5f;
    private const float NmsThreshold = 0.4f;

    public YoloReceiptDetectionService(string modelPath, string configPath, string classesPath)
    {
        // Initialize fallback service for edge cases
        _fallbackService = new ReceiptDetectionService();

        // Load class names
        _classNames = File.ReadAllLines(classesPath);

        // Initialize YOLO network
        _net = CvDnn.ReadNetFromDarknet(configPath, modelPath);
        _net.SetPreferableBackend(Backend.CUDA);
        _net.SetPreferableTarget(Target.CUDA);
    }

    public async Task<bool> DetectAndCropReceiptAsync(string imagePath, string outputPath)
    {
        if (!File.Exists(imagePath))
            throw new FileNotFoundException("Input image not found", imagePath);

        // Load image
        using var image = Cv2.ImRead(imagePath);
        if (image.Empty())
            throw new Exception("Failed to load image");

        var result = await DetectWithYolo(image);

        if (result.Success)
        {
            // Save the cropped receipt
            Cv2.ImWrite(outputPath, result.CroppedImage);
            return true;
        }

        // Fall back to contour-based detection if YOLO fails
        return await _fallbackService.DetectAndCropReceiptAsync(imagePath, outputPath);
    }

    private async Task<(bool Success, Mat CroppedImage)> DetectWithYolo(Mat image)
    {
        return await Task.Run(() =>
        {
            // Convert image to blob for YOLO
            using var blob = CvDnn.BlobFromImage(image, 1 / 255.0, new Size(416, 416),
                new Scalar(0, 0, 0), true, false);

            // Set input to the network
            _net.SetInput(blob);

            // Get output layer names
            var outLayerNames = _net.GetUnconnectedOutLayersNames();

            // Forward pass
            using var outs = new VectorOfMat();
            _net.Forward(outs, outLayerNames);

            // Process outputs
            var receiptBox = ProcessYoloOutput(image, outs);

            if (receiptBox.HasValue)
            {
                var rect = receiptBox.Value;

                // Ensure the rectangle is within image bounds
                rect.X = Math.Max(0, rect.X);
                rect.Y = Math.Max(0, rect.Y);
                rect.Width = Math.Min(image.Width - rect.X, rect.Width);
                rect.Height = Math.Min(image.Height - rect.Y, rect.Height);

                // Crop the receipt
                using var cropped = new Mat(image, rect);
                return (true, cropped.Clone());
            }

            return (false, image.Clone());
        });
    }

    private Rect? ProcessYoloOutput(Mat image, VectorOfMat outs)
    {
        var classIds = new List<int>();
        var confidences = new List<float>();
        var boxes = new List<Rect>();

        // Receipt class ID (assuming "receipt" is in the classes file)
        int receiptClassId = Array.IndexOf(_classNames, "receipt");
        if (receiptClassId == -1)
            receiptClassId = Array.IndexOf(_classNames, "document"); // Fallback

        if (receiptClassId == -1)
            return null; // Neither "receipt" nor "document" in classes

        for (int i = 0; i < outs.Size; ++i)
        {
            var mat = outs[i];
            for (int j = 0; j < mat.Rows; ++j)
            {
                var row = mat.Row(j);
                var scores = row.ColRange(5, mat.Cols);
                Point classIdPoint = new Point();
                double confidence = 0;
                Cv2.MinMaxLoc(scores, out _, out confidence, out _, out classIdPoint);

                if (confidence > ConfidenceThreshold && classIdPoint.X == receiptClassId)
                {
                    int centerX = (int)(row.At<float>(0) * image.Width);
                    int centerY = (int)(row.At<float>(1) * image.Height);
                    int width = (int)(row.At<float>(2) * image.Width);
                    int height = (int)(row.At<float>(3) * image.Height);
                    int left = centerX - width / 2;
                    int top = centerY - height / 2;

                    classIds.Add(classIdPoint.X);
                    confidences.Add((float)confidence);
                    boxes.Add(new Rect(left, top, width, height));
                }
            }
        }

        if (boxes.Count == 0)
            return null;

        // Apply non-maximum suppression
        int[] indices = CvDnn.NMSBoxes(boxes, confidences, ConfidenceThreshold, NmsThreshold).ToArray();

        if (indices.Length == 0)
            return null;

        // Return the box with highest confidence
        int idx = indices[0];
        for (int i = 1; i < indices.Length; i++)
        {
            if (confidences[indices[i]] > confidences[idx])
                idx = indices[i];
        }

        return boxes[idx];
    }
}
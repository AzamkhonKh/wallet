namespace WalletNet.Services;

using System;
using OpenCvSharp;
using System.IO;
using SkiaSharp;

public class ReceiptCropService
{
    // Method to crop the receipt
    public SKBitmap CropReceipt(string imagePath, string outputPath)
    {

        // Load the image
        Mat img = Cv2.ImRead(imagePath);
        if (img.Empty())
        {
            throw new Exception("Image could not be loaded.");
        }

        // Step 1: Convert the image to grayscale
        Mat gray = new Mat();
        Cv2.CvtColor(img, gray, ColorConversionCodes.BGR2GRAY);
        // Step 2: Apply adaptive thresholding for better edge detection
        Mat thresh = new Mat();
        Cv2.AdaptiveThreshold(gray, thresh, 255, AdaptiveThresholdTypes.MeanC, ThresholdTypes.Binary, 19, 1);

        // Step 3: Apply Canny edge detection to detect boundaries
        Mat edges = new Mat();
        Cv2.Canny(thresh, edges, 100, 200);

        // Step 4: Find contours
        OpenCvSharp.Point[][] contours;
        HierarchyIndex[] hierarchy;
        Cv2.FindContours(edges, out contours, out hierarchy, RetrievalModes.External, ContourApproximationModes.ApproxSimple);

        // // Step 5: Filter contours based on area and aspect ratio
        // OpenCvSharp.Point[] receiptContour = null;
        // foreach (var contour in contours)
        // {
        //     // Filter small contours by area
        //     if (Cv2.ContourArea(contour) < 1000) continue;

        //     // Approximate the contour to a polygon
        //     var epsilon = 0.02 * Cv2.ArcLength(contour, true);
        //     var approx = Cv2.ApproxPolyDP(contour, epsilon, true);

        //     // Check if the contour is a quadrilateral (receipt shape)
        //     if (approx.Length == 4)
        //     {
        //         // Check aspect ratio to filter out irregular shapes
        //         var rect = Cv2.BoundingRect(approx);
        //         double aspectRatio = (double)rect.Width / rect.Height;

        //         if (aspectRatio >= 1.0 && aspectRatio <= 3.0) // Adjust this range as needed
        //         {
        //             // Add position filtering to exclude logos or small objects near edges
        //             if (rect.X > 50 && rect.Y > 50 && rect.X + rect.Width < img.Width - 50 && rect.Y + rect.Height < img.Height - 50)
        //             {
        //                 receiptContour = approx;
        //                 break; // We found the best match
        //             }
        //         }
        //     }
        // }

        // if (receiptContour == null)
        // {
        //     throw new Exception("No valid receipt contour found.");
        // }

        // // Step 6: Perform perspective transform to get the cropped receipt
        // var rectPoints = new OpenCvSharp.Point2f[]
        // {
        //     new OpenCvSharp.Point2f(0, 0),
        //     new OpenCvSharp.Point2f(500, 0),
        //     new OpenCvSharp.Point2f(500, 700),
        //     new OpenCvSharp.Point2f(0, 700)
        // };

        // var transformMatrix = Cv2.GetPerspectiveTransform(receiptContour.Select(p => new OpenCvSharp.Point2f(p.X, p.Y)).ToArray(), rectPoints);
        // Mat croppedReceipt = new Mat();
        // Cv2.WarpPerspective(img, croppedReceipt, transformMatrix, new OpenCvSharp.Size(500, 700));

        // Convert the cropped receipt Mat to Bitmap
        SKBitmap croppedBitmap = ConvertMatToBitmap(edges);
        Console.WriteLine(contours);
        Console.WriteLine(hierarchy);
        

        SaveImage(croppedBitmap, outputPath);
        return croppedBitmap;
    }
    // Method to convert Mat to SKBitmap
    private SKBitmap ConvertMatToBitmap(Mat mat)
    {
        // Convert Mat to Bitmap by converting to a byte array
        using (var stream = new MemoryStream())
        {
            // Encode Mat as BMP image
            Cv2.ImEncode(".bmp", mat, out byte[] byteArray);

            // Write byte array to MemoryStream
            stream.Write(byteArray, 0, byteArray.Length);

            // Create SKBitmap from the byte array
            return SKBitmap.Decode(byteArray);
        }
    }

    public String SaveImage(SKBitmap bitmap, string outputPath)
    {

        string filePath = outputPath;

        using (SKImage skImage = SKImage.FromBitmap(bitmap))
        using (SKData data = skImage.Encode(SKEncodedImageFormat.Png, 100)) // Save as PNG with max quality
        {
            using (var stream = File.OpenWrite(filePath))
            {
                data.SaveTo(stream);
            }
        }
        return filePath;
    }
}

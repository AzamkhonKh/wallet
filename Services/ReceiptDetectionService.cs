namespace WalletNet.Services;

using System;
using System.IO;
using System.Threading.Tasks;
using OpenCvSharp;
using OpenCvSharp.Dnn;

public class ReceiptDetectionService
{
    private const double ConfidenceThreshold = 0.5;
    private const double NonMaxSuppressionThreshold = 0.4;

    /// <summary>
    /// Detects and crops receipts from an image
    /// </summary>
    /// <param name="imagePath">Path to the input image</param>
    /// <param name="outputPath">Path where the cropped receipt will be saved</param>
    /// <returns>True if a receipt was detected and cropped, otherwise false</returns>
    public async Task<bool> DetectAndCropReceiptAsync(string imagePath, string outputPath)
    {
        if (!File.Exists(imagePath))
            throw new FileNotFoundException("Input image not found", imagePath);

        // Load image
        using var image = Cv2.ImRead(imagePath);
        if (image.Empty())
            throw new Exception("Failed to load image");

        var detectionResult = await Task.Run(() => DetectReceiptContours(image));

        if (detectionResult.Success)
        {
            // Save the cropped receipt
            Cv2.ImWrite(outputPath, detectionResult.CroppedImage);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Detect receipt using contour-based approach combined with edge detection
    /// </summary>
    private (bool Success, Mat CroppedImage) DetectReceiptContours(Mat image)
    {
        // Create a copy of the original image
        Mat original = image.Clone();

        // Convert to grayscale
        Mat gray = new Mat();
        Cv2.CvtColor(image, gray, ColorConversionCodes.BGR2GRAY);

        // Apply Gaussian blur to reduce noise
        Mat blurred = new Mat();
        Cv2.GaussianBlur(gray, blurred, new Size(5, 5), 0);

        // Apply Canny edge detection
        Mat edges = new Mat();
        Cv2.Canny(blurred, edges, 50, 150);

        // Dilate the edges to close gaps
        Mat dilated = new Mat();
        var kernel = Cv2.GetStructuringElement(MorphShapes.Rect, new Size(3, 3));
        Cv2.Dilate(edges, dilated, kernel);

        // Find contours
        Point[][] contours;
        HierarchyIndex[] hierarchy;
        Cv2.FindContours(dilated, out contours, out hierarchy, RetrievalModes.List, ContourApproximationModes.ApproxSimple);

        // Find the largest contour that could be a receipt
        double maxArea = 0;
        Point[] receiptContour = null;

        foreach (var contour in contours)
        {
            double area = Cv2.ContourArea(contour);
            if (area > 5000) // Minimum area threshold to filter out small contours
            {
                // Approximate the contour to simplify
                Point[] approx = Cv2.ApproxPolyDP(contour, Cv2.ArcLength(contour, true) * 0.02, true);

                // Check if the shape has 4 corners (like a receipt)
                if (approx.Length >= 4 && approx.Length <= 6 && area > maxArea)
                {
                    // Check if it's a convex shape
                    if (Cv2.IsContourConvex(approx))
                    {
                        receiptContour = approx;
                        maxArea = area;
                    }
                }
            }
        }

        // If we found a receipt contour
        if (receiptContour != null && receiptContour.Length >= 4)
        {
            // Get the four corners
            Point[] corners = GetOrderedRectangleCorners(receiptContour);

            // Perspective transform to get a top-down view of the receipt
            Mat croppedReceipt = PerspectiveTransform(original, corners);

            return (true, croppedReceipt);
        }

        // If contour-based detection failed, try threshold-based approach
        return TryThresholdBasedDetection(original, gray);
    }

    /// <summary>
    /// Alternate method using thresholding for cases where contour detection fails
    /// </summary>
    private (bool Success, Mat CroppedImage) TryThresholdBasedDetection(Mat original, Mat gray)
    {
        // Apply adaptive thresholding
        Mat thresh = new Mat();
        Cv2.AdaptiveThreshold(gray, thresh, 255, AdaptiveThresholdTypes.GaussianC, ThresholdTypes.Binary, 11, 2);

        // Invert the image
        Cv2.BitwiseNot(thresh, thresh);

        // Find contours
        Point[][] contours;
        HierarchyIndex[] hierarchy;
        Cv2.FindContours(thresh, out contours, out hierarchy, RetrievalModes.List, ContourApproximationModes.ApproxSimple);

        // Find the largest rectangle-like contour
        double maxArea = 0;
        Point[]? receiptContour = null;

        foreach (var contour in contours)
        {
            double area = Cv2.ContourArea(contour);
            if (area > 10000) // Higher threshold for this method
            {
                var rect = Cv2.MinAreaRect(contour);
                var aspectRatio = Math.Max(rect.Size.Width, rect.Size.Height) / Math.Min(rect.Size.Width, rect.Size.Height);

                // Typical receipts have an aspect ratio between 1.5 and 5
                if (aspectRatio > 1.5 && aspectRatio < 5 && area > maxArea)
                {
                    receiptContour = contour;
                    maxArea = area;
                }
            }
        }

        if (receiptContour != null)
        {
            var rect = Cv2.MinAreaRect(receiptContour);
            Point2f[] rectPoints = rect.Points();
            Point[] corners = Array.ConvertAll(rectPoints, p => new Point((int)p.X, (int)p.Y));

            // Order the corners
            corners = GetOrderedRectangleCorners(corners);

            // Perspective transform
            Mat croppedReceipt = PerspectiveTransform(original, corners);

            return (true, croppedReceipt);
        }

        // If both methods fail, return the original image with basic processing
        Mat processed = original.Clone();
        Cv2.DetailEnhance(processed, processed);
        return (false, processed);
    }

    /// <summary>
    /// Orders rectangle corners in the sequence: top-left, top-right, bottom-right, bottom-left
    /// </summary>
    private Point[] GetOrderedRectangleCorners(Point[] points)
    {
        if (points.Length < 4)
            return points;

        // If we have more than 4 points, get the 4 most extreme
        if (points.Length > 4)
        {
            var boundingRect = Cv2.BoundingRect(points);
            return new Point[]
            {
                    new Point(boundingRect.X, boundingRect.Y),
                    new Point(boundingRect.X + boundingRect.Width, boundingRect.Y),
                    new Point(boundingRect.X + boundingRect.Width, boundingRect.Y + boundingRect.Height),
                    new Point(boundingRect.X, boundingRect.Y + boundingRect.Height)
            };
        }

        // Calculate the center of mass
        Point center = new Point();
        foreach (var point in points)
        {
            center.X += point.X;
            center.Y += point.Y;
        }
        center.X /= points.Length;
        center.Y /= points.Length;

        // Sort the points based on their angle from the center
        Point[] result = new Point[4];
        Array.Copy(points, result, 4);

        Array.Sort(result, (a, b) =>
        {
            double angleA = Math.Atan2(a.Y - center.Y, a.X - center.X);
            double angleB = Math.Atan2(b.Y - center.Y, b.X - center.X);
            return angleA.CompareTo(angleB);
        });

        // Rearrange to ensure top-left, top-right, bottom-right, bottom-left order
        Point[] ordered = new Point[4];
        ordered[0] = result.OrderBy(p => p.X + p.Y).First(); // Top-left
        ordered[2] = result.OrderByDescending(p => p.X + p.Y).First(); // Bottom-right

        var remaining = result.Where(p => p != ordered[0] && p != ordered[2]).ToArray();
        ordered[1] = remaining.OrderBy(p => p.Y - p.X).First(); // Top-right
        ordered[3] = remaining.OrderBy(p => p.X - p.Y).First(); // Bottom-left

        return ordered;
    }

    /// <summary>
    /// Performs perspective transform to get a top-down view of the receipt
    /// </summary>
    private Mat PerspectiveTransform(Mat image, Point[] corners)
    {
        if (corners.Length != 4)
            throw new ArgumentException("Four corners are required for perspective transform");

        // Calculate width and height of the receipt
        double width1 = Math.Sqrt(Math.Pow(corners[1].X - corners[0].X, 2) + Math.Pow(corners[1].Y - corners[0].Y, 2));
        double width2 = Math.Sqrt(Math.Pow(corners[2].X - corners[3].X, 2) + Math.Pow(corners[2].Y - corners[3].Y, 2));
        int width = (int)Math.Max(width1, width2);

        double height1 = Math.Sqrt(Math.Pow(corners[3].X - corners[0].X, 2) + Math.Pow(corners[3].Y - corners[0].Y, 2));
        double height2 = Math.Sqrt(Math.Pow(corners[2].X - corners[1].X, 2) + Math.Pow(corners[2].Y - corners[1].Y, 2));
        int height = (int)Math.Max(height1, height2);

        // Define the destination points for the transform
        Point2f[] src = Array.ConvertAll(corners, p => new Point2f(p.X, p.Y));
        Point2f[] dst = new Point2f[]
        {
                new Point2f(0, 0),
                new Point2f(width - 1, 0),
                new Point2f(width - 1, height - 1),
                new Point2f(0, height - 1)
        };

        // Get the perspective transform matrix
        var perspectiveMatrix = Cv2.GetPerspectiveTransform(src, dst);

        // Apply the perspective transformation
        Mat result = new Mat();
        Cv2.WarpPerspective(image, result, perspectiveMatrix, new Size(width, height));

        return result;
    }
}

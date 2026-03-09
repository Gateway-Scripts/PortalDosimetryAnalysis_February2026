using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using VMS.CA.Scripting;

namespace VMS.DV.PD.Scripting.Models
{
    public class FieldModel
    {
        public string FieldId { get; set; }
        public double GammaPassRate_1mm_1pct { get; set; }
        public double GammaPassRate_2mm_1pct { get; set; }
        public double GammaPassRate_3mm_1pct { get; set; }
        public double GammaPassRate_1mm_2pct { get; set; }
        public double GammaPassRate_2mm_2pct { get; set; }
        public double GammaPassRate_3mm_2pct { get; set; }
        public double GammaPassRate_1mm_3pct { get; set; }
        public double GammaPassRate_2mm_3pct { get; set; }
        public double GammaPassRate_3mm_3pct { get; set; }
        public BitmapSource PredictedImage { get; set; }
        public BitmapSource AcquiredImage { get; set; }
        public BitmapSource GammaImage { get; set; }
        public FieldModel(PDBeam beam)
        {
            FieldId = beam.Id;
            var image = beam.PortalDoseImages.Last();
            GammaPassRate_1mm_1pct = GetGammaPassRate(beam, image, 1.0, 0.01, false);
            GammaPassRate_2mm_1pct = GetGammaPassRate(beam, image, 2.0, 0.01, false);
            GammaPassRate_3mm_1pct = GetGammaPassRate(beam, image, 3.0, 0.01, false);
            GammaPassRate_1mm_2pct = GetGammaPassRate(beam, image, 1.0, 0.02, false);
            GammaPassRate_2mm_2pct = GetGammaPassRate(beam, image, 2.0, 0.02, true);
            GammaPassRate_3mm_2pct = GetGammaPassRate(beam, image, 3.0, 0.02, false);
            GammaPassRate_1mm_3pct = GetGammaPassRate(beam, image, 1.0, 0.03, false);
            GammaPassRate_2mm_3pct = GetGammaPassRate(beam, image, 2.0, 0.03, false);
            GammaPassRate_3mm_3pct = GetGammaPassRate(beam, image, 3.0, 0.03, false);
        }

        private double GetGammaPassRate(PDBeam beam, PortalDoseImage image, double dta, double dd, bool gammaImage)
        {
            var pdTemplate = new PDTemplate(false,//not constancy check
                true,//use alignment
                false,//do not align to setup field.
                false,//no imager rotation
                AnalysisMode.CU,//analyze in absolute mode.
                NormalizationMethod.Unknown,//normalization method doesn't matter in absolute mode.
                true,//allow for ROI from threshold
                0.05,//RPI threshold 5%.
                ROIType.None,//other ROI doesn't matter.
                10.0,//doesn't matter margin since ROI is threshold.
                dd,//dose difference
                dta,//distance to agreement (mm).
                false,//local gamme = true, global gamma = false.
                new List<EvaluationTestDesc> { new EvaluationTestDesc(EvaluationTestKind.GammaAreaLessThanOne, 0, 0.90, true) });
            var analysis = image.CreateTransientAnalysis(pdTemplate, beam.PredictedDoseImage);
            if (gammaImage)
            {
                GenerateImages(beam.PredictedDoseImage, image, analysis.GammaImage);
            }
            return analysis.EvaluationTests.FirstOrDefault().TestValue * 100.0;
        }

        private void GenerateImages(DoseImage predictedDoseImage, PortalDoseImage image, ImageRT gammaImage)
        {
            ushort[,] predictedPixels = new ushort[predictedDoseImage.Image.FramesRT.First().XSize, 
                predictedDoseImage.Image.FramesRT.First().YSize];
            predictedDoseImage.Image.FramesRT.First().GetVoxels(0, predictedPixels);
            ushort[,] acquiredPixels = new ushort[image.Image.FramesRT.First().XSize,
                image.Image.FramesRT.First().YSize];
            image.Image.FramesRT.First().GetVoxels(0, acquiredPixels);
            ushort[,] gammaPixels = new ushort[gammaImage.FramesRT.First().XSize,
                gammaImage.FramesRT.First().YSize];
            gammaImage.FramesRT.First().GetVoxels(0, gammaPixels);

            PredictedImage = CreateHeatMapImage(predictedPixels);
            AcquiredImage = CreateHeatMapImage(acquiredPixels);
            GammaImage = CreateGammaImage(gammaPixels);
        }

        private unsafe BitmapSource CreateHeatMapImage(ushort[,] pixels)
        {
            int width = pixels.GetLength(0);
            int height = pixels.GetLength(1);

            ushort maxValue = 0;
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    if (pixels[x, y] > maxValue)
                        maxValue = pixels[x, y];
                }
            }

            var bitmap = new WriteableBitmap(width, height, 96, 96, System.Windows.Media.PixelFormats.Bgr32, null);
            bitmap.Lock();

            byte* ptr = (byte*)bitmap.BackBuffer;
            int stride = bitmap.BackBufferStride;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    double normalizedValue = maxValue > 0 ? (double)pixels[x, y] / maxValue : 0;
                    var color = GetHeatMapColor(normalizedValue);

                    int offset = y * stride + x * 4;
                    ptr[offset] = color.B;
                    ptr[offset + 1] = color.G;
                    ptr[offset + 2] = color.R;
                    ptr[offset + 3] = 255;
                }
            }

            bitmap.AddDirtyRect(new System.Windows.Int32Rect(0, 0, width, height));
            bitmap.Unlock();
            bitmap.Freeze();

            return bitmap;
        }

        private unsafe BitmapSource CreateGammaImage(ushort[,] pixels)
        {
            int width = pixels.GetLength(0);
            int height = pixels.GetLength(1);

            ushort maxValue = 0;
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    if (pixels[x, y] > maxValue)
                        maxValue = pixels[x, y];
                }
            }

            double threshold90Percent = maxValue * 0.9;

            var bitmap = new WriteableBitmap(width, height, 96, 96, System.Windows.Media.PixelFormats.Bgr32, null);
            bitmap.Lock();

            byte* ptr = (byte*)bitmap.BackBuffer;
            int stride = bitmap.BackBufferStride;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    double gammaValue = maxValue > 0 ? (double)pixels[x, y] / (maxValue / 3.0) : 0;
                    var color = GetGammaColor(gammaValue, pixels[x, y] > threshold90Percent);

                    int offset = y * stride + x * 4;
                    ptr[offset] = color.B;
                    ptr[offset + 1] = color.G;
                    ptr[offset + 2] = color.R;
                    ptr[offset + 3] = 255;
                }
            }

            bitmap.AddDirtyRect(new System.Windows.Int32Rect(0, 0, width, height));
            bitmap.Unlock();
            bitmap.Freeze();

            return bitmap;
        }

        private System.Windows.Media.Color GetHeatMapColor(double value)
        {
            byte r, g, b;

            if (value >= 0.75)
            {
                double t = (value - 0.75) / 0.25;
                r = 255;
                g = (byte)(255 * (1 - t));
                b = 0;
            }
            else if (value >= 0.5)
            {
                double t = (value - 0.5) / 0.25;
                r = (byte)(255 * t);
                g = 255;
                b = 0;
            }
            else if (value >= 0.25)
            {
                double t = (value - 0.25) / 0.25;
                r = 0;
                g = (byte)(255 * t);
                b = (byte)(255 * (1 - t));
            }
            else
            {
                double t = value / 0.25;
                r = 0;
                g = 0;
                b = (byte)(255 * t);
            }

            return System.Windows.Media.Color.FromRgb(r, g, b);
        }

        private System.Windows.Media.Color GetGammaColor(double gammaValue, bool isHighest10Percent)
        {
            if (gammaValue < 1.0)
            {
                return System.Windows.Media.Color.FromRgb(0, 200, 0);
            }
            else if (isHighest10Percent)
            {
                return System.Windows.Media.Color.FromRgb(200, 0, 0);
            }
            else
            {
                return System.Windows.Media.Color.FromRgb(255, 165, 0);
            }
        }
    }
}

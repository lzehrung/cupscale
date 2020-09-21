using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Windows;
using Cupscale.ImageUtils;
using Cupscale.IO;
using Cupscale.UI;
using ImageMagick;
using Paths = Cupscale.IO.Paths;

namespace Cupscale
{
	internal class PreviewMerger
	{
		public static float offsetX;
		public static float offsetY;
		public static string inputCutoutPath;
		public static string outputCutoutPath;
		//public static int scale;

		public static bool showingOriginal = false;

		public static void Merge ()
		{
			Program.mainForm.SetProgress(100f);
			inputCutoutPath = Path.Combine(Paths.previewPath, "preview.png");
			outputCutoutPath = Path.Combine(Paths.previewOutPath, "preview.png");

			//MagickImage sourceImg = new MagickImage(Paths.tempImgPath);
			Image sourceImg = IOUtils.GetImage(Paths.tempImgPath);
			int scale = GetScale();
			if (sourceImg.Width * scale > 6000 || sourceImg.Height * scale > 6000)
            {
				MergeOnlyCutout();
				MessageBox.Show("The output image is very large (>6000px), so only the cutout will be shown.", "Warning");
				return;
			}
			MergeScrollable();
		}

		static void MergeScrollable ()
        {
			
			if (offsetX < 0f) offsetX *= -1f;
			if (offsetY < 0f) offsetY *= -1f;
			int scale = GetScale();
			offsetX *= scale;
			offsetY *= scale;
			Logger.Log("Merging " + outputCutoutPath + " onto " + Program.lastFilename + " using offset " + offsetX + "x" + offsetY);
			string scaledPrevPath = Path.Combine(Paths.previewOutPath, "preview-input-scaled.png");
			//Image image = MergeOnDisk(scale, scaledPrevPath);
			Image image = MergeInMemory(scale, scaledPrevPath);
			MainUIHelper.currentOriginal = IOUtils.GetImage(Paths.tempImgPath);
			MainUIHelper.currentOutput = image;
			MainUIHelper.currentScale = ImgUtils.GetScale(IOUtils.GetImage(inputCutoutPath), IOUtils.GetImage(outputCutoutPath));
			UIHelpers.ReplaceImageAtSameScale(MainUIHelper.previewImg, image);
			Program.mainForm.SetProgress(0f, "Done.");
		}

		public static Image MergeInMemory(int scale, string scaledPrevPath)
		{
			Image sourceImg = IOUtils.GetImage(Paths.tempImgPath);
			Image cutout = IOUtils.GetImage(outputCutoutPath);

			var destImage = new Bitmap(sourceImg.Width * scale, sourceImg.Height * scale);

			using (var graphics = Graphics.FromImage(destImage))
			{
				graphics.CompositingMode = CompositingMode.SourceCopy;
				graphics.CompositingQuality = CompositingQuality.HighQuality;
				graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
				if(Program.currentFilter == FilterType.Point)
					graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
				graphics.SmoothingMode = SmoothingMode.HighQuality;
				graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

				graphics.DrawImage(sourceImg, 0, 0, destImage.Width, destImage.Height);		// Scale up
				graphics.DrawImage(cutout, offsetX, offsetY);     // Overlay cutout
			}

			return destImage;
		}

		public static Image MergeOnDisk (int scale, string scaledPrevPath)
        {
			MagickImage sourceImg = new MagickImage(Paths.tempImgPath);
			MagickImage cutout = new MagickImage(outputCutoutPath);
			sourceImg.FilterType = Program.currentFilter;
			sourceImg.Resize(new Percentage(scale * 100));
			sourceImg.Format = MagickFormat.Png;
			sourceImg.Quality = 0;  // Save preview as uncompressed PNG for max speed
			sourceImg.Write(scaledPrevPath);
			sourceImg.Composite(cutout, (Gravity)1, new PointD(offsetX, offsetY), CompositeOperator.Replace);
			string mergedPreviewPath = Path.Combine(Paths.previewOutPath, "preview-merged.png");
			sourceImg.Write(mergedPreviewPath);
			return IOUtils.GetImage(mergedPreviewPath);
		}

		static void MergeOnlyCutout ()
		{
			int scale = GetScale();

			MagickImage originalCutout = new MagickImage(inputCutoutPath);
			originalCutout.FilterType = Program.currentFilter;
			originalCutout.Resize(new Percentage(scale * 100));
			string scaledCutoutPath = Path.Combine(Paths.previewOutPath, "preview-input-scaled.png");
			originalCutout.Format = MagickFormat.Png;
			originalCutout.Quality = 0;  // Save preview as uncompressed PNG for max speed
			originalCutout.Write(scaledCutoutPath);

			MainUIHelper.currentOriginal = IOUtils.GetImage(scaledCutoutPath);
			MainUIHelper.currentOutput = IOUtils.GetImage(outputCutoutPath);

			MainUIHelper.previewImg.Image = MainUIHelper.currentOutput;
			MainUIHelper.previewImg.ZoomToFit();
			MainUIHelper.previewImg.Zoom = (int)Math.Round(MainUIHelper.previewImg.Zoom * 1.01f);
			Program.mainForm.resetImageOnMove = true;
			Program.mainForm.SetProgress(0f, "Done.");
		}

		private static int GetScale()
		{
			MagickImage val = new MagickImage(inputCutoutPath);
			MagickImage val2 = new MagickImage(outputCutoutPath);
			int result = (int)Math.Round((float)val2.Width / (float)val.Width);
			return result;
		}

		public static void ShowOutput()
		{
			if (MainUIHelper.currentOutput != null)
			{
				showingOriginal = false;
				UIHelpers.ReplaceImageAtSameScale(MainUIHelper.previewImg, MainUIHelper.currentOutput);
			}
		}

		public static void ShowOriginal()
		{
			if (MainUIHelper.currentOriginal != null)
			{
				showingOriginal = true;
				UIHelpers.ReplaceImageAtSameScale(MainUIHelper.previewImg, MainUIHelper.currentOriginal);
			}
		}
	}
}

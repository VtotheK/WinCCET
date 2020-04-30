using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Media;
using System.Windows.Interop;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Media.Imaging;

namespace ErrorTracker.BitMapHelpers
{
    public static class ImageHelper
    {
        [System.Runtime.InteropServices.DllImport("gdi32.dll")] 
        public static extern bool DeleteObject(IntPtr hObject);

        public static BitmapSource GetBitmapSourceFromSector(Rect rect)
        {
            if(rect.Height <= 1 || rect.Width <= 1 || rect.X < 0 || rect.Y < 0)
            {
                return null;
            }
            try
            {
                using (Bitmap bSBitmap = new Bitmap((int)rect.Width, (int)rect.Height, System.Drawing.Imaging.PixelFormat.Format32bppRgb))
                {
                    using (Graphics bSourceGraphics = Graphics.FromImage(bSBitmap))

                    {
                        bSourceGraphics.CopyFromScreen((int)rect.X,(int)rect.Y ,0,0,bSBitmap.Size);
                        IntPtr hBitmap = bSBitmap.GetHbitmap();
                        BitmapSource brSource = Imaging.CreateBitmapSourceFromHBitmap(hBitmap, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
                        DeleteObject(hBitmap); //GC does not take care of this pointer, has to be manually deleted.
                        return brSource;
                    }

                }
            }

            catch (Exception ex) 
            {
                string message = $"At {DateTime.Now.ToShortDateString()} {DateTime.Now.ToShortTimeString()}." +
                    $"\n {ex.ToString()} {ex.Message} \n ------------------------------------------";
                DebugLogger.Log(LogType.FATALERROR, message);
                return null;
            }
        }

        public static Bitmap GetBitmapFromSector(Rect rect)
        {
            Bitmap bitmap = new Bitmap((int)rect.Width, (int)rect.Height, System.Drawing.Imaging.PixelFormat.Format32bppRgb);
            try
            {
                using (Graphics graphics = Graphics.FromImage(bitmap))
                {
                    graphics.CopyFromScreen((int)rect.X, (int)rect.Y, 0, 0, bitmap.Size);
                    return bitmap;
                }
            }

            catch (Exception ex) 
            {
                string message = $"At{DateTime.Now.ToShortDateString()} {DateTime.Now.ToShortTimeString()}." +
                    $"\n {ex.ToString()} {ex.Message} in SectorFromScreenShot.GetBitmapFromSector().";
                    Debug.Write(ex.ToString());
                DebugLogger.Log(LogType.FATALERROR,message);
                return null;
            }
        }

        public static BitmapSource GetBitmapSourceFromBitmap(Bitmap bitmap)
        {
            BitmapData btData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                                                ImageLockMode.ReadOnly, bitmap.PixelFormat);
            BitmapSource btSource = BitmapSource.Create(btData.Width, btData.Height, bitmap.HorizontalResolution,
                                                        bitmap.VerticalResolution, PixelFormats.Bgr24, null,
                                                        btData.Scan0, btData.Stride * btData.Height, btData.Stride);
            bitmap.UnlockBits(btData);
            return btSource;
        }
    }
}


using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;


namespace WindowsFormsApplication1
{
    unsafe public partial class Form1 : Form
    {
        //Gray
        int[,] original_image;//[H, W]
        //RGB
        int[, ,] originalRGB_image;//[H, W, 3]

        //Gray
        int[,] PepperSalt_image;//[H, W]
        //RGB
        int[, ,] PepperSaltRGB_image;//[H, W, 3]

        //Gray
        int[,] Output_Contra;//[H, W]
        //RGB
        int[, ,] OutputRGB_Contra;//[H, W, 3]

        //Gray
        int[,] Output_Alpha;//[H, W]
        //RGB
        int[, ,] OutputRGB_Alpha;//[H, W, 3]

        //Gray
        int[,] Result_image;//[H, W]
        //RGB
        int[, ,] ResultRGB_image;//[H, W, 3]

        int[] MI;
        int[] MIr;
        int[] MIg;
        int[] MIb;

        int mask;

        int d = 2;


        int w, h;
        double Q; //Contraharmonic mean filter
        double enhance;

        //noise除以的倍數
        int noise = 10;


        public Form1()
        {
            InitializeComponent();
            Control.CheckForIllegalCrossThreadCalls = false;
        }

        /////////enhance image///////////
        public static Bitmap Normalization(Bitmap srcImage, double blackPointPercent = 0.1, double whitePointPercent = 0.1)
        {
            //Lock bits for your source image into system memory
            Rectangle rect = new Rectangle(0, 0, srcImage.Width, srcImage.Height);
            BitmapData srcData = srcImage.LockBits(rect, ImageLockMode.ReadOnly,
                PixelFormat.Format32bppArgb);

            //Create a bitmap to which you will write new pixel data
            Bitmap destImage = new Bitmap(srcImage.Width, srcImage.Height);

            //Lock bits for your writable bitmap into system memory
            Rectangle rect2 = new Rectangle(0, 0, destImage.Width, destImage.Height);
            BitmapData destData = destImage.LockBits(rect2, ImageLockMode.WriteOnly,
                PixelFormat.Format32bppArgb);

            //Get the width of a single row of pixels in the bitmap
            int stride = srcData.Stride;

            //Scan for the first pixel data in bitmaps
            IntPtr srcScan0 = srcData.Scan0;
            IntPtr destScan0 = destData.Scan0;

            var freq = new int[256];

            unsafe
            {
                //Create an array of pixel data from source image
                byte* src = (byte*)srcScan0;

                //Get the number of pixels for each intensity value
                for (int y = 0; y < srcImage.Height; ++y)
                {
                    for (int x = 0; x < srcImage.Width; ++x)
                    {
                        freq[src[y * stride + x * 4]]++;
                    }
                }

                //Get the total number of pixels in the image
                int numPixels = srcImage.Width * srcImage.Height;

                //Set the minimum intensity value of an image (0 = black)
                int minI = 0;

                //Get the total number of black pixels
                var blackPixels = numPixels * blackPointPercent;

                //Set a variable for summing up the pixels that will turn black
                int accum = 0;

                //Sum up the darkest shades until you reach the total of black pixels
                while (minI < 255)
                {
                    accum += freq[minI];
                    if (accum > blackPixels) break;
                    minI++;
                }

                //Set the maximum intensity of an image (255 = white)
                int maxI = 255;

                //Set the total number of white pixels
                var whitePixels = numPixels * whitePointPercent;

                //Reset the summing variable back to 0
                accum = 0;

                //Sum up the pixels that are the lightest which will turn white
                while (maxI > 0)
                {
                    accum += freq[maxI];
                    if (accum > whitePixels) break;
                    maxI--;
                }

                //Part of a normalization equation that doesn't vary with each pixel
                double spread = 255d / (maxI - minI);

                //Create an array of pixel data from created image
                byte* dst = (byte*)destScan0;

                //Write new pixel data to the image
                for (int y = 0; y < srcImage.Height; ++y)
                {
                    for (int x = 0; x < srcImage.Width; ++x)
                    {
                        int i = y * stride + x * 4;

                        //Part of equation that varies with each pixel
                        double value = Math.Round((src[i] - minI) * spread);

                        byte val = (byte)(Math.Min(Math.Max(value, 0), 255));
                        dst[i] = val;
                        dst[i + 1] = val;
                        dst[i + 2] = val;
                        dst[i + 3] = 255;
                    }
                }
            }

            //Unlock bits from system memory
            srcImage.UnlockBits(srcData);
            destImage.UnlockBits(destData);

            return destImage;
        }

 
        /////////PSNR////////////
        private void PSNR_process(int[,] image1, int[,] image2)
        {
            //--PSNR--//
            double MSE = 0;
            double PSNR = 0.0;
            double max_pixel = 0;

            max_pixel = 255;

            int w = image1.GetLength(0);
            int h = image1.GetLength(1);

            //get MSE
            for (int x = 0; x < w; x++)
            {
                for (int y = 0; y < h; y++)
                {
                    MSE += Math.Pow(image1[x, y] - image2[x, y], 2);
                }
            }

            PSNR = 20 * Math.Log10(max_pixel * w) - 10 * Math.Log10(MSE);

            //swlog.WriteLine("PSNR = " + PSNR);
            System.Console.WriteLine("PSNR = " + PSNR);

            //sw.WriteLine(PSNR);	//"PSNR");
            //sw.WriteLine(intRLE);	//intRLE");
            //sw.WriteLine("=====");
        }


        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        ////** showpicture在picturebox **/////
        private void show_picture(PictureBox pb, int[,] image)
        {
            Bitmap bm;
            Color c;

            int h = image.GetLength(0);
            int w = image.GetLength(1);

            //清空pictureBox
            pb.Image = null;

            bm = new Bitmap(w, h);

            int x, y;


            //put in picturebox
            for (x = 0; x < h; x++)
            {
                for (y = 0; y < w; y++)
                {
                    int p = image[x, y];
                    c = Color.FromArgb(p, p, p);
                    bm.SetPixel(y, x, c);

                }
            }
            //放回pictureBox
            pb.Image = bm;
        }
        ///////////////////////////////////////////////////////////////////////////
        ////** showpicture(RGB版)在picturebox **/////
        private void showRGB_picture(PictureBox pb, int[,,] image)
        {  
            int h = image.GetLength(0);
            int w = image.GetLength(1);

            //用Bitmap將image包起來
            Bitmap bm = new Bitmap(w, h, PixelFormat.Format24bppRgb);
            Color c;

            //清空pictureBox
            pb.Image = null;

            //int[, ,] RGBdata = new int[w, h, 3];
            bm = new Bitmap(w, h);

            int x, y;
            int r, g, b;

            //put in picturebox
            for (x = 0; x < h; x++)
            {
                for (y = 0; y < w; y++)
                {
            
                    r = image[x, y, 0];
                    g = image[x, y, 1];
                    b = image[x, y, 2];
                    c = Color.FromArgb(r, g, b);
                    bm.SetPixel(y, x, c);
                }
            }
            //放回pictureBox
            pb.Image = bm;
        }


        //////////////////////////////////////////////////////////////////////////////////////////////////////////
        private void button1_Click(object sender, EventArgs e)
        {

            //////** 開啟檔案 **/////
            OpenFileDialog openFileDialog = new OpenFileDialog();
            string FileName;

            //顯示圖片視窗隨開啟的圖片大小縮放
            pictureBox1.SizeMode = PictureBoxSizeMode.AutoSize;
            pictureBox2.SizeMode = PictureBoxSizeMode.AutoSize;
            pictureBox3.SizeMode = PictureBoxSizeMode.AutoSize;
            pictureBox4.SizeMode = PictureBoxSizeMode.AutoSize;
            pictureBox5.SizeMode = PictureBoxSizeMode.AutoSize;
            pictureBox6.SizeMode = PictureBoxSizeMode.AutoSize;

            //設定OpenFileDialog起始的路徑
            openFileDialog.InitialDirectory = "C:\\Users\\USER\\圖片";
            openFileDialog.Filter = "所有檔案(*.*)|*.*";

            try
            {
                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    FileName = openFileDialog.FileName;
                    pictureBox1.Image = Image.FromFile(FileName);
                }
            }
            catch
            {
                MessageBox.Show("讀取錯誤123");
            }


            

            //取得圖檔寬高
            w = pictureBox1.Image.Width;
            h = pictureBox1.Image.Height;

            //調整pictureBox2的位置
            pictureBox2.Location = new Point(pictureBox1.Right + 20, pictureBox1.Top);
            pictureBox3.Location = new Point(pictureBox1.Right - w, pictureBox1.Top + h + 20);
            pictureBox4.Location = new Point(pictureBox1.Right + 20, pictureBox1.Top + h +20);
            pictureBox5.Location = new Point(pictureBox1.Right + w + 20 * 2, pictureBox1.Top);
            pictureBox6.Location = new Point(pictureBox1.Right + w + 20 * 2, pictureBox1.Top + h + 20);

            //new original_image
            original_image = new int[h, w];
            originalRGB_image = new int[h, w, 3];

            //Gray
            PepperSalt_image = new int[h, w];
            //RGB
            PepperSaltRGB_image = new int[h, w, 3];


            /////////////////////////////////////////////////////////////////////////////////////////////////////
            /////** 讀取pixel值 **/////
            //處理像素資料用物件容器
            Bitmap bm = (Bitmap)pictureBox1.Image;
            Bitmap bmRGB = (Bitmap)pictureBox3.Image;
            Color c;

            int x, y;
            int image_w = pictureBox1.Image.Width;
            int image_h = pictureBox1.Image.Height;

            double r, g, b, gray;
            int alpha;

            try
            {
                //讀取圖片像素
                for (x = 0; x < image_h; x++)
                {
                    for (y = 0; y < image_w; y++)
                    {
                        c = bm.GetPixel(y, x);  //螢幕上的位置是以 X 和 Y 座標軸來描述，X 座標軸是向右遞增，而 Y 座標軸則是從上到下遞增
                        //original_image[x, y] = c.R;
                        //Gray = 0.299 * Red + 0.587 * Green + 0.114 * Blue
                        b = c.B;
                        g = c.G;
                        r = c.R;
                        alpha = c.A;


                        //gray = 0.299 * r + 0.587 * g + 0.114 * b;
                        if(Math.Abs(b-g)<=20 && Math.Abs(g-r)<=20 && Math.Abs(b-r)<=20)
                            gray = (b+g+r)/3.0;
                        else
                            gray = 0.299 * r + 0.587 * g + 0.114 * b;

                        original_image[x, y] = (int)gray;
                        PepperSalt_image[x, y] = (int)gray;

                        originalRGB_image[x, y, 0] = (int)r;
                        originalRGB_image[x, y, 1] = (int)g;
                        originalRGB_image[x, y, 2] = (int)b;

                        PepperSaltRGB_image[x, y, 0] = (int)r;
                        PepperSaltRGB_image[x, y, 1] = (int)g;
                        PepperSaltRGB_image[x, y, 2] = (int)b;
                       // System.Console.WriteLine(gray);
                    }
                }
            }
            catch
            {
                System.Console.WriteLine("讀取錯誤321");
            }
            
            show_picture(pictureBox1, original_image);
            showRGB_picture(pictureBox3, originalRGB_image);

            bm = (Bitmap)pictureBox1.Image;
            bmRGB = (Bitmap)pictureBox3.Image;
            bm.Save("Gray.jpg", ImageFormat.Jpeg);


            ////////////////////////////////////////////////////////////////////////////////////////
            //// *** 鎖住Button *** ////

            button2.Enabled = false;
            button6.Enabled = false;
            button7.Enabled = false;
            button3.Enabled = false;
 
        }

        //////////////////////////////////////////////////////////////////////////////////////////
        ////** Run **////
        private void button2_Click(object sender, EventArgs e)
        {
            string combo = comboBox1.Text;

            switch (combo)
            {
                case "3*3": mask = 3; break;
                case "5*5": mask = 5; break;
                case "7*7": mask = 7; break;
                default: mask = 3; break;
            }

            string src = textBox1.Text;
            Q = Double.Parse(src);



            ////Gray////
            Output_Contra = new int[h, w];
            Output_Alpha = new int[h, w];

            ////RGB////
            OutputRGB_Contra = new int[h, w, 3];
            OutputRGB_Alpha = new int[h, w, 3];


            //////////////////////////////////////////////////////////////////////////////////////////////////
            /////////////////複製外圍一圈的值//////////////////

            ////Gray////
            int[,] Copy = new int[h + 2, w + 2];//複製最外圍一圈的值
            ////RGB////
            int[, ,] CopyRGB = new int[h + 2, w + 2, 3];//複製最外圍一圈的值

            ////Gray////
            Copy[0, 0] = original_image[0, 0];
            Copy[0, w + 1] = original_image[0, w - 1];
            Copy[h + 1, 0] = original_image[h - 1, 0];
            Copy[h + 1, w + 1] = original_image[h - 1, w - 1];

            ////RGB////
                //R
            CopyRGB[0, 0, 0] = originalRGB_image[0, 0, 0];
            CopyRGB[0, w + 1, 0] = originalRGB_image[0, w - 1, 0];
            CopyRGB[h + 1, 0, 0] = originalRGB_image[h - 1, 0, 0];
            CopyRGB[h + 1, w + 1, 0] = originalRGB_image[h - 1, w - 1, 0];
                //G
            CopyRGB[0, 0, 1] = originalRGB_image[0, 0, 1];
            CopyRGB[0, w + 1, 1] = originalRGB_image[0, w - 1, 1];
            CopyRGB[h + 1, 0, 1] = originalRGB_image[h - 1, 0, 1];
            CopyRGB[h + 1, w + 1, 1] = originalRGB_image[h - 1, w - 1, 1];
                //R
            CopyRGB[0, 0, 2] = originalRGB_image[0, 0, 2];
            CopyRGB[0, w + 1, 2] = originalRGB_image[0, w - 1, 2];
            CopyRGB[h + 1, 0, 2] = originalRGB_image[h - 1, 0, 2];
            CopyRGB[h + 1, w + 1, 2] = originalRGB_image[h - 1, w - 1, 2];

            for (int j2 = 1; j2 < w + 1; j2++)
            {
                ////Gray////
                Copy[0, j2] = original_image[0, j2 - 1];
                Copy[h + 1, j2] = original_image[h - 1, j2 - 1];

                ////RGB////
                    //R
                CopyRGB[0, j2, 0] = originalRGB_image[0, j2 - 1, 0];
                CopyRGB[h + 1, j2, 0] = originalRGB_image[h - 1, j2 - 1, 0];
                    //G
                CopyRGB[0, j2, 1] = originalRGB_image[0, j2 - 1, 1];
                CopyRGB[h + 1, j2, 1] = originalRGB_image[h - 1, j2 - 1, 1];
                    //B
                CopyRGB[0, j2, 2] = originalRGB_image[0, j2 - 1, 2];
                CopyRGB[h + 1, j2, 2] = originalRGB_image[h - 1, j2 - 1, 2];
            }
            for (int i2 = 1; i2 < h + 1; i2++)
            {
                ////Gray////
                Copy[i2, 0] = original_image[i2 - 1, 0];
                Copy[i2, w + 1] = original_image[i2 - 1, w - 1];

                ////RGB////
                    //R
                CopyRGB[i2, 0, 0] = originalRGB_image[i2 - 1, 0, 0];
                CopyRGB[i2, w + 1, 0] = originalRGB_image[i2 - 1, w - 1, 0];

                    //G
                CopyRGB[i2, 0, 1] = originalRGB_image[i2 - 1, 0, 1];
                CopyRGB[i2, w + 1, 1] = originalRGB_image[i2 - 1, w - 1, 1];

                    //B
                CopyRGB[i2, 0, 2] = originalRGB_image[i2 - 1, 0, 2];
                CopyRGB[i2, w + 1, 2] = originalRGB_image[i2 - 1, w - 1, 2];

            }
            for (int i2 = 0; i2 < h; i2++)
            {
                for (int j2 = 0; j2 < w; j2++)
                {
                    ////Gray////
                    Copy[i2 + 1, j2 + 1] = original_image[i2, j2];

                    ////RGB////
                        //R
                    CopyRGB[i2 + 1, j2 + 1, 0] = originalRGB_image[i2, j2, 0];
                        //G
                    CopyRGB[i2 + 1, j2 + 1, 1] = originalRGB_image[i2, j2, 1];
                        //B
                    CopyRGB[i2 + 1, j2 + 1, 2] = originalRGB_image[i2, j2, 2];
                }
            }

            ///////////arithmetic///////////
            ///////////starting/////////////
            int i , j , x , y ;

            ////Gray////
            double[,]I = new double[mask,mask];

            double sumContra = 0.0, sumContra_1 = 0.0;


            ////RGB////
            double[,,] Irgb = new double[mask, mask, 3];
                //R
            double sumContraR = 0.0, sumContraR_1 = 0.0;
                //G
            double sumContraG = 0.0, sumContraG_1 = 0.0;
                //B
            double sumContraB = 0.0, sumContraB_1 = 0.0;

            ////Gray////
            int sumAlpha = 0;

            ////RGB////
                //R
            int sumAlphaR = 0;
                //G
            int sumAlphaG = 0;
                //B
            int sumAlphaB = 0;


            ////Gray////
            MI = new int[mask * mask];//Alpha-trimmed mean filter

            ////RGB////
                //R
            MIr = new int[mask * mask];//Alpha-trimmed mean filter RGB版
                //G
            MIg = new int[mask * mask];//Alpha-trimmed mean filter RGB版
                //B
            MIb = new int[mask * mask];//Alpha-trimmed mean filter RGB版

            
            for (i = 0; i < h ; i = i ++)
            {
                if (i + mask > h) break;
                for (j = 0; j < w; j = j ++)
                {
                    if (j + mask > w ) break;
                        int count = 0;
                        for (x = 0; x < mask; ++x)
                        {
                            for (y = 0; y < mask; ++y)
                            {

                                           //Gray
                                        I[x, y] = Copy[i + x, j + y];
                                        MI[count] = Copy[i + x, j + y];///Alpha-trimmed mean filter


                                        //System.Console.WriteLine("count = " + count);
                                           //RGB
                                                //R
                                        Irgb[x, y, 0] = CopyRGB[i + x, j + y, 0];
                                        MIr[count] = CopyRGB[i + x, j + y, 0];///Alpha-trimmed mean filter
                                                //G
                                        Irgb[x, y, 1] = CopyRGB[i + x, j + y, 1];
                                        MIg[count] = CopyRGB[i + x, j + y, 1];///Alpha-trimmed mean filter
                                                //B
                                        Irgb[x, y, 2] = CopyRGB[i + x, j + y, 2];
                                        MIb[count] = CopyRGB[i + x, j + y, 2];///Alpha-trimmed mean filter

                                        ////////========sum4=========/////////
                                            //Gray
                                        sumContra += Math.Pow(I[x, y], Q + 1);//Contraharmonic mean filter
                                        sumContra_1 += Math.Pow(I[x, y], Q);//Contraharmonic mean filter

                                            //RGB
                                                 //R
                                        sumContraR += Math.Pow(Irgb[x, y, 0], Q + 1);//Contraharmonic mean filter
                                        sumContraR_1 += Math.Pow(Irgb[x, y, 0], Q);//Contraharmonic mean filter
                                                 //G
                                        sumContraG += Math.Pow(Irgb[x, y, 1], Q + 1);//Contraharmonic mean filter
                                        sumContraG_1 += Math.Pow(Irgb[x, y, 1], Q);//Contraharmonic mean filter
                                                 //B
                                        sumContraB += Math.Pow(Irgb[x, y, 2], Q + 1);//Contraharmonic mean filter
                                        sumContraB_1 += Math.Pow(Irgb[x, y, 2], Q);//Contraharmonic mean filter

                                        

                                       //////////////////sum/////////////////////////

                                        sumAlpha = sumAlpha + (int)I[x, y];

                                       //R
                                        sumAlphaR = sumAlphaR + (int)Irgb[x, y, 0];
                                       //G
                                        sumAlphaG = sumAlphaG + (int)Irgb[x, y, 1];
                                       //B
                                        sumAlphaB = sumAlphaB + (int)Irgb[x, y, 2];

                                        
                                        //System.Console.WriteLine("MI = " + MI[count]);
                                        /*
                                        System.Console.WriteLine("MIr = " + MIr[count]);
                                        System.Console.WriteLine("MIg = " + MIg[count]);
                                        System.Console.WriteLine("MIb = " + MIb[count]);
                                        */
                                        count++;
 
                               
                            }
                        }

                       // MI = new int[8] { I[0, 0], I[0, 1], I[0, 2], I[1, 0], I[1, 2], I[2, 0], I[2, 1], I[2, 2] };//x1 ~ x4 x6 ~ x9
                        ////////////////////////////////////////////////////////////////////////////////////////////////////////
                        //////////////////////////////////////////===Alpha filter===////////////////////////////////////

                        //Gray
                        ArrayList list = new ArrayList(MI);

                        list.Sort();//排序陣列
                        sumAlpha = sumAlpha - Convert.ToInt32(list[0]) - Convert.ToInt32(list[8]);

                        Output_Alpha[i, j] = (int)(1 / ((double)mask * (double)mask - (double)d) * sumAlpha);

                    //(int)Math.Round((double)(n * n) / 2)

                        //RGB
                           //R
                        ArrayList listR = new ArrayList(MIr);
                        listR.Sort();//排序陣列
                        sumAlphaR = sumAlphaR - Convert.ToInt32(listR[0]) - Convert.ToInt32(listR[8]);
                        //OutputRGB_Median[i, j, 0] = Convert.ToInt32(listR[5]);
                        OutputRGB_Alpha[i, j, 0] = (int)(sumAlphaR / ((double)mask * (double)mask - (double)d));

                          //G
                        ArrayList listG = new ArrayList(MIg);
                        listG.Sort();//排序陣列
                        sumAlphaG = sumAlphaG - Convert.ToInt32(listG[0]) - Convert.ToInt32(listG[8]);
                        //OutputRGB_Median[i, j, 1] = Convert.ToInt32(listG[5]);
                        OutputRGB_Alpha[i, j, 1] = (int)(sumAlphaG / ((double)mask * (double)mask - (double)d));

                          //B
                        ArrayList listB = new ArrayList(MIb);
                        listB.Sort();//排序陣列
                        //System.Console.WriteLine("sumB = " + sumB);
                        sumAlphaB = sumAlphaB - Convert.ToInt32(listB[0]) - Convert.ToInt32(listB[8]);
                        //OutputRGB_Median[i, j, 2] = Convert.ToInt32(listB[5]);
                        OutputRGB_Alpha[i, j, 2] = (int)(sumAlphaB / ((double)mask * (double)mask - (double)d));
                        //System.Console.WriteLine("sumb = " + sumB/5);



                        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                        ////////////////////////////////////////////////Contraharmonic mean filter/////////////////////////////////////////
                                //Gray
                        Output_Contra[i, j] = (int)Math.Round(sumContra / sumContra_1);
                        if (Output_Contra[i, j] <= 0)
                            Output_Contra[i, j] = 0;
                        else if (Output_Contra[i, j] >= 255)
                            Output_Contra[i, j] = 255;

                               //RGB
                                     //R
                        OutputRGB_Contra[i, j, 0] = (int)Math.Round(sumContraR / sumContraR_1);
                        if (OutputRGB_Contra[i, j, 0] <= 0)
                            OutputRGB_Contra[i, j, 0] = 0;
                        else if (OutputRGB_Contra[i, j, 0] >= 255)
                            OutputRGB_Contra[i, j, 0] = 255;
                                     //G
                        OutputRGB_Contra[i, j, 1] = (int)Math.Round(sumContraG / sumContraG_1);
                        if (OutputRGB_Contra[i, j, 1] <= 0)
                            OutputRGB_Contra[i, j, 1] = 0;
                        else if (OutputRGB_Contra[i, j, 1] >= 255)
                            OutputRGB_Contra[i, j, 1] = 255;
                                     //B
                        OutputRGB_Contra[i, j, 2] = (int)Math.Round(sumContraB / sumContraB_1);
                        if (OutputRGB_Contra[i, j, 2] <= 0)
                            OutputRGB_Contra[i, j, 2] = 0;
                        else if (OutputRGB_Contra[i, j, 2] >= 255)
                            OutputRGB_Contra[i, j, 2] = 255;


                    /////////////Contraharmonic////////
                    /////////////sum4歸零//////////////

                        ////Gray////
                        sumContra = 0.0;
                        sumContra_1 = 0.0;

                        ////RGB////
                            //R
                        sumContraR = 0.0;
                        sumContraR_1 = 0.0;
                            //G
                        sumContraG = 0.0;
                        sumContraG_1 = 0.0;
                            //B
                        sumContraB = 0.0;
                        sumContraB_1 = 0.0;

                    /////////////Alpha-trimmed////////
                    /////////////sum歸零/////////////////
                        sumAlpha = 0;
                        sumAlphaR = 0;
                        sumAlphaG = 0;
                        sumAlphaB = 0;
                    }
            }

            button6.Enabled = true;
            button4.Enabled = true;

        }

        private void button6_Click(object sender, EventArgs e)
        {
            ////Gray////
            show_picture(pictureBox2, Output_Contra);
            Bitmap bm = (Bitmap)pictureBox2.Image;

            bm.Save("Gray_ContrharmonicMF.jpg", ImageFormat.Jpeg);

            ////RGB////
            showRGB_picture(pictureBox4, OutputRGB_Contra);
            Bitmap bmRGB = (Bitmap)pictureBox4.Image;

            bm.Save("RGB_ContrharmonicMF.jpg", ImageFormat.Jpeg);
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            button2.Enabled = true;
        }

    

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void button7_Click(object sender, EventArgs e)
        {
            string src2 = textBox2.Text;
            noise = Int32.Parse(src2);


            Random rand = new Random();
            int noise_num = (int)(h * w * rand.NextDouble()) / noise;
            int x, y, x2, y2;

            for (int i = 0; i < noise_num; i++)
            {
                int row = (int)(rand.NextDouble() * h);
                int col = (int)(rand.NextDouble() * w);


                int nChannels = 1;
                //int nChannelsRGB = 3;
                //int x, y;

                for (int j = 0; j < nChannels; j++)
                {

                    int val = row * w * nChannels + col * nChannels + j + 1;

                    if (val % w == 0)
                    {
                        x = val / w - 1;
                        y = w - 1;
                    }

                    else
                    {
                        x = val / w;
                        y = val % w - 1;
                    }


                    //Gray
                    PepperSalt_image[x, y] = 255;

                    //RGB
                    for (int z = 0; z < 3; ++z)
                        PepperSaltRGB_image[x, y, z] = 255;
                }
                for (i = 0; i < noise_num; i++)
                {
                    row = (int)(rand.NextDouble() * h);
                    col = (int)(rand.NextDouble() * w);


                    for (int j = 0; j < nChannels; j++)
                    {

                        int val2 = row * w * nChannels + col * nChannels + j + 1;

                        if (val2 % w == 0)
                        {
                            x2 = val2 / w - 1;
                            y2 = w - 1;
                        }

                        else
                        {
                            x2 = val2 / w;
                            y2 = val2 % w - 1;
                        }


                        //Gray
                        PepperSalt_image[x2, y2] = 0;

                        //RGB
                        for (int z = 0; z < 3; ++z)
                            PepperSaltRGB_image[x2, y2, z] = 0;
                    }

                }

                //////show picture in picture box//////

                ////Gray////
                show_picture(pictureBox1, PepperSalt_image);
                ////RGB////
                showRGB_picture(pictureBox3, PepperSaltRGB_image);

                
                ////Gray////
                Bitmap bm = (Bitmap)pictureBox1.Image;
                bm.Save("Gray_PepperSalt.jpg", ImageFormat.Jpeg);

                ////RGB////
                Bitmap bmRGB = (Bitmap)pictureBox3.Image;
                bmRGB.Save("RGB_PepperSalt.jpg", ImageFormat.Jpeg);
            }
        }

        private void textBox2_TextChanged(object sender, EventArgs e)
        {
            button7.Enabled = true;
        }

        private void button3_Click(object sender, EventArgs e)
        {
            string src = textBox3.Text;
            enhance = Double.Parse(src);

            //Gray
            Bitmap bm = (Bitmap)pictureBox2.Image;
            Image img = bm;

            //RGB
            Bitmap bmRGB = (Bitmap)pictureBox4.Image;
            Image imgRGB = bmRGB;


            //Gray
            //圖像水平解析度
            float dpiX = img.HorizontalResolution;
            //圖像垂直解析度
            float dpiY = img.VerticalResolution;

            float dpiXrgb = imgRGB.HorizontalResolution;
            float dpiYrgb = imgRGB.VerticalResolution;

            //列印dpi值
            Console.WriteLine("dpiX = " + dpiX +"\n dpiY = " + dpiY +"\n dpiXrgb = " + dpiXrgb + "\n dpiYrgb = " + dpiYrgb);

            Color c;
            int x, y;

            double r, g, b, gray;
            int alpha;


            int [,] ResultR_image;
            int [,] ResultG_image;
            int [,] ResultB_image;

            ResultR_image = new int[h, w];
            ResultG_image = new int[h, w];
            ResultB_image = new int[h, w];

            try
            {
                //讀取圖片像素
                for (x = 0; x < h; x++)
                {
                    for (y = 0; y < w; y++)
                    {
                        c = bmRGB.GetPixel(y, x);  //螢幕上的位置是以 X 和 Y 座標軸來描述，X 座標軸是向右遞增，而 Y 座標軸則是從上到下遞增
                        //original_image[x, y] = c.R;
                        //Gray = 0.299 * Red + 0.587 * Green + 0.114 * Blue
                        b = c.B;
                        g = c.G;
                        r = c.R;
                        alpha = c.A;

                        ResultR_image[x, y] = (int)r;
                        ResultG_image[x, y] = (int)g;
                        ResultB_image[x, y] = (int)b;
                        // System.Console.WriteLine(gray);
                    }
                }
            }
            catch
            {
                System.Console.WriteLine("讀取錯誤321");
            }

            ///////////////RGB分開Enhance////////////

            //R
            show_picture(pictureBox6, ResultR_image);
            Bitmap bmR = (Bitmap)pictureBox6.Image;
            //G
            show_picture(pictureBox6, ResultG_image);
            Bitmap bmG = (Bitmap)pictureBox6.Image;
            //B
            show_picture(pictureBox6, ResultB_image);
            Bitmap bmB = (Bitmap)pictureBox6.Image;


            ///////////Enhance////////////
            ////Gray
            Bitmap bmEnhance = Normalization(bm, enhance, enhance);

            ////RGB
                //R
            Bitmap bmEnhanceR = Normalization(bmR, enhance, enhance);
                //G
            Bitmap bmEnhanceG = Normalization(bmG, enhance, enhance);
                //B
            Bitmap bmEnhanceB = Normalization(bmB, enhance, enhance);

            bmEnhance.Save(enhance + "Gray_contrast.jpg", ImageFormat.Jpeg);
            //bmEnhanceRGB.Save("RGB_contrast.jpg", ImageFormat.Jpeg);

            pictureBox5.Image = Image.FromFile(enhance + "Gray_contrast.jpg");
           // pictureBox6.Image = Image.FromFile("RGB_contrast.jpg");
           

             /////** 讀取pixel值 **/////
            //處理像素資料用物件容器

            Color R, G, B;

            Result_image = new int[h, w];
            ResultRGB_image = new int[h, w, 3];
            try
            {
                //讀取圖片像素
                for (x = 0; x < h; x++)
                {
                    for (y = 0; y < w; y++)
                    {
                        c = bmEnhance.GetPixel(y, x);  //螢幕上的位置是以 X 和 Y 座標軸來描述，X 座標軸是向右遞增，而 Y 座標軸則是從上到下遞增

                        R = bmEnhanceR.GetPixel(y, x);
                        G = bmEnhanceG.GetPixel(y, x);
                        B = bmEnhanceB.GetPixel(y, x);
                        //original_image[x, y] = c.R;
                        //Gray = 0.299 * Red + 0.587 * Green + 0.114 * Blue
                        b = c.B;
                        g = c.G;
                        r = c.R;
                        alpha = c.A;


                        gray = 0.299 * r + 0.587 * g + 0.114 * b;

                        ////Gray////
                        Result_image[x, y] = (int)gray;

                        ////RGB////
                            //R
                        ResultRGB_image[x, y, 0] = (int)(0.299 * R.B + 0.587 * R.G + 0.114 * R.A);
                            //G
                        ResultRGB_image[x, y, 1] = (int)(0.299 * G.B + 0.587 * G.G + 0.114 * G.A);
                            //B
                        ResultRGB_image[x, y, 2] = (int)(0.299 * B.B + 0.587 * B.G + 0.114 * B.A);

                    }
                }
            }
            catch
            {
                System.Console.WriteLine("讀取錯誤321");
            }

            //show picture in picture box
            showRGB_picture(pictureBox6, ResultRGB_image);

            Bitmap bmEnhanceRGB = (Bitmap)pictureBox6.Image;
            bmEnhanceRGB.Save(enhance + "RGB_enhance.jpg", ImageFormat.Jpeg);

            PSNR_process(original_image, Output_Contra);
            PSNR_process(original_image, Result_image);

        }

        private void textBox3_TextChanged(object sender, EventArgs e)
        {
            button3.Enabled = true;
        }

        private void button4_Click(object sender, EventArgs e)
        {
            ////Gray////
            show_picture(pictureBox2, Output_Alpha);
            Bitmap bm = (Bitmap)pictureBox2.Image;

            bm.Save("Gray_AlphaMF.jpg", ImageFormat.Jpeg);

            ////RGB////
            showRGB_picture(pictureBox4, OutputRGB_Alpha);
            Bitmap bmRGB = (Bitmap)pictureBox4.Image;

            bm.Save("RGB_AlphaMF.jpg", ImageFormat.Jpeg);
            //button3.Enabled = true;
        }


    }
}

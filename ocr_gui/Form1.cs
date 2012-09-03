using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

using System.IO;
using System.Threading;
using System.Text.RegularExpressions;
using System.Reflection;

namespace ocr_gui
{
    public partial class Form1 : Form
    {
        bool validData;
        Thread getImageThread;
        string dragFilename = "";
        Image nextImage;
        int lastX = 0;
        int lastY = 0;
        Parser parser = new Parser();

        public Form1()
        {
            // This program can work as a command line application by changing
            // Project->Properties->Application->Output type = Console Application
            // but this setting has the side effect of opening a console window 
            // when launched from Windows.

            InitializeComponent();

            Assembly asm = Assembly.GetExecutingAssembly();
            Version v = Assembly.GetExecutingAssembly().GetName().Version;
            DateTime compileDate = new DateTime(v.Build * TimeSpan.TicksPerDay + v.Revision * TimeSpan.TicksPerSecond * 2).AddYears(1999);
            if (TimeZone.IsDaylightSavingTime(compileDate, TimeZone.CurrentTimeZone.GetDaylightChanges(compileDate.Year)))
            {
                compileDate = compileDate.AddHours(1);
            }
            this.labelVersion.Text = String.Format("Build {0}", compileDate);

        }

        private void imagePanel_DragEnter(object sender, DragEventArgs e)
        {
            validData = GetFilename(out dragFilename, e);
            if (validData)
            {
                thumbnail.Image = null;
                thumbnail.Visible = false;

                getImageThread = new Thread(new ThreadStart(LoadImage));
                getImageThread.Start();

                e.Effect = DragDropEffects.Copy;
            }
            else
            {
                e.Effect = DragDropEffects.None;
            }

        }
        bool GetFilename(out string filename, DragEventArgs e)
        {
            bool ret = false;
            filename = String.Empty;

            if ((e.AllowedEffect & DragDropEffects.Copy) == DragDropEffects.Copy)
            {
                Array data = e.Data.GetData("FileDrop") as Array;
                if (data != null)
                {
                    if ((data.Length == 1) && (data.GetValue(0) is String))
                    {
                        filename = ((string[])data)[0];
                        string ext = Path.GetExtension(filename).ToLower();
                        if ((ext == ".jpg") || (ext == ".png") || (ext == ".bmp") || (ext==".tif"))
                        {
                            ret = true;
                        }
                    }
                }
            }
            return ret;
        }
        private void imagePanel_DragLeave(object sender, EventArgs e)
        {
            thumbnail.Visible = false;
        }

        public delegate void AssignImageDlgt();

        Image LoadUnlockImage(string asFile)
        {
            // This is necessary because Image constructor ordinarily
            // keeps file locked after opening
            Stream BitmapStream = File.Open(asFile, FileMode.Open);
            Image img = Image.FromStream(BitmapStream);
            BitmapStream.Close();

            return new Bitmap(img);
        }

        void LoadImage()
        {
            nextImage = LoadUnlockImage(dragFilename);
            this.Invoke(new AssignImageDlgt(AssignImage));
        }

        void AssignImage()
        {
            thumbnail.Width = 100;
            thumbnail.Height = thumbnail.Width * nextImage.Height / nextImage.Width;
            SetThumbnailLocation(splitContainer1.Panel2.PointToClient(new Point(lastX, lastY)));
            thumbnail.Image = nextImage;
        }

        void SetThumbnailLocation(Point p)
        {
            if (thumbnail.Image == null)
            {
                thumbnail.Visible = false;
            }
            else
            {
                p.X -= thumbnail.Width / 2;
                p.Y -= thumbnail.Height / 2;
                thumbnail.Location = p;
                thumbnail.Visible = true;
            }
        }

        private void imagePanel_DragDrop(object sender, DragEventArgs e)
        {
            if (validData)
            {
                while (getImageThread.IsAlive)
                {
                    Application.DoEvents();
                    Thread.Sleep(0);
                }
                thumbnail.Visible = false;
                label1.Visible = false;
                pb.Image = nextImage;
                Bitmap bmp = new Bitmap(nextImage);
                try
                {
                    textBox.Text = parser.parse(bmp);
                }
                catch (Exception ex)
                {
                    textBox.Text = ex.Message + "\r\n" + ex.StackTrace;
                }
                textBox.Text= "DLP="+DLPparse(textBox.Text)+"\r\n"+textBox.Text;
            }
        }

        private void imagePanel_DragOver(object sender, DragEventArgs e)
        {
            if (validData)
            {
                if ((e.X != lastX) || (e.Y != lastY))
                {
                    SetThumbnailLocation(splitContainer1.Panel2.PointToClient(new Point(e.X, e.Y)));
                    lastX = e.X;
                    lastY = e.Y;
                }
            }
        }

        private string DLPparse(string inputText)
        {
            string[] DLPregex = {@"Total Exam DLP:.*?(\d+(\.\d*)?)", // GE
                                 @" DLP\(mGycm\).*?(\d+(\.\d*)?)", // Toshiba
                                 @"Total DLP.*?(\d+(\.\d*)?)" }; // Sensation
            string returnValue = "";
            foreach (string s in DLPregex)
            {
                Match m = Regex.Match(inputText, s, RegexOptions.Multiline);
                if (m.Success)
                {
                    returnValue = m.Groups[1].Value;
                }
            }
            return returnValue;
            
        }

    
    }
}

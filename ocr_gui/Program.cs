using System;
using System.Collections.Generic;
using System.Windows.Forms;

using System.Drawing;

namespace ocr_gui
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            if (args.Length > 0)
            {
                if (args.Length > 1)
                {
                    Console.Error.WriteLine("Usage: " + Environment.GetCommandLineArgs()[0] + " imagename");
                    Environment.Exit(1);
                }
                try
                {
                    string imagefile = args[0];
                    Bitmap bmp = new Bitmap(imagefile);
                    Parser parser = new Parser();
                    Console.WriteLine(parser.parse(bmp));
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine(ex.Message);
                    Environment.Exit(1);
                }
            }
            else
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new Form1());
            }
        }

    }
}

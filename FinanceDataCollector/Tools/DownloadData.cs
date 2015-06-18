using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FinanceDataCollector.Tools
{
    public static class DownloadData
    {
        private static FtpClient ftpClient = new FtpClient("ftp://ftp.moex.com", "", "");
        public static void Download()
        {
            DateTime start = DateTime.Now;
            string[] dirs;

            ftpClient.ChangeWorkingDirectory("pub/info/stats/history/F/2006/");
            for (int i = 2006; i <= DateTime.Now.Year; i++)
            {
                ftpClient.ChangeWorkingDirectory("../" + i + "/");//Меняем урл на /F/2007, 2008 etc.
                dirs = ftpClient.ListDirectory();

                if (i == 2006)
                    dirs = dirs.Skip(454).ToArray();//В 2006 данные которые нужны нам идут только с ноября, пропускаем все данные до ноября.
                foreach (string dir in dirs)
                {
                    if (dir.StartsWith("ft") || dir.StartsWith("FT"))
                    {
                        try
                        {
                            if (!Directory.Exists(Properties.Settings.Default.outputPath + "/" + i))
                                Directory.CreateDirectory(Properties.Settings.Default.outputPath + "/" + i);
                            if (!File.Exists(Properties.Settings.Default.outputPath + "/" + i + "/" + dir))
                            {
                                ftpClient.DownloadFile(dir, Properties.Settings.Default.outputPath + "/" + i + "/" + dir);
                                Console.WriteLine("\n{0} downloaded successfully!", dir);
                            }
                            else
                            {
                                Console.WriteLine("\n{0} already downloaded.", dir);
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("\n{0} downloading error. {1}", dir, ex.Message);
                        }
                    }
                }
            }
            Console.WriteLine("Data downloaded successfully! Elapsed time: {0}", DateTime.Now - start);
        }
    }
}

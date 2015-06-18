using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using FinanceDataCollector.Tools;
using System.Timers;
using FinanceDataCollector.Properties;
using System.Globalization;

namespace FinanceDataCollector
{
    class Program//Начало с 2006 года
    {
        private static Timer _timer;
        static void Main(string[] args)
        {
            Console.Title = "Finance Data Collector";
            DateTime tm;
            if(DateTime.Now.TimeOfDay < Settings.Default.readTime)
                tm = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, Settings.Default.readTime.Hours, Settings.Default.readTime.Minutes, Settings.Default.readTime.Seconds);
            else
                tm = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day + 1, Settings.Default.readTime.Hours, Settings.Default.readTime.Minutes, Settings.Default.readTime.Seconds);

            _timer = new Timer((tm - DateTime.Now).TotalMilliseconds);
            _timer.AutoReset = false;
            _timer.Elapsed += _timer_Elapsed;
            _timer.Start();

            main:
            showMenu();
            goto main;
        }

        static void _timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            DownloadData.Download();

            Data.WriteInDB();

            GC.Collect();//Собираем хлам.

            DateTime tm;//Ниже на всякий случай, хотя быть такого не должно.
            if (DateTime.Now.TimeOfDay < Settings.Default.readTime)
                tm = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, Settings.Default.readTime.Hours, Settings.Default.readTime.Minutes, Settings.Default.readTime.Seconds);
            else
                tm = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day + 1, Settings.Default.readTime.Hours, Settings.Default.readTime.Minutes, Settings.Default.readTime.Seconds);

            _timer.Interval = (DateTime.Now - tm).TotalMilliseconds;
            _timer.Elapsed += _timer_Elapsed;
            _timer.Start();
        }

        public static void showMenu()
        {
            string line = "";
            Console.WriteLine("\n1 - Download data" +
                              "\n2 - Parse data and write in database" +
                              "\n3 - Parse data and write in file" +
                              "\n4 - Calculate candlesticks" +
                              "\n5 - Drop database" +
                              "\n6 - Read and write codes of the securities" +
                              "\n7 - Settings" +
                              "\n0 - Exit\n");

            line = Console.ReadLine();
            if (line == "1")
            {
                if (Settings.Default.mode == 0)
                {
                Downloading:
                    Console.WriteLine("\nWrite data in database after downloading? (Y/N)");
                    line = Console.ReadLine();
                    if (line == "y" || line == "Y")
                    {
                        Data.WriteInDB();
                    }
                    else if (line == "n" || line == "N")
                    {
                        DownloadData.Download();
                    }
                    else
                    {
                        goto Downloading;
                    }
                }
                else
                {
                    DownloadData.Download();
                    Db.DropDatabase();
                    Data.WriteInDB();
                }

            }
            else if (line == "2")
            {
                Data.WriteInDB();
            }
            else if (line == "3")
            {
                Data.WriteInFile();
            }
            else if (line == "4")
            {

            }
            else if (line == "5")
            {
                Db.DropDatabase();
                showMenu();
            }
            else if (line == "6")
            {
                Data.WriteCodesInDb();
            }
            else if (line == "7")
            {
                showSettings();
            }
            else if (line == "0")
            {
                Environment.Exit(0);
            }
            else
                showMenu();
            Console.ReadKey();
        }
        public static void showSettings()
        {
            Console.WriteLine("\n\t\t\t --- Settings --- \t\t\t");
            Console.WriteLine("\nEnter number to change setting value: " +
                              "\n1 - DB IP\t|\t" + Settings.Default.ip +
                              "\n2 - DB Port\t|\t" + Settings.Default.port +
                              "\n3 - DB Login\t|\t" + Settings.Default.login +
                              "\n4 - DB Password\t|\t" + Settings.Default.password +
                              "\n5 - DB Name\t|\t" + Settings.Default.dbname +
                              "\n6 - Read Time\t|\t" + Settings.Default.readTime +
                              "\n7 - Output Path\t|\t" + Settings.Default.outputPath +
                              "\n8 - Mode\t|\t" + (Settings.Default.mode == 0 ? "Manual" : "Auto") +
                              "\n0 - Return to main menu.");
            string line = Console.ReadLine();
            if (line == "1")
            {
                Console.WriteLine("\nEnter new ip address or domain: ");
                Settings.Default.ip = Console.ReadLine();
                Settings.Default.Save();
                Console.WriteLine("\nIP address changed successfully.");
                showSettings();

            }
            else if (line == "2")
            {
                prt:
                Console.WriteLine("\nEnter new port (digits only, 0 for exit): ");

                int newport = 0;
                bool p = Int32.TryParse(Console.ReadLine(), out newport);
                if (p)
                {
                    if (newport != 0)
                    {
                        Settings.Default.port = newport;
                        Settings.Default.Save();
                        Console.WriteLine("\nPort changed successfully.");
                        showSettings();
                    }
                    else
                    {
                        Console.WriteLine("\nInput was canceled by user.");
                        showSettings();
                    }
                }
                else
                {
                    Console.WriteLine("\nIncorrect value, try again.");
                    goto prt;
                }
            }
            else if (line == "3")
            {
                Console.WriteLine("\nEnter new login: ");
                Settings.Default.dbname = Console.ReadLine();
                Settings.Default.Save();
                Console.WriteLine("\nDatabase login changed successfully.");
                showSettings();

            }
            else if (line == "4")
            {
                Console.WriteLine("\nEnter new database password: ");
                Settings.Default.password = Console.ReadLine();
                Settings.Default.Save();
                Console.WriteLine("\nDatabase password changed successfully.");
                showSettings();

            }
            else if (line == "5")
            {
                Console.WriteLine("\nEnter new database name: ");
                Settings.Default.dbname = Console.ReadLine();
                Settings.Default.Save();
                Console.WriteLine("\nDatabase name changed successfully.");
                showSettings();
            }
            else if (line == "6")
            {
                nt:
                Console.WriteLine("\nEnter new read time (hh:mm:ss): ");
                TimeSpan newtime;
                if (TimeSpan.TryParse(Console.ReadLine(), out newtime))
                {
                    Settings.Default.readTime = newtime;//
                    Settings.Default.Save();
                    Console.WriteLine("\nRead time changed successfully.");
                    showSettings();
                }
                else
                {
                    Console.WriteLine("\nIncorrect format, try again.");
                    goto nt;
                }

            }
            else if (line == "7")
            {
                Console.WriteLine("\nEnter new output path: ");
                Settings.Default.outputPath = Console.ReadLine();
                Settings.Default.Save();
                Console.WriteLine("\nOutput path changed successfully.");
                showSettings();


            }
            else if (line == "8")
            {
                Console.WriteLine("\nEnter new mode (0 - manual, 1 - auto): ");
                int newmode = 0;
                md:
                bool p = Int32.TryParse(Console.ReadLine(), out newmode);
                if (p)
                {
                    Settings.Default.mode = newmode;
                    Settings.Default.Save();
                    Console.WriteLine("\nPort changed successfully.");
                    showSettings();
                }
                else
                {
                    Console.WriteLine("\nIncorrect value, try again.");
                    goto md;
                }

            }
            else if (line == "0")
            {
                showMenu();
            }
            
        }
    }
}

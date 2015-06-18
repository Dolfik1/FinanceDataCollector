using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FinanceDataCollector.Model;
using FinanceDataCollector.Properties;
using Ionic.Crc;
using Ionic.Zip;

namespace FinanceDataCollector.Tools
{
    public static class Data
    {
        private static CultureInfo ci = new CultureInfo("en");
        public static void WriteInDB()
        {

            if (!Db.CheckConnection())
            {
                Db.Connect();
            }

            Console.WriteLine("\nWrite in DB started!");
            DateTime dat_timeStart;
            DateTime start = DateTime.Now;
            List<string> codeFilter = new List<string>();
            string line;
            if (Settings.Default.mode == 0)
            {
            EnterDate:
                Console.WriteLine("\nEnter date time (dd.MM.yyyy hh:mm:ss) or empty field (take last Db element)");
                line = Console.ReadLine();

                if (String.IsNullOrWhiteSpace(line))
                {
                    start = DateTime.Now;
                    Console.WriteLine("Getting the last transaction...\n");
                    dat_timeStart = Db.GetLastTransaq("moex").dat_time;
                    Console.WriteLine("Last transaction recieved! Elapsed time: {0}", DateTime.Now - start);
                }
                else
                {
                    if (DateTime.TryParseExact(line, "dd.MM.yyyy hh:mm:ss", null, DateTimeStyles.None, out dat_timeStart)) { }
                    else
                    {
                        Console.WriteLine("\nError. Incorrect date time format.");
                        goto EnterDate;
                    }
                }
            enterCode:
                Console.WriteLine("\nEnter securities codes separated by commas (sample GZZ6, ESZ6, RI60000L6) or leave field empty");
                Console.WriteLine("If you can see list of codes, write \"LIST\"\n");

                line = Console.ReadLine();

                if (line == "LIST" || line == "\"LIST\"" || line == "list" || line == "\"list\"" || line == "List" || line == "\"List\"")//Чтобы совсем прокатило
                {
                    List<fcode> cds = new List<fcode>(Db.GetCodes());
                    StringBuilder sb = new StringBuilder();
                    Console.WriteLine("\t\t\t --- Codes --- \t\t\t");
                    foreach (fcode cd in cds)
                    {
                        sb.AppendFormat("\n{0}\n", cd.code);
                    }
                    Console.WriteLine(sb.ToString());
                    goto enterCode;
                }
                else
                {
                    if (!String.IsNullOrWhiteSpace(line))
                    {
                        line = line.Replace(" ", "");
                        codeFilter = new List<string>(line.Split(','));
                    }
                }
            }
            else
            {
                dat_timeStart = DateTime.MinValue;
            }
            
            ci.NumberFormat.CurrencyDecimalSeparator = ".";
            if (Directory.Exists(@"Files\historydata"))
            {
                string[] dirs = Directory.GetDirectories(@"Files\historydata");

                ZipFile zipFile;
                List<ZipEntry> zipEntry;
                
                List<transaction> transData = new List<transaction>();
                //string colName = "";

                int year = 0;
                bool parseresult = false;
                string fileDateStr = "";

                string[] lineData;
                string[][] csvData;

                if (dirs.Count() > 0)
                {
                    Console.WriteLine("Last transaction date: {0}", dat_timeStart);
                    foreach (string dir in dirs)
                    {
                        parseresult = Int32.TryParse(dir.Substring(dir.LastIndexOf(@"\") + 1), out year);
                        if (parseresult == true && year >= dat_timeStart.Year)//Пропускаем лишние года
                        {
                            string[] files = Directory.GetFiles(dir);
                            foreach (string file in files)
                            {
                                if (file.EndsWith(".ZIP") || file.EndsWith(".zip"))
                                {
                                    fileDateStr = "20";

                                    fileDateStr += file.Substring(file.LastIndexOf(@"\") + 3);
                                    fileDateStr = fileDateStr.Remove(fileDateStr.Length - 4);

                                    DateTime fileDate = DateTime.ParseExact(fileDateStr, "yyyyMMdd", null);

                                    if (fileDate > dat_timeStart)//Пропускаем уже записанные архивы
                                    {
                                        Console.WriteLine("{0} read started!", file.Substring(file.LastIndexOf(@"\") + 1));
                                        try
                                        {
                                            using (zipFile = ZipFile.Read(file))
                                            {
                                                zipEntry = new List<ZipEntry>(zipFile.Entries);
                                                transData.Clear();
                                                foreach (ZipEntry entr in zipEntry)
                                                {
                                                    CrcCalculatorStream stream = entr.OpenReader();
                                                    StreamReader sr = new StreamReader(stream);
                                                    lineData = sr.ReadToEnd().Split('\n');
                                                    csvData = lineData.Select(x => x.Split(';')).ToArray();

                                                    Console.WriteLine("{0} reading...", entr.FileName);

                                                    for (int i = 1; i < csvData.Length - 1; i++)//Идём по записям, начиная с первого индекса, так как нулевой заголовки, а -1 так как последняя строка пустая
                                                    {
                                                        try
                                                        {
                                                            if (codeFilter.Count == 0 || codeFilter.Any(x => x == csvData[i][0]) && csvData[i][6] == "\r")
                                                            {
                                                                transaction trans = new transaction();
                                                                trans.code = csvData[i][0];
                                                                trans.contract = csvData[i][1];
                                                                trans.price = double.Parse(csvData[i][2], ci);
                                                                trans.amount = Int32.Parse(csvData[i][3]);
                                                                trans.dat_time = DateTime.Parse(csvData[i][4]);
                                                                trans.trade_id = Int32.Parse(csvData[i][5]);
                                                                transData.Add(trans);
                                                            }
                                                        }
                                                        catch (Exception ex)
                                                        {
                                                            Console.WriteLine(ex);
                                                        }
                                                    }

                                                    sr.Dispose();

                                                    Console.WriteLine("{0} readed!", entr.FileName);
                                                }

                                                if (transData.Count > 0)
                                                {
                                                    var transGrouped = transData.OrderBy(x => x.dat_time).GroupBy(x => x.code);

                                                    foreach (var grp in transGrouped)
                                                    {
                                                        Db.AddRecs(grp, grp.First().code);
                                                    }
                                                }
                                                else
                                                {
                                                    Console.WriteLine("Collection already updated!");
                                                }
                                                //zipFile.Dispose();//Должен и так вызваться
                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            Console.WriteLine("\nError: {0}", ex.Message);
                                        }
                                    }
                                    else
                                    {

                                        Console.WriteLine("{0} Already writed!", file);
                                    }
                                }
                                else
                                {
                                    Console.WriteLine("{0} skipped!", file);
                                }
                            }
                        }
                    }
                }
                else
                {
                    if (Settings.Default.mode == 0)
                    {
                        ask:
                        Console.WriteLine("Data not found. Download data from server? (Y/N)");
                        string ln = Console.ReadLine();
                        if (ln == "Y" || ln == "y")
                        {
                            DownloadData.Download();
                            WriteInDB();
                        }
                        else if (ln == "N" || ln == "n")
                        {
                            Program.showMenu();
                        }
                        else
                            goto ask;
                    }
                    else
                    {
                        DownloadData.Download();
                        WriteInDB();
                    }
                }
            }
            else
            {
                if (Settings.Default.mode == 0)
                {
                    ask:
                    Console.WriteLine("Data not found. Download data from server? (Y/N)");
                    string ln = Console.ReadLine();
                    if (ln == "Y" || ln == "y")
                    {
                        DownloadData.Download();
                        WriteInDB();
                    }
                    else if (ln == "N" || ln == "n")
                    {
                        Program.showMenu();
                    }
                    else
                        goto ask;
                }
                else
                {
                    DownloadData.Download();
                    WriteInDB();
                }
                
            }
            Console.WriteLine("Data writed in DB successfully! Elapsed time: {0}", DateTime.Now - start);
            Program.showMenu();
        }

        public static void WriteInFile()//Переделать
        {
        }

        public static void WriteCodesInDb()
        {
            if (!Db.CheckConnection())
            {
                Db.Connect();
            }

            Console.WriteLine("\nCode parse started!");
            DateTime start = DateTime.Now;
            List<fcode> codes = new List<fcode>();
            List<fcode> exestingCodes = new List<fcode>(Db.GetCodes());

            if (Directory.Exists(@"Files\historydata"))
            {
                string[] dirs = Directory.GetDirectories(@"Files\historydata");

                ZipFile zipFile;
                List<ZipEntry> zipEntry;
                //string colName = "";

                int year = 0;
                bool parseresult = false;
                string fileDateStr = "";

                string[] lineData;
                string[][] csvData;

                if (dirs.Count() > 0)
                {
                    foreach (string dir in dirs)
                    {
                        parseresult = Int32.TryParse(dir.Substring(dir.LastIndexOf(@"\") + 1), out year);
                        if (parseresult == true)//Пропускаем лишние года
                        {
                            string[] files = Directory.GetFiles(dir);
                            foreach (string file in files)
                            {
                                if (file.EndsWith(".ZIP") || file.EndsWith(".zip"))
                                {
                                    fileDateStr = "20";

                                    fileDateStr += file.Substring(file.LastIndexOf(@"\") + 3);
                                    fileDateStr = fileDateStr.Remove(fileDateStr.Length - 4);

                                    DateTime fileDate = DateTime.ParseExact(fileDateStr, "yyyyMMdd", null);
                                    Console.WriteLine("{0} read started!", file.Substring(file.LastIndexOf(@"\") + 1));

                                    try
                                    {
                                        using (zipFile = ZipFile.Read(file))
                                        {
                                            zipEntry = new List<ZipEntry>(zipFile.Entries);
                                            codes.Clear();
                                            foreach (ZipEntry entr in zipEntry)
                                            {
                                                CrcCalculatorStream stream = entr.OpenReader();
                                                StreamReader sr = new StreamReader(stream);
                                                lineData = sr.ReadToEnd().Split('\n');
                                                csvData = lineData.Select(x => x.Split(';')).ToArray();

                                                Console.WriteLine("{0} reading...", entr.FileName);

                                                for (int i = 1; i < csvData.Length - 1; i++)//Идём по записям, начиная с первого индекса, так как нулевой заголовки, а -1 так как последняя строка пустая
                                                {
                                                    try
                                                    {

                                                        if (!exestingCodes.Any(x => x.code == csvData[i][0]) && !codes.Any(x => x.code == csvData[i][0]))
                                                        {
                                                            fcode fc = new fcode();
                                                            fc.code = csvData[i][0];
                                                            codes.Add(fc);
                                                        }
                                                    }
                                                    catch (Exception ex)
                                                    {
                                                        Console.WriteLine(ex);
                                                    }
                                                }

                                                sr.Dispose();

                                                Console.WriteLine("{0} readed!", entr.FileName);
                                            }

                                            if (codes.Count > 0)
                                            {
                                                Db.AddRecs(codes.OrderBy(x => x.code), "codes");
                                            }
                                            else
                                            {
                                                Console.WriteLine("Collection already updated!");
                                            }

                                            //zipFile.Dispose();
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        Console.WriteLine("Error: {0}", ex.Message);
                                    }
                                }
                                else
                                {
                                    Console.WriteLine("{0} skipped! Not valid format.", file);
                                }
                            }
                        }
                    }
                }
                else
                {
                    if (Settings.Default.mode == 0)
                    {
                    ask:
                        Console.WriteLine("Data not found. Download data from server? (Y/N)");
                        string ln = Console.ReadLine();
                        if (ln == "Y" || ln == "y")
                        {
                            DownloadData.Download();
                            WriteCodesInDb();
                        }
                        else if (ln == "N" || ln == "n")
                        {
                            Program.showMenu();
                        }
                        else
                            goto ask;
                    }
                    else
                    {
                        DownloadData.Download();
                        WriteCodesInDb();
                    }
                }
            }
            else
            {
                if (Settings.Default.mode == 0)
                {
                ask:
                    Console.WriteLine("Data not found. Download data from server? (Y/N)");
                    string ln = Console.ReadLine();
                    if (ln == "Y" || ln == "y")
                    {
                        DownloadData.Download();
                        WriteCodesInDb();
                    }
                    else if (ln == "N" || ln == "n")
                    {
                        Program.showMenu();
                    }
                    else
                        goto ask;
                }
                else
                {
                    DownloadData.Download();
                    WriteCodesInDb();
                }
            }
            Console.WriteLine("Codes writed in DB successfully! Elapsed time: {0}", DateTime.Now - start);
            Program.showMenu();
        }

        public static void CalculateCandlesticks()
        {
            Console.WriteLine("\nEnter timeframe (M1, M5, M15, M30, H1, H4, D1, W1, MN");
            string line = Console.ReadLine();
            
            
        }
    }
}

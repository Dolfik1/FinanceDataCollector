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
using MongoDB.Driver;
using MongoDB.Driver.Linq;

namespace FinanceDataCollector.Tools
{
    public static class Db
    {
        private static MongoServer _mServer;
        private static MongoDatabase _database;
        private static MongoCollection<transaction> _currentCollection;

        //private static MongoInsertOptions insertOpt = new MongoInsertOptions();

        public static void Connect()
        {
            try
            {
                Console.WriteLine("\nConnecting to database...");
                MongoClient client = new MongoClient("mongodb://" + Properties.Settings.Default.login + ":" +
                                            Properties.Settings.Default.password + "@" + Properties.Settings.Default.ip +
                                            ":" + Properties.Settings.Default.port + "/?safe=true");

                _mServer = client.GetServer();
                _mServer.Ping();

                Console.WriteLine("Connected to database successfully!");
                _database = _mServer.GetDatabase(Properties.Settings.Default.dbname);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Connected to database failed! {0}", ex.Message);

                if (Settings.Default.mode == 0)
                {
                ReconnectReq:
                    Console.WriteLine("Reconnect? (Y/N)\n");
                    string line = Console.ReadLine();
                    if (line == "Y" || line == "y")
                    {
                        Connect();
                    }
                    else if (line == "N" || line == "n")
                    {
                        Program.showMenu();
                    }
                    else
                    {
                        goto ReconnectReq;
                    }
                }
                else
                {
                    Console.WriteLine("Reconnecting...");
                    Connect();
                }


            }

        }

        public static bool CheckConnection()
        {
            if (_mServer != null)
            {
                try
                {
                    _mServer.Ping();
                    return true;
                }
                catch
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        public static IEnumerable<MongoCollection<transaction>> GetTransHistory()
        {
            List<MongoCollection<transaction>> result = new List<MongoCollection<transaction>>();
            List<string> collectionNames = new List<string>(_database.GetCollectionNames());
            foreach (string name in collectionNames)
            {
                result.Add(_database.GetCollection<transaction>(name));
            }
            return result;
        }

        public static void AddRecs(IEnumerable<object> data, string collectionName)
        {
            try
            {
                MongoCollection<object> col = _database.GetCollection<object>(collectionName);
                if (col != null)
                {
                    col.InsertBatch(data);
                }
                else
                {
                    _database.CreateCollection(collectionName);
                    col = _database.GetCollection<object>(collectionName);
                    col.InsertBatch(data);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Insert data error. {0}", ex.Message);
                AddRecs(data, collectionName);//Почти рекурсия :)
            }
        }

        public static transaction GetLastTransaq(string collectionName)
        {
            _currentCollection = _database.GetCollection<transaction>(collectionName);
            transaction trans = _currentCollection.AsQueryable<transaction>().LastOrDefault();
            if (trans != null)
                return trans;
            else
                return new transaction();
        }

        public static IEnumerable<fcode> GetCodes()
        { 
            List<fcode> codes = new List<fcode>();
            if (_database != null)
            {
                if (!CheckConnection())
                    Connect();
                if (_database.CollectionExists("codes"))
                {
                    MongoCollection<fcode> col = _database.GetCollection<fcode>("codes");
                    MongoCursor cursor = col.FindAll();

                    foreach (fcode code in cursor)
                    {
                        codes.Add(code);
                    }
                    
                    return codes;
                }
                else
                {
                    _database.CreateCollection("codes");
                    return codes;
                }
            }
            else
            {
                Connect();
            }
            return codes;
        }

        /*public static List<transaction> GetBatch(string collectionName)
        {

            MongoCollection<transaction> col = database.GetCollection<transaction>(collectionName);
        }*/

        public static void DropDatabase()
        {
            if (CheckConnection())
            {
                Console.WriteLine("Dropping database...");
                _database.Drop();
                Console.WriteLine("Database dropping successfully!");
            }
            else
            {
                Connect();
                DropDatabase();
            }
        }
    }
}

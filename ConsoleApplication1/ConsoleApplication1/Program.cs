using System;
using System.Collections.Generic;
using System.IO;
using System.Data.OleDb;
using System.Data;
using System.Data.Odbc;
using CommissionParser;

public class CommandLine
{
    public static void ReadwithOleDb(string fileName)
    //Was ignoring sections of data from the spreadsheet after certain values were encountered. Working but unusable methodology//
    {
        var connectionString = string.Format("Provider=Microsoft.Jet.OLEDB.4.0; data source={0}; Extended Properties=Excel 8.0;", fileName);

        var adapter = new OleDbDataAdapter("SELECT * FROM [CC$]", connectionString); //Calls information from the first tab.//
        var ds = new DataTable();

        adapter.Fill(ds);

        foreach (DataRow row in ds.Rows)
        {
            foreach (object col in row.ItemArray)//Places each item from the column into a comma delimited row output.//
            {
                Console.Write(col);
                Console.Write(",");
            }
            Console.WriteLine();
        }

        Console.WriteLine(fileName);
    }
    public static void ReadwithODBC(string fileName)
    //Also ignoring sections of data from the spreadsheet after certain values. Again, working but unusable methodology//
    {
        string dbConnStr = @"Driver={Microsoft Excel Driver (*.xls)};driverid=790;dbq=";

        dbConnStr = string.Concat(dbConnStr, fileName);

        OdbcCommand cmd = new OdbcCommand("Select * from [CC$]", new OdbcConnection(dbConnStr));

        cmd.Connection.Open();

        OdbcDataReader dr = cmd.ExecuteReader();

        foreach (IDataRecord item in dr)
        {
            object[] cols = new object[item.FieldCount];
            item.GetValues(cols);

            foreach (object col in cols)
            {
                Console.Write(col);
                Console.Write(",");
            }
            Console.WriteLine();
        }
    }
    static void Main(string[] args)
    {
        string rootDir = @"C:\Users\Drew\Desktop\Web Dev\CommissionsApp\CommissionTestProject\CommissionTestProject\InputFiles";
        string[] inputCSVDirectories = new string[3];
        inputCSVDirectories[0] = rootDir + @"\DownloadedCSV\Sales";
        inputCSVDirectories[1] = rootDir + @"\DownloadedCSV\SalesPeople";
        inputCSVDirectories[2] = rootDir + @"\DownloadedCSV\Products";
        string[] outputJsonDirectories = new string[3];
        outputJsonDirectories[0] = rootDir + @"\JsonObj\Sales\";
        outputJsonDirectories[1] = rootDir + @"\JsonObj\SalesPeople\";
        outputJsonDirectories[2] = rootDir + @"\JsonObj\Products\";


        for (int i = 0; i < inputCSVDirectories.Length; i++)
        {
            if (Directory.Exists(inputCSVDirectories[i]))
            {
                string[] files = Directory.GetFiles(inputCSVDirectories[i]);
                foreach (string file in files)
                {
                    Console.WriteLine("Found File: {0}", file);
                    switch (i)
                    {
                        case 0:
                            Console.WriteLine("Parse Sales");
                            string jsonSales = CommissionParserSale.CreateJsonSales(file);
                            string fn = Path.GetFileNameWithoutExtension(file);
                            string fullfilename = outputJsonDirectories[0] + fn + ".json";
                            File.WriteAllText(fullfilename, jsonSales);
                            break;
                        case 1:
                            Console.WriteLine("Parse SalesPeople");
                            string jsonPerson = CommissionParserPerson.CreateJsonPerson(file);
                            fn = Path.GetFileNameWithoutExtension(file);
                            fullfilename = outputJsonDirectories[1] + fn + ".json";
                            File.WriteAllText(fullfilename, jsonPerson);
                            break;
                        case 2:
                            Console.WriteLine("Parse Products");
                            string jsonProduct = CommissionParserProduct.CreateJsonProduct(file);
                            fn = Path.GetFileNameWithoutExtension(file);
                            fullfilename = outputJsonDirectories[2] + fn + ".json";
                            File.WriteAllText(fullfilename, jsonProduct);
                            break;
                    }
                }
            }
        }
        Console.WriteLine();

        Dictionary<string, List<Sale>> regionSaleMap = new Dictionary<string, List<Sale>>();
        Dictionary<string, List<Product>> productMap = new Dictionary<string, List<Product>>();
        Dictionary<string, List<Sale>> personMap = new Dictionary<string, List<Sale>>();

        for (int i = 0; i < outputJsonDirectories.Length; i++)
        {
            if (Directory.Exists(outputJsonDirectories[i]))
            {
                string[] files = Directory.GetFiles(outputJsonDirectories[i]);
                foreach (string file in files)
                {
                    switch (i)
                    {
                        case 0:
                            Console.WriteLine("Found File: {0}", file);
                            var jsonSale = CommissionParserSale.ParseJsonSale(file);

                            foreach (KeyValuePair<string, Sale[]> sale in jsonSale)
                            {
                                if (!regionSaleMap.ContainsKey(sale.Key))
                                {
                                    regionSaleMap[sale.Key] = new List<Sale>();
                                }

                                foreach (Sale s in sale.Value)
                                {
                                    regionSaleMap[sale.Key].Add(s);
                                }
                            }
                            break;

                        case 1:
                            Console.WriteLine("Found File: {0}", file);
                            var jsonPerson = CommissionParserSale.ParseJsonSale(file);

                            foreach (KeyValuePair<string, Sale[]> sale in jsonPerson)
                            {
                                if (!personMap.ContainsKey(sale.Key))
                                {
                                    personMap[sale.Key] = new List<Sale>();
                                }

                                foreach (Sale s in sale.Value)
                                {
                                    personMap[sale.Key].Add(s);
                                }
                            }
                            break;

                        case 2:
                            Console.WriteLine("Found File: {0}", file);
                            var jsonProduct = CommissionParserProduct.ParseJsonProduct(file);

                            foreach (KeyValuePair<string, Product[]> product in jsonProduct)
                            {
                                if (!productMap.ContainsKey(product.Key))
                                {
                                    productMap[product.Key] = new List<Product>();
                                }

                                foreach (Product p in product.Value)
                                {
                                    productMap[product.Key].Add(p);
                                }
                            }
                            break;
                    }
                }
            }
        }
        Console.WriteLine();

        Dictionary<string, double> commissionsBracketMap = new Dictionary<string, double>();

        foreach (KeyValuePair<string, List<Sale>> sale in personMap)
        {
            string agent = sale.Key;
            int agentRevenue = 0;

            foreach (Sale s in sale.Value)
            {
                agentRevenue = agentRevenue + Int32.Parse(s.Revenue);

                if (s.Dunno != "")
                {
                    agentRevenue = agentRevenue + Int32.Parse(s.Dunno);
                }

                Console.WriteLine();

            }
            if (0 < agentRevenue && agentRevenue <= 50000)
            {
                commissionsBracketMap[agent] = 0.015;
            }
            else if (50001 <= agentRevenue && agentRevenue <= 100000)
            {
                commissionsBracketMap[agent] = 0.0175;
            }
            else if (100001 <= agentRevenue && agentRevenue <= 150000)
            {
                commissionsBracketMap[agent] = 0.02;
            }
            else if (150001 <= agentRevenue && agentRevenue <= 200000)
            {
                commissionsBracketMap[agent] = 0.0225;
            }
            else if (200001 <= agentRevenue && agentRevenue <= 250000)
            {
                commissionsBracketMap[agent] = 0.025;
            }
            else if (250001 <= agentRevenue)
            {
                commissionsBracketMap[agent] = 0.0275;
            }
        }

        //foreach (KeyValuePair<string, List<Sale>> sale in personMap)
        //{
        //    List<Sale> pointOfSale = sale.Value

        //    string regionSale = sale.Key;
        //    int regionRevenue = 0;

        //    foreach (Sale s in sale.Value)
        //    {

        //        Console.WriteLine();
        //    }


        //    {
        //        regionSaleMap[sale.Key] = regionSaleMap[sale.Key] + Int32.Parse(s.Revenue);
        //    }
        //}

        Console.WriteLine();
    }
}
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
    //Was ignoring sections of data from the spreadsheet after certain values were encountered. Working but unusable methodology
    {
        var connectionString = string.Format("Provider=Microsoft.Jet.OLEDB.4.0; data source={0}; Extended Properties=Excel 8.0;", fileName);

        var adapter = new OleDbDataAdapter("SELECT * FROM [CC$]", connectionString); //Calls information from the first tab.
        var ds = new DataTable();

        adapter.Fill(ds);

        foreach (DataRow row in ds.Rows)
        {
            foreach (object col in row.ItemArray)//Places each value by column into a comma delimited row output.
            {
                Console.Write(col);
                Console.Write(",");
            }
            Console.WriteLine();
        }

        Console.WriteLine(fileName);
    }
    public static void ReadwithODBC(string fileName)
    //Also ignoring sections of data from the spreadsheet after certain values. Again, working but unusable methodology
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

            foreach (object col in cols)//Again takes each value by column and converts to a comma delimited row output.
            {
                Console.Write(col);
                Console.Write(",");
            }
            Console.WriteLine();
        }
    }
    static void Main(string[] args)
    {
        //This directs the iteration of the parsing to be performed further down so that we can maintain the same directory formatting.
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
                    switch (i)
                    {
                        //Cases created for each type of file designated by interpretation of theme based on the available information in the tab.
                        case 0:
                            string jsonSales = CommissionParserSale.CreateJsonSales(file);
                            string fn = Path.GetFileNameWithoutExtension(file);
                            string fullfilename = outputJsonDirectories[0] + fn + ".json";
                            File.WriteAllText(fullfilename, jsonSales);
                            break;
                        case 1:
                            string jsonPerson = CommissionParserPerson.CreateJsonPerson(file);
                            fn = Path.GetFileNameWithoutExtension(file);
                            fullfilename = outputJsonDirectories[1] + fn + ".json";
                            File.WriteAllText(fullfilename, jsonPerson);
                            break;
                        case 2:
                            string jsonProduct = CommissionParserProduct.CreateJsonProduct(file);
                            fn = Path.GetFileNameWithoutExtension(file);
                            fullfilename = outputJsonDirectories[2] + fn + ".json";
                            File.WriteAllText(fullfilename, jsonProduct);
                            break;
                    }
                }
            }
        }
        //Creating sets of associations between region/product/sales person and the list of each sale as definied by the class in jsonParse.cs via dictionary
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
                    //Combines each json file with similar theme to the desired aspects region/product/sales person.
                    switch (i)
                    {
                        case 0:
                            var jsonSale = CommissionParserSale.ParseJsonSale(file);

                            foreach (KeyValuePair<string, Sale[]> sale in jsonSale)
                            {
                                //Checks for the region information in the sale to signal when a new list should be started
                                if (!regionSaleMap.ContainsKey(sale.Key))
                                {
                                    regionSaleMap[sale.Key] = new List<Sale>();
                                }

                                //Adds each sale from every file to the map list associated with the region.
                                foreach (Sale s in sale.Value)
                                {
                                    regionSaleMap[sale.Key].Add(s);
                                }
                            }
                            break;

                        case 1:
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

        Dictionary<string, double> commissionsBracketMap = new Dictionary<string, double>();
        //Adds each revenue value for every sale and any rejected or cancelled sales to find the gross sales and assign the commission bracket to each sales person
        foreach (KeyValuePair<string, List<Sale>> sale in personMap)
        {
            string agent = sale.Key;
            int agentRevenue = 0;

            foreach (Sale s in sale.Value)
            {
                agentRevenue = agentRevenue + Int32.Parse(s.Revenue);
                //Confirms that the unrealized revenue exists prior to attempting to add it to the revenue value.
                if (s.Dunno != "")
                {
                    agentRevenue = agentRevenue + Int32.Parse(s.Dunno);
                }

            }
            //Defining the brackets based upon the gross revenue.
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

        Dictionary<string, Dictionary<string, double>> salesPersonByRegionCommissionsMap = new Dictionary<string, Dictionary<string, double>>();
        
        // Iterate through each SalesPerson (sale.key (type is: string))
        //     and their list of sales (sale.value (type is: List<Sale>)

        foreach (KeyValuePair<string, List<Sale>> sale in personMap)
        {
            string salesPerson = sale.Key;
            List<Sale> salesPersonListOfSales = sale.Value;

            // Add entry to the personbyregion map:
            salesPersonByRegionCommissionsMap[salesPerson] = new Dictionary<string, double>();

             // Iterate through List of sales here:
            foreach (Sale salesPersonSale in salesPersonListOfSales)
            {
                // For each sale this Sales Person made, Look up what region it belongs to:
                foreach (KeyValuePair<string, List<Sale>> salesByRegion in regionSaleMap)
                {
                    string region = salesByRegion.Key;
                    List<Sale> salesByRegionListOfSales = salesByRegion.Value;

                    // This point we have the region, and list of all sales in that region, loop through every sale:
                    foreach (Sale regionSale in salesByRegionListOfSales)
                    {
                        // Test if this sale matches the iterator for the Sale for the salesPerson:
                        if (regionSale.Equals (salesPersonSale))
                        {
                            // Now that we have the region, set the intial revenue for the region equal to 0:
                            if (!salesPersonByRegionCommissionsMap[salesPerson].ContainsKey(region))
                            {
                                salesPersonByRegionCommissionsMap[salesPerson][region] = 0;
                            }

                            // Looks like a match, we now know what region this sale belongs to:
                            salesPersonByRegionCommissionsMap[salesPerson][region] += Int32.Parse(regionSale.Revenue);
                        }
                    }
                }
            }
        }

        bool isTheArgumentValid = true;

        foreach (KeyValuePair<string, Dictionary<string, double>> salesPersonRegionRevenue in salesPersonByRegionCommissionsMap)
        {
            //Checks for given arguments to narrow field to give sales people and otherwise produces all sales person commission by region.
            if ((args.Length>0 && args[0] == salesPersonRegionRevenue.Key)||(args.Length==0))
            {
                Console.WriteLine("{0}:", salesPersonRegionRevenue.Key);

                isTheArgumentValid = false;
                
                foreach (KeyValuePair<string, double> regionRevenue in salesPersonRegionRevenue.Value)
                {
                    //Applies the derived comssions value by agent to the net sales in each region.
                    Console.WriteLine("    {0}:{1}", regionRevenue.Key, (commissionsBracketMap[salesPersonRegionRevenue.Key] * regionRevenue.Value).ToString("C2"));
                }

                Console.WriteLine();
            }
        }

        if (isTheArgumentValid)
        {
            Console.WriteLine("No valid arguments were provided.\nArguments are case sensitive and must include a valid sales person.\nPlease enter a valid argument.\nValid sales people names are listed below.");
            
            foreach (string salesPerson in salesPersonByRegionCommissionsMap.Keys)
            {
                Console.WriteLine("  {0}", salesPerson);
            }
        }

        Console.WriteLine();
    }
}
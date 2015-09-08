using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Newtonsoft.Json;

namespace CommissionParser
{
    class Sale {
        private string cc;
        private string first;
        private string last;
        private string phone;
        private string date;
        private string revenue;
        private string dunno;

        public string CC
        {
            get { return cc; }
            set { cc = value; }
        }
        public string Last
        {
            get { return last; }
            set { last = value; }
        }
        public string First
        {
            get { return first; }
            set { first = value; }
        }
        public string Phone
        {
            get { return phone; }
            set { phone = value; }
        }
        public string Date
        {
            get { return date; }
            set { date = value; }
        }
        public string Revenue
        {
            get { return revenue; }
            set { revenue = value; }
        }
        public string Dunno
        {
            get { return dunno; }
            set { dunno = value; }
        }

        public void Create(string line) {
            string[] columns = line.Split(',');
            this.cc = columns[0];
            this.first = columns[1];
            this.last = columns[2];
            this.phone = columns[3];
            this.date = columns[4];
            this.revenue = columns[5];
            this.dunno = columns[6];
        }

        public override bool Equals(System.Object obj)
        {
            // If parameter is null return false.
            if (obj == null)
            {
                return false;
            }

            // If parameter cannot be cast to Point return false.
            Sale s = obj as Sale;
            if ((System.Object)s == null)
            {
                return false;
            }

            // Return true if the fields match:
            return (Revenue == s.Revenue) && (Last == s.Last) && (First == s.First);
        }
    }
    class CommissionParserSale
    {
        public static string CreateJsonSales(string fileName)
        {
            string[] allLines = File.ReadAllLines(fileName);

            string region = "";
            
            Dictionary<string, List<Sale>> regionMap = new Dictionary<string, List<Sale>>();

            for (int i = 1; i < allLines.Length; i++)
            {
                string line = allLines[i];
            
                if (line.Contains(":"))
                {
                    region = line.Substring(0, line.IndexOf(':'));
                    regionMap[region] = new List<Sale>();
                    
                }
                else
                {
                    if (!line.Equals(",,,,,,"))
                    {
                        Sale newSale = new Sale();
                        newSale.Create(line);
                        regionMap[region].Add(newSale);
                    }
                }
            }
            string output = JsonConvert.SerializeObject(regionMap);

            return output;
        }

        public static Dictionary<string, Sale[]> ParseJsonSale(string fileName)
        {
            string[] fileLine = File.ReadAllLines(fileName);
            var jsonSale = JsonConvert.DeserializeObject<Dictionary<string, Sale[]>>(fileLine[0]);

            return jsonSale;
        }
    }
    class Product
    {
        private string cc;
        private string first;
        private string last;
        private string phone;
        private string date;
        private string revenue;

        public string CC
        {
            get { return cc; }
            set { cc = value; }
        }
        public string Last
        {
            get { return last; }
            set { last = value; }
        }
        public string First
        {
            get { return first; }
            set { first = value; }
        }
        public string Phone
        {
            get { return phone; }
            set { phone = value; }
        }
        public string Date
        {
            get { return date; }
            set { date = value; }
        }
        public string Revenue
        {
            get { return revenue; }
            set { revenue = value; }
        }

        public void Create(string line)
        {
            string[] columns = line.Split(',');
            this.cc = columns[0];
            this.first = columns[1];
            this.last = columns[2];
            this.phone = columns[3];
            this.date = columns[4];
            this.revenue = columns[5];
        }
    }
    class CommissionParserProduct
    {
        public static string CreateJsonProduct(string fileName)
        {
            string[] allLines = File.ReadAllLines(fileName);

            string product = "";

            Dictionary<string, List<Product>> productMap = new Dictionary<string, List<Product>>();

            for (int i = 1; i < allLines.Length; i++)
            {
                string line = allLines[i];

                if (line.Contains(":"))
                {
                    product = line.Substring(0, line.IndexOf(':'));
                    productMap[product] = new List<Product>();

                }
                else
                {
                    if (!line.Equals(",,,,,"))
                    {
                        Product newProduct = new Product();
                        newProduct.Create(line);
                        productMap[product].Add(newProduct);
                    }
                }
            }
            string output = JsonConvert.SerializeObject(productMap);

            return output;
        }

        public static Dictionary<string, Product[]> ParseJsonProduct(string fileName)
        {
            string[] fileLine = File.ReadAllLines(fileName);
            var jsonProduct = JsonConvert.DeserializeObject<Dictionary<string, Product[]>>(fileLine[0]);

            return jsonProduct;
        }
    }
    class CommissionParserPerson
    {
        public static string CreateJsonPerson(string fileName)
        {
            string[] allLines = File.ReadAllLines(fileName);

            string agent = "";

            Dictionary<string, List<Sale>> agentMap = new Dictionary<string, List<Sale>>();

            agent = System.IO.Path.GetFileNameWithoutExtension(fileName);
            agentMap[agent] = new List<Sale>();

            for (int i = 1; i < allLines.Length; i++)
            {
                string line = allLines[i];

                if (!line.Equals(",,,,,,"))
                {
                    Sale newSale = new Sale();
                    newSale.Create(line);
                    agentMap[agent].Add(newSale);
                }
            }

            string output = JsonConvert.SerializeObject(agentMap);

            return output;
        }

        public static Dictionary<string, Sale[]> ParseJsonPerson(string fileName)
        {
            string[] fileLine = File.ReadAllLines(fileName);
            var jsonPerson = JsonConvert.DeserializeObject<Dictionary<string, Sale[]>>(fileLine[0]);

            return jsonPerson;
        }
    }
}

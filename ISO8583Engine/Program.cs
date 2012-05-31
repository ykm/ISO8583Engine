//Sample
#region Using Directives
using System;
using System.Collections.Generic;
using System.Xml;
using System.Text;
#endregion

namespace ISO8583Engine {
    class Program {
        [STAThread]
        private static void Main(string[] args) {
            XmlDocument doc = new XmlDocument();
            doc.Load("c:\\update_request_fed.xml");
            XmlNode node = doc.SelectSingleNode("./Message/ISO8583Message");

            Dictionary<string, string> OnUsValues = new Dictionary<string, string>();
            OnUsValues.Add("AccountNumber", "12345678901234  ");
            //OnUsValues.Add("UpdateRequest", "Update Request");

            //Initialize the parser, 
            ISO8583Engine_Parser parser = new ISO8583Engine_Parser(node, OnUsValues);
            
            //parser.CreateISO8583Request("Common:AccountInfo", "1200"); //Combine the categories to obtain an object
            //byte[] t = parser.Serialize();
            parser.LogMessageEvent += new EventHandler(parser_MessageReceived);
            string tmp = "1210?0?F??        (820000000000000000000095455622332220120525163716201205250650451112035708815408UNI000000BWY             102+0000000004800946+0000000004800946+0000000000000000+0000000000000000+0000000004800946INR              INR003BWY478 21437512CTKNITAC B K                                                                    CTKNITAC B K                                                                    2008102835006SBAASG                                                                                                                                                                                                                                                   350001512    +0000000000000000+0000000000000000";
            //byte[] tmp2 = System.Text.Encoding.Convert(Encoding.UTF8, Encoding.ASCII, UTF32Encoding.UTF32.GetBytes(tmp));
            parser.ParseResponse(ASCIIEncoding.ASCII.GetBytes(tmp));
            //Console.WriteLine(parser.ISO8583MessageEncoder.GetString(t));
            Console.ReadKey();
        }

        static void parser_MessageReceived(object sender, EventArgs e) {
            
        }
    }
}
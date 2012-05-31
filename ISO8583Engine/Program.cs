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
        private static void Main (string[] args)
		{
			XmlDocument doc = new XmlDocument ();
			doc.Load ("update_request.xml");
			XmlNode node = doc.SelectSingleNode ("./Message/ISO8583Message");
			
			Dictionary<string, string> OnUsValues = new Dictionary<string, string> ();
			OnUsValues.Add ("AccountNumber", "12345678901234");
			OnUsValues.Add ("UpdateRequest", "UpdateRequest");
			
			ISO8583Engine_Parser parser = new ISO8583Engine_Parser (node, OnUsValues);
			parser.LogMessageEvent += new EventHandler (parser_MessageReceived);
			ISO8583Engine_Message Message = parser.CreateISO8583Request ("Common:Update", "1200");
			byte[] MessageBytes = Message.Serialize ();
			Console.ReadKey ();
		}
    }
}

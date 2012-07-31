#region Using Directives
using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using ISO8583Engine;
#endregion

namespace Test
{
	class Program
	{

		private static ISO8583Parser parser = null;

		[STAThread]
		private static void Main (string[] args)
		{
			XmlDocument doc = new XmlDocument ();
			doc.Load (@"../../../update_request.xml");
			XmlNode node = doc.SelectSingleNode ("./Message/ISO8583Message");

			//Create the on us values
			Dictionary<string, string> OnUsValues = new Dictionary<string, string> ();
			OnUsValues.Add ("AccountNumber", "1234567890123456");
			OnUsValues.Add ("UpdateRequest", "1234567890123456 20120701 20120801");
			OnUsValues.Add ("DeviceID", "0000000012345678");
			OnUsValues.Add ("SerialNumber", "000012345678");
			OnUsValues.Add ("BranchCode", "1512");
			OnUsValues.Add ("BankId", "1234");
			
			//Initialize the parser engine
			parser = new ISO8583Parser (node);


			//Create the ISO8583 request
			ISO8583Message Message = parser.CreateISO8583Request ("Common:AccountInfo", OnUsValues, "1200"); //Combine the categories to populate the object with required values
			Message.LogMessageEvent += new EventHandler<DebugNotify> (parser_LogMessageEvent);
			byte[] t = Message.Serialize ();
			Console.WriteLine (ASCIIEncoding.ASCII.GetString (t));
			Console.WriteLine (Message.DumpISO8583State ());
			Console.WriteLine ();
			ISO8583Message ParsedMessage = new ISO8583Message ();
			if (ParsedMessage.ParseResponse (t)) {
				Console.WriteLine (ParsedMessage.DumpISO8583State ());
			}
			Console.ReadKey ();
		}

		static void parser_LogMessageEvent (object sender, DebugNotify e)
		{
			//throw new NotImplementedException();
		}

		static void parser_MessageReceived (object sender, EventArgs e)
		{
			//NOTE: the sender value is a string value containing the log message.
			Console.WriteLine (sender.ToString ());
		}
	}
}
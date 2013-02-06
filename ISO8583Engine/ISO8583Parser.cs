/*******************************************************************************************************************
 * Author: Yash K. Mishra.
 * ISO8583 parser engine, parses the request/XML, creates a corresponding ISO8583Message object. A lot of
 * assumptions have been made. A successful request creation completely depends on the validity of the
 * request Xml as well as the values supplied at the runtime. So, it is absolutely important that these
 * two are "in sync". Please read the README file for decription of creating the request XML.
 *
 * DISCLAIMER:
 * THIS SOFTWARE IS PROVIDED "AS IS" AND ANY EXPRESSED OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO,
 * THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT
 * SHALL THE REGENTS OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR
 * CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE,
 * DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT,
 * STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
 * SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 ******************************************************************************************************************/
namespace ISO8583Engine
{

    #region Using Directives
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Xml;
    #endregion

    public struct KeyValue {

        #region Variables
        internal string Value;
        internal int Length;
        #endregion

        public KeyValue(string Value, int Length)
        {
            this.Value = Value;
            this.Length = Length;
        }
    }

    public class ISO8583Parser
    {

        #region Variables
        private XmlNode RequestXml = null;
        private Dictionary<string, KeyValue> Keys = new Dictionary<string, KeyValue>();
        internal Dictionary<string, List<ISO8583Field>> AllCategories = new Dictionary<string, List<ISO8583Field>>();
        internal Dictionary<string, string> _OnUsValues = null;
        public bool IsSecondaryBitmapPresent = true;
        public BitmapType BitmapRegionType = BitmapType.Binary;
        #endregion

        public ISO8583Parser(XmlNode RequestXmlDom, Dictionary<string, string> OnUsValues)
        {
            if (RequestXmlDom == null)
                throw new ArgumentException("The RequestXmlDom argument cannot be null");
            else
                this.RequestXml = RequestXmlDom;
            
            if (OnUsValues == null)
                throw new ArgumentException("The OnUsValues argument cannot be null");
            else 
                this._OnUsValues = OnUsValues;
            
            this.GetKeys();
            this.GetCategories();
        }

    	//Use the current "on us" values and default Bitmap presense
        public ISO8583Message CreateRequest(string Categories, string MTIValue)   
        {
            return this.CreateRequest(Categories, this._OnUsValues, MTIValue, true);
        }
        
        //Use the current "on us" values and override the bitmap presense.
        public ISO8583Message CreateRequest(string Categories, string MTIValue, bool IsSecondaryBitmapPresent)   
        {
            return this.CreateRequest(Categories, this._OnUsValues, MTIValue, IsSecondaryBitmapPresent);
        }

        public ISO8583Message CreateRequest(string Categories, Dictionary<string, string> NewOnUsValues, string MTIValue, bool IsSecondaryBitmapPresent)
        {
            ISO8583Message Message = new ISO8583Message(this.BitmapRegionType, IsSecondaryBitmapPresent);
            this._OnUsValues = NewOnUsValues;
            if (string.IsNullOrEmpty(Categories)) 
                throw new ArgumentException("Category cannot be null");
            else if (Categories.Contains("Response"))
                throw new ArgumentException("Cannot use the Response category to create requests");
            else {
                Message.IsoFields.Clear();
                string[] IndividualCategories = Categories.Trim().Split(':');
                Message.MessageType = MTIValue;
                foreach (string item in IndividualCategories) {
                    if (AllCategories.ContainsKey(item.Trim())) {
                        List<ISO8583Field> CategoryFields = AllCategories[item];
                        foreach (ISO8583Field field in CategoryFields) {
                        	
                        	//Resolve the dynamic values just before creating the request
                            if (IsDynamicField(field.FieldValue)) 
                                field.FieldValue = ResolveDynamicValue(field.FieldValue);
                            
                            try {
                                Message.AddField(field);
                            } catch (ArgumentException) {
                                throw new ArgumentException("The Categories supplied have one or more common "+
                        		                            "fields...failed to create ISO8583Message");
                            }
                        }
                    } else 
                        throw new ArgumentException("Invalid Category " + item + " requested");
                }
                return Message;
            }
        }

        private void GetCategories()
        {
            XmlNodeList KeysXml = this.RequestXml.SelectNodes("ISO8583Fields");
            foreach (XmlNode item in KeysXml) {
                XmlAttribute CategoryAttr = item.Attributes["Category"];
                if (CategoryAttr != null) {
                    string CategoryID = CategoryAttr.Value;
                    List<ISO8583Field> CategoryFields = new List<ISO8583Field>();
                    XmlNodeList FieldsXml = item.SelectNodes("Field");
                    foreach (XmlNode Field in FieldsXml) {
                        ISO8583Field isoField = new ISO8583Field();
                        
                        // Reading the required attributes.
                        XmlAttribute IndexAttribute = Field.Attributes["Index"];
                        XmlAttribute HeaderAttribute = Field.Attributes["Header"];
                        XmlAttribute MaxLengthAttribute = Field.Attributes["MaxLength"];
                        
                        ISO8583MessageElement Index = (ISO8583MessageElement)(Convert.ToInt32(IndexAttribute.Value) - 1);
                        if (IndexAttribute != null && MaxLengthAttribute != null) {
                            isoField.FieldIndex = Index;
                            isoField.IsFieldEnabled = true;
                            string tmpFieldValue = Field.InnerXml;
                            if (isoField.FieldHeaderLength.Equals(0) && tmpFieldValue.Contains("|")) 
                                tmpFieldValue = this.CheckForUserPadding(tmpFieldValue, ref isoField);
                            
                            isoField.FieldValue = tmpFieldValue;
                            try {
                            	Length tmp = new Length();
                            	tmp.FieldLength = isoField.FieldLength = Convert.ToInt32(MaxLengthAttribute.Value);
                            	tmp.HeaderLength = isoField.FieldHeaderLength = 
                            		(HeaderAttribute == null) ? 0 : Convert.ToInt32(HeaderAttribute.Value);
                                if (ISO8583Message.isoFieldLengths.ContainsKey(Index)) 
                                    ISO8583Message.isoFieldLengths[Index] = tmp;
                                else 
                                    ISO8583Message.isoFieldLengths.Add(Index, tmp);
                            } catch (Exception) {
                                throw;
                            }
                            CategoryFields.Add(isoField);
                        } else 
                            throw new XmlException("Invalid XML: A field in Category " + CategoryID + 
                        	                       " has a missing attribute, " + Index.ToString());
                    }
                    AllCategories.Add(CategoryID, CategoryFields);
                } else {
                    throw new XmlException("Invalid XML: Invalid category provided");
                }
            }
        }

        // TODO: We should be able to ignore the '|' value escaped using '\', just like in the printf statement
        private string CheckForUserPadding(string FieldValue, ref ISO8583Field _IsoField)
        {
            string[] tmp = FieldValue.Split('|');
            if (tmp[0].Length.Equals(tmp[1].Length) && tmp[0].Length <= 1) {
                if (tmp[0].Equals(tmp[1])) {
                    _IsoField.PaddingChar = '0';
                    return tmp[0];
                } else 
            		throw new XmlException(string.Format("Invalid padding info provided in field {0}, "+
            	                                            "please check the input Xml", _IsoField.FieldIndex));
            } else if (tmp[0].Length > tmp[1].Length && tmp[1].Length <= 1) { //implies that field should be right padded
                _IsoField.PaddingPlacement = Padding.Right;
                _IsoField.PaddingChar = string.IsNullOrEmpty(tmp[1]) ? ' ' : tmp[1][0];
                return tmp[0];
            } else {
                _IsoField.PaddingPlacement = Padding.Left;
                _IsoField.PaddingChar = string.IsNullOrEmpty(tmp[0]) ? ' ' : tmp[0][0];
                return tmp[1];
            }
        }

        private string ResolveDynamicValue(string DynamicValue)
        {
            string[] MajorExpressions = DynamicValue.Split('+');
            StringBuilder ReturnValue = new StringBuilder();
            foreach (string item in MajorExpressions) {
                string[] Prefix = item.Split(':');
                if (Prefix.Length > 1) {
                    string AppendValue = string.Empty;
                    switch (Prefix[0]) {
                    case "on-us":
                        string Key = Prefix[1];
                        AppendValue = this._OnUsValues[Key];
                        break;

                    case "time":
                        DateTime Now = DateTime.Now;
                        if (Prefix.Length == 3) {
                            if (Prefix[2].ToLower().Equals("utc")) 
                                Now = DateTime.Now.ToUniversalTime();
                        }
                        AppendValue = Now.ToString(Prefix[1]);
                        break;

                    case "key":
                        KeyValue tmp = Keys[Prefix[1]];
                        AppendValue = tmp.Value;
                        if (IsDynamicField(AppendValue)) 
                            AppendValue = ResolveDynamicValue(AppendValue);
                        
                        AppendValue = AppendValue.PadRight(tmp.Length);
                        break;

                    default:
                        break;
                    }
                    ReturnValue.Append(AppendValue);
                } else 
                    ReturnValue.Append(Prefix[0]); //Assuming that the dynamic field contain's an atomic value.
            }
            return ReturnValue.ToString();
        }

        private bool IsDynamicField(string FieldValue)
        {
            return FieldValue.Contains("on-us:") || FieldValue.Contains("time:") ||
            	FieldValue.Contains("key:");                                                                
        }

        private void GetKeys()
        {
            XmlNodeList KeysXml = this.RequestXml.SelectNodes("IsoKeys/Key");
            foreach (XmlNode item in KeysXml) {
                XmlAttribute IndexAttr = item.Attributes["Index"];
                if (IndexAttr != null) {
                    string IndexValue = IndexAttr.Value;
                    string Value = item.InnerXml;
                    XmlAttribute LengthAttr = item.Attributes["Length"];
                    if (LengthAttr != null) {
                        int length = Convert.ToInt32(LengthAttr.Value);
                        Keys.Add(IndexValue, new KeyValue(Value, length));
                    } else 
                        throw new XmlException("Invalid Xml: A key is missing the Length attribute");
                    
                } else 
                    throw new XmlException("Invalid Xml: A key is missing the index attribute");
            }
        }
    }
}
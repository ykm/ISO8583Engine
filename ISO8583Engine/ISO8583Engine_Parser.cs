/*******************************************************************************************************************
 * Author: Yash K. Mishra.
 * ISO8583 parser engine, parses the request/XML, creates a corresponding ISO8583 object. A lot of 
 * assumptions have been made. A successful request creation completely depends on the validity of the 
 * request Xml as well as the values supplied at the runtime. So, it is absolutely important that these
 * two are "in sync". You cannot directly create the ISO8583Engine_Message object. You will have to 
 * parse the message using the parser first. The parser will contain the iso fields of only one message at a
 * time. So if the same Parser is used to parse/create another message, previous entries will be lost. 
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
#region Using Directives
using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
#endregion

namespace ISO8583Engine {
    public class ISO8583Engine_Parser : ISO8583Engine_Message {

        #region Variables
        private XmlNode RequestXml = null;
        private Dictionary<string, string> Keys = new Dictionary<string, string>();
        internal Dictionary<string, List<ISO8583Field>> AllCategories = new Dictionary<string, List<ISO8583Field>>();
        internal Dictionary<string, string> _OnUsValues = null;
        #endregion

        public ISO8583Engine_Parser(XmlNode RequestXmlDom, Dictionary<string, string> OnUsValues) {
            if (RequestXmlDom == null) {
                throw new ArgumentException("The RequestXmlDom argument cannot be null");
            } else {
                this.RequestXml = RequestXmlDom;
            }
            if (OnUsValues == null) {
                throw new ArgumentException("The OnUsValues argument cannot be null");
            } else {
                this._OnUsValues = OnUsValues;
            }
            ParseXml();
        }

        private void ParseXml() {
            try {
                GetKeys();
                GetCategories();
            } catch (Exception) {
                throw;
            }
        }

        public bool CreateISO8583Request(string Categories, string MTIValue) { //Use the current "on us" values.
            return CreateISO8583Request(Categories, this._OnUsValues, MTIValue);
        }

        public bool CreateISO8583Request(string Categories, Dictionary<string, string> NewOnUsValues, string MTIValue) {
            this._OnUsValues = NewOnUsValues;
            if (string.IsNullOrEmpty(Categories)) {
                throw new ArgumentException("Category cannot be null");
            } else if (Categories.Contains("Response")) {
                throw new ArgumentException("Cannot use the Response category to create requests");
            } else {
                this.IsoFields.Clear();
                string[] IndividualCategories = Categories.Trim().Split(':');
                this.MessageType = MTIValue;
                foreach (string item in IndividualCategories) {
                    if (AllCategories.ContainsKey(item)) {
                        List<ISO8583Field> CategoryFields = AllCategories[item];
                        foreach (ISO8583Field field in CategoryFields) {
                            if (field.IsDynamicField) {
                                //Resolve the dynamic values just before creating the request
                                field.FieldValue = ResolveDynamicValue(field.DynamicData);
                            }
                            try {
                                this.AddField(field);
                            } catch (ArgumentException) {
                                throw new ArgumentException("The Categories supplied have one or more common fields...failed to create ISO8583Message");
                            }
                        }
                    } else {
                        throw new ArgumentException("Invalid Category " + item + " requested");
                    }
                }
                return true;
            }
        }

        private void GetCategories() {
            this.FieldHeaderLength.Clear();
            XmlNodeList KeysXml = this.RequestXml.SelectNodes("ISO8583Fields");
            foreach (XmlNode item in KeysXml) {
                XmlAttribute CategoryAttr = item.Attributes["Category"];
                if (CategoryAttr != null) {
                    string CategoryID = CategoryAttr.Value;
                    List<ISO8583Field> CategoryFields = new List<ISO8583Field>();
                    XmlNodeList FieldsXml = item.SelectNodes("Field");
                    foreach (XmlNode Field in FieldsXml) {
                        ISO8583Field _IsoField = new ISO8583Field();
                        XmlAttribute IndexAttribute = Field.Attributes["Index"];
                        XmlAttribute HeaderAttribute = Field.Attributes["Header"];
                        XmlAttribute MaxLengthAttribute = Field.Attributes["MaxLength"];
                        if (IndexAttribute != null && MaxLengthAttribute != null) {
                            isoMessageElement Index = (isoMessageElement)(Convert.ToInt32(IndexAttribute.Value) - 1);
                            _IsoField.FieldIndex = Index;
                            _IsoField.IsDynamicField = IsDynamicField(Field.InnerXml);
                            if (_IsoField.IsDynamicField) {
                                _IsoField.DynamicData = Field.InnerXml;
                            } else {
                                _IsoField.FieldValue = Field.InnerXml;
                            }
                            _IsoField.IsFieldEnabled = true;
                            int FieldHeaderLength = Convert.ToInt32(HeaderAttribute.Value);
                            try {
                                int tmpFieldLength = Convert.ToInt32(MaxLengthAttribute.Value);
                                _IsoField.FieldLength = tmpFieldLength;
                                if (this.isoFieldLengths.ContainsKey(Index)) {
                                    this.isoFieldLengths[Index] = tmpFieldLength;
                                    this.FieldHeaderLength[Index] = FieldHeaderLength;
                                } else {
                                    this.isoFieldLengths.Add(Index, tmpFieldLength);
                                    this.FieldHeaderLength.Add(Index, FieldHeaderLength);
                                }
                            } catch (Exception) {
                                throw;
                            }
                            CategoryFields.Add(_IsoField);
                        } else {
                            throw new XmlException("Invalid XML: A field in Category " + CategoryID + " has a missing attribute");
                        }
                    }
                    AllCategories.Add(CategoryID, CategoryFields);
                } else {
                    throw new XmlException("Invalid XML: Invalid category provided");
                }
            }
        }

        private string ResolveDynamicValue(string DynamicValue) {
            string[] MajorExpressions = DynamicValue.Split('+');
            StringBuilder ReturnValue = new StringBuilder();
            foreach (string item in MajorExpressions) {
                string[] Prefix = item.Split(':');
                if (Prefix.Length > 1) {
                    switch (Prefix[0]) {
                        case "on-us":
                            string Key = Prefix[1];
                            ReturnValue.Append(this._OnUsValues[Key]);
                            break;
                        case "time": ReturnValue.Append(DateTime.Now.ToString(Prefix[1])); break;
                        case "key": ReturnValue.Append(Keys[Prefix[1]]); break;
                        default: break;
                    }
                } else {
                    ReturnValue.Append(Prefix[0]); //Assuming that the dynamic field contains an atomic value.
                }
            }
            return ReturnValue.ToString();
        }

        private bool IsDynamicField(string InnerXml) {
            return (InnerXml.Contains("on-us:") || InnerXml.Contains("time:") || InnerXml.Contains("key:"));
        }

        private void GetKeys() {
            XmlNodeList KeysXml = this.RequestXml.SelectNodes("IsoKeys/Key");
            foreach (XmlNode item in KeysXml) {
                XmlAttribute IndexAttr = item.Attributes["Index"];
                if (IndexAttr != null) {
                    string IndexValue = IndexAttr.Value;
                    string KeyValue = item.InnerXml;
                    XmlAttribute LengthAttr = item.Attributes["Length"];
                    if (LengthAttr != null) {
                        int length = Convert.ToInt32(LengthAttr.Value);
                        Keys.Add(IndexValue, KeyValue.PadRight(length));
                    } else {
                        throw new XmlException("Invalid Xml: A key is missing the Length attribute");
                    }
                } else {
                    throw new XmlException("Invalid Xml: A key is missing the index attribute");
                }
            }
        }
    }
}
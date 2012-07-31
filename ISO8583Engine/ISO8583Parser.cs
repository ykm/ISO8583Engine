/*******************************************************************************************************************
 * Author: Yash K. Mishra.
 * ISO8583 parser engine, parses the request/XML, creates a corresponding ISO8583 object. A lot of 
 * assumptions have been made. A successful request creation completely depends on the validity of the 
 * request Xml as well as the values supplied at the runtime. So, it is absolutely important that these
 * two are "in sync". You cannot directly create the ISO8583Engine_Message object. You will have to 
 * parse the message using the parser first. The parser will contain the iso fields of only one message at a
 * time. So if the same Parser is used to parse/create another message, previous entries will be lost. Please
 * read the README file for decription of creating the request XML.
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


    public struct KeyValue {

        #region Variables
        internal string Value; 
        internal int Length;
        #endregion

        public KeyValue(string Value, int Length) {
            this.Value = Value;
            this.Length = Length;
        }
    }

    public class ISO8583Parser {

        #region Variables
        private XmlNode RequestXml = null;
        private Dictionary<string, KeyValue> Keys = new Dictionary<string, KeyValue>();
        internal Dictionary<string, List<ISO8583Field>> AllCategories = new Dictionary<string, List<ISO8583Field>>();
        internal Dictionary<string, string> _OnUsValues = null;
        #endregion

        public ISO8583Parser(XmlNode RequestXmlDom) {
            if (RequestXmlDom == null) {
                throw new ArgumentException("The RequestXmlDom argument cannot be null");
            } else {
                this.RequestXml = RequestXmlDom;
            }
            GetKeys ();
			GetCategories ();
        }

        public ISO8583Message CreateISO8583Request(string Categories, Dictionary<string, string> NewOnUsValues, string MTIValue) {
            ISO8583Message Message = new ISO8583Message();
            this._OnUsValues = NewOnUsValues;
            if (string.IsNullOrEmpty(Categories)) {
                throw new ArgumentException("Category cannot be null");
            } else if (Categories.Contains("Response")) {
                throw new ArgumentException("Cannot use the Response category to create requests");
            } else {
                Message.IsoFields.Clear();
                string[] IndividualCategories = Categories.Trim().Split(':');
                Message.MessageType = MTIValue;
                foreach (string item in IndividualCategories) {
                    if (AllCategories.ContainsKey(item.Trim())) {
                        List<ISO8583Field> CategoryFields = AllCategories[item];
                        foreach (ISO8583Field field in CategoryFields) {
                            if (IsDynamicField(field.FieldValue)) {
                                //Resolve the dynamic values just before creating the request
                                field.FieldValue = ResolveDynamicValue(field.FieldValue);
                            }
                            try {
                                Message.AddField(field);
                            } catch (ArgumentException) {
                                throw new ArgumentException("The Categories supplied have one or more common fields...failed to create ISO8583Message");
                            }
                        }
                    } else {
                        throw new ArgumentException("Invalid Category " + item + " requested");
                    }
                }
                return Message;
            }
        }

        private void GetCategories() {
            ISO8583Message.FieldHeaderLength.Clear();
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
                        isoMessageElement Index = (isoMessageElement)(Convert.ToInt32(IndexAttribute.Value) - 1);
                        if (IndexAttribute != null && MaxLengthAttribute != null) {
                            _IsoField.FieldIndex = Index;
                            _IsoField.IsFieldEnabled = true;
                            string tmpFieldValue = Field.InnerXml;
                            _IsoField.FieldHeaderLength = Convert.ToInt32(HeaderAttribute.Value);
                            if (_IsoField.FieldHeaderLength.Equals(0) && tmpFieldValue.Contains("|")) {
                                tmpFieldValue = CheckForUserPadding(tmpFieldValue, ref _IsoField);
                            }
                            _IsoField.FieldValue = tmpFieldValue;
                            try {
                                int tmpFieldLength = Convert.ToInt32(MaxLengthAttribute.Value);
                                _IsoField.FieldLength = tmpFieldLength;
                                if (ISO8583Message.isoFieldLengths.ContainsKey(Index)) {
                                    ISO8583Message.isoFieldLengths[Index] = tmpFieldLength;
                                    ISO8583Message.FieldHeaderLength[Index] = _IsoField.FieldHeaderLength;
                                } else {
                                    ISO8583Message.isoFieldLengths.Add(Index, tmpFieldLength);
                                    ISO8583Message.FieldHeaderLength.Add(Index, _IsoField.FieldHeaderLength);
                                }
                            } catch (Exception) {
                                throw;
                            }
                            CategoryFields.Add(_IsoField);
                        } else {
                            throw new XmlException("Invalid XML: A field in Category " + CategoryID + " has a missing attribute, " + Index.ToString());
                        }
                    }
                    AllCategories.Add(CategoryID, CategoryFields);
                } else {
                    throw new XmlException("Invalid XML: Invalid category provided");
                }
            }
        }

        private string CheckForUserPadding(string FieldValue, ref ISO8583Field _IsoField) {
            string[] tmp = FieldValue.Split('|');
            if (tmp[0].Length.Equals(tmp[1].Length) && tmp[0].Length <= 1) {
                if (tmp[0].Equals(tmp[1])) {
                    _IsoField.PaddingChar = '0';
                    return tmp[0];
                } else {
                    throw new XmlException(string.Format("Invalid value provided in field {0}, please check the input Xml", _IsoField.FieldIndex));
                }
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

        private string ResolveDynamicValue(string DynamicValue) {
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
                        case "time": AppendValue = DateTime.Now.ToString(Prefix[1]); break;
                        case "key":
                            KeyValue tmp = Keys[Prefix[1]];
                            AppendValue = tmp.Value;
                            if (IsDynamicField(AppendValue)) {
                                AppendValue = ResolveDynamicValue(AppendValue);
                            }
                            AppendValue = AppendValue.PadRight(tmp.Length);
                            break;
                        default: break;
                    }
                    ReturnValue.Append(AppendValue);
                } else {
                    ReturnValue.Append(Prefix[0]); //Assuming that the dynamic field contains an atomic value.
                }
            }
            return ReturnValue.ToString();
        }

        public bool IsDynamicField(string FieldValue) {
            return (FieldValue.Contains("on-us:") || FieldValue.Contains("time:") ||
                    FieldValue.Contains("key:") || FieldValue.Contains("|"));
        }

        private void GetKeys() {
            XmlNodeList KeysXml = this.RequestXml.SelectNodes("IsoKeys/Key");
            foreach (XmlNode item in KeysXml) {
                XmlAttribute IndexAttr = item.Attributes["Index"];
                if (IndexAttr != null) {
                    string IndexValue = IndexAttr.Value;
                    string Value = item.InnerXml;
                    XmlAttribute LengthAttr = item.Attributes["Length"];
                    if (LengthAttr != null) {
                        int length = Convert.ToInt32(LengthAttr.Value);
                        Keys.Add(IndexValue,new KeyValue(Value, length));
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
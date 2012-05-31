/****************************************************
 * Author: Yash K. Mishra
 * Forms the base of the parser class, only deals with
 * the creation of message.
 ***************************************************/
#region Using Directives
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;
#endregion

namespace ISO8583Engine {
    
    #region enum isoMessageElement
    public enum isoMessageElement {
        Field1_SecondaryBitmap = 0,								//Field1  (Length = 8)  
        Field2_PrimaryAccountNumber,							//Field2  (Length = 19) 
        Field3_ProcessingCode,									//Field3  (Length = 6)  
        Field4_TransactionAmount,								//Field4  (Length = 16)  
        Field5_ReconciliationAmount,							//Field5  (Length = 16)  
        Field6_Amount_CardHolder_Billing = 5,                   //Field6  (Length = 16)
        Field7_Transmission_DateTime = 6,                       //Field7  (Length = 10)
        Field8_Amount_CardHolder_Billing_Fee = 7,               //Field8  (Length = 8)
        Field9_ConversionRateSettlement = 8,					//Field9  (Length = 8)
        Field10_ConversionRateCardHolderBilling = 9,            //Field9  (Length = 8)
        Field11_SystemTraceAuditNumber = 10,					//Field11  (Length = 12) 
        Field12_TimeLocalTransaction = 11,						//Field12  (Length = 14)
        Field13_DateLocalTransaction = 12,
        Field14_DateExpiration = 13,
        Field15_DateSettlement = 14,							//Field15  (Length = 8) 
        Field16_DateConversion = 15,							//Field16  (Length = 8) 
        Field17_DateCapture = 16,								//Field17  (Length = 8) 
        Field18_Merchant_type = 17,								//Field18  (Length = 4)
        Field19_Acquiring_institution_country_code = 18,		//Field19  (Length = 3)
        Field20_PANExtendedCountryCode = 19,
        Field21_ForwardingInstitutionCountryCode = 20,
        Field22_PointOfServiceEntryMode = 21,
        Field23_ApplicationPANNumber = 22,
        Field24_FunctionCode = 23,								//Field24  (Length = 3) 
        Field25_PointOfServiceCountryCode = 24,
        Field26_PointOfServiceCaptureCode = 25,
        Field27_AuthIDResponseLength = 26,
        Field28_AmountTransactionFee = 27,
        Field29_AmountSettlementFee = 28,
        Field30_OriginalAmounts = 29,							//Field30  (Length = 32) 
        Field31_AmountSettlementProcessingFee = 30,
        Field32_AcquringInstitutionIdentificationCode = 31,		//Field32  (Length = 11) 
        Field33_ForwardingInstitutionIdentificationCode = 32,	//Field33  (Length = 11) 
        Field34_PrimaryAccountNumberExtended = 33,				//Field34  (Length = 28)
        Field35_Track_2_data = 34,				                //Field35  (Length = 37)
        Field36_Track_3_data = 35,
        Field37_RetrievalReferenceNumebr = 36,					//Field37  (Length = 12) 
        Field38_AuthorizationIdentificationResponse = 37,		//Field38  (Length = 6) 
        Field39_ActionCode = 38,								//Field39  (Length = 3) 
        Field40_ServiceRestrictionCode = 39,
        Field41_CardAcceptorTerminalIdentification = 40,		//Field41  (Length = 16) 
        Field42_CardAcceptorIdentificationCode = 41,			//Field42  (Length = 15) 
        Field43_CardAcceptorNameLocation = 42,					//Field43  (Length = 99) 
        Field44_AdditionalResponseData = 43,
        Field45_Track_1_Data = 44,
        Field46_AdditionalDataISO = 45,								//Field46  (Length = 300) 
        Field47_AdditionalDataNational = 46,
        Field48_AdditionalDataPrivate = 47,						//Field48  (Length = 999) 
        Field49_CurrencyCodeTransaction,						//Field49  (Length = 3) 
        Field50_CurrencyCodeSettlement = 49,							//Field50  (Length = 3) 
        Field51_CurrencyCodeCardHolderBilling = 50,
        Field52_PersonalIdentificationNumData = 51,
        Field53_SecurityControlInfo = 52,
        Field54_AdditionalAmounts = 53,
        Field55_ReservedISO = 54,
        Field56_OriginalDataElements = 55,						//Field56  (Length = 43) 
        Field57_ReservedNational = 56,
        Field58_ReservedNational = 57,
        Field59_TransportData = 58,								//Field59  (Length = 999) 
        Field60_Advice_reason_code_private_reserved = 59,		    //Field60  (Length = 7) 
        Field61_ReservedPrivate0Field = 60,						    //Field61  (Length = 7) 
        Field62_ReservedPrivate1Field = 61,						//Field62  (Length = 999) 
        Field63_ReservedPrivate2Field = 62,							//Field63  (Length = 28) 
        Field64_MessageAuthenticationCode = 63,
        Field65_TertiaryBitmap = 64,
        Field66_SettlementCode = 65, 						//Field66  (Length = 300) 
        Field67_ExtendedPaymentCode = 66,
        Field68_RecievingInstitutionCountryCode = 67,
        Field69_SettlementInstitutionCountryCode = 68,
        Field70_NetworkManagementInfoCode = 69,
        Field71_MessageNumber = 70,
        Field72_DataRecord = 71,
        Field73_DateAction = 72,
        Field74_CreditsNumber = 73,
        Field75_CreditsReversalNumber = 74,
        Field76_DebitsNumber = 75,
        Field77_DebitsReversalNumber = 76,
        Field78_TransferNumber = 77,
        Field79_TransferReversalNumber = 78,
        Field80_InquiriesNumber = 79,
        Field81_AuthorizationsNumber = 80,
        Field82_CreditsProcessingFee = 81,
        Field83_CreditsTransactionFee = 82,
        Field84_DebitsProcessingFee = 83,
        Field85_DebitsTransactionFee = 84,
        Field86_CreditsAmount = 85,
        Field87_CreditsReversalAmount = 86,
        Field88_DebitsAmount = 87,
        Field89_DebitsReversalAmount = 88,
        Field90_OrignalDataElement = 89,
        Field91_FileUpdateCode = 90,
        Field92_FileSecurityCode = 91,
        Field93_ResponseIndicator = 92,
        Field94_ServiceIndicator = 93,
        Field95_ReplacementAmounts = 94,
        Field96_MessageSecurityCode = 95,
        Field97_AmountNetSettlement = 96,
        Field98_Payee = 97,
        Field99_SettlementInstitutionIdentificationCode = 98,	//Field99  (Length = 11) 
        Field100_ReceivingInstitutionIdentificationCode = 99,
        Field101_FileName = 100,
        Field102_AccountIdentification1 = 101,					//Field102  (Length = 38)
        Field103_AccountIdentification2 = 102,						//Field103  (Length = 40)
        Field104_TransactionDescription = 103,
        Field105_ReservedISO = 104,
        Field106_ReservedISO = 105,
        Field107_ReservedISO = 106,
        Field108_ReservedISO = 107,
        Field109_ReservedISO = 108,
        Field110_ReservedISO = 109,
        Field111_ReservedISO = 110,
        Field112_ReservedNational = 111,
        Field113_AuthAgentInstitutionIDCode = 112,
        Field114_ReservedNational = 113,
        Field115_ReservedNational = 114,
        Field116_ReservedNational = 115,
        Field117_ReservedNational = 116,
        Field118_ReservedNational = 117,
        Field119_ReservedNational = 118,
        Field120_ReservedPrivate = 119,
        Field121_ReservedPrivate = 120,
        Field122_ReservedPrivate = 121,
        //Field123_DeliveryChannelControllerID = 122, 			//Field123  (Length = 6)
        Field123_DeliveryChannelControllerID = 122, 			//Field123  (Length = 3)
        Field124_TerminalType,									//Field124  (Length = 3)
        Field125_ReservedPrivateUse1,							//Field125  (Length = 999)
        Field126_ReservedPrivateUse2,							//Field126  (Length = 999)
        Field127_ReservedPrivateUse3,							//Field127  (Length = 999)     						                		
    }
    #endregion

    public class ISO8583Field {

        #region Variables
        public isoMessageElement FieldIndex;
        public bool IsFieldEnabled = false;
        public int FieldLength = 0;
        public bool IsDynamicField = false;
        public string DynamicData = string.Empty;
        private string _FieldValue = "";
        #endregion

        public string FieldValue {
            get {
                return this._FieldValue;
            }
            set {
                this._FieldValue = value;
            }
        }

        public string GetNormalizedFieldValue(int FieldHeaderLength) {
            if (FieldHeaderLength > 0) {
                return (this._FieldValue.Length.ToString().PadLeft(FieldHeaderLength, '0') + this._FieldValue);
            } else {
                return (string.IsNullOrEmpty(this._FieldValue)) ? this._FieldValue : this._FieldValue.PadRight(this.FieldLength);
            }
        }
    }

    public class ISO8583Engine_Message {

        #region Variables
        public string MessageType = string.Empty;
        public Dictionary<isoMessageElement, ISO8583Field> IsoFields = new Dictionary<isoMessageElement, ISO8583Field>(128);
        protected Dictionary<isoMessageElement, int> isoFieldLengths = new Dictionary<isoMessageElement, int>();
        protected Dictionary<isoMessageElement, int> FieldHeaderLength = new Dictionary<isoMessageElement, int>();
        public event EventHandler LogMessageEvent = null;
        #endregion

        internal ISO8583Engine_Message() { }

        public Encoding ISO8583MessageEncoder {
            get {
                return Encoding.ASCII;
            }
        }

        internal int GetFieldHeaderLength(isoMessageElement Index) {
            try {
                return this.FieldHeaderLength[Index];
            } catch (Exception) {
                return 0;   
            }
        }

        public bool AddField(ISO8583Field Field) {
            this.IsoFields.Add(Field.FieldIndex, Field);
            return true;
        }

        public bool AddField(isoMessageElement FieldIndex, string FieldValue) {
            try {
                ISO8583Field _Field = new ISO8583Field();
                _Field.FieldIndex = FieldIndex;
                _Field.FieldValue = FieldValue;
                _Field.FieldLength = isoFieldLengths[FieldIndex];
                _Field.IsFieldEnabled = true;
                if (this.IsoFields.ContainsKey(FieldIndex)) {
                    this.IsoFields[FieldIndex] = _Field;
                } else {
                    this.IsoFields.Add(FieldIndex, _Field);
                }
                return true;
            } catch (Exception) {
                return false;
            }
        }

        public byte[] Serialize() {
            try {
                string Message = string.Empty;
                StringBuilder BitmapBuilder = new StringBuilder();
                StringBuilder MessageBuilder = new StringBuilder();
                int BitmapIndex = 0;
                bool IsSecondaryBitmapEnabled = this.IsoFields.ContainsKey(isoMessageElement.Field1_SecondaryBitmap);
                int Bound = IsSecondaryBitmapEnabled ? 128 : 64;
                int BitmapLen = Bound / 8;
                byte[] BitmapBytes = new byte[BitmapLen];
                for (int i = 0; i < Bound; i++) {
                    isoMessageElement index = (isoMessageElement)i;
                    if (IsoFields.ContainsKey(index)) {
                        BitmapBuilder.Append("1");
                        MessageBuilder.Append(IsoFields[index].GetNormalizedFieldValue(FieldHeaderLength[index]));
                    } else {
                        BitmapBuilder.Append("0");
                    }
                    if (BitmapBuilder.Length == 8) {
                        BitmapBytes[BitmapIndex++] = Convert.ToByte(BitmapBuilder.ToString(), 2);
                        BitmapBuilder.Remove(0, BitmapBuilder.Length);
                    }
                }
                byte[] MessageTypeBytes = ISO8583MessageEncoder.GetBytes(MessageType);
                byte[] MessageFieldBytes = ISO8583MessageEncoder.GetBytes(MessageBuilder.ToString());
                byte[] MessageBytes = new byte[MessageTypeBytes.Length + BitmapLen + MessageFieldBytes.Length];
                Buffer.BlockCopy(MessageTypeBytes, 0, MessageBytes, 0, MessageTypeBytes.Length);
                Buffer.BlockCopy(BitmapBytes, 0, MessageBytes, MessageTypeBytes.Length, BitmapLen);
                Buffer.BlockCopy(MessageFieldBytes, 0, MessageBytes, MessageTypeBytes.Length + BitmapBytes.Length, MessageFieldBytes.Length);
                return MessageBytes;
            } catch (Exception) {
                return null;
            }
        }

        protected void LogMessage(string Message) {
            if (LogMessageEvent != null) {
                LogMessageEvent(Message, new EventArgs());
            }
        }

        private bool ReceiveAndParseResponse(Stream DataStream) {
            isoMessageElement index = isoMessageElement.Field1_SecondaryBitmap;
            try {
                this.IsoFields.Clear();
                byte[] MessageTypeBytes = new byte[4];
                DataStream.Read(MessageTypeBytes, 0, MessageTypeBytes.Length);
                this.MessageType = Encoding.UTF8.GetString(MessageTypeBytes);
                string BitmapString = string.Empty;
                int BitmapLength = 0;
                byte[] BitmapBytes = new byte[16];
                BitmapBytes[0] = (byte)DataStream.ReadByte();
                LogMessage(string.Format("First Byte: " + BitmapBytes[0].ToString()));
                if (BitmapBytes[0] >= 0x80) {
                    BitmapLength = 16;
                    DataStream.Read(BitmapBytes, 1, 15);
                    this.AddField(isoMessageElement.Field1_SecondaryBitmap, string.Empty);
                } else {
                    BitmapLength = 8;
                    DataStream.Read(BitmapBytes, 1, 7);
                }
                for (int i = 0; i < BitmapLength; i++) {
                    BitmapString += Convert.ToString(BitmapBytes[i], 2).PadLeft(8, '0');
                }
                LogMessage(BitmapString);
                for (int i = 1; i < BitmapString.Length; i++) {
                    if (BitmapString[i].Equals('1')) {
                        index = (isoMessageElement)i;
                        int FieldHeaderLengthVal = GetFieldHeaderLength(index);
                        int tmpSubstringLength = 0;
                        if (FieldHeaderLengthVal == 0) {
                            tmpSubstringLength = isoFieldLengths[index];
                        } else {
                            byte[] FieldHeaderBytes = new byte[FieldHeaderLengthVal];
                            DataStream.Read(FieldHeaderBytes, 0, FieldHeaderBytes.Length);
                            var tmp = ISO8583MessageEncoder.GetString(FieldHeaderBytes);
                            Int32.TryParse(tmp, out tmpSubstringLength);
                        }
                        string FieldData = string.Empty;
                        if (tmpSubstringLength != 0) {
                            byte[] FieldDataBytes = new byte[tmpSubstringLength];
                            DataStream.Read(FieldDataBytes, 0, FieldDataBytes.Length);
                            FieldData = ISO8583MessageEncoder.GetString(FieldDataBytes);
                        }
                        this.AddField(index, FieldData);
                    }
                }
                return true;
            } catch (KeyNotFoundException) {
                LogMessage("key not found: " + index.ToString());
                throw;
            } catch (Exception) {
                throw;
            }
        }
     
        public bool ParseResponse(byte[] ResponseBytes) {
            MemoryStream DataStream = new MemoryStream(ResponseBytes);
            return ReceiveAndParseResponse(DataStream);
        }

        public bool ParseResponse(ref Socket ClientSocket) {
            NetworkStream DataStream = new NetworkStream(ClientSocket);
            return ReceiveAndParseResponse(DataStream);
        }
    }
}
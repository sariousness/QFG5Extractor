using System;
using System.Collections.Generic;
using System.IO;

namespace QFG5Extractor.qfg5msg
{
    public class MsgConverter
    {
        public static void ExtractMsg(string inputFilePath, string outputFilePath)
        {
            Dictionary<int, char> firstLetterTable = new Dictionary<int, char>();
            Dictionary<int, char> secondLetterTable = new Dictionary<int, char>();
            Dictionary<int, char> thirdLetterTable = new Dictionary<int, char>();
            Dictionary<int, char> fourthLetterTable = new Dictionary<int, char>();
            Dictionary<int, char> remainderLetterTable = new Dictionary<int, char>();

            //carriage return and newline
            firstLetterTable.Add(0x1C, (char)0x0C);
            firstLetterTable.Add(0x1F, '\n');

            firstLetterTable.Add(0x00, '4'); firstLetterTable.Add(0x20, 't'); firstLetterTable.Add(0x30, 'T');
            firstLetterTable.Add(0x01, '6'); firstLetterTable.Add(0x21, 'v'); firstLetterTable.Add(0x31, 'V');
            firstLetterTable.Add(0x02, '0'); firstLetterTable.Add(0x22, 'p'); firstLetterTable.Add(0x32, 'P');
            firstLetterTable.Add(0x03, '2'); firstLetterTable.Add(0x23, 'r'); firstLetterTable.Add(0x33, 'R');
            firstLetterTable.Add(0x04, '<'); firstLetterTable.Add(0x24, '|'); firstLetterTable.Add(0x34, '|');
            firstLetterTable.Add(0x05, '>'); firstLetterTable.Add(0x25, '~'); firstLetterTable.Add(0x35, '~');
            firstLetterTable.Add(0x06, '8'); firstLetterTable.Add(0x26, 'x'); firstLetterTable.Add(0x36, 'X');
            firstLetterTable.Add(0x07, ':'); firstLetterTable.Add(0x27, 'z'); firstLetterTable.Add(0x37, 'Z');
            firstLetterTable.Add(0x08, '$'); firstLetterTable.Add(0x28, 'd'); firstLetterTable.Add(0x38, 'D');
            firstLetterTable.Add(0x09, '&'); firstLetterTable.Add(0x29, 'f'); firstLetterTable.Add(0x39, 'F');
            firstLetterTable.Add(0x0A, ' '); firstLetterTable.Add(0x2A, '`'); firstLetterTable.Add(0x3A, '@');
            firstLetterTable.Add(0x0B, '"'); firstLetterTable.Add(0x2B, 'b'); firstLetterTable.Add(0x3B, 'B');
            firstLetterTable.Add(0x0C, ','); firstLetterTable.Add(0x2C, 'l'); firstLetterTable.Add(0x3C, 'L');
            firstLetterTable.Add(0x0D, '.'); firstLetterTable.Add(0x2D, 'n'); firstLetterTable.Add(0x3D, 'N');
            firstLetterTable.Add(0x0E, '('); firstLetterTable.Add(0x2E, 'h'); firstLetterTable.Add(0x3E, 'H');
            firstLetterTable.Add(0x0F, '*'); firstLetterTable.Add(0x2F, 'j'); firstLetterTable.Add(0x3F, 'J');

            //carriage return and newline
            secondLetterTable.Add(0x09, (char)0x0C);
            secondLetterTable.Add(0x0A, '\n');

            secondLetterTable.Add(0x10, '>'); secondLetterTable.Add(0x20, '^'); secondLetterTable.Add(0x30, '~');
            secondLetterTable.Add(0x11, '<'); secondLetterTable.Add(0x21, '\\'); secondLetterTable.Add(0x31, '|');
            secondLetterTable.Add(0x12, ':'); secondLetterTable.Add(0x22, 'Z'); secondLetterTable.Add(0x32, 'z');
            secondLetterTable.Add(0x13, '8'); secondLetterTable.Add(0x23, 'X'); secondLetterTable.Add(0x33, 'x');
            secondLetterTable.Add(0x14, '6'); secondLetterTable.Add(0x24, 'V'); secondLetterTable.Add(0x34, 'v');
            secondLetterTable.Add(0x15, '4'); secondLetterTable.Add(0x25, 'T'); secondLetterTable.Add(0x35, 't');
            secondLetterTable.Add(0x16, '2'); secondLetterTable.Add(0x26, 'R'); secondLetterTable.Add(0x36, 'r'); secondLetterTable.Add(0x46, '\'');
            secondLetterTable.Add(0x17, '0'); secondLetterTable.Add(0x27, 'P'); secondLetterTable.Add(0x37, 'p');
            secondLetterTable.Add(0x18, '.'); secondLetterTable.Add(0x28, 'N'); secondLetterTable.Add(0x38, 'n');
            secondLetterTable.Add(0x19, ','); secondLetterTable.Add(0x29, 'L'); secondLetterTable.Add(0x39, 'l');
            secondLetterTable.Add(0x1A, '*'); secondLetterTable.Add(0x2A, 'J'); secondLetterTable.Add(0x3A, 'j');
            secondLetterTable.Add(0x1B, '('); secondLetterTable.Add(0x2B, 'H'); secondLetterTable.Add(0x3B, 'h');
            secondLetterTable.Add(0x1C, '&'); secondLetterTable.Add(0x2C, 'F'); secondLetterTable.Add(0x3C, 'f');
            secondLetterTable.Add(0x1D, '$'); secondLetterTable.Add(0x2D, 'D'); secondLetterTable.Add(0x3D, 'd');
            secondLetterTable.Add(0x1E, '"'); secondLetterTable.Add(0x2E, 'B'); secondLetterTable.Add(0x3E, 'b');
            secondLetterTable.Add(0x1F, ' '); secondLetterTable.Add(0x2F, '@'); secondLetterTable.Add(0x3F, '`');

            //carriage return and newline
            thirdLetterTable.Add(0x18, '\n');
            thirdLetterTable.Add(0x1B, (char)0x0C);
            
            thirdLetterTable.Add(0x54, '\'');

            thirdLetterTable.Add(0x00, ':'); thirdLetterTable.Add(0x20, 'z'); thirdLetterTable.Add(0x30, 'Z');
            thirdLetterTable.Add(0x01, '8'); thirdLetterTable.Add(0x21, 'x'); thirdLetterTable.Add(0x31, 'X');
            thirdLetterTable.Add(0x02, '>'); thirdLetterTable.Add(0x22, '~'); thirdLetterTable.Add(0x32, '^');
            thirdLetterTable.Add(0x03, '<'); thirdLetterTable.Add(0x23, '|'); thirdLetterTable.Add(0x33, '\\');
            thirdLetterTable.Add(0x04, '2'); thirdLetterTable.Add(0x24, 'r'); thirdLetterTable.Add(0x34, 'R');
            thirdLetterTable.Add(0x05, '0'); thirdLetterTable.Add(0x25, 'p'); thirdLetterTable.Add(0x35, 'P');
            thirdLetterTable.Add(0x06, '6'); thirdLetterTable.Add(0x26, 'v'); thirdLetterTable.Add(0x36, 'V');
            thirdLetterTable.Add(0x07, '4'); thirdLetterTable.Add(0x27, 't'); thirdLetterTable.Add(0x37, 'T');
            thirdLetterTable.Add(0x08, '*'); thirdLetterTable.Add(0x28, 'j'); thirdLetterTable.Add(0x38, 'J');
            thirdLetterTable.Add(0x09, '('); thirdLetterTable.Add(0x29, 'h'); thirdLetterTable.Add(0x39, 'H');
            thirdLetterTable.Add(0x0A, '.'); thirdLetterTable.Add(0x2A, 'n'); thirdLetterTable.Add(0x3A, 'N');
            thirdLetterTable.Add(0x0B, ','); thirdLetterTable.Add(0x2B, 'l'); thirdLetterTable.Add(0x3B, 'L');
            thirdLetterTable.Add(0x0C, '"'); thirdLetterTable.Add(0x2C, 'b'); thirdLetterTable.Add(0x3C, 'B');
            thirdLetterTable.Add(0x0D, ' '); thirdLetterTable.Add(0x2D, '`'); thirdLetterTable.Add(0x3D, '@');
            thirdLetterTable.Add(0x0E, '&'); thirdLetterTable.Add(0x2E, 'f'); thirdLetterTable.Add(0x3E, 'F');
            thirdLetterTable.Add(0x0F, '$'); thirdLetterTable.Add(0x2F, 'd'); thirdLetterTable.Add(0x3F, 'D');

            //carriage return and newline
            fourthLetterTable.Add(0x49, '\n');
            fourthLetterTable.Add(0x4A, (char)0x0C);

            fourthLetterTable.Add(0x05, '\'');

            fourthLetterTable.Add(0x50, '8'); fourthLetterTable.Add(0x60, 'X'); fourthLetterTable.Add(0x70, 'x');
            fourthLetterTable.Add(0x51, ':'); fourthLetterTable.Add(0x61, 'Z'); fourthLetterTable.Add(0x71, 'z');
            fourthLetterTable.Add(0x52, '<'); fourthLetterTable.Add(0x62, '\\'); fourthLetterTable.Add(0x72, '|');
            fourthLetterTable.Add(0x53, '>'); fourthLetterTable.Add(0x63, '^'); fourthLetterTable.Add(0x73, '~');
            fourthLetterTable.Add(0x54, '0'); fourthLetterTable.Add(0x64, 'P'); fourthLetterTable.Add(0x74, 'p');
            fourthLetterTable.Add(0x55, '2'); fourthLetterTable.Add(0x65, 'R'); fourthLetterTable.Add(0x75, 'r');
            fourthLetterTable.Add(0x56, '4'); fourthLetterTable.Add(0x66, 'T'); fourthLetterTable.Add(0x76, 't');
            fourthLetterTable.Add(0x57, '6'); fourthLetterTable.Add(0x67, 'V'); fourthLetterTable.Add(0x77, 'v');
            fourthLetterTable.Add(0x58, '('); fourthLetterTable.Add(0x68, 'H'); fourthLetterTable.Add(0x78, 'h');
            fourthLetterTable.Add(0x59, '*'); fourthLetterTable.Add(0x69, 'J'); fourthLetterTable.Add(0x79, 'j');
            fourthLetterTable.Add(0x5A, ','); fourthLetterTable.Add(0x6A, 'L'); fourthLetterTable.Add(0x7A, 'l');
            fourthLetterTable.Add(0x5B, '.'); fourthLetterTable.Add(0x6B, 'N'); fourthLetterTable.Add(0x7B, 'n');
            fourthLetterTable.Add(0x5C, ' '); fourthLetterTable.Add(0x6C, '@'); fourthLetterTable.Add(0x7C, '`');
            fourthLetterTable.Add(0x5D, '"'); fourthLetterTable.Add(0x6D, 'B'); fourthLetterTable.Add(0x7D, 'b');
            fourthLetterTable.Add(0x5E, '$'); fourthLetterTable.Add(0x6E, 'D'); fourthLetterTable.Add(0x7E, 'd');
            fourthLetterTable.Add(0x5F, '&'); fourthLetterTable.Add(0x6F, 'F'); fourthLetterTable.Add(0x7F, 'f');

            //Standard ASCII table in reverse from 0x80

            //carriage return and newline
            remainderLetterTable.Add(0xF2, '\r');
            remainderLetterTable.Add(0xF5, '\n');

            remainderLetterTable.Add(0x80, (char)0x7F); remainderLetterTable.Add(0x90, 'o'); remainderLetterTable.Add(0xA0, '_'); remainderLetterTable.Add(0xB0, 'O'); remainderLetterTable.Add(0xC0, '?'); remainderLetterTable.Add(0xD0, '/');
            remainderLetterTable.Add(0x81, '~'); remainderLetterTable.Add(0x91, 'n'); remainderLetterTable.Add(0xA1, '^'); remainderLetterTable.Add(0xB1, 'N'); remainderLetterTable.Add(0xC1, '>'); remainderLetterTable.Add(0xD1, '.');
            remainderLetterTable.Add(0x82, '}'); remainderLetterTable.Add(0x92, 'm'); remainderLetterTable.Add(0xA2, ']'); remainderLetterTable.Add(0xB2, 'M'); remainderLetterTable.Add(0xC2, '='); remainderLetterTable.Add(0xD2, '-');
            remainderLetterTable.Add(0x83, '|'); remainderLetterTable.Add(0x93, 'l'); remainderLetterTable.Add(0xA3, '\\'); remainderLetterTable.Add(0xB3, 'L'); remainderLetterTable.Add(0xC3, '<'); remainderLetterTable.Add(0xD3, ',');
            remainderLetterTable.Add(0x84, '{'); remainderLetterTable.Add(0x94, 'k'); remainderLetterTable.Add(0xA4, '['); remainderLetterTable.Add(0xB4, 'K'); remainderLetterTable.Add(0xC4, ';'); remainderLetterTable.Add(0xD4, '+');
            remainderLetterTable.Add(0x85, 'z'); remainderLetterTable.Add(0x95, 'j'); remainderLetterTable.Add(0xA5, 'Z'); remainderLetterTable.Add(0xB5, 'J'); remainderLetterTable.Add(0xC5, ':'); remainderLetterTable.Add(0xD5, '*');
            remainderLetterTable.Add(0x86, 'y'); remainderLetterTable.Add(0x96, 'i'); remainderLetterTable.Add(0xA6, 'Y'); remainderLetterTable.Add(0xB6, 'I'); remainderLetterTable.Add(0xC6, '9'); remainderLetterTable.Add(0xD6, ')');
            remainderLetterTable.Add(0x87, 'x'); remainderLetterTable.Add(0x97, 'h'); remainderLetterTable.Add(0xA7, 'X'); remainderLetterTable.Add(0xB7, 'H'); remainderLetterTable.Add(0xC7, '8'); remainderLetterTable.Add(0xD7, '(');
            remainderLetterTable.Add(0x88, 'w'); remainderLetterTable.Add(0x98, 'g'); remainderLetterTable.Add(0xA8, 'W'); remainderLetterTable.Add(0xB8, 'G'); remainderLetterTable.Add(0xC8, '7'); remainderLetterTable.Add(0xD8, '\'');
            remainderLetterTable.Add(0x89, 'v'); remainderLetterTable.Add(0x99, 'f'); remainderLetterTable.Add(0xA9, 'V'); remainderLetterTable.Add(0xB9, 'F'); remainderLetterTable.Add(0xC9, '6'); remainderLetterTable.Add(0xD9, '&');
            remainderLetterTable.Add(0x8A, 'u'); remainderLetterTable.Add(0x9A, 'e'); remainderLetterTable.Add(0xAA, 'U'); remainderLetterTable.Add(0xBA, 'E'); remainderLetterTable.Add(0xCA, '5'); remainderLetterTable.Add(0xDA, '%');
            remainderLetterTable.Add(0x8B, 't'); remainderLetterTable.Add(0x9B, 'd'); remainderLetterTable.Add(0xAB, 'T'); remainderLetterTable.Add(0xBB, 'D'); remainderLetterTable.Add(0xCB, '4'); remainderLetterTable.Add(0xDB, '$');
            remainderLetterTable.Add(0x8C, 's'); remainderLetterTable.Add(0x9C, 'c'); remainderLetterTable.Add(0xAC, 'S'); remainderLetterTable.Add(0xBC, 'C'); remainderLetterTable.Add(0xCC, '3'); remainderLetterTable.Add(0xDC, '#');
            remainderLetterTable.Add(0x8D, 'r'); remainderLetterTable.Add(0x9D, 'b'); remainderLetterTable.Add(0xAD, 'R'); remainderLetterTable.Add(0xBD, 'B'); remainderLetterTable.Add(0xCD, '2'); remainderLetterTable.Add(0xDD, '"');
            remainderLetterTable.Add(0x8E, 'q'); remainderLetterTable.Add(0x9E, 'a'); remainderLetterTable.Add(0xAE, 'Q'); remainderLetterTable.Add(0xBE, 'A'); remainderLetterTable.Add(0xCE, '1'); remainderLetterTable.Add(0xDE, '!');
            remainderLetterTable.Add(0x8F, 'p'); remainderLetterTable.Add(0x9F, '`'); remainderLetterTable.Add(0xAF, 'P'); remainderLetterTable.Add(0xBF, '@'); remainderLetterTable.Add(0xCF, '0'); remainderLetterTable.Add(0xDF, ' ');

            string output = null;

            byte[] msgData = File.ReadAllBytes(inputFilePath);
            
            int fileHeaderLength = 16;
            int dataStartIndex = fileHeaderLength;
            int msgHeaderLength = 32;
            int msgAudIdLength = 13;
            int unknownAddrLength = 4;

            int fileFormatVersion;

            bool msgObfuscated = false;

            /* Temp vars (per msg) */

            string tmpOutput = null;

            byte[] tmpMsgData;

            int tmpTextLength;
            int tmpTextStartIndex;
            int tmpShift;
            int tmpRemainder;
            int tmpKey;

            char tmpFirst;
            char tmpSecond;
            char tmpThird;
            char tmpFourth;

            fileFormatVersion = msgData[4];

            //version differences 
            //v2 = qfg5demo, v3 = qfg5, v4 = qfg5 + obfuscation
            if (fileFormatVersion > 3) {
                msgObfuscated = true;
            }
            else if (fileFormatVersion == 2)
            {
                fileHeaderLength = 14;
                dataStartIndex = fileHeaderLength;
                msgHeaderLength = 28;
            }

            while ((dataStartIndex + 1) < msgData.Length)
            {
                tmpOutput = null;
                byte[] tmp;

                if (fileFormatVersion == 2)
                {
                    tmp = new byte[] { msgData[dataStartIndex + 20], msgData[dataStartIndex + 21] }; //Read text length from header
                }
                else
                {
                    tmp = new byte[] { msgData[dataStartIndex + 24], msgData[dataStartIndex + 25] }; //Read text length from header
                }
                tmpTextLength = BitConverter.ToInt16(tmp, 0);

                //Determine text start
                tmpTextStartIndex = dataStartIndex + msgHeaderLength;

                //Skip audio ID's
                if (msgData[tmpTextStartIndex] == 0x41 && msgData[tmpTextStartIndex + 1] == 0x30) //Audio ID's begin with A0
                {
                    while (msgData[tmpTextStartIndex] == 0x41 && msgData[tmpTextStartIndex + 1] == 0x30)
                    {
                        tmpTextStartIndex += msgAudIdLength;
                    }
                }

                if (tmpTextLength == 0)
                {
                    dataStartIndex = tmpTextStartIndex + unknownAddrLength; //next data start
                }
                else
                {
                    dataStartIndex = tmpTextStartIndex + tmpTextLength + unknownAddrLength; //next data start

                    if (!msgObfuscated)
                    {
                        for (int j = tmpTextStartIndex; j < tmpTextStartIndex + tmpTextLength; j += 1)
                        {
                            tmpOutput += Convert.ToString((char)msgData[j]);
                        }
                        
                        output += tmpOutput + Environment.NewLine + Environment.NewLine;

                        continue;
                    }

                    for (int j = tmpTextStartIndex; j < tmpTextStartIndex + tmpTextLength; j += 4)
                    {

                        tmpFirst = (char)0;
                        tmpSecond = (char)0;
                        tmpThird = (char)0;
                        tmpFourth = (char)0;

                        tmpRemainder = (tmpTextStartIndex + tmpTextLength) - j;

                        //get bytes
                        if (tmpRemainder > 3)
                        {
                            tmpMsgData = new byte[4] { msgData[j], msgData[j + 1], msgData[j + 2], msgData[j + 3] };
                        }
                        else
                        {
                            if (tmpRemainder == 3)
                            {
                                tmpMsgData = new byte[3];

                                tmpMsgData[0] = msgData[j];
                                tmpMsgData[1] = msgData[j + 1];
                                tmpMsgData[2] = msgData[j + 2];
                            }
                            else if (tmpRemainder == 2)
                            {
                                tmpMsgData = new byte[2];

                                tmpMsgData[0] = msgData[j];
                                tmpMsgData[1] = msgData[j + 1];
                            }
                            else
                            {
                                tmpMsgData = new byte[1];

                                tmpMsgData[0] = msgData[j];
                            }
                        }

                        //If remaining bytes are less than 4 use remainderLetterTable
                        if (tmpRemainder < 4)
                        {
                            tmpFirst = (remainderLetterTable.ContainsKey(tmpMsgData[0])) ? remainderLetterTable[tmpMsgData[0]] : (char)0;

                            if (tmpRemainder == 3)
                            {
                                tmpSecond = (remainderLetterTable.ContainsKey(tmpMsgData[1])) ? remainderLetterTable[tmpMsgData[1]] : (char)0;
                                tmpThird = (remainderLetterTable.ContainsKey(tmpMsgData[2])) ? remainderLetterTable[tmpMsgData[2]] : (char)0;                            
                            }
                            else if (tmpRemainder == 2)
                            {
                                tmpSecond = (remainderLetterTable.ContainsKey(tmpMsgData[1])) ? remainderLetterTable[tmpMsgData[1]] : (char)0;
                            }

                        }
                        else
                        {
                            for (int k = 0; k < tmpMsgData.Length; k++)
                            {

                                switch (k)
                                {
                                    //third letter
                                    case 0:
                                        //check if previous byte is high, if so 'shift' letter by 1
                                        tmpShift = (tmpMsgData[3] > 127) ? 1 : 0;

                                        //if this byte is above 127, subtract 128 to get table value
                                        if (tmpMsgData[k] > 127)
                                        {
                                            tmpKey = tmpMsgData[k] - 128;

                                            tmpThird = (thirdLetterTable.ContainsKey(tmpKey)) ? (char)(thirdLetterTable[tmpKey] + tmpShift) : (char)0;
                                        }
                                        else
                                        {
                                            tmpThird = (thirdLetterTable.ContainsKey(tmpMsgData[k])) ? (char)(thirdLetterTable[tmpMsgData[k]] + tmpShift) : (char)0;
                                        }

                                        break;
                                    //fourth letter
                                    case 1:
                                        //check if previous byte is high, if so 'shift' letter by 1
                                        tmpShift = (tmpMsgData[0] > 127) ? 1 : 0;

                                        //if this byte is above 127, subtract 128 to get table value
                                        if (tmpMsgData[k] > 127)
                                        {
                                            tmpKey = tmpMsgData[k] - 128;

                                            tmpFourth = (fourthLetterTable.ContainsKey(tmpKey)) ? (char)(fourthLetterTable[tmpKey] + tmpShift) : (char)0;
                                        }
                                        else
                                        {
                                            tmpFourth = (fourthLetterTable.ContainsKey(tmpMsgData[k])) ? (char)(fourthLetterTable[tmpMsgData[k]] + tmpShift) : (char)0;
                                        }

                                        break;
                                    //first letter
                                    case 2:

                                        //EXCEPTION TO RULE
                                        //check if previous byte is low, if so 'shift' letter by 1
                                        tmpShift = (tmpMsgData[1] < 128) ? 1 : 0;

                                        //if this byte is above 127, subtract 128 to get table value
                                        if (tmpMsgData[k] > 127)
                                        {
                                            tmpKey = tmpMsgData[k] - 128;

                                            tmpFirst = (firstLetterTable.ContainsKey(tmpKey)) ? (char)(firstLetterTable[tmpKey] + tmpShift) : (char)0;
                                        }
                                        else
                                        {
                                            tmpFirst = (firstLetterTable.ContainsKey(tmpMsgData[k])) ? (char)(firstLetterTable[tmpMsgData[k]] + tmpShift) : (char)0;
                                        }

                                        break;
                                    //second letter
                                    case 3:
                                        //check if previous byte is high, if so 'shift' letter by 1
                                        tmpShift = (tmpMsgData[2] > 127) ? 1 : 0;

                                        //if this byte is above 127, subtract 128 to get table value
                                        if (tmpMsgData[k] > 127)
                                        {
                                            tmpKey = tmpMsgData[k] - 128;

                                            tmpSecond = (secondLetterTable.ContainsKey(tmpKey)) ? (char)(secondLetterTable[tmpKey] + tmpShift) : (char)0;
                                        }
                                        else
                                        {
                                            tmpSecond = (secondLetterTable.ContainsKey(tmpMsgData[k])) ? (char)(secondLetterTable[tmpMsgData[k]] + tmpShift) : (char)0;
                                        }
                                        break;
                                }
                            }
                        }

                        if (tmpFirst != (char)0)
                        {
                            tmpOutput += Convert.ToString(tmpFirst);
                        }
                        if (tmpSecond != (char)0)
                        {
                            tmpOutput += Convert.ToString(tmpSecond);
                        }
                        if (tmpThird != (char)0)
                        {
                            tmpOutput += Convert.ToString(tmpThird);
                        }
                        if (tmpFourth != (char)0)
                        {
                            tmpOutput += Convert.ToString(tmpFourth);
                        }
                    }
                }

                if (tmpOutput != null)
                {
                    output += tmpOutput + Environment.NewLine + Environment.NewLine;
                }

            }

            File.WriteAllText(outputFilePath, output);
        }
    }
}

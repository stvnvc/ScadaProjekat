using Common;
using Modbus.FunctionParameters;
using System;
using System.Collections.Generic;
using System.Net;
using System.Reflection;

namespace Modbus.ModbusFunctions
{
    /// <summary>
    /// Class containing logic for parsing and packing modbus read holding registers functions/requests.
    /// </summary>
    public class ReadHoldingRegistersFunction : ModbusFunction
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ReadHoldingRegistersFunction"/> class.
        /// </summary>
        /// <param name="commandParameters">The modbus command parameters.</param>
        public ReadHoldingRegistersFunction(ModbusCommandParameters commandParameters) : base(commandParameters)
        {
            CheckArguments(MethodBase.GetCurrentMethod(), typeof(ModbusReadCommandParameters));
        }

        /// <inheritdoc />
        public override byte[] PackRequest() //citanje je isto kao kod digitalnog citanja
        {
            ModbusCommandParameters paramCom = this.CommandParameters as ModbusCommandParameters;

            byte[] request = new byte[12];

            Buffer.BlockCopy((Array)BitConverter.GetBytes( //pretvaranje short u byte[2]
                    IPAddress.HostToNetworkOrder( //LTE <-> BIG ENDIAN
                        (short)paramCom.TransactionId)), //sta kopiramo
                0, //odakle kopiramo
                (Array)request, //gde kopiramo
                0,  //na koje mesto kopiramo
                2); //koliko bajtova kopiramo
            Buffer.BlockCopy((Array)BitConverter.GetBytes(IPAddress.HostToNetworkOrder((short)paramCom.ProtocolId)), 0, (Array)request, 2, 2);
            Buffer.BlockCopy((Array)BitConverter.GetBytes(IPAddress.HostToNetworkOrder((short)paramCom.Length)), 0, (Array)request, 4, 2);


            request[6] = paramCom.UnitId;
            request[7] = paramCom.FunctionCode;
            Buffer.BlockCopy((Array)BitConverter.GetBytes(IPAddress.HostToNetworkOrder((short)(paramCom as ModbusReadCommandParameters).StartAddress)), 0, (Array)request, 8, 2);
            Buffer.BlockCopy((Array)BitConverter.GetBytes(IPAddress.HostToNetworkOrder((short)(paramCom as ModbusReadCommandParameters).Quantity)), 0, (Array)request, 10, 2);

            return request;
        }

        /// <inheritdoc />
        public override Dictionary<Tuple<PointType, ushort>, ushort> ParseResponse(byte[] response)
        {
            ModbusReadCommandParameters paramCom = this.CommandParameters as ModbusReadCommandParameters;
            Dictionary<Tuple<PointType, ushort>, ushort> d = new Dictionary<Tuple<PointType, ushort>, ushort>();

            ushort address = paramCom.StartAddress;
            byte byteCount = response[8]; //data deo

            for (int i = 0; i < byteCount/2; i ++)
            {
                byte byte1 = response[8 + 1 + i* 2]; //9-i bajt je prvi podatak, u sledecm krugu 11-i bajt je prvi podatak...
                byte byte2 = response[8 + 2 + i* 2]; //10-i bajt je drugi podatak, u sledecem krugu 12-i bajt je drugi podatak...

                ushort value = BitConverter.ToUInt16(new byte[2] { byte2, byte1 }, 0); //spajamo 2 bajta u jedan ushort
            
                d.Add(new Tuple<PointType, ushort>(PointType.ANALOG_OUTPUT, address), value);
                address++; //upis na adresi 1000, pa na 1001, pa na 1002...
            }
            return d;
        }
    }
}
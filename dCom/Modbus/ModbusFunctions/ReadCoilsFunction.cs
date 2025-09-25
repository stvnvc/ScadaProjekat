using Common;
using Modbus.FunctionParameters;
using System;
using System.Collections.Generic;
using System.Net;
using System.Reflection;

namespace Modbus.ModbusFunctions
{
    /// <summary>
    /// Class containing logic for parsing and packing modbus read coil functions/requests.
    /// </summary>
    public class ReadCoilsFunction : ModbusFunction
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ReadCoilsFunction"/> class.
        /// </summary>
        /// <param name="commandParameters">The modbus command parameters.</param>
		public ReadCoilsFunction(ModbusCommandParameters commandParameters) : base(commandParameters)
        {
            CheckArguments(MethodBase.GetCurrentMethod(), typeof(ModbusReadCommandParameters));
        }

        /// <inheritdoc/>
        public override byte[] PackRequest()
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

            int q = response[8];

            for (int i = 0; i < q; i++)
            {
               
                for (int j = 0; j < 8; j++)
                {
                    if(paramCom.Quantity < (j+i*8))
                    {
                        break;
                    }

                    //9+i jer od 9. bajta pocinju podaci

                    ushort v = (ushort)((response[9 + i]) & (byte)0x1); //preklapamo masku
                    response[9 + i] /= 2; //pomeramo bitove u desno za 1 mesto

                    d.Add(new Tuple<PointType, ushort>(PointType.DIGITAL_OUTPUT, (ushort)(paramCom.StartAddress + j + i * 8)), v); //dodajemo u recnik
                }
            }
            return d;
        }
    }
}
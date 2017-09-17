using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using PCSC;
using PCSC.Iso7816;
using Console = Colorful.Console;

namespace Hackiibo
{
    public class MifareUltralight : IDisposable
    {
        private const byte CUSTOM_CLA = 0xFF;

        private readonly ISCardContext _context;
        private readonly IsoReader _isoReader;

        public MifareUltralight(ISCardContext context, IsoReader isoReader)
        {
            _context = context;
            _isoReader = isoReader;
        }

        public byte[] ReadPages(byte pageIndex, byte readSize = 0x04)
        {
            unchecked
            {
                var isFirstBlock = pageIndex == 0;

                var readBinaryCmd = new CommandApdu(IsoCase.Case2Short, SCardProtocol.Any)
                {
                    CLA = CUSTOM_CLA,
                    Instruction = InstructionCode.ReadBinary,
                    P1 = 0x00,
                    P2 = pageIndex,
                    Le = isFirstBlock ? 0x10 : readSize
                };

                Console.WriteLineFormatted("--> Read MIFARE Block = {0}", Color.Blue, Color.White, pageIndex);
                Console.WriteLineFormatted("--> C-APDU: {0}", Color.Blue, Color.White, BitConverter.ToString(readBinaryCmd.ToArray()));

                var responses = _isoReader.Transmit(readBinaryCmd);

                DumpResponse(responses);

                if (IsSuccess(responses))
                    return responses.GetData();

                return null;
            }

        }

        private static void DumpResponse(Response responses)
        {
            foreach (var response in responses)
            {
                Console.WriteLineFormatted("<-- R-APDU: {0}",
                    Color.Green,
                    Color.White,
                    BitConverter.ToString(response.FullApdu));

                var responseCode = string.Format("{0:X2} {1:X2}",
                    response.SW1,
                    response.SW2);

                Console.WriteLineFormatted("    SW1 SW2: {0}",
                    Color.Green,
                    Color.White,
                    responseCode);

                if (response.HasData)
                {
                    Console.WriteLineFormatted("    Data: {0}",
                        Color.Green,
                        Color.White,
                        BitConverter.ToString(response.GetData()));
                }
            }
        }

        public bool WritePage(byte pageIndex, byte[] data)
        {
            var updateBinaryCmd = new CommandApdu(IsoCase.Case3Short, SCardProtocol.Any)
            {
                CLA = CUSTOM_CLA,
                Instruction = InstructionCode.UpdateBinary,
                P1 = 0x00,
                P2 = pageIndex,
                Data = data
            };

            Console.WriteLineFormatted("--> Write MIFARE Block = {0}", Color.Blue, Color.White, pageIndex);
            Console.WriteLineFormatted("--> C-APDU: {0}", Color.Blue, Color.White, BitConverter.ToString(updateBinaryCmd.ToArray()));

            var responses = _isoReader.Transmit(updateBinaryCmd);

            DumpResponse(responses);

            return IsSuccess(responses);
        }

        public void Close()
        {
            _isoReader.Reader.Disconnect(SCardReaderDisposition.Eject);
        }

        public static MifareUltralight GetTagInfo()
        {
            var contextFactory = ContextFactory.Instance;

            var context = contextFactory.Establish(SCardScope.System);

            var readerName = ChooseReader(context);

            if (readerName == null)
            {
                return null;
            }

            var isoReader = new IsoReader(context, readerName, SCardShareMode.Shared, SCardProtocol.Any, false);

            PrintReaderStatus(isoReader.Reader);

            return new MifareUltralight(context, isoReader);
        }

        /// <summary>
        /// Asks the user to select a smartcard reader containing the Mifare chip
        /// </summary>
        /// <param name="readerNames">Collection of available smartcard readers</param>
        /// <returns>The selected reader name or <c>null</c> if none</returns>
        private static string ChooseReader(ISCardContext context)
        {
            var readerNames = context.GetReaders();

            if (NoReaderAvailable(readerNames))
            {
                Console.WriteLine("You need at least one reader.", Color.Red);
                return null;
            }

            if (readerNames.Length == 1)
            {
                return readerNames.Single();
            }

            // Show available readers.
            Console.WriteLine("Available readers: ", Color.White);
            for (var i = 0; i < readerNames.Length; i++)
            {
                Console.WriteLine("[{0}] {1}", Color.White, i, readerNames[i]);
            }

            // Ask the user which one to choose.
            Console.Write("Which reader has an inserted NTAG215 card? ", Color.White);

            var line = Console.ReadLine();

            if (int.TryParse(line, out int choice) && (choice >= 0) && (choice <= readerNames.Length))
            {
                return readerNames[choice];
            }

            Console.WriteLine("An invalid number has been entered.", Color.Red);

            return null;
        }

        private static bool NoReaderAvailable(ICollection<string> readerNames)
        {
            return readerNames == null || readerNames.Count < 1;
        }

        /// <summary>
        /// Queries the reader's status and prints it out
        /// </summary>
        /// <param name="reader">Connected reader</param>
        private static byte[] PrintReaderStatus(ISCardReader reader)
        {
            var sc = reader.Status(
                out string[] readerNames, // contains the reader name(s)
                out SCardState state, // contains the current state (flags)
                out SCardProtocol proto, // contains the currently used communication protocol
                out byte[] atr); // contains the ATR

            if (sc != SCardError.Success)
            {
                Console.WriteLine("Unable to retrieve card status.\nError message: {0}", Color.Red, SCardHelper.StringifyError(sc));
                return null;
            }

            if (atr == null || atr.Length <= 0)
            {
                Console.WriteLine("Unable to retrieve card ATR.", Color.Red);
                return null; ;
            }

            Console.WriteLineFormatted("Current reader name: {0}", Color.Yellow, Color.White, reader.ReaderName);
            Console.WriteLineFormatted("Connected with protocol {0} in state {1}", Color.Yellow, Color.White, proto, state);
            Console.WriteLineFormatted("Card ATR: {0}", Color.Yellow, Color.White, BitConverter.ToString(atr));

            return atr;
        }

        private bool IsSuccess(Response response) => (response.SW1 == (byte)SW1Code.Normal) && (response.SW2 == 0x00);

        public void Dispose()
        {
            _isoReader.Dispose();
            _context.Dispose();
        }
    }
}
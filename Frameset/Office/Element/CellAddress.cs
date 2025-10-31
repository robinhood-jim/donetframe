using System;

namespace Frameset.Office.Element
{
    public class CellAddress
    {
        private static char ABSOLUTE_REFERENCE_MARKER = '$';
        private static int COL_RADIX = 'Z' - 'A' + 1;

        private int row;
        private int col;
        public CellAddress(int row, int col)
        {
            this.row = row;
            this.col = col;
        }
        public CellAddress(String address)
        {
            SetAddress(address);
        }
        public void SetPos(int row, int col)
        {
            this.row = row;
            this.col = col;
        }
        public void SetAddress(String address)
        {
            int length = address.Length;
            if (length == 0)
            {
                row = 0;
                col = 0;
            }
            else
            {
                int offset = address[0] == ABSOLUTE_REFERENCE_MARKER ? 1 : 0;
                int col = 0;
                for (; offset < length; offset++)
                {
                    char c = address[offset];
                    if (c == ABSOLUTE_REFERENCE_MARKER)
                    {
                        offset++;
                        break; //next there must be digits
                    }
                    if (isAsciiDigit(c))
                    {
                        break;
                    }
                    col = col * COL_RADIX + toUpperCase(c) - (int)'A' + 1;
                }
                this.col = col - 1;
                this.row = Convert.ToInt32(address.Substring(offset)) - 1;
            }
        }
        private static bool isAsciiDigit(char c)
        {
            return '0' <= c && c <= '9';
        }
        private static char toUpperCase(char c)
        {
            if (isAsciiUpperCase(c))
            {
                return c;
            }
            if (isAsciiLowerCase(c))
            {
                return (char)(c + ('A' - 'a'));
            }
            throw new MissingFieldException("Unexpected char: " + c);
        }
        private static bool isAsciiLowerCase(char c)
        {
            return 'a' <= c && c <= 'z';
        }

        private static bool isAsciiUpperCase(char c)
        {
            return 'A' <= c && c <= 'Z';
        }
        public int GetColumn()
        {
            return col;
        }
        public int GetRow()
        {
            return row;
        }
    }

}

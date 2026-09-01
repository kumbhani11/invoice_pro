using System;
using System.Text;

namespace InvoicePro.Utils;

public static class NumberToWordsConverter
{
    private static readonly string[] Ones = {
        "", "One", "Two", "Three", "Four", "Five", "Six", "Seven", "Eight", "Nine", "Ten",
        "Eleven", "Twelve", "Thirteen", "Fourteen", "Fifteen", "Sixteen", "Seventeen", "Eighteen", "Nineteen"
    };

    private static readonly string[] Tens = {
        "", "", "Twenty", "Thirty", "Forty", "Fifty", "Sixty", "Seventy", "Eighty", "Ninety"
    };

    public static string ConvertAmount(decimal amount)
    {
        long rupees = (long)Math.Floor(amount);
        int paise = (int)((amount - rupees) * 100);

        string rupeesText = ConvertToWords(rupees);
        string result = rupeesText == "" ? "Zero Rupees" : rupeesText + " Rupees";

        if (paise > 0)
        {
            result += " and " + ConvertToWords(paise) + " Paise";
        }

        return result + " Only";
    }

    private static string ConvertToWords(long number)
    {
        if (number == 0) return "";

        if (number < 0) return "Minus " + ConvertToWords(Math.Abs(number));

        StringBuilder words = new StringBuilder();

        if ((number / 10000000) > 0)
        {
            words.Append(ConvertToWords(number / 10000000) + " Crore ");
            number %= 10000000;
        }

        if ((number / 100000) > 0)
        {
            words.Append(ConvertToWords(number / 100000) + " Lakh ");
            number %= 100000;
        }

        if ((number / 1000) > 0)
        {
            words.Append(ConvertToWords(number / 1000) + " Thousand ");
            number %= 1000;
        }

        if ((number / 100) > 0)
        {
            words.Append(ConvertToWords(number / 100) + " Hundred ");
            number %= 100;
        }

        if (number > 0)
        {
            if (number < 20)
                words.Append(Ones[number]);
            else
            {
                words.Append(Tens[number / 10]);
                if ((number % 10) > 0)
                    words.Append(" " + Ones[number % 10]);
            }
        }

        return words.ToString().Trim();
    }
}

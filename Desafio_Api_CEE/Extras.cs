namespace Desafio_Api_CEE
{
    public class Extras
    {
        public bool isConsecutive(string str)
        {
            // variable to store starting number
            int start;

            // length of the input string
            int length = str.Length;

            // find the number till half of the string
            for (int i = 0; i < length / 2; i++)
            {

                // new string containing the starting
                // substring of input string
                string new_str = str.Substring(0, i + 1);

                // converting starting substring into number
                int num = Int32.Parse(new_str);

                // backing up the starting number in start
                start = num;

                // while loop until the new_string is
                // smaller than input string
                while (new_str.Length < length)
                {

                    // next number
                    num++;

                    new_str += num.ToString();
                }
                if (new_str == str)
                    return true;
            }
            return false;

        }
    }
}

using System;

namespace PuppyProxy
{
    /// <summary>
    /// Commonly-used methods.
    /// </summary>
    public static class Common
    {
        /// <summary>
        /// Display a prompt and return a boolean response.
        /// </summary>
        /// <param name="question">The prompt to display.</param>
        /// <param name="yesDefault">Indicates whether or not 'true' is the default.</param>
        /// <returns>Boolean.</returns>
        public static bool InputBoolean(string question, bool yesDefault)
        {
            Console.Write(question);

            if (yesDefault) Console.Write(" [Y/n]? ");
            else Console.Write(" [y/N]? ");

            string userInput = Console.ReadLine();

            if (String.IsNullOrEmpty(userInput))
            {
                if (yesDefault) return true;
                return false;
            }

            userInput = userInput.ToLower();

            if (yesDefault)
            {
                if (
                    (String.Compare(userInput, "n") == 0)
                    || (String.Compare(userInput, "no") == 0)
                   )
                {
                    return false;
                }

                return true;
            }
            else
            {
                if (
                    (String.Compare(userInput, "y") == 0)
                    || (String.Compare(userInput, "yes") == 0)
                   )
                {
                    return true;
                }

                return false;
            }
        }

        /// <summary>
        /// Display a prompt and return a string response.
        /// </summary>
        /// <param name="question">The prompt to display.</param>
        /// <param name="defaultAnswer">The default response.</param>
        /// <param name="allowNull">Indicates whether or not null is acceptable.</param>
        /// <returns>String.</returns>
        public static string InputString(string question, string defaultAnswer, bool allowNull)
        {
            while (true)
            {
                Console.Write(question);

                if (!String.IsNullOrEmpty(defaultAnswer))
                {
                    Console.Write(" [" + defaultAnswer + "]");
                }

                Console.Write(" ");

                string userInput = Console.ReadLine();

                if (String.IsNullOrEmpty(userInput))
                {
                    if (!String.IsNullOrEmpty(defaultAnswer)) return defaultAnswer;
                    if (allowNull) return null;
                    else continue;
                }

                return userInput;
            }
        }

        /// <summary>
        /// Display a prompt and return an integer response.
        /// </summary>
        /// <param name="question">The prompt to display.</param>
        /// <param name="defaultAnswer">The default response.</param>
        /// <param name="positiveOnly">Indicates whether or not a positive value must be supplied.</param>
        /// <param name="allowZero">Indicates whether or not zero is an acceptable response.</param>
        /// <returns>Integer.</returns>
        public static int InputInteger(string question, int defaultAnswer, bool positiveOnly, bool allowZero)
        {
            while (true)
            {
                Console.Write(question);
                Console.Write(" [" + defaultAnswer + "] ");

                string userInput = Console.ReadLine();

                if (String.IsNullOrEmpty(userInput))
                {
                    return defaultAnswer;
                }

                int ret = 0;
                if (!Int32.TryParse(userInput, out ret))
                {
                    Console.WriteLine("Please enter a valid integer.");
                    continue;
                }

                if (ret == 0)
                {
                    if (allowZero)
                    {
                        return 0;
                    }
                }

                if (ret < 0)
                {
                    if (positiveOnly)
                    {
                        Console.WriteLine("Please enter a value greater than zero.");
                        continue;
                    }
                }

                return ret;
            }
        }
    }
}

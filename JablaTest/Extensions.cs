using System;
using System.Runtime.CompilerServices;
using System.Text;

namespace CJablotron
{
    /// <summary>
    /// Методы расширения некоторых классов.
    /// </summary>
    public static class Extensions
    {
        /// <summary>
        /// Получить описание исключения.
        /// </summary>
        /// <remarks>
        /// Возвращает описание исключения, включая вложенные исключения.
        /// </remarks>
        /// <param name="ex">Исключение.</param>
        /// <returns>Строка с описанием исключения.</returns>
        public static string GetDescription(this Exception ex)
        {
            StringBuilder builder = new StringBuilder();
            for (Exception exception = ex; exception != null; exception = exception.InnerException)
            {
                if (builder.Length > 0)
                {
                    builder.AppendLine();
                    builder.Append('\t');
                }
                builder.AppendFormat("{0}: {1}", exception.GetType().FullName, exception.Message);
            }
            return builder.ToString();
        }

        /// <summary>
        /// Ищет исключение заданного типа в иерархии исключений.
        /// </summary>
        /// <typeparam name="T">Тип нужного исключения.</typeparam>
        /// <param name="ex">Текущее исключение.</param>
        /// <returns>Искомое исключение указанного типа.</returns>
        public static T GetException<T>(this Exception ex) where T: Exception
        {
            T result = null;
            if (ex is T)
            {
                result = ex as T;
            }
            else if (ex is AggregateException)
            {
                foreach (var innerEx in ((AggregateException)ex).InnerExceptions)
                {
                    result = innerEx.GetException<T>();
                    if (result != null)
                        break;
                }
            }
            else if (ex.InnerException != null)
            {
                result = ex.InnerException.GetException<T>();
            }
            return result;
        }

        /// <summary>
        /// Заменяет символы в строке.
        /// </summary>
        /// <param name="str">Исходная строка.</param>
        /// <param name="oldChars">Искомые символы.</param>
        /// <param name="newChars">Новые символы.</param>
        /// <returns>Результирующая строка.</returns>
        public static string ReplaceChars(this string str, string oldChars, string newChars)
        {
            if (oldChars == null)
                throw new ArgumentNullException("oldChars");
            if (newChars == null)
                throw new ArgumentNullException("newChars");
            if (oldChars.Length != newChars.Length)
                throw new ArgumentException("oldChars length must match newChars length");

            var chars = str.ToCharArray();
            for (int i = 0; i < chars.Length; i++)
            {
                int index = oldChars.IndexOf(chars[i]);
                if (index >= 0)
                    chars[i] = newChars[index];
            }

            return new String(chars);
        }

        public static string ReadLine(this System.IO.StreamReader reader, char endChar)
        {
            var sb = new StringBuilder(32);
            while(true)
            {
                int n = reader.Read();
                if (n == -1)
                    break;
                char ch = (char)n;
                if (ch == endChar)
                    break;
                sb.Append(ch);
            }
            return sb.ToString();
        }
    }
}

